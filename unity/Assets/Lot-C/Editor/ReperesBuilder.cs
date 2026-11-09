using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AGL3D.LotC.EditorTools
{
    /// <summary>
    /// Crée ou met à jour les étiquettes du lot C : les sprites (cadre arrondi, goutte, éclair),
    /// le matériau qui les affiche par-dessus les murs, le prefab <c>PointMarker</c> et l'objet
    /// <c>Reperes</c> de la scène principale. Se relance sans risque : ce qui existe déjà est
    /// mis à jour, pas dupliqué.
    /// </summary>
    public static class ReperesBuilder
    {
        private const string Dossier = "Assets/Lot-C/Reperes";
        private const string CheminPrefab = Dossier + "/PointMarker.prefab";
        private const string CheminScene = "Assets/Scenes/Main.unity";
        private const string NomObjet = "Reperes";

        private const float HauteurTige = 22f;
        private const int TaillePolice = 18;

        [MenuItem("AGL3D/Lot C/Créer ou mettre à jour les repères")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Lot-C", "Reperes");

            var cadre = CreerSprite("Cadre", 48, CouvertureCadre, 16);
            var goutte = CreerSprite("Goutte", 64, CouvertureGoutte, 0);
            var eclair = CreerSprite("Eclair", 64, CouvertureEclair, 0);
            var materiau = MateriauParDessus();
            CreerPrefab(cadre, goutte, eclair, materiau);
            AjouterDansMain();

            AssetDatabase.SaveAssets();
            Debug.Log("[ReperesBuilder] Repères prêts.");
        }

        /* ------------------------------- Sprites ------------------------------- */

        /// <summary>Dessine une forme blanche anticrénelée (4×4 échantillons par pixel) et l'importe en sprite.</summary>
        private static Sprite CreerSprite(string nom, int taille, Func<float, float, bool> dedans, int bord)
        {
            string chemin = $"{Dossier}/{nom}.png";
            var texture = new Texture2D(taille, taille, TextureFormat.RGBA32, false);
            for (int y = 0; y < taille; y++)
            {
                for (int x = 0; x < taille; x++)
                {
                    int couverts = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                            if (dedans((x + (sx + 0.5f) / 4f) / taille, 1f - (y + (sy + 0.5f) / 4f) / taille)) couverts++;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, couverts / 16f));
                }
            }
            File.WriteAllBytes(chemin, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(chemin);

            var import = (TextureImporter)AssetImporter.GetAtPath(chemin);
            import.textureType = TextureImporterType.Sprite;
            import.spriteImportMode = SpriteImportMode.Single;
            import.spriteBorder = new Vector4(bord, bord, bord, bord);
            import.mipmapEnabled = false;
            import.alphaIsTransparency = true;
            import.wrapMode = TextureWrapMode.Clamp;
            import.filterMode = FilterMode.Bilinear;
            import.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(chemin);
        }

        /// <summary>Carré aux coins arrondis (rayon 1/3), étiré en 9 tranches.</summary>
        private static bool CouvertureCadre(float u, float v)
        {
            const float r = 1f / 3f;
            float dx = Mathf.Max(Mathf.Abs(u - 0.5f) - (0.5f - r), 0f);
            float dy = Mathf.Max(Mathf.Abs(v - 0.5f) - (0.5f - r), 0f);
            return dx * dx + dy * dy <= r * r;
        }

        /// <summary>Goutte d'eau : un disque en bas et une pointe vers le haut (v vers le bas).</summary>
        private static bool CouvertureGoutte(float u, float v)
        {
            float cx = 0.5f, cy = 0.62f, r = 0.28f;
            if ((u - cx) * (u - cx) + (v - cy) * (v - cy) <= r * r) return true;
            return DansPolygone(u, v, new[] { new Vector2(0.5f, 0.06f), new Vector2(0.26f, 0.48f), new Vector2(0.74f, 0.48f) });
        }

        /// <summary>Éclair, d'après l'icône « zap » de Lucide (grille 24 × 24, v vers le bas).</summary>
        private static bool CouvertureEclair(float u, float v)
        {
            var points = new[] { (13f, 2f), (3f, 14f), (12f, 14f), (11f, 22f), (21f, 10f), (12f, 10f) }
                .Select(p => new Vector2(p.Item1 / 24f, p.Item2 / 24f)).ToArray();
            return DansPolygone(u, v, points);
        }

        private static bool DansPolygone(float x, float y, Vector2[] p)
        {
            bool dedans = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                if ((p[i].y > y) != (p[j].y > y)
                    && x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                    dedans = !dedans;
            }
            return dedans;
        }

        /* ------------------------------ Matériau ------------------------------- */

        /// <summary>Matériau du shader EtiquetteParDessus : les étiquettes restent visibles derrière les murs.</summary>
        private static Material MateriauParDessus()
        {
            string chemin = $"{Dossier}/Etiquette.mat";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>($"{Dossier}/EtiquetteParDessus.shader");
            var materiau = AssetDatabase.LoadAssetAtPath<Material>(chemin);
            if (materiau == null)
            {
                materiau = new Material(shader);
                AssetDatabase.CreateAsset(materiau, chemin);
            }
            materiau.shader = shader;
            EditorUtility.SetDirty(materiau);
            return materiau;
        }

        /* ------------------------------- Prefab -------------------------------- */

        private static void CreerPrefab(Sprite cadreSprite, Sprite goutte, Sprite eclair, Material materiau)
        {
            var racine = new GameObject("PointMarker");

            var etiquette = new GameObject("Etiquette", typeof(RectTransform), typeof(Canvas), typeof(BoxCollider));
            etiquette.transform.SetParent(racine.transform, false);
            var canvas = etiquette.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            var rectEtiquette = (RectTransform)etiquette.transform;
            rectEtiquette.sizeDelta = Vector2.zero;

            var tige = CreerImage("Tige", etiquette.transform, null, materiau);
            var rectTige = (RectTransform)tige.transform;
            rectTige.pivot = new Vector2(0.5f, 0f);
            rectTige.anchoredPosition = Vector2.zero;
            rectTige.sizeDelta = new Vector2(2f, HauteurTige);

            var bordure = CreerImage("Cadre", etiquette.transform, cadreSprite, materiau);
            bordure.type = UnityEngine.UI.Image.Type.Sliced;
            bordure.pixelsPerUnitMultiplier = 1.4f;
            var rectCadre = (RectTransform)bordure.transform;
            rectCadre.pivot = new Vector2(0.5f, 0f);
            rectCadre.anchoredPosition = new Vector2(0f, HauteurTige);
            var groupeBordure = bordure.gameObject.AddComponent<HorizontalLayoutGroup>();
            groupeBordure.padding = new RectOffset(2, 2, 2, 2);
            Controler(groupeBordure);
            var ajuste = bordure.gameObject.AddComponent<ContentSizeFitter>();
            ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var fond = CreerImage("Fond", bordure.transform, cadreSprite, materiau);
            fond.type = UnityEngine.UI.Image.Type.Sliced;
            fond.pixelsPerUnitMultiplier = 1.6f;
            var groupeFond = fond.gameObject.AddComponent<HorizontalLayoutGroup>();
            groupeFond.padding = new RectOffset(14, 14, 7, 7);
            groupeFond.spacing = 6f;
            groupeFond.childAlignment = TextAnchor.MiddleCenter;
            Controler(groupeFond);

            var icone = CreerImage("Icone", fond.transform, goutte, materiau);
            icone.preserveAspect = true;
            var tailleIcone = icone.gameObject.AddComponent<LayoutElement>();
            tailleIcone.preferredWidth = TaillePolice;
            tailleIcone.preferredHeight = TaillePolice;

            var texte = new GameObject("Texte", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            texte.transform.SetParent(fond.transform, false);
            texte.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texte.fontSize = TaillePolice;
            texte.fontStyle = FontStyle.Bold;
            texte.alignment = TextAnchor.MiddleLeft;
            texte.horizontalOverflow = HorizontalWrapMode.Overflow;
            texte.verticalOverflow = VerticalWrapMode.Overflow;
            texte.raycastTarget = false;
            texte.material = materiau;
            texte.text = "1 · Élément";

            var repere = racine.AddComponent<PointMarker>();
            var champs = new SerializedObject(repere);
            champs.FindProperty("etiquette").objectReferenceValue = rectEtiquette;
            champs.FindProperty("cadre").objectReferenceValue = rectCadre;
            champs.FindProperty("bordure").objectReferenceValue = bordure;
            champs.FindProperty("fond").objectReferenceValue = fond;
            champs.FindProperty("tige").objectReferenceValue = tige;
            champs.FindProperty("icone").objectReferenceValue = icone;
            champs.FindProperty("texte").objectReferenceValue = texte;
            champs.FindProperty("zoneClic").objectReferenceValue = etiquette.GetComponent<BoxCollider>();
            champs.FindProperty("goutte").objectReferenceValue = goutte;
            champs.FindProperty("eclair").objectReferenceValue = eclair;
            champs.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(racine, CheminPrefab);
            UnityEngine.Object.DestroyImmediate(racine);
        }

        private static Image CreerImage(string nom, Transform parent, Sprite sprite, Material materiau)
        {
            var image = new GameObject(nom, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.material = materiau;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Le groupe dimensionne ses enfants à leur taille préférée, sans les étirer.</summary>
        private static void Controler(HorizontalLayoutGroup groupe)
        {
            groupe.childControlWidth = true;
            groupe.childControlHeight = true;
            groupe.childForceExpandWidth = false;
            groupe.childForceExpandHeight = false;
        }

        /* -------------------------------- Scène -------------------------------- */

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
