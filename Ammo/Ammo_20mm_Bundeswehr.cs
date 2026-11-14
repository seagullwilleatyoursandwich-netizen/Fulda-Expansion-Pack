using GHPC.Effects;
using GHPC.Weaponry;
using GHPC.Weapons;
using ModUtil;
using System;
using UnityEngine;

namespace Fulda1989
{
    public class Ammo_20mm_Bundeswehr : ModUtil.Module
    {
        public static AmmoClipCodexScriptable clip_codex_dm81;
        public static AmmoType.AmmoClip clip_dm81;
        public static AmmoCodexScriptable ammo_codex_dm81;
        public static AmmoType ammo_dm81;
        public static GameObject ammo_dm81_vis = null;

        public override void UnloadDynamicAssets()
        {
            GameObject.DestroyImmediate(ammo_dm81_vis);
        }

        public override void LoadDynamicAssets()
        {
            // DM81 HEI-T (RH202 KA1+; base K uses vanilla DM51A2 via RH202.Setup)
            ammo_dm81 = new AmmoType();
            Util.ShallowCopy(ammo_dm81, Bundeswehr_20mm_Assets.ammo_HEI);
            ammo_dm81.Name = "(20MM) DM81 HEI-T";
            ammo_dm81.Caliber = 20;
            ammo_dm81.RhaPenetration = 16;
            ammo_dm81.ImpactAudio = GHPC.Audio.ImpactAudioType.AutocannonExplosive;
            ammo_dm81.TntEquivalentKg = 0.03f;
            ammo_dm81.MuzzleVelocity = 1055;
            ammo_dm81.Mass = 0.12f;
            ammo_dm81.SpallMultiplier = 0.15f;
            ammo_dm81.CertainRicochetAngle = 10;
            ammo_dm81.ShatterOnRicochet = false;
            ammo_dm81.AlwaysProduceBlast = false;
            ammo_dm81.DetonateSpallCount = 15;
            ammo_dm81.SectionalArea = 0.0003f;
            ammo_dm81.CachedIndex = -1;
            ammo_dm81.VisualType = LiveRoundMarshaller.LiveRoundVisualType.APDS;
            ammo_dm81.ShortName = AmmoType.AmmoShortName.He;
            ammo_dm81.Category = AmmoType.AmmoCategory.Explosive;
            ammo_dm81.ImpactEffectDescriptor.HasImpactEffect = true;
            ammo_dm81.ImpactEffectDescriptor.ImpactCategory = ParticleEffectsManager.Category.HighExplosive;
            ammo_dm81.ImpactDecalDescriptor.HasImpactDecal = true;
            ammo_dm81.ImpactDecalDescriptor.DecalCategory = ImpactDecalsManager.DecalCategory.Explosion;

            Util.Coalesce(ref ammo_codex_dm81);
            ammo_codex_dm81.AmmoType = ammo_dm81;
            ammo_codex_dm81.name = "ammo_dm81";

            clip_dm81 = new AmmoType.AmmoClip();
            clip_dm81.Capacity = 1;
            clip_dm81.Name = "DM81 HEI-T";
            clip_dm81.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm81.MinimalPattern[0] = ammo_codex_dm81;

            Util.Coalesce(ref clip_codex_dm81);
            clip_codex_dm81.name = "clip_dm81";
            clip_codex_dm81.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[2];
            clip_codex_dm81.ClipType = clip_dm81;

            Util.CacheAmmo(ammo_dm81);
        }
    }
}