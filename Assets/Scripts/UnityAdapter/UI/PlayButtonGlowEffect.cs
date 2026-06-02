using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class PlayButtonGlowEffect : MonoBehaviour
    {
        private const string GlowRigName = "__PlayButtonGlowRig";
        private const string OuterGlowName = "OuterGlow";
        private const string CoreGlowName = "CoreGlow";
        private const string BeamGlowName = "BeamGlow";

        [Header("Target")]
        [SerializeField] private RectTransform targetRect;

        [Header("Visibility")]
        [SerializeField] private bool enableGlow = false;

        [Header("Pulse Integration")]
        [SerializeField] private bool ensurePulseEffect = true;

        [Header("Authoring")]
        [SerializeField] private bool autoCreateGlowRig = false;

        [Header("Outer Halo")]
        [SerializeField] private Color glowColor = new Color(1f, 0.78f, 0.24f, 1f);
        [SerializeField, Range(0f, 1f)] private float minGlowAlpha = 0.24f;
        [SerializeField, Range(0f, 1f)] private float maxGlowAlpha = 0.52f;
        [SerializeField] private Vector2 outerGlowScale = new Vector2(1.54f, 2.8f);
        [SerializeField] private Vector2 outerGlowOffset = new Vector2(0f, -2f);

        [Header("Core Light")]
        [SerializeField] private Color coreGlowColor = new Color(1f, 0.96f, 0.72f, 1f);
        [SerializeField, Range(0f, 1f)] private float minCoreGlowAlpha = 0.12f;
        [SerializeField, Range(0f, 1f)] private float maxCoreGlowAlpha = 0.24f;
        [SerializeField] private Vector2 coreGlowScale = new Vector2(1.22f, 1.7f);
        [SerializeField] private Vector2 coreGlowOffset = new Vector2(0f, -1f);

        [Header("Beam")]
        [SerializeField] private bool enableLightBeam = true;
        [SerializeField] private Color beamGlowColor = new Color(1f, 0.88f, 0.34f, 1f);
        [SerializeField, Range(0f, 1f)] private float minBeamAlpha = 0.1f;
        [SerializeField, Range(0f, 1f)] private float maxBeamAlpha = 0.24f;
        [SerializeField] private Vector2 beamScale = new Vector2(2.1f, 5.1f);
        [SerializeField] private Vector2 beamOffset = new Vector2(0f, -4f);

        [Header("Pulse")]
        [SerializeField, Min(0.05f)] private float pulseSpeed = 1.45f;
        [SerializeField, Range(0f, 0.2f)] private float scalePulseAmount = 0.05f;
        [SerializeField] private bool useUnscaledTime = true;

        private RectTransform _buttonRect;
        private RectTransform _glowRigRect;
        private Image _outerGlowImage;
        private Image _coreGlowImage;
        private Image _beamGlowImage;
        private bool _legacyCleanupDone;

        private static Sprite s_glowSprite;
        private static Sprite s_beamSprite;
        private static Sprite s_ringGlowSprite;

        private void Reset()
        {
            targetRect = transform as RectTransform;
        }

        private void Awake()
        {
            Initialize();
            ApplyInitialFrame();
        }

        private void OnEnable()
        {
            Initialize();

            if (_glowRigRect != null)
                _glowRigRect.gameObject.SetActive(true);

            ApplyInitialFrame();
        }

        private void LateUpdate()
        {
            if (!Initialize())
                return;

            if (UISettingsProfile.IsReduceMotionEnabled())
            {
                ApplyReducedMotionFrame();
                return;
            }

            float t = EvaluateWave();
            ApplyFrame(t);
        }

        private void OnDisable()
        {
            ResetButtonScale();

            if (_glowRigRect != null)
                _glowRigRect.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            ResetButtonScale();

            if (_glowRigRect != null)
                DestroyUnityObject(_glowRigRect.gameObject);
        }

        private void OnValidate()
        {
            minGlowAlpha = Mathf.Clamp01(minGlowAlpha);
            maxGlowAlpha = Mathf.Clamp(maxGlowAlpha, minGlowAlpha, 1f);
            minCoreGlowAlpha = Mathf.Clamp01(minCoreGlowAlpha);
            maxCoreGlowAlpha = Mathf.Clamp(maxCoreGlowAlpha, minCoreGlowAlpha, 1f);
            minBeamAlpha = Mathf.Clamp01(minBeamAlpha);
            maxBeamAlpha = Mathf.Clamp(maxBeamAlpha, minBeamAlpha, 1f);
            pulseSpeed = Mathf.Max(0.05f, pulseSpeed);
            outerGlowScale = ClampScale(outerGlowScale);
            coreGlowScale = ClampScale(coreGlowScale);
            beamScale = ClampScale(beamScale);

            if (!gameObject.activeInHierarchy)
                return;

            Initialize();
            ApplyInitialFrame();
        }

        private bool Initialize()
        {
            if (_buttonRect == null)
                _buttonRect = targetRect != null ? targetRect : transform as RectTransform;

            if (_buttonRect == null || _buttonRect.parent == null)
                return false;

            EnsurePulseEffectOnTarget();

            if (!enableGlow)
            {
                CleanupAllGlowRigsInScope();
                return false;
            }

            if (!_legacyCleanupDone)
            {
                CleanupLegacyOutlineGlow();
                _legacyCleanupDone = true;
            }

            EnsureGlowRig();
            SyncGlowRigTransform();

            return _glowRigRect != null;
        }

        private void EnsureGlowRig()
        {
            if (_buttonRect == null)
                return;

            if (_glowRigRect == null)
            {
                Transform existing = _buttonRect.Find(GlowRigName);
                if (existing != null)
                    _glowRigRect = existing as RectTransform;
            }

            if (_glowRigRect == null && autoCreateGlowRig)
            {
                var glowRig = new GameObject(GlowRigName, typeof(RectTransform));
                glowRig.layer = gameObject.layer;
                _glowRigRect = glowRig.GetComponent<RectTransform>();
                _glowRigRect.SetParent(_buttonRect, false);
            }

            if (_glowRigRect == null)
            {
                _outerGlowImage = null;
                _beamGlowImage = null;
                _coreGlowImage = null;
                return;
            }

            _outerGlowImage = EnsureLayerImage(_glowRigRect, OuterGlowName, GetRingGlowSprite());
            _beamGlowImage = EnsureLayerImage(_glowRigRect, BeamGlowName, GetBeamSprite());
            _coreGlowImage = EnsureLayerImage(_glowRigRect, CoreGlowName, GetGlowSprite());
        }

        private void SyncGlowRigTransform()
        {
            if (_buttonRect == null || _glowRigRect == null)
                return;

            _glowRigRect.anchorMin = new Vector2(0.5f, 0.5f);
            _glowRigRect.anchorMax = new Vector2(0.5f, 0.5f);
            _glowRigRect.pivot = new Vector2(0.5f, 0.5f);
            _glowRigRect.sizeDelta = GetButtonSize();
            _glowRigRect.anchoredPosition3D = Vector3.zero;
            _glowRigRect.localRotation = Quaternion.identity;
            _glowRigRect.localScale = Vector3.one;

            if (_glowRigRect.GetSiblingIndex() != 0)
                _glowRigRect.SetSiblingIndex(0);
        }

        private float EvaluateWave()
        {
            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            float wave = Mathf.PingPong(time * pulseSpeed, 1f);
            return Mathf.SmoothStep(0f, 1f, wave);
        }

        private void ApplyFrame(float t)
        {
            if (_buttonRect == null || _glowRigRect == null)
                return;

            SyncGlowRigTransform();

            float outerAlpha = Mathf.Lerp(minGlowAlpha, maxGlowAlpha, t);
            float coreAlpha = Mathf.Lerp(minCoreGlowAlpha, maxCoreGlowAlpha, t);
            float beamAlpha = enableLightBeam ? Mathf.Lerp(minBeamAlpha, maxBeamAlpha, t) : 0f;
            float glowPulse = 1f + (scalePulseAmount * t);

            ApplyLayer(_outerGlowImage, outerGlowScale, outerGlowOffset, glowColor, outerAlpha, glowPulse);
            ApplyLayer(_coreGlowImage, coreGlowScale, coreGlowOffset, coreGlowColor, coreAlpha, 1f + (scalePulseAmount * 0.55f * t));

            if (_beamGlowImage != null)
            {
                _beamGlowImage.gameObject.SetActive(enableLightBeam);
                if (enableLightBeam)
                    ApplyLayer(_beamGlowImage, beamScale, beamOffset, beamGlowColor, beamAlpha, 1f + (scalePulseAmount * 0.85f * t));
            }
        }

        private void ApplyInitialFrame()
        {
            if (UISettingsProfile.IsReduceMotionEnabled())
            {
                ApplyReducedMotionFrame();
                return;
            }

            ApplyFrame(0.6f);
        }

        private void ApplyReducedMotionFrame()
        {
            if (_buttonRect == null || _glowRigRect == null)
                return;

            SyncGlowRigTransform();

            ApplyLayer(_outerGlowImage, outerGlowScale, outerGlowOffset, glowColor, minGlowAlpha * 0.72f, 1f);
            ApplyLayer(_coreGlowImage, coreGlowScale, coreGlowOffset, coreGlowColor, minCoreGlowAlpha * 0.8f, 1f);

            if (_beamGlowImage != null)
                _beamGlowImage.gameObject.SetActive(false);
        }

        private void ApplyLayer(Image image, Vector2 scale, Vector2 offset, Color color, float alpha, float pulse)
        {
            if (image == null)
                return;

            RectTransform rect = image.rectTransform;
            Vector2 buttonSize = GetButtonSize();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.localScale = Vector3.one;
            rect.sizeDelta = Vector2.Scale(buttonSize, scale) * pulse;

            Color layerColor = color;
            layerColor.a = alpha;
            image.color = layerColor;
        }

        private Vector2 GetButtonSize()
        {
            if (_buttonRect == null)
                return new Vector2(200f, 50f);

            Vector2 rectSize = _buttonRect.rect.size;
            if (rectSize.x > 0f && rectSize.y > 0f)
                return rectSize;

            Vector2 sizeDelta = _buttonRect.sizeDelta;
            return new Vector2(Mathf.Max(1f, sizeDelta.x), Mathf.Max(1f, sizeDelta.y));
        }

        private void CleanupLegacyOutlineGlow()
        {
            var outlines = GetComponents<Outline>();
            for (int i = 0; i < outlines.Length; i++)
            {
                if (outlines[i] != null)
                    DestroyUnityObject(outlines[i]);
            }
        }

        private void ResetButtonScale()
        {
        }

        private void EnsurePulseEffectOnTarget()
        {
            if (!ensurePulseEffect || _buttonRect == null)
                return;

            if (_buttonRect.GetComponent<PlayButtonPulseEffect>() != null)
                return;

            _buttonRect.gameObject.AddComponent<PlayButtonPulseEffect>();
        }

        private void CleanupAllGlowRigsInScope()
        {
            CleanupGlowRigOn(_buttonRect);

            if (_buttonRect != null)
                CleanupGlowRigOn(_buttonRect.parent as RectTransform);

            _glowRigRect = null;
            _outerGlowImage = null;
            _beamGlowImage = null;
            _coreGlowImage = null;
        }

        private void CleanupGlowRigOn(RectTransform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null && child.name == GlowRigName)
                    DestroyUnityObject(child.gameObject);
            }
        }

        private Image EnsureLayerImage(RectTransform parent, string layerName, Sprite sprite)
        {
            Transform existing = parent.Find(layerName);
            Image image = existing != null ? existing.GetComponent<Image>() : null;

            if (image == null)
            {
                var go = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = gameObject.layer;
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }

            image.sprite = sprite;
            image.raycastTarget = false;
            image.maskable = false;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;

            return image;
        }

        private static Vector2 ClampScale(Vector2 scale)
        {
            return new Vector2(Mathf.Max(0.05f, scale.x), Mathf.Max(0.05f, scale.y));
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private static Sprite GetGlowSprite()
        {
            if (s_glowSprite == null)
                s_glowSprite = CreateGlowSprite(256, 256, 1.55f, 1f);

            return s_glowSprite;
        }

        private static Sprite GetRingGlowSprite()
        {
            if (s_ringGlowSprite == null)
                s_ringGlowSprite = CreateRingGlowSprite(256, 256, 0.46f, 0.14f, 0.2f);

            return s_ringGlowSprite;
        }

        private static Sprite GetBeamSprite()
        {
            if (s_beamSprite == null)
                s_beamSprite = CreateBeamSprite(256, 128);

            return s_beamSprite;
        }

        private static Sprite CreateGlowSprite(int width, int height, float exponent, float alphaScale)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radius = Mathf.Min(width, height) * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = Mathf.Clamp01(1f - (Vector2.Distance(new Vector2(x, y), center) / radius));
                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, Mathf.Pow(alpha, exponent) * alphaScale);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateBeamSprite(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            float halfWidth = (width - 1) * 0.5f;
            float halfHeight = (height - 1) * 0.5f;

            for (int y = 0; y < height; y++)
            {
                float vertical = Mathf.Abs((y - halfHeight) / halfHeight);
                float verticalFade = Mathf.Pow(Mathf.Clamp01(1f - vertical), 2.8f);

                for (int x = 0; x < width; x++)
                {
                    float horizontal = Mathf.Abs((x - halfWidth) / halfWidth);
                    float horizontalFade = Mathf.Pow(Mathf.Clamp01(1f - horizontal), 1.6f);
                    float hotspot = Mathf.Pow(Mathf.Clamp01(1f - horizontal * 1.8f), 4.5f);
                    float alpha = Mathf.Clamp01((horizontalFade * verticalFade * 0.78f) + (hotspot * verticalFade * 0.22f));
                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateRingGlowSprite(int width, int height, float innerRadius, float innerSoftness, float outerSoftness)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radius = Mathf.Min(width, height) * 0.5f;
            float innerEnd = Mathf.Clamp01(innerRadius + innerSoftness);
            float outerStart = Mathf.Clamp01(1f - outerSoftness);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalized = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float inner = Mathf.SmoothStep(innerRadius, innerEnd, normalized);
                    float outer = 1f - Mathf.SmoothStep(outerStart, 1f, normalized);
                    float alpha = Mathf.Clamp01(inner * outer);
                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
