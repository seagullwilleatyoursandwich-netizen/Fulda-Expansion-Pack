using System.Collections;
using Fulda1989;
using GHPC.Camera;
using GHPC.Equipment;
using GHPC.Equipment.Optics;
using GHPC.Utility;
using GHPC.Weapons;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FuldaExpansion1989.Sights
{
    internal static class LEO2KCustomGunnerHud
    {
        private const string HudRootName = "Fulda Leo2K custom HUD";

        public const float RangeCharacterSpacing = 8f;

        private static GameObject canvasShell;

        private static readonly Vector3 RangeTextLocalPos = new Vector3(-20f, -275f, 0f);
        private static readonly Vector3 RangeTextLocalScale = new Vector3(1.5f, 1.5f, 1.5f);

        private static readonly Vector3 GateLocalPos = new Vector3(100f, -268f, 0f);
        private static readonly Vector3 GateLocalScale = new Vector3(2f, 2f, 2f);

        private static readonly Vector3 RangeBarLocalPos = new Vector3(-20f, -270f, 0f);
        private static readonly Vector2 RangeBarSize = new Vector2(200f, 30f);
        private static readonly Vector3 RangeBarLocalScale = new Vector3(6f, 2f, 1.5f);

        private static readonly string[] LegacyGpsObjectNames = { "ReadyToFire", "E-Scale", "E-Scale Red" };

        private static readonly Color GateReadyColor = new Color(0.05f, 0.85f, 0.1f, 1f);
        private static readonly Color GateUnreadyColor = new Color(0.92f, 0.08f, 0.06f, 1f);
        private static readonly Vector2 GateCircleSize = new Vector2(14f, 14f);

        private static Sprite gateCircleSprite;

        internal static void WarmTemplate()
        {
            if (canvasShell != null || SharedAssets.m1ip_range_canvas == null)
                return;

            canvasShell = Object.Instantiate(SharedAssets.m1ip_range_canvas);
            canvasShell.name = "Leo2K HUD shell (template)";

            // Abrams GPS canvas: child 0 = ready lamp, child 2 = extra chrome.
            if (canvasShell.transform.childCount > 2)
                Object.Destroy(canvasShell.transform.GetChild(2).gameObject);
            if (canvasShell.transform.childCount > 0)
                Object.Destroy(canvasShell.transform.GetChild(0).gameObject);

            TMP_Text shellRange = FindRangeText(canvasShell);
            if (shellRange != null)
                ApplyLeo2RangeStyle(shellRange);

            canvasShell.SetActive(false);
            canvasShell.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        internal static void Apply(UsableOptic day, UsableOptic night, FireControlSystem fcs)
        {
            if (day == null || fcs == null)
                return;

            WarmTemplate();
            if (canvasShell == null)
                return;

            MelonCoroutines.Start(ApplyPair(day, night, fcs));
        }

        static IEnumerator ApplyPair(UsableOptic day, UsableOptic night, FireControlSystem fcs)
        {
            yield return ApplyOne(day, fcs, registerMain: true);
            if (night != null && night != day)
                yield return ApplyOne(night, fcs, registerMain: false);
        }

        static IEnumerator ApplyOne(UsableOptic optic, FireControlSystem fcs, bool registerMain)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return null;
                if (optic?.slot != null)
                    break;
            }

            if (optic == null || optic.GetComponent<Leo2KCustomHudInstalled>() != null)
                yield break;

            BootstrapFcs(fcs);
            DisableStockEmesHud(optic, fcs);
            DisableLegacyGpsByName(optic, fcs);

            if (!BuildCustomHud(optic, out TMP_Text range))
                yield break;

            optic.RangeText = range;
            ApplyRangeFormat(optic);
            ApplyRangeStyle(range);
            WireFcs(optic, fcs, registerMain);
            SyncFireGate(optic, fcs);

            if (optic.GetComponent<Leo2KCustomHudDriver>() == null)
                optic.gameObject.AddComponent<Leo2KCustomHudDriver>();

            optic.gameObject.AddComponent<Leo2KCustomHudInstalled>();
        }

        static void BootstrapFcs(FireControlSystem fcs)
        {
            float boot = fcs.IgnoreRangeBelow > 0f ? fcs.IgnoreRangeBelow : 200f;
            if (fcs.MarkRangeInvalidBelow <= 0f)
                fcs.MarkRangeInvalidBelow = 200f;
            fcs.SetRange(boot, forceUpdate: true);
            fcs.RangeInvalid = false;
        }

        static bool BuildCustomHud(UsableOptic optic, out TMP_Text range)
        {
            range = null;
            // Parent on the optic root — not WFOV/NFOV reticle branches that FovLimitedItems toggle off.
            Transform anchor = optic.transform;
            PurgeHudUnder(anchor);

            if (!BuildHudShellOnAnchor(anchor, out range, out GameObject ready, out GameObject unready))
                return false;

            optic.UnreadyToFireObject = unready;
            optic.ReadyToFireObject = ready;
            return true;
        }

        static void PurgeHudUnder(Transform anchor)
        {
            foreach (Transform child in anchor.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == HudRootName)
                {
                    Object.Destroy(child.gameObject);
                    break;
                }
            }
        }

        static bool BuildHudShellOnAnchor(
            Transform anchor,
            out TMP_Text range,
            out GameObject ready,
            out GameObject unready)
        {
            range = null;
            ready = null;
            unready = null;

            GameObject hud = Object.Instantiate(canvasShell);
            hud.name = HudRootName;
            hud.transform.SetParent(anchor, false);
            hud.transform.localPosition = Vector3.zero;
            hud.transform.localRotation = Quaternion.identity;
            hud.transform.localScale = Vector3.one;
            hud.SetActive(true);

            foreach (Canvas c in hud.GetComponentsInChildren<Canvas>(true))
            {
                c.enabled = true;
                c.gameObject.SetActive(true);
            }

            range = FindRangeText(hud);
            Transform uiRoot = range != null ? range.transform.parent : hud.transform;

            PurgeLeftoverShellUi(hud, range);

            GameObject rangeBar = CreateBlackBar(uiRoot, "range bar", RangeBarLocalPos, RangeBarSize);
            rangeBar.transform.localScale = RangeBarLocalScale;

            if (range != null)
            {
                range.transform.localPosition = RangeTextLocalPos;
                range.transform.localScale = RangeTextLocalScale;
                range.transform.SetAsLastSibling();
                ApplyLeo2RangeStyle(range);
            }

            unready = CreateGateCircle(uiRoot, "unready gate lamp", GateLocalPos, GateUnreadyColor);
            ready = CreateGateCircle(uiRoot, "ready gate lamp", GateLocalPos, GateReadyColor);
            unready.transform.localScale = GateLocalScale;
            ready.transform.localScale = GateLocalScale;
            ready.SetActive(false);
            unready.SetActive(true);

            SetVisibilityLayer(hud);
            return range != null;
        }

        static GameObject CreateBlackBar(Transform parent, string name, Vector3 localPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            Image img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return go;
        }

        static GameObject CreateGateCircle(Transform parent, string name, Vector3 localPos, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            Image img = go.AddComponent<Image>();
            img.sprite = GetGateCircleSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = GateCircleSize;
            return go;
        }

        static Sprite GetGateCircleSprite()
        {
            if (gateCircleSprite != null)
                return gateCircleSprite;

            const int res = 32;
            var tex = new Texture2D(res, res, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;

            float cx = (res - 1) * 0.5f;
            float r = cx;
            float rSq = r * r;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = x - cx;
                    float dy = y - cx;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= rSq ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            gateCircleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
            return gateCircleSprite;
        }

        static TMP_Text FindRangeText(GameObject hud)
        {
            Transform rangeNode = hud.transform.Find("range text (TMP)");
            if (rangeNode != null)
            {
                TMP_Text t = rangeNode.GetComponent<TMP_Text>();
                if (t != null)
                    return t;
            }

            return hud.GetComponentInChildren<TMP_Text>(true);
        }

        static void PurgeLeftoverShellUi(GameObject hud, TMP_Text range)
        {
            foreach (Image img in hud.GetComponentsInChildren<Image>(true))
            {
                if (img == null)
                    continue;

                string n = img.gameObject.name;
                if (n == "range bar" || n.Contains("gate lamp"))
                    continue;

                img.gameObject.SetActive(false);
            }

            foreach (TMP_Text tmp in hud.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null || tmp == range)
                    continue;

                tmp.gameObject.SetActive(false);
            }
        }

        static void ApplyRangeFormat(UsableOptic optic)
        {
            optic.RangeTextQuantize = 10;
            optic.RangeTextDivideBy = 1;
            optic.RangeTextArchetype = "0000";
            optic.RangeTextPrefix = "<mspace=0.5em>";
            optic.RangeTextSuffix = "</mspace>";
            optic.InvalidRangeBlink = true;
            optic.ZeroOutInvalidRange = true;
            optic.RangeBlinkInterval = 0.5f;
        }

        static void ApplyRangeStyle(TMP_Text text) => ApplyLeo2RangeStyle(text);

        static void ApplyLeo2RangeStyle(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset segment7 = SharedAssets.leo2_segment7_sdf;
            if (segment7 != null)
            {
                text.font = segment7;
                if (segment7.material != null)
                    text.fontSharedMaterial = segment7.material;
            }

            text.color = Color.red;
            text.faceColor = Color.red;
            text.outlineColor = new Color(0.4f, 0f, 0f, 0.5f);
            text.outlineWidth = 1f;
            text.characterSpacing = RangeCharacterSpacing;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = true;
            text.ForceMeshUpdate(true);
        }
        static void DisableLegacyGpsByName(UsableOptic optic, FireControlSystem fcs)
        {
            if (optic != null)
                HideLegacyGpsObjects(optic.transform);
            if (fcs != null)
                HideLegacyGpsObjects(fcs.transform);
        }

        static void HideLegacyGpsObjects(Transform root)
        {
            if (root == null)
                return;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!IsUnderGps(t))
                    continue;

                if (!IsNamedLegacyGps(t))
                    continue;

                t.gameObject.SetActive(false);
            }
        }

        static bool IsNamedLegacyGps(Transform t)
        {
            if (IsLegacyName(t.name))
                return true;

            Transform p = t.parent;
            while (p != null && !string.Equals(p.name, "GPS", System.StringComparison.OrdinalIgnoreCase))
            {
                if (IsLegacyName(p.name))
                    return true;
                p = p.parent;
            }

            return false;
        }

        static bool IsLegacyName(string name)
        {
            for (int i = 0; i < LegacyGpsObjectNames.Length; i++)
            {
                if (string.Equals(name, LegacyGpsObjectNames[i], System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static bool IsUnderGps(Transform t)
        {
            while (t != null)
            {
                if (string.Equals(t.name, "GPS", System.StringComparison.OrdinalIgnoreCase))
                    return true;
                t = t.parent;
            }

            return false;
        }

        static void DisableStockEmesHud(UsableOptic optic, FireControlSystem fcs)
        {
            if (optic != null)
                HideEmesCanvases(optic.transform);
            if (fcs != null)
                HideEmesCanvases(fcs.transform);
        }

        static void HideEmesCanvases(Transform root)
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas == null || !canvas.gameObject.activeSelf)
                    continue;

                string n = canvas.name.ToUpperInvariant();
                if (n.Contains("FULDA") || n.Contains("FLIR") || n.Contains("THERMAL"))
                    continue;

                if (n.Contains("EMES") || n.Contains("GPS"))
                    canvas.gameObject.SetActive(false);
            }
        }

        static void WireFcs(UsableOptic optic, FireControlSystem fcs, bool registerMain)
        {
            if (optic.FCS != null && optic.FCS != fcs)
                Unhook(optic, optic.FCS);

            optic.FCS = fcs;
            if (registerMain && fcs.MainOptic != optic)
                fcs.RegisterOptic(optic);

            Unhook(optic, fcs);
            fcs.AmmoTypeChanged += optic.FCS_AmmoTypeChanged;
            fcs.RangeChanged += optic.FCS_RangeChanged;
            fcs.LaserFired += optic.FCS_LaserFired;
            fcs.StabsActiveChanged += optic.FCS_StabsActiveChanged;
            optic.StabsActive = fcs.StabsActive;
            optic.FCS_AmmoTypeChanged(fcs.CurrentAmmoType);
            optic.FCS_RangeChanged(fcs.CurrentRange);
        }

        static void Unhook(UsableOptic optic, FireControlSystem fcs)
        {
            fcs.AmmoTypeChanged -= optic.FCS_AmmoTypeChanged;
            fcs.RangeChanged -= optic.FCS_RangeChanged;
            fcs.LaserFired -= optic.FCS_LaserFired;
            fcs.StabsActiveChanged -= optic.FCS_StabsActiveChanged;
        }

        internal static void SyncFireGate(UsableOptic optic, FireControlSystem fcs)
        {
            if (optic?.UnreadyToFireObject == null || optic.ReadyToFireObject == null)
                return;

            bool able = fcs?.CurrentWeaponSystem != null && fcs.CurrentWeaponSystem.AbleToFire;
            optic.UnreadyToFireObject.SetActive(!able);
            optic.ReadyToFireObject.SetActive(able);
        }

        static void SetVisibilityLayer(GameObject root)
        {
            int layer = CodeUtils.LAYER_INDEX_VISIBILITYONLY;
            if (layer < 0)
                layer = LayerMask.NameToLayer("Visibility Only");

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        sealed class Leo2KCustomHudInstalled : MonoBehaviour { }

        sealed class Leo2KCustomHudDriver : MonoBehaviour
        {
            UsableOptic _optic;

            void Awake() => _optic = GetComponent<UsableOptic>();

            void LateUpdate()
            {
                UsableOptic optic = _optic ?? GetComponent<UsableOptic>();
                if (optic?.slot == null || !optic.slot.IsActive)
                    return;

                if (optic.FCS != null)
                    LEO2KCustomGunnerHud.SyncFireGate(optic, optic.FCS);
            }
        }
    }
}