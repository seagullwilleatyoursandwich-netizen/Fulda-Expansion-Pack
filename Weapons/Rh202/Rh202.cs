using FuldaExpansion1989.Sights;
using GHPC;
using GHPC.Camera;
using GHPC.Crew;
using GHPC.Equipment.Optics;
using GHPC.Equipment;
using GHPC.Player;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weaponry;
using GHPC.Weapons;
using MelonLoader;
using ModUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Fulda1989
{
    public class Rh202DamageStatusReporter : MonoBehaviour, IDamageStatus
    {
        public WeaponSystem Rh202Weapon { get; private set; }
        public AimablePlatform Traverse { get; private set; }
        public AimablePlatform Elevation { get; private set; }

        public void Bind(WeaponSystem weapon, AimablePlatform traverse, AimablePlatform elevation)
        {
            Rh202Weapon = weapon;
            Traverse = traverse;
            Elevation = elevation;
        }

        public bool Damaged
        {
            get
            {
                if (Rh202Weapon != null && Rh202Weapon.Damaged)
                    return true;
                if (Traverse != null && Traverse.Damaged)
                    return true;
                if (Elevation != null && Elevation.Damaged)
                    return true;

                return false;
            }
        }

        public string DamageStatus
        {
            get
            {
                StringBuilder builder = new StringBuilder();

                if (Rh202Weapon != null)
                {
                    if (Rh202Weapon._barrelDamaged)
                        builder.Append(RH202.Rh202BarrelHudLine);
                    if (Rh202Weapon._breechDamaged)
                        builder.Append(RH202.Rh202BreechHudLine);
                }

                if (Traverse != null && Traverse.Damaged)
                    builder.Append(RH202.Rh202MotorHudLine);

                return builder.ToString();
            }
        }
    }

    // Cosmetic driver that makes the citv follow the horizontal RCWS aim point
    public class PeriRh202Follower : MonoBehaviour
    {
        const string Rh202WeaponObjectName = "RH202_MarderWeapon";

        public AimablePlatform rh202Traverse;
        public AimablePlatform rh202Elevation;
        public Transform periTransform;
        public Transform periR12Optic;

        bool ShouldDriveCosmeticPeri()
        {
            PlayerInput input = PlayerInput.Instance;
            if (input == null)
                return true;

            Unit onUnit = rh202Traverse != null ? rh202Traverse._unit : null;
            if (onUnit != null && input.CurrentPlayerUnit != null && input.CurrentPlayerUnit != onUnit)
                return true;

            ICrewBrain crew = input.CurrentPlayerCrewBrain;
            if (crew == null)
                return true;

            if (crew.Position == CrewPosition.Commander)
                return false;

            WeaponSystem active = crew.WeaponsModule?.ActiveWeapon?.Weapon;
            return active != null && active.name == Rh202WeaponObjectName;
        }

        void Update()
        {
            if (rh202Traverse == null || rh202Elevation == null || periTransform == null || periR12Optic == null)
                return;

            if (!ShouldDriveCosmeticPeri())
                return;

            float targetY = rh202Traverse.Transform.localEulerAngles.y;
            float currentY = periTransform.localEulerAngles.y;
            float nextY = Mathf.MoveTowardsAngle(currentY, targetY, rh202Traverse.CurrentMaxSpeed * Time.deltaTime);

            float targetX = rh202Elevation.Transform.localEulerAngles.x;
            float currentX = periR12Optic.localEulerAngles.x;
            float nextX = Mathf.MoveTowardsAngle(currentX, targetX, rh202Elevation.CurrentMaxSpeed * Time.deltaTime);

            Vector3 periEuler = periTransform.localEulerAngles;
            periEuler.y = nextY;
            periTransform.localEulerAngles = periEuler;

            Vector3 opticEuler = periR12Optic.localEulerAngles;
            opticEuler.x = nextX;
            periR12Optic.localEulerAngles = opticEuler;
        }
    }

    public class RH202LaserPointCorrection : MonoBehaviour
    {
        public Transform laser;
        public UsableOptic dayOptic;
        public UsableOptic nightOptic;

        public Vector3 preservedSpotDay;
        public Quaternion preservedRotationDay;

        public Vector3 preservedSpotNight;
        public Quaternion preservedRotationNight;

        void Start()
        {
            laser.SetParent(dayOptic.transform, true);
            preservedSpotDay = laser.transform.localPosition;
            preservedRotationDay = laser.transform.localRotation;

            laser.SetParent(nightOptic.transform, true);
            preservedSpotNight = laser.transform.localPosition;
            preservedRotationNight = laser.transform.localRotation;

            laser.SetParent(dayOptic.transform, true);
        }

        void Update()
        {
            if (dayOptic.isActiveAndEnabled)
            {
                laser.SetParent(dayOptic.transform, true);
                laser.transform.SetLocalPositionAndRotation(preservedSpotDay, preservedRotationDay);
            }

            if (nightOptic.isActiveAndEnabled)
            {
                laser.SetParent(nightOptic.transform, true);
                laser.transform.SetLocalPositionAndRotation(preservedSpotNight, preservedRotationNight);
            }
        }
    }

    public sealed class Rh202CameraSlots : MonoBehaviour
    {
        public CameraSlot Rh202SightSlot { get; private set; }
        public CameraSlot Rh202NightSightSlot { get; private set; }
        public CameraSlot AuxSightSlot { get; private set; }
        public WeaponSystem Rh202Weapon { get; private set; }

        public void Configure(CameraSlot rh202DaySlot, CameraSlot rh202NightSlot, CameraSlot auxSightSlot, WeaponSystem rh202Weapon)
        {
            Rh202SightSlot = rh202DaySlot;
            Rh202NightSightSlot = rh202NightSlot;
            AuxSightSlot = auxSightSlot;
            Rh202Weapon = rh202Weapon;
        }
    }
    public class StaticWhenNotInUse : MonoBehaviour
    {
        public FireControlSystem fcs;
        public WeaponSystem weapon;

        Vehicle _vic;
        FireControlSystem _mantletFcs;
        ICrewBrain _gunner;
        bool _subscribed;

        public void Bind(WeaponSystem rh202Weapon, ICrewBrain gunner, Vehicle vic = null)
        {
            weapon = rh202Weapon;
            _gunner = gunner;
            _vic = vic ?? rh202Weapon?.GetComponentInParent<Vehicle>();
            CacheMantletFcs();
            TrySubscribe();
            ApplyOwnership();
        }

        void OnEnable()
        {
            TrySubscribe();
            ApplyOwnership();
        }

        void OnDisable() => Unsubscribe();

        void TrySubscribe()
        {
            if (_subscribed || _gunner?.WeaponsModule == null)
                return;

            _gunner.WeaponsModule.WeaponSystemChanged += OnWeaponSystemChanged;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed || _gunner?.WeaponsModule == null)
                return;

            _gunner.WeaponsModule.WeaponSystemChanged -= OnWeaponSystemChanged;
            _subscribed = false;
        }

        void OnWeaponSystemChanged(WeaponSystemInfo oldWeapon, WeaponSystemInfo newWeapon) => ApplyOwnership();

        void Update() => ApplyOwnership();

        void CacheMantletFcs()
        {
            if (_vic == null)
                _vic = weapon?.GetComponentInParent<Vehicle>();

            WeaponSystemInfo mainInfo = null;
            WeaponSystem mainGun = null;
            WeaponSystemInfo[] weapons = _vic?._weaponsManager?.Weapons;
            if (weapons != null)
            {
                for (int i = 0; i < weapons.Length; i++)
                {
                    if (weapons[i]?.Role != WeaponSystemRole.MainGun)
                        continue;

                    mainInfo = weapons[i];
                    mainGun = mainInfo.Weapon;
                    break;
                }
            }

            _mantletFcs = RH202.ResolveMainGunnerFcs(_vic, mainInfo, mainGun);
        }

        void ApplyOwnership()
        {
            if (fcs == null || weapon == null)
                return;

            if (_gunner?.WeaponsModule == null)
            {
                fcs.SetStabsActive(false);
                return;
            }

            bool owns = _gunner.WeaponsModule.ActiveWeapon?.Weapon == weapon;
            if (owns && !fcs.StabsActive)
                RH202.SeedRh202FcsAimFromBore(fcs, weapon);

            fcs.SetStabsActive(owns);

            if (fcs.enabled != owns)
                fcs.enabled = owns;

            if (_vic == null)
                _vic = weapon.GetComponentInParent<Vehicle>();

            if (owns)
            {
                WeaponSystemInfo activeInfo = _gunner?.WeaponsModule?.ActiveWeapon;
                WeaponSystem activeWs = activeInfo?.Weapon;
                if (activeWs == null)
                    return;

                if (activeWs.FCS != fcs)
                    activeWs.FCS = fcs;
                if (activeInfo.FCS != fcs)
                    activeInfo.FCS = fcs;

                if (fcs.CurrentWeaponSystem != activeWs)
                    fcs.SetWeaponSystem(activeWs);

                return;
            }

            if (_vic == null || _mantletFcs == null)
            {
                CacheMantletFcs();
                if (_mantletFcs == null)
                    return;
            }

            RH202.RepairNonRoofFcsAfterRh202Clone(_vic, fcs);

            WeaponSystemInfo active = _gunner?.WeaponsModule?.ActiveWeapon;
            WeaponSystem activeWeapon = active?.Weapon;
            if (activeWeapon == null || activeWeapon == weapon)
                return;

            if (activeWeapon.FCS != _mantletFcs)
                activeWeapon.FCS = _mantletFcs;
            if (active.FCS != _mantletFcs)
                active.FCS = _mantletFcs;

            if (_mantletFcs.CurrentWeaponSystem != activeWeapon)
                _mantletFcs.SetWeaponSystem(activeWeapon);

            WeaponSystemInfo[] all = _vic._weaponsManager?.Weapons;
            if (all == null)
                return;

            for (int i = 0; i < all.Length; i++)
            {
                WeaponSystemInfo info = all[i];
                if (info?.Weapon == null || info.Role == WeaponSystemRole.RoofGun)
                    continue;

                if (info.Weapon.FCS == fcs)
                    info.Weapon.FCS = _mantletFcs;
                if (info.FCS == fcs)
                    info.FCS = _mantletFcs;
            }
        }
    }

    public class RH202ReticleBothActive : MonoBehaviour
    {
        public Reticle.ReticleMesh primaryReticle;
        public Reticle.ReticleMesh wfovReticle;

        public void ForceActive()
        {
            if (primaryReticle != null)
            {
                primaryReticle.gameObject.SetActive(true);
                primaryReticle.enabled = true;
            }

            if (wfovReticle != null)
            {
                wfovReticle.gameObject.SetActive(true);
                wfovReticle.enabled = true;
            }
        }

        void LateUpdate()
        {
            ForceActive();
        }
    }

    public static class RH202
    {
        public static GHPC.Weapons.AmmoRack AmmoRack;
        public static AmmoType.AmmoClip APClip;
        public static AmmoType.AmmoClip HEClip;
        public static AmmoType.AmmoClip[] AvailableClips;
        public static CameraSlot AuxSightSlot;
        public static CameraSlot RH202Slot;

        // Append RH202 day slot to vehicle DesignatedCameraSlots.
        public static bool AppendRh202ToDesignatedCameraSlots = true;

        // Toggle <see cref="Rh202Diagnostics"/> install / +2f / +2s spawn logs.
        public static bool EnableFcsSpawnDiagnostics = false;

        const string Rh202BarrelZoneName = "RCWS barrel";
        const string Rh202BreechZoneName = "autocannon breech";
        const string Rh202TraverseZoneName = "autocannon traverse";
        const string Rh202WeaponDamageName = "RCWS";

        internal const string Rh202BarrelHudLine = "RCWS barrel damaged\n";
        internal const string Rh202BreechHudLine = "RCWS breech damaged\n";
        internal const string Rh202MotorHudLine = "RCWS motor damaged\n";

        const float Rh202BarrelHitHealth = 50f;
        const float Rh202BreechHitHealth = 150f;
        const float Rh202TraverseHitHealth = 150f;

        public enum Leopard2KVariant
        {
            K,
            KA1,
            KA2,
            KA3
        }

        sealed class FcsSterilizeRestore
        {
            public FireControlSystem Fcs;
            public bool WasEnabled;
            public bool StabsWereActive;
        }
        sealed class InstallState
        {
            public Vehicle Vic;
            public Transform Rh202Gun;
            public Leopard2KVariant Variant;
            public Transform Leo2KVisualTurret;
            public Transform VehicleTurret;
            public Transform GunScripts;
            public GameObject MarderGunPrefab;
            public FireControlSystem DonorFcs;
            public FireControlSystem Rh202Fcs;
            public WeaponSystem Weapon;
            public GHPC.Weapons.AmmoFeed Feed;
            public AimablePlatform TraversePlatform;
            public AimablePlatform ElevationPlatform;
            public CameraSlot DaySlot;
            public CameraSlot NightSlot;
            public CameraSlot GunnerFallbackSlot;
            public bool SightsInstalled;
            public List<FcsSterilizeRestore> SterilizedMantletFcs;
            public GameObject DayOpticObject;
            public GameObject NightOpticObject;
            public Transform PeriR12Transform;
            public Transform PeriR12Optic;
        }
        public static FireControlSystem ResolveMainGunnerFcs(Vehicle vic, WeaponSystemInfo mainGunInfo, WeaponSystem mainGun)
        {
            if (mainGunInfo?.FCS != null && mainGunInfo.FCS.gameObject.name != "RH202_FCS")
                return mainGunInfo.FCS;

            if (mainGun?.FCS != null && mainGun.FCS.gameObject.name != "RH202_FCS")
                return mainGun.FCS;

            if (vic == null)
                return null;

            foreach (FireControlSystem candidate in vic.GetComponentsInChildren<FireControlSystem>(true))
            {
                if (candidate != null && candidate.gameObject.name != "RH202_FCS")
                    return candidate;
            }

            return null;
        }

        public static void Setup(Vehicle vehicle, Transform gunTransform, Leopard2KVariant leopardVariant)
        {
            if (!TryBeginInstall(vehicle, gunTransform, leopardVariant, out InstallState state))
                return;

            if (!ResolveMarderDonors(state))
                return;

            ReparentRh202GameplayToVehicleTurret(state);

            Vehicle vic = state.Vic;
            state.SterilizedMantletFcs = BeginSterilizedRh202Install(vic);
            Transform rh202Gun = state.Rh202Gun;
            Leopard2KVariant variant = state.Variant;
            Transform leo2kVisualTurret = state.Leo2KVisualTurret;
            Transform vehicleTurret = state.VehicleTurret;

            UsableOptic[] donorFcsOptics = state.DonorFcs.GetComponentsInChildren<UsableOptic>(true);
            bool[] donorFcsOpticRegisters = new bool[donorFcsOptics.Length];
            for (int i = 0; i < donorFcsOptics.Length; i++)
            {
                donorFcsOpticRegisters[i] = donorFcsOptics[i].RegisterAtStart;
                donorFcsOptics[i].RegisterAtStart = false;
            }

            GameObject fcsClone = UnityEngine.Object.Instantiate(state.DonorFcs.gameObject, null, false);
            fcsClone.SetActive(false);

            for (int i = 0; i < donorFcsOptics.Length; i++)
                donorFcsOptics[i].RegisterAtStart = donorFcsOpticRegisters[i];

            fcsClone.name = "RH202_FCS";

            state.Rh202Fcs = fcsClone.GetComponent<FireControlSystem>();
            if (state.Rh202Fcs == null) { MelonLogger.Error("[RH202] Cloned FCS has no FireControlSystem."); return; }

            foreach (UsableOptic strayOptic in fcsClone.GetComponentsInChildren<UsableOptic>(true))
            {
                strayOptic.RegisterAtStart = false;
                if (strayOptic.gameObject != fcsClone)
                    strayOptic.gameObject.SetActive(false);
            }

            FireControlSystem rh202Fcs = state.Rh202Fcs;

            GameObject clone = UnityEngine.Object.Instantiate(state.MarderGunPrefab, state.GunScripts, false);
            Transform muzzleIdentity = state.Rh202Gun.Find("gun model/barrel/muzzleID");
            if (muzzleIdentity == null) { MelonLogger.Error("[RH202] Could not find RH202 muzzleID."); return; }

            clone.transform.SetParent(muzzleIdentity, false);

            Reparent reparent = clone.GetComponent<Reparent>();
            if (reparent != null) UnityEngine.Object.Destroy(reparent);

            clone.name = "RH202_MarderWeapon";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            WeaponSystem weapon = clone.GetComponent<WeaponSystem>();
            GHPC.Weapons.AmmoFeed feed = clone.GetComponent<GHPC.Weapons.AmmoFeed>();

            if (weapon == null) { MelonLogger.Error("[RH202] Cloned weapon has no WeaponSystem."); return; }
            if (feed == null) { MelonLogger.Error("[RH202] Cloned weapon has no AmmoFeed."); return; }

            state.Weapon = weapon;
            state.Feed = feed;

            foreach (AimablePlatform strayPlatform in clone.GetComponentsInChildren<AimablePlatform>(true))
            {
                if (strayPlatform != null)
                    UnityEngine.Object.Destroy(strayPlatform);
            }

            Transform audioTransform = clone.transform.Find("FMODWeaponAudio");
            if (audioTransform != null)
            {
                WeaponAudio weaponAudio = audioTransform.GetComponent<WeaponAudio>();
                if (weaponAudio != null)
                {
                    FieldInfo audioUnitField = typeof(WeaponAudio).GetField("_unit", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (audioUnitField != null) audioUnitField.SetValue(weaponAudio, vic);
                }
            }

            FieldInfo unitField = typeof(WeaponSystem).GetField("_unit", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo impulseRigidBodyField = typeof(WeaponSystem).GetField("_impulseRigidBody", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo muzzleIdentityField = typeof(WeaponSystem).GetField("_muzzleIdentity", BindingFlags.Instance | BindingFlags.NonPublic);

            if (unitField != null) unitField.SetValue(weapon, vic);
            if (impulseRigidBodyField != null) impulseRigidBodyField.SetValue(weapon, vic);
            if (muzzleIdentityField != null) muzzleIdentityField.SetValue(weapon, muzzleIdentity);

            FieldInfo crewManagerField = typeof(GHPC.Weapons.AmmoFeed).GetField("_crewManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo loadoutManagerField = typeof(GHPC.Weapons.AmmoFeed).GetField("_loadoutManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo rootRigidbodyField = typeof(GHPC.Weapons.AmmoFeed).GetField("_rootRigidbody", BindingFlags.Instance | BindingFlags.NonPublic);

            if (crewManagerField != null) crewManagerField.SetValue(feed, vic.InfoBroker._crewManager);
            if (loadoutManagerField != null) loadoutManagerField.SetValue(feed, vic.GetComponent<LoadoutManager>());

            if (rootRigidbodyField != null)
            {
                Component rootRigidbody = vic.GetComponent(rootRigidbodyField.FieldType);
                if (rootRigidbody == null) rootRigidbody = vic.GetComponentInChildren(rootRigidbodyField.FieldType, true);
                rootRigidbodyField.SetValue(feed, rootRigidbody);
            }

            AmmoCodexScriptable dm43Codex = null;
            AmmoCodexScriptable dm51a2Codex = null;
            AmmoCodexScriptable dm63Codex = null;
            AmmoCodexScriptable dm81Codex = Ammo_20mm_Bundeswehr.ammo_codex_dm81;

            foreach (AmmoCodexScriptable codex in Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>())
            {
                if (codex == null || codex.AmmoType == null) continue;
                if (codex.name == "ammo_20mm_DM43" && dm43Codex == null) dm43Codex = codex;
                if (codex.name == "ammo_20mm_DM51A2" && dm51a2Codex == null) dm51a2Codex = codex;
                if (codex.name == "ammo_20mm_DM63" && dm63Codex == null) dm63Codex = codex;
            }

            if (dm43Codex == null || dm51a2Codex == null || ((variant == Leopard2KVariant.KA1 || variant == Leopard2KVariant.KA2 || variant == Leopard2KVariant.KA3) && dm81Codex == null) || (variant == Leopard2KVariant.KA3 && dm63Codex == null))
            {
                MelonLogger.Error("[RH202] Required 20mm ammo codex entries are missing.");
                return;
            }

            AmmoType.AmmoClip rh202Dm43 = new AmmoType.AmmoClip
            {
                Name = "RH202 DM43 API-T",
                Capacity = 375,
                MinimalPattern = new AmmoCodexScriptable[] { dm43Codex }
            };

            AmmoType.AmmoClip rh202Dm51a2 = new AmmoType.AmmoClip
            {
                Name = "RH202 DM51A2 HE",
                Capacity = 375,
                MinimalPattern = new AmmoCodexScriptable[] { dm51a2Codex }
            };

            AmmoType.AmmoClip rh202Dm63 = new AmmoType.AmmoClip
            {
                Name = "RH202 DM63 APDS-T",
                Capacity = 375,
                MinimalPattern = new AmmoCodexScriptable[] { dm63Codex }
            };

            AmmoType.AmmoClip rh202Dm81 = new AmmoType.AmmoClip
            {
                Name = "RH202 DM81 HEI-T",
                Capacity = 375,
                MinimalPattern = new AmmoCodexScriptable[] { dm81Codex }
            };

            APClip = rh202Dm43;
            HEClip = rh202Dm51a2;

            if (variant == Leopard2KVariant.KA1 || variant == Leopard2KVariant.KA2 || variant == Leopard2KVariant.KA3)
                HEClip = rh202Dm81;

            if (variant == Leopard2KVariant.KA3)
                APClip = rh202Dm63;

            AvailableClips = new AmmoType.AmmoClip[] { APClip, HEClip };

            GameObject rackObject = new GameObject("RH202_AmmoRack");
            rackObject.transform.SetParent(clone.transform, false);
            rackObject.SetActive(false);

            GHPC.Weapons.AmmoRack rh202Rack = rackObject.AddComponent<GHPC.Weapons.AmmoRack>();
            rh202Rack.Name = "RH202 20mm Ammo Rack";
            rh202Rack.UseFixedAmmoLoad = true;
            rh202Rack.UseVisibleRounds = false;
            rh202Rack.ExposedRack = false;
            rh202Rack.ClipTypes = AvailableClips;
            rh202Rack._initialClipCounts = new int[] { 1, 1 };

            rackObject.SetActive(true);

            feed.ReadyRack = rh202Rack;
            AmmoRack = rh202Rack;
            feed.AmmoTypeInBreech = null;
            feed.LoadedClipType = null;

            Transform elevationTransform = rh202Gun.parent;
            Transform mountTransform = elevationTransform != null ? elevationTransform.parent : null;
            Transform traverseTransform = mountTransform != null ? mountTransform.parent : null;

            if (traverseTransform == null || elevationTransform == null)
            {
                MelonLogger.Error("[RH202] RH202 traverse/elevation pivot not found.");
                return;
            }

            AimablePlatform traversePlatform = traverseTransform.GetComponent<AimablePlatform>();
            AimablePlatform elevationPlatform = elevationTransform.GetComponent<AimablePlatform>();

            Transform periR12Transform = leo2kVisualTurret.Find("Peri R12 Transform");
            if (periR12Transform == null) { MelonLogger.Error("[RH202] Could not find Peri R12 Transform."); return; }

            Transform periR12Optic = periR12Transform.Find("Peri R12 Optic");
            if (periR12Optic == null) { MelonLogger.Error("[RH202] Could not find Peri R12 Optic."); return; }

            state.PeriR12Transform = periR12Transform;
            state.PeriR12Optic = periR12Optic;

            bool traverseWasActive = traverseTransform.gameObject.activeSelf;
            bool elevationWasActive = elevationTransform.gameObject.activeSelf;
            traverseTransform.gameObject.SetActive(false);
            elevationTransform.gameObject.SetActive(false);

            try
            {
                if (traversePlatform == null) traversePlatform = traverseTransform.gameObject.AddComponent<AimablePlatform>();
                traversePlatform.Axis = EnumHelpers.Axis3.Y;
                traversePlatform.Transform = traverseTransform;
                traversePlatform.HitZones = new GHPC.Equipment.DestructibleComponent[0];
                traversePlatform.StabilizerActive = true;
                traversePlatform.ProtectFromBadProjection = true;
                traversePlatform.ReverseLocalEuler = false;
                traversePlatform.SpeedPowered = 80f;
                traversePlatform.SpeedUnpowered = 8f;
                traversePlatform.LockTransformToRotation = true;
                traversePlatform._unit = vic;
                traversePlatform._equipmentManager = vic._equipmentManager;

                if (elevationPlatform == null) elevationPlatform = elevationTransform.gameObject.AddComponent<AimablePlatform>();
                elevationPlatform.Axis = EnumHelpers.Axis3.X;
                elevationPlatform.Transform = elevationTransform;
                elevationPlatform.HitZones = new GHPC.Equipment.DestructibleComponent[0];
                elevationPlatform.StabilizerActive = true;
                elevationPlatform.ProtectFromBadProjection = true;
                elevationPlatform.ReverseLocalEuler = true;
                elevationPlatform.SpeedPowered = 48f;
                elevationPlatform.SpeedUnpowered = 6f;
                elevationPlatform.LocalEulerLimits = new Vector2(-9f, 45f);
                elevationPlatform.LockTransformToRotation = true;
                elevationPlatform._unit = vic;
                elevationPlatform._equipmentManager = vic._equipmentManager;
                elevationPlatform.ParentPlatform = traversePlatform;
            }
            finally
            {
                traverseTransform.gameObject.SetActive(traverseWasActive);
                elevationTransform.gameObject.SetActive(elevationWasActive);
            }

            PeriRh202Follower periFollower = periR12Transform.GetComponent<PeriRh202Follower>();
            if (periFollower == null)
                periFollower = periR12Transform.gameObject.AddComponent<PeriRh202Follower>();

            periFollower.rh202Traverse = traversePlatform;
            periFollower.rh202Elevation = elevationPlatform;
            periFollower.periTransform = periR12Transform;
            periFollower.periR12Optic = periR12Optic;

            CameraSlot rh202Slot = null;
            CameraSlot nightSlot = null;

            Transform marderPeriZ11 = LEO2K_Assets.marder.transform.Find("Marder1A1_rig/hull/turret/PERI Z11");

            if (marderPeriZ11 == null)
            {
                foreach (Transform t in LEO2K_Assets.marder.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "PERI Z11")
                    {
                        marderPeriZ11 = t;
                        break;
                    }
                }
            }

            if (marderPeriZ11 == null)
            {
                MelonLogger.Error("[RH202] Could not find Marder PERI Z11.");
                return;
            }

            UsableOptic donorOptic = marderPeriZ11.GetComponent<UsableOptic>();
            CameraSlot donorSlot = marderPeriZ11.GetComponent<CameraSlot>();

            if (donorOptic == null || donorSlot == null)
            {
                MelonLogger.Error("[RH202] Marder PERI Z11 is missing UsableOptic or CameraSlot.");
                return;
            }

            bool donorRegisterAtStart = donorOptic.RegisterAtStart;
            Transform donorReparentTarget = donorSlot._reparentTarget;
            CameraSlot donorLinkedNightSight = donorSlot.LinkedNightSight;
            bool donorOpticWasActive = marderPeriZ11.gameObject.activeSelf;

            donorOptic.RegisterAtStart = false;
            donorSlot._reparentTarget = vehicleTurret;
            donorSlot.LinkedNightSight = null;

            GameObject opticClone = UnityEngine.Object.Instantiate(marderPeriZ11.gameObject, vehicleTurret, true);

            marderPeriZ11.gameObject.SetActive(donorOpticWasActive);
            donorOptic.RegisterAtStart = donorRegisterAtStart;
            donorSlot._reparentTarget = donorReparentTarget;
            donorSlot.LinkedNightSight = donorLinkedNightSight;

            Reparent opticReparent = opticClone.GetComponent<Reparent>();
            if (opticReparent != null) UnityEngine.Object.Destroy(opticReparent);

            opticClone.name = "RH202_Optic";
            opticClone.transform.SetPositionAndRotation(periR12Optic.position, periR12Optic.rotation);
            opticClone.transform.localScale = Vector3.one;
            state.DayOpticObject = opticClone;

            UsableOptic optic = opticClone.GetComponent<UsableOptic>();
            rh202Slot = opticClone.GetComponent<CameraSlot>();
            optic.RegisterAtStart = true;

            if (optic == null || rh202Slot == null)
            {
                MelonLogger.Error("[RH202] RH202 optic clone is missing UsableOptic or CameraSlot.");
                UnityEngine.Object.Destroy(opticClone);
                return;
            }

            RH202Slot = rh202Slot;

            Transform auxSightTransform = vehicleTurret.Find("1A3 mantlet/--Gun Scripts--/Aux sight TZF1A");

            if (auxSightTransform == null)
            {
                MelonLogger.Error("[RH202 CAM] Could not find Aux sight TZF1A.");
            }
            else
            {
                AuxSightSlot = auxSightTransform.GetComponent<CameraSlot>();

                if (AuxSightSlot == null)
                    MelonLogger.Error("[RH202 CAM] Aux sight TZF1A has no CameraSlot component.");
            }

            rh202Slot.LinkedNightSight = null;
            rh202Slot.ExclusiveWeapons = new WeaponSystem[] { weapon };
            rh202Slot._pairedOptic = optic;
            rh202Slot._reparentTarget = periR12Optic;

            LateFollow rh202LateFollow = opticClone.GetComponent<LateFollow>();
            if (rh202LateFollow == null)
                rh202LateFollow = opticClone.AddComponent<LateFollow>();

            rh202LateFollow.FollowTarget = periR12Optic;
            rh202LateFollow.ForceToRoot = false;
            rh202LateFollow.SuppressWarnings = true;

            rh202Fcs.Mounts = new AimablePlatform[] { traversePlatform, elevationPlatform };
            rh202Fcs.MainOptic = optic;
            rh202Fcs.ReferenceTransform = elevationTransform;
            optic.FCS = rh202Fcs;
            optic.Alignment = OpticAlignment.ElevationCompensated;
            optic.RotateAzimuth = true;
            optic.RotateElevation = true;
            optic.LocalElevationLimits = elevationPlatform.LocalEulerLimits;
            if (optic.reticleMesh != null)
                optic.reticleMesh.Load();

            if (variant == Leopard2KVariant.KA2 || variant == Leopard2KVariant.KA3)
            {
                optic.Alignment = OpticAlignment.BoresightStabilized;
            }

            rh202Fcs.LinkedWeaponSystems = new WeaponSystem[] { weapon };

            fcsClone.transform.SetParent(state.GunScripts, false);
            fcsClone.transform.localPosition = Vector3.zero;
            fcsClone.transform.localRotation = Quaternion.identity;
            fcsClone.transform.localScale = Vector3.one;

            rh202Fcs.CurrentStabMode = StabilizationMode.Vector;
            rh202Fcs.SetStabsActive(false);
            weapon.FCS = rh202Fcs;
            RebindRh202FcsAfterInstall(rh202Fcs, vic, traversePlatform, elevationPlatform, elevationTransform, weapon, optic);

            bool nightDonorRegisterAtStart = donorOptic.RegisterAtStart;
            Transform nightDonorReparentTarget = donorSlot._reparentTarget;
            CameraSlot nightDonorLinkedNightSight = donorSlot.LinkedNightSight;

            donorOptic.RegisterAtStart = false;
            donorSlot._reparentTarget = vehicleTurret;
            donorSlot.LinkedNightSight = null;

            GameObject nightOpticClone = UnityEngine.Object.Instantiate(marderPeriZ11.gameObject, vehicleTurret, true);

            donorOptic.RegisterAtStart = nightDonorRegisterAtStart;
            donorSlot._reparentTarget = nightDonorReparentTarget;
            donorSlot.LinkedNightSight = nightDonorLinkedNightSight;

            Reparent nightReparent = nightOpticClone.GetComponent<Reparent>();
            if (nightReparent != null)
                UnityEngine.Object.Destroy(nightReparent);

            nightOpticClone.name = "RH202_NightOptic";
            nightOpticClone.transform.SetPositionAndRotation(periR12Optic.position, periR12Optic.rotation);
            nightOpticClone.transform.localScale = Vector3.one;
            state.NightOpticObject = nightOpticClone;

            UsableOptic nightOptic = nightOpticClone.GetComponent<UsableOptic>();
            nightSlot = nightOpticClone.GetComponent<CameraSlot>();

            if (nightOptic == null || nightSlot == null)
            {
                MelonLogger.Error("[RH202 NIGHT] Marder PERI Z11 clone is missing UsableOptic or CameraSlot.");
                UnityEngine.Object.Destroy(nightOpticClone);
                return;
            }

            nightOptic.RegisterAtStart = true;

            nightSlot.name = "RH202_NightSlot";
            nightSlot.IsLinkedNightSight = true;
            nightSlot.LinkedDaySight = rh202Slot;
            nightSlot.LinkedNightSight = null;
            nightSlot._pairedOptic = nightOptic;
            nightSlot.ExclusiveWeapons = new WeaponSystem[] { weapon };
            nightSlot._reparentTarget = periR12Optic;

            LateFollow nightFollow = nightOpticClone.GetComponent<LateFollow>();
            if (nightFollow == null)
                nightFollow = nightOpticClone.AddComponent<LateFollow>();

            nightFollow.FollowTarget = periR12Optic;
            nightFollow.ForceToRoot = false;
            nightFollow.SuppressWarnings = true;

            nightOptic.FCS = rh202Fcs;
            nightOptic.Alignment = optic.Alignment;
            nightOptic.RotateAzimuth = true;
            nightOptic.RotateElevation = true;
            nightOptic.LocalElevationLimits = elevationPlatform.LocalEulerLimits;

            rh202Slot.LinkedNightSight = nightSlot;
            rh202Fcs.NightOptic = nightOptic;

            nightSlot.VisionType = NightVisionType.Intensifier;

            if (variant == Leopard2KVariant.KA2 || variant == Leopard2KVariant.KA3)
            {
                GameObject lase = new GameObject("RH202_LaserOrigin");
                lase.transform.SetParent(optic.transform, false);

                rh202Fcs.LaserOrigin = lase.transform;
                rh202Fcs.LaserAim = LaserAimMode.Fixed;
                rh202Fcs.MaxLaserRange = 4000f;
                rh202Fcs.OpticalRangefinder = null;

                RH202LaserPointCorrection laserFix = rh202Fcs.gameObject.GetComponent<RH202LaserPointCorrection>();
                if (laserFix == null)
                    laserFix = rh202Fcs.gameObject.AddComponent<RH202LaserPointCorrection>();

                laserFix.laser = lase.transform;
                laserFix.dayOptic = optic;
                laserFix.nightOptic = nightOptic;
            }

            if (variant != Leopard2KVariant.KA3)
            {
                Transform periR12PostObject = vehicleTurret.Find("1A3 mantlet/--Gun Scripts--/PERI-R12");

                if (periR12PostObject == null)
                {
                    MelonLogger.Error("[RH202 NIGHT] Could not find runtime PERI-R12 GameObject.");
                }
                else
                {
                    UnityEngine.Rendering.PostProcessing.PostProcessVolume periPost =
                        periR12PostObject.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();

                    UnityEngine.Rendering.PostProcessing.PostProcessVolume nightPost =
                        nightOpticClone.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();

                    if (periPost == null)
                    {
                        MelonLogger.Error("[RH202 NIGHT] PERI-R12 GameObject has no PostProcessVolume.");
                    }
                    else if (nightPost == null)
                    {
                        MelonLogger.Error("[RH202 NIGHT] RH202_NightOptic has no PostProcessVolume.");
                    }
                    else
                    {
                        nightPost.sharedProfile = periPost.sharedProfile;
                        nightPost.profile = periPost.profile;
                        nightPost.isGlobal = periPost.isGlobal;
                        nightPost.blendDistance = periPost.blendDistance;
                        nightPost.weight = periPost.weight;
                        nightPost.priority = periPost.priority;
                        nightPost.enabled = true;
                    }
                }
            }

            if (variant != Leopard2KVariant.KA3)
            {
                Transform pzbReticle = vehicleTurret.Find("1A3 mantlet/--Gun Scripts--/PERI-R12/pzb reticle");
                Transform newNfov = vehicleTurret.Find("LEO1A1A1_rig/HULL/TURRET/RH202_NightSlot");
                UnityEngine.GameObject.Destroy(newNfov);

                if (pzbReticle == null)
                {
                    MelonLogger.Error("[RH202 NIGHT] Could not find native PERI-R12 pzb reticle.");
                }
                else
                {
                    Transform oldWfov = nightOpticClone.transform.Find("WFOV reticle");
                    Transform oldNfov = nightOpticClone.transform.Find("NFOV reticle");

                    if (oldWfov != null)
                    {
                        Reticle.PostMeshComp wfovPost = oldWfov.GetComponent<Reticle.PostMeshComp>();

                        if (wfovPost != null)
                            UnityEngine.Object.Destroy(wfovPost);

                        GameObject wfovClone = UnityEngine.Object.Instantiate(pzbReticle.gameObject, oldWfov, false);
                        wfovClone.name = "RH202_PZB_WFOV";
                        wfovClone.transform.localPosition = pzbReticle.localPosition;
                        wfovClone.transform.localRotation = pzbReticle.localRotation;
                        wfovClone.transform.localScale = pzbReticle.localScale;
                        wfovClone.SetActive(true);

                        Transform wfovBoresight = wfovClone.transform.Find("FFP/Boresight");

                        if (wfovBoresight != null)
                        {
                            wfovBoresight.localScale = new Vector3(4f, 4f, 4f);
                        }
                        else
                        {
                            MelonLogger.Warning("[RH202 NIGHT] Could not find WFOV PZB Boresight.");
                        }
                    }
                    else
                    {
                        MelonLogger.Error("[RH202 NIGHT] Could not find original WFOV reticle.");
                    }

                    if (oldNfov != null)
                    {
                        Reticle.PostMeshComp nfovPost = oldNfov.GetComponent<Reticle.PostMeshComp>();

                        if (nfovPost != null)
                            UnityEngine.Object.Destroy(nfovPost);

                        GameObject nfovClone = UnityEngine.Object.Instantiate(pzbReticle.gameObject, oldNfov, false);
                        nfovClone.name = "RH202_PZB_NFOV";
                        nfovClone.transform.localPosition = pzbReticle.localPosition;
                        nfovClone.transform.localRotation = pzbReticle.localRotation;
                        nfovClone.transform.localScale = pzbReticle.localScale;
                        nfovClone.SetActive(true);

                        Transform nfovBoresight = nfovClone.transform.Find("FFP/Boresight");

                        if (nfovBoresight != null)
                        {
                            nfovBoresight.localScale = new Vector3(1.45f, 1.45f, 1.45f);
                        }
                        else
                        {
                            MelonLogger.Warning("[RH202 NIGHT] Could not find NFOV PZB Boresight.");
                        }
                    }
                    else
                    {
                        MelonLogger.Error("[RH202 NIGHT] Could not find original NFOV reticle.");
                    }
                }
            }

            if (variant == Leopard2KVariant.KA3)
            {
                LEO2KThermals.Add(nightOptic, LEO2KThermals.ThermalVariant.Leopard2K2);

                Reticle.ReticleMesh primaryReticle = nightOptic.reticleMesh;
                Reticle.ReticleMesh wfovReticle = null;

                Transform wfovTransform = nightOptic.transform.Find("__LEO2K_THERMAL_WFOV__");
                if (wfovTransform != null)
                    wfovReticle = wfovTransform.GetComponent<Reticle.ReticleMesh>();

                if (primaryReticle != null && wfovReticle != null)
                {
                    RH202ReticleBothActive bothActive = nightOptic.GetComponent<RH202ReticleBothActive>();
                    if (bothActive == null)
                        bothActive = nightOptic.gameObject.AddComponent<RH202ReticleBothActive>();

                    bothActive.primaryReticle = primaryReticle;
                    bothActive.wfovReticle = wfovReticle;
                    bothActive.ForceActive();
                }
                else
                {
                    MelonLogger.Warning("[RH202] Could not find both thermal reticles to force active.");
                }
            }

            state.TraversePlatform = traversePlatform;
            state.ElevationPlatform = elevationPlatform;
            state.Rh202Fcs = rh202Fcs;

            BindAuxSightToMainAndCoax(vic, AuxSightSlot, weapon);

            state.GunnerFallbackSlot = ResolveGunnerFallbackSlot(vic, rh202Slot, nightSlot, AuxSightSlot);
            BindRh202SlotDestroyFallback(rh202Slot, nightSlot, state.GunnerFallbackSlot);

            Rh202CameraSlots rh202Cam = vic.GetComponent<Rh202CameraSlots>();
            if (rh202Cam == null)
                rh202Cam = vic.gameObject.AddComponent<Rh202CameraSlots>();
            rh202Cam.Configure(rh202Slot, nightSlot, AuxSightSlot, weapon);

            WeaponSystemInfo rh202Info = new WeaponSystemInfo
            {
                Name = "20mm RCWS Rh202",
                FCS = rh202Fcs,
                Weapon = weapon,
                Role = WeaponSystemRole.RoofGun,
                PreAimWeapon = WeaponSystemRole.RoofGun
            };

            bool replacedRoofGun = false;
            WeaponSystemInfo[] weapons = vic._weaponsManager.Weapons;

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null || weapons[i].Role != WeaponSystemRole.RoofGun) continue;
                weapons[i] = rh202Info;
                replacedRoofGun = true;
                break;
            }

            if (!replacedRoofGun)
            {
                Array.Resize(ref weapons, weapons.Length + 1);
                weapons[weapons.Length - 1] = rh202Info;
            }

            vic._weaponsManager.Weapons = weapons;

            ICrewBrain gunnerBrain = vic.InfoBroker._crewManager.GetCrewBrain(CrewPosition.Gunner);

            if (gunnerBrain != null && gunnerBrain.WeaponsModule != null)
                ReplaceGunnerRoofWeaponEntry(gunnerBrain.WeaponsModule.Weapons, rh202Info);
            else
                MelonLogger.Error("[RH202] Gunner WeaponsModule not found.");

            state.DaySlot = rh202Slot;
            state.NightSlot = nightSlot;
            state.TraversePlatform = traversePlatform;
            state.ElevationPlatform = elevationPlatform;
            state.SightsInstalled = rh202Slot != null;

            WireRh202DamageModel(vic, weapon, state.Rh202Gun, traversePlatform, elevationPlatform);
            RepairNonRoofFcsAfterRh202Clone(vic, rh202Fcs);
            EndSterilizedRh202Install(state);
            Rh202Diagnostics.QueueSpawn(vic, rh202Fcs);

            FinalizeGunnerCameraIntegration(state);
        }

        static bool TryBeginInstall(Vehicle vic, Transform rh202Gun, Leopard2KVariant variant, out InstallState state)
        {
            state = null;

            if (rh202Gun == null) { MelonLogger.Error("[RH202] RH202 gun is null."); return false; }
            if (vic == null) { MelonLogger.Error("[RH202] Vehicle is null."); return false; }
            if (rh202Gun.Find("RH202_MarderWeapon") != null) { MelonLogger.Warning("[RH202] RH202_MarderWeapon already exists."); return false; }
            if (LEO2K_Assets.marder == null) { MelonLogger.Error("[RH202] Marder donor was not loaded."); return false; }

            Transform leo2kVisualTurret = rh202Gun;
            while (leo2kVisualTurret != null && leo2kVisualTurret.name != "turret")
                leo2kVisualTurret = leo2kVisualTurret.parent;

            if (leo2kVisualTurret == null) { MelonLogger.Error("[RH202] Could not find runtime Leopard 2K turret."); return false; }

            Transform gunScripts = leo2kVisualTurret.Find("20MM RIG/RH202_TRAVERSE_PIVOT/20mm_MOUNT/Gun Script");
            if (gunScripts == null) { MelonLogger.Error("[RH202] Could not find runtime RH202 Gun Script."); return false; }

            Transform vehicleTurret = vic.transform.Find("LEO1A1A1_rig/HULL/TURRET");
            if (vehicleTurret == null) { MelonLogger.Error("[RH202] Could not find runtime vehicle turret."); return false; }

            state = new InstallState
            {
                Vic = vic,
                Rh202Gun = rh202Gun,
                Variant = variant,
                Leo2KVisualTurret = leo2kVisualTurret,
                VehicleTurret = vehicleTurret,
                GunScripts = gunScripts
            };

            return true;
        }

        static bool ResolveMarderDonors(InstallState state)
        {
            foreach (Transform t in LEO2K_Assets.marder.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "20mm Gun MK20" && state.MarderGunPrefab == null)
                    state.MarderGunPrefab = t.gameObject;
            }

            if (state.MarderGunPrefab == null)
            {
                MelonLogger.Error("[RH202] Could not find vanilla Marder 20mm Gun MK20.");
                return false;
            }

            foreach (FireControlSystem fcs in LEO2K_Assets.marder.GetComponentsInChildren<FireControlSystem>(true))
            {
                if (fcs.name == "FCS and sights")
                {
                    state.DonorFcs = fcs;
                    break;
                }
            }

            if (state.DonorFcs == null)
            {
                MelonLogger.Error("[RH202] Could not find Marder FCS.");
                return false;
            }

            return true;
        }
        static void ReplaceGunnerRoofWeaponEntry(List<WeaponSystemInfo> gunnerWeapons, WeaponSystemInfo rh202Info)
        {
            for (int i = gunnerWeapons.Count - 1; i >= 0; i--)
            {
                if (gunnerWeapons[i]?.Role == WeaponSystemRole.RoofGun)
                    gunnerWeapons.RemoveAt(i);
            }

            if (!gunnerWeapons.Contains(rh202Info))
                gunnerWeapons.Add(rh202Info);
        }
        static List<FcsSterilizeRestore> BeginSterilizedRh202Install(Vehicle vic)
        {
            List<FcsSterilizeRestore> restores = new List<FcsSterilizeRestore>();
            if (vic == null)
                return restores;

            foreach (FireControlSystem fcs in vic.GetComponentsInChildren<FireControlSystem>(true))
            {
                if (fcs == null || string.Equals(fcs.gameObject.name, "RH202_FCS", StringComparison.Ordinal))
                    continue;

                restores.Add(new FcsSterilizeRestore
                {
                    Fcs = fcs,
                    WasEnabled = fcs.enabled,
                    StabsWereActive = fcs.StabsActive
                });

                fcs.SetStabsActive(false);
                fcs.enabled = false;
            }

            //if (restores.Count > 0)
            //    MelonLogger.Msg("[RH202] Sterilized " + restores.Count + " mantlet FCS during RCWS install.");

            return restores;
        }

        static void EndSterilizedRh202Install(InstallState state)
        {
            if (state?.Vic == null || state.Rh202Fcs == null)
                return;

            Vehicle vic = state.Vic;
            FireControlSystem rh202Fcs = state.Rh202Fcs;
            WeaponSystem roofWeapon = state.Weapon;

            if (state.GunScripts != null && rh202Fcs.transform.parent != state.GunScripts)
            {
                rh202Fcs.transform.SetParent(state.GunScripts, false);
                rh202Fcs.transform.localPosition = Vector3.zero;
                rh202Fcs.transform.localRotation = Quaternion.identity;
                rh202Fcs.transform.localScale = Vector3.one;
            }

            WeaponSystem mainWeapon = null;
            FireControlSystem mantletFcs = null;
            WeaponSystemInfo[] weapons = vic._weaponsManager?.Weapons;
            if (weapons != null)
            {
                for (int i = 0; i < weapons.Length; i++)
                {
                    WeaponSystemInfo info = weapons[i];
                    if (info == null || info.Role != WeaponSystemRole.MainGun)
                        continue;

                    mainWeapon = info.Weapon;
                    mantletFcs = ResolveMainGunnerFcs(vic, info, mainWeapon);
                    break;
                }
            }

            if (state.SterilizedMantletFcs != null)
            {
                for (int i = 0; i < state.SterilizedMantletFcs.Count; i++)
                {
                    FcsSterilizeRestore entry = state.SterilizedMantletFcs[i];
                    if (entry?.Fcs == null)
                        continue;

                    entry.Fcs.enabled = entry.WasEnabled;
                    if (entry.Fcs == mantletFcs && mainWeapon != null)
                        entry.Fcs.SetWeaponSystem(mainWeapon);
                    else if (entry.StabsWereActive)
                        entry.Fcs.SetStabsActive(true);
                }
            }
            else if (mantletFcs != null && mainWeapon != null)
            {
                mantletFcs.SetWeaponSystem(mainWeapon);
            }

            if (!rh202Fcs.gameObject.activeSelf)
                rh202Fcs.gameObject.SetActive(true);

            rh202Fcs.SetStabsActive(false);
            if (roofWeapon != null)
                roofWeapon.FCS = rh202Fcs;

            ICrewBrain gunner = vic.InfoBroker?._crewManager?.GetCrewBrain(CrewPosition.Gunner);
            StaticWhenNotInUse stabGate = rh202Fcs.gameObject.GetComponent<StaticWhenNotInUse>();
            if (stabGate == null)
                stabGate = rh202Fcs.gameObject.AddComponent<StaticWhenNotInUse>();
            stabGate.fcs = rh202Fcs;
            if (roofWeapon != null)
                stabGate.Bind(roofWeapon, gunner, vic);

            LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();
            if (loadoutManager != null)
                loadoutManager.RegisterAllBallistics();

            //MelonLogger.Msg("[RH202] End sterilized install: mantlet FCS restored, RH202_FCS active, ballistics re-registered.");
        }
        static void ReparentRh202GameplayToVehicleTurret(InstallState state)
        {
            Transform vehicleTurret = state?.VehicleTurret;
            Transform visualTurret = state?.Leo2KVisualTurret;
            if (vehicleTurret == null || visualTurret == null)
                return;

            Transform twentyMmRig = visualTurret.Find("20MM RIG");
            if (twentyMmRig == null)
            {
                MelonLogger.Error("[RH202] Could not find 20MM RIG to reparent to gameplay turret.");
                return;
            }

            if (twentyMmRig.parent == vehicleTurret)
                return;

            twentyMmRig.SetParent(vehicleTurret, true);
            //MelonLogger.Msg("[RH202] Reparented 20MM RIG under gameplay turret (" + Rh202Diagnostics.DescribeHierarchyPath(vehicleTurret, state.Vic != null ? state.Vic.transform : null) + "), world pose preserved.");
        }

        /// <summary>
        /// Marder FCS Awake runs on Instantiate while ReferenceTransform / AimPoint still point at the temp donor vehicle.
        /// Rebind Leo mounts and clear that stale world aim so elevation does not snap at a ghost point.
        /// </summary>
        internal static void RebindRh202FcsAfterInstall(
            FireControlSystem fcs,
            Vehicle vic,
            AimablePlatform traverse,
            AimablePlatform elevation,
            Transform referenceTransform,
            WeaponSystem weapon,
            UsableOptic mainOptic)
        {
            if (fcs == null || weapon == null || referenceTransform == null || traverse == null || elevation == null)
                return;

            fcs.ReferenceTransform = referenceTransform;
            fcs.TurretPlatform = traverse;
            fcs.Mounts = new AimablePlatform[] { traverse, elevation };
            fcs.GatedAimablePlatforms = new AimablePlatform[] { traverse, elevation };
            fcs.LinkedWeaponSystems = new WeaponSystem[] { weapon };
            if (mainOptic != null)
                fcs.MainOptic = mainOptic;

            Transform bore = weapon.MuzzleIdentity;
            if (bore == null)
                bore = weapon.transform;

            if (fcs.LaserOrigin == null)
                fcs.LaserOrigin = referenceTransform;

            if (vic != null && fcs.Chassis == null)
                fcs.Chassis = vic.GetComponent<IChassis>();

            float range = fcs.TargetRange;
            if (range <= 0f)
                range = fcs.CurrentRange;
            if (range <= 0f)
                range = fcs.DefaultRange;
            if (range <= 0f)
                range = 1000f;

            Vector3 forward = bore.forward;
            fcs.AimWorldVector = forward;
            fcs.AimPoint = referenceTransform.position + forward * range;

            AimablePlatform[] mounts = fcs.Mounts;
            for (int i = 0; i < mounts.Length; i++)
            {
                if (mounts[i] != null)
                    mounts[i].ForceAimVectorNow(forward);
            }

            if (fcs._initialized)
                fcs.SetAimVector(forward, worldSpace: true);
        }

        internal static void SeedRh202FcsAimFromBore(FireControlSystem fcs, WeaponSystem weapon)
        {
            if (fcs == null || weapon == null)
                return;

            Transform bore = weapon.MuzzleIdentity;
            if (bore == null)
                bore = weapon.transform;

            Transform reference = fcs.ReferenceTransform;
            if (reference != null)
            {
                float range = fcs.TargetRange;
                if (range <= 0f)
                    range = fcs.CurrentRange;
                if (range <= 0f)
                    range = 1000f;

                Vector3 forward = bore.forward;
                fcs.AimWorldVector = forward;
                fcs.AimPoint = reference.position + forward * range;
            }

            if (fcs.Mounts != null)
            {
                for (int i = 0; i < fcs.Mounts.Length; i++)
                {
                    if (fcs.Mounts[i] != null)
                        fcs.Mounts[i].ForceAimVectorNow(bore.forward);
                }
            }

            if (fcs._initialized)
                fcs.SetAimVector(bore.forward, worldSpace: true);
        }

        internal static void RepairNonRoofFcsAfterRh202Clone(Vehicle vic, FireControlSystem rh202Fcs)
        {
            if (vic == null || rh202Fcs == null)
                return;

            FireControlSystem mantletFcs = null;
            foreach (FireControlSystem candidate in vic.GetComponentsInChildren<FireControlSystem>(true))
            {
                if (candidate == null || candidate == rh202Fcs)
                    continue;

                mantletFcs = candidate;
                break;
            }

            if (mantletFcs == null)
                return;

            WeaponSystemInfo[] weapons = vic._weaponsManager?.Weapons;
            if (weapons == null)
                return;

            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponSystemInfo info = weapons[i];
                if (info?.Weapon == null || info.Role == WeaponSystemRole.RoofGun)
                    continue;

                if (info.Weapon.FCS != rh202Fcs && info.FCS != rh202Fcs)
                    continue;

                info.Weapon.FCS = mantletFcs;
                info.FCS = mantletFcs;
                //MelonLogger.Msg("[RH202] Repaired " + info.Role + " FCS pointer after RCWS FCS clone.");
            }
        }

        static void BindAuxSightToMainAndCoax(Vehicle vic, CameraSlot auxSightSlot, WeaponSystem rh202Weapon)
        {
            if (auxSightSlot == null || vic?._weaponsManager?.Weapons == null)
                return;

            List<WeaponSystem> exclusive = new List<WeaponSystem>();

            foreach (WeaponSystemInfo info in vic._weaponsManager.Weapons)
            {
                if (info?.Weapon == null || info.Weapon == rh202Weapon)
                    continue;

                if (info.Role == WeaponSystemRole.MainGun || info.Role == WeaponSystemRole.Coaxial)
                    exclusive.Add(info.Weapon);
            }

            if (exclusive.Count == 0)
            {
                MelonLogger.Warning("[RH202] Aux sight blocker: no main/coax weapons found for TZF1A exclusivity.");
                return;
            }

            WeaponSystem[] weapons = exclusive.ToArray();
            auxSightSlot.ExclusiveWeapons = weapons;

            CameraSlot auxNight = auxSightSlot.LinkedNightSight;
            if (auxNight != null)
                auxNight.ExclusiveWeapons = weapons;
        }
        static CameraSlot ResolveGunnerFallbackSlot(Vehicle vic, CameraSlot rh202Day, CameraSlot rh202Night, CameraSlot auxSight)
        {
            CameraSlot[] slots = vic?.DesignatedCameraSlots;
            if (slots == null)
                return null;

            foreach (CameraSlot slot in slots)
            {
                if (slot == null || slot.AllowFreeLook)
                    continue;

                if (slot == rh202Day || slot == rh202Night)
                    continue;

                if (slot.IsLinkedNightSight)
                    continue;

                if (auxSight != null && slot == auxSight)
                    continue;

                return slot;
            }

            return auxSight;
        }

        static void BindRh202SlotDestroyFallback(CameraSlot rh202Day, CameraSlot rh202Night, CameraSlot fallback)
        {
            if (fallback == null)
                return;

            if (rh202Day != null)
                rh202Day.SlotToUseOnDestroy = fallback;

            if (rh202Night != null)
                rh202Night.SlotToUseOnDestroy = fallback;
        }

        static void FinalizeGunnerCameraIntegration(InstallState state)
        {
            if (!state.SightsInstalled || !AppendRh202ToDesignatedCameraSlots || state.DaySlot == null)
                return;

            Vehicle vic = state.Vic;
            CameraSlot[] cameraSlots = vic.DesignatedCameraSlots;
            Array.Resize(ref cameraSlots, cameraSlots.Length + 1);
            cameraSlots[cameraSlots.Length - 1] = state.DaySlot;
            vic._designatedCameraSlots = cameraSlots;

            Rh202GunnerCameraLifecycle lifecycle = vic.GetComponent<Rh202GunnerCameraLifecycle>();
            if (lifecycle == null)
                lifecycle = vic.gameObject.AddComponent<Rh202GunnerCameraLifecycle>();

            lifecycle.Bind(state.Vic, state.Weapon, state.DaySlot, state.GunnerFallbackSlot);

            CameraManager cameraManager = CameraManager.Instance;
            PlayerInput playerInput = PlayerInput.Instance;
            if (cameraManager != null && playerInput != null && playerInput.CurrentPlayerUnit == state.Vic)
            {
                WeaponSystem active = playerInput.CurrentPlayerCrewBrain?.WeaponsModule?.ActiveWeapon?.Weapon;
                if (active != null)
                    cameraManager.UpdateWeaponExclusions(active);
            }
        }

        sealed class Rh202GunnerCameraLifecycle : MonoBehaviour
        {
            Vehicle _vehicle;
            WeaponSystem _roofWeapon;
            CameraSlot _daySlot;
            CameraSlot _gunnerFallbackSlot;
            WeaponSystem _lastWeapon;

            public void Bind(Vehicle vehicle, WeaponSystem roofWeapon, CameraSlot daySlot, CameraSlot gunnerFallbackSlot)
            {
                _vehicle = vehicle;
                _roofWeapon = roofWeapon;
                _daySlot = daySlot;
                _gunnerFallbackSlot = gunnerFallbackSlot;
                _lastWeapon = GetGunnerActiveWeapon();
                StartCoroutine(CommitAfterFrames());
            }

            IEnumerator CommitAfterFrames()
            {
                yield return null;
                yield return null;
                TryCommitGraph();
            }

            void LateUpdate()
            {
                if (_vehicle == null || _roofWeapon == null)
                    return;

                if (!IsLocalGunnerOnVehicle())
                    return;

                WeaponSystem active = GetGunnerActiveWeapon();
                if (active == null || active == _lastWeapon)
                    return;

                WeaponSystem previous = _lastWeapon;
                _lastWeapon = active;
                SyncForWeapon(previous, active);
            }

            bool IsLocalGunnerOnVehicle()
            {
                PlayerInput input = PlayerInput.Instance;
                if (input == null || input.CurrentPlayerUnit != _vehicle)
                    return false;

                ICrewBrain gunner = _vehicle.InfoBroker._crewManager.GetCrewBrain(CrewPosition.Gunner);
                return gunner != null && input.CurrentPlayerCrewBrain == gunner;
            }

            WeaponSystem GetGunnerActiveWeapon()
            {
                ICrewBrain gunner = _vehicle?.InfoBroker?._crewManager?.GetCrewBrain(CrewPosition.Gunner);
                return gunner?.WeaponsModule?.ActiveWeapon?.Weapon;
            }

            void TryCommitGraph()
            {
                if (!IsLocalGunnerOnVehicle())
                    return;

                CameraManager cm = CameraManager.Instance;
                if (cm == null || _vehicle?.DesignatedCameraSlots == null)
                    return;

                cm.RescanCamSlots(_vehicle.DesignatedCameraSlots);
                _lastWeapon = GetGunnerActiveWeapon();

                if (_lastWeapon != null)
                    cm.UpdateWeaponExclusions(_lastWeapon);
            }

            void SyncForWeapon(WeaponSystem previousWeapon, WeaponSystem activeWeapon)
            {
                CameraManager cm = CameraManager.Instance;
                if (cm == null || activeWeapon == null)
                    return;

                cm.UpdateWeaponExclusions(activeWeapon);

                // Do not hijack the camera in third-person / exterior view (same intent as vanilla not forcing ADS).
                if (cm.ExteriorMode)
                    return;

                CameraSlot activeSlot = CameraSlot.ActiveInstance;
                if (activeSlot == null || previousWeapon == null)
                    return;

                // Match CameraManager.CheckForExclusiveOpticChangeNeeded: only swap if leaving a sight tied to the old weapon.
                if (!SlotExclusiveToWeapon(activeSlot, previousWeapon))
                    return;

                if (SlotAcceptsWeapon(activeSlot, activeWeapon))
                    return;

                CameraSlot target = FindSlotForWeapon(activeWeapon);
                if (target != null)
                    cm.ForceOpticChangeAfterWeaponChange(target);
                else if (activeSlot.SlotToUseOnDestroy != null)
                    cm.ForceOpticChangeAfterWeaponChange(activeSlot.SlotToUseOnDestroy);
            }

            static bool SlotExclusiveToWeapon(CameraSlot slot, WeaponSystem weapon)
            {
                if (slot == null || weapon == null)
                    return false;

                WeaponSystem[] exclusive = slot.ExclusiveWeapons;
                if (exclusive == null || exclusive.Length == 0)
                    return false;

                for (int i = 0; i < exclusive.Length; i++)
                {
                    if (exclusive[i] == weapon)
                        return true;
                }

                return false;
            }

            static bool SlotAcceptsWeapon(CameraSlot slot, WeaponSystem weapon)
            {
                if (slot == null || weapon == null)
                    return false;

                WeaponSystem[] exclusive = slot.ExclusiveWeapons;
                if (exclusive == null || exclusive.Length == 0)
                    return true;

                for (int i = 0; i < exclusive.Length; i++)
                {
                    if (exclusive[i] == weapon)
                        return true;
                }

                return false;
            }

            CameraSlot FindSlotForWeapon(WeaponSystem weapon)
            {
                if (weapon == _roofWeapon && _daySlot != null)
                    return _daySlot;

                if (_gunnerFallbackSlot != null && SlotAcceptsWeapon(_gunnerFallbackSlot, weapon))
                    return _gunnerFallbackSlot;

                if (_vehicle?.DesignatedCameraSlots == null)
                    return null;

                foreach (CameraSlot slot in _vehicle.DesignatedCameraSlots)
                {
                    if (SlotAcceptsWeapon(slot, weapon))
                        return slot;
                }

                return null;
            }
        }
        static void WireRh202DamageModel(Vehicle vic, WeaponSystem weapon, Transform rh202Gun, AimablePlatform traversePlatform, AimablePlatform elevationPlatform)
        {
            if (vic == null || weapon == null || rh202Gun == null)
                return;

            Transform breechRoot = rh202Gun.Find("20mm gun mantlet");
            Transform barrelRoot = rh202Gun.Find("gun model/barrel (1)");
            Transform mountRoot = null;
            Transform elevationPivot = rh202Gun.parent;
            if (elevationPivot != null)
            {
                Transform mountPivot = elevationPivot.parent;
                if (mountPivot != null)
                    mountRoot = mountPivot.Find("20mm gun base");
            }

            GHPC.Equipment.DestructibleComponent breechHit = EnsureRh202DestructibleOnTransform(breechRoot, Rh202BreechZoneName, Rh202BreechHitHealth);
            GHPC.Equipment.DestructibleComponent barrelHit = EnsureRh202DestructibleOnTransform(barrelRoot, Rh202BarrelZoneName, Rh202BarrelHitHealth);
            GHPC.Equipment.DestructibleComponent mountHit = EnsureRh202DestructibleOnTransform(mountRoot, Rh202TraverseZoneName, Rh202TraverseHitHealth);

            foreach (GHPC.Equipment.DestructibleComponent hit in new[] { breechHit, barrelHit, mountHit })
            {
                StripRh202ColliderProxy(hit?.componentCollision);
                AssignDestructibleUnit(hit, vic);
            }

            weapon.DamageName = Rh202WeaponDamageName;
            BindWeaponHitZones(weapon, barrelHit, breechHit);

            if (traversePlatform != null)
            {
                traversePlatform.Name = Rh202WeaponDamageName;
                GHPC.Equipment.DestructibleComponent[] traverseZones = mountHit != null ? new[] { mountHit } : Array.Empty<GHPC.Equipment.DestructibleComponent>();
                BindPlatformHitZones(traversePlatform, traverseZones);
            }

            if (elevationPlatform != null)
            {
                elevationPlatform.Name = Rh202WeaponDamageName;
                BindPlatformHitZones(elevationPlatform, Array.Empty<GHPC.Equipment.DestructibleComponent>());
                SubscribeRh202ElevationKnockoutOnTraverseDestroyed(elevationPlatform, mountHit);
            }

            Rh202DamageStatusReporter reporter = vic.GetComponent<Rh202DamageStatusReporter>();
            if (reporter == null)
                reporter = vic.gameObject.AddComponent<Rh202DamageStatusReporter>();
            reporter.Bind(weapon, traversePlatform, elevationPlatform);

            SubscribeRh202DamageHudRefresh(vic, breechHit, barrelHit, mountHit);
            RefreshUnitDamageStatuses(vic);

            //MelonLogger.Msg("[RH202 DM] Roof gun damage model wired.");
        }
        static void StripRh202ColliderProxy(Collider collider)
        {
            if (collider == null)
                return;

            foreach (GHPC.PhysicsHelpers.ProxyHitZone proxy in collider.GetComponents<GHPC.PhysicsHelpers.ProxyHitZone>())
            {
                if (proxy != null)
                    UnityEngine.Object.Destroy(proxy);
            }
        }

        static void AssignDestructibleUnit(GHPC.Equipment.DestructibleComponent hit, Vehicle vic)
        {
            if (hit == null || vic == null)
                return;

            hit.Unit = vic;
            hit.Hidden = false;
            hit._parent = null;
            hit._proxyHitZone = null;
            hit._proxyManager = null;
            hit._proxyHitZoneID = 0;
            if (hit._children == null)
                hit._children = new List<GHPC.Equipment.DestructibleComponent>();
            else
                hit._children.Clear();
        }

        static void SubscribeRh202ElevationKnockoutOnTraverseDestroyed(
            AimablePlatform elevation,
            GHPC.Equipment.DestructibleComponent traverse)
        {
            if (elevation == null || traverse == null)
                return;

            traverse.Destroyed += _ => elevation._destroyed = true;
            traverse.Repaired += _ =>
            {
                elevation._destroyed = false;
                elevation.DamageStatus = string.Empty;
            };
        }

        internal static bool UnitHasRh202(Unit unit)
        {
            if (unit == null)
                return false;

            if (unit.GetComponent<Rh202DamageStatusReporter>() != null)
                return true;

            foreach (WeaponSystem weapon in unit.GetComponentsInChildren<WeaponSystem>(true))
            {
                if (weapon != null && weapon.name == "RH202_MarderWeapon")
                    return true;
            }

            return false;
        }

        internal static void RefreshUnitDamageStatuses(Unit unit)
        {
            if (unit == null)
                return;

            unit.DamageStatuses = CollectUnitDamageStatuses(unit);
        }

        static IDamageStatus[] CollectUnitDamageStatuses(Unit unit)
        {
            HashSet<IDamageStatus> seen = new HashSet<IDamageStatus>();
            List<IDamageStatus> collected = new List<IDamageStatus>();

            foreach (IDamageStatus status in unit.GetComponentsInChildren<IDamageStatus>(true))
            {
                if (status == null || ReferenceEquals(status, unit))
                    continue;

                if (seen.Add(status))
                    collected.Add(status);
            }

            foreach (MonoBehaviour behaviour in unit.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || ReferenceEquals(behaviour, unit))
                    continue;

                if (behaviour is IDamageStatus status && seen.Add(status))
                    collected.Add(status);
            }

            Rh202DamageStatusReporter reporter = unit.GetComponent<Rh202DamageStatusReporter>();
            if (reporter != null)
            {
                for (int i = collected.Count - 1; i >= 0; i--)
                {
                    IDamageStatus entry = collected[i];
                    if (ReferenceEquals(entry, reporter))
                        continue;

                    if (ReferenceEquals(entry, reporter.Rh202Weapon) || ReferenceEquals(entry, reporter.Traverse) || ReferenceEquals(entry, reporter.Elevation))
                    {
                        collected.RemoveAt(i);
                    }
                }

                if (seen.Add(reporter))
                    collected.Add(reporter);
            }

            return collected.ToArray();
        }

        static void SubscribeRh202DamageHudRefresh(Vehicle vic, GHPC.Equipment.DestructibleComponent breech, GHPC.Equipment.DestructibleComponent barrel, GHPC.Equipment.DestructibleComponent traverse)
        {
            if (vic == null)
                return;

            if (breech != null)
                breech.Destroyed += _ => ScheduleRefreshUnitDamageStatuses(vic);
            if (barrel != null)
                barrel.Destroyed += _ => ScheduleRefreshUnitDamageStatuses(vic);
            if (traverse != null)
                traverse.Destroyed += _ => ScheduleRefreshUnitDamageStatuses(vic);
        }

        static void ScheduleRefreshUnitDamageStatuses(Unit unit)
        {
            if (unit == null)
                return;

            MelonCoroutines.Start(RefreshUnitDamageStatusesAfterHandlers(unit));
        }

        static IEnumerator RefreshUnitDamageStatusesAfterHandlers(Unit unit)
        {
            yield return null;
            RefreshUnitDamageStatuses(unit);
        }

        static void BindWeaponHitZones(WeaponSystem weapon, GHPC.Equipment.DestructibleComponent barrel, GHPC.Equipment.DestructibleComponent breech)
        {
            if (weapon == null)
                return;

            if (weapon.BarrelHitZone != null && weapon.BarrelHitZone != barrel)
                weapon.BarrelHitZone.Destroyed -= weapon.SingleBarrelDestroyed;

            if (weapon.BreechHitZone != null && weapon.BreechHitZone != breech)
                weapon.BreechHitZone.Destroyed -= weapon.BreechDestroyed;

            weapon.BarrelHitZone = barrel;
            weapon.BreechHitZone = breech;

            if (barrel != null)
                barrel.Destroyed += weapon.SingleBarrelDestroyed;

            if (breech != null)
                breech.Destroyed += weapon.BreechDestroyed;
        }

        static void BindPlatformHitZones(AimablePlatform platform, GHPC.Equipment.DestructibleComponent[] zones)
        {
            if (platform == null)
                return;

            GHPC.Equipment.DestructibleComponent[] previous = platform.HitZones;
            if (previous != null)
            {
                foreach (GHPC.Equipment.DestructibleComponent old in previous)
                {
                    if (old == null)
                        continue;
                    old.Destroyed -= platform.hitZoneDestroyed;
                    old.Repaired -= platform.hitZoneRepaired;
                }
            }

            platform.HitZones = zones ?? Array.Empty<GHPC.Equipment.DestructibleComponent>();

            foreach (GHPC.Equipment.DestructibleComponent hit in platform.HitZones)
            {
                if (hit == null)
                    continue;
                hit.Destroyed += platform.hitZoneDestroyed;
                hit.Repaired += platform.hitZoneRepaired;
            }
        }

        // Prefab colliders on the 20MM RIG (LEO2K_Assets) — static load; runtime <see cref="WireRh202DamageModel"/> re-binds owner + weapon.
        public static GHPC.Equipment.DestructibleComponent EnsureRh202Destructible(MeshCollider collider, string zoneName, float fullHealth)
        {
            if (collider == null)
                return null;

            GHPC.Equipment.DestructibleComponent hit = collider.GetComponent<GHPC.Equipment.DestructibleComponent>();
            if (hit == null)
                hit = collider.gameObject.AddComponent<GHPC.Equipment.DestructibleComponent>();

            hit._name = zoneName;
            hit._health = fullHealth;
            hit._fullHealth = fullHealth;
            hit._damageThreshold = 0f;
            hit._pressureTolerance = 0f;
            hit.componentCollision = collider;
            return hit;
        }

        public static GHPC.Equipment.DestructibleComponent EnsureRh202DestructibleOnTransform(Transform colliderRoot, string zoneName, float fullHealth)
        {
            if (colliderRoot == null)
                return null;

            MeshCollider collider = colliderRoot.GetComponent<MeshCollider>() ?? colliderRoot.GetComponentInChildren<MeshCollider>(true);

            return EnsureRh202Destructible(collider, zoneName, fullHealth);
        }
    }
}
