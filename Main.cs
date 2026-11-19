using FMOD;
using Fulda1989;
using Fulda1989.Missions;
using GHPC;
using GHPC.Audio;
using GHPC.Camera;
using GHPC.Mission;
using GHPC.Player;
using GHPC.State;
using GHPC.Vehicle;
using MelonLoader;
using ModUtil;
using System;
using Fulda1989.FailRate;
using System.Collections;
using System.Linq;
using UnityEngine;

[assembly: MelonInfo(typeof(Mod), "Fulda Expansion Pack [Modernisation Effort - PART 1]", "1.5.2", "ZironTheDerg + The Fulda Team")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace Fulda1989
{
    public class Mod : MelonMod
    {
        internal const string MelonCategoryId = "Fulda 1989 | Expansion Pack";

        private static ModuleManager module_manager;
        internal static Vehicle[] vics;
        internal static MelonPreferences_Category cfg;

        private GameObject game_manager;
        internal static AudioSettingsManager audio_settings_manager;
        internal static PlayerInput player_manager;
        internal static CameraManager camera_manager;

        private int valid_scene_count = 0;
        internal static FMOD.ChannelGroup audio_channel_group;


        public IEnumerator OnPlayerReady(GameState _) 
        {
            game_manager = GameObject.Find("_APP_GHPC_");
            audio_settings_manager = game_manager.GetComponent<AudioSettingsManager>();
            player_manager = game_manager.GetComponent<PlayerInput>();
            camera_manager = game_manager.GetComponent<CameraManager>();
            vics = GameObject.FindObjectsByType<Vehicle>(FindObjectsSortMode.None);
            yield break;
        }

        public override void OnInitializeMelon()
        {
            module_manager = new ModuleManager("Fulda1989");
            cfg = MelonPreferences.CreateCategory(MelonCategoryId);
            FailRateSystem.Config(cfg);
            T64AV.Config(cfg);
            T64BV.Config(cfg);
            M60ERA.Config(cfg);
            BMP2D.Config(cfg);
            LEO2A4.Config(cfg);
            LEO2A4_86.Config(cfg);
            LEO2A4_140.Config(cfg);
            LEO2K.Config(cfg);

            new HarmonyLib.Harmony("Fulda1989.RH202Ammo").PatchAll();

            var cor_system = FMODUnity.RuntimeManager.CoreSystem;

            cor_system.createChannelGroup("master", out audio_channel_group);
            audio_channel_group.setVolumeRamp(true);
            audio_channel_group.setMode(MODE._3D_WORLDRELATIVE);

            module_manager.Add("SharedAssets", new SharedAssets());
            module_manager.Add("Ammo_120mm_assets_Leo2k", new Bundeswehr_120mm_Assets());
            module_manager.Add("Ammo_20mm_assets_Leo2k", new Bundeswehr_20mm_Assets());
            module_manager.Add("Ammo_140mm_Bundeswehr", new Ammo_140mm_Bundeswehr());
            module_manager.Add("AMMO_125MM", new Ammo_125mm());
            module_manager.Add("Ammo_105mm_usa", new Ammo_105mm_USA());
            module_manager.Add("Ammo_120mm_Leo2k", new Ammo_120mm_Bundeswehr());
            module_manager.Add("Ammo_20mm_Leo2k", new Ammo_20mm_Bundeswehr());
            module_manager.Add("M60_Assets", new M60_Assets());
            module_manager.Add("LEO2A4_Assets", new LEO2A4_Assets());
            module_manager.Add("LEO2K_Assets", new LEO2K_Assets());
            module_manager.Add("BMP2D_Assets", new BMP2D_Assets());
            module_manager.Add("T64Assets", new T64Assets());
            module_manager.Add("T64AV", new T64AV());
            module_manager.Add("T64BV", new T64BV());
            module_manager.Add("M60ERA", new M60ERA());
            module_manager.Add("LEO2A4_140", new LEO2A4_140());
            module_manager.Add("LEO2A4", new LEO2A4());
            module_manager.Add("LEO2A4_86", new LEO2A4_86());
            module_manager.Add("LEO2K", new LEO2K());
            module_manager.Add("BMP-2D", new BMP2D());
            module_manager.Add("SuperFCS", new SuperFCS());
            module_manager.Add("PactThermal", new PactThermal());
            module_manager.Add("1A40", new FireControlSystem1A40());
            module_manager.Add("BOM", new BOM());

            FuldaTrainingRangeMissions.TryRegister();
        }

        public IEnumerator MissionUnitsLoaded(GameState _)
        {
            if (Util.ShouldSkipVehicleConvert())
                yield break;

            FailRateSystem.ResetForNewMission();
            UnitSpawner.Instance.SpawnUnit("LEO2K", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "LEO2K",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1228.898f, 23.5087f, 1516.342f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("LEO2A4", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "LEO2A4",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1233.509f, 23.631f, 1488.909f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("LEO2A4_86", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "LEO2A4_86",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1246.271f, 23.6095f, 1477.414f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("LEO2A4_140", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "LEO2A4_140",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1239.392f, 23.5152f, 1532.086f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("T64AV", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "T64AV",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1231.894f, 23.6437f, 1551.609f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("T64BV_1988", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "T64BV_1988",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1219.982f, 23.3118f, 1586.291f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("T64BV", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "T64BV",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1231.222f, 23.4277f, 1561.181f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("M60A1RISEP_ERA", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "M60A1RISEP_ERA",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1381.515f, 26.014f, 1357.938f),
                    Rotation = Quaternion.Euler(359.4105f, 248.9597f, 0.712f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("M60A3TTS_ERA", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "M60A3TTS_ERA",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1381.515f, 26.014f, 1347.938f),
                    Rotation = Quaternion.Euler(359.4105f, 248.9597f, 0.712f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            yield break;
        }

        public override void OnApplicationQuit()
        {
            FuldaStandaloneRegistrations.DisposeAll();
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            FuldaStandaloneRegistrations.SyncCustomizerFromPatchToggles();
            FuldaTrainingRangeMissions.TryRegister();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            FuldaCampaignRoster.StartApply();

            if (Util.MenuScreens.Contains(sceneName))
            {
                module_manager.LoadAllStaticAssets();
            }

            if (sceneName == "TR01_showcase")
            {
                StateController.RunOrDefer(GameState.MissionUnitsLoaded, new GameStateEventHandler(MissionUnitsLoaded), GameStatePriority.Medium);
            }

            if (Util.MenuScreensWithLoader.Contains(sceneName)) return;

            if (!Util.MenuScreens.Contains(sceneName))
                FailRateSystem.ResetForNewMission();
            valid_scene_count++;

            if (valid_scene_count == 1)
            {
                module_manager.UnloadAllDynamicAssets();
            }

            if (valid_scene_count == 2)
            {
                module_manager.LoadAllDynamicAssets();
                StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(OnPlayerReady), GameStatePriority.Highest);
                StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Ammo_125mm.CreateCompositeOptimizationsDeferred), GameStatePriority.Low);
                StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Ammo_140mm_Bundeswehr.CreateCompositeOptimizationsDeferred), GameStatePriority.Low);
                StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Ammo_105mm_USA.CreateCompositeOptimizationsDeferred), GameStatePriority.Low);
                StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Ammo_120mm_Bundeswehr.CreateCompositeOptimizationsDeferred), GameStatePriority.Low);

                FuldaEra.Init();
                M60ERA.Init();
                T64AV.Init();
                T64BV.Init();
                BMP2D.Init();
                LEO2A4_140.Init();
                LEO2A4.Init();
                LEO2A4_86.Init();
                LEO2K.Init();

                valid_scene_count = 0;
            }
        }
    }
}
