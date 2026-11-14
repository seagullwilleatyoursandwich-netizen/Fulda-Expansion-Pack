using GHPC;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using ModUtil;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MelonLoader.Utils;
using System.IO;
using System.Linq;

namespace Fulda1989
{
    public class LEO2A4 : ModUtil.Module
    {
        public static MelonPreferences_Entry<bool> leo2a4_patch;
        public static bool PatchEnabled => leo2a4_patch != null && leo2a4_patch.Value;
        public static Material ctechAarMaterial;
        public static void Config(MelonPreferences_Category category)
        {
            leo2a4_patch = category.CreateEntry("Leopard 2A4 Patch", true);
            leo2a4_patch.Comment = "Enables the Leopard 2A4 1988 (Standalone - Found inside the vehicle customiser)";
        }
        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;

                GameObject vic_go = vic.gameObject;
                if (!vic.UniqueName.Equals("LEO2A4")) continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                    continue;

                vic_go.AddComponent<AlreadyConverted>();
                ApplyCtechArmour(vic);

                LoadoutManager loadout_manager = vic.GetComponent<LoadoutManager>();
                WeaponsManager weapons_manager = vic.GetComponent<WeaponsManager>();

                if (loadout_manager != null && loadout_manager.LoadedAmmoList != null && loadout_manager.LoadedAmmoList.AmmoClips != null && loadout_manager.LoadedAmmoList.AmmoClips.Length > 0 && weapons_manager != null && weapons_manager.Weapons != null && weapons_manager.Weapons.Length > 0 && weapons_manager.Weapons[0].Weapon != null)
                {
                    WeaponSystem main_gun = weapons_manager.Weapons[0].Weapon;

                    loadout_manager.LoadedAmmoList.AmmoClips[0] = Ammo_120mm_Bundeswehr.ap["DM33"];

                    for (int i = 0; i < loadout_manager.RackLoadouts.Length; i++)
                        Util.EmptyRack(loadout_manager.RackLoadouts[i].Rack);

                    loadout_manager.SpawnCurrentLoadout();

                    if (main_gun.Feed != null)
                    {
                        main_gun.Feed.AmmoTypeInBreech = null;
                        main_gun.Feed.Start();
                    }

                    loadout_manager.RegisterAllBallistics();
                }
                else
                {
                    MelonLogger.Warning("[LEO2A4] Could not update the DM33 ammunition loadout.");
                }
                Transform skirtdown = vic_go.transform.Find("LEO2_mesh/rear skirt down");
                skirtdown.gameObject.SetActive(true);
                Transform altskirt = vic_go.transform.Find("LEO2_mesh/rear skirt unclipped");
                altskirt.gameObject.SetActive(false);
                Transform skirtup = vic_go.transform.Find("LEO2_mesh/rear skirt up");
                skirtup.gameObject.SetActive(false);

                if (LEO2A4_Assets.full140 != null)
                {
                    GameObject kwsfull = GameObject.Instantiate(LEO2A4_Assets.full140);
                    Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                    Transform leo2hullarmour = hull_late_follow.Find("LEO2_armour_hull");
                    Transform sideskirtaddons = kwsfull.transform.Find("hull/side skirts");
                    sideskirtaddons.SetParent(leo2hullarmour, false);
                    sideskirtaddons.localPosition = new Vector3(0.06f, 1.4f, 0.885f);
                    sideskirtaddons.localEulerAngles = new Vector3(0f, 90f, 0f);
                    Transform skirtLeft = sideskirtaddons.Find("2a4 side skirt left");
                    Transform skirtRight = sideskirtaddons.Find("2a4 side skirt right");
                    Transform hullnet = vic.transform.Find("LEO2_camo");
                    GameObject.Destroy(hullnet.gameObject);

                    Transform commonturret = vic.transform.Find("leo2_rig/hull/turret/turret common parts");
                    Material leo2Co = TakeLeo2CoFromCommonTurret(commonturret);
                    ApplyLeo2CoToSideSkirts(skirtLeft, skirtRight, leo2Co);

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

                    GameObject.Destroy(kwsfull);
                    ScorchUtil.PruneInvalidScorchRenderers(vic);
                }
                else
                {
                    MelonLogger.Warning("[LEO2A4] full140 missing; skipped side skirt conversion.");
                }

                EnableTurretToss(vic, loadout_manager);

                yield return null;
            }
        }


        private static readonly FieldInfo f_ammoRackLocation = typeof(GHPC.Weapons.AmmoRack).GetField("Location", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo f_canPopTurret = typeof(GHPC.Weapons.AmmoRack).GetField("CanPopTurret", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static void EnableTurretToss(Vehicle vic, LoadoutManager loadoutManager)
        {
            VehicleInfo info = vic.GetComponent<VehicleInfo>();
            if (info == null)
                info = vic.GetComponentInChildren<VehicleInfo>(true);

            SlewableTurret turret = info != null ? info.Turret : null;
            if (turret != null)
            {
                turret.AmmoHitPopsTurret = true;
                if (turret.PoppedTurretColliders == null || turret.PoppedTurretColliders.Length == 0)
                {
                    GameObject turretObject = turret.Turret != null ? turret.Turret : turret.gameObject;
                    turret.PoppedTurretColliders = turretObject.GetComponentsInChildren<Collider>(true);
                }

                MelonLogger.Msg($"[LEO2A4] Ammo detonations toss the turret. Pop colliders={turret.PoppedTurretColliders.Length}.");
            }

            if (loadoutManager != null && loadoutManager.RackLoadouts != null && f_ammoRackLocation != null && f_canPopTurret != null)
            {
                foreach (LoadoutManager.RackLoadout rackLoadout in loadoutManager.RackLoadouts)
                {
                    GHPC.Weapons.AmmoRack rack = rackLoadout.Rack;
                    if (rack == null)
                        continue;

                    object location = f_ammoRackLocation.GetValue(rack);
                    if (location != null && System.Convert.ToInt32(location) == 1)
                        f_canPopTurret.SetValue(rack, true);
                }
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

            // TURRET FRONT ARRAYS
            ApplyCtechAtPath(turretRoot,"LEO2_armour_turret/turret armour/cheek array left A", eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/cheek array right A", eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/cheek array right B", eberAarMaterial, ref updated);

            // TURRET SIDE ARRAYS
            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/array left", eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/array right", eberAarMaterial, ref updated);

            // GUN MANTLET
            // The path matcher intentionally returns ALL matching objects,
            // including duplicates with identical names and hierarchy paths.
            ApplyCtechAtPath(gunRoot,"mantlet armour/mantlet", eberAarMaterial, ref updated);

            // HULL FRONT ARRAYS
            ApplyCtechAtPath(hullRoot,"LEO2_armour_hull/upper glacis array", eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot,"LEO2_armour_hull/lower glacis array", eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/sponson array left", eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/sponson array right", eberAarMaterial, ref updated);

            // HULL FRONT LEFT/RIGHT COMPOSITE ARRAYS
            ApplyCtechAtPath(hullRoot,"LEO2_armour_hull/front left array", eberAarMaterial, ref updated);

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

            if (LEO2A4_Assets.full140 == null)
                return;

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

        private static Material TakeLeo2CoFromCommonTurret(Transform commonturret)
        {
            if (commonturret == null)
                return null;

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

        private static void ApplyLeo2CoToSideSkirts(Transform skirtLeft, Transform skirtRight, Material turretMaterial)
        {
            if (turretMaterial == null)
            {
                MelonLogger.Warning("[LEO2A4] Could not find LEO2_co on donor turret common parts.");
                return;
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

        public static void Init()
        {
            if (!leo2a4_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}
