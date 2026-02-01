using UnityEngine;
using BeatSaberAlefy.Audio;
using BeatSaberAlefy.UI;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Modula l'emissione del pavimento al beat della musica.
    /// Richiede un Renderer sullo stesso GameObject (es. NeonFloor) e GameplayDirector in scena.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class DancefloorController : MonoBehaviour
    {
        [Tooltip("Colore base emissione (neon)")]
        public Color BaseEmissionColor = new Color(0f, 0.8f, 1f);
        [Tooltip("Intensità emissione base (HDR)")]
        public float BaseEmissionStrength = 0.3f;
        [Tooltip("Intensità extra sul beat (HDR)")]
        public float PulseStrength = 0.8f;
        [Tooltip("Durata impulso in secondi")]
        public float PulseDuration = 0.2f;

        Renderer _renderer;
        Material _floorMatInstance;
        GameplayDirector _director;
        RhythmData _rhythm;
        float _pulseTimer;
        int _lastBeatIndex = -1;

        void Start()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
                _floorMatInstance = _renderer.material;
            _director = FindObjectOfType<GameplayDirector>();
            _rhythm = GameSessionData.CurrentRhythm;
            if (_director != null && _rhythm == null)
                _rhythm = _director.RhythmData;
        }

        void Update()
        {
            if (_floorMatInstance == null || !_floorMatInstance.HasProperty("_EmissionColor"))
                return;
            if (_rhythm == null)
                _rhythm = GameSessionData.CurrentRhythm;
            if (_director != null && _rhythm == null)
                _rhythm = _director.RhythmData;

            float audioTime = _director != null ? _director.GetAudioTime() : 0f;
            int currentBeatIndex = GetCurrentBeatIndex(audioTime);

            if (currentBeatIndex > _lastBeatIndex && _lastBeatIndex >= 0)
                _pulseTimer = PulseDuration;
            _lastBeatIndex = currentBeatIndex;

            if (_pulseTimer > 0f)
                _pulseTimer -= Time.deltaTime;

            float pulseMultiplier = _pulseTimer > 0f
                ? (1f + PulseStrength * (1f - _pulseTimer / PulseDuration))
                : 1f;
            Color emission = BaseEmissionColor * (BaseEmissionStrength * pulseMultiplier);
            _floorMatInstance.SetColor("_EmissionColor", emission);
        }

        int GetCurrentBeatIndex(float audioTime)
        {
            if (_rhythm == null || _rhythm.BPM <= 0f)
                return -1;
            float beats = (audioTime - _rhythm.FirstBeatOffsetSeconds) * _rhythm.BPM / 60f;
            return Mathf.FloorToInt(beats);
        }
    }
}
