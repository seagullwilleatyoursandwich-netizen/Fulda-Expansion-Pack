using GHPC;
using GHPC.Equipment;
using GHPC.Equipment.Lamps;
using GHPC.Equipment.Optics;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Module = ModUtil.Module;

namespace Fulda1989
{
    public class T64BV : Module
    {
        static MelonPreferences_Entry<bool> t64bv_patch;
        static MelonPreferences_Entry<string> t64bv_ammo_type_1985;
        static MelonPreferences_Entry<string> t64bv_heat_type_1985;
        static MelonPreferences_Entry<bool> t64bv_random_ammo_1985;
        static MelonPreferences_Entry<List<string>> t64bv_random_ammo_pool_1985;

        static MelonPreferences_Entry<string> t64bv_ammo_type_1988;
        static MelonPreferences_Entry<string> t64bv_heat_type_1988;
        static MelonPreferences_Entry<bool> t64bv_random_ammo_1988;
        static MelonPreferences_Entry<List<string>> t64bv_random_ammo_pool_1988;
        static MelonPreferences_Entry<bool> zoom_snapper;

        const string AgavaPreset = "low";

        public static bool PatchEnabled => t64bv_patch != null && t64bv_patch.Value;

        struct BvVariant
        {
            public string FriendlyName;
            public bool HasAgava;
        }

        static bool TryGetVariant(string uniqueName, out BvVariant variant)
        {
            variant = default;

            if (string.IsNullOrEmpty(uniqueName))
                return false;

            if (string.Equals(uniqueName, FuldaStandaloneRegistrations.T64BvUnit, StringComparison.OrdinalIgnoreCase))
            {
                variant = new BvVariant { FriendlyName = "T-64BV obr.1985", HasAgava = false };
                return true;
            }

            if (string.Equals(uniqueName, FuldaStandaloneRegistrations.T64Bv1988Unit, StringComparison.OrdinalIgnoreCase))
            {
                variant = new BvVariant { FriendlyName = "T-64BV obr.1988", HasAgava = true };
                return true;
            }

            if (string.Equals(uniqueName, FuldaStandaloneRegistrations.T64Bv1Unit, StringComparison.OrdinalIgnoreCase))
            {
                variant = new BvVariant { FriendlyName = "T-64BV1 obr.1985", HasAgava = false };
                return true;
            }

            if (string.Equals(uniqueName, FuldaStandaloneRegistrations.T64Bv1_1988Unit, StringComparison.OrdinalIgnoreCase))
            {
                variant = new BvVariant { FriendlyName = "T-64BV1 obr.1988", HasAgava = true };
                return true;
            }

            return false;
        }

        public static bool IsStandaloneSpawn(Vehicle vic)
        {
            return vic != null && TryGetVariant(vic.UniqueName, out _);
        }

        static string ResolveApRound(BvVariant variant)
        {
            bool obr1988 = variant.HasAgava;

            MelonPreferences_Entry<bool> randomToggle = obr1988 ? t64bv_random_ammo_1988 : t64bv_random_ammo_1985;
            MelonPreferences_Entry<List<string>> randomPool = obr1988 ? t64bv_random_ammo_pool_1988 : t64bv_random_ammo_pool_1985;
            MelonPreferences_Entry<string> fixedAp = obr1988 ? t64bv_ammo_type_1988 : t64bv_ammo_type_1985;

            if (randomToggle != null && randomToggle.Value
                && randomPool != null && randomPool.Value != null && randomPool.Value.Count > 0)
            {
                int pick = UnityEngine.Random.Range(0, randomPool.Value.Count);
                return randomPool.Value[pick];
            }

            return fixedAp != null ? fixedAp.Value : "3BM32";
        }

        static string ResolveHeatRound(BvVariant variant)
        {
            MelonPreferences_Entry<string> heat = variant.HasAgava ? t64bv_heat_type_1988 : t64bv_heat_type_1985;
            return heat != null ? heat.Value : "3BK18M";
        }

        const string UfpTextolitePath = "T64_armour_hull/textolite_1";

        static Transform FindUfpTextoliteTransform(Vehicle vic)
        {
            LateFollowTarget lateFollow = vic.GetComponent<LateFollowTarget>();
            if (lateFollow != null && lateFollow._lateFollowers != null)
            {
                foreach (LateFollow follower in lateFollow._lateFollowers)
                {
                    if (follower == null)
                        continue;

                    Transform hit = follower.transform.Find(UfpTextolitePath);
                    if (hit != null)
                        return hit;
                }
            }

            foreach (Transform t in vic.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "textolite_1")
                    continue;

                if (t.parent != null && t.parent.name == "T64_armour_hull")
                    return t;
            }

            return null;
        }

        static void ApplyFiveLayerUfpArmour(Vehicle vic)
        {
            Armour.Init();

            if (Armour.t64bvUfpFiveLayer == null)
                return;

            Transform ufp = FindUfpTextoliteTransform(vic);
            if (ufp == null)
                return;

            VariableArmor armor = ufp.GetComponent<VariableArmor>() ?? ufp.GetComponentInChildren<VariableArmor>(true);
            if (armor == null)
                return;

            armor._armorType = Armour.t64bvUfpFiveLayer;
            armor._name = "5-layer UFP";

            AarVisual aar = ufp.GetComponent<AarVisual>() ?? ufp.GetComponentInChildren<AarVisual>(true);
            if (aar != null && T64Assets.aar5layerufp != null)
            {
                aar.AarMaterial = T64Assets.aar5layerufp;
                aar.UpdateVisibility();
            }
        }

        public static void Config(MelonPreferences_Category cfg)
        {
            var random_ammo_pool = new List<string>()
            {
                "3BM26",
                "3BM32",
                "3BM42",
                "3BM46"
            };

            t64bv_patch = cfg.CreateEntry<bool>("T-64BV Patch", true);
            t64bv_patch.Comment = "Stand-alone T-64BV / T-64BV1 (obr.1985 & obr.1988 Agava). Kontakt-1 ERA is always applied.";

            t64bv_ammo_type_1985 = cfg.CreateEntry<string>("AP Round (T-64BV)", "3BM32");
            t64bv_ammo_type_1985.Comment = "T-64BV / T-64BV1 obr.1985. 3BM32, 3BM26, 3BM42, 3BM46";

            t64bv_heat_type_1985 = cfg.CreateEntry<string>("HEAT Round (T-64BV)", "3BK18M");
            t64bv_heat_type_1985.Comment = "3BK18M";

            t64bv_random_ammo_1985 = cfg.CreateEntry<bool>("Random AP Round (T-64BV)", false);
            t64bv_random_ammo_pool_1985 = cfg.CreateEntry<List<string>>("Random AP Round Pool (T-64BV)", random_ammo_pool);
            t64bv_random_ammo_pool_1985.Comment = "3BM26, 3BM32, 3BM42, 3BM46";

            t64bv_ammo_type_1988 = cfg.CreateEntry<string>("AP Round (T-64BV 1988)", "3BM32");
            t64bv_ammo_type_1988.Comment = "T-64BV / T-64BV1 obr.1988. 3BM32, 3BM26, 3BM42, 3BM46";

            t64bv_heat_type_1988 = cfg.CreateEntry<string>("HEAT Round (T-64BV 1988)", "3BK18M");
            t64bv_heat_type_1988.Comment = "3BK18M";

            t64bv_random_ammo_1988 = cfg.CreateEntry<bool>("Random AP Round (T-64BV 1988)", false);
            t64bv_random_ammo_pool_1988 = cfg.CreateEntry<List<string>>("Random AP Round Pool (T-64BV 1988)", random_ammo_pool);
            t64bv_random_ammo_pool_1988.Comment = "3BM26, 3BM32, 3BM42, 3BM46";

            zoom_snapper = cfg.CreateEntry<bool>("Quick Zoom Switch (T-64BV)", true);
            zoom_snapper.Comment = "Press middle mouse to instantly switch between low and high magnification on the daysight";
        }

        public static IEnumerator Convert(GameState _)
        {
            Mod.vics = UnityEngine.Object.FindObjectsByType<Vehicle>(FindObjectsSortMode.None);

            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;

                if (t64bv_patch == null || !t64bv_patch.Value)
                    continue;

                if (!TryGetVariant(vic.UniqueName, out BvVariant variant))
                    continue;

                GameObject vic_go = vic.gameObject;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                    continue;

                vic_go.AddComponent<AlreadyConverted>();
                vic._friendlyName = variant.FriendlyName;

                ArmyBinocularLookupFix.EnsureSovietEntries(vic.FriendlyName);

                {
                    Transform hull = vic.transform.Find("---T64A_MESH---/HULL");
                    Transform turret = vic.transform.Find("---T64A_MESH---/HULL/TURRET");
                    Transform markers = turret.transform.Find("T64A_markings");
                    if (markers != null)
                        markers.gameObject.SetActive(false);
                    else
                        MelonLogger.Warning($"[T64BV] Could not find T64A_markings on {vic.FriendlyName}.");

                    Transform oldturret = turret.transform.Find("turret_mesh");
                    if (oldturret != null)
                        oldturret.gameObject.SetActive(false);

                    GameObject k1_full = GameObject.Instantiate(T64Assets.t64bv_full);

                    Transform era_hull = k1_full.transform.Find("HULL K1");
                    Transform era_turret = k1_full.transform.Find("TURRET K1");
                    Transform hull_misc = k1_full.transform.Find("HULL MISC");
                    Transform turret_misc = k1_full.transform.Find("TURRET MISC");

                    GameObject newTurretInstance = T64Assets.t64bv_turret != null ? GameObject.Instantiate(T64Assets.t64bv_turret) : null;
                    Transform newturret = newTurretInstance != null ? newTurretInstance.transform : null;

                    Transform originalTurretMesh = vic.transform.Find("---T64A_MESH---/HULL/TURRET/turret_mesh");

                    Renderer originalRenderer = originalTurretMesh != null ? originalTurretMesh.GetComponentInChildren<Renderer>(true) : null;

                    Renderer replacementRenderer = newturret != null ? newturret.GetComponentInChildren<Renderer>(true) : null;

                    if (originalRenderer != null && replacementRenderer != null)
                    {
                        Material originalMaterial = originalRenderer.sharedMaterial;

                        if (originalMaterial != null && originalMaterial.HasProperty("_Albedo") && originalMaterial.GetTexture("_Albedo") != null)
                        {
                            replacementRenderer.sharedMaterial = originalMaterial;

                        }
                        else
                        {
                            MelonLogger.Warning("[T64BV MATERIAL] Original turret material has no assigned _Albedo.");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[T64BV MATERIAL] Original or replacement turret renderer not found.");
                    }

                    // HULL ERA
                    if (era_hull != null)
                    {
                        era_hull.SetParent(hull, false);
                        if (hull_misc != null)
                            hull_misc.SetParent(hull, false);

                        era_hull.localPosition = new Vector3(-0.06f, -0.35f, -0.785f);
                        era_hull.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_hull.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_hull.SetParent(hull_late_follow);

                        hull_misc.localPosition = new Vector3(-0.06f, 0.39f, -0.785f);
                        hull_misc.localEulerAngles = new Vector3(0f, 90f, 0f);
                        hull_misc.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        hull_misc.SetParent(hull_late_follow);

                    }
                    // TURRET ERA
                    if (era_turret != null)
                    {
                        era_turret.SetParent(turret, false);
                        if (turret_misc != null)
                            turret_misc.SetParent(turret, false);

                        era_turret.localPosition = new Vector3(-0.075f, -0.25f, -0.24f);
                        era_turret.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_turret.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        Transform turret_late_follow = turret.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_turret.SetParent(turret_late_follow);

                        turret_misc.localPosition = new Vector3(-0.06f, -0.27f, -0.43f);
                        turret_misc.localEulerAngles = new Vector3(0f, 90f, 0f);
                        turret_misc.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        turret_misc.SetParent(turret_late_follow);
                    }

                    if (newturret != null)
                    {
                        newturret.SetParent(turret, false);

                        newturret.localPosition = new Vector3(0f, -0.33f, -0.0175f);
                        newturret.localEulerAngles = new Vector3(270f, 270f, 0f);
                        newturret.localScale = new Vector3(355f, 355f, 355f);

                        ScorchUtil.RegisterScorchRoot(vic, newturret, oldturret);
                        ScorchUtil.PruneInvalidScorchRenderers(vic);
                    }

                    VehicleSmokeManager smoke_manager = vic.GetComponentInChildren<VehicleSmokeManager>();

                    if (smoke_manager != null)
                    {
                        Transform smokes = turret_misc.Find("SMOKES");

                        if (smokes != null)
                        {
                            // MelonLogger.Msg("Found K1 smoke rack");
                            List<Transform> new_smokes = new List<Transform>();

                            for (int i = 0; i < 8; i++)
                            {
                                Transform cap = smokes.Find("SMOKE CAP " + i);

                                if (cap != null)
                                    new_smokes.Add(cap);
                            }
                            // MelonLogger.Msg("K1 smoke caps found: " + new_smokes.Count);

                            for (int i = 0; i < smoke_manager._smokeSlots.Length; i++)
                            {
                                VehicleSmokeManager.SmokeSlot slot = smoke_manager._smokeSlots[i];
                                Transform smoke_cap = new_smokes[i];

                                slot.DisplayBone = smoke_cap;
                                slot.SpawnLocation.transform.SetParent(smoke_cap);
                                slot.SpawnLocation.transform.position = smoke_cap.GetComponent<Renderer>().bounds.center;
                            }
                        }
                        else
                        {
                            // MelonLogger.Msg("FAILED TO FIND TURRET K1");
                        }
                    }
                    else
                    {
                        // MelonLogger.Msg("FAILED TO FIND SMOKE MANAGER");
                    }

                    GameObject.Destroy(k1_full);

                    Transform old_smokes = turret.transform.Find("T64B_smoke");
                    old_smokes?.gameObject.SetActive(false);

                    Transform camonet1 = vic.transform.Find("---T64A_MESH---/HULL/t64a hull net");
                    camonet1?.gameObject.SetActive(false);

                    Transform camonet2 = vic.transform.Find("---T64A_MESH---/HULL/TURRET/t64a turret net");
                    if (camonet2 != null)
                    {
                        camonet2.gameObject.SetActive(false);
                    }

                    Transform camonet3 = vic.transform.Find("---T64A_MESH---/HULL/TURRET/Main gun/Muzzle identity/t64a gun net");
                    if (camonet3 != null)
                    {
                        camonet3.gameObject.SetActive(false);
                    }
                }

                WeaponSystem weapon = vic.GetComponent<WeaponsManager>().Weapons[0].Weapon;
                LoadoutManager loadout_manager = vic.GetComponent<LoadoutManager>();

                string ammo_str = ResolveApRound(variant);
                string heat_str = ResolveHeatRound(variant);

                FireControlSystem fcs = vic.GetComponentInChildren<FireControlSystem>();
                UsableOptic day_optic = Util.GetDayOptic(fcs);
                UsableOptic night_optic = day_optic.slot.LinkedNightSight.PairedOptic;

                try
                {
                    loadout_manager.LoadedAmmoList.AmmoClips[0] = Ammo_125mm.ap[ammo_str];

                    if (heat_str == "3BK18M")
                        loadout_manager.LoadedAmmoList.AmmoClips[1] = SharedAssets.clip_codex_3bk18m;

                    for (int i = 0; i < loadout_manager.RackLoadouts.Length; i++)
                    {
                        GHPC.Weapons.AmmoRack rack = loadout_manager.RackLoadouts[i].Rack;
                        Util.EmptyRack(rack);
                    }

                    loadout_manager.SpawnCurrentLoadout();
                    weapon.Feed.AmmoTypeInBreech = null;
                    weapon.Feed.Start();
                    loadout_manager.RegisterAllBallistics();
                }
                catch (Exception)
                {
                    MelonLogger.Msg("Failed, loading default ammo for " + vic.FriendlyName);
                }

                if (zoom_snapper.Value)
                    day_optic.gameObject.AddComponent<DigitalZoomSnapper>();

                Transform canvas = vic.transform.Find("---T64A_MESH---/HULL/TURRET/Main gun/---MAIN GUN SCRIPTS---/2A46/1G42 gunner's sight/GPS/1G42 Canvas/GameObject");
                canvas.Find("ammo text APFSDS (TMP)").gameObject.SetActive(true);

                vic.AimablePlatforms[3].transform.Find("optic cover parent").gameObject.SetActive(false);

                if (variant.HasAgava)
                {
                    PactThermal.Add(night_optic, AgavaPreset);

                    night_optic.Alignment = OpticAlignment.BoresightStabilized;
                    night_optic.RotateAzimuth = true;

                    AiSpotterUtil.ApplyGen1ThermalNightFighting(vic);

                    LampType lampType = LampType.GunnerSpotlight;
                    EquipmentManager equipmentManager = vic._equipmentManager;
                    if (equipmentManager != null)
                    {
                        equipmentManager.lampsDict.Remove(lampType);
                        equipmentManager.AllLamps = equipmentManager.AllLamps.Where(o => o.Type != lampType).ToArray();
                    }
                }

                yield return null;
                ApplyFiveLayerUfpArmour(vic);
            }
        }

        public override void LoadStaticAssets()
        {
            if (!t64bv_patch.Value) return;

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "t64bv_full");

            if (T64Assets.t64bv_full == null)
            {
                MelonLogger.Error("Could not find t64bv_full inside bundle, please check that your fulda1989 assets are installed!");
                return;
            }

            T64Assets.t64bv_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Transform hull_k1 = T64Assets.t64bv_full.transform.Find("HULL K1/ARMOUR");
            Transform turret_k1 = T64Assets.t64bv_full.transform.Find("TURRET K1/TURRET K1 ARMOUR");

            Kontakt1.Setup(hull_k1, hull_k1.parent);
            Kontakt1.Setup(turret_k1, turret_k1.parent);

            Util.SetupFLIRShaders(T64Assets.t64bv_full);

            MelonLogger.Msg("Loaded T64BV ERA asset successfully");
        }

        [HarmonyPatch]
        public static class PILT64BCompatibilityPatch
        {
            static bool Prepare()
            {
                Type type = AccessTools.TypeByName("PactIncreasedLethality.T64B");
                return type != null && AccessTools.Method(type, "HandleConversion") != null;
            }

            static MethodBase TargetMethod()
            {
                Type type = AccessTools.TypeByName("PactIncreasedLethality.T64B");
                return AccessTools.Method(type, "HandleConversion");
            }

            static bool Prefix(Vehicle vic)
            {
                if (vic == null)
                    return true;

                return !IsStandaloneSpawn(vic);
            }
        }

        [HarmonyPatch]
        public static class PILT64B1CompatibilityPatch
        {
            static bool Prepare()
            {
                Type type = AccessTools.TypeByName("PactIncreasedLethality.T64B1");
                return type != null && AccessTools.Method(type, "HandleConversion") != null;
            }

            static MethodBase TargetMethod()
            {
                Type type = AccessTools.TypeByName("PactIncreasedLethality.T64B1");
                return AccessTools.Method(type, "HandleConversion");
            }

            static bool Prefix(Vehicle vic)
            {
                if (vic == null)
                    return true;

                return !IsStandaloneSpawn(vic);
            }
        }

        public static void Init()
        {
            if (!t64bv_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}
