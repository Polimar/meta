using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BeatSaberAlefy.UI;

namespace BeatSaberAlefy.VR
{
    /// <summary>
    /// Menu VR accessibile nel visore. Crea un Canvas world-space posizionato davanti al player.
    /// </summary>
    public class VRMenu : MonoBehaviour
    {
        [Tooltip("Distanza dal player (metri)")]
        public float Distance = 2f;

        [Tooltip("Altezza relativa al player (metri)")]
        public float Height = 0f;

        [Tooltip("Canvas del menu VR")]
        public Canvas MenuCanvas;

        [Tooltip("MenuController da collegare")]
        public MenuController MenuController;

        Camera _vrCamera;
        bool _menuVisible = true;

        void Start()
        {
            // Trova la camera VR
            var xrOrigin = UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                _vrCamera = xrOrigin.Camera;
            }
            else
            {
                _vrCamera = Camera.main;
            }

            // Crea il canvas se non esiste
            if (MenuCanvas == null)
            {
                CreateVRMenuCanvas();
            }

            // Collega MenuController se esiste
            if (MenuController == null)
            {
                MenuController = UnityEngine.Object.FindFirstObjectByType<MenuController>();
            }

            UpdateMenuPosition();
        }

        void CreateVRMenuCanvas()
        {
            var canvasGo = new GameObject("VR Menu Canvas");
            canvasGo.transform.SetParent(transform);
            MenuCanvas = canvasGo.AddComponent<Canvas>();
            MenuCanvas.renderMode = RenderMode.WorldSpace;
            MenuCanvas.worldCamera = _vrCamera;

            var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Imposta dimensioni del canvas (2m x 1.2m)
            var rectTransform = MenuCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(2f, 1.2f);
            rectTransform.localScale = Vector3.one * 0.001f; // Scala per world-space

            // Crea pannello principale
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Se MenuController esiste, copia la UI esistente
            var existingMenu = UnityEngine.Object.FindFirstObjectByType<MenuController>();
            if (existingMenu != null)
            {
                MenuController = existingMenu;
                // Collega i riferimenti del MenuController al nuovo canvas
                SetupMenuControllerUI(panel.transform, existingMenu);
            }
            else
            {
                // Crea UI base
                CreateBasicMenuUI(panel.transform);
            }
        }

        void SetupMenuControllerUI(Transform parent, MenuController menu)
        {
            // Crea StatusText
            if (menu.StatusText == null)
            {
                var statusGo = new GameObject("StatusText");
                statusGo.transform.SetParent(parent, false);
                var statusRect = statusGo.AddComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0.5f, 0.9f);
                statusRect.anchorMax = new Vector2(0.5f, 0.9f);
                statusRect.pivot = new Vector2(0.5f, 0.5f);
                statusRect.sizeDelta = new Vector2(1800, 80);
                var statusText = statusGo.AddComponent<Text>();
                statusText.text = "Carica lista tracce da Alefy";
                statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statusText.fontSize = 40;
                statusText.alignment = TextAnchor.MiddleCenter;
                statusText.color = Color.white;
                menu.StatusText = statusText;
            }

            // Crea ButtonLoadFromAlefy
            if (menu.ButtonLoadFromAlefy == null)
            {
                menu.ButtonLoadFromAlefy = CreateButton(parent, "ButtonLoadFromAlefy", "Carica da Alefy", 
                    new Vector2(0.5f, 0.75f), new Vector2(400, 80));
            }

            // Crea ButtonStartGame
            if (menu.ButtonStartGame == null)
            {
                menu.ButtonStartGame = CreateButton(parent, "ButtonStartGame", "Avvia", 
                    new Vector2(0.5f, 0.1f), new Vector2(300, 100));
            }

            // Crea TrackListContainer
            if (menu.AlefyTrackListContainer == null)
            {
                var listGo = new GameObject("TrackListContainer");
                listGo.transform.SetParent(parent, false);
                var listRect = listGo.AddComponent<RectTransform>();
                listRect.anchorMin = new Vector2(0.1f, 0.2f);
                listRect.anchorMax = new Vector2(0.9f, 0.7f);
                listRect.offsetMin = listRect.offsetMax = Vector2.zero;
                var layout = listGo.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10;
                layout.childControlHeight = false;
                layout.childControlWidth = true;
                menu.AlefyTrackListContainer = listGo.transform;
            }
        }

        void CreateBasicMenuUI(Transform parent)
        {
            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(parent, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0.9f);
            statusRect.anchorMax = new Vector2(0.5f, 0.9f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.sizeDelta = new Vector2(1800, 80);
            var statusText = statusGo.AddComponent<Text>();
            statusText.text = "Menu VR - Usa i controller per interagire";
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 40;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = Color.white;
        }

        Button CreateButton(Transform parent, string name, string text, Vector2 anchor, Vector2 size)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = anchor;
            btnRect.anchorMax = anchor;
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = size;
            var btnImage = btnGo.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.5f, 0.8f);
            var btn = btnGo.AddComponent<Button>();

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;

            return btn;
        }

        void Update()
        {
            UpdateMenuPosition();

            // Toggle menu con un pulsante (es. menu button del controller)
            // Per ora sempre visibile, puoi aggiungere toggle in seguito
        }

        void UpdateMenuPosition()
        {
            if (MenuCanvas == null || _vrCamera == null) return;

            // Posiziona il menu davanti al player
            Vector3 forward = _vrCamera.transform.forward;
            Vector3 right = _vrCamera.transform.right;
            Vector3 up = _vrCamera.transform.up;

            Vector3 position = _vrCamera.transform.position + 
                              forward * Distance + 
                              up * Height;

            MenuCanvas.transform.position = position;
            MenuCanvas.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        public void ToggleMenu()
        {
            _menuVisible = !_menuVisible;
            if (MenuCanvas != null)
                MenuCanvas.gameObject.SetActive(_menuVisible);
        }
    }
}
