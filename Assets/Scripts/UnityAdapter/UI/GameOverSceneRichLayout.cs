using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UnityAdapter.UI
{
    internal static class GameOverSceneRichLayout
    {
        private const string RootName = "RichLayoutRoot";
        private const string MarkerName = "MenuThemeV3";

        public static void EnsureBuilt(GameObject panel)
        {
            if (panel == null)
                return;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect == null)
                return;

            Stretch(panelRect);
            DisableLegacyChildren(panel.transform);

            Transform existing = panel.transform.Find(RootName);
            if (existing != null && existing.Find(MarkerName) != null)
            {
                existing.gameObject.SetActive(true);
                existing.SetAsLastSibling();
                return;
            }

            if (existing != null)
            {
                existing.name = RootName + "_Legacy";
                existing.gameObject.SetActive(false);
            }

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            RectTransform root = CreateRect(RootName, panel.transform);
            Stretch(root);
            CreateRect(MarkerName, root).gameObject.SetActive(false);

            BuildBackdrop(root);
            BuildContent(root, font);
        }

        private static void BuildBackdrop(RectTransform root)
        {
            CreateImage("Backdrop", root, GameOverThemeArt.BackgroundSprite, Color.white, false);

            RectTransform topGlow = CreateRect("TopGlow", root);
            SetRect(topGlow, 0.5f, 1f, 980f, 760f, 0f, -40f);
            CreateImage("Fill", topGlow, GameOverThemeArt.GlowSprite, new Color(1f, 0.75f, 0.24f, 0.2f), false);

            RectTransform midGlow = CreateRect("MidGlow", root);
            SetRect(midGlow, 0.5f, 0.52f, 1040f, 1320f, 0f, 0f);
            CreateImage("Fill", midGlow, GameOverThemeArt.GlowSprite, new Color(0.22f, 0.95f, 1f, 0.16f), false);

            RectTransform leftBeam = CreateRect("LeftBeam", root);
            SetRect(leftBeam, 0.5f, 1f, 320f, 980f, -170f, -40f);
            leftBeam.localRotation = Quaternion.Euler(0f, 0f, 10f);
            CreateImage("Fill", leftBeam, GameOverThemeArt.BeamSprite, new Color(1f, 1f, 1f, 0.11f), false);

            RectTransform rightBeam = CreateRect("RightBeam", root);
            SetRect(rightBeam, 0.5f, 1f, 320f, 980f, 170f, -40f);
            rightBeam.localRotation = Quaternion.Euler(0f, 0f, -10f);
            CreateImage("Fill", rightBeam, GameOverThemeArt.BeamSprite, new Color(1f, 1f, 1f, 0.11f), false);

            BuildCube(root, new Vector2(-416f, -596f), 162f, -12f, new Color(0.18f, 0.94f, 1f, 0.95f), 0.28f);
            BuildCube(root, new Vector2(420f, -618f), 228f, 9f, new Color(1f, 0.68f, 0.08f, 1f), 0.3f);
            BuildCube(root, new Vector2(-452f, 706f), 126f, 18f, new Color(0.78f, 0.24f, 1f, 0.92f), 0.2f);
            BuildCube(root, new Vector2(432f, 688f), 114f, -14f, new Color(1f, 0.48f, 0.12f, 0.92f), 0.18f);
            BuildCube(root, new Vector2(-344f, 470f), 84f, -18f, new Color(0.22f, 0.88f, 1f, 0.78f), 0.12f);
            BuildCube(root, new Vector2(356f, 420f), 78f, 16f, new Color(1f, 0.42f, 0.82f, 0.72f), 0.1f);
        }

        private static void BuildContent(RectTransform root, TMP_FontAsset font)
        {
            RectTransform content = CreateRect("ContentRoot", root);
            SetRect(content, 0.5f, 0.5f, 860f, 1600f, 0f, -12f);

            BuildTitle(content, font);
            BuildScore(content, font);
            BuildStats(content, font);
            BuildButton(content, font, "RestartButton", "TEKRAR OYNA", "\u21BB", new Vector2(0f, -524f), new Vector2(650f, 148f), new Color(1f, 0.78f, 0.1f), new Color(0.66f, 0.18f, 0.03f));
            BuildButton(content, font, "MainMenuButton", "ANA MENU", "\u2302", new Vector2(0f, -694f), new Vector2(548f, 108f), new Color(0.12f, 0.67f, 1f), new Color(0.06f, 0.24f, 0.58f));
        }

        private static void BuildTitle(RectTransform parent, TMP_FontAsset font)
        {
            RectTransform shadow = CreateRect("TitleShadow", parent);
            SetRect(shadow, 0.5f, 1f, 610f, 328f, 0f, -72f);
            CreateImage("Fill", shadow, GameOverThemeArt.PanelSprite, new Color(0f, 0f, 0f, 0.4f), false, true);

            RectTransform plate = CreateRect("TitlePlate", parent);
            SetRect(plate, 0.5f, 1f, 586f, 310f, 0f, -56f);
            CreateImage("Fill", plate, GameOverThemeArt.PanelSprite, new Color(1f, 0.54f, 0.06f), false, true);

            RectTransform inner = CreateRect("Inner", plate);
            SetRect(inner, 0.5f, 0.5f, 528f, 250f, 0f, -4f);
            CreateImage("Fill", inner, GameOverThemeArt.PanelSprite, new Color(1f, 0.63f, 0.1f), false, true);

            TextMeshProUGUI brand = CreateText("BrandText", plate, "BLOK DUNYASI", font, 86f, new Color(1f, 0.98f, 0.42f), FontStyles.Bold, TextAlignmentOptions.Center, true, 58f, 92f);
            SetRect(brand.rectTransform, 0.5f, 1f, 494f, 120f, 0f, -82f);
            AddShadow(brand, new Color(0.46f, 0.08f, 0.01f, 0.9f), new Vector2(0f, -8f));
            AddOutline(brand, new Color(0.72f, 0.18f, 0.02f, 0.9f), new Vector2(4f, -4f));

            RectTransform ribbon = CreateRect("StatusRibbon", plate);
            SetRect(ribbon, 0.5f, 0f, 438f, 92f, 0f, 42f);
            BuildRibbon(ribbon, new Color(1f, 0.68f, 0.12f), new Color(0.72f, 0.14f, 0.02f));

            TextMeshProUGUI gameOver = CreateText("GameOverText", ribbon, "OYUN BITTI", font, 50f, new Color(0.34f, 0.1f, 0.04f), FontStyles.Bold, TextAlignmentOptions.Center, true, 34f, 54f);
            Stretch(gameOver.rectTransform);
            AddShadow(gameOver, new Color(1f, 0.94f, 0.76f, 0.3f), new Vector2(0f, 2f));
        }

        private static void BuildScore(RectTransform parent, TMP_FontAsset font)
        {
            TextMeshProUGUI label = CreateText("ScoreLabel", parent, "SKOR", font, 42f, new Color(0.98f, 0.99f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(label.rectTransform, 0.5f, 1f, 280f, 54f, 0f, -404f);
            AddShadow(label, new Color(0f, 0f, 0f, 0.34f), new Vector2(0f, -4f));

            RectTransform ribbon = CreateRect("ScoreRibbon", parent);
            SetRect(ribbon, 0.5f, 1f, 540f, 126f, 0f, -488f);
            BuildRibbon(ribbon, new Color(1f, 0.78f, 0.1f), new Color(0.84f, 0.24f, 0.01f));

            TextMeshProUGUI score = CreateText("FinalScoreText", ribbon, "0", font, 108f, new Color(0.26f, 0.09f, 0.03f), FontStyles.Bold, TextAlignmentOptions.Center, true, 58f, 114f);
            Stretch(score.rectTransform);
            AddShadow(score, new Color(1f, 0.98f, 0.74f, 0.42f), new Vector2(0f, 4f));

            TextMeshProUGUI best = CreateText("BestScoreText", parent, "En iyi skor: 0", font, 40f, new Color(0.98f, 0.95f, 0.88f), FontStyles.Bold, TextAlignmentOptions.Center, true, 24f, 42f);
            SetRect(best.rectTransform, 0.5f, 1f, 620f, 56f, 0f, -592f);
            AddShadow(best, new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -4f));

            RectTransform newBest = CreateRect("NewBestBanner", parent);
            SetRect(newBest, 0.5f, 1f, 430f, 82f, 0f, -668f);
            BuildRibbon(newBest, new Color(0.98f, 0.44f, 0.8f), new Color(0.62f, 0.08f, 0.38f));

            TextMeshProUGUI newBestText = CreateText("NewBestText", newBest, "YENI REKOR", font, 38f, new Color(0.38f, 0.08f, 0.25f), FontStyles.Bold, TextAlignmentOptions.Center, true, 26f, 40f);
            Stretch(newBestText.rectTransform);
            AddShadow(newBestText, new Color(1f, 0.92f, 0.98f, 0.34f), new Vector2(0f, 2f));
        }

        private static void BuildStats(RectTransform parent, TMP_FontAsset font)
        {
            RectTransform root = CreateRect("StatsRoot", parent);
            SetRect(root, 0.5f, 1f, 730f, 360f, 0f, -770f);

            BuildStat(root, font, "BestMoveValueText", "M", "EN IYI HAMLE", -10f, new Color(0.08f, 0.52f, 0.94f), new Color(0.08f, 0.34f, 0.72f));
            BuildStat(root, font, "MaxComboValueText", "C", "MAX KOMBO", -104f, new Color(0.9f, 0.34f, 0.76f), new Color(0.6f, 0.12f, 0.42f));
            BuildStat(root, font, "TotalLinesValueText", "L", "TOPLAM CIZGI", -198f, new Color(0.16f, 0.82f, 0.3f), new Color(0.08f, 0.46f, 0.16f));
            BuildStat(root, font, "AverageMoveValueText", "A", "ORT / HAMLE", -292f, new Color(0.84f, 0.38f, 0.16f), new Color(0.54f, 0.2f, 0.08f));
        }

        private static void BuildStat(RectTransform parent, TMP_FontAsset font, string valueName, string badgeValue, string label, float y, Color rowColor, Color badgeColor)
        {
            RectTransform row = CreateRect(valueName + "_Row", parent);
            SetRect(row, 0.5f, 1f, 690f, 78f, 0f, y);
            Image rowImage = row.gameObject.AddComponent<Image>();
            rowImage.sprite = GameOverThemeArt.PanelSprite;
            rowImage.type = Image.Type.Sliced;
            rowImage.color = rowColor;
            rowImage.raycastTarget = false;

            Shadow rowShadow = row.gameObject.AddComponent<Shadow>();
            rowShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            rowShadow.effectDistance = new Vector2(0f, -5f);
            rowShadow.useGraphicAlpha = true;

            RectTransform badge = CreateRect("Badge", row);
            SetRect(badge, 0f, 0.5f, 56f, 56f, 42f, 0f, new Vector2(0.5f, 0.5f));
            CreateImage("Fill", badge, GameOverThemeArt.OrbSprite, badgeColor, false, true);

            TextMeshProUGUI badgeText = CreateText("BadgeLabel", badge, badgeValue, font, 28f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(badgeText.rectTransform);
            AddShadow(badgeText, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -2f));

            TextMeshProUGUI labelText = CreateText("LabelText", row, label, font, 31f, new Color(0.98f, 0.99f, 1f), FontStyles.Bold, TextAlignmentOptions.Left, true, 22f, 32f);
            SetRect(labelText.rectTransform, 0f, 0.5f, 382f, 42f, 88f, 0f, new Vector2(0f, 0.5f));

            TextMeshProUGUI valueText = CreateText(valueName, row, "0", font, 34f, new Color(1f, 0.97f, 0.84f), FontStyles.Bold, TextAlignmentOptions.Right, true, 24f, 36f);
            SetRect(valueText.rectTransform, 1f, 0.5f, 170f, 44f, -24f, 0f, new Vector2(1f, 0.5f));
            AddShadow(valueText, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -2f));
        }

        private static void BuildButton(RectTransform parent, TMP_FontAsset font, string objectName, string label, string glyph, Vector2 pos, Vector2 size, Color bodyColor, Color textShadow)
        {
            RectTransform buttonRect = CreateRect(objectName, parent);
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = size;
            buttonRect.anchoredPosition = pos;

            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.sprite = GameOverThemeArt.PanelSprite;
            image.type = Image.Type.Sliced;
            image.color = bodyColor;

            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.98f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Shadow shadow = buttonRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -8f);

            RectTransform gloss = CreateRect("Gloss", buttonRect);
            SetRect(gloss, 0.5f, 1f, size.x * 0.76f, size.y * 0.44f, 0f, -10f);
            CreateImage("Fill", gloss, GameOverThemeArt.GlowSprite, new Color(1f, 1f, 1f, 0.16f), false);

            RectTransform orb = CreateRect("IconOrb", buttonRect);
            SetRect(orb, 1f, 0.5f, size.y - 26f, size.y - 26f, -14f, 0f, new Vector2(1f, 0.5f));
            CreateImage("Fill", orb, GameOverThemeArt.OrbSprite, new Color(1f, 1f, 1f, 0.18f), false, true);

            TextMeshProUGUI icon = CreateText("IconText", orb, glyph, font, size.y * 0.26f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, true, 18f, size.y * 0.28f);
            Stretch(icon.rectTransform);
            AddShadow(icon, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -2f));

            TextMeshProUGUI text = CreateText("LabelText", buttonRect, label, font, size.y * 0.28f, new Color(0.02f, 0.05f, 0.16f), FontStyles.Bold, TextAlignmentOptions.Center, true, 18f, size.y * 0.3f);
            SetRect(text.rectTransform, 0.5f, 0.5f, size.x - 118f, size.y * 0.42f, -22f, 0f);
            AddShadow(text, new Color(1f, 1f, 1f, 0.24f), new Vector2(0f, 2f));
            AddOutline(text, textShadow, new Vector2(2f, -2f));
        }

        private static void BuildRibbon(RectTransform ribbon, Color bodyColor, Color outlineColor)
        {
            RectTransform left = CreateRect("LeftTail", ribbon);
            SetRect(left, 0.5f, 0.5f, 116f, ribbon.sizeDelta.y * 0.72f, -(ribbon.sizeDelta.x * 0.42f), 0f, new Vector2(1f, 0.5f));
            left.localRotation = Quaternion.Euler(0f, 0f, 5f);
            CreateImage("Fill", left, GameOverThemeArt.PanelSprite, bodyColor, false, true);

            RectTransform right = CreateRect("RightTail", ribbon);
            SetRect(right, 0.5f, 0.5f, 116f, ribbon.sizeDelta.y * 0.72f, ribbon.sizeDelta.x * 0.42f, 0f, new Vector2(0f, 0.5f));
            right.localRotation = Quaternion.Euler(0f, 0f, -5f);
            CreateImage("Fill", right, GameOverThemeArt.PanelSprite, bodyColor, false, true);

            Image center = CreateImage("Center", ribbon, GameOverThemeArt.PanelSprite, bodyColor, false, true);
            AddOutline(center, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.44f), new Vector2(2f, -2f));
        }

        private static void BuildCube(RectTransform parent, Vector2 pos, float size, float rotZ, Color color, float glowAlpha)
        {
            RectTransform root = CreateRect("DecorCube", parent);
            SetRect(root, 0.5f, 0.5f, size, size, pos.x, pos.y);
            root.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            RectTransform glow = CreateRect("Glow", root);
            SetRect(glow, 0.5f, 0.5f, size * 1.38f, size * 1.38f, 0f, 0f);
            CreateImage("Fill", glow, GameOverThemeArt.GlowSprite, new Color(color.r, color.g, color.b, glowAlpha), false);

            CreateImage("Cube", root, GameOverThemeArt.CubeSprite, color, false);
        }

        private static void DisableLegacyChildren(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name != RootName)
                    child.gameObject.SetActive(false);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget, bool sliced = false, bool attachRounded = true)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (sliced && attachRounded)
                AttachRoundedStyle(image, sprite == GameOverThemeArt.OrbSprite ? 0.5f : 0.22f, sprite == GameOverThemeArt.OrbSprite ? 0.32f : 0.18f);
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size, Color color, FontStyles style, TextAlignmentOptions align, bool auto = false, float min = 18f, float max = 72f)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            if (auto)
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = min;
                text.fontSizeMax = max;
            }
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetRect(RectTransform rect, float anchorX, float anchorY, float width, float height, float posX, float posY, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(posX, posY);
            rect.localScale = Vector3.one;
        }

        private static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void AttachRoundedStyle(Image image, float radius, float border)
        {
            if (image == null)
                return;

            var rounded = image.GetComponent<RoundedSpriteImage>();
            if (rounded == null)
                rounded = image.gameObject.AddComponent<RoundedSpriteImage>();

            rounded.CornerRadius = radius;
            rounded.SpriteBorder = border;
        }
    }

    internal static class InGameContinueOfferLayout
    {
        private const string RootName = "InGameContinueOfferRoot";
        private const string MarkerName = "InGameContinueOfferV1";

        public static void EnsureBuilt(GameObject panel)
        {
            if (panel == null)
                return;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect == null)
                return;

            Stretch(panelRect);
            panelRect.localScale = Vector3.one;
            CleanupPanelSurface(panel);

            Transform existing = panel.transform.Find(RootName);
            if (existing != null && existing.Find(MarkerName) != null)
            {
                DisableLegacyChildren(panel.transform);
                ApplyRoundedStylesToExisting(existing);
                existing.gameObject.SetActive(true);
                existing.SetAsLastSibling();
                return;
            }

            if (existing != null)
            {
                existing.name = RootName + "_Legacy";
                existing.gameObject.SetActive(false);
            }

            DisableLegacyChildren(panel.transform);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            RectTransform root = CreateRect(RootName, panel.transform);
            Stretch(root);
            CreateRect(MarkerName, root).gameObject.SetActive(false);

            BuildBackdrop(root);
            BuildCard(root, font);
            ApplyRoundedStylesToExisting(root);
        }

        private static void BuildBackdrop(RectTransform root)
        {
            Image bg = CreateImage("BackgroundLayer", root, null, new Color(0.01f, 0.03f, 0.08f, 0.76f), true);
            bg.raycastTarget = true;

            RectTransform topGlow = CreateRect("TopGlow", root);
            SetRect(topGlow, 0.5f, 0.5f, 860f, 920f, 0f, 120f);
            CreateImage("Fill", topGlow, GameOverThemeArt.GlowSprite, new Color(0.19f, 0.71f, 1f, 0.16f), false);

            RectTransform bottomGlow = CreateRect("BottomGlow", root);
            SetRect(bottomGlow, 0.5f, 0.5f, 920f, 1040f, 0f, -120f);
            CreateImage("Fill", bottomGlow, GameOverThemeArt.GlowSprite, new Color(0.12f, 0.9f, 0.95f, 0.08f), false);
        }

        private static void BuildCard(RectTransform root, TMP_FontAsset font)
        {
            RectTransform shadow = CreateRect("CardShadow", root);
            SetRect(shadow, 0.5f, 0.5f, 786f, 914f, 0f, -4f);
            CreateImage("Fill", shadow, GameOverThemeArt.PanelSprite, new Color(0f, 0f, 0f, 0.42f), false, true);

            RectTransform card = CreateRect("CardRoot", root);
            SetRect(card, 0.5f, 0.5f, 760f, 886f, 0f, 0f);
            Image cardImage = CreateImage("Fill", card, GameOverThemeArt.PanelSprite, new Color(0.08f, 0.12f, 0.2f, 0.97f), false, true);
            AddOutline(cardImage, new Color(0.19f, 0.31f, 0.46f, 0.92f), new Vector2(2f, -2f));

            RectTransform inner = CreateRect("Inner", card);
            SetRect(inner, 0.5f, 0.5f, 716f, 840f, 0f, 0f);
            Image innerImage = CreateImage("Fill", inner, GameOverThemeArt.PanelSprite, new Color(0.1f, 0.15f, 0.24f, 0.98f), false, true);
            AddOutline(innerImage, new Color(1f, 1f, 1f, 0.035f), new Vector2(1f, -1f));

            BuildHeader(inner, font);
            BuildTexts(inner, font);
            BuildHintPill(inner, font);
            BuildContinueButton(inner, font);
            BuildBottomButtons(inner, font);
        }

        private static void BuildHeader(RectTransform parent, TMP_FontAsset font)
        {
            RectTransform anchor = CreateRect("HeaderGroup", parent);
            SetRect(anchor, 0.5f, 1f, 260f, 180f, 0f, -56f, new Vector2(0.5f, 1f));

            BuildSpark(anchor, new Vector2(-92f, -48f), 14f);
            BuildSpark(anchor, new Vector2(94f, -38f), 16f);
            BuildSpark(anchor, new Vector2(-118f, -6f), 10f);
            BuildSpark(anchor, new Vector2(116f, 8f), 10f);

            RectTransform face = CreateRect("LostFace", anchor);
            SetRect(face, 0.5f, 0.5f, 176f, 112f, 0f, -18f);

            Image faceBase = CreateImage("FaceBase", face, GameOverThemeArt.PanelSprite, new Color(0.33f, 0.42f, 0.58f, 0.96f), false, true);
            AddOutline(faceBase, new Color(0.56f, 0.68f, 0.86f, 0.28f), new Vector2(1f, -1f));

            RectTransform top = CreateRect("TopBlock", face);
            SetRect(top, 0.5f, 1f, 108f, 54f, 0f, 18f);
            CreateImage("Fill", top, GameOverThemeArt.PanelSprite, new Color(0.37f, 0.47f, 0.64f, 0.96f), false, true);

            TextMeshProUGUI leftEye = CreateText("LeftEye", face, "×", font, 34f, new Color(0.07f, 0.1f, 0.16f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(leftEye.rectTransform, 0.5f, 0.5f, 40f, 40f, -28f, 8f);

            TextMeshProUGUI rightEye = CreateText("RightEye", face, "×", font, 34f, new Color(0.07f, 0.1f, 0.16f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(rightEye.rectTransform, 0.5f, 0.5f, 40f, 40f, 28f, 8f);

            TextMeshProUGUI mouth = CreateText("Mouth", face, "⌒", font, 44f, new Color(0.07f, 0.1f, 0.16f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(mouth.rectTransform, 0.5f, 0.5f, 48f, 32f, 0f, -26f);
            mouth.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        private static void BuildTexts(RectTransform parent, TMP_FontAsset font)
        {
            TextMeshProUGUI title = CreateText("NoMovesText", parent, "Hamlen Kalmadı!", font, 72f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, true, 42f, 72f);
            SetRect(title.rectTransform, 0.5f, 1f, 620f, 92f, 0f, -262f, new Vector2(0.5f, 1f));
            AddShadow(title, new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -4f));

            TextMeshProUGUI subtitle = CreateText("ContinueCountdownText", parent, "Reklam izlemek için 5 saniyen var", font, 34f, new Color(0.78f, 0.85f, 0.92f), FontStyles.Normal, TextAlignmentOptions.Center, true, 24f, 36f);
            SetRect(subtitle.rectTransform, 0.5f, 1f, 620f, 52f, 0f, -334f, new Vector2(0.5f, 1f));
        }

        private static void BuildHintPill(RectTransform parent, TMP_FontAsset font)
        {
            RectTransform pill = CreateRect("OfferHintPill", parent);
            SetRect(pill, 0.5f, 1f, 478f, 66f, 0f, -414f, new Vector2(0.5f, 1f));

            Image pillImage = CreateImage("Fill", pill, GameOverThemeArt.PanelSprite, new Color(0.12f, 0.21f, 0.31f, 0.96f), false, true);
            AddOutline(pillImage, new Color(0.24f, 0.74f, 0.93f, 0.34f), new Vector2(1f, -1f));

            TextMeshProUGUI icon = CreateText("OfferHintIcon", pill, "▶", font, 28f, new Color(1f, 0.86f, 0.35f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(icon.rectTransform, 0f, 0.5f, 40f, 40f, 32f, 0f, new Vector2(0.5f, 0.5f));

            TextMeshProUGUI text = CreateText("OfferHintText", pill, "5 saniye dolmadan reklam izle!", font, 26f, new Color(0.84f, 0.92f, 1f), FontStyles.Normal, TextAlignmentOptions.Center, true, 18f, 28f);
            SetRect(text.rectTransform, 0.5f, 0.5f, 360f, 34f, 18f, 0f);

            TextMeshProUGUI spark = CreateText("OfferHintSpark", pill, "✦", font, 24f, new Color(0.96f, 0.9f, 0.38f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(spark.rectTransform, 1f, 0.5f, 28f, 28f, -24f, 0f, new Vector2(0.5f, 0.5f));
        }

        private static void BuildContinueButton(RectTransform parent, TMP_FontAsset font)
        {
            RectTransform buttonRect = CreateRect("ContinueButton", parent);
            SetRect(buttonRect, 0.5f, 1f, 648f, 154f, 0f, -548f, new Vector2(0.5f, 1f));

            Image image = CreateImage("Fill", buttonRect, GameOverThemeArt.PanelSprite, new Color(0.1f, 0.47f, 1f, 1f), false, true, false);
            AddOutline(image, new Color(0.49f, 0.78f, 1f, 0.42f), new Vector2(2f, -2f));

            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.97f, 1f, 1f);
            colors.pressedColor = new Color(0.76f, 0.86f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.45f);
            colors.colorMultiplier = 1.08f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Shadow shadow = buttonRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.06f, 0.18f, 0.42f);
            shadow.effectDistance = new Vector2(0f, -12f);
            shadow.useGraphicAlpha = true;

            RectTransform gloss = CreateRect("Gloss", buttonRect);
            SetRect(gloss, 0.5f, 1f, 560f, 54f, 0f, -18f, new Vector2(0.5f, 1f));
            CreateImage("Fill", gloss, GameOverThemeArt.GlowSprite, new Color(1f, 1f, 1f, 0.18f), false);

            RectTransform iconWrap = CreateRect("IconWrap", buttonRect);
            SetRect(iconWrap, 0f, 0.5f, 124f, 124f, 36f, 0f, new Vector2(0f, 0.5f));
            CreateImage("Fill", iconWrap, GameOverThemeArt.OrbSprite, new Color(1f, 1f, 1f, 0.12f), false, true);

            TextMeshProUGUI icon = CreateText("IconText", iconWrap, "▶", font, 58f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(icon.rectTransform);
            AddShadow(icon, new Color(0.01f, 0.08f, 0.2f, 0.4f), new Vector2(0f, -3f));

            TextMeshProUGUI label = CreateText("LabelText", buttonRect, "Reklam İzle", font, 58f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, true, 32f, 58f);
            SetRect(label.rectTransform, 0.5f, 0.5f, 380f, 54f, 54f, -10f);
            AddShadow(label, new Color(0.01f, 0.08f, 0.2f, 0.42f), new Vector2(0f, -3f));

            TextMeshProUGUI reward = CreateText("RewardText", buttonRect, "5 SN", font, 34f, new Color(0.8f, 0.92f, 1f), FontStyles.Bold, TextAlignmentOptions.Center, true, 20f, 34f);
            SetRect(reward.rectTransform, 0.5f, 0.5f, 280f, 38f, 54f, 36f);
            AddShadow(reward, new Color(0.01f, 0.08f, 0.2f, 0.32f), new Vector2(0f, -2f));
        }

        private static void BuildBottomButtons(RectTransform parent, TMP_FontAsset font)
        {
            BuildSmallButton(parent, font, "RestartButton", "↻", "Tekrar Oyna", new Vector2(-162f, -750f));
            BuildSmallButton(parent, font, "MainMenuButton", "⌂", "Ana Menü", new Vector2(162f, -750f));
        }

        private static void BuildSmallButton(RectTransform parent, TMP_FontAsset font, string name, string iconValue, string labelValue, Vector2 anchoredPos)
        {
            RectTransform buttonRect = CreateRect(name, parent);
            SetRect(buttonRect, 0.5f, 1f, 286f, 94f, anchoredPos.x, anchoredPos.y, new Vector2(0.5f, 1f));

            Image image = CreateImage("Fill", buttonRect, GameOverThemeArt.PanelSprite, new Color(0.07f, 0.15f, 0.22f, 0.98f), false, true);
            AddOutline(image, new Color(0.18f, 0.71f, 0.95f, 0.28f), new Vector2(1f, -1f));

            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.86f, 0.94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.45f);
            colors.colorMultiplier = 1.06f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Shadow shadow = buttonRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -8f);
            shadow.useGraphicAlpha = true;

            TextMeshProUGUI icon = CreateText("IconText", buttonRect, iconValue, font, 34f, new Color(0.24f, 0.9f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(icon.rectTransform, 0f, 0.5f, 52f, 40f, 38f, 0f, new Vector2(0.5f, 0.5f));

            TextMeshProUGUI label = CreateText("LabelText", buttonRect, labelValue, font, 28f, new Color(0.88f, 0.94f, 1f), FontStyles.Bold, TextAlignmentOptions.Left, true, 16f, 28f);
            SetRect(label.rectTransform, 0f, 0.5f, 170f, 34f, 84f, 0f, new Vector2(0f, 0.5f));
        }

        private static void BuildSpark(RectTransform parent, Vector2 pos, float size)
        {
            RectTransform spark = CreateRect("Spark", parent);
            SetRect(spark, 0.5f, 0.5f, size, size, pos.x, pos.y);
            TextMeshProUGUI text = CreateText("Glyph", spark, "✦", TMP_Settings.defaultFontAsset, size * 1.18f, new Color(0.62f, 0.78f, 1f, 0.62f), FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
        }

        private static void ApplyRoundedStylesToExisting(Transform root)
        {
            if (root == null)
                return;

            AttachRoundedToNamedFill(root, "CardRoot", 0.24f, 0.18f);
            AttachRoundedToNamedFill(root, "Inner", 0.2f, 0.16f);
            AttachRoundedToNamedFill(root, "OfferHintPill", 0.5f, 0.24f);
            RemoveRoundedFromNamedFill(root, "ContinueButton");
            AttachRoundedToNamedFill(root, "RestartButton", 0.18f, 0.16f);
            AttachRoundedToNamedFill(root, "MainMenuButton", 0.18f, 0.16f);
            AttachRoundedToNamedFill(root, "FaceBase", 0.18f, 0.14f, directObject: true);
            AttachRoundedToNamedFill(root, "TopBlock", 0.18f, 0.14f, directObject: true);
            AttachRoundedToNamedFill(root, "IconWrap", 0.5f, 0.28f, directObject: true);
        }

        private static void RemoveRoundedFromNamedFill(Transform root, string objectName, bool directObject = false)
        {
            Transform target = FindDeepChild(root, objectName);
            if (target == null)
                return;

            Transform imageTarget = directObject ? target : target.Find("Fill");
            if (imageTarget == null)
                return;

            var rounded = imageTarget.GetComponent<RoundedSpriteImage>();
            if (rounded != null)
                DestroyObject(rounded);
        }

        private static void AttachRoundedToNamedFill(Transform root, string objectName, float radius, float border, bool directObject = false)
        {
            Transform target = FindDeepChild(root, objectName);
            if (target == null)
                return;

            Image image = null;
            if (directObject)
            {
                image = target.GetComponent<Image>();
            }
            else
            {
                Transform fill = target.Find("Fill");
                image = fill != null ? fill.GetComponent<Image>() : target.GetComponent<Image>();
            }

            AttachRoundedStyle(image, radius, border);
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void DisableLegacyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.name != RootName)
                    DestroyObject(child.gameObject);
            }
        }

        private static void CleanupPanelSurface(GameObject panel)
        {
            if (panel == null)
                return;

            var image = panel.GetComponent<Image>();
            if (image != null)
                DestroyObject(image);

            var shadow = panel.GetComponent<Shadow>();
            if (shadow != null)
                DestroyObject(shadow);

            var outline = panel.GetComponent<Outline>();
            if (outline != null)
                DestroyObject(outline);
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget, bool sliced = false, bool attachRounded = true)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (sliced && attachRounded)
                AttachRoundedStyle(image, sprite == GameOverThemeArt.OrbSprite ? 0.5f : 0.22f, sprite == GameOverThemeArt.OrbSprite ? 0.32f : 0.18f);
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size, Color color, FontStyles style, TextAlignmentOptions align, bool auto = false, float min = 18f, float max = 72f)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = align;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (auto)
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = min;
                text.fontSizeMax = max;
            }

            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetRect(RectTransform rect, float anchorX, float anchorY, float width, float height, float posX, float posY, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(posX, posY);
            rect.localScale = Vector3.one;
        }

        private static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            Outline outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void AttachRoundedStyle(Image image, float radius, float border)
        {
            if (image == null)
                return;

            var rounded = image.GetComponent<RoundedSpriteImage>();
            if (rounded == null)
                rounded = image.gameObject.AddComponent<RoundedSpriteImage>();

            rounded.CornerRadius = radius;
            rounded.SpriteBorder = border;
        }
    }

    internal static class GameOverThemeArt
    {
        private static Sprite _background;
        private static Sprite _glow;
        private static Sprite _beam;
        private static Sprite _panel;
        private static Sprite _orb;
        private static Sprite _cube;

        public static Sprite BackgroundSprite => _background ??= CreateBackground();
        public static Sprite GlowSprite => _glow ??= CreateGlow(256, 256, 2.5f, 1f);
        public static Sprite BeamSprite => _beam ??= CreateBeam();
        public static Sprite PanelSprite => _panel ??= CreateRounded(256, 256, 56, 48);
        public static Sprite OrbSprite => _orb ??= CreateRounded(256, 256, 128, 64);
        public static Sprite CubeSprite => _cube ??= CreateCube();

        private static Sprite CreateBackground()
        {
            const int w = 540;
            const int h = 960;
            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[w * h];
            System.Random rng = new System.Random(20260323);
            Vector2 center = new Vector2(w * 0.5f, h * 0.56f);

            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                Color vertical = Color.Lerp(new Color(0.14f, 0.08f, 0.72f), new Color(0.32f, 0.12f, 0.9f), Mathf.SmoothStep(0f, 1f, v));
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    float edge = Mathf.Pow(Mathf.Abs((u - 0.5f) * 2f), 2.4f) * 0.28f;
                    Color c = Color.Lerp(vertical, new Color(0.39f, 0.04f, 0.55f), edge);
                    float dx = (x - center.x) / (w * 0.48f);
                    float dy = (y - center.y) / (h * 0.34f);
                    float radial = Mathf.Clamp01(1f - Mathf.Sqrt((dx * dx) + (dy * dy)));
                    radial = Mathf.Pow(radial, 2.1f);
                    float beam = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs((u - 0.5f) * 2.1f)), 3.8f) * Mathf.SmoothStep(0.2f, 1f, v) * 0.22f;
                    c = Color.Lerp(c, new Color(0.18f, 0.84f, 1f), radial * 0.76f);
                    px[(y * w) + x] = Color.Lerp(c, Color.white, beam);
                }
            }

            for (int i = 0; i < 48; i++)
                AddCircle(px, w, h, (float)rng.NextDouble() * w, (float)rng.NextDouble() * h, Mathf.Lerp(16f, 62f, (float)rng.NextDouble()), Color.Lerp(new Color(0.24f, 0.94f, 1f, 0.14f), new Color(1f, 0.4f, 0.9f, 0.14f), (float)rng.NextDouble()), 2.2f);
            for (int i = 0; i < 22; i++)
                AddCircle(px, w, h, (float)rng.NextDouble() * w, (float)rng.NextDouble() * h, Mathf.Lerp(3f, 8f, (float)rng.NextDouble()), new Color(1f, 1f, 1f, 0.32f), 1.2f);

            texture.SetPixels(px);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateGlow(int w, int h, float exponent, float alphaScale)
        {
            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[w * h];
            Vector2 center = new Vector2((w - 1) * 0.5f, (h - 1) * 0.5f);
            float max = Mathf.Min(w, h) * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = Mathf.Clamp01(1f - (Vector2.Distance(new Vector2(x, y), center) / max));
                    px[(y * w) + x] = new Color(1f, 1f, 1f, Mathf.Pow(a, exponent) * alphaScale);
                }
            texture.SetPixels(px);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateBeam()
        {
            const int w = 256;
            const int h = 512;
            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[w * h];
            float center = (w - 1) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                float width = Mathf.Lerp(w * 0.32f, w * 0.08f, v);
                for (int x = 0; x < w; x++)
                {
                    float a = Mathf.Pow(Mathf.Clamp01(1f - (Mathf.Abs(x - center) / width)), 2.6f) * Mathf.SmoothStep(0.18f, 1f, 1f - v) * 0.9f;
                    px[(y * w) + x] = new Color(1f, 1f, 1f, a);
                }
            }
            texture.SetPixels(px);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateRounded(int w, int h, int radius, int border)
        {
            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!Inside(x, y, w, h, radius))
                    {
                        px[(y * w) + x] = Color.clear;
                        continue;
                    }
                    float v = y / (float)(h - 1);
                    Color c = new Color(1f, 1f, 1f, 0.92f);
                    if (!Inside(x - border, y - border, w - (border * 2), h - (border * 2), Mathf.Max(0, radius - border)))
                        c = Color.Lerp(c, new Color(1f, 1f, 1f, 0.7f), 0.62f);
                    c = Color.Lerp(c, Color.white, Mathf.Clamp01((v - 0.62f) / 0.34f) * 0.2f);
                    float hot = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.22f, h * 0.82f)) / (w * 0.38f));
                    px[(y * w) + x] = Color.Lerp(c, Color.white, hot * 0.22f);
                }
            texture.SetPixels(px);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateCube()
        {
            const int w = 192;
            const int h = 192;
            Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[w * h];
            float radius = w * 0.18f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!Inside(x, y, w, h, Mathf.RoundToInt(radius)))
                    {
                        px[(y * w) + x] = Color.clear;
                        continue;
                    }
                    Color c = new Color(1f, 1f, 1f, 0.92f);
                    float diag = Mathf.Clamp01(1f - ((x + (h - y)) / (float)(w + h)));
                    c = Color.Lerp(c, Color.white, diag * 0.28f);
                    float hot = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(w * 0.28f, h * 0.78f)) / (w * 0.55f));
                    px[(y * w) + x] = Color.Lerp(c, Color.white, hot * 0.34f);
                }
            texture.SetPixels(px);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static bool Inside(int x, int y, int w, int h, int radius)
        {
            if (w <= 0 || h <= 0)
                return false;
            float hw = w * 0.5f;
            float hh = h * 0.5f;
            float dx = Mathf.Abs(x - hw) - (hw - radius);
            float dy = Mathf.Abs(y - hh) - (hh - radius);
            if (dx <= 0f || dy <= 0f)
                return true;
            return (dx * dx) + (dy * dy) <= (radius * radius);
        }

        private static void AddCircle(Color[] px, int w, int h, float cx, float cy, float radius, Color color, float exponent)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(cx + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(cy + radius));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist > radius)
                        continue;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - (dist / radius)), exponent) * color.a;
                    int index = (y * w) + x;
                    px[index] = Color.Lerp(px[index], new Color(color.r, color.g, color.b, 1f), a);
                }
        }
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed partial class RoundedSpriteImage : MonoBehaviour
    {
        [SerializeField] [Range(0f, 0.5f)] private float cornerRadius = 0.22f;
        [SerializeField] [Range(0f, 0.5f)] private float spriteBorder = 0.18f;
        [SerializeField] [Min(32)] private int textureSize = 256;
        [SerializeField] private bool autoApply = true;

        private static readonly System.Collections.Generic.Dictionary<string, Sprite> SpriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();
        private Image _image;

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                cornerRadius = Mathf.Clamp01(value);
                Apply();
            }
        }

        public float SpriteBorder
        {
            get => spriteBorder;
            set
            {
                spriteBorder = Mathf.Clamp01(value);
                Apply();
            }
        }

        public void Apply()
        {
            EnsureImage();
            if (_image == null)
                return;

            int size = Mathf.Max(32, textureSize);
            float radius = Mathf.Clamp01(cornerRadius);
            float border = Mathf.Clamp01(spriteBorder);
            string key = $"{size}_{Mathf.RoundToInt(radius * 1000f)}_{Mathf.RoundToInt(border * 1000f)}";

            if (!SpriteCache.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = BuildSprite(size, radius, border);
                SpriteCache[key] = sprite;
            }

            _image.sprite = sprite;
            _image.type = Image.Type.Sliced;
            _image.preserveAspect = false;
        }

        private void Awake()
        {
            EnsureImage();
            if (autoApply)
                Apply();
        }

        private void OnEnable()
        {
            if (autoApply)
                Apply();
        }

        private void OnValidate()
        {
            if (autoApply)
                Apply();
        }

        private void EnsureImage()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }

        private static Sprite BuildSprite(int size, float normalizedRadius, float normalizedBorder)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = $"RoundedSprite_{size}_{Mathf.RoundToInt(normalizedRadius * 1000f)}_{Mathf.RoundToInt(normalizedBorder * 1000f)}";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[size * size];
            float radius = Mathf.Clamp01(normalizedRadius) * size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = GetRoundedAlpha(x + 0.5f, y + 0.5f, size, radius);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            int borderPx = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(normalizedBorder) * size * 0.5f), 1, Mathf.Max(1, size / 2));
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(borderPx, borderPx, borderPx, borderPx));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static float GetRoundedAlpha(float x, float y, int size, float radius)
        {
            float min = 0.5f;
            float max = size - 0.5f;
            float closestX = Mathf.Clamp(x, min + radius, max - radius);
            float closestY = Mathf.Clamp(y, min + radius, max - radius);
            float dx = x - closestX;
            float dy = y - closestY;
            float distance = Mathf.Sqrt((dx * dx) + (dy * dy));

            if (distance <= radius - 1f)
                return 1f;

            return Mathf.Clamp01(radius - distance);
        }
    }
}
