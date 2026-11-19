using GHPC;
using GHPC.AI;
using GHPC.Equipment.Optics;
using GHPC.Vehicle;

namespace Fulda1989
{
    // AI night spotting uses "Unit._nightVisionType" and "TargetSpotterSettings._bestNightVision", player thermal installs alone do not update those fields and must be changed so that the AI gets the intended range buff at night.
    public static class AiSpotterUtil
    {
        public static void ApplyGen1ThermalNightFighting(Vehicle vic)
        {
            if (vic == null)
                return;

            vic._nightVisionType = NightVisionType.Thermal;

            TargetSpotterSettings spotter = vic.TargetSpotterSettings;
            if (spotter == null)
                return;

            spotter._bestNightVision = TargetSpotterSettings.NightVisionQuality.Gen1Thermal;
            spotter._initialized = false;
            spotter.Initialize();
        }
    }
}