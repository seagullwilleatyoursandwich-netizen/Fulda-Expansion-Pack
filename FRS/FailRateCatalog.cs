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
                if (rule.ChancePercent != null && rule.GuaranteedOriginalsPerPlatoon != null)
                    continue;

                string verb = rule.Mode == FailRateMode.Upgrade ? "upgrade" : "downgrade";

                string chanceId = $"FRS | {rule.ConfigKey} ({verb} %)";
                string guaranteedId = $"FRS | {rule.ConfigKey} (guaranteed per platoon)";

                rule.ChancePercent = category.GetEntry<int>(chanceId) ?? category.CreateEntry<int>(chanceId, rule.DefaultChancePercent);
                rule.ChancePercent.Comment = rule.ConfigComment + " Value is 0-100 (percent chance after platoon guarantees are used).";

                rule.GuaranteedOriginalsPerPlatoon = category.GetEntry<int>(guaranteedId) ?? category.CreateEntry<int>(guaranteedId, rule.DefaultGuaranteedOriginalsPerPlatoon);
                rule.GuaranteedOriginalsPerPlatoon.Comment = "How many vehicles in each platoon always spawn as this type (FRS ignored). Platoon leader is always first among these. 0 = every member can roll fail-rate.";
            }
        }

        static void RegisterBuiltInRules()
        {
            AddDowngrade(FuldaStandaloneRegistrations.Leo2A4_140, "NPzK-140", new[] { FuldaStandaloneRegistrations.Leo2A4 }, "Chance NPzK-140 spawns as Leopard 2A4 (1988) instead.", defaultChancePercent: 75, defaultGuaranteedPerPlatoon: 1);

            AddDowngrade(FuldaStandaloneRegistrations.Leo2A4, "Leopard 2A4 (1988)", new[] { "LEO2A3" }, "Chance for a Leopard 2A4 to spawn as an earlier Leopard 2 stand-in (2A3).", defaultChancePercent: 30, defaultGuaranteedPerPlatoon: 1);

            AddDowngrade(FuldaStandaloneRegistrations.Leo2K, "Leopard 2K", new[] { "LEO1A4", "LEO1A3A3" }, "Chance that Leopard 2K's spawn as Leopard 1A4/1A1A1 stand-ins.", defaultChancePercent: 20, defaultGuaranteedPerPlatoon: 2);
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
