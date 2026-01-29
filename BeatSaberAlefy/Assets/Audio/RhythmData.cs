using System;
using UnityEngine;

namespace BeatSaberAlefy.Audio
{
    /// <summary>
    /// Output dell'analisi ritmica: BPM, offset prima battuta, opzionale lista tempi beat.
    /// </summary>
    [Serializable]
    public class RhythmData
    {
        [Tooltip("Battute per minuto")]
        public float BPM = 120f;

        [Tooltip("Offset in secondi della prima battuta rispetto all'inizio dell'audio")]
        public float FirstBeatOffsetSeconds;

        [Tooltip("Tempi in secondi di ogni beat/onset (opzionale). Se vuoto, si usa BPM + offset per generare la griglia.")]
        public float[] BeatTimesSeconds = Array.Empty<float>();

        /// <summary>
        /// Restituisce il tempo in secondi del beat di indice <paramref name="beatIndex"/> (0-based).
        /// Usa BeatTimesSeconds se disponibili, altrimenti BPM + FirstBeatOffsetSeconds.
        /// </summary>
        public float GetBeatTime(int beatIndex)
        {
            if (BeatTimesSeconds != null && beatIndex >= 0 && beatIndex < BeatTimesSeconds.Length)
                return BeatTimesSeconds[beatIndex];
            return FirstBeatOffsetSeconds + (beatIndex * 60f / BPM);
        }

        /// <summary>
        /// Numero di beat fino alla fine (stima da durata audio se BeatTimesSeconds non è popolato).
        /// </summary>
        public int GetBeatCount(float audioDurationSeconds)
        {
            if (BeatTimesSeconds != null && BeatTimesSeconds.Length > 0)
                return BeatTimesSeconds.Length;
            if (BPM <= 0) return 0;
            float beats = (audioDurationSeconds - FirstBeatOffsetSeconds) * BPM / 60f;
            return Mathf.Max(0, Mathf.FloorToInt(beats));
        }
    }
}
