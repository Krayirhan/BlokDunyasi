using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using System;
using System.Collections.Generic;
using BlockPuzzle.UnityAdapter.Privacy;
using UnityEngine;

/// <summary>
/// UMP consent akisini yonetir ve reklam istegi icin gerekli izin durumunu sunar.
/// </summary>
public class AdMobConsentManager : MonoBehaviour
{
    private static AdMobConsentManager _instance;

    public static AdMobConsentManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject consentGO = new GameObject("AdMobConsentManager");
                _instance = consentGO.AddComponent<AdMobConsentManager>();
                DontDestroyOnLoad(consentGO);
            }

            return _instance;
        }
    }

    public bool CanRequestAds => ConsentInformation.CanRequestAds();
    public bool IsPrivacyOptionsRequired =>
        ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
    public ConsentState CurrentConsentState => ConsentGate.CurrentState;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GatherConsent(AdMobRuntimeConfig config, Action<string> onComplete)
    {
        var requestParameters = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = config != null && config.TagForUnderAgeOfConsent,
            ConsentDebugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.Disabled,
                TestDeviceHashedIds = config != null
                    ? new List<string>(config.GetTestDeviceHashedIds())
                    : new List<string>()
            }
        };

        ConsentInformation.Update(requestParameters, (FormError updateError) =>
        {
            if (updateError != null)
            {
                ConsentGate.SetConsentState(ConsentState.Unknown);
                DispatchCompletion(onComplete, updateError.Message);
                return;
            }

            if (CanRequestAds)
            {
                ConsentGate.SetConsentState(ConsentState.Accepted);
                DispatchCompletion(onComplete, null);
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
            {
                if (showError != null)
                {
                    ConsentGate.SetConsentState(ConsentState.Unknown);
                }
                else
                {
                    ConsentGate.SetConsentState(CanRequestAds ? ConsentState.Accepted : ConsentState.Declined);
                }

                DispatchCompletion(onComplete, showError?.Message);
            });
        });
    }

    public void EnsureConsentResolved(AdMobRuntimeConfig config, Action<ConsentState, string> onComplete)
    {
        if (ConsentGate.IsResolved)
        {
            onComplete?.Invoke(ConsentGate.CurrentState, null);
            return;
        }

        GatherConsent(config, errorMessage =>
        {
            onComplete?.Invoke(ConsentGate.CurrentState, errorMessage);
        });
    }

    public void ShowPrivacyOptionsForm(Action<string> onComplete)
    {
        ConsentForm.ShowPrivacyOptionsForm((FormError showError) =>
        {
            DispatchCompletion(onComplete, showError?.Message);
        });
    }

    private static void DispatchCompletion(Action<string> onComplete, string errorMessage)
    {
        if (onComplete == null)
            return;

        MobileAdsEventExecutor.ExecuteInUpdate(() => onComplete(errorMessage));
    }
}
