using GHPC;
using GHPC.Player;
using GHPC.State;
using GHPC.UI.Hud;
using GHPC.Weapons;
using HarmonyLib;

namespace Fulda1989
{
    /// <summary>Stop mantlet and RCWS FCS from adopting each other's weapon/ballistics.</summary>
    [HarmonyPatch(typeof(FireControlSystem), nameof(FireControlSystem.SetWeaponSystem))]
    public static class Rh202FcsSetWeaponSystemGuard
    {
        const string Rh202FcsObjectName = "RH202_FCS";
        const string Rh202WeaponObjectName = "RH202_MarderWeapon";

        public static bool Prefix(FireControlSystem __instance, WeaponSystem w)
        {
            if (__instance == null || w == null)
                return true;

            bool rh202Fcs = __instance.gameObject.name == Rh202FcsObjectName;
            bool rh202Weapon = w.name == Rh202WeaponObjectName;

            if (rh202Fcs && !rh202Weapon)
                return false;

            if (!rh202Fcs && rh202Weapon)
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.QueueAmmoIndex))]
    public static class RH202AmmoSwitchPatch
    {
        public static bool Prefix(PlayerInput __instance, int index)
        {
            if (__instance.CurrentPlayerCrewBrain == null || __instance.CurrentPlayerCrewBrain.WeaponsModule == null || __instance.CurrentPlayerCrewBrain.WeaponsModule.ActiveWeapon == null)
                return true;

            WeaponSystem activeWeapon = __instance.CurrentPlayerCrewBrain.WeaponsModule.ActiveWeapon.Weapon;
            if (activeWeapon == null || activeWeapon.name != "RH202_MarderWeapon")
                return true;

            AmmoFeed feed = activeWeapon.Feed;
            if (feed == null || feed.ReadyRack == null)
                return false;

            AmmoType.AmmoClip[] clipTypes = feed.ReadyRack.ClipTypes;
            if (clipTypes == null || index < 0 || index >= clipTypes.Length)
                return false;

            __instance.CurrentPlayerCrewBrain.WeaponsModule.QueueClipType(clipTypes[index], firstWeaponOnly: false);
            return false;
        }
    }

    /// <summary>Runtime RH202 is added after <see cref="Unit.Awake"/>; refresh before stock HUD reads damage.</summary>
    [HarmonyPatch(typeof(Unit), nameof(Unit.DamageStatus), MethodType.Getter)]
    public static class Rh202UnitDamageStatusPatch
    {
        public static void Prefix(Unit __instance)
        {
            if (RH202.UnitHasRh202(__instance))
                RH202.RefreshUnitDamageStatuses(__instance);
        }
    }

    [HarmonyPatch(typeof(Unit), nameof(Unit.Damaged), MethodType.Getter)]
    public static class Rh202UnitDamagedPatch
    {
        public static void Prefix(Unit __instance)
        {
            if (RH202.UnitHasRh202(__instance))
                RH202.RefreshUnitDamageStatuses(__instance);
        }
    }

    [HarmonyPatch(typeof(DamageStatusHud), "Update")]
    public static class Rh202DamageStatusHudRefreshPatch
    {
        public static void Prefix()
        {
            PlayerInput input = PlayerInput.Instance;
            Unit unit = input != null ? input.CurrentPlayerUnit : null;
            if (unit != null && RH202.UnitHasRh202(unit))
                RH202.RefreshUnitDamageStatuses(unit);
        }
    }
}