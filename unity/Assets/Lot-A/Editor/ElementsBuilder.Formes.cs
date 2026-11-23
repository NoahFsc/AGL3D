using System.Collections.Generic;
using UnityEditor;
using EditorMeshUtility = UnityEditor.ProBuilder.EditorMeshUtility;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Formes communes aux générateurs : boîtes et cylindres ProBuilder (et l'enregistrement de
    /// leurs maillages), surfaces de lames ou de carreaux (parquets, carrelage, crédence), modèles
    /// des packs de l'Asset Store, matériaux.
    /// </summary>
    public static partial class ElementsBuilder
    {
        /* ------------------------------ Modèles des packs ------------------------------ */

        private const string PackDir = "Assets/LowPolyFurniturePackFree/Mesh";
        private const string MegaPackDir = "Assets/Furniture Mega Pack/Prefabs";

        /// <summary>Instance d'un modèle du pack Low Poly Furniture (prefab imbriqué), sans collider.</summary>
        private static Transform InstancePack(string nom, Transform parent) =>
            InstancePrefab($"{PackDir}/{nom}.prefab", "Low Poly Furniture Pack Free", parent);

        /// <summary>
        /// Instance d'un modèle du Furniture Mega Pack (ex. « Kitchen/Sink03 »), sans collider. Les
        /// modèles du pack regardent vers -z : par défaut, ils sont tournés de 180° pour suivre la
        /// convention des prefabs (dos en z = 0, façade vers +z). <paramref name="lacet"/> donne une
        /// autre rotation, ou null pour garder celle du modèle.
        /// </summary>
        private static Transform InstanceMega(string modele, Transform parent, float? lacet = 180f)
        {
            var instance = InstancePrefab($"{MegaPackDir}/{modele}.prefab", "Furniture Mega Pack - Free", parent);
            if (lacet.HasValue) instance.localRotation = Quaternion.Euler(0f, lacet.Value, 0f);
            return instance;
        }

        /// <summary>Instance d'un prefab d'un pack de l'Asset Store (prefab imbriqué), sans collider.</summary>
        private static Transform InstancePrefab(string chemin, string pack, Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(chemin);
            if (source == null)
                throw new System.IO.FileNotFoundException($"Modèle « {chemin} » introuvable : importez le pack {pack} (voir le README).");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            foreach (var c in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return instance.transform;
        }

        /* ------------------------------ Boîtes et cylindres ------------------------------ */

        /// <summary>Boîte entre deux coins, dans le repère de <paramref name="parent"/>.</summary>
        private static Transform Boite(string nom, Transform parent, Vector3 min, Vector3 max, Material materiau)
        {
            var pb = ShapeGenerator.GenerateCube(PivotLocation.Center, max - min);
            pb.gameObject.name = nom;
            pb.transform.SetParent(parent, false);
            pb.transform.localPosition = (min + max) / 2f;
            pb.GetComponent<MeshRenderer>().sharedMaterial = materiau;
            var boite = pb.transform;
            Finaliser(pb);
            return boite;
        }

        /// <summary>
        /// Cylindre à faces plates (effet low poly) entre deux points, dans le repère de
        /// <paramref name="parent"/>. <paramref name="cotes"/> est arrondi au nombre pair.
        /// </summary>
        private static void Cylindre(string nom, Transform parent, Vector3 debut, Vector3 fin, float rayon, int cotes, Material materiau)
        {
            var axe = fin - debut;
            var pb = ShapeGenerator.GenerateCylinder(PivotLocation.Center, cotes, rayon, axe.magnitude, 0, -1);
            pb.gameObject.name = nom;
            pb.transform.SetParent(parent, false);
            pb.transform.localPosition = (debut + fin) / 2f;
            pb.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axe.normalized);
            pb.GetComponent<MeshRenderer>().sharedMaterial = materiau;
            Finaliser(pb);
        }

        /// <summary>Maillages des boîtes et cylindres construits, en attente d'enregistrement (EnregistrerMaillages).</summary>
        private static readonly List<MeshFilter> maillagesEnAttente = new List<MeshFilter>();

        /// <summary>
        /// Construit le maillage, retire les colliders (celui de la racine suffit) et vérifie les
        /// normales : une normale invalide (forme trop petite) donne des pixels NaN que les
        /// post-effets du WebGL étalent en halo blanc.
        /// Le composant ProBuilderMesh est ensuite retiré : le maillage devient un maillage Unity
        /// ordinaire, enregistré en .asset avec le prefab. Sinon ProBuilder reconstruit le maillage
        /// à chaque ouverture de la scène et en recopie un exemplaire dans Main.unity.
        /// </summary>
        private static void Finaliser(ProBuilderMesh pb)
        {
            pb.ToMesh();
            pb.Refresh();
            foreach (var c in pb.GetComponents<Collider>()) Object.DestroyImmediate(c);
            var mf = pb.GetComponent<MeshFilter>();
            foreach (var n in mf.sharedMesh.normals)
            {
                if (float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z) || n.sqrMagnitude < 0.5f)
                    throw new System.InvalidOperationException($"Normales invalides sur « {pb.name} » : forme trop petite, agrandissez-la ou utilisez une boîte.");
            }
            // Même optimisation que ProBuilder à l'ouverture d'une scène (sommets confondus fusionnés) :
            // le maillage enregistré est celui qu'on voyait avant.
            EditorMeshUtility.Optimize(pb);
            pb.preserveMeshAssetOnDestroy = true;
            Object.DestroyImmediate(pb);
            maillagesEnAttente.Add(mf);
        }

        /// <summary>
        /// Enregistre dans <paramref name="chemin"/> (.asset) les maillages en attente des objets
        /// sous <paramref name="racine"/>. Chaque maillage porte le chemin de son objet : quand on
        /// relance un générateur, les maillages existants sont mis à jour en place (mêmes
        /// identifiants, le prefab ne change que si la forme change) et ceux qui ne servent plus
        /// sont retirés.
        /// </summary>
        private static void EnregistrerMaillages(string chemin, Transform racine)
        {
            var nouveaux = maillagesEnAttente.FindAll(mf => mf != null && mf.transform.IsChildOf(racine));
            maillagesEnAttente.RemoveAll(mf => mf == null || mf.transform.IsChildOf(racine));
            var conteneur = AssetDatabase.LoadMainAssetAtPath(chemin);
            if (nouveaux.Count == 0 && conteneur == null) return;

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(chemin));
            if (conteneur == null)
            {
                // Objet principal vide : les maillages sont des sous-objets, nommés par leur chemin.
                conteneur = new Mesh { name = System.IO.Path.GetFileNameWithoutExtension(chemin) };
                AssetDatabase.CreateAsset(conteneur, chemin);
            }
            var existants = new Dictionary<string, Mesh>();
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(chemin))
                if (o is Mesh m) existants[m.name] = m;

            var noms = new HashSet<string>();
            foreach (var mf in nouveaux)
            {
                var nom = CheminRelatif(mf.transform, racine);
                for (int i = 2; !noms.Add(nom); i++) nom = $"{CheminRelatif(mf.transform, racine)} ({i})";
                var maillage = mf.sharedMesh;
                maillage.name = nom;
                if (existants.TryGetValue(nom, out var ancien))
                {
                    EditorUtility.CopySerialized(maillage, ancien);
                    ancien.name = nom;
                    Object.DestroyImmediate(maillage);
                    mf.sharedMesh = ancien;
                    existants.Remove(nom);
                }
                else AssetDatabase.AddObjectToAsset(maillage, conteneur);
            }
            foreach (var reste in existants.Values)
            {
                AssetDatabase.RemoveObjectFromAsset(reste);
                Object.DestroyImmediate(reste);
            }
            EditorUtility.SetDirty(conteneur);
        }

        private static string CheminRelatif(Transform objet, Transform racine)
        {
            var chemin = objet.name;
            for (var t = objet.parent; t != null && t != racine; t = t.parent) chemin = t.name + "/" + chemin;
            return chemin;
        }

        /* ------------------------------ Lames et carreaux ------------------------------ */

        /// <summary>
        /// Pose de lames ou de carreaux sur un rectangle, en coordonnées (u, v) : les rangées, de
        /// largeur <see cref="rangee"/>, se suivent en v de v0 à v1 ; dans chaque rangée, les pièces
        /// se suivent en u de u0 à u1, coupées au bord.
        /// </summary>
        private struct Calepinage
        {
            public float u0, u1, v0, v1;
            /// <summary>Largeur d'une rangée.</summary>
            public float rangee;
            /// <summary>Retrait de chaque bord d'une pièce : la moitié du joint.</summary>
            public float joint;
            /// <summary>Longueurs successives des pièces, reprises en boucle.</summary>
            public float[] longueurs;
            /// <summary>Fin de la première pièce de la rangée r, qui commence en u (décale les joints d'une rangée à l'autre).</summary>
            public System.Func<float, int, float> premiere;
            /// <summary>La rangée r reprend les longueurs à partir de la pièce r × decalage.</summary>
            public int decalage;
            /// <summary>Teinte de la pièce i de la rangée r : (r × pasTeinte + i) modulo le nombre de teintes.</summary>
            public int pasTeinte;
        }

        /// <summary>Pièces du calepinage : (u0, u1, v0, v1), joints retirés, et numéro de teinte.</summary>
        private static List<(float u0, float u1, float v0, float v1, int teinte)> Calepiner(Calepinage c, int teintes)
        {
            var pieces = new List<(float, float, float, float, int)>();
            int r = 0;
            for (float v = c.v0; v < c.v1 - 0.01f; v += c.rangee, r++)
            {
                float v1 = Mathf.Min(v + c.rangee, c.v1);
                float u = c.u0;
                int i = r * c.decalage;
                float fin = c.premiere(u, r);
                while (u < c.u1 - 0.01f)
                {
                    fin = Mathf.Min(fin, c.u1);
                    pieces.Add((u + c.joint, fin - c.joint, v + c.joint, v1 - c.joint, (r * c.pasTeinte + i) % teintes));
                    u = fin;
                    i++;
                    fin = u + c.longueurs[i % c.longueurs.Length];
                }
            }
            return pieces;
        }

        /// <summary>
        /// Surface de lames ou de carreaux en un seul objet, enregistrée dans
        /// Lot-A/Elements/dossier/nom.asset, une teinte par matériau. <paramref name="boite"/> place
        /// chaque pièce (u0, u1, v0, v1) en volume : coins min et max dans le repère du parent.
        /// </summary>
        private static void Surface(string nom, Transform parent, string dossier, Calepinage c, Material[] teintes,
            System.Func<float, float, float, float, (Vector3, Vector3)> boite)
        {
            var boites = new List<(Vector3, Vector3, int)>();
            foreach (var (u0, u1, v0, v1, teinte) in Calepiner(c, teintes.Length))
            {
                var (min, max) = boite(u0, u1, v0, v1);
                boites.Add((min, max, teinte));
            }
            var surface = new GameObject(nom);
            surface.transform.SetParent(parent, false);
            surface.AddComponent<MeshFilter>().sharedMesh = Lames(dossier, nom, boites, teintes.Length);
            surface.AddComponent<MeshRenderer>().sharedMaterials = teintes;
        }

        /// <summary>
        /// Maillage unique fait de boîtes (sans face du dessous), une teinte par sous-maillage,
        /// enregistré dans Lot-A/Elements/dossier/nom.asset. Sert aux surfaces faites de
        /// nombreuses pièces, comme le parquet, pour ne pas multiplier les objets.
        /// </summary>
        private static Mesh Lames(string dossier, string nom, List<(Vector3 min, Vector3 max, int teinte)> boites, int teintes)
        {
            var sommets = new List<Vector3>();
            var parTeinte = new List<int>[teintes];
            for (int i = 0; i < teintes; i++) parTeinte[i] = new List<int>();

            void Face(List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normale)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normale) < 0f) { (b, d) = (d, b); }
                int s0 = sommets.Count;
                sommets.Add(a); sommets.Add(b); sommets.Add(c); sommets.Add(d);
                tris.Add(s0); tris.Add(s0 + 1); tris.Add(s0 + 2);
                tris.Add(s0); tris.Add(s0 + 2); tris.Add(s0 + 3);
            }

            foreach (var (min, max, teinte) in boites)
            {
                var tris = parTeinte[teinte];
                Face(tris, new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(max.x, max.y, min.z), Vector3.up);
                Face(tris, new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z), Vector3.back);
                Face(tris, new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(max.x, min.y, max.z), Vector3.forward);
                Face(tris, new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z), new Vector3(min.x, min.y, max.z), Vector3.left);
                Face(tris, new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(max.x, max.y, min.z), Vector3.right);
            }

            System.IO.Directory.CreateDirectory($"{Dir}/{dossier}");
            var path = $"{Dir}/{dossier}/{nom}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = nom };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.indexFormat = sommets.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(sommets);
            mesh.subMeshCount = teintes;
            for (int i = 0; i < teintes; i++) mesh.SetTriangles(parTeinte[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        /* ---------------------------------- Matériaux ---------------------------------- */

        private static Material Materiau(string nom, Color couleur, float lisse = 0.15f)
        {
            System.IO.Directory.CreateDirectory(MatDir);
            var path = $"{MatDir}/{nom}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial.shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", couleur);
            m.SetFloat("_Smoothness", lisse);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
