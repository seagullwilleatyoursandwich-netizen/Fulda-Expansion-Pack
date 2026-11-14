using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using GHPC;
using GHPC.AI;
using GHPC.Audio;
using GHPC.Crew;
using GHPC.Effects;
using GHPC.Equipment.AzimuthLocking;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weaponry;
using GHPC.Weapons;
using HarmonyLib;
using MelonLoader;
using ModUtil;
using NWH.VehiclePhysics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using VanillaAssetUtil.AssetUtil;
using static UnityEngine.GraphicsBuffer;

namespace Fulda1989
{
    public class LEO2A4_140AmmoVisualScaler : MonoBehaviour
    {
        private GHPC.Weapons.AmmoRack _rack;

        private void LateUpdate()
        {
            if (_rack == null)
                _rack = GetComponent<GHPC.Weapons.AmmoRack>();

            if (_rack?.VisualSlots == null)
                return;

            Vector3 ammoScale = Vector3.one * LEO2A4_140.ReadyRackAmmoVisualScale;
            foreach (Transform slot in _rack.VisualSlots)
            {
                if (slot == null)
                    continue;

                foreach (AmmoStoredVisual visual in slot.GetComponentsInChildren<AmmoStoredVisual>(true))
                {
                    if (visual != null)
                        visual.transform.localScale = ammoScale;
                }
            }
        }
    }

    [HarmonyPatch(typeof(WeaponAudio), "FinalStartLoop")]
    internal static class LEO2A4_140GunSound
    {
        private static bool Prefix(WeaponAudio __instance)
        {
            if (__instance == null || !LEO2A4_140.pitchedGunSounds.Contains(__instance.GetInstanceID()))
                return true;

            if (!__instance.SingleShotMode)
                return true;

            string[] paths = __instance.SingleShotEventPaths;
            if (paths == null || paths.Length == 0)
                return true;

            string path = paths[UnityEngine.Random.Range(0, paths.Length)];
            if (string.IsNullOrEmpty(path))
                return true;

            EventInstance shot = FMODUnity.RuntimeManager.CreateInstance(path);
            shot.setPitch(LEO2A4_140.GunFirePitch);
            shot.setVolume(LEO2A4_140.GunFireVolume);

            if (VehicleParameterTracker._instance != null)
                VehicleParameterTracker._instance.TrackUnit(shot, __instance._unit);

            Vector3 position = __instance.Transform != null ? __instance.Transform.position : __instance.transform.position;

            FmodGenericAudioManager.StartOneShot(shot, position, System.Array.Empty<System.ValueTuple<PARAMETER_ID, float>>());

            if (path == __instance.LoopEventPath)
                shot.setParameterByID(FmodParameters.End, 1f, false);

            return false;
        }
    }

    public class LEO2A4_140AutoloaderAudio : MonoBehaviour
    {
        private AmmoFeed feed;
        private EventInstance playing;

        public void Bind(AmmoFeed source)
        {
            Unsubscribe();
            feed = source;
            Subscribe();
        }
        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            Unsubscribe();
            StopPlaying();
        }

        private void Subscribe()
        {
            if (feed == null || !isActiveAndEnabled)
                return;

            feed.ReloadStageStarted -= OnReloadStageStarted;
            feed.LoadedNewClip -= OnRoundLoaded;

            feed.ReloadStageStarted += OnReloadStageStarted;
            feed.LoadedNewClip += OnRoundLoaded;
        }

        private void Unsubscribe()
        {
            if (feed == null)
                return;

            feed.ReloadStageStarted -= OnReloadStageStarted;
            feed.LoadedNewClip -= OnRoundLoaded;
        }

        private void OnRoundLoaded(AmmoType.AmmoClip clip) => StopPlaying();

        private void OnReloadStageStarted(int stageIndex)
        {
            if (stageIndex != 0)
                return;

            StopPlaying();

            try
            {
                playing = FmodGenericAudioManager.StartEvent(LEO2A4_140.AutoloaderEventPath, gameObject, null, false, System.Array.Empty<System.ValueTuple<PARAMETER_ID, float>>());
                playing.setPitch(LEO2A4_140.AutoloaderPitch);
                playing.setVolume(LEO2A4_140.AutoloaderVolume);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[LEO2A4(140)] Autoloader sound failed: {ex.Message}");
            }
        }

        private void StopPlaying()
        {
            if (playing.isValid())
                FmodGenericAudioManager.StopEvent(ref playing);
        }
    }

    public class LEO2A4_140SecondBlowout : MonoBehaviour
    {
        public CompartmentExit Source;
        public CompartmentExit Copy;

        private void LateUpdate()
        {
            if (Source == null || Copy == null || !Copy.Closed)
                enabled = false;
            else if (!Source.Closed)
            {
                Copy.Open(0.1f, false, false);
                enabled = false;
            }
        }
    }

    public class LEO2A4_140 : ModUtil.Module
    {
        static WeaponSystemCodexScriptable gun_l47;
        public static MelonPreferences_Entry<bool> leo2a4_140_patch;
        public static bool PatchEnabled => leo2a4_140_patch != null && leo2a4_140_patch.Value;
        public static Material ctechAarMaterial;

        // T-72 donor carousel noise, pitched down to sound heavy and unique compared to the already existing autoloaders cus it sounds AWESOME
        public const string AutoloaderEventPath = "event:/Effects/Reload/AZ_Autoloader";
        public const float AutoloaderPitch = 0.82f;
        public const float AutoloaderVolume = 2f;

        public const float GunFirePitch = 0.95f;
        public const float GunFireVolume = 1.1f;
        internal static readonly HashSet<int> pitchedGunSounds = new HashSet<int>();

        // Ready-rack ratio, first is AP (XM33), second is HEAT (XM16)
        public const int ReadyRackApShare = 6;
        public const int ReadyRackHeatShare = 4;
        public const float ReadyRackAmmoVisualScale = 140f / 120f;

        public static readonly Vector3 ReadyRackSlotLocalOffset = new Vector3(0.02f, -0.12f, 0.3f);
        public static readonly Vector3 ReadyRackSlotLocalEuler = new Vector3(180f, 0f, 0f);

        // Second bustle blowoff 
        public static readonly Vector3 BlowoffPanelLeftLocalPosition = new Vector3(-0.2175f, 0.502f, -1.93f);
        public static readonly Vector3 BlowoffPanelLeftLocalEuler = new Vector3(0f, 90f, 0f);
        public static readonly Vector3 BlowoffPanelLeftLocalScale = new Vector3(-4.3f, 4.2309f, 4f);
        public static readonly Vector3 BlowoutMarkerLeftLocalPosition = new Vector3(0.55f, 2.13f, -0.89f);

        // T-72M donor autoloader clone on the kws turret 
        public static readonly Vector3 KwsAutoloaderLocalPosition = new Vector3(0f, 0.2f, -0.45f);
        public static readonly Vector3 KwsAutoloaderLocalEuler = new Vector3(0f, 180f, 0f);
        public static readonly Vector3 KwsAutoloaderLocalScale = new Vector3(80f, 80f, 90f);

        public static void Config(MelonPreferences_Category category)
        {
            leo2a4_140_patch = category.CreateEntry("NPzK-140 (1991) Patch", true);
            leo2a4_140_patch.Comment = "Enables the NPzK-140 (Standalone - Found inside the vehicle customiser)";
        }
        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;
                GameObject vic_go = vic.gameObject;
                if (!vic.UniqueName.Contains("LEO2A4_140")) continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                    continue;

                vic_go.AddComponent<AlreadyConverted>();
                ApplyCtechArmour(vic);

                LoadoutManager loadout_manager = vic.GetComponent<LoadoutManager>();
                WeaponsManager weapons_manager = vic.GetComponent<WeaponsManager>();
                GHPC.Weapons.AmmoRack readyRack = null;
                if (loadout_manager?.RackLoadouts != null && loadout_manager.RackLoadouts.Length > 0)
                    readyRack = loadout_manager.RackLoadouts[0].Rack;

                // This is the teardown of the original 2A3 donor so everything is already clean for me to slap shit together (original concept build did not do this and was super messy and i hated it)

                GameObject kwsfull = GameObject.Instantiate(LEO2A4_Assets.full140);
                GameObject kwsgun = GameObject.Instantiate(LEO2A4_Assets.kwsGun);
                Transform skirtdown = vic_go.transform.Find("LEO2_mesh/rear skirt down");
                skirtdown.gameObject.SetActive(true);
                Transform altskirt = vic_go.transform.Find("LEO2_mesh/rear skirt unclipped");
                altskirt.gameObject.SetActive(false);
                Transform skirtup = vic_go.transform.Find("LEO2_mesh/rear skirt up");
                skirtup.gameObject.SetActive(false);
                Transform hull = vic.transform.Find("leo2_rig/hull");
                Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                Transform hullammoaar = hull_late_follow.transform.Find("LEO2_aar_hull/front ammo rack");
                GameObject.Destroy(hullammoaar.gameObject);
                Transform turret = vic.transform.Find("leo2_rig/hull/turret");
                Transform turret_late_follow = turret.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                Transform mantlet = turret.transform.Find("mantlet");
                Transform mantletnet = mantlet.transform.Find("net_gun");
                mantletnet.gameObject.SetActive(false);
                Transform mantlet_late_follow = mantlet.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                Transform barrelcollider = mantlet_late_follow.Find("mantlet_armour/gun barrel");
                if (barrelcollider == null)
                    barrelcollider = mantlet_late_follow.Find("gun barrel");
                Transform thermalsleeve = mantlet_late_follow.Find("mantlet_armour/thermal sleeve");
                if (thermalsleeve != null)
                    GameObject.Destroy(thermalsleeve.gameObject);
                Transform leo2hullarmour = hull_late_follow.Find("LEO2_armour_hull");
                Transform sideskirtaddons = kwsfull.transform.Find("hull/side skirts");
                sideskirtaddons.SetParent(leo2hullarmour, false);
                sideskirtaddons.localPosition = new Vector3(0.06f, 1.39f, 0.86f);
                sideskirtaddons.localEulerAngles = new Vector3(0f, 90f, 0f);
                sideskirtaddons.localScale = new Vector3(1.02f, 1f, 1f);
                Transform skirtLeft = sideskirtaddons.Find("2a4 side skirt left");
                Transform skirtRight = sideskirtaddons.Find("2a4 side skirt right");
                Transform newbarrel = kwsgun.transform;
                newbarrel.SetParent(mantlet_late_follow, false);
                newbarrel.localPosition = new Vector3(58.08f, 3.46f, -0.1f);
                newbarrel.localScale = new Vector3(0.9f, 0.9f, 1f);
                Transform basebarrel = mantlet.transform.Find("muzzle");
                basebarrel.localScale = new Vector3(0f, 0f, 0f);
                basebarrel.localPosition = new Vector3(0f, 0f, 1f);
                Transform muzzlestart = mantlet.transform.Find("---MAIN GUN SCRIPTS---/Gun Recoil Script/muzzle start");
                muzzlestart.localPosition = new Vector3(0f, 1.928f, 3f);
                Transform muzzleend = mantlet.transform.Find("---MAIN GUN SCRIPTS---/Gun Recoil Script/muzzle end");
                muzzleend.localPosition = new Vector3(0f, 1.928f, 3f);
                Transform muzzlerigend = mantlet.transform.Find("---MAIN GUN SCRIPTS---/120mm Gun Rh-120 L44/muzzle end");
                muzzlerigend.localPosition = new Vector3(0f, 0f, 1.2f);
                Transform mantletctech = mantlet_late_follow.transform.Find("mantlet armour/mantlet");
                GameObject.Destroy(mantletctech.gameObject);
                Transform interiorturret = turret.transform.Find("turret parent");
                interiorturret.gameObject.SetActive(false);
                Transform interiorhull = vic.transform.Find("LEO2_int");
                interiorhull.gameObject.SetActive(false);
                Transform Loader = turret_late_follow.transform.Find("Loader");
                GameObject.Destroy(Loader.gameObject);


                Transform readyrack = turret_late_follow.transform.Find("ready rack");
                readyrack.localPosition = new Vector3(-0.25f, 0f, -0.25f);

                if (readyrack != null)
                {
                    foreach (Transform child in readyrack.GetComponentsInChildren<Transform>(true))
                    {
                        if (child == readyrack)
                            continue;

                        bool vanillaSlot = child.name.IndexOf("case_dm23", System.StringComparison.OrdinalIgnoreCase) >= 0;
                        bool legacyPlaceholder = child.GetComponent<AmmoVisualPlaceholder>() != null
                            && !child.name.StartsWith("rack_", System.StringComparison.OrdinalIgnoreCase);

                        if (vanillaSlot || legacyPlaceholder)
                            GameObject.Destroy(child.gameObject);
                    }
                }

                // bustle ready rack conversion
                Transform bustlerack = kwsfull.transform.Find("turret/bustle rack");
                bustlerack.SetParent(turret_late_follow, false);
                bustlerack.localPosition = new Vector3(0.075f, -0.25f, -1.15f);
                bustlerack.localEulerAngles = new Vector3(0f, 180f, 0f);

                List<Transform> markers = new List<Transform>();
                for (int i = 0; i < bustlerack.childCount; i++)
                {
                    Transform marker = bustlerack.GetChild(i);
                    if (IsBustleRackSlotMarker(marker))
                        markers.Add(marker);
                }

                markers.Sort(CompareBustleRackSlotOrder);

                if (loadout_manager?.RackLoadouts != null)
                {
                foreach (LoadoutManager.RackLoadout rackLoadout in loadout_manager.RackLoadouts)
                {
                    if (rackLoadout?.Rack != null)
                        Util.EmptyRack(rackLoadout.Rack);
                }
                }

                if (readyRack != null)
                {
                Transform[] oldSlots = readyRack.VisualSlots.ToArray();

                if (markers.Count == 0)
                {
                    MelonLogger.Error("[LEO2A4(140) RACKS] No bustle rack markers found.");
                }
                else
                {
                    Transform slotParent = readyRack.transform;
                    List<Transform> slots = new List<Transform>(markers.Count);

                    for (int i = 0; i < markers.Count; i++)
                    {
                        Transform marker = markers[i];
                        marker.SetParent(slotParent, true);
                        marker.gameObject.SetActive(true);
                        marker.localPosition += ReadyRackSlotLocalOffset;
                        marker.localEulerAngles += ReadyRackSlotLocalEuler;

                        AmmoVisualPlaceholder placeholder = marker.GetComponent<AmmoVisualPlaceholder>();
                        if (placeholder == null)
                            placeholder = marker.gameObject.AddComponent<AmmoVisualPlaceholder>();

                        placeholder.RackIndex = i;
                        slots.Add(marker);
                    }

                    GameObject.Destroy(bustlerack.gameObject);

                    foreach (Transform oldSlot in oldSlots)
                    {
                        if (oldSlot != null)
                            GameObject.Destroy(oldSlot.gameObject);
                    }

                    Transform firemarker = readyrack.transform.Find("fire marker");
                    if (firemarker != null)
                        firemarker.localPosition = new Vector3(0f, 2.043f, -0.76f);
                    readyRack.VisualSlots.Clear();
                    readyRack.VisualSlots.AddRange(slots);
                    readyRack.ClipCapacity = slots.Count;
                    readyRack.ManualSlotSetup = true;
                    readyRack.RegenerateSlotIndices();

                    if (readyRack.transform.GetComponent<LEO2A4_140AmmoVisualScaler>() == null)
                        readyRack.gameObject.AddComponent<LEO2A4_140AmmoVisualScaler>();

                    MelonLogger.Msg($"[LEO2A4(140) RACKS] Registered {slots.Count} ready-rack slots from bustle markers.");
                }
                }
                else
                {
                    MelonLogger.Error("[LEO2A4(140) RACKS] Ready rack missing; skipped bustle slot setup.");
                }

                Transform commonturret = turret.transform.Find("turret common parts");
                Material leo2Co = TakeLeo2CoFromCommonTurret(commonturret);
                commonturret.gameObject.SetActive(false);
                Transform kwsturret = kwsfull.transform.Find("turret");
                Transform internalkwswall = kwsturret.Find("internal walls");
                kwsturret.SetParent(turret, false);
                internalkwswall.SetParent(turret_late_follow, true);
                internalkwswall.localPosition = new Vector3(0f, 0f, 0f);
                internalkwswall.localEulerAngles = new Vector3(0f, 180f, 0f);
                kwsturret.localPosition = new Vector3(0.03f, -0.205f, 0.042f);
                kwsturret.localEulerAngles = new Vector3(0f, 90f, 0f);
                ApplyLeo2CoToKwsTurret(kwsturret, leo2Co, skirtLeft, skirtRight);

                foreach (Transform skirt in new[] { skirtLeft, skirtRight })
                {
                    if (skirt == null)
                        continue;

                    AarVisual onGameplaySkirt = skirt.GetComponent<AarVisual>();
                    if (onGameplaySkirt != null)
                        UnityEngine.Object.Destroy(onGameplaySkirt);

                    skirt.gameObject.layer = 0;

                    MeshRenderer gameplayRenderer = skirt.GetComponent<MeshRenderer>();
                    if (gameplayRenderer != null)
                        gameplayRenderer.enabled = true;
                }

                Transform leftSkirtAar = skirtLeft != null ? skirtLeft.Find("2a4 side skirt left aar") : null;
                Transform rightSkirtAar = skirtRight != null ? skirtRight.Find("2a4 side skirt right aar") : null;
                foreach (Transform aarTransform in new[] { leftSkirtAar, rightSkirtAar })
                {
                    if (aarTransform == null)
                        continue;

                    AarVisual skirtAarVisual = aarTransform.GetComponent<AarVisual>() ?? aarTransform.gameObject.AddComponent<AarVisual>();

                    skirtAarVisual._renderers.Clear();
                    MeshRenderer aarRenderer = aarTransform.GetComponent<MeshRenderer>();
                    if (aarRenderer != null)
                        skirtAarVisual._renderers.Add(aarRenderer);

                    skirtAarVisual.OriginalMaterials.Clear();
                    skirtAarVisual.InitialiseRenderers();

                    skirtAarVisual.RenderMode = AarVisual.AarVisualRenderMode.HighlightedOnly;
                    skirtAarVisual.SwitchMaterials = false;
                    skirtAarVisual.UpdateVisibility();
                }

                Transform storagerack = hull_late_follow.transform.Find("hull spare rack");
                if (storagerack != null)
                    GameObject.Destroy(storagerack.gameObject);

                Transform originalwalls = turret_late_follow.transform.Find("LEO2_armour_turret/turret armour/inside walls");
                if (originalwalls != null)
                    GameObject.Destroy(originalwalls.gameObject);

                Transform hullnet = vic.transform.Find("LEO2_camo");
                GameObject.Destroy(hullnet.gameObject);
                Transform blowoffpanel = vic.transform.Find("leo2_rig/hull/turret/blowoff panel/Blowoff scripts (bustle)");
                CompartmentFirewall firewall = blowoffpanel != null ? blowoffpanel.GetComponent<CompartmentFirewall>() : null;
                if (firewall != null && internalkwswall != null)
                {
                    List<UniformArmor> shields = firewall.ArmoredShields1 != null ? new List<UniformArmor>(firewall.ArmoredShields1) : new List<UniformArmor>();

                    foreach (MeshCollider collider in internalkwswall.GetComponentsInChildren<MeshCollider>(true))
                    {
                        UniformArmor armor = collider.GetComponent<UniformArmor>();
                        if (armor != null && !shields.Contains(armor))
                            shields.Add(armor);
                    }

                    firewall.ArmoredShields1 = shields.ToArray();
                }

                Transform ammoDoor = vic.transform.Find("leo2_rig/hull/turret/---TURRET SCRIPTS---/Ammo Door animation");
                ammoDoor.localPosition = new Vector3(0f, 1.85f, -0.335f);
                ammoDoor.localEulerAngles = new Vector3(0f, 0f, 180f);
                ammoDoor.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                

                AnimatedPart doorPart = ammoDoor.GetComponent<AnimatedPart>();
                if (doorPart != null && doorPart.StartTransform != null)
                {
                    doorPart.StartTransform.localPosition = new Vector3(0f, 1.85f, -0.335f);
                    doorPart.StartTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
                    doorPart.StartTransform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                }
                else
                {
                    MelonLogger.Warning("[LEO2A4(140)] Could not find the ammo door start transform.");
                }

                if (doorPart != null && doorPart.EndTransform != null)
                {
                    doorPart.EndTransform.localPosition = new Vector3(0f, 2.18f, -0.335f);
                    doorPart.EndTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
                    doorPart.EndTransform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                }
                Transform aar = turret_late_follow.Find("LEO2_aar_turret");

                if (aar != null)
                {
                    string[] removePieces = {"TC seat001", "loader's control box", "loader's intercom box", "loader's periscope", "gun stub bin",};

                    foreach (string piece in removePieces)
                    {
                        Transform item = aar.Find(piece);
                        if (item != null)
                            GameObject.Destroy(item.gameObject);
                    }

                    Transform periComputer = aar.Find("PERI computer");
                    if (periComputer != null)
                        periComputer.localPosition = new Vector3(-0.75f, 2f, 0.6f);

                    Transform radio = aar.Find("radio");
                    if (radio != null)
                    {
                        radio.localPosition = new Vector3(-0.73f, 1.5f, 0.58f);
                        radio.localEulerAngles = new Vector3(270f, 90f, 0f);
                    }
                }

                // CONVERT 140MM MAIN GUN

                if (weapons_manager == null || weapons_manager.Weapons == null || weapons_manager.Weapons.Length == 0)
                {
                    MelonLogger.Error("[LEO2A4(140) GUN] WeaponsManager or main weapon missing.");
                }
                else
                {
                WeaponSystemInfo main_gun_info = weapons_manager.Weapons[0];
                WeaponSystem main_gun = main_gun_info.Weapon;

                main_gun_info.Name = "Rh-460 L/47";
                main_gun.Impulse = 94000;
                main_gun.CodexEntry = gun_l47;
                if (main_gun.WeaponSound?.SingleShotEventPaths != null && main_gun.WeaponSound.SingleShotEventPaths.Length > 0)
                main_gun.WeaponSound.SingleShotEventPaths[0] = "event:/Weapons/canon_125mm-2A46";
                WireKwsGunBarrel(kwsgun, mantlet_late_follow, main_gun, barrelcollider);

                Transform muzzleFlashes = main_gun.MuzzleEffects[1].transform;
                muzzleFlashes.GetChild(1).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);
                muzzleFlashes.GetChild(2).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);
                muzzleFlashes.GetChild(3).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);
                muzzleFlashes.GetChild(4).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);
                muzzleFlashes.GetChild(5).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);


                // CONFIGURE 140MM AMMUNITION
                if (loadout_manager == null || loadout_manager.LoadedAmmoList == null)
                {
                    MelonLogger.Error("[LEO2A4(140) AMMO] LoadoutManager or LoadedAmmoList is missing.");
                }
                else
                {
                    AmmoClipCodexScriptable apClip;
                    AmmoClipCodexScriptable heatClip;

                    if (Ammo_140mm_Bundeswehr.ap == null || Ammo_140mm_Bundeswehr.heat == null || !Ammo_140mm_Bundeswehr.ap.TryGetValue("XM33", out apClip) || !Ammo_140mm_Bundeswehr.heat.TryGetValue("XM16", out heatClip))
                    {
                        MelonLogger.Error("[LEO2A4(140) AMMO] Could not find XM33 APFSDS or XM16 HEAT.");
                    }
                    else
                    {
                        apClip.CompatibleWeaponSystems = new[] { gun_l47 };
                        heatClip.CompatibleWeaponSystems = new[] { gun_l47 };

                        loadout_manager.LoadedAmmoList.AmmoClips = new[] { apClip, heatClip };
                        loadout_manager._totalAmmoTypes = 2;
                        loadout_manager.RegisterAllBallistics();

                        MelonLogger.Msg("[LEO2A4(140) AMMO] Registered XM33 APFSDS and XM16 HEAT.");
                    }
                }

                if (main_gun.WeaponSound != null)
                    pitchedGunSounds.Add(main_gun.WeaponSound.GetInstanceID());

                AmmoFeed feed = main_gun.GetComponent<AmmoFeed>();
                if (feed != null)
                {
                    feed.HumanLoaded = false;
                    feed._loaderBrain = null;
                    feed._loaderIncapacitated = false;
                }

                RemoveLoaderFromCrew(vic, feed);

                ReloadAzimuthLock reloadLock = main_gun.GetComponent<ReloadAzimuthLock>();
                if (reloadLock != null)
                {
                    reloadLock.ReloadStageIndexEnd = 3;
                    if (reloadLock.Lock != null)
                        reloadLock.Lock.PitchLockValue = 4f;
                }

                if (feed != null)
                {
                    AmmoFeed.ReloadStage[] reloadStages = feed.ClipReloadStages;
                    if (reloadStages == null || reloadStages.Length == 0)
                    {
                        feed._totalReloadTime = 4.5f;
                    }
                    else
                    {
                        float current = 0f;
                        foreach (AmmoFeed.ReloadStage stage in reloadStages)
                        {
                            if (stage != null)
                                current += stage.Duration;
                        }

                        if (current <= 0.01f)
                        {
                            feed._totalReloadTime = 4.5f;
                        }
                        else
                        {
                            float scale = 4.5f / current;
                            foreach (AmmoFeed.ReloadStage stage in reloadStages)
                            {
                                if (stage == null)
                                    continue;

                                stage.Duration *= scale;
                                stage.LiveDuration = stage.Duration;
                            }

                            feed._totalReloadTime = 0f;
                        }
                    }

                    feed.SlowReloadMultiplier = 8f;

                    // bustle rack population 
                    if (readyRack != null && loadout_manager?.LoadedAmmoList?.AmmoClips != null && loadout_manager.LoadedAmmoList.AmmoClips.Length >= 2 && loadout_manager.RackLoadouts != null && loadout_manager.RackLoadouts.Length >= 2)
                    {
                        readyRack = loadout_manager.RackLoadouts[0].Rack;
                        Util.EmptyRack(readyRack);

                        LoadoutManager.RackLoadout readyLoadout = loadout_manager.RackLoadouts[0];
                        LoadoutManager.RackLoadout hullLoadout = loadout_manager.RackLoadouts[1];
                        GHPC.Weapons.AmmoRack hullRack = hullLoadout.Rack;

                        foreach (LoadoutManager.RackLoadout rackLoadout in loadout_manager.RackLoadouts)
                        {
                            if (rackLoadout == null)
                                continue;

                            rackLoadout.FixedChoices = new LoadoutManager.RackLoadoutFixedChoice[0];
                            rackLoadout.OverrideInitialClips = null;
                            rackLoadout.ForbiddenAmmoIndices = new int[0];
                        }

                        if (hullRack != null)
                        {
                            if (hullRack.VisualSlots != null)
                            {
                                foreach (Transform slot in hullRack.VisualSlots.ToArray())
                                {
                                    if (slot != null)
                                        slot.gameObject.SetActive(false);
                                }

                                hullRack.VisualSlots.Clear();
                            }

                            hullRack.ManualSlotSetup = true;
                            hullRack.ClipCapacity = 0;
                            hullRack.RegenerateSlotIndices();

                            if (hullRack.Flammables?.Items != null)
                            {
                                hullRack.Flammables.Items.Clear();
                                hullRack.Flammables.RefreshExplosivesStatus();
                            }
                        }

                        loadout_manager.RackLoadouts = new[] { readyLoadout };

                        int readyCapacity = readyRack.VisualSlots.Count;
                        readyRack.ClipCapacity = readyCapacity;

                        // 6:4 ratio for bustle rack
                        int share = ReadyRackApShare + ReadyRackHeatShare;
                        int readyAp = share > 0 ? Mathf.Clamp(Mathf.RoundToInt(readyCapacity * ReadyRackApShare / (float)share), 0, readyCapacity) : 0;

                        int readyHeat = readyCapacity - readyAp;

                        loadout_manager.TotalAmmoCounts = new[] { readyAp, readyHeat };
                        loadout_manager._totalAmmoCount = readyCapacity;

                        readyLoadout.AmmoCounts = new[] { readyAp, readyHeat };
                        readyLoadout.Interleave = false;

                        LoadoutManager.RackLoadoutFixedChoice[] choices = new LoadoutManager.RackLoadoutFixedChoice[readyCapacity];

                        for (int i = 0; i < readyCapacity; i++)
                        {
                            choices[i] = new LoadoutManager.RackLoadoutFixedChoice
                            { RackSlotIndex = i, AmmoClipIndex = i < readyAp ? 0 : 1 };
                        }

                        readyLoadout.FixedChoices = choices;

                        readyLoadout.Rack = readyRack;

                        try
                        {
                            loadout_manager.SpawnCurrentLoadout(true);

                            if (main_gun.Feed != null)
                            {
                                main_gun.Feed.AmmoTypeInBreech = null;
                                main_gun.Feed.Start();
                            }

                            loadout_manager.RegisterAllBallistics();

                            MelonLogger.Msg($"[LEO2A4(140) AMMO] Spawned ready rack: {readyAp} XM33 APFSDS, {readyHeat} XM16 HEAT, {readyCapacity} total.");
                        }
                        catch (System.Exception ex)
                        {
                            MelonLogger.Error($"[LEO2A4(140) AMMO] Failed to spawn ready-rack ammunition: {ex}");
                        }
                }
                else
                {
                        MelonLogger.Error("[LEO2A4(140) AMMO] Cannot populate ready rack; ammo clips or rack loadouts are missing.");
                    }

                    if (main_gun.WeaponSound != null)
                        main_gun.WeaponSound.ReloadEventPath = string.Empty;

                    AmmoFeed.ReloadStage[] soundStages = feed.ClipReloadStages;
                    int breechStage = LastAudibleStage(soundStages);
                    if (soundStages != null)
                    {
                        for (int i = 0; i < soundStages.Length; i++)
                        {
                            if (i == breechStage || soundStages[i] == null)
                                continue;

                            soundStages[i].StageAudio = null;
                            soundStages[i].StageClips = null;
                            soundStages[i].StageAudioEvent = string.Empty;
                            soundStages[i].StageEventReference = default(EventReference);
                        }
                    }

                    LEO2A4_140AutoloaderAudio autoloaderAudio = main_gun.GetComponent<LEO2A4_140AutoloaderAudio>();
                    if (autoloaderAudio == null)
                        autoloaderAudio = main_gun.gameObject.AddComponent<LEO2A4_140AutoloaderAudio>();

                    autoloaderAudio.Bind(feed);
                    WireKwsAutoloaderArm(vic_go, feed);

                    int LastAudibleStage(AmmoFeed.ReloadStage[] stages)
                    {
                        if (stages == null)
                            return -1;

                        for (int i = stages.Length - 1; i >= 0; i--)
                        {
                            AmmoFeed.ReloadStage stage = stages[i];
                            if (stage == null)
                                continue;

                            bool hasEvent = !string.IsNullOrEmpty(stage.StageAudioEvent) || !stage.StageEventReference.IsNull;
                            bool hasClip = stage.StageAudio != null && stage.StageClips != null && stage.StageClips.Length > 0;
                            if (hasEvent || hasClip)
                                return i;
                        }

                        return -1;
                    }
                }
                }
                VehicleController this_vic_controller = vic_go.GetComponent<VehicleController>();
                NwhChassis chassis = vic_go.GetComponent<NwhChassis>();
                chassis._originalEnginePower = 1100f;
                Transform turretScripts = vic_go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "---TURRET SCRIPTS---");

                if (turretScripts != null)
                {
                    AimablePlatform turretPlatform = turretScripts.GetComponent<AimablePlatform>();

                    if (turretPlatform != null)
                    {
                        turretPlatform.SpeedPowered = 30f;
                        turretPlatform.SpeedUnpowered = 8f;

                    }
                    else
                    {
                        MelonLogger.Warning("[NPzK-140] ---TURRET SCRIPTS--- has no AimablePlatform!");
                    }
                }
                else
                {
                    MelonLogger.Warning("[NPzK-140] Could not find ---TURRET SCRIPTS---!");
                }

                UnitAI ai = vic.GetComponentInChildren<UnitAI>();
                AIAccuracyModifiers accuracy = ai.AccuracyModifiers;

                if (vic.FriendlyName == "NPzK-140 (1991)")
                {
                    accuracy.Range.Min = 1f;
                    accuracy.Range.Max = 1f;
                    accuracy.Range.Target = 1f;
                    accuracy.Range.IncreaseAccuracyPerShot = false;

                    accuracy.Velocity.Min = 1f;
                    accuracy.Velocity.Max = 1.02f;
                    accuracy.Velocity.Target = 1f;
                    accuracy.Velocity.IncreaseAccuracyPerShot = true;

                    accuracy.Angle.MaxDistance = 3800f;
                    accuracy.Angle.MaxRadius = 1.75f;
                    accuracy.Angle.MinRadius = 1f;
                    accuracy.Angle.IncreaseAccuracyPerShot = false;
                }

                ScorchUtil.RegisterScorchRoot(vic, kwsturret, commonturret);
                WireSecondBlowoutEffect(vic);
                CloneBlowoffPanel(vic);
                ScorchUtil.PruneInvalidScorchRenderers(vic);
                yield return null;
            }
        }
        static void WireKwsGunBarrel(GameObject kwsGun, Transform mantletLateFollow, WeaponSystem mainGun, Transform stockBarrel = null)
        {
            if (kwsGun == null || mantletLateFollow == null || mainGun == null)
                return;

            Transform kwsColliderRoot = kwsGun.transform.Find("barrel collider");
            if (kwsColliderRoot == null)
            {
                MelonLogger.Warning("[LEO2A4(140) GUN] KWS gun has no 'barrel collider' transform.");
                return;
            }

            Collider barrelCollider = kwsColliderRoot.GetComponent<Collider>();
            if (barrelCollider == null)
                barrelCollider = kwsColliderRoot.GetComponentInChildren<Collider>(true);
            if (barrelCollider == null)
            {
                MelonLogger.Warning("[LEO2A4(140) GUN] KWS 'barrel collider' has no collider.");
                return;
            }

            Transform vanillaBarrel = stockBarrel;
            if (vanillaBarrel == null)
                vanillaBarrel = mantletLateFollow.Find("mantlet_armour/gun barrel");
            if (vanillaBarrel == null)
                vanillaBarrel = mantletLateFollow.Find("gun barrel");

            GHPC.Equipment.DestructibleComponent donorDestructible = vanillaBarrel != null ? vanillaBarrel.GetComponent<GHPC.Equipment.DestructibleComponent>() : null;
            UniformArmor donorUniform = vanillaBarrel != null ? vanillaBarrel.GetComponent<UniformArmor>() : null;
            VariableArmor donorVariable = vanillaBarrel != null ? vanillaBarrel.GetComponent<VariableArmor>() : null;

            GameObject hitbox = barrelCollider.gameObject;
            UniformArmor armor = hitbox.GetComponent<UniformArmor>();
            VariableArmor variableArmor = hitbox.GetComponent<VariableArmor>();

            if (donorUniform != null && armor != null)
            {
                armor._name = donorUniform._name;
                armor._armorType = donorUniform._armorType;
                armor.PrimarySabotRha = donorUniform.PrimarySabotRha;
                armor.PrimaryHeatRha = donorUniform.PrimaryHeatRha;
                armor.SecondarySabotRha = donorUniform.SecondarySabotRha;
                armor.SecondaryHeatRha = donorUniform.SecondaryHeatRha;
                armor.AngleMatters = donorUniform.AngleMatters;
                armor._normalizesHits = donorUniform._normalizesHits;
                armor._canShatterLongRods = donorUniform._canShatterLongRods;
                armor._canRicochet = donorUniform._canRicochet;
                armor._surfaceMaterial = donorUniform._surfaceMaterial;
            }
            else if (donorVariable != null && variableArmor != null)
            {
                variableArmor._name = donorVariable._name;
                variableArmor._armorType = donorVariable._armorType;
                variableArmor.AverageRha = donorVariable.AverageRha;
            }

            GHPC.Equipment.DestructibleComponent barrelHit = hitbox.GetComponent<GHPC.Equipment.DestructibleComponent>();
            if (barrelHit == null)
                barrelHit = hitbox.AddComponent<GHPC.Equipment.DestructibleComponent>();

            if (donorDestructible != null)
            {
                barrelHit._name = donorDestructible._name;
                barrelHit._health = donorDestructible._health;
                barrelHit._fullHealth = donorDestructible._fullHealth;
                barrelHit._damageThreshold = donorDestructible._damageThreshold;
                barrelHit._pressureTolerance = donorDestructible._pressureTolerance;
                barrelHit._parent = donorDestructible._parent;
                barrelHit._children = new List<GHPC.Equipment.DestructibleComponent>();
                barrelHit._proxyHitZone = null;
                barrelHit._proxyManager = null;
                barrelHit._proxyHitZoneID = 0;
            }
            else if (barrelHit._fullHealth <= 0f)
            {
                barrelHit._name = "gun barrel";
                barrelHit._health = 100f;
                barrelHit._fullHealth = 100f;
                barrelHit._damageThreshold = 0f;
                barrelHit._pressureTolerance = 0f;
                barrelHit._parent = null;
                if (barrelHit._children == null)
                    barrelHit._children = new List<GHPC.Equipment.DestructibleComponent>();
            }

            barrelHit.componentCollision = barrelCollider;
            mainGun.BarrelHitZone = barrelHit;

            if (vanillaBarrel != null)
                GameObject.Destroy(vanillaBarrel.gameObject);

            MelonLogger.Msg("[LEO2A4(140) GUN] BarrelHitZone on KWS barrel collider; stock gun barrel removed.");
        }

        static void WireKwsAutoloaderArm(GameObject vicGo, AmmoFeed feed)
        {
            if (vicGo == null || feed == null)
                return;

            Vehicle t72 = LEO2A4_Assets.t72;
            GHPC.Equipment.DestructibleComponent donorLoader = null;
            if (t72 != null)
            {
                foreach (AmmoFeed donorFeed in t72.GetComponentsInChildren<AmmoFeed>(true))
                {
                    if (donorFeed != null && donorFeed.Autoloader != null)
                    {
                        donorLoader = donorFeed.Autoloader;
                        break;
                    }
                }
            }

            Transform loaderTurret = vicGo.transform.Find("leo2_rig/hull/turret");
            if (t72 == null || donorLoader == null || loaderTurret == null)
            {
                MelonLogger.Warning("[LEO2A4(140)] Could not take the T-72 autoloader (donor missing or turret not found).");
                return;
            }

            bool donorActive = donorLoader.gameObject.activeSelf;
            donorLoader.gameObject.SetActive(false);
            GameObject arm = GameObject.Instantiate(donorLoader.gameObject, loaderTurret, false);
            donorLoader.gameObject.SetActive(donorActive);

            Transform donorArm = null;
            foreach (Transform candidate in t72.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == "Autoloader Arm")
                {
                    donorArm = candidate;
                    break;
                }
            }

            if (donorArm != null && !donorArm.IsChildOf(donorLoader.transform))
            {
                bool armActive = donorArm.gameObject.activeSelf;
                donorArm.gameObject.SetActive(false);
                GameObject.Instantiate(donorArm.gameObject, arm.transform, false);
                donorArm.gameObject.SetActive(armActive);
            }

            arm.name = "KWS autoloader";
            GHPC.Equipment.DestructibleComponent loader = arm.GetComponent<GHPC.Equipment.DestructibleComponent>();
            if (loader == null)
            {
                MelonLogger.Warning("[LEO2A4(140)] Cloned T-72 autoloader has no DestructibleComponent.");
                GameObject.Destroy(arm);
                return;
            }

            loader._parent = null;
            loader._proxyHitZone = null;
            loader._proxyManager = null;
            if (loader._children != null)
                loader._children.RemoveAll(child => child == null || !child.transform.IsChildOf(arm.transform));

            arm.transform.localPosition = KwsAutoloaderLocalPosition;
            arm.transform.localEulerAngles = KwsAutoloaderLocalEuler;
            arm.transform.localScale = KwsAutoloaderLocalScale;
            arm.SetActive(true);

            feed.Autoloader = loader;
            loader.Destroyed += feed.AutoloaderDestroyed;

            MelonLogger.Msg("[LEO2A4(140)] T-72 autoloader arm parented to turret and bound to AmmoFeed.");
        }

        static void RemoveLoaderFromCrew(Vehicle vic, AmmoFeed feed)
        {
            CrewManager crew = vic.GetComponentInChildren<CrewManager>(true);
            if (crew == null || crew.CrewMembers == null)
                return;

            CrewManager.CrewMember loader = crew.GetCrewMember(CrewPosition.Loader);
            if (loader != null)
            {
                if (feed != null && loader.Human != null)
                    loader.Human.DamageStatusChanged -= feed.LoaderHealthChanged;

                DisableLoaderCrewObject(loader.Human as Component, crew, loader);
                DisableLoaderCrewObject(loader.BodyPartRegistry, crew, loader);
                DisableLoaderCrewObject(loader.AutoBoneRig, crew, loader);
                DisableLoaderCrewObject(loader.AnimationTriggers, crew, loader);
            }

            crew.CrewMembers = crew.CrewMembers.Where(member => member == null || member.Position != CrewPosition.Loader).ToArray();
        }

        static void DisableLoaderCrewObject(Component component, CrewManager crew, CrewManager.CrewMember loader)
        {
            if (component == null)
                return;

            GameObject crewObject = component.gameObject;
            if (crewObject.transform.parent == null || CrewObjectHostsOtherMember(crewObject, crew, loader))
                return;

            crewObject.SetActive(false);
        }

        static bool CrewObjectHostsOtherMember(GameObject crewObject, CrewManager crew, CrewManager.CrewMember loader)
        {
            if (crew?.CrewMembers == null)
                return false;

            foreach (CrewManager.CrewMember member in crew.CrewMembers)
            {
                if (member == null || member == loader || member.Position == CrewPosition.Loader)
                    continue;

                if ((member.Human is Component human && human.gameObject == crewObject) || (member.BodyPartRegistry != null && member.BodyPartRegistry.gameObject == crewObject) || (member.AutoBoneRig != null && member.AutoBoneRig.gameObject == crewObject) || (member.AnimationTriggers != null && member.AnimationTriggers.gameObject == crewObject))
                    return true;
            }

            return false;
        }
        private static bool IsBustleRackSlotMarker(Transform transform)
        {
            return transform != null
                && transform.name.StartsWith("rack_", System.StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseBustleRackSlotNumber(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.StartsWith("rack_", System.StringComparison.OrdinalIgnoreCase))
                return int.MaxValue;

            int start = 5;
            int end = start;
            while (end < name.Length && char.IsDigit(name[end]))
                end++;

            if (end <= start)
                return int.MaxValue;

            int number;
            return int.TryParse(name.Substring(start, end - start), out number) ? number : int.MaxValue;
        }

        private static int CompareBustleRackSlotOrder(Transform a, Transform b)
        {
            int cmp = ParseBustleRackSlotNumber(a.name).CompareTo(ParseBustleRackSlotNumber(b.name));
            if (cmp != 0)
                return cmp;

            return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
        }

        private static Material TakeLeo2CoFromCommonTurret(Transform commonturret)
        {
            Material fallback = null;

            foreach (Renderer renderer in commonturret.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterials == null)
                    continue;

                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    Texture albedo = material.HasProperty("_Albedo") ? material.GetTexture("_Albedo") : null;
                    if (albedo == null && material.HasProperty("_MainTex"))
                        albedo = material.GetTexture("_MainTex");
                    if (albedo == null)
                        continue;

                    if (fallback == null)
                        fallback = material;

                    string textureName = albedo.name.Replace(" (Instance)", "");
                    if (textureName.IndexOf("LEO2_co", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return material;
                }
            }

            return fallback;
        }

        private static void ApplyLeo2CoToKwsTurret(Transform kwsturret, Material turretMaterial, Transform skirtLeft, Transform skirtRight)
        {
            if (turretMaterial == null)
            {
                MelonLogger.Warning("[LEO2A4(140)] Could not find LEO2_co on donor turret common parts.");
                return;
            }

            foreach (Transform part in kwsturret.GetComponentsInChildren<Transform>(true))
            {
                if (part.name != "turret common parts")
                    continue;

                Renderer renderer = part.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = turretMaterial;
            }

            foreach (Transform skirt in new[] { skirtLeft, skirtRight })
            {
                if (skirt == null)
                    continue;

                MeshRenderer gameplayRenderer = skirt.GetComponent<MeshRenderer>();
                if (gameplayRenderer != null)
                    gameplayRenderer.sharedMaterial = turretMaterial;
            }
        }

        private static void ApplyCtechArmour(Vehicle vic)
        {
            Armour.Init();

            if (Armour.ctech == null)
            {
                MelonLogger.Error("[LEO2A4 ARMOUR] C-Tech composite is not initialized.");
                return;
            }

            Transform turretRoot = Find2A4ArmourRoot(vic, "LEO2A4 TURRET COLLIDERS");
            Transform gunRoot = Find2A4ArmourRoot(vic, "LEO2A4 GUN COLLIDERS");
            Transform hullRoot = Find2A4ArmourRoot(vic, "LEO2A4 HULL COLLIDERS");

            if (turretRoot == null)
                MelonLogger.Warning("[LEO2A4 ARMOUR] Could not find LEO2A4 TURRET COLLIDERS.");

            if (gunRoot == null)
                MelonLogger.Warning("[LEO2A4 ARMOUR] Could not find LEO2A4 GUN COLLIDERS.");

            if (hullRoot == null)
                MelonLogger.Warning("[LEO2A4 ARMOUR] Could not find LEO2A4 HULL COLLIDERS.");

            Material eberAarMaterial = ctechAarMaterial;

            if (eberAarMaterial == null)
                MelonLogger.Warning("[LEO2A4 ARMOUR] C-Tech AAR material unavailable; armour conversion will continue without changing AAR materials.");

            int updated = 0;

            // C-tech compsoite values

            // TURRET FRONT ARRAYS
            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/cheek array left A", eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/cheek array right A", eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/cheek array right B", eberAarMaterial, ref updated);

            // TURRET SIDE ARRAYS
            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/array left", eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/array right", eberAarMaterial, ref updated);

            // HULL FRONT ARRAYS
            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/upper glacis array", eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/lower glacis array", eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/sponson array left", eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/sponson array right", eberAarMaterial, ref updated);

            // HULL FRONT LEFT/RIGHT COMPOSITE ARRAYS
            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/front left array", eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/front right array", eberAarMaterial, ref updated);
        }

        private static Transform Find2A4ArmourRoot(Vehicle vic, string rootName)
        {
            // Check the vehicle hierarchy first.
            foreach (Transform candidate in vic.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == rootName)
                    return candidate;
            }

            // Also search LateFollow hierarchies associated with this vehicle.
            foreach (LateFollow follow in Resources.FindObjectsOfTypeAll<LateFollow>())
            {
                if (follow == null || !follow.gameObject.scene.IsValid())
                    continue;

                Component parentUnit = follow.ParentUnit as Component;
                Transform originalParent = follow.OriginalParent;

                bool belongsToVehicle = parentUnit == vic || (parentUnit != null && parentUnit.gameObject == vic.gameObject) || originalParent == vic.transform || (originalParent != null && originalParent.IsChildOf(vic.transform));

                if (!belongsToVehicle)
                    continue;

                foreach (Transform candidate in follow.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == rootName)
                        return candidate;
                }
            }

            return null;
        }

        private static List<Transform> FindAllExactArmourPaths(Transform root, string relativePath)
        {
            List<Transform> matches = new List<Transform>();

            if (root == null)
                return matches;

            string[] segments = relativePath.Split(new[] { '/' });

            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                Transform cursor = candidate;
                bool matchesPath = true;

                for (int i = segments.Length - 1; i >= 0; i--)
                {
                    if (cursor == null || cursor.name != segments[i])
                    {
                        matchesPath = false;
                        break;
                    }

                    cursor = cursor.parent;
                }

                if (matchesPath && cursor == root)
                    matches.Add(candidate);
            }

            return matches;
        }

        private static void ApplyCtechAtPath(Transform root, string relativePath, Material eberAarMaterial, ref int updated)
        {
            if (root == null)
                return;

            List<Transform> targets = FindAllExactArmourPaths(root, relativePath);

            if (targets.Count == 0)
            {
                MelonLogger.Warning($"[LEO2A4 ARMOUR] No object matched '{root.name}/{relativePath}'.");
                return;
            }

            foreach (Transform target in targets)
            {
                VariableArmor armor = target.GetComponent<VariableArmor>();

                if (armor == null)
                {
                    MelonLogger.Warning($"[LEO2A4 ARMOUR] '{target.name}' has no VariableArmor component.");
                    continue;
                }

                string previousType = armor._armorType != null ? armor._armorType.name : "NULL";

                // Replace the existing armour configuration; do not add another component.
                armor._armorType = Armour.ctech;
                armor._name = "C-Tech composite";

                AarVisual aar = target.GetComponent<AarVisual>();

                if (aar != null && eberAarMaterial != null)
                {
                    aar.AarMaterial = eberAarMaterial;
                    aar.UpdateVisibility();
                }
                else if (aar == null)
                {
                    MelonLogger.Warning($"[LEO2A4 ARMOUR] '{target.name}' has no AarVisual component.");
                }

                updated++;

            }
        }

        public override void LoadStaticAssets()
        {
            ctechAarMaterial = LEO2A4_Assets.aarctech2a4;

            Transform kwsBarrelPrefab = LEO2A4_Assets.kwsGun != null ? LEO2A4_Assets.kwsGun.transform.Find("barrel collider") : null;
            ArmorCodexScriptable gunSteel = null;
            foreach (ArmorCodexScriptable codex in Resources.FindObjectsOfTypeAll<ArmorCodexScriptable>())
            {
                if (codex != null && codex.name == "US Gun Steel")
                {
                     gunSteel = codex;
                     break;
                }
            }

            if (kwsBarrelPrefab != null)
            {
            foreach (MeshCollider collider in kwsBarrelPrefab.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();
                armor.tag = "Penetrable";
                armor.gameObject.layer = 8;
                armor._canShatterLongRods = true;
                armor._normalizesHits = true;
                armor.AngleMatters = true;
                armor._name = "gun barrel";
                armor.PrimaryHeatRha = 20f;
                armor.PrimarySabotRha = 20f;
                if (gunSteel != null)
                    armor._armorType = gunSteel;

                GHPC.Equipment.DestructibleComponent barrelDestructible = collider.gameObject.AddComponent<GHPC.Equipment.DestructibleComponent>();
                barrelDestructible._name = "gun barrel";
                barrelDestructible._health = 100f;
                barrelDestructible._fullHealth = 100f;
                barrelDestructible._damageThreshold = 0f;
                barrelDestructible._pressureTolerance = 0f;
                barrelDestructible._parent = null;
                barrelDestructible._children = new List<GHPC.Equipment.DestructibleComponent>();
                barrelDestructible._proxyHitZone = null;
                barrelDestructible._proxyManager = null;
                barrelDestructible._proxyHitZoneID = 0;
                barrelDestructible.componentCollision = collider;
            }
            }
            else
            {
                MelonLogger.Warning("[LEO2A4(140) GUN] KWS 'barrel collider' prefab missing; skipped barrel armour/destructible setup.");
            }

            Transform kwsarmourmesh = LEO2A4_Assets.full140.transform.Find("turret/internal walls");
            AarVisual internalarmour = kwsarmourmesh.gameObject.AddComponent<AarVisual>();
            internalarmour._renderers.Clear();
            internalarmour._renderers.AddRange(kwsarmourmesh.GetComponentsInChildren<MeshRenderer>(true));
            internalarmour.AarMaterial = null;
            internalarmour.SwitchMaterials = false;
            internalarmour.InitialiseRenderers();
            internalarmour.UpdateVisibility();
            foreach (MeshCollider collider in kwsarmourmesh.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                armor.tag = "Penetrable";
                armor.gameObject.layer = 8;
                armor._canShatterLongRods = true;
                armor._normalizesHits = true;
                armor.AngleMatters = true;
                armor._name = "internal walls";
                armor.PrimaryHeatRha = 10f;
                armor.PrimarySabotRha = 10f;
            }

            Transform sideskirtaddons = LEO2A4_Assets.full140.transform.Find("hull/side skirts");
            Transform skirtLeft = sideskirtaddons.Find("2a4 side skirt left");
            Transform skirtRight = sideskirtaddons.Find("2a4 side skirt right");
            Armour.Init();
            if (skirtLeft.GetComponent<VariableArmor>() == null)
            {
                VariableArmor leftArmour = skirtLeft.gameObject.AddComponent<VariableArmor>();
                skirtLeft.tag = "Penetrable";
                leftArmour._name = "side skirt left";
                leftArmour._armorType = Armour.ctech;
                leftArmour.AverageRha = 80f;
            }

            if (skirtRight.GetComponent<VariableArmor>() == null)
            {
                VariableArmor rightArmour = skirtRight.gameObject.AddComponent<VariableArmor>();
                skirtRight.tag = "Penetrable";
                rightArmour._name = "side skirt right";
                rightArmour._armorType = Armour.ctech;
                rightArmour.AverageRha = 80f;
            }
        }

        private static List<Transform> Named(Vehicle vic, string name)
            {
            List<Transform> found = new List<Transform>();
            HashSet<int> seen = new HashSet<int>();
            void Scan(Transform root)
            {
            if (root == null)
            return;
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
            if (candidate.name != name)
            continue;
            if (seen.Add(candidate.gameObject.GetInstanceID()))
            found.Add(candidate);
            }
            }
            Scan(vic.transform);
            foreach (LateFollowTarget target in vic.GetComponentsInChildren<LateFollowTarget>(true))
            {
            if (target._lateFollowers == null)
            continue;
            foreach (LateFollow follower in target._lateFollowers)
            {
            if (follower != null)
            Scan(follower.transform);
            }
            }
            foreach (LateFollow follow in Resources.FindObjectsOfTypeAll<LateFollow>())
            {
            if (FollowsVehicle(follow, vic))
            Scan(follow.transform);
            }
            return found;
            }
        private static void CloneBlowoffPanel(Vehicle vic)
            {
            Transform turret = vic.transform.Find("leo2_rig/hull/turret");
            List<Transform> panels = new List<Transform>();
            if (turret != null)
            {
            foreach (Transform candidate in turret.GetComponentsInChildren<Transform>(true))
            {
            if (candidate.name == "blowoff panel")
            panels.Add(candidate);
            }
            }
            if (panels.Count == 0)
            panels = Named(vic, "blowoff panel");
            if (panels.Count == 0)
            {
            MelonLogger.Warning("[LEO2A4(140)] Could not find 'blowoff panel'.");
            return;
            }
            panels.RemoveAll(panel =>
            {
            Transform cursor = panel.parent;
            while (cursor != null)
            {
            if (cursor.name == "blowoff panel")
            return true;
            cursor = cursor.parent;
            }
            return false;
            });
            foreach (Transform panel in panels)
            MirrorBlowoffPanel(vic, panel);
            }
        private static void MirrorBlowoffPanel(Vehicle vic, Transform panel)
            {
            const string copyName = "blowoff panel left";
            if (panel.parent != null)
            {
            for (int i = 0; i < panel.parent.childCount; i++)
            {
            if (panel.parent.GetChild(i).name == copyName)
            return;
            }
            }
            Transform copy = UnityEngine.Object.Instantiate(panel.gameObject, panel.parent, false).transform;
            copy.name = copyName;
            List<GameObject> nestedCopies = new List<GameObject>();
            foreach (Transform nested in copy.GetComponentsInChildren<Transform>(true))
            {
            if (nested != copy && nested.name == copyName)
            nestedCopies.Add(nested.gameObject);
            }
            foreach (GameObject nested in nestedCopies)
            UnityEngine.Object.Destroy(nested);
            copy.localPosition = BlowoffPanelLeftLocalPosition;
            copy.localEulerAngles = BlowoffPanelLeftLocalEuler;
            copy.localScale = BlowoffPanelLeftLocalScale;
            DetachableParent[] originalDetachables = panel.GetComponentsInChildren<DetachableParent>(true);
            DetachableParent[] copiedDetachables = copy.GetComponentsInChildren<DetachableParent>(true);
            AnimatedPart[] originalParts = panel.GetComponentsInChildren<AnimatedPart>(true);
            AnimatedPart[] copiedParts = copy.GetComponentsInChildren<AnimatedPart>(true);
            int paired = Mathf.Min(originalDetachables.Length, copiedDetachables.Length);
            for (int i = 0; i < paired; i++)
            {
            if (originalDetachables[i] == null || copiedDetachables[i] == null)
                    continue;
            RetargetCopiedDetachable(originalDetachables[i], copiedDetachables[i], panel, copy);
            }
            foreach (Compartment compartment in Resources.FindObjectsOfTypeAll<Compartment>())
            {
            if (!BelongsToVehicle(compartment, vic))
            continue;
            if (compartment.Detachables != null)
            {
            List<DetachableParent> added = new List<DetachableParent>();
            foreach (DetachableParent detachable in compartment.Detachables)
            {
            int index = System.Array.IndexOf(originalDetachables, detachable);
            if (index < 0 || index >= copiedDetachables.Length || copiedDetachables[index] == null)
            continue;
            if (compartment.Detachables.Contains(copiedDetachables[index]) || added.Contains(copiedDetachables[index]))
            continue;
            added.Add(copiedDetachables[index]);
            }
            foreach (DetachableParent detachable in added)
            compartment.Detachables.Add(detachable);
            if (added.Count > 0)
            {
            float[] previous = compartment._detachablesCumulativeOverpressure;
            float[] resized = new float[compartment.Detachables.Count];
            if (previous != null)
            System.Array.Copy(previous, resized, System.Math.Min(previous.Length, resized.Length));
            compartment._detachablesCumulativeOverpressure = resized;
            }
            }
            if (compartment.Exits == null)
            continue;
            foreach (CompartmentExit exit in compartment.Exits)
            {
            if (exit == null || exit.AnimatedParts == null)
            continue;
            List<AnimatedPart> addedParts = new List<AnimatedPart>();
            foreach (AnimatedPart part in exit.AnimatedParts)
            {
            int index = System.Array.IndexOf(originalParts, part);
            if (index < 0 || index >= copiedParts.Length || copiedParts[index] == null)
            continue;
            if (System.Array.IndexOf(exit.AnimatedParts, copiedParts[index]) >= 0 || addedParts.Contains(copiedParts[index]))
            continue;
            addedParts.Add(copiedParts[index]);
            }
            if (addedParts.Count == 0)
            continue;
            AnimatedPart[] combined = new AnimatedPart[exit.AnimatedParts.Length + addedParts.Count];
            exit.AnimatedParts.CopyTo(combined, 0);
            addedParts.CopyTo(combined, exit.AnimatedParts.Length);
            exit.AnimatedParts = combined;
            }
            }
            }
        private static SlewableTurret FindTurret(Vehicle vic)
        {
            return vic.GetComponentInChildren<SlewableTurret>(true);
        }

        private static void WireSecondBlowoutEffect(Vehicle vic)
            {
            List<Transform> markers = new List<Transform>();
            CollectBlowoutMarkers(vic.transform, markers);
            foreach (LateFollow follow in Resources.FindObjectsOfTypeAll<LateFollow>())
            {
            if (FollowsVehicle(follow, vic))
            CollectBlowoutMarkers(follow.transform, markers);
            }
            if (markers.Count == 0)
            {
            MelonLogger.Warning("[LEO2A4(140)] Could not find a blowout panel marker.");
            return;
            }
            foreach (Transform marker in markers)
            DuplicateBlowoutEffect(vic, marker);
            }
        private static void CollectBlowoutMarkers(Transform root, List<Transform> markers)
            {
            if (root == null)
            return;
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
            if (candidate.name.EndsWith(" left"))
            continue;
            if (candidate.name.ToLower().IndexOf("blowout panel marker") < 0)
            continue;
            if (!markers.Contains(candidate))
            markers.Add(candidate);
            }
            }
        private static void DuplicateBlowoutEffect(Vehicle vic, Transform marker)
            {
            const string copyName = "blowout panel marker left";
            if (marker.parent != null)
            {
            for (int i = 0; i < marker.parent.childCount; i++)
            {
            if (marker.parent.GetChild(i).name == copyName)
            return;
            }
            }
            Transform copy = UnityEngine.Object.Instantiate(marker.gameObject, marker.parent, false).transform;
            copy.name = copyName;
            copy.localPosition = BlowoutMarkerLeftLocalPosition;
            copy.localRotation = marker.localRotation;
            copy.localScale = marker.localScale;
            ParticleSystem[] originalSystems = marker.GetComponentsInChildren<ParticleSystem>(true);
            ParticleSystem[] copiedSystems = copy.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in copiedSystems)
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            TankParticleEffect[] originalEffects = marker.GetComponentsInChildren<TankParticleEffect>(true);
            TankParticleEffect[] copiedEffects = copy.GetComponentsInChildren<TankParticleEffect>(true);
            int addedParticles = 0;
            SlewableTurret turret = FindTurret(vic);
            if (turret != null)
            {
            addedParticles += AssignCopiedEffects(turret.AmmoRackEffects, originalSystems, copiedSystems, value => turret.AmmoRackEffects = value);
            addedParticles += AssignCopiedEffects(turret.AmmoFireFx, originalEffects, copiedEffects, value => turret.AmmoFireFx = value);
            }
            DrivableChassis chassis = vic.GetComponent<DrivableChassis>();
            if (chassis == null)
            chassis = vic.GetComponentInChildren<DrivableChassis>(true);
            if (chassis != null)
            {
            addedParticles += AssignCopiedEffects(chassis.AmmoFire, originalSystems, copiedSystems, value => chassis.AmmoFire = value);
            addedParticles += AssignCopiedEffects(chassis.AmmoFireFx, originalEffects, copiedEffects, value => chassis.AmmoFireFx = value);
            addedParticles += AssignCopiedEffects(chassis.AmmoFireNoTurretFx, originalEffects, copiedEffects, value => chassis.AmmoFireNoTurretFx = value);
            }
            int addedExits = 0;
            foreach (Compartment compartment in Resources.FindObjectsOfTypeAll<Compartment>())
            {
            if (!BelongsToVehicle(compartment, vic) || compartment.Exits == null)
            continue;
            List<CompartmentExit> sources = new List<CompartmentExit>();
            foreach (CompartmentExit exit in compartment.Exits)
            {
            if (exit == null || exit.PositionMarker == null)
            continue;
            if (exit.PositionMarker == marker || exit.PositionMarker.IsChildOf(marker))
            sources.Add(exit);
            }
            foreach (CompartmentExit exit in sources)
            {
            Transform mappedMarker = CorrespondingTransform(marker, copy, exit.PositionMarker);
            if (mappedMarker == null)
            continue;
            CompartmentExit clone = new CompartmentExit
            {
            Name = exit.Name + " left",
            Radius = exit.Radius,
            FlameHeightThreshold = exit.FlameHeightThreshold,
            SourceType = exit.SourceType,
            PositionMarker = mappedMarker,
            AnimatedParts = System.Array.Empty<AnimatedPart>(),
            DetachTrigger = null,
            KillFireWhenDetached = exit.KillFireWhenDetached,
            Closed = exit.Closed,
            PressureToOpenBar = exit.PressureToOpenBar,
            FireParticles = RemapParticles(exit.FireParticles, marker, copy),
            FireBurstParticles = RemapParticles(exit.FireBurstParticles, marker, copy),
            SmokeParticles = RemapParticles(exit.SmokeParticles, marker, copy),
            };
            clone.DoAwake(compartment);
            compartment.Exits.Add(clone);
            addedExits++;
            LEO2A4_140SecondBlowout link = copy.gameObject.AddComponent<LEO2A4_140SecondBlowout>();
            link.Source = exit;
            link.Copy = clone;
            }
            }
            if (addedExits == 0 && addedParticles == 0)
            MelonLogger.Warning($"[LEO2A4(140)] '{marker.name}' was duplicated, but nothing was playing an effect from it.");
            else
            MelonLogger.Msg($"[LEO2A4(140)] Wired a second blowout effect from '{marker.name}' ({addedExits} exit(s), {addedParticles} particle slot(s)).");
            }
        private static int AssignCopiedEffects<T>(T[] current, T[] originals, T[] copies, System.Action<T[]> assign) where T : UnityEngine.Object
            {if (current == null || originals == null || copies == null)
            return 0;
            List<T> extras = new List<T>();
            int count = Mathf.Min(originals.Length, copies.Length);
            for (int i = 0; i < count; i++)
            {T original = originals[i]; T clone = copies[i]; if (original == null || clone == null || System.Array.IndexOf(current, original) < 0 || System.Array.IndexOf(current, clone) >= 0) continue; extras.Add(clone);}
            if (extras.Count == 0)
            return 0;
            T[] next = new T[current.Length + extras.Count];
            current.CopyTo(next, 0);
            extras.CopyTo(next, current.Length);
            assign(next);
            return extras.Count;
            }
        private static IFireParticleSystem RemapParticles(IFireParticleSystem source, Transform marker, Transform copy)
            {
            Component component = source as Component;
            if (component == null)
            return null;
            Transform mapped = CorrespondingTransform(marker, copy, component.transform);
            if (mapped == null)
            return null;
            return mapped.GetComponent<IFireParticleSystem>();
            }
        private static void RetargetCopiedDetachable(DetachableParent source, DetachableParent clone, Transform originalRoot, Transform copyRoot)
            {
            clone.ColliderObjects = CopyColliderObjects(source.ColliderObjects, originalRoot, copyRoot);
            clone.ObjectsToDeactivate = RemapObjectsInside(source.ObjectsToDeactivate, originalRoot, copyRoot);
            clone.TransformsToLeaveBehind = RemapTransformsInside(source.TransformsToLeaveBehind, originalRoot, copyRoot);
            clone.AarHideItemsParents = RemapTransformsInside(source.AarHideItemsParents, originalRoot, copyRoot);
            clone.OtherParents = RemapTransformsInside(source.OtherParents, originalRoot, copyRoot);
            Transform mapped = CorrespondingTransform(originalRoot, copyRoot, source.Transform);
            if (mapped != null)
            clone.Transform = mapped;
            mapped = CorrespondingTransform(originalRoot, copyRoot, source.CenterOfMass);
            if (mapped != null)
            clone.CenterOfMass = mapped;
            mapped = CorrespondingTransform(originalRoot, copyRoot, source.ForceReceivingLocation);
            if (mapped != null)
            clone.ForceReceivingLocation = mapped;
            mapped = CorrespondingTransform(originalRoot, copyRoot, source.UpReferenceTransform);
            if (mapped != null)
            clone.UpReferenceTransform = mapped;
            EnsureSolidCollider(clone);
            }
        private static GameObject[] CopyColliderObjects(GameObject[] sources, Transform originalRoot, Transform copyRoot)
            {
            if (sources == null)
            return null;
            GameObject[] result = new GameObject[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
            GameObject source = sources[i];
            if (source == null)
            continue;
            Transform mapped = CorrespondingTransform(originalRoot, copyRoot, source.transform);
            if (mapped != null)
            {
            result[i] = mapped.gameObject;
            continue;
            }
            Transform duplicate = UnityEngine.Object.Instantiate(source).transform;
            duplicate.name = source.name;
            duplicate.SetParent(copyRoot, true);
            duplicate.position = copyRoot.TransformPoint(originalRoot.InverseTransformPoint(source.transform.position));
            duplicate.rotation = copyRoot.rotation * Quaternion.Inverse(originalRoot.rotation) * source.transform.rotation;
            result[i] = duplicate.gameObject;
            }
            return result;
            }
        private static GameObject[] RemapObjectsInside(GameObject[] sources, Transform originalRoot, Transform copyRoot)
            {
            if (sources == null)
            return null;
            List<GameObject> result = new List<GameObject>(sources.Length);
            foreach (GameObject source in sources)
            {
            if (source == null)
            continue;
            Transform mapped = CorrespondingTransform(originalRoot, copyRoot, source.transform);
            if (mapped != null)
            result.Add(mapped.gameObject);
            }
            return result.ToArray();
            }
        private static Transform[] RemapTransformsInside(Transform[] sources, Transform originalRoot, Transform copyRoot)
            {
            if (sources == null)
            return null;
            List<Transform> result = new List<Transform>(sources.Length);
            foreach (Transform source in sources)
            {
            if (source == null)
            continue;
            Transform mapped = CorrespondingTransform(originalRoot, copyRoot, source);
            if (mapped != null)
            result.Add(mapped);
            }
            return result.ToArray();
            }
        private static void EnsureSolidCollider(DetachableParent detachable)
            {
            Transform root = detachable.Transform != null ? detachable.Transform : detachable.transform;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
            MeshCollider mesh = collider as MeshCollider;
            if (mesh != null)
            {
            Vector3 lossy = mesh.transform.lossyScale;
            if (lossy.x < 0f || lossy.y < 0f || lossy.z < 0f)
            continue;
            }
            collider.enabled = true;
                return;
            }
            GameObject holder = new GameObject("blowoff collider");
            holder.transform.SetParent(root, false);
            holder.SetActive(false);
            BoxCollider box = holder.AddComponent<BoxCollider>();
            bool started = false;
            Bounds local = new Bounds();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
            Bounds world = renderer.bounds;
            for (int i = 0; i < 8; i++)
            {
            Vector3 corner = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
            Vector3 point = root.InverseTransformPoint(world.center + Vector3.Scale(world.extents, corner));
            if (!started)
            {
            local = new Bounds(point, Vector3.zero);
            started = true;
            }
            else
            {
            local.Encapsulate(point);
            }
            }
            }
            if (started)
            {
            box.center = local.center;
            box.size = local.size;
            }
            GameObject[] current = detachable.ColliderObjects;
            if (current == null || current.Length == 0)
            {
            detachable.ColliderObjects = new[] { holder };
            return;
            }
            foreach (GameObject existing in current)
            {
            if (existing == holder)
            return;
            }
            GameObject[] expanded = new GameObject[current.Length + 1];
            current.CopyTo(expanded, 0);
            expanded[current.Length] = holder;
            detachable.ColliderObjects = expanded;
            }
        private static Transform CorrespondingTransform(Transform marker, Transform copy, Transform source)
            {
            if (source == marker)
            return copy;
            if (source == null || marker == null || !source.IsChildOf(marker))
            return null;
            List<int> indices = new List<int>();
            Transform cursor = source;
            while (cursor != marker)
            {
            indices.Add(cursor.GetSiblingIndex());
            cursor = cursor.parent;
            }
            indices.Reverse();
            Transform found = copy;
            foreach (int index in indices)
            {
            if (index < 0 || index >= found.childCount)
            return null;
            found = found.GetChild(index);
            }
            return found;
            }
        private static bool BelongsToVehicle(Component component, Vehicle vic)
            {
            if (component == null || vic == null || !component.gameObject.scene.IsValid())
            return false;
            if (component.transform == vic.transform || component.transform.IsChildOf(vic.transform))
            return true;
            return FollowsVehicle(component.GetComponentInParent<LateFollow>(true), vic);
            }
        private static bool FollowsVehicle(LateFollow follow, Vehicle vic)
            {
            if (follow == null || vic == null || !follow.gameObject.scene.IsValid())
            return false;
            Component parentUnit = follow.ParentUnit as Component;
            Transform originalParent = follow.OriginalParent;
            return parentUnit == vic || (parentUnit != null && parentUnit.gameObject == vic.gameObject) || originalParent == vic.transform || (originalParent != null && originalParent.IsChildOf(vic.transform));
            }

        public static void Init()
        {
            if (!PatchEnabled)
                return;

            if (gun_l47 == null)
            {
                gun_l47 = ScriptableObject.CreateInstance<WeaponSystemCodexScriptable>();
                gun_l47.name = "gun_l47";
                gun_l47.CaliberMm = 140;
                gun_l47.FriendlyName = "Rh-460 L/47";
                gun_l47.Type = WeaponSystemCodexScriptable.WeaponType.LargeCannon;
            }

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}
