using UnityEngine;
using BeatSaberAlefy.Audio;
using BeatSaberAlefy.BeatMap;
using BeatSaberAlefy.UI;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Solo in Editor: se la scena Game viene aperta senza dati da Menu, crea clip e beat map
    /// fittizi per poter provare il gioco in modalità desktop (Play senza passare dal Menu).
    /// In build non fa nulla.
    /// </summary>
    public class DesktopTestBootstrap : MonoBehaviour
    {
#if UNITY_EDITOR
        [Tooltip("Crea dati di test solo se GameSessionData è vuoto")]
        public bool OnlyWhenEmpty = true;

        void Awake()
        {
            if (OnlyWhenEmpty && GameSessionData.CurrentClip != null)
                return;

            float duration = 10f;
            var clip = CreateDummyClip(duration);
            var rhythm = new RhythmData { BPM = 120f, FirstBeatOffsetSeconds = 0.5f };
            var beatMap = CreateDummyBeatMap(duration);

            GameSessionData.Set(clip, rhythm, beatMap);
            // Imposta SelectedTrackId per evitare che GameplayDirector carichi il Menu
            GameSessionData.SelectedTrackId = "test_track";
            Debug.Log("[DesktopTestBootstrap] Dati di test impostati (clip 10s, beat map con note ogni ~0.5s). Usa mouse: sinistro=left, destro=right.");
        }

        static AudioClip CreateDummyClip(float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(duration * sampleRate);
            var clip = AudioClip.Create("TestClip", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / sampleRate) * 0.1f;
            clip.SetData(data, 0);
            return clip;
        }

        static BeatMapData CreateDummyBeatMap(float duration)
        {
            var entries = new System.Collections.Generic.List<BeatMapEntry>();
            float t = 1f;
            while (t < duration - 1f)
            {
                entries.Add(new BeatMapEntry(t, 0, (int)(t % 3)));   // left
                entries.Add(new BeatMapEntry(t + 0.25f, 1, (int)t % 3)); // right
                t += 0.5f;
            }
            return new BeatMapData { Entries = entries.ToArray(), AudioDurationSeconds = duration };
        }
#endif
    }
}
