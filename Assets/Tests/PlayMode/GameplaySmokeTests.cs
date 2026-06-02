#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Tests.PlayMode
{
    public class GameplaySmokeTests
    {
        [Test]
        public void MainMenuToGameSceneLoad_Smoke()
        {
            if (!Application.CanStreamedLevelBeLoaded("MainMenu") || !Application.CanStreamedLevelBeLoaded("OyunEkranı"))
                Assert.Ignore("Required scenes are not in Build Settings.");

            SceneManager.LoadScene("MainMenu");
            SceneManager.LoadScene("OyunEkranı");

            Assert.AreEqual("OyunEkranı", SceneManager.GetActiveScene().name);
        }

        [Test]
        public void GameBootstrapExistsInGameplayScene_Smoke()
        {
            if (!Application.CanStreamedLevelBeLoaded("OyunEkranı"))
                Assert.Ignore("Gameplay scene is not in Build Settings.");

            SceneManager.LoadScene("OyunEkranı");

            var bootstrap = Object.FindFirstObjectByType<BlockPuzzle.UnityAdapter.Boot.GameBootstrap>();
            Assert.IsNotNull(bootstrap, "GameBootstrap should exist in gameplay scene.");
        }

        [Test]
        public void GameOverSceneLoad_Smoke()
        {
            if (!Application.CanStreamedLevelBeLoaded("GameOver"))
                Assert.Ignore("GameOver scene is not in Build Settings.");

            SceneManager.LoadScene("GameOver");

            Assert.AreEqual("GameOver", SceneManager.GetActiveScene().name);
        }

        [Test]
        public void RestartPath_GameOverToGameplay_Smoke()
        {
            if (!Application.CanStreamedLevelBeLoaded("GameOver") || !Application.CanStreamedLevelBeLoaded("OyunEkranı"))
                Assert.Ignore("Required scenes are not in Build Settings.");

            SceneManager.LoadScene("GameOver");
            SceneManager.LoadScene("OyunEkranı");

            Assert.AreEqual("OyunEkranı", SceneManager.GetActiveScene().name);
        }
    }
}
#endif
