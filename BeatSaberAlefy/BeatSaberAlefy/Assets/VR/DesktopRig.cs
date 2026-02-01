using UnityEngine;
using UnityEngine.InputSystem;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// In modalità desktop (Editor o build Windows senza headset XR) istanzia due spade
    /// e le muove con il mouse: tasto sinistro = lane sinistra, destro = lane destra.
    /// Se un dispositivo XR è attivo, non fa nulla.
    /// </summary>
    public class DesktopRig : MonoBehaviour
    {
        [Tooltip("Prefab della spada (Saber) da istanziare due volte")]
        public GameObject SaberPrefab;

        [Tooltip("GameplayDirector per GetAudioTime sui SliceDetector")]
        public GameplayDirector Director;

        [Tooltip("Camera per il raycast del mouse (null = Main Camera)")]
        public Camera Camera;

        [Tooltip("Distanza dal piano di hit davanti alla camera (metri). Deve coincidere con HitDistance dello SpawnController (es. 1.5) affinché le spade incontrano i cubi.")]
        public float PlaneDistance = 1.5f;

        [Tooltip("Usa il rig solo quando non c'è dispositivo XR attivo")]
        public bool OnlyWhenNoXR = true;

        GameObject _leftSaber;
        GameObject _rightSaber;
        bool _active;

        void Start()
        {
            if (OnlyWhenNoXR && XRDeviceActive())
                return;

            if (SaberPrefab == null || Director == null)
            {
                Debug.LogWarning("DesktopRig: assegna SaberPrefab e Director in Inspector.");
                return;
            }

            Camera = Camera != null ? Camera : Camera.main;
            if (Camera == null)
            {
                Debug.LogWarning("DesktopRig: nessuna camera.");
                return;
            }

            _leftSaber = Instantiate(SaberPrefab, transform);
            _leftSaber.name = "LeftSaber";
            SetupSaber(_leftSaber, 0);

            _rightSaber = Instantiate(SaberPrefab, transform);
            _rightSaber.name = "RightSaber";
            SetupSaber(_rightSaber, 1);

            PlaceAtRest();
            _active = true;
        }

        static bool XRDeviceActive()
        {
#if UNITY_EDITOR
            return false;
#else
            return UnityEngine.XR.XRSettings.isDeviceActive;
#endif
        }

        void SetupSaber(GameObject saber, int lane)
        {
            var det = saber.GetComponent<SliceDetector>();
            if (det != null)
            {
                det.SaberLane = lane;
                det.AudioTimeProvider = Director;
                det.AudioTimeMethodName = "GetAudioTime";
            }
        }

        void PlaceAtRest()
        {
            if (Camera == null) return;
            Vector3 c = Camera.transform.position + Camera.transform.forward * PlaneDistance;
            Vector3 r = Camera.transform.right;
            if (_leftSaber != null)
                _leftSaber.transform.position = c - r * 0.35f;
            if (_rightSaber != null)
                _rightSaber.transform.position = c + r * 0.35f;
            SetSaberRotation(Camera.transform.rotation);
        }

        void SetSaberRotation(Quaternion camRot)
        {
            if (_leftSaber != null) _leftSaber.transform.rotation = camRot;
            if (_rightSaber != null) _rightSaber.transform.rotation = camRot;
        }

        void Update()
        {
            if (!_active || Camera == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pos = mouse.position.ReadValue();
            bool left = mouse.leftButton.isPressed;
            bool right = mouse.rightButton.isPressed;

            var plane = new Plane(Camera.transform.forward, Camera.transform.position + Camera.transform.forward * PlaneDistance);
            var ray = Camera.ScreenPointToRay(new Vector3(pos.x, pos.y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 world = ray.GetPoint(enter);
                if (left && _leftSaber != null)
                    _leftSaber.transform.position = world;
                if (right && _rightSaber != null)
                    _rightSaber.transform.position = world;
            }

            SetSaberRotation(Camera.transform.rotation);
        }
    }
}
