using System;
using GHPC.Effects;
using GHPC.Utility;
using GHPC.Vehicle;
using HarmonyLib;
using UnityEngine;

namespace Fulda1989
{
    public static class ScorchUtil
    {
        public static void RegisterScorchRoot(Vehicle vic, Transform scorchRoot, params Transform[] dropRenderersUnder)
        {
            if (vic == null || scorchRoot == null)
                return;

            FlammablesManager fm = vic.GetComponentInChildren<FlammablesManager>(true);
            if (fm == null)
                return;

            PruneInvalidScorchRenderers(fm);

            if (dropRenderersUnder != null)
            {
                foreach (Transform drop in dropRenderersUnder)
                {
                    if (drop != null)
                        RemoveScorchRenderersUnder(fm, drop);
                }
            }

            AppendScorchParent(fm, scorchRoot);

            Renderer[] renderers = scorchRoot.GetComponentsInChildren<Renderer>(true);
            RenderUtil.AddMPBRenderers(
                fm._scorchRendererMaterials,
                renderers,
                FlammablesManager.IsScorchMaterial);

            PruneInvalidScorchRenderers(fm);
        }

        public static void PruneInvalidScorchRenderers(Vehicle vic)
        {
            if (vic == null)
                return;

            FlammablesManager fm = vic.GetComponentInChildren<FlammablesManager>(true);
            if (fm != null)
                PruneInvalidScorchRenderers(fm);
        }

        public static void PruneInvalidScorchRenderers(FlammablesManager fm)
        {
            if (fm == null || fm._scorchRendererMaterials == null)
                return;

            var list = fm._scorchRendererMaterials;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!IsScorchEntryUsable(list[i]))
                    list.RemoveAt(i);
            }
        }

        public static void RemoveScorchRenderersUnder(FlammablesManager fm, Transform root)
        {
            if (fm == null || root == null || fm._scorchRendererMaterials == null)
                return;

            var list = fm._scorchRendererMaterials;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Renderer renderer = list[i].Renderer;
                if (renderer == null || renderer.transform.IsChildOf(root))
                    list.RemoveAt(i);
            }
        }

        static bool IsScorchEntryUsable(RendererMaterial entry)
        {
            return IsScorchRendererUsable(entry.Renderer, entry.MaterialIndex);
        }

        static bool IsScorchRendererUsable(Renderer renderer, int materialIndex = 0)
        {
            if (renderer == null)
                return false;

            GameObject go = renderer.gameObject;
            if (go == null || !go.activeInHierarchy)
                return false;

            Material[] materials = renderer.sharedMaterials;
            return materials != null
                && materialIndex >= 0
                && materialIndex < materials.Length;
        }

        [HarmonyPatch(typeof(FlammablesManager), nameof(FlammablesManager.ForceScorchAll))]
        static class ForceScorchAllPrunePatch
        {
            static void Prefix(FlammablesManager __instance)
            {
                PruneInvalidScorchRenderers(__instance);
            }
        }

        static void AppendScorchParent(FlammablesManager fm, Transform parent)
        {
            Transform[] existing = fm.ScorchRenderersParents;
            if (existing != null)
            {
                foreach (Transform t in existing)
                {
                    if (t == parent)
                        return;
                }
            }

            int oldLen = existing?.Length ?? 0;
            var next = new Transform[oldLen + 1];
            if (oldLen > 0)
                Array.Copy(existing, next, oldLen);
            next[oldLen] = parent;
            fm.ScorchRenderersParents = next;
        }
    }
}
