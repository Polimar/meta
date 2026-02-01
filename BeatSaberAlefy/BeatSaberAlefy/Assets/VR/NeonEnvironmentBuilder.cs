using UnityEngine;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Costruisce un ambiente neon minimale: luci colorate e piano/parati con emissione.
    /// Eseguire da menu BeatSaberAlefy/Setup/Create Neon Environment o chiamare Build() da Editor.
    /// </summary>
    public class NeonEnvironmentBuilder : MonoBehaviour
    {
        [Tooltip("Colore neon principale (es. cyan)")]
        public Color NeonPrimary = new Color(0f, 0.8f, 1f);
        [Tooltip("Colore neon secondario (es. magenta)")]
        public Color NeonSecondary = new Color(1f, 0.2f, 0.8f);
        [Tooltip("Intensità luci")]
        public float LightIntensity = 2f;

        public void Build()
        {
            var root = new GameObject("NeonEnvironment");
            root.transform.SetParent(transform);

            var light1 = new GameObject("NeonLight_Cyan");
            light1.transform.SetParent(root.transform);
            light1.transform.localPosition = new Vector3(2f, 2f, 2f);
            var l1 = light1.AddComponent<Light>();
            l1.type = LightType.Point;
            l1.color = NeonPrimary;
            l1.intensity = LightIntensity;
            l1.range = 10f;

            var light2 = new GameObject("NeonLight_Magenta");
            light2.transform.SetParent(root.transform);
            light2.transform.localPosition = new Vector3(-2f, 2f, 2f);
            var l2 = light2.AddComponent<Light>();
            l2.type = LightType.Point;
            l2.color = NeonSecondary;
            l2.intensity = LightIntensity;
            l2.range = 10f;

            var floorGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floorGo.name = "NeonFloor";
            floorGo.transform.SetParent(root.transform);
            floorGo.transform.localPosition = new Vector3(0f, -1.5f, 0f);
            floorGo.transform.localScale = new Vector3(4f, 1f, 4f);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            var floorMat = new Material(shader);
            if (floorMat != null)
            {
                floorMat.color = new Color(0.05f, 0.05f, 0.15f);
                floorMat.EnableKeyword("_EMISSION");
                floorMat.SetColor("_EmissionColor", NeonPrimary * 0.3f);
                floorGo.GetComponent<Renderer>().sharedMaterial = floorMat;
            }
            var dancefloor = floorGo.AddComponent<DancefloorController>();
            dancefloor.BaseEmissionColor = NeonPrimary;
            dancefloor.BaseEmissionStrength = 0.3f;
            dancefloor.PulseStrength = 0.8f;
            dancefloor.PulseDuration = 0.2f;
        }
    }
}
