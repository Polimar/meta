#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BeatSaberAlefy.VR;
using BeatSaberAlefy.BeatMap;
using BeatSaberAlefy.UI;
using BeatSaberAlefy.Alefy;

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

            string path = "Assets/Cubes/Cube.prefab";
            EnsureDirectory(path);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
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
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.1f, 1f, 0.1f);
            box.center = new Vector3(0, 0.5f, 0);
            go.AddComponent<SliceDetector>();

            string path = "Assets/VR/Saber.prefab";
            EnsureDirectory(path);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"Created {path}");
        }

        [MenuItem(MenuRoot + "/Create Menu Scene", false, 10)]
        static void CreateMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null) cam.gameObject.name = "Main Camera";

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var menu = panel.AddComponent<MenuController>();
            // UI references: user can wire in Inspector

            string path = "Assets/Scenes/Menu.unity";
            EnsureDirectory(path);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Created {path}");
        }

        [MenuItem(MenuRoot + "/Create Game Scene", false, 11)]
        static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null) cam.gameObject.name = "Main Camera";

            var directorGo = new GameObject("GameplayDirector");
            var audioSource = directorGo.AddComponent<AudioSource>();
            var director = directorGo.AddComponent<GameplayDirector>();
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

            string path = "Assets/Scenes/Game.unity";
            EnsureDirectory(path);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Created {path}. Add XR Origin and attach Saber prefabs to controllers for VR.");
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

        [MenuItem(MenuRoot + "/Add Scenes to Build Settings", false, 30)]
        static void AddScenesToBuildSettings()
        {
            var scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Game.unity" };
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var path in scenes)
            {
                if (System.IO.File.Exists(path) && list.Find(s => s.path == path) == null)
                    list.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("Build settings updated with Menu and Game scenes.");
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
