using GHPC;
using GHPC.AI;
using GHPC.Mission;
using GHPC.Mission.Data;
using GHPC.Vehicle;
using HarmonyLib;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Fulda1989
{
    public static class FuldaStandaloneRegistrations
    {
        public const string Leo2K = "LEO2K";
        public const string Leo2A4 = "LEO2A4";
        public const string Leo2A4_140 = "LEO2A4_140";
        public const string T90 = "T90";
        public const string Obj640 = "Obj_640";
        public const string M60A1RiseEra = "M60A1RISEP_ERA";
        public const string M60A3TtsEra = "M60A3TTS_ERA";

        private sealed class Definition
        {
            public string SourceUniqueName;
            public string UniqueName;
            public string FriendlyName;
            public string LogLabel;
            // ^ When set, Army metadata is taken from this unit instead of the prefab source. 
            public string ArmySourceUniqueName;
            public bool UseEmptyBaseAmmoClips = true;
        }

        private static readonly Definition[] Catalog =
        {
            new Definition
            {
                SourceUniqueName = "LEO1A4",
                UniqueName = "LEO2K",
                FriendlyName = "Leopard 2K",
                LogLabel = "LEO2K CUSTOMIZER",
            },
            new Definition
            {
                SourceUniqueName = "LEO2A3",
                UniqueName = "LEO2A4",
                FriendlyName = "Leopard 2A4 (1988)",
                LogLabel = "LEO2A4 CUSTOMIZER",
            },
            new Definition
            {
                SourceUniqueName = "LEO2A3",
                UniqueName = "LEO2A4_140",
                FriendlyName = "NPzK-140 (1991)",
                LogLabel = "LEO2A4(140) CUSTOMIZER",
            },
            new Definition
            {
                SourceUniqueName = "T72M1",
                UniqueName = "T90",
                FriendlyName = "T-90 obr.1992",
                LogLabel = "T-90 CUSTOMIZER",
                ArmySourceUniqueName = "T80B",
            },
            new Definition
            {
                SourceUniqueName = "T80B",
                UniqueName = "Obj_640",
                FriendlyName = "Object 640 'Black Eagle'",
                LogLabel = "OBJECT 640 CUSTOMIZER",
            },
            new Definition
            {
                SourceUniqueName = "M60A1RISEP",
                UniqueName = "M60A1RISEP_ERA",
                FriendlyName = "M60A1 RISE(P) 1991",
                LogLabel = "M60A1 ERA CUSTOMIZER",
            },
            new Definition
            {
                SourceUniqueName = "M60A3TTS",
                UniqueName = "M60A3TTS_ERA",
                FriendlyName = "M60A3 TTS 1991",
                LogLabel = "M60A3 ERA CUSTOMIZER",
            },
        };

        private static readonly HashSet<string> Registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> GiveUpUntilNextTry = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _coroutineRunning;

        private static Definition FindDefinition(string uniqueName)
        {
            return Catalog.FirstOrDefault(d =>
                string.Equals(d.UniqueName, uniqueName, StringComparison.OrdinalIgnoreCase));
        }

        public static void TryRegister(string uniqueName)
        {
            Definition def = FindDefinition(uniqueName);

            if (def == null)
                return;

            GiveUpUntilNextTry.Remove(def.UniqueName);
            Enabled.Add(def.UniqueName);

            if (Registered.Contains(def.UniqueName))
                return;

            EnsureRegisterCoroutine();
        }

        public static void Dispose(string uniqueName)
        {
            Definition def = FindDefinition(uniqueName);

            if (def == null)
                return;

            Enabled.Remove(def.UniqueName);
            Registered.Remove(def.UniqueName);
        }

        public static void DisposeAll()
        {
            Enabled.Clear();
            Registered.Clear();
            _coroutineRunning = false;
        }
        public static void SyncCustomizerFromPatchToggles()
        {
            SetEnabledFromPatch(Leo2K, LEO2K.PatchEnabled);
            SetEnabledFromPatch(Leo2A4, LEO2A4.PatchEnabled);
            SetEnabledFromPatch(Leo2A4_140, LEO2A4_140.PatchEnabled);
            SetEnabledFromPatch(M60A1RiseEra, M60ERA.PatchEnabled);
            SetEnabledFromPatch(M60A3TtsEra, M60ERA.PatchEnabled);
        }

        private static void SetEnabledFromPatch(string uniqueName, bool enabled)
        {
            if (enabled)
                TryRegister(uniqueName);
            else
                Dispose(uniqueName);
        }

        public static void ResetSceneAttempt()
        {
        }

        private static void EnsureRegisterCoroutine()
        {
            if (_coroutineRunning)
                return;

            _coroutineRunning = true;
            MelonCoroutines.Start(RegisterRoutine());
        }

        private static IEnumerator RegisterRoutine()
        {
            while (Enabled.Count > 0)
            {
                bool anyPending = false;

                foreach (Definition def in Catalog)
                {
                    if (!Enabled.Contains(def.UniqueName))
                        continue;

                    if (Registered.Contains(def.UniqueName))
                        continue;

                    if (GiveUpUntilNextTry.Contains(def.UniqueName))
                        continue;

                    anyPending = true;

                    if (!TryRegisterOne(def))
                        yield return null;
                }

                if (!anyPending)
                    break;

                yield return null;
            }

            _coroutineRunning = false;
        }

        private static bool TryRegisterOne(Definition def)
        {
            UnitPrefabLookupScriptable lookup =
                Resources.FindObjectsOfTypeAll<UnitPrefabLookupScriptable>().FirstOrDefault();

            if (lookup == null || lookup.AllUnits == null)
                return false;

            UnitPrefabLookupScriptable.UnitPrefabMetadata sourceMetadata = lookup.AllUnits.FirstOrDefault(metadata =>
                metadata != null &&
                string.Equals(metadata.Name, def.SourceUniqueName, StringComparison.OrdinalIgnoreCase));

            if (sourceMetadata == null)
                return false;

            if (sourceMetadata.PrefabReference == null)
            {
                MelonLogger.Error($"[{def.LogLabel}] Native {def.SourceUniqueName} has no prefab AssetReference.");
                GiveUpUntilNextTry.Add(def.UniqueName);
                return true;
            }

            UnitPrefabLookupScriptable.UnitPrefabMetadata armyMetadata = sourceMetadata;

            if (!string.IsNullOrEmpty(def.ArmySourceUniqueName))
            {
                armyMetadata = lookup.AllUnits.FirstOrDefault(metadata =>
                    metadata != null &&
                    string.Equals(metadata.Name, def.ArmySourceUniqueName, StringComparison.OrdinalIgnoreCase));

                if (armyMetadata == null)
                {
                    MelonLogger.Warning($"[{def.LogLabel}] Could not find army source {def.ArmySourceUniqueName} yet.");
                    return false;
                }
            }

            if (lookup.AllUnits.Any(metadata =>
                    metadata != null &&
                    string.Equals(metadata.Name, def.UniqueName, StringComparison.OrdinalIgnoreCase)))
            {
                Registered.Add(def.UniqueName);
                return true;
            }

            AssetReference baseAmmo = def.UseEmptyBaseAmmoClips ? new AssetReference() : sourceMetadata.BaseAmmoClipsReference;

            List<UnitPrefabLookupScriptable.UnitPrefabMetadata> allUnits = lookup.AllUnits.ToList();

            allUnits.Add(new UnitPrefabLookupScriptable.UnitPrefabMetadata
            {
                AllowInCustomizer = true,
                Class = sourceMetadata.Class,
                Army = armyMetadata.Army,
                DecalLayout = sourceMetadata.DecalLayout,
                PrefabReference = sourceMetadata.PrefabReference,
                BaseAmmoClipsReference = baseAmmo,
                UseDecalLayout = sourceMetadata.UseDecalLayout,
                AlternativeClasses = sourceMetadata.AlternativeClasses,
                Name = def.UniqueName,
                FriendlyName = def.FriendlyName,
            });

            lookup.AllUnits = allUnits.ToArray();
            Registered.Add(def.UniqueName);

            if (def.UniqueName.Equals("T90", StringComparison.OrdinalIgnoreCase))
                MelonLogger.Msg($"[{def.LogLabel}] Registered T-90 using the T-72M1 prefab and USSR classification.");

            return true;
        }

        internal static void ApplySpawnIdentity(Vehicle vehicle, string uniqueName)
        {
            Definition def = FindDefinition(uniqueName);

            if (def == null)
                return;

            vehicle._uniqueName = def.UniqueName;
            vehicle._friendlyName = def.FriendlyName;
        }
    }

    [HarmonyPatch(typeof(UnitSpawner), "SpawnUnit", new Type[]
    {
        typeof(string),
        typeof(UnitMetaData),
        typeof(WaypointHolder),
        typeof(Transform),
    })]
    public static class FuldaStandaloneCustomizerSpawnPatch
    {
        public static void Postfix(string uniqueName, IUnit __result)
        {
            if (string.IsNullOrEmpty(uniqueName) || __result == null)
                return;

            Vehicle vehicle = __result as Vehicle;

            if (vehicle == null)
                return;

            FuldaStandaloneRegistrations.ApplySpawnIdentity(vehicle, uniqueName);
        }
    }
}
