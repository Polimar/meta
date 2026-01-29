using UnityEngine;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Aggiunge un TrailRenderer alla spada per effetto visivo stile Beat Saber.
    /// Attaccare al GameObject della spada (o al figlio "Blade").
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class SaberTrail : MonoBehaviour
    {
        [Tooltip("Colore del trail (es. blu per sinistra, rosa per destra)")]
        public Color TrailColor = new Color(0.2f, 0.5f, 1f, 0.8f);

        [Tooltip("Tempo in secondi che il trail resta visibile")]
        public float Time = 0.15f;

        [Tooltip("Spessore alla base e in cima")]
        public float WidthMultiplier = 0.03f;

        void Awake()
        {
            var tr = GetComponent<TrailRenderer>();
            if (tr == null) tr = gameObject.AddComponent<TrailRenderer>();
            tr.time = Time;
            tr.startWidth = WidthMultiplier;
            tr.endWidth = WidthMultiplier * 0.3f;
            tr.material = new Material(Shader.Find("Sprites/Default"));
            tr.startColor = TrailColor;
            tr.endColor = new Color(TrailColor.r, TrailColor.g, TrailColor.b, 0f);
            tr.autodestruct = false;
        }
    }
}
