using System.Linq;
using AGL3D.Bridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AGL3D.LotB.Editor
{
    /// <summary>
    /// Crée la scène principale <c>Main.unity</c> et la place en tête des scènes du build.
    /// Ne remplace jamais une scène existante : on la modifie ensuite dans l'Éditeur.
    /// Menu : AGL3D > Lot B > Créer la scène principale.
    /// Batchmode : -executeMethod AGL3D.LotB.Editor.MainSceneBuilder.Build
    /// </summary>
    public static class MainSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        // Cubes de test du lot A, en attendant le prefab Appartement (A2).
        private const string TestCubesPrefabPath = "Assets/Lot-A/TestCubes/TestCubes.prefab";

        [MenuItem("AGL3D/Lot B/Créer la scène principale")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.LogWarning($"[MainSceneBuilder] {ScenePath} existe déjà, rien n'est remplacé.");
                AddToBuildSettings();
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Caméra provisoire, remplacée par le prefab CameraRig du lot A (A3).
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0f, 9.5f, -10f), Quaternion.Euler(45f, 0f, 0f));
            var cam = camera.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.93f, 0.91f);

            var light = new GameObject("Directional Light", typeof(Light));
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = light.GetComponent<Light>();
            l.type = LightType.Directional;
            l.shadows = LightShadows.Soft;
            l.intensity = 1.2f;

            // Sol sans InspectableElement : un clic dessus compte comme un clic dans le vide.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Sol";
            ground.transform.localScale = new Vector3(2.2f, 1f, 1f);

            new GameObject(WebBridge.GameObjectName, typeof(WebBridge));

            var testCubes = AssetDatabase.LoadAssetAtPath<GameObject>(TestCubesPrefabPath);
            if (testCubes != null) PrefabUtility.InstantiatePrefab(testCubes, scene);
            else Debug.LogWarning($"[MainSceneBuilder] Prefab introuvable : {TestCubesPrefabPath}");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[MainSceneBuilder] {ScenePath} créée.");
        }

        /// <summary>Met Main.unity en première position : c'est la scène chargée par le build WebGL.</summary>
        private static void AddToBuildSettings()
        {
            var others = EditorBuildSettings.scenes.Where(s => s.path != ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(others)
                .ToArray();
        }
    }
}
