using GHPC.Equipment;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weaponry;
using GHPC.Weapons;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Fulda1989
{
    public class M60ERA : ModUtil.Module
    {
        static MelonPreferences_Entry<bool> m60era_patch;
        static MelonPreferences_Entry<bool> m833_apfsds;
        static MelonPreferences_Entry<bool> m900;
        static MelonPreferences_Entry<int> m900_chance;

        public static bool PatchEnabled => m60era_patch != null && m60era_patch.Value;

        public static void Config(MelonPreferences_Category category)
        {
            m60era_patch = category.CreateEntry("M60 1991 Patch", true);
            m60era_patch.Comment = "Enables M60's equipped with ERA and stronger ammunition - standalone, found in the customiser.";

            m833_apfsds = category.CreateEntry("M60s Use M833 APFSDS-T", true);
            m833_apfsds.Comment = "Applies M833 APFSDS-T to M60 '91 vehicles.";

            m900 = category.CreateEntry("M60A3 TTS 1991 Uses M900 APFSDS-T", false);
            m900.Comment = "Allows M60A3 TTS 1991's to roll M900 instead of M833, changes main gun to M68A1 when successful.";

            m900_chance = category.CreateEntry("M60A3 TTS 1991 M900 Chance (%)", 20);
            m900_chance.Comment = "Chance for an M60A3 TTS 1991 to receive M900 instead of M833.";
        }

        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null)
                    continue;

                GameObject vic_go = vic.gameObject;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                    continue;

                string uniqueName = vic.UniqueName;
                if (string.IsNullOrEmpty(uniqueName))
                    continue;

                if (!string.Equals(uniqueName, FuldaStandaloneRegistrations.M60A1RiseEra, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(uniqueName, FuldaStandaloneRegistrations.M60A3TtsEra, StringComparison.OrdinalIgnoreCase))
                    continue;

                IEnumerator conversion = ApplyM60EraConversion(vic);
                while (conversion.MoveNext())
                    yield return conversion.Current;
            }
        }

        private static IEnumerator ApplyM60EraConversion(Vehicle vic)
        {
            GameObject vic_go = vic.gameObject;
            vic_go.AddComponent<AlreadyConverted>();

            bool isA3 = string.Equals(vic.UniqueName, FuldaStandaloneRegistrations.M60A3TtsEra, StringComparison.OrdinalIgnoreCase);
            bool isA1 = string.Equals(vic.UniqueName, FuldaStandaloneRegistrations.M60A1RiseEra, StringComparison.OrdinalIgnoreCase);

            if (isA3)
                vic._friendlyName = "M60A3 TTS 1991";
            else if (isA1)
                vic._friendlyName = "M60A1 RISE(P) 1991";

            Transform hull = vic.transform.Find("M60_meshes/hull_late");
            Transform turret = isA3 ? vic.transform.Find("M60A3TTS_rig/hull/turret") : isA1 ? vic.transform.Find("--RIG/hull/turret") : null;
            Transform mantlet = isA3 ? vic.transform.Find("M60A3TTS_rig/hull/turret/main gun mantlet") : isA1 ? vic.transform.Find("--RIG/hull/turret/main gun mantlet") : null;

            bool m900Applied = false;

            if (isA3)
            {
                bool getsM900 = m900.Value && UnityEngine.Random.Range(0, 100) < Mathf.Clamp(m900_chance.Value, 0, 100);

                if (getsM900)
                {
                    m900Applied = ApplyApfsdsToRefurbishedM60(vic, Ammo_105mm_USA.clip_codex_m900, "M900 APFSDS-T");

                    if (m900Applied)
                    {
                        Transform gunTransform = vic.transform.Find("M60A3TTS_rig/hull/turret/main gun mantlet/Gun Scripts/105mm Gun M68");
                        WeaponSystem gun = gunTransform != null ? gunTransform.GetComponent<WeaponSystem>() : null;

                        if (gun != null && gun.CodexEntry != null)
                        {
                            WeaponSystemCodexScriptable m68a1Codex = ScriptableObject.Instantiate(gun.CodexEntry);
                            m68a1Codex.name = "gun_M68A1";
                            m68a1Codex.FriendlyName = "105mm Gun M68A1";
                            gun.CodexEntry = m68a1Codex;
                        }
                        else
                        {
                            MelonLogger.Warning($"[M60 ERA] M900 was applied, but the M68 WeaponSystem or CodexEntry was not found on {vic.FriendlyName}.");
                        }
                    }
                }
            }

            if (!m900Applied && m833_apfsds.Value && (isA1 || isA3))
                ApplyApfsdsToRefurbishedM60(vic, FindM833Clip(), "M833 APFSDS-T");

            GameObject m1_full = GameObject.Instantiate(M60_Assets.m60_full);

            Transform era_hull = m1_full.transform.Find("HULL M1");
            Transform era_turret = m1_full.transform.Find("TURRET M1");
            Transform era_mantlet = m1_full.transform.Find("MANTLET M1");

            if (era_hull != null)
            {
                era_hull.SetParent(hull, false);

                era_hull.localPosition = new Vector3(0.14f, -0.035f, 0.03f);
                era_hull.localEulerAngles = new Vector3(0f, 90f, 0f);
                era_hull.localScale = new Vector3(0.1725f, 0.1725f, 0.1725f);

                Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                era_hull.SetParent(hull_late_follow);
            }

            if (era_turret != null)
            {
                era_turret.SetParent(turret, false);

                era_turret.localPosition = new Vector3(0.8f, -0.51f, 2.7f);
                era_turret.localEulerAngles = new Vector3(0f, 90f, 0f);
                era_turret.localScale = new Vector3(1f, 1f, 1f);

                Transform turret_late_follow = turret.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                era_turret.SetParent(turret_late_follow);
            }

            if (era_mantlet != null)
            {
                era_mantlet.SetParent(mantlet, false);

                era_mantlet.localPosition = new Vector3(0.81f, -0.88f, 1.7f);
                era_mantlet.localEulerAngles = new Vector3(0f, 90f, 0f);
                era_mantlet.localScale = new Vector3(1f, 1f, 1f);

                Transform mantlet_late_follow = mantlet.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                era_mantlet.SetParent(mantlet_late_follow);
            }

            GameObject.Destroy(m1_full);

            if (isA1)
            {
                GHPC.Equipment.Lamps.LampType lamp_type = GHPC.Equipment.Lamps.LampType.GunnerSpotlight;

                EquipmentManager equipment_manager = vic._equipmentManager;

                if (equipment_manager != null)
                {
                    equipment_manager.lampsDict.Remove(lamp_type);
                    equipment_manager.AllLamps = equipment_manager.AllLamps.Where(o => o.Type != lamp_type).ToArray();
                }

                Transform irLamp = vic.transform.Find("--RIG/hull/turret/main gun mantlet/M60_VVS");

                if (irLamp != null)
                {
                    foreach (Renderer renderer in irLamp.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer != null)
                            renderer.enabled = false;
                    }

                    foreach (Light lampLight in irLamp.GetComponentsInChildren<Light>(true))
                    {
                        if (lampLight != null)
                            lampLight.enabled = false;
                    }

                    irLamp.gameObject.SetActive(false);
                }
            }

            string[] objectsToDestroy =
            {
                "M60A3TTS_rig/hull/M60 hull net",
                "M60A3TTS_rig/hull/turret/M60 turret net",
                "--RIG/hull/M60 hull net",
                "--RIG/hull/turret/M60 turret net",
                "M60A3TTS_rig/hull/turret/main gun mantlet/spare_wheel_mantlet",
                "--RIG/hull/turret/Turret decorations/us_20L_water",
            };

            foreach (string path in objectsToDestroy)
            {
                Transform obj = vic.transform.Find(path);

                if (obj != null)
                    GameObject.Destroy(obj.gameObject);
            }

            ApplyM60EraCamo(vic_go);
            yield return null;
        }

        private static bool IsApfsdsRound(AmmoCodexScriptable round)
        {
            return round != null && round.AmmoType != null && !string.IsNullOrEmpty(round.AmmoType.Name) && round.AmmoType.Name.IndexOf("APFSDS", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsApfsdsClip(AmmoClipCodexScriptable clip)
        {
            return clip != null && clip.ClipType != null && clip.ClipType.MinimalPattern != null && clip.ClipType.MinimalPattern.Any(IsApfsdsRound);
        }

        private static bool IsApfsdsClip(AmmoType.AmmoClip clip)
        {
            return clip != null && clip.MinimalPattern != null && clip.MinimalPattern.Any(IsApfsdsRound);
        }

        private static AmmoClipCodexScriptable FindM833Clip()
        {
            return Resources.FindObjectsOfTypeAll<AmmoClipCodexScriptable>().FirstOrDefault(c => c != null && c.name.Equals("clip_M833", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ApplyApfsdsToRefurbishedM60(Vehicle vic, AmmoClipCodexScriptable replacementClip, string roundName)
        {
            LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();

            if (loadoutManager == null || loadoutManager.LoadedAmmoList == null ||
                loadoutManager.LoadedAmmoList.AmmoClips == null || loadoutManager.RackLoadouts == null)
            {
                MelonLogger.Warning($"[M60 ERA] Cannot apply {roundName} to {vic.FriendlyName}: loadout unavailable.");
                return false;
            }

            if (replacementClip == null || replacementClip.ClipType == null)
            {
                MelonLogger.Warning($"[M60 ERA] Cannot apply {roundName} to {vic.FriendlyName}: replacement clip unavailable.");
                return false;
            }

            AmmoClipCodexScriptable[] loadedClips = loadoutManager.LoadedAmmoList.AmmoClips;
            List<int> apIndices = new List<int>();

            for (int i = 0; i < loadedClips.Length; i++)
            {
                if (IsApfsdsClip(loadedClips[i]))
                    apIndices.Add(i);
            }

            if (apIndices.Count == 0)
            {
                string loadedNames = string.Join(", ", loadedClips.Select(c => c != null ? $"{c.name} ({c.ClipType?.Name})" : "NULL"));
                MelonLogger.Warning($"[M60 ERA] No existing APFSDS clip found for {vic.FriendlyName}. Loaded clips: {loadedNames}");
                return false;
            }

            foreach (int index in apIndices)
                loadedClips[index] = replacementClip;

            loadoutManager.LoadedAmmoList.AmmoClips = loadedClips;

            for (int i = 0; i < loadoutManager.RackLoadouts.Length; i++)
            {
                var rackLoadout = loadoutManager.RackLoadouts[i];
                GHPC.Weapons.AmmoRack rack = rackLoadout.Rack;

                if (rack == null)
                    continue;

                AmmoClipCodexScriptable[] initialClips = rackLoadout.OverrideInitialClips;

                if (initialClips != null)
                {
                    for (int j = 0; j < initialClips.Length; j++)
                    {
                        if (!IsApfsdsClip(initialClips[j]))
                            continue;

                        initialClips[j] = replacementClip;
                    }

                    rackLoadout.OverrideInitialClips = initialClips;
                }

                if (rack.ClipTypes != null)
                {
                    for (int j = 0; j < rack.ClipTypes.Length; j++)
                    {
                        if (!IsApfsdsClip(rack.ClipTypes[j]))
                            continue;

                        rack.ClipTypes[j] = replacementClip.ClipType;
                    }
                }

                Util.EmptyRack(rack);
            }

            loadoutManager.SpawnCurrentLoadout();

            WeaponsManager weaponsManager = vic.GetComponent<WeaponsManager>();

            if (weaponsManager != null && weaponsManager.Weapons != null && weaponsManager.Weapons.Length > 0)
            {
                WeaponSystem mainGun = weaponsManager.Weapons[0].Weapon;

                if (mainGun != null && mainGun.Feed != null)
                {
                    mainGun.Feed.AmmoTypeInBreech = null;
                    mainGun.Feed.Start();
                }
            }

            loadoutManager.RegisterAllBallistics();

            return true;
        }

        public override void LoadStaticAssets()
        {
            if (!m60era_patch.Value) return;

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "m60_full");

            if (M60_Assets.m60_full == null)
            {
                MelonLogger.Error("Could not find m60_full inside bundle, please check that your fulda1989 assets are installed!");
                return;
            }

            M60_Assets.m60_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Transform hull_m1 = M60_Assets.m60_full.transform.Find("HULL M1/HULL M1 ERA");
            Transform turret_m1 = M60_Assets.m60_full.transform.Find("TURRET M1/TURRET M1 ERA");
            Transform mantlet_m1 = M60_Assets.m60_full.transform.Find("MANTLET M1/MANTLET M1 ERA");

            M1ERA.Setup(hull_m1, hull_m1.parent);
            M1ERA.Setup(turret_m1, turret_m1.parent);
            M1ERA.Setup(mantlet_m1, mantlet_m1.parent);

            Util.SetupFLIRShaders(M60_Assets.m60_full);

            MelonLogger.Msg("Loaded M60 ERA asset successfully");
        }

        private static void ApplyM60EraCamo(GameObject vehicleRoot)
        {
            if (vehicleRoot == null)
                return;

            Texture2D replacement = M60_Assets.woodlandM60Era;

            if (replacement == null)
            {
                MelonLogger.Warning("[M60 ERA CAMO] Woodland M60 ERA texture is unavailable.");
                return;
            }

            Transform meshRoot = vehicleRoot.transform.Find("M60_meshes");

            if (meshRoot == null)
            {
                MelonLogger.Warning("[M60 ERA CAMO] Could not find M60_meshes on vehicle; skipping MERDC swap.");
                return;
            }

            foreach (Renderer renderer in meshRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.materials;

                foreach (Material material in materials)
                {
                    if (material == null)
                        continue;

                    foreach (string propertyName in material.GetTexturePropertyNames())
                    {
                        Texture texture = material.GetTexture(propertyName);

                        if (texture == null)
                            continue;

                        string textureName = texture.name.Replace(" (Instance)", "");

                        if (!textureName.Equals("MERDC", StringComparison.OrdinalIgnoreCase))
                            continue;

                        material.SetTexture(propertyName, replacement);
                    }
                }

                renderer.materials = materials;
            }
        }

        public static void Init()
        {
            if (!m60era_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}
