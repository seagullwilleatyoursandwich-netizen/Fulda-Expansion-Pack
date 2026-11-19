using GHPC.Equipment;
using UnityEngine;

namespace Fulda1989
{
    public class Armour
    {
        internal static bool done = false;
        public static ArmorCodexScriptable eber;
        public static ArmorCodexScriptable vorschlaghammer;
        public static ArmorCodexScriptable ctech;
        public static ArmorCodexScriptable t64bvUfpFiveLayer;

        public static void Init()
        {
            if (done) return;

            eber = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
            eber.name = "Eber composite";
            ArmorType ebercomposite= new ArmorType();
            ebercomposite.Name = "Eber composite";
            ebercomposite.CanRicochet = true;
            ebercomposite.CanShatterLongRods = true;
            ebercomposite.NormalizesHits = true;
            ebercomposite.ThicknessSource = ArmorType.RhaSource.Multipliers;
            ebercomposite.SpallAngleMultiplier = 0.9f;
            ebercomposite.SpallPowerMultiplier = 0.7f;
            ebercomposite.RhaeMultiplierCe = 2.4f;
            ebercomposite.RhaeMultiplierKe = 1f;
            ebercomposite.CrushThicknessModifier = 1f;
            eber.ArmorType = ebercomposite;

            vorschlaghammer = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
            vorschlaghammer.name = "Vorschlaghammer composite";
            ArmorType vorschlag = new ArmorType();
            vorschlag.Name = "Vorschlaghammer composite";
            vorschlag.CanRicochet = true;
            vorschlag.CanShatterLongRods = true;
            vorschlag.NormalizesHits = true;
            vorschlag.ThicknessSource = ArmorType.RhaSource.Multipliers;
            vorschlag.SpallAngleMultiplier = 0.8f;
            vorschlag.SpallPowerMultiplier = 0.5f;
            vorschlag.RhaeMultiplierCe = 2.25f;
            vorschlag.RhaeMultiplierKe = 1f;
            vorschlag.CrushThicknessModifier = 1f;
            vorschlaghammer.ArmorType = vorschlag;

            ctech = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
            ctech.name = "C-Tech composite";
            ArmorType ctec = new ArmorType();
            ctec.Name = "C-Tech composite";
            ctec.CanRicochet = true;
            ctec.CanShatterLongRods = true;
            ctec.NormalizesHits = true;
            ctec.ThicknessSource = ArmorType.RhaSource.Multipliers;
            ctec.SpallAngleMultiplier = 0.9f;
            ctec.SpallPowerMultiplier = 0.7f;
            ctec.RhaeMultiplierCe = 1.5f;
            ctec.RhaeMultiplierKe = 1f;
            ctec.CrushThicknessModifier = 1f;
            ctech.ArmorType = ctec;

            t64bvUfpFiveLayer = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
            t64bvUfpFiveLayer.name = "5-layer upper glacis array";
            ArmorType ufplayer = new ArmorType();
            ufplayer.Name = "5-layer upper glacis array";
            ufplayer.CanRicochet = true;
            ufplayer.CanShatterLongRods = true;
            ufplayer.NormalizesHits = true;
            ufplayer.ThicknessSource = ArmorType.RhaSource.Multipliers;
            ufplayer.SpallAngleMultiplier = 0.9f;
            ufplayer.SpallPowerMultiplier = 0.7f;
            ufplayer.RhaeMultiplierCe = 1.35f;
            ufplayer.RhaeMultiplierKe = 1f;
            ufplayer.CrushThicknessModifier = 1f;
            t64bvUfpFiveLayer.ArmorType = ufplayer;

            done = true;
        }
    }
}
