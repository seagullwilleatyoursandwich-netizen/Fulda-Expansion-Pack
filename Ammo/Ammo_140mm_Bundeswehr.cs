using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GHPC.State;
using GHPC.Weapons;
using UnityEngine;
using GHPC.Weaponry;
using MelonLoader;

namespace Fulda1989
{
    public class Ammo_140mm_Bundeswehr : ModUtil.Module
    {
        public static Dictionary<string, AmmoClipCodexScriptable> ap;
        public static Dictionary<string, AmmoClipCodexScriptable> heat;
        public static AmmoClipCodexScriptable clip_codex_xm16;
        public static AmmoType.AmmoClip clip_xm16;
        public static AmmoCodexScriptable ammo_codex_xm16;
        public static AmmoType ammo_xm16;
        public static AmmoClipCodexScriptable clip_codex_xm33;
        public static AmmoType.AmmoClip clip_xm33;
        public static AmmoCodexScriptable ammo_codex_xm33;
        public static AmmoType ammo_xm33;
        public static GameObject ammo_xm33_vis = null;
        public static GameObject ammo_xm16_vis = null;

        public static void CreateCompositeOptimizations()
        {
            var composite_optimizations_xm33 = new List<AmmoType.ArmorOptimization>() { };

            string[] composite_names = new string[]
            {
                "C-Tech composite",
                "Vorschlaghammer composite",
                "Eber composite",
                "Abrams special armor gen 1 hull front",
                "Abrams special armor gen 1 mantlet",
                "Abrams special armor gen 1 turret cheeks",
                "Abrams special armor gen 1 turret sides",
                "Abrams special armor gen 0 turret cheeks",
                "Corundum ball armor",
                "Kvartz"
            };

            ArmorCodexScriptable[] armours = Resources.FindObjectsOfTypeAll<ArmorCodexScriptable>();

            foreach (string name in composite_names)
            {
                IEnumerable<ArmorCodexScriptable> possible_armours = armours.Where(o => o.name == name);

                if (possible_armours.Count() == 0) continue;

                ArmorCodexScriptable armour = possible_armours.First();

                AmmoType.ArmorOptimization optimization_xm33 = new AmmoType.ArmorOptimization();
                optimization_xm33.Armor = armour;
                optimization_xm33.RhaRatio = 0.80f;
                composite_optimizations_xm33.Add(optimization_xm33);
            }

            ammo_xm33.ArmorOptimizations = composite_optimizations_xm33.ToArray<AmmoType.ArmorOptimization>();
        }

        public static IEnumerator CreateCompositeOptimizationsDeferred(GameState _)
        {
            CreateCompositeOptimizations();
            yield break;
        }

        public override void UnloadDynamicAssets()
        {
            GameObject.DestroyImmediate(ammo_xm33_vis);
            GameObject.DestroyImmediate(ammo_xm16_vis);
        }

        public override void LoadDynamicAssets()
        {
            // XM33 Experimental APFSDS
            ammo_xm33 = new AmmoType();
            Util.ShallowCopy(ammo_xm33, Bundeswehr_120mm_Assets.ammo_APFSDS);
            ammo_xm33.Name = "XM33 APFSDS-T";
            ammo_xm33.Caliber = 140;
            ammo_xm33.RhaPenetration = 795;
            ammo_xm33.SpallMultiplier = 1.1f;
            ammo_xm33.MuzzleVelocity = 1800;
            ammo_xm33.Mass = 7.46f;
            ammo_xm33.SectionalArea = 0.0014f;
            ammo_xm33.Coeff = 0.3f;
            ammo_xm33.CachedIndex = -1;
            ammo_xm33.SpallMultiplier = 1f;
            ammo_xm33.CachedIndex = -1;
            ammo_xm33.CertainRicochetAngle = 4f;
            ammo_xm33.Coeff = 0.3f;
            ammo_xm33.MaxSpallRha = 18f;
            ammo_xm33.MinSpallRha = 2f;
            ammo_xm33.ShatterOnRicochet = true;
            ammo_xm33.MaximumRange = 3800f;

            Util.Coalesce(ref ammo_codex_xm33);
            ammo_codex_xm33.AmmoType = ammo_xm33;
            ammo_codex_xm33.name = "ammo_xm33";

            clip_xm33 = new AmmoType.AmmoClip();
            clip_xm33.Capacity = 1;
            clip_xm33.Name = "XM33 APFSDS-T";
            clip_xm33.MinimalPattern = new AmmoCodexScriptable[1];
            clip_xm33.MinimalPattern[0] = ammo_codex_xm33;

            Util.Coalesce(ref clip_codex_xm33);
            clip_codex_xm33.name = "ammo_XM33";
            clip_codex_xm33.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_xm33.ClipType = clip_xm33;

            // XM16 Experimental HEAT-FS-T
            ammo_xm16 = new AmmoType();
            Util.ShallowCopy(ammo_xm16, Bundeswehr_120mm_Assets.ammo_HEAT);
            ammo_xm16.Name = "XM16 HEAT-FS-T";
            ammo_xm16.Caliber = 140;
            ammo_xm16._radius = 0.0625f;
            ammo_xm16.RhaPenetration = 880f;
            ammo_xm16.TntEquivalentKg = 3.76f;
            ammo_xm16.MuzzleVelocity = 1560f;
            ammo_xm16.Mass = 12.6f;
            ammo_xm16.MaxSpallRha = 18;
            ammo_xm16.MinSpallRha = 2;
            ammo_xm16.CertainRicochetAngle = 3f;
            ammo_xm16.ShatterOnRicochet = false;
            ammo_xm16.DetonateSpallCount = 70;
            ammo_xm16.SectionalArea = 0.0112f;
            ammo_xm16.Coeff = 0.22f;
            ammo_xm16.ForcedSpallAngle = 60;
            ammo_xm16.IgnoreSlat = true;
            ammo_xm16.CachedIndex = -1;
            ammo_xm16.AlwaysProduceBlast = true;
            ammo_xm16.Category = AmmoType.AmmoCategory.ShapedCharge;
            ammo_xm16.SpallMultiplier = 1.10f;
            ammo_xm16.MicroFragScaling = 0.8f;
            ammo_xm16.MaximumRange = 3250f;

            Util.Coalesce(ref ammo_codex_xm16);
            ammo_codex_xm16.AmmoType = ammo_xm16;
            ammo_codex_xm16.name = "ammo_xm16";

            clip_xm16 = new AmmoType.AmmoClip();
            clip_xm16.Capacity = 1;
            clip_xm16.Name = "XM16 HEAT-FS-T";
            clip_xm16.MinimalPattern = new AmmoCodexScriptable[1];
            clip_xm16.MinimalPattern[0] = ammo_codex_xm16;

            Util.Coalesce(ref clip_codex_xm16);
            clip_codex_xm16.name = "ammo_XM16";
            clip_codex_xm16.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_xm16.ClipType = clip_xm16;

            if (Bundeswehr_120mm_Assets.ammo_APFSDS == null || Bundeswehr_120mm_Assets.ammo_APFSDS.VisualModel == null)
                MelonLogger.Error("[140MM] XM33 has no 120mm DM23 visual to copy.");
            else
            {
                ammo_xm33_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_APFSDS.VisualModel);
                ammo_xm33_vis.name = "XM33 visual";
                ammo_xm33_vis.SetActive(true);
                ammo_xm33.VisualModel = ammo_xm33_vis;
                ammo_xm33.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_xm33;
                ammo_xm33.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_xm33;
            }

            if (Bundeswehr_120mm_Assets.ammo_HEAT == null || Bundeswehr_120mm_Assets.ammo_HEAT.VisualModel == null)
                MelonLogger.Error("[140MM] XM16 has no 120mm DM12A1 visual to copy.");
            else
            {
                ammo_xm16_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_HEAT.VisualModel);
                ammo_xm16_vis.name = "XM16 visual";
                ammo_xm16_vis.SetActive(true);
                ammo_xm16.VisualModel = ammo_xm16_vis;
                ammo_xm16.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_xm16;
                ammo_xm16.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_xm16;
            }

            Util.CacheAmmo(ammo_xm33);
            Util.CacheAmmo(ammo_xm16);

            ap = new Dictionary<string, AmmoClipCodexScriptable>()
            {
                ["XM33"] = clip_codex_xm33,
            };

            heat = new Dictionary<string, AmmoClipCodexScriptable>()
            {
                ["XM16"] = clip_codex_xm16,
            };
        }
    }
}
