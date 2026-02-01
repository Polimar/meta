using UnityEngine;
using BeatSaberAlefy.Audio;
using BeatSaberAlefy.BeatMap;

namespace BeatSaberAlefy.UI
{
    /// <summary>
    /// Dati della sessione corrente: clip, rhythm, beat map. Compilato dal menu prima di caricare la scena di gioco.
    /// </summary>
    public static class GameSessionData
    {
        public static AudioClip CurrentClip { get; set; }
        public static RhythmData CurrentRhythm { get; set; }
        public static BeatMapData CurrentBeatMap { get; set; }
        /// <summary>TrackId della canzone in corso (per high score e statistiche).</summary>
        public static string SelectedTrackId { get; set; }

        public static void Set(AudioClip clip, RhythmData rhythm, BeatMapData beatMap)
        {
            CurrentClip = clip;
            CurrentRhythm = rhythm;
            CurrentBeatMap = beatMap;
        }

        public static void Clear()
        {
            CurrentClip = null;
            CurrentRhythm = null;
            CurrentBeatMap = null;
            SelectedTrackId = null;
        }
    }
}
