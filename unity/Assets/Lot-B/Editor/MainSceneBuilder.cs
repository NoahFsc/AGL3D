using System.Linq;
using AGL3D.Bridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AGL3D.LotB.Editor
{
    /// <summary>
    /// Crée la scène principale <c>Main.unity</c> et la place en tête des scènes du build.
    /// Ne remplace jamais une scène existante : sur une scène déjà créée, ajoute seulement
    /// les prefabs qui manquent (Appartement, CameraRig, Interaction) et retire les objets
    /// provisoires (cubes de test, caméra et sol).
    /// Menu : AGL3D > Lot B > Créer ou compléter la scène principale.
    /// Batchmode : -executeMethod AGL3D.LotB.Editor.MainSceneBuilder.Build
    /// </summary>
    public static class MainSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        private const string AppartementPrefabPath = "Assets/Lot-A/Appartement/Appartement.prefab";
        private const string CameraRigPrefabPath = "Assets/Lot-A/CameraRig/CameraRig.prefab";

        /// <summary>Prefab du lot B qui porte le survol (B2), puis le clic (B3).</summary>
        private const string InteractionPrefabPath = "Assets/Lot-B/Interaction.prefab";

        // Objets provisoires des premières versions de Main, remplacés par les prefabs du lot A.
        private const string TestCubesPrefabPath = "Assets/Lot-A/TestCubes/TestCubes.prefab";
        private static readonly string[] ProvisionalNames = { "Main Camera", "Sol" };

        [MenuItem("AGL3D/Lot B/Créer ou compléter la scène principale")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) CreateScene();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool changed = RemoveProvisional(scene);
            changed |= EnsurePrefab(scene, CameraRigPrefabPath);
            changed |= EnsurePrefab(scene, AppartementPrefabPath);
            GetOrCreateInteractionPrefab();
            changed |= EnsurePrefab(scene, InteractionPrefabPath);
            if (changed) EditorSceneManager.SaveScene(scene);

            AddToBuildSettings();
            AssetDatabase.SaveAssets();
        }

        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Directional Light", typeof(Light));
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = light.GetComponent<Light>();
            l.type = LightType.Directional;
            l.shadows = LightShadows.Soft;
            l.intensity = 1.2f;

            new GameObject(WebBridge.GameObjectName, typeof(WebBridge));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[MainSceneBuilder] {ScenePath} créée.");
        }

        /// <summary>Ajoute une instance du prefab à la racine de la scène si elle n'y est pas.</summary>
        private static bool EnsurePrefab(Scene scene, string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[MainSceneBuilder] Prefab introuvable : {path}");
                return false;
            }
            if (scene.GetRootGameObjects().Any(go => IsInstanceOf(go, prefab))) return false;

            PrefabUtility.InstantiatePrefab(prefab, scene);
            Debug.Log($"[MainSceneBuilder] {prefab.name} ajouté à {ScenePath}.");
            return true;
        }

        /// <summary>Retire les cubes de test et les objets provisoires posés à la racine.</summary>
        private static bool RemoveProvisional(Scene scene)
        {
            var testCubes = AssetDatabase.LoadAssetAtPath<GameObject>(TestCubesPrefabPath);
            var provisional = scene.GetRootGameObjects()
                .Where(go => (testCubes != null && IsInstanceOf(go, testCubes))
                             || (ProvisionalNames.Contains(go.name) && !PrefabUtility.IsPartOfPrefabInstance(go)))
                .ToArray();
            foreach (var go in provisional)
            {
                Debug.Log($"[MainSceneBuilder] Objet provisoire retiré : {go.name}");
                Object.DestroyImmediate(go);
            }
            return provisional.Length > 0;
        }

        private static bool IsInstanceOf(GameObject go, GameObject prefab)
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(go) == prefab;
        }

        private static void GetOrCreateInteractionPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(InteractionPrefabPath) != null) return;

            var root = new GameObject("Interaction", typeof(HoverHighlighter));
            PrefabUtility.SaveAsPrefabAsset(root, InteractionPrefabPath);
            Object.DestroyImmediate(root);
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
