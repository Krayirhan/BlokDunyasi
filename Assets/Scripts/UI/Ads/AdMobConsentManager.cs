using GoogleMobileAds.Common;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using BlockPuzzle.UnityAdapter.Privacy;
using UnityEngine;

/// <summary>
/// UMP consent akisini yonetir ve reklam istegi icin gerekli izin durumunu sunar.
/// </summary>
public class AdMobConsentManager : MonoBehaviour
{
    private static AdMobConsentManager _instance;
    private int _consentRequestGeneration;

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
        int requestGeneration = ++_consentRequestGeneration;

#if UNITY_EDITOR
        // UMP has no native consent client in the Unity Editor and falls back
        // to Placeholder ConsentInformationClient. Do not let that editor
        // placeholder block Google test-ad flow; real consent is still used
        // on Android/iOS builds below.
        ConsentGate.SetConsentState(ConsentState.Accepted);
        Debug.Log("[AdMobConsentManager] Unity Editor: UMP native client bypassed; test consent accepted.");
        // Do not route this synthetic editor result through
        // MobileAdsEventExecutor. The editor placeholder may not drain that
        // native event queue, which leaves AdMobManager in a retry loop.
        onComplete?.Invoke(null);
        return;
#endif

#pragma warning disable 0162, 0618
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
#pragma warning restore 0162, 0618

        // ConsentInformation.Update() has been observed to hang forever (native
        // callback never fires, CanRequestAds() never flips) on any app launch
        // that isn't the very first one after install -- i.e. whenever local
        // UMP storage already holds a prior determination and the SDK tries to
        // revalidate it instead of determining fresh. Resetting first forces
        // the SDK back onto the "first launch" code path, which has been
        // confirmed to work reliably. Skip the reset if we already have a
        // usable decision from earlier in this same process (e.g. a re-entrant
        // call after regaining foreground) to avoid an unnecessary round trip.
        if (!CanRequestAds)
        {
            ConsentInformation.Reset();
        }

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

        // ConsentInformation.Update's native callback has been observed to never
        // fire on some accounts/devices even though the SDK resolves
        // CanRequestAds() locally almost immediately. Poll that getter as a
        // fallback so a broken native->C# callback bridge cannot block ad
        // loading forever; the first of {callback, poll} to resolve wins.
        var dispatched = new BoolRef();
        StartCoroutine(PollCanRequestAdsFallback(requestGeneration, dispatched, onComplete));

        ConsentInformation.Update(requestParameters, (FormError updateError) =>
        {
            if (requestGeneration != _consentRequestGeneration || dispatched.Value)
                return;

            if (updateError != null)
            {
                // A transient UMP/network error must not revoke a consent
                // decision that was already accepted on a previous launch.
                if (CanRequestAds)
                {
                    ConsentGate.SetConsentState(ConsentState.Accepted);
                    dispatched.Value = true;
                    DispatchCompletion(onComplete, null);
                }
                else
                {
                    ConsentGate.SetConsentState(ConsentState.Unknown);
                    dispatched.Value = true;
                    DispatchCompletion(onComplete, updateError.Message);
                }
                return;
            }

            if (CanRequestAds)
            {
                ConsentGate.SetConsentState(ConsentState.Accepted);
                dispatched.Value = true;
                DispatchCompletion(onComplete, null);
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
            {
                if (requestGeneration != _consentRequestGeneration || dispatched.Value)
                    return;

                if (showError != null)
                {
                    if (CanRequestAds)
                        ConsentGate.SetConsentState(ConsentState.Accepted);
                    else
                        ConsentGate.SetConsentState(ConsentState.Unknown);
                }
                else
                {
                    ConsentGate.SetConsentState(CanRequestAds ? ConsentState.Accepted : ConsentState.Declined);
                }

                string completionError = CanRequestAds
                    ? null
                    : showError?.Message;
                dispatched.Value = true;
                DispatchCompletion(onComplete, completionError);
            });
        });
    }

    private sealed class BoolRef
    {
        public bool Value;
    }

    private IEnumerator PollCanRequestAdsFallback(int requestGeneration, BoolRef dispatched, Action<string> onComplete)
    {
        const float pollIntervalSeconds = 0.3f;
        const float pollTimeoutSeconds = 5f;
        float elapsed = 0f;

        while (elapsed < pollTimeoutSeconds)
        {
            yield return new WaitForSecondsRealtime(pollIntervalSeconds);
            elapsed += pollIntervalSeconds;

            if (requestGeneration != _consentRequestGeneration || dispatched.Value)
                yield break;

            if (CanRequestAds)
            {
                dispatched.Value = true;
                ConsentGate.SetConsentState(ConsentState.Accepted);
                DispatchCompletion(onComplete, null);
                yield break;
            }
        }
    }

    public void InvalidatePendingRequest()
    {
        _consentRequestGeneration++;
    }

    public void EnsureConsentResolved(AdMobRuntimeConfig config, Action<ConsentState, string> onComplete)
    {
        // UMP, kullanicinin bolgesi veya gizlilik secenekleri degismis olabilecegi icin
        // uygulamanin her acilisinda consent bilgisinin yenilenmesini bekler.
        GatherConsent(config, errorMessage =>
        {
            onComplete?.Invoke(ConsentGate.CurrentState, errorMessage);
        });
    }

    public void ShowPrivacyOptionsForm(Action<string> onComplete)
    {
        ConsentForm.ShowPrivacyOptionsForm((FormError showError) =>
        {
            if (showError == null)
                ConsentGate.SetConsentState(CanRequestAds ? ConsentState.Accepted : ConsentState.Declined);

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
