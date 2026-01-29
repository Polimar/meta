using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace BeatSaberAlefy.Audio
{
    /// <summary>
    /// Carica un AudioClip da un file locale (es. cache Alefy).
    /// </summary>
    public static class AudioClipLoader
    {
        /// <summary>
        /// Carica un clip da path assoluto (es. Application.temporaryCachePath + "/AlefyCache/1.mp3").
        /// </summary>
        public static IEnumerator LoadFromFile(string absolutePath, System.Action<AudioClip> onLoaded, System.Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                onError?.Invoke("Path vuoto");
                yield break;
            }

            string url = "file://" + absolutePath;
            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                var op = req.SendWebRequest();
                while (!op.isDone)
                    yield return null;

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(req.error);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(req);
                onLoaded?.Invoke(clip);
            }
        }
    }
}
