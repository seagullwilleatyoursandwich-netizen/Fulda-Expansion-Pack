using Fulda1989;
using MelonLoader;
using System;
using System.Collections.Generic;

namespace Fulda1989.FailRate
{
    public static class FailRateCatalog
    {
        static readonly List<FailRateVehicleRule> Rules = new List<FailRateVehicleRule>();

        public static IReadOnlyList<FailRateVehicleRule> AllRules => Rules;

        static FailRateCatalog()
        {
            RegisterBuiltInRules();
        }

        public static FailRateVehicleRule FindRule(string anchorUniqueName)
        {
            if (string.IsNullOrEmpty(anchorUniqueName))
                return null;

            foreach (FailRateVehicleRule rule in Rules)
            {
                if (rule != null && string.Equals(rule.AnchorUniqueName, anchorUniqueName, StringComparison.OrdinalIgnoreCase))
                    return rule;
            }

            return null;
        }

        public static void Config(MelonPreferences_Category category)
        {
            if (category == null)
                return;

            if (Rules.Count == 0)
                RegisterBuiltInRules();

            foreach (FailRateVehicleRule rule in Rules)
            {
                string verb = rule.Mode == FailRateMode.Upgrade ? "upgrade" : "downgrade";

                string chanceId = $"FRS | {rule.AnchorUniqueName} ({verb} %)";
                string guaranteedId = $"FRS | {rule.AnchorUniqueName} (guaranteed per platoon)";

                string legacyChanceId = $"FRS | {rule.ConfigKey} ({verb} %)";
                string legacyGuaranteedId = $"FRS | {rule.ConfigKey} (guaranteed per platoon)";

                rule.ChancePercent = category.GetEntry<int>(chanceId);
                if (rule.ChancePercent == null)
                {
                    string renameFrom = legacyChanceId != chanceId ? legacyChanceId : null;
                    rule.ChancePercent = category.CreateEntry<int>(chanceId, rule.DefaultChancePercent, oldIdentifier: renameFrom);
                }

                rule.ChancePercent.Comment = $"{rule.ConfigKey}: {rule.ConfigComment} Value is 0-100 (percent chance after platoon guarantees are used).";

                rule.GuaranteedOriginalsPerPlatoon = category.GetEntry<int>(guaranteedId);
                if (rule.GuaranteedOriginalsPerPlatoon == null)
                {
                    string renameFrom = legacyGuaranteedId != guaranteedId ? legacyGuaranteedId : null;
                    rule.GuaranteedOriginalsPerPlatoon = category.CreateEntry<int>(guaranteedId, rule.DefaultGuaranteedOriginalsPerPlatoon, oldIdentifier: renameFrom);
                }

                rule.GuaranteedOriginalsPerPlatoon.Comment = $"How many {rule.ConfigKey} platoon members (formation order, leader first) always spawn as this type before fail-rate rolls. 0 = every member rolls.";
            }
        }

        static void RegisterBuiltInRules()
        {
            AddDowngrade(FuldaStandaloneRegistrations.Leo2A4_140, "NPzK-140", new[] { FuldaStandaloneRegistrations.Leo2A4 }, "Chance NPzK-140 spawns as Leopard 2A4 (1988) instead.", defaultChancePercent: 75, defaultGuaranteedPerPlatoon: 1);

            AddDowngrade(FuldaStandaloneRegistrations.Leo2A4, "Leopard 2A4 (1988)", new[] { FuldaStandaloneRegistrations.Leo2A4_86 }, "Chance Leopard 2A4 (1988) spawns as Leopard 2A4 (1986) instead (no C-tech).", defaultChancePercent: 30, defaultGuaranteedPerPlatoon: 1);

            AddDowngrade(FuldaStandaloneRegistrations.Leo2A4_86, "Leopard 2A4 (1986)", new[] { "LEO2A3" }, "Chance Leopard 2A4 (1986) spawns as a Leopard 2A3 instead.", defaultChancePercent: 30, defaultGuaranteedPerPlatoon: 1);

            AddDowngrade(FuldaStandaloneRegistrations.Leo2K, "Leopard 2K", new[] { "LEO1A4", "LEO1A3A3" }, "Chance that Leopard 2K's spawn as Leopard 1A4/1A3A3 stand-ins.", defaultChancePercent: 20, defaultGuaranteedPerPlatoon: 2);

            AddDowngrade(FuldaStandaloneRegistrations.T64Bv1988Unit, "T-64BV 1988", new[] { FuldaStandaloneRegistrations.T64BvUnit }, "Chance T-64BV obr.1988 spawns as obr.1985 (no Agava) instead.", defaultChancePercent: 30, defaultGuaranteedPerPlatoon: 1);

            AddDowngrade(FuldaStandaloneRegistrations.T64Bv1_1988Unit, "T-64BV1 1988", new[] { FuldaStandaloneRegistrations.T64Bv1Unit }, "Chance T-64BV1 obr.1988 spawns as obr.1985 (no Agava) instead.", defaultChancePercent: 30, defaultGuaranteedPerPlatoon: 1);
        }

        public static FailRateVehicleRule AddDowngrade(string anchorUniqueName, string configKey, string[] alternateUniqueNames, string configComment, int defaultChancePercent, int defaultGuaranteedPerPlatoon = 0)
        {
            return AddRule(anchorUniqueName, alternateUniqueNames, FailRateMode.Downgrade, configKey, configComment, defaultChancePercent, defaultGuaranteedPerPlatoon);
        }

        public static FailRateVehicleRule AddUpgrade(string anchorUniqueName, string configKey, string[] alternateUniqueNames, string configComment, int defaultChancePercent, int defaultGuaranteedPerPlatoon = 0)
        {
            return AddRule(anchorUniqueName, alternateUniqueNames, FailRateMode.Upgrade, configKey, configComment, defaultChancePercent, defaultGuaranteedPerPlatoon);
        }

        public static FailRateVehicleRule AddRule(string anchorUniqueName, string[] alternateUniqueNames, FailRateMode mode, string configKey, string configComment, int defaultChancePercent, int defaultGuaranteedPerPlatoon)
        {
            var rule = new FailRateVehicleRule(anchorUniqueName, alternateUniqueNames, mode, configKey, configComment, defaultChancePercent, defaultGuaranteedPerPlatoon);

            Rules.Add(rule);
            return rule;
        }

        public static int ClampChancePercent(int value)
        {
            if (value < 0)
                return 0;

            if (value > 100)
                return 100;

            return value;
        }
    }
}
