using UnityEngine;

namespace BeatSaberAlefy.UI
{
    /// <summary>
    /// Salvataggio e lettura del miglior punteggio per trackId (PlayerPrefs).
    /// </summary>
    public static class HighScoreService
    {
        const string KeyPrefix = "BeatSaber_HighScore_";

        public static int GetHighScore(string trackId)
        {
            if (string.IsNullOrEmpty(trackId)) return 0;
            return PlayerPrefs.GetInt(KeyPrefix + trackId.Trim(), 0);
        }

        public static void SetHighScore(string trackId, int score)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            PlayerPrefs.SetInt(KeyPrefix + trackId.Trim(), score);
            PlayerPrefs.Save();
        }
    }
}
