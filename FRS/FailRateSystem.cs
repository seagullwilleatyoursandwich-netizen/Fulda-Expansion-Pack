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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public static MelonPreferences_Entry<bool> LogSwaps;
        public static MelonPreferences_Entry<bool> CascadeEnabled;

        const int CascadeSafetyHopCap = 32;
        const string ShowcaseSceneName = "TR01_showcase";

        public static bool IsEnabled => Enabled != null && Enabled.Value;

        public static void Config(MelonPreferences_Category category)
        {
            Enabled = category.GetEntry<bool>("FRS | Master switch") ?? category.CreateEntry<bool>("FRS | Master switch", true);
            Enabled.Comment = "When true, standalone vehicles listed in the Fail-Rate catalog may spawn as a different vehicle (downgrades / upgrades).";

            LogSwaps = category.GetEntry<bool>("FRS | Log spawn swaps") ?? category.CreateEntry<bool>("FRS | Log spawn swaps", false);
            LogSwaps.Comment = "Writes a MelonLoader message when FRS changes a unit prefab at spawn.";

            CascadeEnabled = category.GetEntry<bool>("FRS | Cascade");
            if (CascadeEnabled == null)
            {
                MelonPreferences_Entry<int> legacyDepth = category.GetEntry<int>("FRS | Cascade depth");
                bool defaultOn = legacyDepth == null || legacyDepth.Value > 0;
                CascadeEnabled = category.CreateEntry<bool>("FRS | Cascade", defaultOn);
            }

            CascadeEnabled.Comment = "After a swap, roll again on the new vehicle type (e.g. NPzK-140 -> 2A4 '88 -> 2A4 '86). Platoon guarantees only apply on the first hop. Off = one roll per spawn.";

            FailRateCatalog.Config(category);
            // MelonLogger.Msg($"[FRS] MelonPreferences: {FailRateCatalog.AllRules.Count} vehicle rule(s) registered under \"{category?.Identifier}\".");
        }

        public static void ResetForNewMission()
        {
            FailRatePlatoonState.Reset();
        }

        public static string ResolveSpawnUniqueName(string requestedUniqueName, UnitMetaData metaData)
        {
            if (!IsEnabled || string.IsNullOrEmpty(requestedUniqueName))
                return requestedUniqueName;

            if (IsShowcaseScene())
                return requestedUniqueName;

            if (FuldaTrainingRangeMissions.IsActiveFuldaTrainingMission())
                return requestedUniqueName;

            if (FailRatePlatoonState.IsLoneVehicleSpawn)
            {
                if (LogSwaps != null && LogSwaps.Value && FailRateCatalog.FindRule(requestedUniqueName) != null)
                {
                    int platoonSize = FailRatePlatoonState.GetKnownPlatoonMemberCount();
                    string reason = platoonSize == 0 ? "no platoon" : "1-vehicle platoon";
                    // MelonLogger.Msg($"[FRS] lone spawn ({reason}): {requestedUniqueName} (FRS skipped)");
                }

                return requestedUniqueName;
            }

            bool cascade = CascadeEnabled != null && CascadeEnabled.Value;
            int maxHop = cascade ? CascadeSafetyHopCap : 0;
            string current = requestedUniqueName;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { current };

            for (int hop = 0; hop <= maxHop; hop++)
            {
                bool allowPlatoonGuarantees = hop == 0;
                if (!TryApplyFailRateHop(ref current, metaData, hop, allowPlatoonGuarantees, out bool swapped))
                    break;

                if (!swapped)
                    break;

                if (visited.Contains(current))
                    break;

                visited.Add(current);
            }

            return current;
        }

        static bool TryApplyFailRateHop(ref string uniqueName, UnitMetaData metaData, int hopIndex, bool allowPlatoonGuarantees, out bool swapped)
        {
            swapped = false;

            FailRateVehicleRule rule = FailRateCatalog.FindRule(uniqueName);
            if (rule == null || rule.AlternateUniqueNames == null || rule.AlternateUniqueNames.Length == 0)
                return false;

            if (allowPlatoonGuarantees && FailRatePlatoonState.TryConsumeGuaranteedOriginal(uniqueName, metaData, rule.CurrentGuaranteedOriginalsPerPlatoon))
            {
                if (LogSwaps != null && LogSwaps.Value)
                {
                    bool inPlatoon = FailRatePlatoonState.InsideComposerSpawnScope && FailRatePlatoonState.ActivePlatoonInstanceId.HasValue;
                    string role = !inPlatoon ? "lone spawn" : FailRatePlatoonState.CurrentSpawnIsPlatoonLeaderSlot ? "platoon leader" : "platoon wingman";
                    string hopLabel = hopIndex > 0 ? $"cascade hop {hopIndex + 1}, " : string.Empty;
                    MelonLogger.Msg($"[FRS] guaranteed original ({hopLabel}): {uniqueName} ({role})");
                }

                return true;
            }

            int chancePercent = rule.CurrentChancePercent;
            if (chancePercent <= 0 || UnityEngine.Random.Range(0, 100) >= chancePercent)
                return false;

            string[] valid = rule.AlternateUniqueNames.Where(alt => !string.IsNullOrEmpty(alt) && PrefabExists(alt)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            if (valid.Length == 0)
                return false;

            string picked = valid[UnityEngine.Random.Range(0, valid.Length)];

            if (string.Equals(picked, uniqueName, StringComparison.OrdinalIgnoreCase))
                return false;

            if (LogSwaps != null && LogSwaps.Value)
            {
                string direction = rule.Mode == FailRateMode.Upgrade ? "upgrade" : "downgrade";
                string hopLabel = hopIndex > 0 ? $"cascade hop {hopIndex + 1}: " : string.Empty;
                MelonLogger.Msg($"[FRS] {hopLabel}{direction}: {uniqueName} → {picked} ({chancePercent}% roll)");
            }

            uniqueName = picked;
            swapped = true;
            return true;
        }

        static bool IsShowcaseScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            return scene.IsValid() && string.Equals(scene.name, ShowcaseSceneName, StringComparison.OrdinalIgnoreCase);
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

            PlatoonData platoonData = ResolvePlatoonDataForSpawnNode(node);
            FailRatePlatoonState.BindPlatoonSpawn(platoonData, allegiance);
        }

        public static void Postfix(ref PlatoonScopeState __state)
        {
            FailRatePlatoonState.ActivePlatoonInstanceId = __state.PreviousPlatoonInstanceId;
            FailRatePlatoonState.CurrentSpawnIsPlatoonLeaderSlot = __state.PreviousSpawnIsPlatoonLeaderSlot;
            FailRatePlatoonState.InsideComposerSpawnScope = __state.PreviousInsideComposerSpawnScope;
            FailRatePlatoonState.RestoreScopedPlatoonBucketKey(__state.PreviousScopedPlatoonBucketKey);
        }

        static PlatoonData ResolvePlatoonDataForSpawnNode(Transform node)
        {
            if (node == null)
                return null;

            PlatoonData[] onNode = node.GetComponents<PlatoonData>();
            if (onNode != null && onNode.Length > 0)
                return onNode[onNode.Length - 1];

            return node.GetComponentInParent<PlatoonData>();
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

        public static void Postfix()
        {
            FailRatePlatoonState.EndPlatoonSpawn();
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
