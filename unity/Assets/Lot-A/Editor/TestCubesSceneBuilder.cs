using System;
using System.IO;
using System.Linq;
using AGL3D.Bridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Génère la scène de test des lots B et C : un cube par élément du jeu de démo,
    /// avec un <see cref="InspectableElement"/> qui porte la vraie clé du JSON.
    /// Les clés sont lues dans web/public/data/appart-demo/appart-demo.json, jamais recopiées.
    /// Menu : AGL3D > Lot A > Générer la scène de test des cubes.
    /// Batchmode : -executeMethod AGL3D.LotA.Editor.TestCubesSceneBuilder.Build
    /// </summary>
    public static class TestCubesSceneBuilder
    {
        private const string Root = "Assets/Lot-A/TestCubes";
        private const string ScenePath = Root + "/Lot-A_TestCubes.unity";
        private const string PrefabPath = Root + "/TestCubes.prefab";
        private const string MaterialsDir = Root + "/Materials";
        private const string DataPath = "../web/public/data/appart-demo/appart-demo.json";

        private const float CubeSize = 0.8f;
        private const float Spacing = 2.2f;
        private const float RoomGap = 2f;
        private const int Columns = 3;

        // Couleurs neutres par pièce, distinctes des couleurs d'état (vert, orange, rouge).
        private static readonly Color[] RoomColors =
        {
            new Color(0.62f, 0.70f, 0.82f),
            new Color(0.78f, 0.70f, 0.84f),
            new Color(0.85f, 0.80f, 0.66f),
        };

        [Serializable] private class Dossier { public Piece[] pieces; public Element[] elements; }
        [Serializable] private class Piece { public string key; public string label; }
        [Serializable] private class Element { public string key; public string pieceKey; public string label; }

        [MenuItem("AGL3D/Lot A/Générer la scène de test des cubes")]
        public static void Build()
        {
            var dossier = LoadDossier();
            Directory.CreateDirectory(MaterialsDir);

            var root = new GameObject("TestCubes");
            float x = 0f;
            for (int p = 0; p < dossier.pieces.Length; p++)
            {
                var piece = dossier.pieces[p];
                var elements = dossier.elements.Where(e => e.pieceKey == piece.key).ToArray();
                var material = CreateRoomMaterial(piece.key, RoomColors[p % RoomColors.Length]);

                var room = new GameObject(piece.key);
                room.transform.SetParent(root.transform, false);
                int cols = Mathf.Min(Columns, Mathf.Max(1, elements.Length));
                room.transform.localPosition = new Vector3(x, 0f, 0f);

                for (int i = 0; i < elements.Length; i++)
                {
                    var cube = CreateCube(elements[i], material);
                    cube.transform.SetParent(room.transform, false);
                    cube.transform.localPosition = new Vector3((i % cols) * Spacing, CubeSize / 2f, -(i / cols) * Spacing);
                }

                x += (cols - 1) * Spacing + RoomGap + Spacing;
            }

            // Centre l'ensemble sur l'origine.
            float width = x - RoomGap - Spacing;
            foreach (Transform room in root.transform)
                room.localPosition -= new Vector3(width / 2f, 0f, -Spacing / 2f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            BuildScene(prefab);
            Debug.Log($"[TestCubesSceneBuilder] {dossier.elements.Length} cubes générés dans {ScenePath}");
        }

        private static Dossier LoadDossier()
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DataPath));
            if (!File.Exists(path)) throw new FileNotFoundException($"Jeu de données introuvable : {path}");
            var dossier = JsonUtility.FromJson<Dossier>(File.ReadAllText(path));
            if (dossier?.pieces == null || dossier.elements == null || dossier.elements.Length == 0)
                throw new InvalidDataException($"Aucun élément lu dans {path}");
            return dossier;
        }

        private static Material CreateRoomMaterial(string roomKey, Color color)
        {
            var path = $"{MaterialsDir}/TestCube_{roomKey}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = GraphicsSettings.currentRenderPipeline != null
                    ? GraphicsSettings.currentRenderPipeline.defaultMaterial.shader
                    : Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateCube(Element element, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = element.key;
            cube.transform.localScale = Vector3.one * CubeSize;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;

            var inspectable = cube.AddComponent<InspectableElement>();
            var serialized = new SerializedObject(inspectable);
            serialized.FindProperty("key").stringValue = element.key;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Étiquette lisible au-dessus du cube, sans collider pour ne pas gêner le picking.
            var label = new GameObject("Label");
            label.transform.SetParent(cube.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            label.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            label.transform.localScale = Vector3.one / CubeSize;
            var text = label.AddComponent<TextMesh>();
            text.text = $"{element.label}\n<size=20>{element.key}</size>";
            text.richText = true;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.045f;
            text.fontSize = 40;
            text.color = new Color(0.15f, 0.15f, 0.18f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return cube;
        }

        private static void BuildScene(GameObject prefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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

            PrefabUtility.InstantiatePrefab(prefab, scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
    }
}
