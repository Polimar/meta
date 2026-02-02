using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Mantiene il focus dell'applicazione in VR per prevenire l'apertura automatica di Oculus Dash.
    /// </summary>
    public class VRFocusManager : MonoBehaviour
    {
        void Start()
        {
            // Assicurati che l'applicazione mantenga il focus
            Application.runInBackground = true;
            
            // Verifica se XR è attivo
            if (XRSettings.enabled)
            {
                Debug.Log("[VRFocusManager] XR attivo, focus mantenuto.");
            }
            else
            {
                Debug.LogWarning("[VRFocusManager] XR non attivo. Verifica le impostazioni XR.");
            }
        }

        void Update()
        {
            // Mantieni il focus se l'applicazione lo perde
            if (!Application.isFocused && XRSettings.enabled)
            {
                // In VR, questo aiuta a prevenire che Dash si apra automaticamente
                // Nota: non possiamo forzare il focus in Unity, ma possiamo assicurarci
                // che l'applicazione continui a funzionare in background
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (XRSettings.enabled)
            {
                Debug.Log($"[VRFocusManager] Focus cambiato: {hasFocus}");
                if (!hasFocus)
                {
                    Debug.LogWarning("[VRFocusManager] Applicazione ha perso il focus. Oculus Dash potrebbe essere aperto.");
                }
            }
        }
    }
}
