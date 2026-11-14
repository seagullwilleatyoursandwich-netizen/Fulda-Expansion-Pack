using MelonLoader;

namespace Fulda1989.FailRate
{
    // One fail-rate mapping
    public sealed class FailRateVehicleRule
    {
        public string AnchorUniqueName { get; }
        public string[] AlternateUniqueNames { get; }
        public FailRateMode Mode { get; }
        public string ConfigKey { get; }
        public string ConfigComment { get; }
        public int DefaultChancePercent { get; }
        public int DefaultGuaranteedOriginalsPerPlatoon { get; }

        public MelonPreferences_Entry<int> ChancePercent { get; internal set; }
        public MelonPreferences_Entry<int> GuaranteedOriginalsPerPlatoon { get; internal set; }

        public FailRateVehicleRule(string anchorUniqueName, string[] alternateUniqueNames, FailRateMode mode, string configKey, string configComment, int defaultChancePercent, int defaultGuaranteedOriginalsPerPlatoon)
        {
            AnchorUniqueName = anchorUniqueName;
            AlternateUniqueNames = alternateUniqueNames ?? new string[0];
            Mode = mode;
            ConfigKey = configKey;
            ConfigComment = configComment;
            DefaultChancePercent = FailRateCatalog.ClampChancePercent(defaultChancePercent);
            DefaultGuaranteedOriginalsPerPlatoon = defaultGuaranteedOriginalsPerPlatoon < 0 ? 0 : defaultGuaranteedOriginalsPerPlatoon;
        }

        public int CurrentChancePercent
        {
            get
            {
                if (ChancePercent == null)
                    return DefaultChancePercent;

                return FailRateCatalog.ClampChancePercent(ChancePercent.Value);
            }
        }

        public int CurrentGuaranteedOriginalsPerPlatoon
        {
            get
            {
                if (GuaranteedOriginalsPerPlatoon == null)
                    return DefaultGuaranteedOriginalsPerPlatoon;

                return GuaranteedOriginalsPerPlatoon.Value < 0 ? 0 : GuaranteedOriginalsPerPlatoon.Value;
            }
        }
    }
}
