using System;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Audio
{
    [Serializable]
    public sealed class AudioCue
    {
        public AudioClip[] clips = Array.Empty<AudioClip>();
        [Range(0f, 1.5f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitchMin = 1f;
        [Range(0.1f, 3f)] public float pitchMax = 1f;
        [Range(0f, 0.25f)] public float randomVolumeJitter = 0f;

        public bool TryPick(out AudioClip clip, out float resolvedVolume, out float resolvedPitch)
        {
            clip = null;
            resolvedVolume = Mathf.Max(0f, volume);
            resolvedPitch = Mathf.Max(0.1f, pitchMin);

            if (clips == null || clips.Length == 0)
                return false;

            int validCount = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return false;

            int pickIndex = UnityEngine.Random.Range(0, validCount);
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                if (pickIndex == 0)
                {
                    clip = clips[i];
                    break;
                }

                pickIndex--;
            }

            float minPitch = Mathf.Max(0.1f, Mathf.Min(pitchMin, pitchMax));
            float maxPitch = Mathf.Max(minPitch, Mathf.Max(pitchMin, pitchMax));
            resolvedPitch = UnityEngine.Random.Range(minPitch, maxPitch);

            float jitter = randomVolumeJitter > 0f
                ? UnityEngine.Random.Range(-randomVolumeJitter, randomVolumeJitter)
                : 0f;
            resolvedVolume = Mathf.Max(0f, volume + jitter);
            return clip != null;
        }
    }

    [CreateAssetMenu(fileName = "DefaultAudioBank", menuName = "BlockPuzzle/Audio Bank")]
    public sealed class AudioBank : ScriptableObject
    {
        [Header("Music")]
        public AudioClip mainMenuMusic;
        public AudioClip gameplayMusic;
        public AudioClip gameOverMusic;

        [Header("UI")]
        public AudioCue uiClick = new AudioCue
        {
            volume = 0.75f,
            pitchMin = 0.98f,
            pitchMax = 1.03f,
            randomVolumeJitter = 0.02f
        };

        [Header("Gameplay")]
        public AudioCue blockPlace = new AudioCue
        {
            volume = 0.85f,
            pitchMin = 0.98f,
            pitchMax = 1.04f,
            randomVolumeJitter = 0.03f
        };

        public AudioCue invalidDrop = new AudioCue
        {
            volume = 0.8f,
            pitchMin = 0.92f,
            pitchMax = 0.98f,
            randomVolumeJitter = 0.02f
        };

        public AudioCue lineClear = new AudioCue
        {
            volume = 1f,
            pitchMin = 0.98f,
            pitchMax = 1.06f,
            randomVolumeJitter = 0.03f
        };

        public AudioCue combo = new AudioCue
        {
            volume = 1f,
            pitchMin = 1f,
            pitchMax = 1.08f,
            randomVolumeJitter = 0.03f
        };

        public AudioCue gameOver = new AudioCue
        {
            volume = 1f,
            pitchMin = 1f,
            pitchMax = 1f,
            randomVolumeJitter = 0f
        };
    }
}
