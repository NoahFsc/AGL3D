using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AGL3D.LotC.EditorTools
{
    /// <summary>
    /// Crée ou met à jour les repères du lot C : les trois matériaux d'état, le prefab
    /// <c>PointMarker</c> et l'objet <c>Reperes</c> de la scène principale. Les couleurs
    /// reprennent celles de la fiche côté Vue (bon-300, usage-300, mauvais-300).
    /// Se relance sans risque : ce qui existe déjà est mis à jour, pas dupliqué.
    /// </summary>
    public static class ReperesBuilder
    {
        private const string Dossier = "Assets/Lot-C/Reperes";
        private const string CheminPrefab = Dossier + "/PointMarker.prefab";
        private const string CheminScene = "Assets/Scenes/Main.unity";
        private const string NomObjet = "Reperes";

        [MenuItem("AGL3D/Lot C/Créer ou mettre à jour les repères")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Lot-C", "Reperes");

            var bon = Materiau("Repere_Bon", "#16A34A");
            var usage = Materiau("Repere_Usage", "#EA580C");
            var mauvais = Materiau("Repere_Mauvais", "#DC2626");
            CreerPrefab(bon, usage, mauvais);
            AjouterDansMain();

            AssetDatabase.SaveAssets();
            Debug.Log("[ReperesBuilder] Repères prêts.");
        }

        /// <summary>Matériau URP mat et peu brillant, pour l'aspect maquette.</summary>
        private static Material Materiau(string nom, string couleurHex)
        {
            string chemin = $"{Dossier}/{nom}.mat";
            var materiau = AssetDatabase.LoadAssetAtPath<Material>(chemin);
            if (materiau == null)
            {
                materiau = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(materiau, chemin);
            }
            ColorUtility.TryParseHtmlString(couleurHex, out var couleur);
            materiau.SetColor("_BaseColor", couleur);
            materiau.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(materiau);
            return materiau;
        }

        /// <summary>Prefab : une sphère de 1 m (mise à l'échelle par PointMarkers) avec son collider.</summary>
        private static void CreerPrefab(Material bon, Material usage, Material mauvais)
        {
            var racine = new GameObject("PointMarker");
            var pastille = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pastille.name = "Pastille";
            pastille.transform.SetParent(racine.transform, false);
            var rendu = pastille.GetComponent<MeshRenderer>();
            rendu.sharedMaterial = bon;
            rendu.shadowCastingMode = ShadowCastingMode.Off;

            var repere = racine.AddComponent<PointMarker>();
            var champs = new SerializedObject(repere);
            champs.FindProperty("pastille").objectReferenceValue = rendu;
            champs.FindProperty("bon").objectReferenceValue = bon;
            champs.FindProperty("usage").objectReferenceValue = usage;
            champs.FindProperty("mauvais").objectReferenceValue = mauvais;
            champs.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(racine, CheminPrefab);
            Object.DestroyImmediate(racine);
        }

        /// <summary>Ajoute l'objet Reperes à la racine de Main s'il n'y est pas, et le relie au prefab.</summary>
        private static void AjouterDansMain()
        {
            var scene = EditorSceneManager.OpenScene(CheminScene, OpenSceneMode.Single);
            // Rechargé après l'ouverture de la scène, qui décharge les objets gardés en mémoire.
            var prefab = AssetDatabase.LoadAssetAtPath<PointMarker>(CheminPrefab);
            var objet = scene.GetRootGameObjects().FirstOrDefault(go => go.name == NomObjet)
                        ?? new GameObject(NomObjet);
            // Pas de « ?? » : dans l'Éditeur, un composant absent n'est pas un vrai null C#.
            var gestionnaire = objet.GetComponent<PointMarkers>();
            if (gestionnaire == null) gestionnaire = objet.AddComponent<PointMarkers>();
            var champs = new SerializedObject(gestionnaire);
            champs.FindProperty("modele").objectReferenceValue = prefab;
            champs.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
