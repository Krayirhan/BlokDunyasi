using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Privacy
{
    /// <summary>
    /// Release-only legal link source. Leave empty in development if final URLs are not available yet;
    /// release validation must then fail instead of silently showing placeholder legal text.
    /// </summary>
    [CreateAssetMenu(fileName = "LegalLinksConfig", menuName = "Blok Dunyasi/Privacy/Legal Links Config")]
    public sealed class LegalLinksConfig : ScriptableObject
    {
        public const string ResourcesPath = "LegalLinksConfig";

        [SerializeField] private string privacyPolicyUrl = string.Empty;
        [SerializeField] private string termsOfServiceUrl = string.Empty;

        public string PrivacyPolicyUrl => privacyPolicyUrl != null ? privacyPolicyUrl.Trim() : string.Empty;
        public string TermsOfServiceUrl => termsOfServiceUrl != null ? termsOfServiceUrl.Trim() : string.Empty;
        public bool HasPrivacyPolicyUrl => !string.IsNullOrWhiteSpace(PrivacyPolicyUrl);
        public bool HasTermsOfServiceUrl => !string.IsNullOrWhiteSpace(TermsOfServiceUrl);
    }
}
