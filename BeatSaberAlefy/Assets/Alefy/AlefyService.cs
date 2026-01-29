using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaberAlefy.Alefy
{
    /// <summary>
    /// Implementazione di IAlefyClient per alefy.alevale.it.
    /// Configurare BaseUrl e credenziali (token/API key) tramite AlefySettings o variabili d'ambiente.
    /// Da completare con la documentazione API reale.
    /// </summary>
    public class AlefyService : IAlefyClient
    {
        public string BaseUrl = "https://alefy.alevale.it";
        public string AuthToken;

        readonly string _cacheDir;

        public AlefyService()
        {
            _cacheDir = Path.Combine(Application.temporaryCachePath, "AlefyCache");
            if (!Directory.Exists(_cacheDir))
                Directory.CreateDirectory(_cacheDir);
        }

        public async Task<AlefySongMetadata[]> GetSongsAsync(string searchQuery = null)
        {
            // TODO: sostituire con endpoint reale quando disponibile la documentazione API
            // Esempio: GET {BaseUrl}/api/songs?q={searchQuery} con header Authorization: Bearer {AuthToken}
            await Task.Yield();
            return Array.Empty<AlefySongMetadata>();
        }

        public async Task<AlefySongMetadata> GetSongByIdAsync(string songId)
        {
            // TODO: sostituire con endpoint reale
            // Esempio: GET {BaseUrl}/api/songs/{songId}
            await Task.Yield();
            return null;
        }

        public async Task<AlefySongResult> PrepareSongAsync(string songId)
        {
            var metadata = await GetSongByIdAsync(songId);
            if (metadata == null || string.IsNullOrEmpty(metadata.AudioUrl))
                return null;

            string localPath = Path.Combine(_cacheDir, $"{songId}.audio");
            if (File.Exists(localPath))
                return new AlefySongResult { LocalAudioPath = localPath, Metadata = metadata };

            using (var req = UnityWebRequest.Get(metadata.AudioUrl))
            {
                req.downloadHandler = new DownloadHandlerFile(localPath);
                if (!string.IsNullOrEmpty(AuthToken))
                    req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
                var op = req.SendWebRequest();
                while (!op.isDone)
                    await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Alefy: download failed {req.error}");
                    return null;
                }
            }

            return new AlefySongResult { LocalAudioPath = localPath, Metadata = metadata };
        }
    }
}
