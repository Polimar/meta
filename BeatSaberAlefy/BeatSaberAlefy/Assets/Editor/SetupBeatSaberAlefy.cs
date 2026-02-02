#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using BeatSaberAlefy.VR;
using BeatSaberAlefy.BeatMap;
using BeatSaberAlefy.UI;
using BeatSaberAlefy.Alefy;
using Unity.XR.CoreUtils;
using System;
using System.Reflection;

namespace BeatSaberAlefy.Editor
{
    /// <summary>
    /// Menu per creare prefab Cubo e Saber, scene Menu e Game, e AlefySettings.
    /// </summary>
    public static class SetupBeatSaberAlefy
    {
        const string MenuRoot = "BeatSaberAlefy/Setup";

        [MenuItem(MenuRoot + "/Create Alefy Settings", false, 0)]
        static void CreateAlefySettings()
        {
            string dir = "Assets/Resources/Settings";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets"))
                    return;
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Settings"))
                AssetDatabase.CreateFolder("Assets/Resources", "Settings");
            string path = dir + "/AlefySettings.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AlefySettings>(path);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("AlefySettings già presente. Inserisci il token in Inspector (AuthToken).");
                return;
            }
            var settings = ScriptableObject.CreateInstance<AlefySettings>();
            settings.BaseUrl = "https://alefy.alevale.it";
            settings.AuthToken = "";
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
            Debug.Log("Creato " + path + ". Inserisci il token API in Inspector (AuthToken) e salva.");
        }

        [MenuItem(MenuRoot + "/Create Cube Materials", false, 11)]
        static void CreateCubeMaterials()
        {
            EnsureDirectory("Assets/Materials/Cube_Blue.mat");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) { Debug.LogWarning("Shader Lit/Standard non trovato."); return; }
            var blue = new Material(shader);
            blue.name = "Cube_Blue";
            blue.color = new Color(0.2f, 0.4f, 1f);
            if (blue.HasProperty("_Smoothness")) blue.SetFloat("_Smoothness", 0.6f);
            AssetDatabase.CreateAsset(blue, "Assets/Materials/Cube_Blue.mat");
            var pink = new Material(shader);
            pink.name = "Cube_Pink";
            pink.color = new Color(1f, 0.3f, 0.6f);
            if (pink.HasProperty("_Smoothness")) pink.SetFloat("_Smoothness", 0.6f);
            AssetDatabase.CreateAsset(pink, "Assets/Materials/Cube_Pink.mat");
            AssetDatabase.SaveAssets();
            Debug.Log("Materiali Cube_Blue e Cube_Pink creati in Assets/Materials. Assegna texture Albedo/Normal in Inspector.");
        }

        [MenuItem(MenuRoot + "/Assign Cube Material to Prefab", false, 14)]
        static void AssignCubeMaterialToPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cubes/Cube.prefab");
            if (prefab == null) { Debug.LogWarning("Prefab Cube non trovato."); return; }
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cube_Blue.mat");
            if (mat == null) { Debug.LogWarning("Esegui prima Create Cube Materials."); return; }
            var path = AssetDatabase.GetAssetPath(prefab);
            var contents = PrefabUtility.LoadPrefabContents(path);
            var rend = contents.GetComponent<Renderer>();
            if (rend != null) { rend.sharedMaterial = mat; }
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log("Materiale Cube_Blue assegnato al prefab Cube.");
        }

        [MenuItem(MenuRoot + "/Create Cube Prefab", false, 1)]
        static void CreateCubePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Cube";
            go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;
            go.AddComponent<Sliceable>();
            go.AddComponent<CubeMover>();
            var cubeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cube_Blue.mat");
            if (cubeMat != null)
                go.GetComponent<Renderer>().sharedMaterial = cubeMat;

            string path = "Assets/Cubes/Cube.prefab";
            EnsureDirectory(path);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"Created {path}");
        }

        [MenuItem(MenuRoot + "/Create Saber Prefab", false, 2)]
        static void CreateSaberPrefab()
        {
            var go = new GameObject("Saber");
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Blade";
            cylinder.transform.SetParent(go.transform, false);
            cylinder.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);
            cylinder.transform.localPosition = new Vector3(0, 0.5f, 0);
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.1f, 1f, 0.1f);
            box.center = new Vector3(0, 0.5f, 0);
            go.AddComponent<SliceDetector>();

            string path = "Assets/VR/Saber.prefab";
            EnsureDirectory(path);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"Created {path}");
        }

        [MenuItem(MenuRoot + "/Create Menu Scene", false, 10)]
        static void CreateMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam != null) cam.gameObject.name = "Main Camera";

            EnsureEventSystem();

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(panel.transform, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 1f);
            statusRect.anchorMax = new Vector2(0.5f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0, -20);
            statusRect.sizeDelta = new Vector2(600, 40);
            var statusText = statusGo.AddComponent<Text>();
            statusText.text = "Carica lista tracce da Alefy";
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 22;
            statusText.alignment = TextAnchor.MiddleCenter;

            var loadBtnGo = new GameObject("ButtonLoadFromAlefy");
            loadBtnGo.transform.SetParent(panel.transform, false);
            var loadBtnRect = loadBtnGo.AddComponent<RectTransform>();
            loadBtnRect.anchorMin = new Vector2(0.5f, 1f);
            loadBtnRect.anchorMax = new Vector2(0.5f, 1f);
            loadBtnRect.pivot = new Vector2(0.5f, 1f);
            loadBtnRect.anchoredPosition = new Vector2(0, -70);
            loadBtnRect.sizeDelta = new Vector2(220, 40);
            var loadBtnImage = loadBtnGo.AddComponent<Image>();
            loadBtnImage.color = new Color(0.2f, 0.5f, 0.8f);
            var loadBtn = loadBtnGo.AddComponent<Button>();
            var loadLabelGo = new GameObject("Text");
            loadLabelGo.transform.SetParent(loadBtnGo.transform, false);
            var loadLabelRect = loadLabelGo.AddComponent<RectTransform>();
            loadLabelRect.anchorMin = Vector2.zero;
            loadLabelRect.anchorMax = Vector2.one;
            loadLabelRect.offsetMin = loadLabelRect.offsetMax = Vector2.zero;
            var loadLabel = loadLabelGo.AddComponent<Text>();
            loadLabel.text = "Carica da Alefy";
            loadLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            loadLabel.fontSize = 20;
            loadLabel.alignment = TextAnchor.MiddleCenter;

            var searchInputGo = new GameObject("SearchInputField");
            searchInputGo.transform.SetParent(panel.transform, false);
            var searchInputRect = searchInputGo.AddComponent<RectTransform>();
            searchInputRect.anchorMin = new Vector2(0.5f, 1f);
            searchInputRect.anchorMax = new Vector2(0.5f, 1f);
            searchInputRect.pivot = new Vector2(0.5f, 1f);
            searchInputRect.anchoredPosition = new Vector2(-120, -120);
            searchInputRect.sizeDelta = new Vector2(200, 36);
            var searchInputImage = searchInputGo.AddComponent<Image>();
            searchInputImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            var searchInput = searchInputGo.AddComponent<InputField>();
            var searchPlaceholderGo = new GameObject("Placeholder");
            searchPlaceholderGo.transform.SetParent(searchInputGo.transform, false);
            var searchPlaceholderRect = searchPlaceholderGo.AddComponent<RectTransform>();
            searchPlaceholderRect.anchorMin = Vector2.zero;
            searchPlaceholderRect.anchorMax = Vector2.one;
            searchPlaceholderRect.offsetMin = new Vector2(10, 6);
            searchPlaceholderRect.offsetMax = new Vector2(-10, -6);
            var searchPlaceholder = searchPlaceholderGo.AddComponent<Text>();
            searchPlaceholder.text = "Cerca tracce...";
            searchPlaceholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            searchPlaceholder.fontSize = 18;
            searchPlaceholder.color = new Color(0.5f, 0.5f, 0.5f);
            var searchTextGo = new GameObject("Text");
            searchTextGo.transform.SetParent(searchInputGo.transform, false);
            var searchTextRect = searchTextGo.AddComponent<RectTransform>();
            searchTextRect.anchorMin = Vector2.zero;
            searchTextRect.anchorMax = Vector2.one;
            searchTextRect.offsetMin = new Vector2(10, 6);
            searchTextRect.offsetMax = new Vector2(-10, -6);
            var searchText = searchTextGo.AddComponent<Text>();
            searchText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            searchText.fontSize = 18;
            searchInput.textComponent = searchText;
            searchInput.placeholder = searchPlaceholder;

            var searchBtnGo = new GameObject("ButtonSearch");
            searchBtnGo.transform.SetParent(panel.transform, false);
            var searchBtnRect = searchBtnGo.AddComponent<RectTransform>();
            searchBtnRect.anchorMin = new Vector2(0.5f, 1f);
            searchBtnRect.anchorMax = new Vector2(0.5f, 1f);
            searchBtnRect.pivot = new Vector2(0.5f, 1f);
            searchBtnRect.anchoredPosition = new Vector2(120, -120);
            searchBtnRect.sizeDelta = new Vector2(100, 36);
            var searchBtnImage = searchBtnGo.AddComponent<Image>();
            searchBtnImage.color = new Color(0.3f, 0.6f, 0.3f);
            var searchBtn = searchBtnGo.AddComponent<Button>();
            var searchBtnLabelGo = new GameObject("Text");
            searchBtnLabelGo.transform.SetParent(searchBtnGo.transform, false);
            var searchBtnLabelRect = searchBtnLabelGo.AddComponent<RectTransform>();
            searchBtnLabelRect.anchorMin = Vector2.zero;
            searchBtnLabelRect.anchorMax = Vector2.one;
            searchBtnLabelRect.offsetMin = searchBtnLabelRect.offsetMax = Vector2.zero;
            var searchBtnLabel = searchBtnLabelGo.AddComponent<Text>();
            searchBtnLabel.text = "Cerca";
            searchBtnLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            searchBtnLabel.fontSize = 18;
            searchBtnLabel.alignment = TextAnchor.MiddleCenter;

            var listContainerGo = new GameObject("TrackListContainer");
            listContainerGo.transform.SetParent(panel.transform, false);
            var listRect = listContainerGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0, -50);
            listRect.sizeDelta = new Vector2(500, 400);
            var listBg = listContainerGo.AddComponent<Image>();
            listBg.color = new Color(0.12f, 0.12f, 0.18f, 0.92f);
            listBg.raycastTarget = true;
            var listContent = listContainerGo.AddComponent<VerticalLayoutGroup>();
            listContent.childAlignment = TextAnchor.UpperCenter;
            listContent.spacing = 4;
            listContent.childControlHeight = true;
            listContent.childControlWidth = true;
            listContent.childForceExpandHeight = false;
            listContent.childForceExpandWidth = true;
            listContent.padding = new RectOffset(10, 10, 10, 10);

            var startBtnGo = new GameObject("ButtonStartGame");
            startBtnGo.transform.SetParent(panel.transform, false);
            var startBtnRect = startBtnGo.AddComponent<RectTransform>();
            startBtnRect.anchorMin = new Vector2(0.5f, 0f);
            startBtnRect.anchorMax = new Vector2(0.5f, 0f);
            startBtnRect.pivot = new Vector2(0.5f, 0f);
            startBtnRect.anchoredPosition = new Vector2(0, 30);
            startBtnRect.sizeDelta = new Vector2(200, 50);
            var startBtnImage = startBtnGo.AddComponent<Image>();
            startBtnImage.color = new Color(0.2f, 0.7f, 0.3f);
            var startBtn = startBtnGo.AddComponent<Button>();
            var startLabelGo = new GameObject("Text");
            startLabelGo.transform.SetParent(startBtnGo.transform, false);
            var startLabelRect = startLabelGo.AddComponent<RectTransform>();
            startLabelRect.anchorMin = Vector2.zero;
            startLabelRect.anchorMax = Vector2.one;
            startLabelRect.offsetMin = startLabelRect.offsetMax = Vector2.zero;
            var startLabel = startLabelGo.AddComponent<Text>();
            startLabel.text = "Avvia";
            startLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            startLabel.fontSize = 24;
            startLabel.alignment = TextAnchor.MiddleCenter;

            var trackButtonPrefab = CreateTrackButtonPrefab();
            var menu = panel.AddComponent<MenuController>();
            menu.ButtonLoadFromAlefy = loadBtn;
            menu.ButtonSearch = searchBtn;
            menu.SearchInputField = searchInput;
            menu.ButtonStartGame = startBtn;
            menu.StatusText = statusText;
            menu.AlefyTrackListContainer = listContainerGo.transform;
            menu.AlefyTrackButtonPrefab = trackButtonPrefab;

            string path = "Assets/Scenes/Menu.unity";
            EnsureDirectory(path);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Created {path}. MenuController collegato. Inserisci token in AlefySettings e premi Play.");
        }

        static GameObject CreateTrackButtonPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/TrackButton.prefab");
            if (existing != null) return existing;

            var go = new GameObject("TrackButton");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480, 44);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            go.AddComponent<Button>();
            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 4);
            labelRect.offsetMax = new Vector2(-10, -4);
            var label = labelGo.AddComponent<Text>();
            label.text = "Title - Artist";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleLeft;

            string path = "Assets/UI/TrackButton.prefab";
            EnsureDirectory(path);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        [MenuItem(MenuRoot + "/Create Game Scene", false, 11)]
        static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam != null) cam.gameObject.name = "Main Camera";

            EnsureEventSystem();

            var gameStateGo = new GameObject("GameState");
            gameStateGo.AddComponent<GameState>();

            var directorGo = new GameObject("GameplayDirector");
            var audioSource = directorGo.AddComponent<AudioSource>();
            var director = directorGo.AddComponent<GameplayDirector>();
            directorGo.AddComponent<DesktopTestBootstrap>();
            director.AudioSource = audioSource;
            director.PlayerForward = cam != null ? cam.transform : directorGo.transform;

            var spawnGo = new GameObject("SpawnController");
            spawnGo.transform.SetParent(directorGo.transform);
            var spawn = spawnGo.AddComponent<SpawnController>();
            spawn.AudioSource = audioSource;
            spawn.PlayerForward = cam != null ? cam.transform : directorGo.transform;
            director.SpawnController = spawn;

            var cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cubes/Cube.prefab");
            if (cubePrefab != null) spawn.CubePrefab = cubePrefab;

            var desktopRigGo = new GameObject("DesktopRig");
            var desktopRig = desktopRigGo.AddComponent<DesktopRig>();
            desktopRig.Director = director;
            var saberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VR/Saber.prefab");
            if (saberPrefab != null) desktopRig.SaberPrefab = saberPrefab;
            if (cam != null) desktopRig.Camera = cam;

            CreateGameplayCanvas();

            var neonRoot = new GameObject("NeonEnvironmentRoot");
            var neonBuilder = neonRoot.AddComponent<NeonEnvironmentBuilder>();
            neonBuilder.Build();

            string path = "Assets/Scenes/Game.unity";
            EnsureDirectory(path);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Created {path}. In Editor/Windows: usa mouse (sinistro=left, destro=right) per le spade. Per VR aggiungi XR Origin e Saber sui controller.");
        }

        [MenuItem(MenuRoot + "/Create Neon Environment in scene", false, 12)]
        static void CreateNeonEnvironmentInScene()
        {
            var root = new GameObject("NeonEnvironmentRoot");
            var builder = root.AddComponent<NeonEnvironmentBuilder>();
            builder.Build();
            Selection.activeGameObject = root;
            Debug.Log("Neon environment creato. Aggiungi Volume con Bloom per post-processing.");
        }

        [MenuItem(MenuRoot + "/Assign 360 Skybox", false, 13)]
        static void Assign360Skybox()
        {
            EnsureDirectory("Assets/Resources/SkyboxRoom.mat");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environment/Room360.png");
            if (tex == null)
            {
                Debug.LogWarning("Texture non trovata: Assets/Textures/Environment/Room360.png. Copia mkzjwfbf.png lì e rinominala Room360.png.");
                return;
            }
            var shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
            {
                Debug.LogWarning("Shader Skybox/Panoramic non trovato. Verifica che il progetto includa il modulo di rendering appropriato.");
                return;
            }
            var mat = new Material(shader);
            mat.SetTexture("_MainTex", tex);
            AssetDatabase.CreateAsset(mat, "Assets/Resources/SkyboxRoom.mat");
            AssetDatabase.SaveAssets();
            RenderSettings.skybox = mat;
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("Skybox 360° creato (Assets/Resources/SkyboxRoom.mat) e assegnato alla scena. In Game verrà applicato a runtime se non già impostato.");
        }

        [MenuItem(MenuRoot + "/Create All (Prefabs + Scenes)", false, 20)]
        static void CreateAll()
        {
            CreateCubePrefab();
            CreateSaberPrefab();
            CreateMenuScene();
            CreateGameScene();
            AddScenesToBuildSettings();
        }

        [MenuItem(MenuRoot + "/Add Lobby UI to current scene", false, 24)]
        static void AddLobbyUIToScene()
        {
            EnsureEventSystem();
            var menu = UnityEngine.Object.FindFirstObjectByType<MenuController>();
            if (menu == null)
            {
                var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogWarning("Nessun Canvas nella scena. Esegui prima Create Menu Scene.");
                    return;
                }
                var panel = new GameObject("Panel");
                panel.transform.SetParent(canvas.transform, false);
                var panelRect = panel.AddComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
                menu = panel.AddComponent<MenuController>();
            }

            var panelTransform = menu.transform;
            if (menu.StatusText == null)
            {
                var statusGo = new GameObject("StatusText");
                statusGo.transform.SetParent(panelTransform, false);
                var statusRect = statusGo.AddComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0.5f, 1f);
                statusRect.anchorMax = new Vector2(0.5f, 1f);
                statusRect.pivot = new Vector2(0.5f, 1f);
                statusRect.anchoredPosition = new Vector2(0, -20);
                statusRect.sizeDelta = new Vector2(600, 40);
                var statusText = statusGo.AddComponent<Text>();
                statusText.text = "Carica lista tracce da Alefy";
                statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statusText.fontSize = 22;
                statusText.alignment = TextAnchor.MiddleCenter;
                menu.StatusText = statusText;
            }
            if (menu.ButtonLoadFromAlefy == null)
            {
                var loadBtnGo = new GameObject("ButtonLoadFromAlefy");
                loadBtnGo.transform.SetParent(panelTransform, false);
                var loadBtnRect = loadBtnGo.AddComponent<RectTransform>();
                loadBtnRect.anchorMin = new Vector2(0.5f, 1f);
                loadBtnRect.anchorMax = new Vector2(0.5f, 1f);
                loadBtnRect.pivot = new Vector2(0.5f, 1f);
                loadBtnRect.anchoredPosition = new Vector2(0, -70);
                loadBtnRect.sizeDelta = new Vector2(220, 40);
                loadBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);
                var loadBtn = loadBtnGo.AddComponent<Button>();
                var loadLabelGo = new GameObject("Text");
                loadLabelGo.transform.SetParent(loadBtnGo.transform, false);
                var loadLabelRect = loadLabelGo.AddComponent<RectTransform>();
                loadLabelRect.anchorMin = Vector2.zero;
                loadLabelRect.anchorMax = Vector2.one;
                loadLabelRect.offsetMin = loadLabelRect.offsetMax = Vector2.zero;
                var loadLabel = loadLabelGo.AddComponent<Text>();
                loadLabel.text = "Carica da Alefy";
                loadLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                loadLabel.fontSize = 20;
                loadLabel.alignment = TextAnchor.MiddleCenter;
                menu.ButtonLoadFromAlefy = loadBtn;
            }
            if (menu.SearchInputField == null)
            {
                var searchInputGo = new GameObject("SearchInputField");
                searchInputGo.transform.SetParent(panelTransform, false);
                var searchInputRect = searchInputGo.AddComponent<RectTransform>();
                searchInputRect.anchorMin = new Vector2(0.5f, 1f);
                searchInputRect.anchorMax = new Vector2(0.5f, 1f);
                searchInputRect.pivot = new Vector2(0.5f, 1f);
                searchInputRect.anchoredPosition = new Vector2(-120, -120);
                searchInputRect.sizeDelta = new Vector2(200, 36);
                searchInputGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
                var searchInput = searchInputGo.AddComponent<InputField>();
                var searchPlaceholderGo = new GameObject("Placeholder");
                searchPlaceholderGo.transform.SetParent(searchInputGo.transform, false);
                var searchPlaceholderRect = searchPlaceholderGo.AddComponent<RectTransform>();
                searchPlaceholderRect.anchorMin = Vector2.zero;
                searchPlaceholderRect.anchorMax = Vector2.one;
                searchPlaceholderRect.offsetMin = new Vector2(10, 6);
                searchPlaceholderRect.offsetMax = new Vector2(-10, -6);
                var searchPlaceholder = searchPlaceholderGo.AddComponent<Text>();
                searchPlaceholder.text = "Cerca tracce...";
                searchPlaceholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                searchPlaceholder.fontSize = 18;
                searchPlaceholder.color = new Color(0.5f, 0.5f, 0.5f);
                var searchTextGo = new GameObject("Text");
                searchTextGo.transform.SetParent(searchInputGo.transform, false);
                var searchTextRect = searchTextGo.AddComponent<RectTransform>();
                searchTextRect.anchorMin = Vector2.zero;
                searchTextRect.anchorMax = Vector2.one;
                searchTextRect.offsetMin = new Vector2(10, 6);
                searchTextRect.offsetMax = new Vector2(-10, -6);
                var searchText = searchTextGo.AddComponent<Text>();
                searchText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                searchText.fontSize = 18;
                searchInput.textComponent = searchText;
                searchInput.placeholder = searchPlaceholder;
                menu.SearchInputField = searchInput;
            }
            if (menu.ButtonSearch == null)
            {
                var searchBtnGo = new GameObject("ButtonSearch");
                searchBtnGo.transform.SetParent(panelTransform, false);
                var searchBtnRect = searchBtnGo.AddComponent<RectTransform>();
                searchBtnRect.anchorMin = new Vector2(0.5f, 1f);
                searchBtnRect.anchorMax = new Vector2(0.5f, 1f);
                searchBtnRect.pivot = new Vector2(0.5f, 1f);
                searchBtnRect.anchoredPosition = new Vector2(120, -120);
                searchBtnRect.sizeDelta = new Vector2(100, 36);
                searchBtnGo.AddComponent<Image>().color = new Color(0.3f, 0.6f, 0.3f);
                var searchBtn = searchBtnGo.AddComponent<Button>();
                var searchBtnLabelGo = new GameObject("Text");
                searchBtnLabelGo.transform.SetParent(searchBtnGo.transform, false);
                var searchBtnLabelRect = searchBtnLabelGo.AddComponent<RectTransform>();
                searchBtnLabelRect.anchorMin = Vector2.zero;
                searchBtnLabelRect.anchorMax = Vector2.one;
                searchBtnLabelRect.offsetMin = searchBtnLabelRect.offsetMax = Vector2.zero;
                var searchBtnLabel = searchBtnLabelGo.AddComponent<Text>();
                searchBtnLabel.text = "Cerca";
                searchBtnLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                searchBtnLabel.fontSize = 18;
                searchBtnLabel.alignment = TextAnchor.MiddleCenter;
                menu.ButtonSearch = searchBtn;
            }
            if (menu.AlefyTrackListContainer == null)
            {
                var listContainerGo = new GameObject("TrackListContainer");
                listContainerGo.transform.SetParent(panelTransform, false);
                var listRect = listContainerGo.AddComponent<RectTransform>();
                listRect.anchorMin = new Vector2(0.5f, 0.5f);
                listRect.anchorMax = new Vector2(0.5f, 0.5f);
                listRect.pivot = new Vector2(0.5f, 0.5f);
                listRect.anchoredPosition = new Vector2(0, -50);
                listRect.sizeDelta = new Vector2(500, 400);
                listContainerGo.AddComponent<VerticalLayoutGroup>();
                menu.AlefyTrackListContainer = listContainerGo.transform;
            }
            if (menu.ButtonStartGame == null)
            {
                var startBtnGo = new GameObject("ButtonStartGame");
                startBtnGo.transform.SetParent(panelTransform, false);
                var startBtnRect = startBtnGo.AddComponent<RectTransform>();
                startBtnRect.anchorMin = new Vector2(0.5f, 0f);
                startBtnRect.anchorMax = new Vector2(0.5f, 0f);
                startBtnRect.pivot = new Vector2(0.5f, 0f);
                startBtnRect.anchoredPosition = new Vector2(0, 30);
                startBtnRect.sizeDelta = new Vector2(200, 50);
                startBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f);
                var startBtn = startBtnGo.AddComponent<Button>();
                var startLabelGo = new GameObject("Text");
                startLabelGo.transform.SetParent(startBtnGo.transform, false);
                var startLabelRect = startLabelGo.AddComponent<RectTransform>();
                startLabelRect.anchorMin = Vector2.zero;
                startLabelRect.anchorMax = Vector2.one;
                startLabelRect.offsetMin = startLabelRect.offsetMax = Vector2.zero;
                var startLabel = startLabelGo.AddComponent<Text>();
                startLabel.text = "Avvia";
                startLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                startLabel.fontSize = 24;
                startLabel.alignment = TextAnchor.MiddleCenter;
                menu.ButtonStartGame = startBtn;
            }
            if (menu.AlefyTrackButtonPrefab == null)
                menu.AlefyTrackButtonPrefab = CreateTrackButtonPrefab();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Selection.activeGameObject = menu.gameObject;
            Debug.Log("Lobby UI aggiunta e collegata al MenuController. Salva la scena (Ctrl+S) e premi Play.");
        }

        [MenuItem(MenuRoot + "/Add EventSystem to scene (se i pulsanti non rispondono)", false, 24)]
        static void AddEventSystemToScene()
        {
            EnsureEventSystem();
            Debug.Log("EventSystem aggiunto. Se i pulsanti ancora non rispondono, verifica che MenuController sia sul Panel e che i pulsanti siano collegati in Inspector.");
        }

        [MenuItem(MenuRoot + "/Add Desktop Rig to current scene", false, 25)]
        static void AddDesktopRigToScene()
        {
            var director = UnityEngine.Object.FindFirstObjectByType<GameplayDirector>();
            if (director == null)
            {
                Debug.LogWarning("Nessun GameplayDirector nella scena. Apri la scena Game o creala con Create Game Scene.");
                return;
            }
            var existing = UnityEngine.Object.FindFirstObjectByType<DesktopRig>();
            if (existing != null)
            {
                Debug.Log("DesktopRig già presente nella scena.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            var desktopRigGo = new GameObject("DesktopRig");
            var desktopRig = desktopRigGo.AddComponent<DesktopRig>();
            desktopRig.Director = director;
            var saberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VR/Saber.prefab");
            if (saberPrefab != null) desktopRig.SaberPrefab = saberPrefab;
            desktopRig.Camera = Camera.main;
            Selection.activeGameObject = desktopRigGo;
            Debug.Log("DesktopRig aggiunto. In Play: mouse sinistro = left saber, destro = right saber.");
        }

        [MenuItem(MenuRoot + "/Add Game State and Gameplay UI to current scene", false, 26)]
        static void AddGameStateAndGameplayUIToScene()
        {
            EnsureEventSystem();
            if (UnityEngine.Object.FindFirstObjectByType<GameState>() == null)
            {
                var go = new GameObject("GameState");
                go.AddComponent<GameState>();
                Debug.Log("GameState aggiunto.");
            }
            if (UnityEngine.Object.FindFirstObjectByType<GameplayUI>() == null)
            {
                CreateGameplayCanvas();
                Debug.Log("GameplayCanvas con GameplayUI aggiunto.");
            }
            var spawn = UnityEngine.Object.FindFirstObjectByType<SpawnController>();
            if (spawn != null && spawn.CubePrefab == null)
            {
                var cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cubes/Cube.prefab");
                if (cubePrefab != null)
                {
                    spawn.CubePrefab = cubePrefab;
                    Debug.Log("SpawnController: assegnato CubePrefab.");
                }
            }
        }

        [MenuItem(MenuRoot + "/Attiva tutta l'UI nella scena (per editing)", false, 27)]
        static void ActivateAllUIInScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Esci da Play mode prima di usare questo comando.");
                return;
            }
            int count = 0;
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.gameObject.activeSelf)
                {
                    canvas.gameObject.SetActive(true);
                    count++;
                }
            }
            var panel = GameObject.Find("Panel");
            if (panel != null && !panel.activeSelf) { panel.SetActive(true); count++; }
            var panelGo = GameObject.Find("PanelGameOver");
            if (panelGo != null && !panelGo.activeSelf) { panelGo.SetActive(true); count++; }
            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log($"Attivati {count} oggetti UI. Salva la scena (Ctrl+S) se vuoi che restino attivi; a runtime la lobby e il pannello Game Over vengono gestiti dallo script.");
            }
            else
                Debug.Log("Nessun oggetto UI disattivato trovato (Canvas, Panel, PanelGameOver).");
        }

        [MenuItem(MenuRoot + "/Add XR Origin (VR) to current scene", false, 15)]
        static void AddXROriginToScene()
        {
            // Verifica se XR Origin esiste già
            var existing = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (existing != null)
            {
                Debug.LogWarning("XR Origin già presente nella scena. Selezionato quello esistente.");
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            // Disabilita la vecchia Main Camera se esiste e non è parte di XR Origin
            var oldCam = Camera.main;
            if (oldCam != null && oldCam.GetComponent<TrackedPoseDriver>() == null)
            {
                oldCam.gameObject.SetActive(false);
                Debug.Log("Vecchia Main Camera disabilitata.");
            }

            // Crea XR Origin
            var xrOriginGo = new GameObject("XR Origin (VR)");
            var xrOrigin = xrOriginGo.AddComponent<XROrigin>();
            
            // Crea Camera Offset
            var cameraOffsetGo = new GameObject("Camera Offset");
            cameraOffsetGo.transform.SetParent(xrOriginGo.transform, false);
            cameraOffsetGo.transform.localPosition = Vector3.zero;
            cameraOffsetGo.transform.localRotation = Quaternion.identity;

            // Crea Main Camera
            var cameraGo = new GameObject("Main Camera");
            cameraGo.transform.SetParent(cameraOffsetGo.transform, false);
            cameraGo.transform.localPosition = new Vector3(0, 1.1176f, 0); // Altezza standard VR
            cameraGo.transform.localRotation = Quaternion.identity;
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.192f, 0.302f, 0.475f, 0f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.fieldOfView = 60f;
            camera.depth = 0;
            camera.stereoTargetEye = StereoTargetEyeMask.Both;

            // Aggiungi TrackedPoseDriver per il tracking VR
            var trackedPose = cameraGo.AddComponent<TrackedPoseDriver>();
            trackedPose.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPose.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            // Aggiungi AudioListener
            cameraGo.AddComponent<AudioListener>();

            // Configura XR Origin
            xrOrigin.Camera = camera;
            // OriginBaseGameObject non esiste più in Unity 6, viene gestito automaticamente
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGo;
            xrOrigin.CameraYOffset = 1.1176f;
            // Usa Floor per ambiente fisso (l'ambiente non ruota con la testa)
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            // Aggiungi InputActionManager (opzionale ma utile) - usa reflection per evitare dipendenze
            try
            {
                var inputActionManagerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager, Unity.XR.Interaction.Toolkit");
                if (inputActionManagerType != null)
                {
                    var inputActionManager = xrOriginGo.AddComponent(inputActionManagerType);
                    var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                        "Assets/InputSystem_Actions.inputactions");
                    if (inputActions != null)
                    {
                        var actionAssetsProp = inputActionManagerType.GetProperty("actionAssets");
                        if (actionAssetsProp != null)
                        {
                            var list = new System.Collections.Generic.List<UnityEngine.InputSystem.InputActionAsset> { inputActions };
                            actionAssetsProp.SetValue(inputActionManager, list);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"InputActionManager non disponibile: {ex.Message}");
            }

            // Assicurati che XR Interaction Manager esista nella scena - usa reflection
            try
            {
                var xrManagerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager, Unity.XR.Interaction.Toolkit");
                if (xrManagerType != null)
                {
                    var xrManager = UnityEngine.Object.FindFirstObjectByType(xrManagerType);
                    if (xrManager == null)
                    {
                        var managerGo = new GameObject("XR Interaction Manager");
                        managerGo.AddComponent(xrManagerType);
                        Debug.Log("XR Interaction Manager aggiunto alla scena.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"XR Interaction Manager non disponibile: {ex.Message}");
            }

            // Collega GameplayDirector se esiste
            var director = UnityEngine.Object.FindFirstObjectByType<GameplayDirector>();
            if (director != null)
            {
                director.PlayerForward = cameraGo.transform;
                Debug.Log("GameplayDirector.PlayerForward collegato alla camera XR Origin.");
            }

            // Collega SpawnController se esiste
            var spawn = UnityEngine.Object.FindFirstObjectByType<SpawnController>();
            if (spawn != null)
            {
                spawn.PlayerForward = cameraGo.transform;
                Debug.Log("SpawnController.PlayerForward collegato alla camera XR Origin.");
            }

            // Disabilita DesktopRig se esiste (non serve in VR)
            var desktopRig = UnityEngine.Object.FindFirstObjectByType<DesktopRig>();
            if (desktopRig != null)
            {
                desktopRig.gameObject.SetActive(false);
                Debug.Log("DesktopRig disabilitato (non necessario in VR).");
            }

            // Aggiungi VRFocusManager per prevenire l'apertura automatica di Oculus Dash
            var focusManager = UnityEngine.Object.FindFirstObjectByType<VRFocusManager>();
            if (focusManager == null)
            {
                var focusManagerGo = new GameObject("VR Focus Manager");
                focusManagerGo.AddComponent<VRFocusManager>();
                Debug.Log("VRFocusManager aggiunto per mantenere il focus in VR.");
            }

            // Aggiungi VRControllerSetup per configurare automaticamente i controller
            var controllerSetup = UnityEngine.Object.FindFirstObjectByType<VRControllerSetup>();
            if (controllerSetup == null)
            {
                var setupGo = new GameObject("VR Controller Setup");
                controllerSetup = setupGo.AddComponent<VRControllerSetup>();
                var saberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VR/Saber.prefab");
                if (saberPrefab != null)
                    controllerSetup.SaberPrefab = saberPrefab;
                Debug.Log("VRControllerSetup aggiunto per configurare automaticamente i controller XR.");
            }

            // Aggiungi VRMenu per menu accessibile nel visore
            var vrMenu = UnityEngine.Object.FindFirstObjectByType<VRMenu>();
            if (vrMenu == null)
            {
                var menuGo = new GameObject("VR Menu");
                menuGo.AddComponent<VRMenu>();
                Debug.Log("VRMenu aggiunto per menu accessibile nel visore.");
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Selection.activeGameObject = xrOriginGo;
            EditorGUIUtility.PingObject(xrOriginGo);
            
            Debug.Log("XR Origin (VR) creato e configurato correttamente!\n" +
                     "- Camera configurata per rendering stereoscopico\n" +
                     "- TrackedPoseDriver aggiunto per tracking VR\n" +
                     "- GameplayDirector e SpawnController collegati automaticamente\n" +
                     "- Per aggiungere i Saber ai controller, crea Left Controller e Right Controller come figli di XR Origin e aggiungi i Saber come loro figli.");
        }

        [MenuItem(MenuRoot + "/Add Saber to XR Controllers (dopo XR Origin)", false, 16)]
        static void AddSaberToXRControllers()
        {
            var xrOrigin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogWarning("XR Origin non trovato nella scena. Esegui prima 'Add XR Origin (VR) to current scene'.");
                return;
            }

            var saberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VR/Saber.prefab");
            if (saberPrefab == null)
            {
                Debug.LogWarning("Prefab Saber non trovato in Assets/VR/Saber.prefab. Esegui prima 'Create Saber Prefab'.");
                return;
            }

            // Cerca o crea Left Controller
            Transform leftController = null;
            Transform rightController = null;
            
            foreach (Transform child in xrOrigin.transform)
            {
                if (child.name.Contains("Left") || child.name.Contains("left"))
                    leftController = child;
                if (child.name.Contains("Right") || child.name.Contains("right"))
                    rightController = child;
            }

            if (leftController == null)
            {
                var leftGo = new GameObject("Left Controller");
                leftGo.transform.SetParent(xrOrigin.transform, false);
                leftController = leftGo.transform;
            }

            if (rightController == null)
            {
                var rightGo = new GameObject("Right Controller");
                rightGo.transform.SetParent(xrOrigin.transform, false);
                rightController = rightGo.transform;
            }

            // Aggiungi Saber ai controller se non esistono già
            if (leftController.GetComponentInChildren<SliceDetector>() == null)
            {
                var leftSaber = PrefabUtility.InstantiatePrefab(saberPrefab) as GameObject;
                leftSaber.name = "Left Saber";
                leftSaber.transform.SetParent(leftController, false);
                leftSaber.transform.localPosition = Vector3.zero;
                leftSaber.transform.localRotation = Quaternion.identity;
                
                var sliceDetector = leftSaber.GetComponent<SliceDetector>();
                if (sliceDetector != null)
                {
                    sliceDetector.SaberLane = 0; // Left lane
                    var director = UnityEngine.Object.FindFirstObjectByType<GameplayDirector>();
                    if (director != null)
                    {
                        sliceDetector.AudioTimeProvider = director; // AudioTimeProvider è MonoBehaviour, non GameObject
                        sliceDetector.AudioTimeMethodName = "GetAudioTime";
                    }
                }
                Debug.Log("Left Saber aggiunto al Left Controller.");
            }

            if (rightController.GetComponentInChildren<SliceDetector>() == null)
            {
                var rightSaber = PrefabUtility.InstantiatePrefab(saberPrefab) as GameObject;
                rightSaber.name = "Right Saber";
                rightSaber.transform.SetParent(rightController, false);
                rightSaber.transform.localPosition = Vector3.zero;
                rightSaber.transform.localRotation = Quaternion.identity;
                
                var sliceDetector = rightSaber.GetComponent<SliceDetector>();
                if (sliceDetector != null)
                {
                    sliceDetector.SaberLane = 1; // Right lane
                    var director = UnityEngine.Object.FindFirstObjectByType<GameplayDirector>();
                    if (director != null)
                    {
                        sliceDetector.AudioTimeProvider = director; // AudioTimeProvider è MonoBehaviour, non GameObject
                        sliceDetector.AudioTimeMethodName = "GetAudioTime";
                    }
                }
                Debug.Log("Right Saber aggiunto al Right Controller.");
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Saber aggiunti ai controller XR. Nota: i controller verranno posizionati automaticamente da XR quando il visore è collegato.");
        }

        [MenuItem(MenuRoot + "/Add Scenes to Build Settings", false, 30)]
        static void AddScenesToBuildSettings()
        {
            var scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Game.unity" };
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var path in scenes)
            {
                if (System.IO.File.Exists(path))
                    list.Add(new EditorBuildSettingsScene(path, true));
            }
            if (list.Count > 0)
            {
                var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                foreach (var s in existing)
                    if (list.FindIndex(x => x.path == s.path) < 0)
                        list.Add(s);
                EditorBuildSettings.scenes = list.ToArray();
                Debug.Log("Build settings: Menu (indice 0) e Game aggiunte. Il gioco parte dal menu.");
            }
        }

        static void CreateGameplayCanvas()
        {
            var canvasGo = new GameObject("GameplayCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var scoreGo = new GameObject("ScoreText");
            scoreGo.transform.SetParent(canvasGo.transform, false);
            var scoreRect = scoreGo.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 1f);
            scoreRect.anchorMax = new Vector2(0.5f, 1f);
            scoreRect.pivot = new Vector2(0.5f, 1f);
            scoreRect.anchoredPosition = new Vector2(0, -20);
            scoreRect.sizeDelta = new Vector2(300, 40);
            var scoreText = scoreGo.AddComponent<Text>();
            scoreText.text = "Score: 0";
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 24;

            var comboGo = new GameObject("ComboText");
            comboGo.transform.SetParent(canvasGo.transform, false);
            var comboRect = comboGo.AddComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.5f, 1f);
            comboRect.anchorMax = new Vector2(0.5f, 1f);
            comboRect.pivot = new Vector2(0.5f, 1f);
            comboRect.anchoredPosition = new Vector2(0, -60);
            comboRect.sizeDelta = new Vector2(200, 30);
            var comboText = comboGo.AddComponent<Text>();
            comboText.text = "Combo x1";
            comboText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            comboText.fontSize = 20;
            comboGo.SetActive(false);

            var lifeBgGo = new GameObject("LifeBarBg");
            lifeBgGo.transform.SetParent(canvasGo.transform, false);
            var lifeBgRect = lifeBgGo.AddComponent<RectTransform>();
            lifeBgRect.anchorMin = new Vector2(0, 1f);
            lifeBgRect.anchorMax = new Vector2(0, 1f);
            lifeBgRect.pivot = new Vector2(0, 1f);
            lifeBgRect.anchoredPosition = new Vector2(20, -70);
            lifeBgRect.sizeDelta = new Vector2(200, 20);
            var lifeBgImage = lifeBgGo.AddComponent<Image>();
            lifeBgImage.color = new Color(0.35f, 0.08f, 0.08f, 0.9f);

            var lifeFillGo = new GameObject("LifeBarFill");
            lifeFillGo.transform.SetParent(lifeBgGo.transform, false);
            var lifeFillRect = lifeFillGo.AddComponent<RectTransform>();
            lifeFillRect.anchorMin = Vector2.zero;
            lifeFillRect.anchorMax = Vector2.one;
            lifeFillRect.offsetMin = lifeFillRect.offsetMax = Vector2.zero;
            var lifeFillImage = lifeFillGo.AddComponent<Image>();
            lifeFillImage.color = new Color(0.2f, 0.85f, 0.25f, 1f);
            lifeFillImage.type = Image.Type.Filled;
            lifeFillImage.fillMethod = Image.FillMethod.Horizontal;
            lifeFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            lifeFillImage.fillAmount = 1f;

            var panelGo = new GameObject("PanelGameOver");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);
            panelGo.SetActive(false);

            var goTextGo = new GameObject("GameOverLabel");
            goTextGo.transform.SetParent(panelGo.transform, false);
            var goTextRect = goTextGo.AddComponent<RectTransform>();
            goTextRect.anchorMin = new Vector2(0.5f, 0.6f);
            goTextRect.anchorMax = new Vector2(0.5f, 0.6f);
            goTextRect.sizeDelta = new Vector2(400, 50);
            var goLabel = goTextGo.AddComponent<Text>();
            goLabel.text = "Game Over";
            goLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            goLabel.fontSize = 36;
            goLabel.alignment = TextAnchor.MiddleCenter;

            var goScoreGo = new GameObject("GameOverScoreText");
            goScoreGo.transform.SetParent(panelGo.transform, false);
            var goScoreRect = goScoreGo.AddComponent<RectTransform>();
            goScoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            goScoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            goScoreRect.sizeDelta = new Vector2(400, 40);
            var goScoreText = goScoreGo.AddComponent<Text>();
            goScoreText.text = "Score: 0";
            goScoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            goScoreText.fontSize = 28;
            goScoreText.alignment = TextAnchor.MiddleCenter;

            var goBestGo = new GameObject("GameOverBestText");
            goBestGo.transform.SetParent(panelGo.transform, false);
            var goBestRect = goBestGo.AddComponent<RectTransform>();
            goBestRect.anchorMin = new Vector2(0.5f, 0.42f);
            goBestRect.anchorMax = new Vector2(0.5f, 0.42f);
            goBestRect.sizeDelta = new Vector2(400, 30);
            var goBestText = goBestGo.AddComponent<Text>();
            goBestText.text = "";
            goBestText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            goBestText.fontSize = 22;
            goBestText.alignment = TextAnchor.MiddleCenter;

            var retryBtnGo = new GameObject("RetryButton");
            retryBtnGo.transform.SetParent(panelGo.transform, false);
            var retryRect = retryBtnGo.AddComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(0.5f, 0.35f);
            retryRect.anchorMax = new Vector2(0.5f, 0.35f);
            retryRect.sizeDelta = new Vector2(160, 40);
            var retryImage = retryBtnGo.AddComponent<Image>();
            retryImage.color = new Color(0.2f, 0.6f, 0.2f);
            var retryBtn = retryBtnGo.AddComponent<Button>();
            var retryLabelGo = new GameObject("Text");
            retryLabelGo.transform.SetParent(retryBtnGo.transform, false);
            var retryLabelRect = retryLabelGo.AddComponent<RectTransform>();
            retryLabelRect.anchorMin = Vector2.zero;
            retryLabelRect.anchorMax = Vector2.one;
            retryLabelRect.offsetMin = retryLabelRect.offsetMax = Vector2.zero;
            var retryLabel = retryLabelGo.AddComponent<Text>();
            retryLabel.text = "Riprova";
            retryLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            retryLabel.fontSize = 22;
            retryLabel.alignment = TextAnchor.MiddleCenter;

            var backBtnGo = new GameObject("BackToMenuButton");
            backBtnGo.transform.SetParent(panelGo.transform, false);
            var backRect = backBtnGo.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0.25f);
            backRect.anchorMax = new Vector2(0.5f, 0.25f);
            backRect.sizeDelta = new Vector2(180, 40);
            var backImage = backBtnGo.AddComponent<Image>();
            backImage.color = new Color(0.3f, 0.3f, 0.6f);
            var backBtn = backBtnGo.AddComponent<Button>();
            var backLabelGo = new GameObject("Text");
            backLabelGo.transform.SetParent(backBtnGo.transform, false);
            var backLabelRect = backLabelGo.AddComponent<RectTransform>();
            backLabelRect.anchorMin = Vector2.zero;
            backLabelRect.anchorMax = Vector2.one;
            backLabelRect.offsetMin = backLabelRect.offsetMax = Vector2.zero;
            var backLabel = backLabelGo.AddComponent<Text>();
            backLabel.text = "Torna al menu";
            backLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            backLabel.fontSize = 20;
            backLabel.alignment = TextAnchor.MiddleCenter;

            var gameplayUIGo = new GameObject("GameplayUI");
            gameplayUIGo.transform.SetParent(canvasGo.transform, false);
            var gameplayUI = gameplayUIGo.AddComponent<GameplayUI>();
            gameplayUI.ScoreText = scoreText;
            gameplayUI.ComboText = comboText;
            gameplayUI.LifeBarFill = lifeFillImage;
            gameplayUI.PanelGameOver = panelGo;
            gameplayUI.GameOverLabelText = goLabel;
            gameplayUI.GameOverScoreText = goScoreText;
            gameplayUI.GameOverBestText = goBestText;
            gameplayUI.RetryButton = retryBtn;
            gameplayUI.BackToMenuButton = backBtn;
            gameplayUI.GameSceneName = "Game";
            gameplayUI.MenuSceneName = "Menu";
        }

        static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                if (existing.GetComponent<StandaloneInputModule>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.GetComponent<StandaloneInputModule>());
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                    Debug.Log("EventSystem: sostituito StandaloneInputModule con InputSystemUIInputModule (nuovo Input System).");
                }
                return;
            }
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        static void EnsureDirectory(string assetPath)
        {
            string dir = System.IO.Path.GetDirectoryName(assetPath);
            if (!AssetDatabase.IsValidFolder("Assets"))
                return;
            string[] parts = dir.Replace("Assets/", "").Replace("Assets\\", "").Split('/', '\\');
            string current = "Assets";
            foreach (var p in parts)
            {
                if (string.IsNullOrEmpty(p)) continue;
                string next = current + "/" + p;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, p);
                current = next;
            }
        }
    }
}
#endif
