using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Simple editor helpers to test leaderboard submit + simulated sign-in flow
/// without Play Games SDK. Use menu: BlokDunyasi / Test / SubmitScoreAndSimulateSignIn
/// </summary>
public static class TestLeaderboardFlow
{
    [MenuItem("BlokDunyasi/Test/SubmitScoreAndSimulateSignIn")]
    public static void SubmitAndSimulate()
    {
        // Resolve types
        var authType = Type.GetType("BlockPuzzle.UnityAdapter.Auth.AuthManager, BlockPuzzleUnityAdapter");
        var lbType = Type.GetType("BlockPuzzle.UnityAdapter.Social.LeaderboardManager, BlockPuzzleUnityAdapter");

        if (authType == null || lbType == null)
        {
            Debug.LogError("Manager types not found. Is BlockPuzzleUnityAdapter assembly available?");
            return;
        }

        // Create/find AuthManager and invoke Awake manually
        var authObj = GameObject.Find("AuthManager") ?? new GameObject("AuthManager");
        var authComp = authObj.GetComponent(authType) ?? authObj.AddComponent(authType);
        var awakeMethod = authType.GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (awakeMethod != null)
        {
            awakeMethod.Invoke(authComp, null);
            Debug.Log("AuthManager Awake invoked.");
        }

        // Create/find LeaderboardManager and invoke Awake manually
        var lbObj = GameObject.Find("LeaderboardManager") ?? new GameObject("LeaderboardManager");
        var lbComp = lbObj.GetComponent(lbType) ?? lbObj.AddComponent(lbType);
        var lbAwakeMethod = lbType.GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (lbAwakeMethod != null)
        {
            lbAwakeMethod.Invoke(lbComp, null);
            Debug.Log("LeaderboardManager Awake invoked.");
        }

        // Get instances
        var authInstProp = authType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var lbInstProp = lbType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        var authInst = authInstProp?.GetValue(null, null);
        var lbInst = lbInstProp?.GetValue(null, null);

        if (lbInst == null)
        {
            Debug.LogError("LeaderboardManager.Instance still null after Awake.");
            return;
        }

        // Submit test score
        var submitMethod = lbType.GetMethod("SubmitScore");
        if (submitMethod != null)
        {
            submitMethod.Invoke(lbInst, new object[] { 1234, 10, 2 });
            Debug.Log("Test score 1234 submitted (queued as guest, will flush on sign-in).");
        }

        // Simulate sign-in to trigger flush
        if (authInst != null)
        {
            var setSignedMethod = authType.GetMethod("SetSignedIn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (setSignedMethod != null)
            {
                setSignedMethod.Invoke(authInst, new object[] { true, "play_sim_001", "PlayerSim" });
                Debug.Log("Simulated sign-in → pending scores should flush.");
            }
            else
            {
                Debug.LogWarning("Could not find SetSignedIn method.");
            }
        }
        else
        {
            Debug.LogWarning("AuthManager.Instance not found; skipping sign-in simulation.");
        }
    }
}
