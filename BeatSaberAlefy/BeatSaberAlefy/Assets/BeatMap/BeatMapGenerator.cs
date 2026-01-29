using System.Collections.Generic;
using UnityEngine;
using BeatSaberAlefy.Audio;

namespace BeatSaberAlefy.BeatMap
{
    /// <summary>
    /// Genera BeatMapData da RhythmData: un cubo ogni N beat, con lane e altezza variate.
    /// </summary>
    public static class BeatMapGenerator
    {
        [Tooltip("Default: un cubo ogni N beat (1 = ogni beat, 2 = ogni 2 beat)")]
        public const int DefaultBeatsPerNote = 1;

        /// <summary>
        /// Genera beat map automatica da RhythmData e durata audio.
        /// </summary>
        /// <param name="rhythm">Dati ritmo (BPM, offset, opzionale beat list)</param>
        /// <param name="audioDurationSeconds">Durata dell'audio in secondi</param>
        /// <param name="beatsPerNote">Un cubo ogni N beat (1 = ogni beat)</param>
        /// <param name="lanes">Numero di lane (2 = sinistra/destra)</param>
        /// <param name="heights">Numero di altezze (3 = basso/centro/alto)</param>
        public static BeatMapData Generate(
            RhythmData rhythm,
            float audioDurationSeconds,
            int beatsPerNote = DefaultBeatsPerNote,
            int lanes = 2,
            int heights = 3)
        {
            int beatCount = rhythm.GetBeatCount(audioDurationSeconds);
            var entries = new List<BeatMapEntry>();

            for (int i = 0; i < beatCount; i += beatsPerNote)
            {
                float t = rhythm.GetBeatTime(i);
                if (t >= audioDurationSeconds) break;
                int lane = i % lanes;
                int height = (i / lanes) % heights;
                entries.Add(new BeatMapEntry(t, lane, height));
            }

            return new BeatMapData
            {
                Entries = entries.ToArray(),
                AudioDurationSeconds = audioDurationSeconds
            };
        }
    }
}
