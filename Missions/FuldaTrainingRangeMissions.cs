using Eflatun.SceneReference;
using GHPC;
using GHPC.Event;
using GHPC.Mission;
using GHPC.Mission.Data;
using GHPC.State;
using GHPC.UI;
using GHPC.UI.Menu;
using GHPC.World;
using TMPro;
using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fulda1989.Missions
{
    public sealed class FuldaTrainingRangeMissionDefinition
    {
        public Func<bool> PatchEnabled;
        public bool Enabled = true;
        public string MissionTitle;
        // Mission briefing body (FactionMissionInfo.Description / Mission Brief menu TMP).
        public string Debrief;
        public string PlayerUnitKey;
        public string[] PlayerVehicleSourceKeys;
        public (string From, string To)[] EnemyUnitSwaps;
        public string BlueVictoryMessage;
        public string FuldaFlexAssetName;
        public string LogLabel;
        // Vanilla flex mission to clone (default: Leo 2 range layout).
        public string SourceFlexAssetName;
        public Faction VictoryFaction = Faction.Blue;

        public bool IsMissionEnabled => Enabled && PatchEnabled != null && PatchEnabled();

        public string ResolveSourceFlexAssetName()
        {
            return string.IsNullOrEmpty(SourceFlexAssetName)
                ? FuldaTrainingRangeMissions.DefaultLeo2SourceFlexAssetName
                : SourceFlexAssetName;
        }
    }
    public static class FuldaNpzk140TrainingRangeConfig
    {
        public static readonly FuldaTrainingRangeMissionDefinition Definition = new FuldaTrainingRangeMissionDefinition
        {
            PatchEnabled = () => LEO2A4_140.PatchEnabled,
            Enabled = true,
            MissionTitle = "NPzK-140 Training Range",
            Debrief =
@"You've been selected to test an experimental MBT; 
x1 'NPzK-140' at Grafenwoehr Range. 

It is being rolled out in scarce numbers by the Bundeswehr, it is equipped with less armour but a significantly more potent main cannon of the 140mm caliber type.
You will be shooting elite soviet targets, ranging from T-64, T-72 and T-80 units.

General Guide:

The NPzK-140 (1991) is a modification of a 7th batch Leopard 2A4, as such it behaves very similarly to the other mainline Leopard 2's, it is noticeably more sluggish to accelerate, and has a slower turret traverse speed thanks to the added weight.
It is equipped with a bustle-rack fitted autoloader system firing the 140mm main gun at 13.33 RPM (4.5 seconds per round), and, while an amazing tank hunter/armoured vehicle destroyer, it lacks the versatility against infantry or lighter targets seen in other vehicles like the Leopard 2K with its 20mm RCWS suite. 
You will be thoroughly punished for assuming it can take any target due to its gun size alone.

'XM33' - The APFSDS shell used, travels at nearly 2km/s after being fired, this allows for effective engagement ranges to reach well over 3.5km+ whilst the HEAT round, 'XM16', is best used at maximum ranges of 2.5-3.25km
It is also additionally important to note the lack of composite array in the mantlet portion of the turret, the NPzK-140's all have priority on using the latest 'C-Tech' composite technology, but this area specifically had to be omitted due to weight and gun breech constraints.

The Bundeswehr will record and review your performance for future endeavors.",
            PlayerUnitKey = "LEO2A4_140",
            PlayerVehicleSourceKeys = new[] { "LEO2A3" },
            EnemyUnitSwaps = new[]
            {
                ("T55A", "T80B"),
                ("BMP1", "T72M1"),
                ("BTR60PB", "T64B")
            },
            BlueVictoryMessage = "You have completed the NPzK-140 training range.",
            FuldaFlexAssetName = "TR01_LEO2A4_140_Range_Fulda",
            LogLabel = "NPzK-140",
        };
    }

    public static class FuldaLeo2KTrainingRangeConfig
    {
        public static readonly FuldaTrainingRangeMissionDefinition Definition = new FuldaTrainingRangeMissionDefinition
        {
            PatchEnabled = () => LEO2K.PatchEnabled,
            Enabled = true,
            MissionTitle = "Leopard 2K Training Range",
            Debrief =
@"You've been assigned a 'Leopard 2K' for range qualification and testing.

Utilise both the 120 mm main gun and the roof-mounted Rh202 autocannon to your heart's content and get comfortable with the fire-control system.

General guide:

Use Ctrl + E to dump lead on the Leopard 2KA2 and 2KA3 respectively, as they automatically apply lead indefinitely once a lase is successful.
The Rh202 RCWS unit is effective but fragile, it is most effectively used in defilade as a peeking tool for spotting units, and disabling the optics or critical parts of enemy vehicles.

HEAT is a multi-purpose shell with some anti-armour use against older or lighter vehicles. DM12 is the weakest, DM12A1 is in-between, and DM12A1K is an experimental HEAT shell with better penetration but worse flight performance.
APFSDS is for anti-armour purposes, and is best used against MBT's. DM13 is the weakest, DM23 is standard, DM33 is extremely effective and DM43K is an experimental APFSDS shell with some of the highest penetration but poor flight performance, both at range and against composite armour compared to DM33.

FCS notes:

The base Leopard 2K utilises the original EMES-12 you can find on the Leopard 1A4; they behave identically.
The Leopard 2KA1 'Vogel' utilises EMES-12K, which is EMES-12 with an added laser rangefinder, in addition to having thermal imaging albeit at poor quality, and point stabilisation like the main Leopard 2 line.
The Leopard 2KA2 'Falke' and Leopard 2KA3 'Adler' receive the best fire control systems available. They get automatic lead on lase, which can be dumped by pressing Ctrl + E if needed. Both come with dual-axis point stabilisation, and their Rh202 roof cannons receive a laser rangefinder (2KA2 and 2KA3) and thermal sight (2KA3 only), respectively.",
            PlayerUnitKey = "LEO2K",
            PlayerVehicleSourceKeys = new[] { "LEO2A3" },
            EnemyUnitSwaps = new[]
            {
                ("T55A", "T72M1"),
                ("BMP1", "BMP2"),
                ("BTR60PB", "T62")
            },
            BlueVictoryMessage = "You have completed the Leopard 2K training range.",
            FuldaFlexAssetName = "TR01_LEO2K_Range_Fulda",
            LogLabel = "Leopard 2K",
        };
    }

    public static class FuldaT64Bv1988TrainingRangeConfig
    {
        public static readonly FuldaTrainingRangeMissionDefinition Definition = new FuldaTrainingRangeMissionDefinition
        {
            PatchEnabled = () => T64BV.PatchEnabled,
            Enabled = true,
            MissionTitle = "T-64BV obr.1988 Training Range",
            Debrief =
@"You've been issued a T-64BV obr.1988 with the Agava thermal imager for range qualification at Grafenwoehr.

Engage NATO stand-ins across the layout. Pick a **night** time option in mission config to practice the Agava sight and thermal target identification.

General guide:

The T-64BV obr.1988 carries Kontakt-1 ERA and the Agava Gen-1 thermal channel on the night sight; Agava does not have multiple magnification modes and has undesirable quality, 
but its nightfighting capability is still vastly superior compared to the IR spotlight system.
Use the day sight for long-range daylight work; switch to the thermal channel after dusk or under overcast.
APFSDS is your primary anti-armour loadout. HEAT remains useful against lighter armour and soft targets at closer range.

You can expect OPFOR on this range to be M1 Abrams and Marder 1A2 in place of the usual M60A1 and M113's.",
            PlayerUnitKey = FuldaStandaloneRegistrations.T64Bv1988Unit,
            PlayerVehicleSourceKeys = new[] { "T80B" },
            SourceFlexAssetName = "TR01_T80B_Range",
            EnemyUnitSwaps = new[]
            {
                ("M60A1RISEP", "M1"),
                ("M113", "MARDER1A2"),
            },
            BlueVictoryMessage = "You have completed the T-64BV obr.1988 training range.",
            VictoryFaction = Faction.Red,
            FuldaFlexAssetName = "TR01_T64BV_1988_Range_Fulda",
            LogLabel = "T-64BV obr.1988",
        };
    }

    public static class FuldaTrainingRangeMissions
    {
        public const string DefaultLeo2SourceFlexAssetName = "TR01_LEO2_Range";
        public const string TrainingTheaterKey = "FG85_TR01";

        private static readonly FuldaTrainingRangeMissionDefinition[] AllDefinitions =
        {
            FuldaNpzk140TrainingRangeConfig.Definition,
            FuldaLeo2KTrainingRangeConfig.Definition,
            FuldaT64Bv1988TrainingRangeConfig.Definition,
        };

        private sealed class RegisteredRangeMission
        {
            public FuldaTrainingRangeMissionDefinition Definition;
            public MissionMetaData Mission;
            public DynamicMissionMetadataScriptable Flex;
        }

        private static readonly List<RegisteredRangeMission> Registered = new List<RegisteredRangeMission>();

        private static readonly FieldInfo SelectedMissionField = typeof(MissionBriefMenu).GetField("_selectedMission", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo BriefTitleField = typeof(MissionBriefMenu).GetField("_title", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo BriefingBodyField = typeof(MissionBriefMenu).GetField("_briefing", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool IsFuldaTrainingMission(MissionMetaData meta)
        {
            return ResolveDefinition(meta) != null;
        }

        public static bool IsActiveFuldaTrainingMission()
        {
            MissionDataState missionState = UnityEngine.Object.FindObjectOfType<MissionDataState>();
            return ResolveDefinition(missionState?.MetaData) != null;
        }

        private static FuldaTrainingRangeMissionDefinition ResolveDefinition(MissionMetaData meta)
        {
            if (meta == null)
                return null;

            foreach (RegisteredRangeMission entry in Registered)
            {
                if (entry?.Definition == null)
                    continue;

                if (entry.Mission != null && ReferenceEquals(meta, entry.Mission))
                    return entry.Definition;

                if (entry.Flex != null && meta.FlexMissionData != null && ReferenceEquals(meta.FlexMissionData, entry.Flex))
                    return entry.Definition;

                if (meta.FlexMissionData != null &&
                    string.Equals(meta.FlexMissionData.name, entry.Definition.FuldaFlexAssetName, StringComparison.OrdinalIgnoreCase))
                    return entry.Definition;

                if (string.Equals(meta.MissionName, entry.Definition.MissionTitle, StringComparison.Ordinal))
                    return entry.Definition;
            }

            return null;
        }

        public static void PreferMenuSelectedMission(string theaterKey, string resourceKey, ref MissionMetaData resolved)
        {
            MissionMetaData picked = MissionMenuSetup.lastSelectedMission;
            if (picked == null || picked.IsCategory)
                return;

            SceneReference sceneRef = picked.MissionSceneReference;
            if (sceneRef == null || string.IsNullOrEmpty(sceneRef.Name))
                return;

            if (!string.Equals(sceneRef.Name, resourceKey, StringComparison.Ordinal))
                return;

            if (resolved != null && ReferenceEquals(resolved, picked))
                return;

            resolved = picked;
        }

        public static void ApplyLaunchTitleForEndScreen(MissionBriefMenu menu)
        {
            if (menu == null)
                return;

            MissionMetaData meta = SelectedMissionField != null ? SelectedMissionField.GetValue(menu) as MissionMetaData : null;
            FuldaTrainingRangeMissionDefinition def = ResolveDefinition(meta);
            if (def == null || string.IsNullOrWhiteSpace(def.MissionTitle))
                return;

            if (BriefTitleField?.GetValue(menu) is TextMeshProUGUI titleTmp)
                titleTmp.text = def.MissionTitle;
        }

        public static void ApplyEndScreenResultsTitle(ScenarioResultManager manager)
        {
            if (manager == null)
                return;

            MissionMetaData meta = manager._missionDataState?.MetaData;
            FuldaTrainingRangeMissionDefinition def = ResolveDefinition(meta);
            if (def == null)
                return;

            string title = def.MissionTitle;
            if (DynamicMissionComposer.IsModifiedFlexMission)
                title += " (Customized)";

            MissionResults results = manager.MissionResults;
            if (results != null)
                results.MissionName = title;
        }

        public static void ApplyBriefingOverride(MissionBriefMenu menu)
        {
            if (menu == null)
                return;

            MissionMetaData meta = SelectedMissionField != null ? SelectedMissionField.GetValue(menu) as MissionMetaData : null;
            FuldaTrainingRangeMissionDefinition def = ResolveDefinition(meta);
            if (def == null || string.IsNullOrWhiteSpace(def.Debrief))
                return;

            string text = def.Debrief;
            if (BriefingBodyField?.GetValue(menu) is TextMeshProUGUI briefingTmp)
                briefingTmp.SetText(text);
            else
                menu.Briefing = text;
        }

        public static void TryRegister()
        {
            AllMissionsMetaDataScriptable catalog = Resources.FindObjectsOfTypeAll<AllMissionsMetaDataScriptable>().FirstOrDefault();
            if (catalog == null || catalog.Theaters == null)
                return;

            MissionTheaterScriptable trainingTheater = catalog.Theaters.FirstOrDefault(t => t != null && (string.Equals(t.Key, TrainingTheaterKey, StringComparison.OrdinalIgnoreCase) || (t.Name != null && t.Name.IndexOf("Training", StringComparison.OrdinalIgnoreCase) >= 0)));

            if (trainingTheater == null)
            {
                MelonLogger.Warning("[FULDA RANGE] Could not find FG85_TR01 / Training Range theater.");
                return;
            }

            foreach (FuldaTrainingRangeMissionDefinition def in AllDefinitions)
            {
                if (!def.IsMissionEnabled)
                {
                    UnregisterOne(def.FuldaFlexAssetName);
                    continue;
                }

                RegisteredRangeMission existing = Registered.FirstOrDefault(r => r?.Definition != null && string.Equals(r.Definition.FuldaFlexAssetName, def.FuldaFlexAssetName, StringComparison.OrdinalIgnoreCase));

                if (existing != null && existing.Mission != null && Array.IndexOf(trainingTheater.Missions, existing.Mission) >= 0)
                {
                    RefreshMutableFields(existing);
                    continue;
                }

                MissionMetaData templateMission = FindTemplateRangeMission(trainingTheater, def);
                if (templateMission == null)
                {
                    MelonLogger.Warning($"[FULDA RANGE] Could not find {def.ResolveSourceFlexAssetName()} template mission ({def.LogLabel}).");
                    continue;
                }

                if (templateMission.FlexMissionData == null)
                {
                    MelonLogger.Warning($"[FULDA RANGE] Template mission has no flex metadata ({def.LogLabel}).");
                    continue;
                }

                DynamicMissionMetadataScriptable flexClone = UnityEngine.Object.Instantiate(templateMission.FlexMissionData);
                flexClone.name = def.FuldaFlexAssetName;
                flexClone.hideFlags = HideFlags.DontUnloadUnusedAsset;

                ApplyFlexUnitSwaps(flexClone, def);

                MissionMetaData missionClone = CloneMissionMeta(templateMission, flexClone, def);

                List<MissionMetaData> missions = trainingTheater.Missions != null ? trainingTheater.Missions.ToList() : new List<MissionMetaData>();

                missions.RemoveAll(m => m != null && m.FlexMissionData != null && string.Equals(m.FlexMissionData.name, def.FuldaFlexAssetName, StringComparison.OrdinalIgnoreCase));

                missions.Add(missionClone);
                trainingTheater.Missions = missions.ToArray();

                UnregisterOne(def.FuldaFlexAssetName);

                Registered.Add(new RegisteredRangeMission
                {
                    Definition = def,
                    Mission = missionClone,
                    Flex = flexClone,
                });

                MelonLogger.Msg($"[FULDA RANGE] Registered {def.LogLabel} training range mission.");
            }
        }

        private static void UnregisterOne(string fuldaFlexAssetName)
        {
            RegisteredRangeMission entry = Registered.FirstOrDefault(r => r?.Definition != null && string.Equals(r.Definition.FuldaFlexAssetName, fuldaFlexAssetName, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                return;

            AllMissionsMetaDataScriptable catalog = Resources.FindObjectsOfTypeAll<AllMissionsMetaDataScriptable>().FirstOrDefault();

            if (catalog?.Theaters != null && entry.Mission != null)
            {
                foreach (MissionTheaterScriptable theater in catalog.Theaters)
                {
                    if (theater?.Missions == null)
                        continue;

                    if (theater.Missions.All(m => !ReferenceEquals(m, entry.Mission)))
                        continue;

                    theater.Missions = theater.Missions.Where(m => !ReferenceEquals(m, entry.Mission)).ToArray();
                }
            }

            if (entry.Flex != null)
                UnityEngine.Object.Destroy(entry.Flex);

            Registered.Remove(entry);
        }

        private static void RefreshMutableFields(RegisteredRangeMission entry)
        {
            if (entry?.Mission == null || entry.Definition == null)
                return;

            FuldaTrainingRangeMissionDefinition def = entry.Definition;

            entry.Mission.MissionName = def.MissionTitle;
            entry.Mission.FactionInfo = CloneFactionInfoWithDebrief(entry.Mission.FactionInfo, def);

            if (entry.Flex != null)
                ApplyFlexUnitSwaps(entry.Flex, def);
        }

        public static void InjectFlexMetadataBeforeSetup(DynamicMissionComposer composer)
        {
            MissionDataState missionState = UnityEngine.Object.FindObjectOfType<MissionDataState>();
            MissionMetaData meta = missionState?.MetaData;
            FuldaTrainingRangeMissionDefinition def = ResolveDefinition(meta);
            if (def == null)
                return;

            RegisteredRangeMission entry = Registered.FirstOrDefault(r => r?.Definition != null && string.Equals(r.Definition.FuldaFlexAssetName, def.FuldaFlexAssetName, StringComparison.OrdinalIgnoreCase));

            DynamicMissionMetadataScriptable flex = meta?.FlexMissionData ?? entry?.Flex;
            if (flex == null)
                return;

            ApplyFlexUnitSwaps(flex, def);

            MissionSceneMeta sceneMeta = composer?.MissionSceneMeta ?? UnityEngine.Object.FindObjectOfType<MissionSceneMeta>();

            if (sceneMeta == null)
                return;

            sceneMeta.DynamicMetadata = flex;

            PatchSceneVictoryMessages(def);
        }

        private static void PatchSceneVictoryMessages(FuldaTrainingRangeMissionDefinition def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.BlueVictoryMessage))
                return;

            MissionDataState state = UnityEngine.Object.FindObjectOfType<MissionDataState>();
            if (ResolveDefinition(state?.MetaData) != def)
                return;

            string message = def.BlueVictoryMessage;

            MissionResultAction[] actions = UnityEngine.Object.FindObjectsByType<MissionResultAction>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (MissionResultAction action in actions)
            {
                if (action == null || !action.Win)
                    continue;

                if (action.Faction != def.VictoryFaction)
                    continue;

                action.CustomMessage = message;
            }
        }

        private static MissionMetaData FindTemplateRangeMission(MissionTheaterScriptable theater, FuldaTrainingRangeMissionDefinition def)
        {
            if (theater.Missions == null || def == null)
                return null;

            string sourceFlexAssetName = def.ResolveSourceFlexAssetName();

            foreach (MissionMetaData mission in theater.Missions)
            {
                if (mission?.FlexMissionData == null)
                    continue;

                if (string.Equals(mission.FlexMissionData.name, sourceFlexAssetName, StringComparison.OrdinalIgnoreCase))
                    return mission;

                if (mission.MissionName == null || mission.MissionName.IndexOf("Range", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (string.Equals(sourceFlexAssetName, DefaultLeo2SourceFlexAssetName, StringComparison.OrdinalIgnoreCase)
                    && mission.MissionName.IndexOf("Leopard 2", StringComparison.OrdinalIgnoreCase) >= 0)
                    return mission;

                if (sourceFlexAssetName.IndexOf("T80", StringComparison.OrdinalIgnoreCase) >= 0
                    && mission.MissionName.IndexOf("T-80", StringComparison.OrdinalIgnoreCase) >= 0)
                    return mission;
            }

            DynamicMissionMetadataScriptable flex = Resources
                .FindObjectsOfTypeAll<DynamicMissionMetadataScriptable>()
                .FirstOrDefault(f => f != null && string.Equals(f.name, sourceFlexAssetName, StringComparison.OrdinalIgnoreCase));

            if (flex == null)
                return null;

            return theater.Missions.FirstOrDefault(m => m != null && ReferenceEquals(m.FlexMissionData, flex));
        }

        private static MissionMetaData CloneMissionMeta(MissionMetaData source, DynamicMissionMetadataScriptable flexClone, FuldaTrainingRangeMissionDefinition def)
        {
            return new MissionMetaData
            {
                MissionName = def.MissionTitle,
                MissionSceneReference = source.MissionSceneReference,
                FactionInfo = CloneFactionInfoWithDebrief(source.FactionInfo, def),
                DefaultTime = source.DefaultTime,
                TimeOptions = source.TimeOptions,
                CloudBias = source.CloudBias,
                FlexMissionData = flexClone,
                NoCustomization = source.NoCustomization,
                ForceCustomization = source.ForceCustomization,
            };
        }

        private static List<FactionMissionInfo> CloneFactionInfoWithDebrief(List<FactionMissionInfo> source, FuldaTrainingRangeMissionDefinition def)
        {
            if (source == null || source.Count == 0)
                return source;

            string debrief = def.Debrief;
            var clone = new List<FactionMissionInfo>(source.Count);

            foreach (FactionMissionInfo info in source)
            {
                if (info == null)
                    continue;

                clone.Add(new FactionMissionInfo(info.Allegiance, debrief));
            }

            return clone;
        }

        private static void ApplyFlexUnitSwaps(DynamicMissionMetadataScriptable flex, FuldaTrainingRangeMissionDefinition def)
        {
            if (flex?.MissionData == null || def == null)
                return;

            ReplacePlayerVehicleKeys(flex.MissionData.FriendlySpawnInfo, def);
            ApplyEnemyKeySwaps(flex.MissionData.EnemySpawnInfo, def);
        }

        private static void ReplacePlayerVehicleKeys(FactionSpawnInfo faction, FuldaTrainingRangeMissionDefinition def)
        {
            if (faction?.SpawnOrders == null)
                return;

            foreach (FactionSpawnInfo.SpawnOrder order in faction.SpawnOrders)
            {
                if (order == null || string.IsNullOrEmpty(order.Key))
                    continue;

                if (!IsPlayerSourceKey(order.Key, def))
                    continue;

                order.Key = def.PlayerUnitKey;
                order.BlockCustomization = false;
            }
        }

        private static bool IsPlayerSourceKey(string key, FuldaTrainingRangeMissionDefinition def)
        {
            if (def?.PlayerVehicleSourceKeys == null)
                return false;

            foreach (string source in def.PlayerVehicleSourceKeys)
            {
                if (string.IsNullOrEmpty(source))
                    continue;

                if (string.Equals(key, source, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void ApplyEnemyKeySwaps(FactionSpawnInfo faction, FuldaTrainingRangeMissionDefinition def)
        {
            if (faction?.SpawnOrders == null)
                return;

            (string From, string To)[] swaps = def.EnemyUnitSwaps;
            if (swaps == null || swaps.Length == 0)
                return;

            foreach (FactionSpawnInfo.SpawnOrder order in faction.SpawnOrders)
            {
                if (order == null || string.IsNullOrEmpty(order.Key))
                    continue;

                string key = order.Key.Trim();

                foreach ((string from, string to) in swaps)
                {
                    if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                        continue;

                    if (!string.Equals(key, from.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    order.Key = to.Trim();
                    break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MissionBriefMenu), "LoadMap")]
    public static class FuldaTrainingRangeLoadMapPatch
    {
        public static void Prefix(MissionBriefMenu __instance)
        {
            FuldaTrainingRangeMissions.ApplyLaunchTitleForEndScreen(__instance);
        }
    }

    [HarmonyPatch(typeof(ScenarioResultManager), "FinalResolveMission")]
    public static class FuldaTrainingRangeEndScreenPatch
    {
        public static void Postfix(ScenarioResultManager __instance)
        {
            FuldaTrainingRangeMissions.ApplyEndScreenResultsTitle(__instance);
        }
    }

    [HarmonyPatch(typeof(DynamicMissionComposer), "SetupFlexMission")]
    public static class FuldaTrainingRangeFlexSetupPatch
    {
        public static void Prefix(DynamicMissionComposer __instance)
        {
            FuldaTrainingRangeMissions.InjectFlexMetadataBeforeSetup(__instance);
        }
    }

    [HarmonyPatch(typeof(MissionBriefMenu), "GetMissionMetaData", typeof(string), typeof(string))]
    public static class FuldaTrainingRangeMissionMetaPatch
    {
        public static void Postfix(string theaterKey, string resourceKey, ref MissionMetaData __result)
        {
            FuldaTrainingRangeMissions.PreferMenuSelectedMission(theaterKey, resourceKey, ref __result);
        }
    }

    [HarmonyPatch(typeof(MissionBriefMenu), "LoadMissionBriefing", typeof(SceneMissionKey))]
    public static class FuldaTrainingRangeLoadBriefingPatch
    {
        public static void Postfix(MissionBriefMenu __instance)
        {
            FuldaTrainingRangeMissions.ApplyBriefingOverride(__instance);
        }
    }

    [HarmonyPatch(typeof(MissionBriefMenu), "UpdateBriefingData", typeof(bool))]
    public static class FuldaTrainingRangeBriefingPatch
    {
        public static void Postfix(MissionBriefMenu __instance)
        {
            FuldaTrainingRangeMissions.ApplyBriefingOverride(__instance);
        }
    }
}
