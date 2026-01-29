using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BeatSaberAlefy.Audio;
using BeatSaberAlefy.BeatMap;
using BeatSaberAlefy.Alefy;

namespace BeatSaberAlefy.UI
{
    /// <summary>
    /// Menu: selezione canzone (da alefy o file locale), avvio analisi ritmo, "Tutto pronto" e "Avvia partita".
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("UI")]
        public Button ButtonSelectSong;
        public Button ButtonLoadFromAlefy;
        public Button ButtonAnalyze;
        public Button ButtonStartGame;
        public Text StatusText;
        public GameObject PanelAnalyzing;
        public GameObject PanelReady;
        [Tooltip("Container per i pulsanti delle tracce Alefy (opzionale). Se vuoto, la prima traccia viene caricata direttamente.")]
        public Transform AlefyTrackListContainer;
        [Tooltip("Prefab pulsante per una traccia (deve avere Button + Text come figlio per titolo). Se vuoto, non si mostra lista.")]
        public GameObject AlefyTrackButtonPrefab;

        [Header("Scene")]
        public string GameSceneName = "Game";

        [Header("Audio")]
        public AudioSource PreviewAudioSource;

        AudioClip _currentClip;
        RhythmData _currentRhythm;
        BeatMapData _currentBeatMap;
        IAlefyClient _alefyClient;
        AlefySongMetadata[] _alefyTracks;

        void Start()
        {
            _alefyClient = new AlefyService();
            var settings = AlefySettings.Load();
            if (settings != null && _alefyClient is AlefyService svc)
            {
                svc.BaseUrl = settings.BaseUrl;
                svc.AuthToken = settings.AuthToken;
            }

            if (ButtonSelectSong != null)
                ButtonSelectSong.onClick.AddListener(OnSelectSong);
            if (ButtonLoadFromAlefy != null)
                ButtonLoadFromAlefy.onClick.AddListener(OnLoadFromAlefy);
            if (ButtonAnalyze != null)
                ButtonAnalyze.onClick.AddListener(OnAnalyze);
            if (ButtonStartGame != null)
                ButtonStartGame.onClick.AddListener(OnStartGame);

            SetStatus("Seleziona una canzone (locale o da Alefy)");
            SetReadyPanel(false);
            SetAnalyzingPanel(false);
            if (ButtonStartGame != null) ButtonStartGame.interactable = false;
        }

        void OnSelectSong()
        {
            var clip = Resources.Load<AudioClip>("Audio/TestClip");
            if (clip != null)
            {
                _currentClip = clip;
                SetStatus($"Caricata: {clip.name}");
                if (ButtonAnalyze != null) ButtonAnalyze.interactable = true;
            }
            else
            {
                SetStatus("Nessun file di test (Resources/Audio/TestClip). Usa \"Carica da Alefy\".");
            }
        }

        void OnLoadFromAlefy()
        {
            StartCoroutine(LoadFromAlefyCoroutine());
        }

        IEnumerator LoadFromAlefyCoroutine()
        {
            SetStatus("Caricamento tracce da Alefy...");
            if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = false;

            var svc = _alefyClient as AlefyService;
            if (svc == null)
            {
                SetStatus("Client Alefy non disponibile.");
                if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = true;
                yield break;
            }

            AlefySongMetadata[] tracks = null;
            yield return svc.GetSongsAsyncCoroutine(null, t => tracks = t);
            _alefyTracks = tracks;

            if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = true;

            if (_alefyTracks == null || _alefyTracks.Length == 0)
            {
                SetStatus("Nessuna traccia da Alefy. Controlla BaseUrl e AuthToken in AlefySettings.");
                yield break;
            }

            if (AlefyTrackListContainer != null && AlefyTrackButtonPrefab != null)
            {
                foreach (Transform c in AlefyTrackListContainer)
                    Destroy(c.gameObject);
                for (int i = 0; i < _alefyTracks.Length; i++)
                {
                    int index = i;
                    var meta = _alefyTracks[i];
                    var go = Instantiate(AlefyTrackButtonPrefab, AlefyTrackListContainer);
                    var label = go.GetComponentInChildren<Text>();
                    if (label != null) label.text = meta.Title + " - " + meta.Artist;
                    var btn = go.GetComponent<Button>();
                    if (btn != null) btn.onClick.AddListener(() => OnAlefyTrackSelected(index));
                }
                SetStatus($"Caricate {_alefyTracks.Length} tracce. Scegli una dalla lista.");
            }
            else
            {
                OnAlefyTrackSelected(0);
            }
        }

        void OnAlefyTrackSelected(int index)
        {
            if (_alefyTracks == null || index < 0 || index >= _alefyTracks.Length) return;
            StartCoroutine(PrepareAndLoadAlefyTrack(_alefyTracks[index]));
        }

        IEnumerator PrepareAndLoadAlefyTrack(AlefySongMetadata meta)
        {
            SetStatus("Download in corso: " + meta.Title + "...");
            if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = false;

            var svc = _alefyClient as AlefyService;
            if (svc == null)
            {
                SetStatus("Client Alefy non disponibile.");
                if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = true;
                yield break;
            }

            AlefySongResult result = null;
            yield return svc.PrepareSongAsyncCoroutine(meta.Id, r => result = r);

            if (ButtonLoadFromAlefy != null) ButtonLoadFromAlefy.interactable = true;

            if (result == null || string.IsNullOrEmpty(result.LocalAudioPath))
            {
                SetStatus("Download fallito. Controlla token e connessione.");
                yield break;
            }

            var loadDone = false;
            AudioClip clip = null;
            string loadError = null;
            StartCoroutine(AudioClipLoader.LoadFromFile(result.LocalAudioPath, c => { clip = c; loadDone = true; }, e => { loadError = e; loadDone = true; }));
            yield return new WaitUntil(() => loadDone);

            if (clip == null)
            {
                SetStatus("Errore caricamento audio: " + (loadError ?? "unknown"));
                yield break;
            }

            _currentClip = clip;
            SetStatus("Caricata: " + meta.Title + ". Avvia analisi ritmo.");
            if (ButtonAnalyze != null) ButtonAnalyze.interactable = true;
        }

        void OnAnalyze()
        {
            if (_currentClip == null)
            {
                SetStatus("Carica prima una canzone");
                return;
            }

            SetAnalyzingPanel(true);
            if (ButtonAnalyze != null) ButtonAnalyze.interactable = false;
            SetStatus("Analisi ritmo in corso...");

            StartCoroutine(AnalyzeCoroutine());
        }

        IEnumerator AnalyzeCoroutine()
        {
            var done = false;
            RhythmData result = null;
            RhythmAnalyzer.AnalyzeAsync(_currentClip, r =>
            {
                result = r;
                done = true;
            });
            yield return new WaitUntil(() => done);

            _currentRhythm = result;
            _currentBeatMap = BeatMapGenerator.Generate(
                _currentRhythm,
                _currentClip.length,
                BeatMapGenerator.DefaultBeatsPerNote,
                2,
                3
            );

            SetAnalyzingPanel(false);
            SetReadyPanel(true);
            SetStatus("Tutto pronto. Avvia partita.");
            if (ButtonStartGame != null) ButtonStartGame.interactable = true;
        }

        void OnStartGame()
        {
            if (_currentClip == null || _currentRhythm == null || _currentBeatMap == null)
            {
                SetStatus("Prepara prima una canzone e avvia l'analisi");
                return;
            }

            GameSessionData.Set(_currentClip, _currentRhythm, _currentBeatMap);
            SceneManager.LoadScene(GameSceneName);
        }

        void SetStatus(string text)
        {
            if (StatusText != null) StatusText.text = text;
        }

        void SetReadyPanel(bool active)
        {
            if (PanelReady != null) PanelReady.SetActive(active);
        }

        void SetAnalyzingPanel(bool active)
        {
            if (PanelAnalyzing != null) PanelAnalyzing.SetActive(active);
        }
    }
}
