using System.Collections;
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
        public Button ButtonAnalyze;
        public Button ButtonStartGame;
        public Text StatusText;
        public GameObject PanelAnalyzing;
        public GameObject PanelReady;

        [Header("Scene")]
        public string GameSceneName = "Game";

        [Header("Audio")]
        public AudioSource PreviewAudioSource;

        AudioClip _currentClip;
        RhythmData _currentRhythm;
        BeatMapData _currentBeatMap;
        IAlefyClient _alefyClient;

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
            if (ButtonAnalyze != null)
                ButtonAnalyze.onClick.AddListener(OnAnalyze);
            if (ButtonStartGame != null)
                ButtonStartGame.onClick.AddListener(OnStartGame);

            SetStatus("Seleziona una canzone");
            SetReadyPanel(false);
            SetAnalyzingPanel(false);
            if (ButtonStartGame != null) ButtonStartGame.interactable = false;
        }

        void OnSelectSong()
        {
            // TODO: aprire lista da alefy (GetSongsAsync) o file picker per audio locale
            // Per ora: caricamento da Resources o StreamingAssets per test
            var clip = Resources.Load<AudioClip>("Audio/TestClip");
            if (clip != null)
            {
                _currentClip = clip;
                SetStatus($"Caricata: {clip.name}");
                if (ButtonAnalyze != null) ButtonAnalyze.interactable = true;
            }
            else
            {
                SetStatus("Nessun file di test. Aggiungi Assets/Resources/Audio/TestClip o integra alefy.");
            }
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
