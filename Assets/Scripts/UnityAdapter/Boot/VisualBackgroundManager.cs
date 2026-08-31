using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.Boot
{
    public sealed class VisualBackgroundManager
    {
        private readonly Transform _transform;
        private readonly bool _useWorldBackgroundLayer;
        private readonly bool _preserveAuthoredWorldBackground;
        private Sprite _gameplayBackgroundSpriteOverride;
        private Color _gameplayBackgroundTint;
        private Color _gameplayBackgroundDimmerColor;
        private Color _gameplayCameraClearColor;
        private readonly float _legacyOverlayBackgroundAlpha;
        private readonly int _worldBackgroundSortingOrder;
        private readonly int _worldDimmerSortingOrder;
        private readonly string _legacyOverlayBackgroundName;

        private SpriteRenderer _worldBackgroundRenderer;
        private SpriteRenderer _worldDimmerRenderer;
        private Image _legacyOverlayBackground;
        private Sprite _generatedBackdropSprite;

        public VisualBackgroundManager(
            Transform transform,
            bool useWorldBackgroundLayer,
            bool preserveAuthoredWorldBackground,
            Sprite gameplayBackgroundSpriteOverride,
            Color gameplayBackgroundTint,
            Color gameplayBackgroundDimmerColor,
            Color gameplayCameraClearColor,
            float legacyOverlayBackgroundAlpha,
            int worldBackgroundSortingOrder,
            int worldDimmerSortingOrder,
            string legacyOverlayBackgroundName)
        {
            _transform = transform;
            _useWorldBackgroundLayer = useWorldBackgroundLayer;
            _preserveAuthoredWorldBackground = preserveAuthoredWorldBackground;
            _gameplayBackgroundSpriteOverride = gameplayBackgroundSpriteOverride;
            _gameplayBackgroundTint = gameplayBackgroundTint;
            _gameplayBackgroundDimmerColor = gameplayBackgroundDimmerColor;
            _gameplayCameraClearColor = gameplayCameraClearColor;
            _legacyOverlayBackgroundAlpha = legacyOverlayBackgroundAlpha;
            _worldBackgroundSortingOrder = worldBackgroundSortingOrder;
            _worldDimmerSortingOrder = worldDimmerSortingOrder;
            _legacyOverlayBackgroundName = legacyOverlayBackgroundName;
        }

        public void NormalizeGameplayCamera(Camera camera)
        {
            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = _gameplayCameraClearColor;
            camera.nearClipPlane = 0.3f;
        }

        public void SetThemeBackground(Sprite background, Color tint, Color dimmer, Color clearColor)
        {
            _gameplayBackgroundSpriteOverride = background;
            _gameplayBackgroundTint = tint;
            _gameplayBackgroundDimmerColor = dimmer;
            _gameplayCameraClearColor = clearColor;
        }

        public void ApplyVisualReadability(Camera camera)
        {
            if (!_useWorldBackgroundLayer || camera == null)
                return;

            Image overlayBackground = ResolveLegacyOverlayBackground();
            Sprite existingBackgroundSprite = _worldBackgroundRenderer != null ? _worldBackgroundRenderer.sprite : null;
            bool hasThemeBackgroundOverride = _gameplayBackgroundSpriteOverride != null;
            bool keepAuthoredBackground = _preserveAuthoredWorldBackground && !hasThemeBackgroundOverride && existingBackgroundSprite != null;
            Sprite backgroundSprite = hasThemeBackgroundOverride
                ? _gameplayBackgroundSpriteOverride
                : keepAuthoredBackground
                    ? existingBackgroundSprite
                    : overlayBackground != null
                        ? overlayBackground.sprite
                        : null;

            if (overlayBackground != null)
            {
                Color overlayColor = overlayBackground.color;
                if (!Mathf.Approximately(overlayColor.a, _legacyOverlayBackgroundAlpha))
                    overlayColor.a = _legacyOverlayBackgroundAlpha;

                overlayBackground.color = overlayColor;
                overlayBackground.raycastTarget = false;
            }

            if (backgroundSprite == null)
                return;

            SpriteRenderer backgroundRenderer = GetOrCreateBackdropRenderer(
                ref _worldBackgroundRenderer,
                "SceneBackground",
                backgroundSprite,
                _worldBackgroundSortingOrder);
            if (!keepAuthoredBackground)
                backgroundRenderer.color = _gameplayBackgroundTint;
            backgroundRenderer.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 10f);
            ScaleRendererToCamera(backgroundRenderer, camera, preserveAspectCover: true);

            bool hasAuthoredDimmer = _preserveAuthoredWorldBackground && _worldDimmerRenderer != null;
            SpriteRenderer dimmerRenderer = GetOrCreateBackdropRenderer(
                ref _worldDimmerRenderer,
                "SceneBackgroundDimmer",
                GetGeneratedBackdropSprite(),
                _worldDimmerSortingOrder);
            if (!hasAuthoredDimmer)
                dimmerRenderer.color = _gameplayBackgroundDimmerColor;
            dimmerRenderer.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 9.5f);
            ScaleRendererToCamera(dimmerRenderer, camera, preserveAspectCover: false);
        }

        public void CleanupDuplicates()
        {
            _worldBackgroundRenderer = CleanupDuplicatesForName("SceneBackground", _worldBackgroundRenderer);
            _worldDimmerRenderer = CleanupDuplicatesForName("SceneBackgroundDimmer", _worldDimmerRenderer);
        }

        private SpriteRenderer CleanupDuplicatesForName(string objectName, SpriteRenderer preferred)
        {
            SpriteRenderer keptRenderer = preferred;

            for (int i = _transform.childCount - 1; i >= 0; i--)
            {
                Transform child = _transform.GetChild(i);
                if (child == null || child.name != objectName)
                    continue;

                var childRenderer = child.GetComponent<SpriteRenderer>();
                if (childRenderer == null)
                    continue;

                if (keptRenderer == null)
                {
                    keptRenderer = childRenderer;
                    continue;
                }

                if (childRenderer == keptRenderer)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }

            return keptRenderer;
        }

        private Image ResolveLegacyOverlayBackground()
        {
            if (_legacyOverlayBackground != null)
                return _legacyOverlayBackground;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return null;

            Transform candidate = canvas.transform.Find(_legacyOverlayBackgroundName);
            if (candidate == null)
                candidate = FindDeep(canvas.transform, _legacyOverlayBackgroundName);

            _legacyOverlayBackground = candidate != null ? candidate.GetComponent<Image>() : null;
            return _legacyOverlayBackground;
        }

        private SpriteRenderer GetOrCreateBackdropRenderer(ref SpriteRenderer renderer, string objectName, Sprite sprite, int sortingOrder)
        {
            if (renderer == null)
                renderer = FindOrCreateBackdropRenderer(objectName);

            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = true;
            return renderer;
        }

        private SpriteRenderer FindOrCreateBackdropRenderer(string objectName)
        {
            SpriteRenderer foundRenderer = null;

            for (int i = _transform.childCount - 1; i >= 0; i--)
            {
                Transform child = _transform.GetChild(i);
                if (child == null || child.name != objectName)
                    continue;

                var childRenderer = child.GetComponent<SpriteRenderer>();
                if (childRenderer == null)
                    continue;

                if (foundRenderer == null)
                {
                    foundRenderer = childRenderer;
                    continue;
                }

                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }

            if (foundRenderer != null)
                return foundRenderer;

            var go = new GameObject(objectName, typeof(SpriteRenderer));
            go.transform.SetParent(_transform, false);
            return go.GetComponent<SpriteRenderer>();
        }

        private void ScaleRendererToCamera(SpriteRenderer renderer, Camera camera, bool preserveAspectCover)
        {
            if (renderer == null || renderer.sprite == null || camera == null)
                return;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float worldHeight = camera.orthographicSize * 2f;
            float worldWidth = worldHeight * camera.aspect;

            if (preserveAspectCover)
            {
                float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
                renderer.transform.localScale = new Vector3(scale, scale, 1f);
                return;
            }

            renderer.transform.localScale = new Vector3(
                worldWidth / spriteSize.x,
                worldHeight / spriteSize.y,
                1f);
        }

        private Sprite GetGeneratedBackdropSprite()
        {
            if (_generatedBackdropSprite != null)
                return _generatedBackdropSprite;

            const int size = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GameplayBackdropRuntime";
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            _generatedBackdropSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _generatedBackdropSprite.name = "GameplayBackdropRuntimeSprite";
            _generatedBackdropSprite.hideFlags = HideFlags.HideAndDontSave;
            return _generatedBackdropSprite;
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nested = FindDeep(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
