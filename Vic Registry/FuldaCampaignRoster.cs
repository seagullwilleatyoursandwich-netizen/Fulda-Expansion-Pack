using GHPC;
using GHPC.Mission;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fulda1989
{
    // Appends or removes Fulda vehicles in GHPC theater campaign rosters (TheaterMetaScriptable), the keys should match customizer unique names (same as FuldaStandaloneRegistrations)
    public static class FuldaCampaignRoster
    {
        public enum CampaignFaction
        {
            // Factions[0] on AllTheaters[0] — USA roster in the default Fulda theater.
            Usa = 0,
            Ussr = 1,
            Nva = 2,
        }

        private sealed class Entry
        {
            public string Key;
            public string FriendlyName;
            public Func<bool> IsEnabled;
            public CampaignFaction Faction = CampaignFaction.Usa;
            public UnitType Type = UnitType.GroundVehicle;
            public UnitClass Class = UnitClass.Tank;
            public EffectivenessTier Tier = EffectivenessTier.FirstLine;
            public float SpawnRatio = 1f;
            public int BlockUntilMissionsCompleted;
            public float Prevalence = 0.5f;
            // Existing theater unit whose LogisticsModifierCurve is copied (e.g. M1, M1IP).
            public string LogisticsDonorKey;
        }

        private static readonly Entry[] Catalog =
        {
            new Entry
            {
                Key = FuldaStandaloneRegistrations.Leo2K,
                FriendlyName = "Leopard 2K",
                IsEnabled = () => LEO2K.PatchEnabled,
                SpawnRatio = 1.25f,
                BlockUntilMissionsCompleted = 0,
                Prevalence = 0.9f,
                LogisticsDonorKey = "M1",
            },
            new Entry
            {
                Key = FuldaStandaloneRegistrations.Leo2A4,
                FriendlyName = "Leopard 2A4 (1988)",
                IsEnabled = () => LEO2A4.PatchEnabled,
                SpawnRatio = 0.9f,
                BlockUntilMissionsCompleted = 1,
                Prevalence = 0.65f,
                LogisticsDonorKey = "M1IP",
            },
            new Entry
            {
                Key = FuldaStandaloneRegistrations.Leo2A4_140,
                FriendlyName = "NPzK-140 (1991)",
                IsEnabled = () => LEO2A4_140.PatchEnabled,
                SpawnRatio = 0.25f,
                BlockUntilMissionsCompleted = 2,
                Prevalence = 0.45f,
                LogisticsDonorKey = "M1IP",
            },
            new Entry
            {
                Key = FuldaStandaloneRegistrations.M60A1RiseEra,
                FriendlyName = "M60A1 RISE(P) 1991",
                IsEnabled = () => M60ERA.PatchEnabled,
                SpawnRatio = 1f,
                BlockUntilMissionsCompleted = 0,
                Prevalence = 1.1f,
                LogisticsDonorKey = "M1IP",
            },
            new Entry
            {
                Key = FuldaStandaloneRegistrations.M60A1RiseEra,
                FriendlyName = "M60A3 TTS 1991",
                IsEnabled = () => M60ERA.PatchEnabled,
                SpawnRatio = 0.8f,
                BlockUntilMissionsCompleted = 1,
                Prevalence = 1f,
                LogisticsDonorKey = "M1IP",
            }
        };

        public static void StartApply()
        {
            MelonCoroutines.Start(ApplyRoutine());
        }

        private static IEnumerator ApplyRoutine()
        {
            TheaterMetaScriptable theaterMeta = null;

            while (theaterMeta == null)
            {
                theaterMeta = Resources.FindObjectsOfTypeAll<TheaterMetaScriptable>().FirstOrDefault();

                if (theaterMeta == null)
                    yield return null;
            }

            if (theaterMeta.AllTheaters == null || theaterMeta.AllTheaters.Length == 0)
            {
                MelonLogger.Error("[VIC CAMPAIGN] No theater data exists.");
                yield break;
            }

            var byFaction = Catalog.GroupBy(e => e.Faction);

            foreach (var group in byFaction)
            {
                TheaterFactionMetadata faction = ResolveFaction(theaterMeta, group.Key);

                if (faction == null || faction.AllUnits == null)
                {
                    MelonLogger.Error("[VIC CAMPAIGN] Faction or AllUnits is null for " + group.Key + ".");
                    continue;
                }

                ApplyToFaction(faction, group.ToArray());
            }

            yield break;
        }

        private static TheaterFactionMetadata ResolveFaction(TheaterMetaScriptable theaterMeta, CampaignFaction faction)
        {
            if (theaterMeta.AllTheaters == null || theaterMeta.AllTheaters.Length == 0)
                return null;

            TheaterMetadata theater = theaterMeta.AllTheaters[0];

            if (theater.Factions == null || theater.Factions.Length == 0)
                return null;

            int index = (int)faction;

            if (index < 0 || index >= theater.Factions.Length)
                return null;

            return theater.Factions[index];
        }

        private static void ApplyToFaction(TheaterFactionMetadata faction, Entry[] entries)
        {
            HashSet<string> managedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Entry entry in entries)
                managedKeys.Add(entry.Key);

            List<TheaterUnitMetadata> units = faction.AllUnits.ToList();

            units.RemoveAll(meta =>
            {
                if (meta == null || string.IsNullOrEmpty(meta.Key))
                    return false;

                if (!managedKeys.Contains(meta.Key))
                    return false;

                Entry entry = FindEntry(meta.Key);
                return entry != null && !entry.IsEnabled();
            });

            foreach (Entry entry in entries)
            {
                if (!entry.IsEnabled())
                    continue;

                if (units.Any(meta => meta != null && string.Equals(meta.Key, entry.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                TheaterUnitMetadata donor = units.FirstOrDefault(meta =>
                    meta != null && string.Equals(meta.Key, entry.LogisticsDonorKey, StringComparison.OrdinalIgnoreCase));

                if (donor == null)
                {
                    MelonLogger.Warning("[VIC CAMPAIGN] Skipped " + entry.Key + ": logistics donor " + entry.LogisticsDonorKey + " not in roster.");
                    continue;
                }

                units.Add(new TheaterUnitMetadata
                {
                    Key = entry.Key,
                    FriendlyName = entry.FriendlyName,
                    Type = entry.Type,
                    Class = entry.Class,
                    Tier = entry.Tier,
                    SpawnRatio = entry.SpawnRatio,
                    BlockUntilMissionsCompleted = entry.BlockUntilMissionsCompleted,
                    Prevalence = entry.Prevalence,
                    LogisticsModifierCurve = donor.LogisticsModifierCurve,
                });
            }

            faction.AllUnits = units.ToArray();
        }

        private static Entry FindEntry(string key)
        {
            return Catalog.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
        }
    }
}
