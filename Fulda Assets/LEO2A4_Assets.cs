using BehaviorDesigner.Runtime.Tasks.Movement;
using GHPC.Vehicle;
using GHPC.Weaponry;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using VanillaAssetUtil.AssetUtil;

namespace Fulda1989
{
    public class LEO2A4_Assets : Module
    {
        private static AssetBundle leo2a4Bundle;
        private static bool assetsResolved;

        internal static GameObject full140;
        internal static GameObject kwsGun;
        internal static Material aarctech2a4;
        public static Vehicle t72;

        public override void LoadDynamicAssets()
        {
            MelonLogger.Msg("[LEO2A4 Assets] Loading T-72M donor...");

            t72 = AssetUtilApi.LoadVanillaVehicle("T72M", temp: true);

            MelonLogger.Msg("[LEO2A4 Assets] T-72M donor result: " + $"{(t72 != null ? t72.name : "NULL")}");
        }

        public override void LoadStaticAssets()
        {
            string path = Path.Combine(MelonEnvironment.ModsDirectory, "Fulda1989", "leo2a4");

            leo2a4Bundle = AssetBundle.LoadFromFile(path);

            if (leo2a4Bundle == null)
            {
                MelonLogger.Error("[LEO2A4 ASSETS] Failed to load leo2a4 AssetBundle.");
                return;
            }

            leo2a4Bundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            full140 = leo2a4Bundle.LoadAsset<GameObject>("2a4_140full");
            if (full140 == null)
            {
                MelonLogger.Error("[LEO2A4 ASSETS] Failed to find 2a4_140full.");
                return;
            }

            full140.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            MelonLogger.Msg("[LEO2A4 ASSETS] Loaded 2a4_140full.");

            kwsGun = leo2a4Bundle.LoadAsset<GameObject>("KWS III Gun");
            if (kwsGun == null)
            {
                MelonLogger.Error("[LEO2A4 ASSETS] Failed to find KWS III Gun.");
                return;
            }

            kwsGun.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            MelonLogger.Msg("[LEO2A4 ASSETS] Loaded KWS III Gun.");

            foreach (string assetPath in leo2a4Bundle.GetAllAssetNames())
            {
                if (Path.GetFileNameWithoutExtension(assetPath).IndexOf("aar2a4ctech", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                aarctech2a4 = leo2a4Bundle.LoadAsset<Material>(assetPath);
                if (aarctech2a4 != null)
                {
                    aarctech2a4.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    break;
                }
            }

            if (aarctech2a4 == null)
            {
                MelonLogger.Error("[LEO2A4 ASSETS] Failed to find aar2a4ctech material.");
                return;
            }

            Shader shader = Shader.Find("Standard (FLIR)");
            if (shader != null)
            {
                aarctech2a4.shader = shader;
                aarctech2a4.renderQueue = 2000;
                aarctech2a4.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            if (kwsGun != null)
                Util.SetupFLIRShaders(kwsGun);
        }
    }
}