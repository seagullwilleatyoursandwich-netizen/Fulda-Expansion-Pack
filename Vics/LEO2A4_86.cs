using GHPC;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using ModUtil;
using System.Collections;
using UnityEngine;

namespace Fulda1989
{
    // LEO2A3 stand-in with DM33; no C-tech or 2A4 side-skirt kit (1986 batch vs 1988).
    public class LEO2A4_86 : ModUtil.Module
    {
        public static MelonPreferences_Entry<bool> leo2a4_86_patch;
        public static bool PatchEnabled => leo2a4_86_patch != null && leo2a4_86_patch.Value;

        public static void Config(MelonPreferences_Category category)
        {
            leo2a4_86_patch = category.CreateEntry("Leopard 2A4 (1986) Patch", true);
            leo2a4_86_patch.Comment = "Enables the Leopard 2A4 (1986) stand-alone (2A3 hull, DM33 - no C-tech).";
        }

        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null)
                    continue;

                if (!vic.UniqueName.Equals("LEO2A4_86"))
                    continue;

                GameObject vic_go = vic.gameObject;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                    continue;

                vic_go.AddComponent<AlreadyConverted>();

                LoadoutManager loadout_manager = vic.GetComponent<LoadoutManager>();
                WeaponsManager weapons_manager = vic.GetComponent<WeaponsManager>();

                if (loadout_manager != null && loadout_manager.LoadedAmmoList != null && loadout_manager.LoadedAmmoList.AmmoClips != null
                    && loadout_manager.LoadedAmmoList.AmmoClips.Length > 0 && weapons_manager != null && weapons_manager.Weapons != null
                    && weapons_manager.Weapons.Length > 0 && weapons_manager.Weapons[0].Weapon != null)
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
                    MelonLogger.Warning("[LEO2A4_86] Could not update the DM33 ammunition loadout.");
                }

                LEO2A4.EnableTurretToss(vic, loadout_manager);

                yield return null;
            }
        }

        public static void Init()
        {
            if (!leo2a4_86_patch.Value)
                return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}