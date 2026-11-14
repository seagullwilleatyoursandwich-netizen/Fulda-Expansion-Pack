using GHPC;
using GHPC.AI.Platoons;
using GHPC.Mission.Data;
using System;
using System.Collections.Generic;

namespace Fulda1989.FailRate
{
    // Tracks how many guaranteed-original spawns remain per bucket + FRS anchor for the current mission.
    internal static class FailRatePlatoonState
    {
        // key = spawn bucket + anchor unique name → count of originals already reserved
        static readonly Dictionary<string, int> GuaranteedIssuedByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Platoon bucket → leader (index 0) has finished spawning; wingmen may take remaining guarantees.
        static readonly Dictionary<string, bool> PlatoonLeaderSpawnCommittedByBucket = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        // GHPC spawns platoon members in formation order; index 0 becomes PlatoonLeader (logging only).
        static readonly Dictionary<int, int> PlatoonSpawnCounters = new Dictionary<int, int>();

        // Set while DynamicMissionComposer spawns platoon members (PlatoonData on the spawn transform).
        internal static int? ActivePlatoonInstanceId;

        // True only inside SpawnUnitAtTransform prefix/postfix.
        internal static bool InsideComposerSpawnScope;

        internal static bool CurrentSpawnIsPlatoonLeaderSlot = true;

        // unitKeys.Length from DynamicMissionComposer.SpawnPlatoon (set before member spawns).
        static int PendingPlatoonSpawnSize;
        static readonly Dictionary<int, int> ExpectedMembersByPlatoonId = new Dictionary<int, int>();

        // True when this spawn is the only vehicle in its spawn scope (see GetExpectedPlatoonSize).
        internal static bool IsLoneVehicleSpawn
        {
            get
            {
                if (!ActivePlatoonInstanceId.HasValue)
                    return true;

                return GetExpectedPlatoonSize() <= 1;
            }
        }

        // Not in a platoon → 0; 1-vehicle platoon → 1; multi-vehicle platoon → N.
        internal static int GetExpectedPlatoonSize()
        {
            if (!ActivePlatoonInstanceId.HasValue)
                return 0;

            if (ExpectedMembersByPlatoonId.TryGetValue(ActivePlatoonInstanceId.Value, out int expected))
                return expected;

            if (PendingPlatoonSpawnSize > 0)
                return PendingPlatoonSpawnSize;

            return int.MaxValue;
        }

        public static void BeginPlatoonSpawn(string[] unitKeys)
        {
            PendingPlatoonSpawnSize = unitKeys != null ? unitKeys.Length : 0;
        }

        public static void RememberPlatoonSpawnSize(int platoonInstanceId)
        {
            if (PendingPlatoonSpawnSize <= 0)
                return;

            ExpectedMembersByPlatoonId[platoonInstanceId] = PendingPlatoonSpawnSize;
        }

        public static void BindPlatoonSpawn(PlatoonData platoonData, Faction allegiance)
        {
            if (platoonData == null)
            {
                ActivePlatoonInstanceId = null;
                CurrentSpawnIsPlatoonLeaderSlot = true;
                ClearScopedPlatoonBucketKey();
                return;
            }

            int platoonId = ((UnityEngine.Object)platoonData).GetInstanceID();
            ActivePlatoonInstanceId = platoonId;
            RememberPlatoonSpawnSize(platoonId);
            SetScopedPlatoonBucketKey(allegiance, platoonId);
        }

        // One bucket per platoon, or one per lone SpawnUnit call.
        static string ScopedPlatoonBucketKey;
        static string ActiveSpawnBucketKey;
        static int SoloSpawnSerial;

        public static void Reset()
        {
            GuaranteedIssuedByKey.Clear();
            PlatoonLeaderSpawnCommittedByBucket.Clear();
            PlatoonSpawnCounters.Clear();
            ExpectedMembersByPlatoonId.Clear();
            PendingPlatoonSpawnSize = 0;
            ActivePlatoonInstanceId = null;
            InsideComposerSpawnScope = false;
            CurrentSpawnIsPlatoonLeaderSlot = true;
            ScopedPlatoonBucketKey = null;
            ActiveSpawnBucketKey = null;
            SoloSpawnSerial = 0;
        }

        public static void SetScopedPlatoonBucketKey(Faction allegiance, int platoonInstanceId)
        {
            ScopedPlatoonBucketKey = $"plt:{platoonInstanceId}:{allegiance}";
        }

        public static void ClearScopedPlatoonBucketKey()
        {
            ScopedPlatoonBucketKey = null;
        }

        internal static string ScopedPlatoonBucketKeySnapshot => ScopedPlatoonBucketKey;

        public static void RestoreScopedPlatoonBucketKey(string key)
        {
            ScopedPlatoonBucketKey = key;
        }

        public static void BeginSpawnBucket(UnitMetaData metaData)
        {
            if (!string.IsNullOrEmpty(ScopedPlatoonBucketKey))
            {
                ActiveSpawnBucketKey = ScopedPlatoonBucketKey;
                return;
            }

            SoloSpawnSerial++;
            Faction allegiance = metaData != null ? metaData.Allegiance : Faction.Neutral;
            ActiveSpawnBucketKey = $"solo:{allegiance}:s{SoloSpawnSerial}";
        }

        public static void EndSpawnBucket()
        {
            ActiveSpawnBucketKey = null;
        }

        internal static string ActiveSpawnBucketKeyForCommit => ActiveSpawnBucketKey;

        public static void PreparePlatoonMemberSpawn(int platoonInstanceId)
        {
            if (!PlatoonSpawnCounters.TryGetValue(platoonInstanceId, out int index))
                index = 0;

            CurrentSpawnIsPlatoonLeaderSlot = index == 0;
        }

        public static void CommitPlatoonMemberSpawn(int platoonInstanceId, string spawnBucketKey)
        {
            if (!PlatoonSpawnCounters.TryGetValue(platoonInstanceId, out int index))
                index = 0;

            if (index == 0 && !string.IsNullOrEmpty(spawnBucketKey))
                PlatoonLeaderSpawnCommittedByBucket[spawnBucketKey] = true;

            PlatoonSpawnCounters[platoonInstanceId] = index + 1;
        }

        public static bool TryConsumeGuaranteedOriginal(string anchorUniqueName, UnitMetaData metaData, int guaranteedPerPlatoon)
        {
            if (guaranteedPerPlatoon <= 0)
                return false;

            string bucketKey = BuildSpawnBucketKey(metaData);
            string stateKey = bucketKey + "|" + anchorUniqueName;

            GuaranteedIssuedByKey.TryGetValue(stateKey, out int issued);
            if (issued >= guaranteedPerPlatoon)
                return false;

            bool multiVehiclePlatoon = ActivePlatoonInstanceId.HasValue && GetExpectedPlatoonSize() > 1;
            if (multiVehiclePlatoon && guaranteedPerPlatoon >= 1 && issued == 0 && !CurrentSpawnIsPlatoonLeaderSlot)
            {
                // First guarantee slot is leader-only; wingmen wait until that spawn has finished.
                if (!PlatoonLeaderSpawnCommittedByBucket.TryGetValue(bucketKey, out bool leaderDone) || !leaderDone)
                    return false;
            }

            GuaranteedIssuedByKey[stateKey] = issued + 1;
            return true;
        }

        static string BuildSpawnBucketKey(UnitMetaData metaData)
        {
            if (!string.IsNullOrEmpty(ActiveSpawnBucketKey))
                return ActiveSpawnBucketKey;

            if (metaData == null)
                return "unknown";

            if (!string.IsNullOrEmpty(ScopedPlatoonBucketKey))
                return ScopedPlatoonBucketKey;

            if (InsideComposerSpawnScope && ActivePlatoonInstanceId.HasValue)
                return $"plt:{ActivePlatoonInstanceId.Value}:{metaData.Allegiance}";

            if (metaData.InstanceId > 0)
                return $"solo:{metaData.Allegiance}:id{metaData.InstanceId}";

            SoloSpawnSerial++;
            return $"solo:{metaData.Allegiance}:s{SoloSpawnSerial}";
        }
    }
}