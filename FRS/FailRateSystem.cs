using Fulda1989.Missions;
using GHPC;
using GHPC.AI;
using GHPC.AI.Platoons;
using GHPC.Mission;
using GHPC.Mission.Data;
using GHPC.State;
using GHPC.Vehicle;
using HarmonyLib;
using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Fulda1989.FailRate
{
    public enum FailRateMode
    {
        Downgrade,
        Upgrade,
    }

    public static class FailRateSystem
    {
        public static MelonPreferences_Entry<bool> Enabled;
        public static MelonPreferences_Entry<bool> SkipNeutralSpawns;
        public static MelonPreferences_Entry<bool> LogSwaps;

        public static bool IsEnabled => Enabled != null && Enabled.Value;

        public static void Config(MelonPreferences_Category category)
        {
            Enabled = category.GetEntry<bool>("FRS | Master switch") ?? category.CreateEntry<bool>("FRS | Master switch", true);
            Enabled.Comment = "When true, standalone vehicles listed in the Fail-Rate catalog may spawn as a different vehicle (downgrades / upgrades).";

            SkipNeutralSpawns = category.GetEntry<bool>("FRS | Skip neutral / showcase spawns") ?? category.CreateEntry<bool>("FRS | Skip neutral / showcase spawns", true);
            SkipNeutralSpawns.Comment = "Keeps training-range and other Neutral showcase spawns exact (no FRS roll).";

            LogSwaps = category.GetEntry<bool>("FRS | Log spawn swaps") ?? category.CreateEntry<bool>("FRS | Log spawn swaps", false);
            LogSwaps.Comment = "Writes a MelonLoader message when FRS changes a unit prefab at spawn.";

            FailRateCatalog.Config(category);
            MelonLogger.Msg($"[FRS] MelonPreferences: {FailRateCatalog.AllRules.Count} vehicle rule(s) registered under \"{category?.Identifier}\".");
        }

        public static void ResetForNewMission()
        {
            FailRatePlatoonState.Reset();
        }

        public static string ResolveSpawnUniqueName(string requestedUniqueName, UnitMetaData metaData)
        {
            if (!IsEnabled || string.IsNullOrEmpty(requestedUniqueName))
                return requestedUniqueName;

            if (SkipNeutralSpawns != null && SkipNeutralSpawns.Value && metaData != null && metaData.Allegiance == Faction.Neutral)
                return requestedUniqueName;

            if (FuldaTrainingRangeMissions.IsActiveFuldaTrainingMission())
                return requestedUniqueName;

            if (FailRatePlatoonState.IsLoneVehicleSpawn)
            {
                if (LogSwaps != null && LogSwaps.Value && FailRateCatalog.FindRule(requestedUniqueName) != null)
                {
                    int platoonSize = FailRatePlatoonState.GetExpectedPlatoonSize();
                    string reason = platoonSize == 0 ? "no platoon" : "1-vehicle platoon";
                    MelonLogger.Msg($"[FRS] lone spawn ({reason}): {requestedUniqueName} (FRS skipped)");
                }

                return requestedUniqueName;
            }

            FailRateVehicleRule rule = FailRateCatalog.FindRule(requestedUniqueName);
            if (rule == null || rule.AlternateUniqueNames == null || rule.AlternateUniqueNames.Length == 0)
                return requestedUniqueName;

            if (FailRatePlatoonState.TryConsumeGuaranteedOriginal(requestedUniqueName, metaData, rule.CurrentGuaranteedOriginalsPerPlatoon))
            {
                if (LogSwaps != null && LogSwaps.Value)
                {
                    bool inPlatoon = FailRatePlatoonState.InsideComposerSpawnScope && FailRatePlatoonState.ActivePlatoonInstanceId.HasValue;
                    string role = !inPlatoon ? "lone spawn" : FailRatePlatoonState.CurrentSpawnIsPlatoonLeaderSlot ? "platoon leader" : "platoon wingman";
                    MelonLogger.Msg($"[FRS] guaranteed original: {requestedUniqueName} ({role})");
                }

                return requestedUniqueName;
            }

            int chancePercent = rule.CurrentChancePercent;
            if (chancePercent <= 0 || UnityEngine.Random.Range(0, 100) >= chancePercent)
                return requestedUniqueName;

            string[] valid = rule.AlternateUniqueNames.Where(alt => !string.IsNullOrEmpty(alt) && PrefabExists(alt)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            if (valid.Length == 0)
                return requestedUniqueName;

            string picked = valid[UnityEngine.Random.Range(0, valid.Length)];

            if (string.Equals(picked, requestedUniqueName, StringComparison.OrdinalIgnoreCase))
                return requestedUniqueName;

            if (LogSwaps != null && LogSwaps.Value)
            {
                string direction = rule.Mode == FailRateMode.Upgrade ? "upgrade" : "downgrade";
                MelonLogger.Msg($"[FRS] {direction}: {requestedUniqueName} → {picked} ({chancePercent}% roll)");
            }

            return picked;
        }

        static bool PrefabExists(string uniqueName)
        {
            UnitSpawner spawner = UnitSpawner.Instance;
            if (spawner == null)
                spawner = UnityEngine.Object.FindObjectOfType<UnitSpawner>();

            if (spawner == null)
                return false;

            return spawner.GetPrefabByUniqueName(uniqueName) != null;
        }
    }

    [HarmonyPatch(typeof(UnitSpawner), nameof(UnitSpawner.SpawnUnit), new Type[] { typeof(string), typeof(UnitMetaData), typeof(WaypointHolder), typeof(Transform) })]
    public static class FailRateSpawnPatch
    {
        public static void Prefix(ref string uniqueName, UnitMetaData metaData)
        {
            if (FailRatePlatoonState.ActivePlatoonInstanceId.HasValue)
                FailRatePlatoonState.PreparePlatoonMemberSpawn(FailRatePlatoonState.ActivePlatoonInstanceId.Value);

            FailRatePlatoonState.BeginSpawnBucket(metaData);
            uniqueName = FailRateSystem.ResolveSpawnUniqueName(uniqueName, metaData);
        }

        public static void Postfix(IUnit __result)
        {
            if (FailRatePlatoonState.ActivePlatoonInstanceId.HasValue)
                FailRatePlatoonState.CommitPlatoonMemberSpawn(
                    FailRatePlatoonState.ActivePlatoonInstanceId.Value,
                    FailRatePlatoonState.ActiveSpawnBucketKeyForCommit);

            FailRatePlatoonState.EndSpawnBucket();
        }
    }

    // Mission flex platoons: PlatoonData is on the spawn transform before each member is spawned through UnitSpawner.
    [HarmonyPatch(typeof(DynamicMissionComposer), "SpawnUnitAtTransform", typeof(string), typeof(Transform), typeof(Faction), typeof(bool))]
    public static class FailRateComposerSpawnUnitScopePatch
    {
        public struct PlatoonScopeState
        {
            public int? PreviousPlatoonInstanceId;
            public bool PreviousSpawnIsPlatoonLeaderSlot;
            public bool PreviousInsideComposerSpawnScope;
            public string PreviousScopedPlatoonBucketKey;
        }

        public static void Prefix(Transform node, Faction allegiance, ref PlatoonScopeState __state)
        {
            __state.PreviousPlatoonInstanceId = FailRatePlatoonState.ActivePlatoonInstanceId;
            __state.PreviousSpawnIsPlatoonLeaderSlot = FailRatePlatoonState.CurrentSpawnIsPlatoonLeaderSlot;
            __state.PreviousInsideComposerSpawnScope = FailRatePlatoonState.InsideComposerSpawnScope;
            __state.PreviousScopedPlatoonBucketKey = FailRatePlatoonState.ScopedPlatoonBucketKeySnapshot;
            FailRatePlatoonState.InsideComposerSpawnScope = true;

            if (node == null)
                return;

            PlatoonData platoonData = node.GetComponent<PlatoonData>() ?? node.GetComponentInParent<PlatoonData>();
            FailRatePlatoonState.BindPlatoonSpawn(platoonData, allegiance);
        }

        public static void Postfix(ref PlatoonScopeState __state)
        {
            FailRatePlatoonState.ActivePlatoonInstanceId = __state.PreviousPlatoonInstanceId;
            FailRatePlatoonState.CurrentSpawnIsPlatoonLeaderSlot = __state.PreviousSpawnIsPlatoonLeaderSlot;
            FailRatePlatoonState.InsideComposerSpawnScope = __state.PreviousInsideComposerSpawnScope;
            FailRatePlatoonState.RestoreScopedPlatoonBucketKey(__state.PreviousScopedPlatoonBucketKey);
        }
    }

    [HarmonyPatch(typeof(DynamicMissionComposer), "SpawnPlatoon", new Type[]
    {
        typeof(VehicleSpawnPoint),
        typeof(string[]),
        typeof(PlatoonDecalHelper.DecalLayoutPreset?[]),
        typeof(Faction),
        typeof(VehicleSpawnPoint),
        typeof(string),
    })]
    public static class FailRateSpawnPlatoonSizePatch
    {
        public static void Prefix(string[] unitKeys)
        {
            FailRatePlatoonState.BeginPlatoonSpawn(unitKeys);
        }
    }

    // MissionUnitsLoaded fires after flex spawns; reset must run before SetupFlexMission / spawn.
    [HarmonyPatch(typeof(DynamicMissionComposer), nameof(DynamicMissionComposer.SetupFlexMission))]
    public static class FailRateResetBeforeFlexSpawnPatch
    {
        public static void Prefix()
        {
            FailRateSystem.ResetForNewMission();
        }
    }
}
