using GHPC.Weaponry;
using GHPC.Weapons;
using MelonLoader;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VanillaAssetUtil.AssetUtil;
using System;

namespace Fulda1989
{
    internal class Bundeswehr_140mm_Assets : ModUtil.Module
    {
        public static AmmoType ammo_APFSDS;
        public static AmmoType ammo_HEAT;

        private static GameObject ammo_APFSDS_Visual;
        private static GameObject ammo_HEAT_Visual;
        private const float VisualScale140mm = 140f / 120f;
        public override void LoadDynamicAssets()
        {
            AssetUtilApi.LoadVanillaVehicle("LEO2A3", temp: true);

            var clipCodexes = Resources.FindObjectsOfTypeAll<AmmoClipCodexScriptable>();

            var dm23 = clipCodexes.FirstOrDefault(o => o.name == "clip_DM23(120)");
            var dm12a1 = clipCodexes.FirstOrDefault(o => o.name == "clip_DM12A1(120)");

            AmmoType sourceAP = dm23?.ClipType?.MinimalPattern?.FirstOrDefault()?.AmmoType;
            AmmoType sourceHEAT = dm12a1?.ClipType?.MinimalPattern?.FirstOrDefault()?.AmmoType;

            ammo_APFSDS = CreateScaledAmmoType(sourceAP, "DM23 140mm Visual", out ammo_APFSDS_Visual);
            ammo_HEAT = CreateScaledAmmoType(sourceHEAT, "DM12A1 140mm Visual", out ammo_HEAT_Visual);

            if (ammo_APFSDS == null)
                MelonLogger.Error("[140MM] Could not create the DM23 ammo type.");

            if (ammo_HEAT == null)
                MelonLogger.Error("[140MM] Could not create the DM12A1 ammo type.");

            MelonLogger.Msg($"[140MM] DM23 visual ready: {ammo_APFSDS?.VisualModel != null}");
            MelonLogger.Msg($"[140MM] DM12A1 visual ready: {ammo_HEAT?.VisualModel != null}");
        }
        //public override void UnloadDynamicAssets()
        //{
        //    if (ammo_APFSDS_Visual != null)
        //        GameObject.DestroyImmediate(ammo_APFSDS_Visual);

        //    if (ammo_HEAT_Visual != null)
        //        GameObject.DestroyImmediate(ammo_HEAT_Visual);

        //    ammo_APFSDS_Visual = null;
        //    ammo_HEAT_Visual = null;
        //    ammo_APFSDS = null;
        //    ammo_HEAT = null;
        //}
        private static AmmoType CreateScaledAmmoType(AmmoType source, string visualName, out GameObject visual)
        {
            visual = null;

            if (source == null)
                return null;

            AmmoType resizedAmmo = new AmmoType();

            // Copy the original ammunition data without modifying the vanilla type.
            for (Type type = typeof(AmmoType); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!field.IsInitOnly && !field.IsLiteral)
                        field.SetValue(resizedAmmo, field.GetValue(source));
                }
            }

            resizedAmmo.CachedIndex = -1;

            if (source.VisualModel != null)
            {
                visual = GameObject.Instantiate(source.VisualModel);
                visual.name = visualName;
                visual.transform.localScale = source.VisualModel.transform.localScale * VisualScale140mm;

                AmmoStoredVisual storedVisual = visual.GetComponent<AmmoStoredVisual>();

                if (storedVisual != null)
                    storedVisual.AmmoType = resizedAmmo;

                resizedAmmo.VisualModel = visual;
            }

            Util.CacheAmmo(resizedAmmo);

            return resizedAmmo;
        }
    }
}
