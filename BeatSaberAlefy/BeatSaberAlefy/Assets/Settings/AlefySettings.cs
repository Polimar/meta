using UnityEngine;

namespace BeatSaberAlefy.Alefy
{
    /// <summary>
    /// ScriptableObject per configurare URL base e credenziali API alefy.
    /// Non committare valori reali: usare un asset in .gitignore o variabili d'ambiente.
    /// </summary>
    public class AlefySettings : ScriptableObject
    {
        public const string ResourcePath = "Settings/AlefySettings";

        [Tooltip("URL base API (es. https://alefy.alevale.it)")]
        public string BaseUrl = "https://alefy.alevale.it";

        [Tooltip("Token o API key per autenticazione (lasciare vuoto in repo)")]
        public string AuthToken;

        public static AlefySettings Load()
        {
            var settings = Resources.Load<AlefySettings>(ResourcePath);
            if (settings == null)
            {
                settings = CreateInstance<AlefySettings>();
#if UNITY_EDITOR
                string dir = "Assets/Resources/Settings";
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                UnityEditor.AssetDatabase.CreateAsset(settings, dir + "/AlefySettings.asset");
#endif
            }
            return settings;
        }
    }
}
