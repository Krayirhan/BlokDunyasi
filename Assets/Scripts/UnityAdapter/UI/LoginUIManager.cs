using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core.Social;
using BlockPuzzle.UnityAdapter.Social;
using BlockPuzzle.UnityAdapter.UI.Localization;

namespace BlockPuzzle.UnityAdapter.UI
{
    public class LoginUIManager : MonoBehaviour
    {
        private static readonly Vector2 LogoutButtonSize = new Vector2(200f, 60f);

        [Header("Buttons")]
        public Button playLoginButton;
        public Button guestLoginButton;
        public Button logoutButton;

        private GameObject usernameDialog;
        private InputField usernameInput;
        private InputField passwordInput;
        private InputField confirmPasswordInput;
        private Text dialogMessage;
        private Text dialogTitle;
        private Button confirmButton;
        private Button closeButton;
        private Text loggedInUsernameText;
        private AuthDialogMode dialogMode;

        private enum AuthDialogMode
        {
            Register,
            Login
        }

        private void Start()
        {
            EnsureManagersExist();

            if (GooglePlayGamesManager.Instance != null)
            {
                GooglePlayGamesManager.Instance.OnStateChanged += UpdateUI;
            }

            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnFirebaseInitialized += UpdateUI;
                FirebaseManager.Instance.OnUserLogin += HandleFirebaseUserChanged;
            }

            playLoginButton?.onClick.AddListener(OnLoginClicked);
            guestLoginButton?.onClick.AddListener(OnRegisterClicked);
            logoutButton?.onClick.AddListener(OnLogoutClicked);

            UpdateUI();
        }

        private void OnRegisterClicked()
        {
            EnsureManagersExist();
            var firebase = FirebaseManager.Instance;
            if (firebase == null || !firebase.IsInitialized)
            {
                SetAuthButtonsState(false);
                return;
            }

            if (!string.IsNullOrEmpty(firebase.NormalizedUsername))
            {
                UpdateUI();
                return;
            }

            ShowAuthDialog(AuthDialogMode.Register);
        }

        private void OnLoginClicked()
        {
            EnsureManagersExist();
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
            {
                SetAuthButtonsState(false);
                return;
            }

            ShowAuthDialog(AuthDialogMode.Login);
        }

        private void OnDestroy()
        {
            if (GooglePlayGamesManager.Instance != null)
            {
                GooglePlayGamesManager.Instance.OnStateChanged -= UpdateUI;
            }

            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.OnFirebaseInitialized -= UpdateUI;
                FirebaseManager.Instance.OnUserLogin -= HandleFirebaseUserChanged;
            }

            playLoginButton?.onClick.RemoveListener(OnLoginClicked);
            guestLoginButton?.onClick.RemoveListener(OnRegisterClicked);
            logoutButton?.onClick.RemoveListener(OnLogoutClicked);
        }

        private void HandleFirebaseUserChanged(Firebase.Auth.FirebaseUser _)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            bool isReady = FirebaseManager.Instance != null && FirebaseManager.Instance.IsInitialized;
            bool hasRegisteredSession = FirebaseManager.Instance != null && FirebaseManager.Instance.HasCompletedGuestLogin;

            if (playLoginButton != null)
            {
                playLoginButton.gameObject.SetActive(!hasRegisteredSession);
                playLoginButton.interactable = isReady && !hasRegisteredSession;
                SetButtonText(playLoginButton, GetTranslation("Giriş Yap", "Log In", "로그인"));
            }

            if (guestLoginButton != null)
            {
                guestLoginButton.gameObject.SetActive(!hasRegisteredSession);
                guestLoginButton.interactable = isReady && !hasRegisteredSession;
                SetButtonText(guestLoginButton, GetTranslation("Kayıt Ol", "Register", "회원가입"));
            }

            if (logoutButton != null)
            {
                logoutButton.gameObject.SetActive(hasRegisteredSession);
                logoutButton.interactable = isReady && hasRegisteredSession;
                SetButtonText(logoutButton, GetTranslation("Çıkış Yap", "Log Out", "로그아웃"));
                NormalizeLogoutButtonLayout();
            }

            UpdateLoggedInUsernameText(hasRegisteredSession ? FirebaseManager.Instance.Username : null);
        }

        private void OnLogoutClicked()
        {
            EnsureManagersExist();

            if (FirebaseManager.Instance == null)
                return;

            FirebaseManager.Instance.SignOut();
            UpdateUI();
        }

        private void ShowAuthDialog(AuthDialogMode mode)
        {
            dialogMode = mode;
            if (usernameDialog != null)
            {
                usernameDialog.SetActive(true);
                ApplyDialogMode();
                usernameInput.Select();
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("UsernameDialogCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            usernameDialog = new GameObject("UsernameDialog", typeof(RectTransform), typeof(Image));
            usernameDialog.transform.SetParent(canvas.transform, false);

            var dialogRect = usernameDialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = Vector2.zero;
            dialogRect.anchorMax = Vector2.one;
            dialogRect.offsetMin = Vector2.zero;
            dialogRect.offsetMax = Vector2.zero;
            usernameDialog.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var panel = CreateRect("Panel", usernameDialog.transform, new Vector2(0.5f, 0.5f), new Vector2(616f, 572f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.11f, 1f);

            closeButton = CreateButton("CloseButton", panel, font, "X", HideAuthDialog);
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchoredPosition = new Vector2(264f, 242f);
            closeRect.sizeDelta = new Vector2(51f, 51f);

            dialogTitle = CreateText("Title", panel, font, string.Empty, 31, TextAnchor.MiddleCenter);
            dialogTitle.rectTransform.anchoredPosition = new Vector2(0f, 220f);
            dialogTitle.rectTransform.sizeDelta = new Vector2(550f, 55f);

            usernameInput = CreateInput(panel, font, GetTranslation("kullanici_adi", "username", "사용자 이름"), false);

            passwordInput = CreateInput(panel, font, GetTranslation("sifre", "password", "비밀번호"), true);

            confirmPasswordInput = CreateInput(panel, font, GetTranslation("sifreyi tekrar gir", "re-enter password", "비밀번호 재입력"), true);

            dialogMessage = CreateText("Message", panel, font, string.Empty, 20, TextAnchor.MiddleCenter);

            confirmButton = CreateButton("ConfirmButton", panel, font, GetTranslation("Devam Et", "Continue", "계속하기"), OnUsernameConfirmClicked);

            ApplyDialogMode();
            usernameInput.Select();
        }

        private void HideAuthDialog()
        {
            if (usernameDialog != null)
            {
                usernameDialog.SetActive(false);
            }
        }

        private void OnUsernameConfirmClicked()
        {
            var firebase = FirebaseManager.Instance;
            if (firebase == null) return;

            if (dialogMode == AuthDialogMode.Register && passwordInput.text != confirmPasswordInput.text)
            {
                dialogMessage.text = GetTranslation("Şifreler eşleşmedi.", "Passwords do not match.", "비밀번호가 일치하지 않습니다.");
                return;
            }

            confirmButton.interactable = false;
            usernameInput.interactable = false;
            passwordInput.interactable = false;
            if (confirmPasswordInput != null) confirmPasswordInput.interactable = false;
            dialogMessage.text = GetTranslation("Kontrol ediliyor...", "Checking...", "확인 중...");

            var callback = new System.Action<bool, string>((success, error) =>
            {
                confirmButton.interactable = true;
                usernameInput.interactable = true;
                passwordInput.interactable = true;
                if (confirmPasswordInput != null) confirmPasswordInput.interactable = true;

                if (!success)
                {
                    dialogMessage.text = error;
                    return;
                }

                usernameDialog.SetActive(false);
                UpdateUI();
            });

            if (dialogMode == AuthDialogMode.Register)
            {
                firebase.RegisterAccount(usernameInput.text, passwordInput.text, callback);
            }
            else
            {
                firebase.LoginAccount(usernameInput.text, passwordInput.text, callback);
            }
        }

        private void ApplyDialogMode()
        {
            if (dialogTitle == null || dialogMessage == null || confirmButton == null) return;

            bool isRegister = dialogMode == AuthDialogMode.Register;
            dialogTitle.text = isRegister ? GetTranslation("Kayıt Ol", "Register", "회원가입") : GetTranslation("Giriş Yap", "Log In", "로그인");
            dialogMessage.text = isRegister
                ? GetTranslation("3-16 karakter kullanıcı adı. Şifre en az 6 karakter.", "3-16 character username. Password at least 6 characters.", "사용자 이름 3~16자. 비밀번호 최소 6자.")
                : GetTranslation("Kullanıcı adı ve şifrenizi girin.", "Enter your username and password.", "사용자 이름과 비밀번호를 입력하세요.");
            SetButtonText(confirmButton, isRegister ? GetTranslation("Kayıt Ol", "Register", "회원가입") : GetTranslation("Giriş Yap", "Log In", "로그인"));
            if (passwordInput != null) passwordInput.text = string.Empty;
            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.text = string.Empty;
                confirmPasswordInput.gameObject.SetActive(isRegister);
            }

            if (usernameInput != null)
            {
                usernameInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, isRegister ? 127f : 94f);
                if (usernameInput.placeholder != null)
                {
                    var phText = usernameInput.placeholder.GetComponent<Text>();
                    if (phText != null) phText.text = GetTranslation("kullanici_adi", "username", "사용자 이름");
                }
            }

            if (passwordInput != null)
            {
                passwordInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, isRegister ? 50f : 17f);
                if (passwordInput.placeholder != null)
                {
                    var phText = passwordInput.placeholder.GetComponent<Text>();
                    if (phText != null) phText.text = GetTranslation("sifre", "password", "비밀번호");
                }
            }

            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -28f);
                if (confirmPasswordInput.placeholder != null)
                {
                    var phText = confirmPasswordInput.placeholder.GetComponent<Text>();
                    if (phText != null) phText.text = GetTranslation("sifreyi tekrar gir", "re-enter password", "비밀번호 재입력");
                }
            }

            dialogMessage.rectTransform.anchoredPosition = new Vector2(0f, isRegister ? -110f : -72f);
            confirmButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, isRegister ? -209f : -171f);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, Font font, string text, int size, TextAnchor alignment)
        {
            var rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(440f, 44f));
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            return label;
        }

        private static InputField CreateInput(Transform parent, Font font, string placeholderText, bool password)
        {
            var rect = CreateRect("UsernameInput", parent, new Vector2(0.5f, 0.5f), new Vector2(506f, 62f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;

            var text = CreateText("Text", rect, font, string.Empty, 26, TextAnchor.MiddleLeft);
            text.color = Color.black;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(18f, 0f);
            text.rectTransform.offsetMax = new Vector2(-18f, 0f);

            var placeholder = CreateText("Placeholder", rect, font, placeholderText, 26, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0.35f, 0.35f, 0.35f, 1f);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(18f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-18f, 0f);

            var input = rect.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = password ? 64 : 16;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            return input;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string text, UnityEngine.Events.UnityAction action)
        {
            var rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(286f, 64f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.48f, 0.95f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);

            var label = CreateText("Text", rect, font, text, 26, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private void SetAuthButtonsState(bool interactable)
        {
            if (playLoginButton != null)
            {
                playLoginButton.interactable = interactable;
            }

            if (guestLoginButton != null)
            {
                guestLoginButton.interactable = interactable;
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null) return;
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = value;
        }

        private void NormalizeLogoutButtonLayout()
        {
            if (logoutButton == null)
                return;

            var rect = logoutButton.transform as RectTransform;
            if (rect == null)
                return;

            rect.localScale = Vector3.one;
            rect.sizeDelta = LogoutButtonSize;

            var layoutElement = logoutButton.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minWidth = LogoutButtonSize.x;
                layoutElement.preferredWidth = LogoutButtonSize.x;
                layoutElement.flexibleWidth = 0f;
                layoutElement.minHeight = LogoutButtonSize.y;
                layoutElement.preferredHeight = LogoutButtonSize.y;
                layoutElement.flexibleHeight = 0f;
            }
        }

        private void UpdateLoggedInUsernameText(string username)
        {
            if (guestLoginButton == null && logoutButton == null)
            {
                return;
            }

            if (loggedInUsernameText == null)
            {
                loggedInUsernameText = CreateUsernameTextAtButton();
            }

            bool hasUsername = !string.IsNullOrEmpty(username);
            loggedInUsernameText.gameObject.SetActive(hasUsername);
            if (hasUsername)
            {
                UpdateUsernameLabelLayout();
                loggedInUsernameText.text = username;
            }
        }

        private Text CreateUsernameTextAtButton()
        {
            var sourceButton = logoutButton != null ? logoutButton : guestLoginButton;
            var sourceRect = sourceButton.GetComponent<RectTransform>();
            var labelObject = new GameObject("LoggedInUsernameText", typeof(RectTransform));
            labelObject.transform.SetParent(sourceButton.transform.parent, false);
            labelObject.transform.SetSiblingIndex(sourceButton.transform.GetSiblingIndex() + 1);

            var labelRect = labelObject.GetComponent<RectTransform>();
            ApplyUsernameLabelLayout(labelRect, sourceRect);

            var label = labelObject.AddComponent<Text>();
            var sourceText = sourceButton.GetComponentInChildren<Text>(true);
            label.font = sourceText != null ? sourceText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = sourceText != null ? Mathf.Max(18, sourceText.fontSize - 6) : 22;
            label.fontStyle = sourceText != null ? sourceText.fontStyle : FontStyle.Bold;
            label.alignment = TextAnchor.UpperCenter;
            label.color = sourceText != null ? sourceText.color : Color.white;
            label.raycastTarget = false;
            labelObject.SetActive(false);
            return label;
        }

        private void UpdateUsernameLabelLayout()
        {
            if (loggedInUsernameText == null)
                return;

            var sourceButton = logoutButton != null && logoutButton.gameObject.activeInHierarchy
                ? logoutButton
                : guestLoginButton;
            if (sourceButton == null)
                return;

            var sourceRect = sourceButton.GetComponent<RectTransform>();
            var labelRect = loggedInUsernameText.GetComponent<RectTransform>();
            if (sourceRect == null || labelRect == null)
                return;

            if (labelRect.parent != sourceButton.transform.parent)
                labelRect.SetParent(sourceButton.transform.parent, false);

            labelRect.SetSiblingIndex(sourceButton.transform.GetSiblingIndex() + 1);
            ApplyUsernameLabelLayout(labelRect, sourceRect);
        }

        private static void ApplyUsernameLabelLayout(RectTransform labelRect, RectTransform sourceRect)
        {
            if (labelRect == null || sourceRect == null)
                return;

            labelRect.anchorMin = sourceRect.anchorMin;
            labelRect.anchorMax = sourceRect.anchorMax;
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -sourceRect.sizeDelta.y * 0.72f);
            labelRect.sizeDelta = new Vector2(sourceRect.sizeDelta.x * 1.1f, Mathf.Max(32f, sourceRect.sizeDelta.y * 0.55f));
        }

        private static void EnsureManagersExist()
        {
            if (GooglePlayGamesManager.Instance != null && FirebaseManager.Instance != null && BlockPuzzle.UnityAdapter.Auth.AuthManager.Instance != null)
            {
                return;
            }

            var managerObject = GameObject.Find("GooglePlayManager");
            if (managerObject == null)
            {
                managerObject = new GameObject("GooglePlayManager");
            }

            if (GooglePlayGamesManager.Instance == null)
            {
                managerObject.AddComponent<GooglePlayGamesManager>();
            }

            if (FirebaseManager.Instance == null)
            {
                managerObject.AddComponent<FirebaseManager>();
            }

            if (BlockPuzzle.UnityAdapter.Auth.AuthManager.Instance == null)
            {
                var authObject = GameObject.Find("AuthManager");
                if (authObject == null)
                    authObject = new GameObject("AuthManager");

                if (authObject.GetComponent<BlockPuzzle.UnityAdapter.Auth.AuthManager>() == null)
                    authObject.AddComponent<BlockPuzzle.UnityAdapter.Auth.AuthManager>();
            }
        }

        private string GetTranslation(string tr, string en, string ko)
        {
            if (LanguageManager.Instance == null) return en;
            var lang = LanguageManager.Instance.CurrentLanguage;
            if (lang == LanguageManager.Language.Korean) return ko;
            if (lang == LanguageManager.Language.English) return en;
            return tr;
        }
    }
}
