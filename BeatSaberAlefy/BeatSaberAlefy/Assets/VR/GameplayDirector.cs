using UnityEngine;
using UnityEngine.SceneManagement;
using BeatSaberAlefy.Audio;
using BeatSaberAlefy.BeatMap;
using BeatSaberAlefy.UI;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Coordina audio, beat map e spawn: avvia l'audio con offset, fornisce GetAudioTime per SliceDetector,
    /// e riferimenti per SpawnController. Da mettere nella scena di gioco VR.
    /// </summary>
    public class GameplayDirector : MonoBehaviour
    {
        [Tooltip("AudioSource che suona la traccia")]
        public AudioSource AudioSource;

        [Tooltip("Beat map da usare (generata da RhythmData + BeatMapGenerator). Se null, usa GameSessionData.")]
        public BeatMapData BeatMap;

        [Tooltip("Dati ritmo (BPM, offset). Se null, usa GameSessionData.")]
        public RhythmData RhythmData;

        [Tooltip("SpawnController che istanzia i cubi")]
        public SpawnController SpawnController;

        [Tooltip("Transform del player (XR Origin) per SpawnController")]
        public Transform PlayerForward;

        [Tooltip("Nome scena menu (per redirect se si entra in Game senza traccia selezionata)")]
        public string MenuSceneName = "Menu";

        bool _started;

        void Start()
        {
            if (string.IsNullOrEmpty(GameSessionData.SelectedTrackId))
            {
                SceneManager.LoadScene(MenuSceneName);
                return;
            }

            var menu = FindObjectOfType<MenuController>();
            if (menu != null)
                menu.gameObject.SetActive(false);
            if (RenderSettings.skybox == null)
            {
                var skybox = Resources.Load<Material>("SkyboxRoom");
                if (skybox != null)
                    RenderSettings.skybox = skybox;
            }
            if (!string.IsNullOrEmpty(GameSessionData.SelectedTrackId))
            {
                var cached = TrackCache.Instance.GetReady(GameSessionData.SelectedTrackId);
                if (cached != null)
                {
                    RhythmData = cached.Rhythm;
                    BeatMap = cached.BeatMap;
                    if (AudioSource != null) AudioSource.clip = cached.Clip;
                    GameSessionData.Set(cached.Clip, cached.Rhythm, cached.BeatMap);
                }
            }
            if (RhythmData == null) RhythmData = GameSessionData.CurrentRhythm;
            if (BeatMap == null) BeatMap = GameSessionData.CurrentBeatMap;
            if (AudioSource != null && AudioSource.clip == null && GameSessionData.CurrentClip != null)
                AudioSource.clip = GameSessionData.CurrentClip;

            if (AudioSource == null) AudioSource = GetComponent<AudioSource>();
            if (AudioSource != null && RhythmData != null)
                AudioSource.time = RhythmData.FirstBeatOffsetSeconds;

            if (SpawnController != null)
            {
                SpawnController.BeatMap = BeatMap;
                SpawnController.AudioSource = AudioSource;
                SpawnController.PlayerForward = PlayerForward != null ? PlayerForward : transform;
            }
#if UNITY_EDITOR
            // #region agent log
            BeatSaberAlefy.UI.DebugLog.Write("GameplayDirector.Start", "Director init", "H1 H2 H3",
                ("BeatMapNull", BeatMap == null),
                ("EntriesLength", BeatMap?.Entries?.Length ?? -1),
                ("SpawnControllerNull", SpawnController == null),
                ("CubePrefabNull", SpawnController?.CubePrefab == null),
                ("AudioSourceNull", AudioSource == null),
                ("ClipNull", AudioSource?.clip == null));
            // #endregion
#endif
        }

        void Update()
        {
            if (GameState.Instance != null && GameState.Instance.IsGameOver)
            {
                if (AudioSource != null && AudioSource.isPlaying)
                    AudioSource.Stop();
                return;
            }
            if (!_started && AudioSource != null && AudioSource.clip != null)
            {
                if (!AudioSource.isPlaying)
                    AudioSource.Play();
                _started = true;
            }
        }

        /// <summary>
        /// Tempo audio corrente in secondi (per SliceDetector).
        /// </summary>
        public float GetAudioTime()
        {
            if (AudioSource == null || AudioSource.clip == null) return 0f;
            return AudioSource.time;
        }

        /// <summary>
        /// Prepara la partita: imposta clip, rhythm e beat map (chiamare dal menu prima di caricare la scena di gioco).
        /// </summary>
        public void PrepareGame(AudioClip clip, RhythmData rhythm, BeatMapData beatMap)
        {
            if (AudioSource != null)
                AudioSource.clip = clip;
            RhythmData = rhythm;
            BeatMap = beatMap;
            if (SpawnController != null)
            {
                SpawnController.BeatMap = beatMap;
                SpawnController.ResetSpawn();
            }
        }
    }
}
