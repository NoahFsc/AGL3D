using AGL3D.Bridge;
using UnityEditor;
using UnityEngine;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Génère les pièces de l'appartement (salon, chambre, cuisine, habillage des murs) et les
    /// place dans le prefab Appartement, à la place des cubes de test. Chaque modèle est un prefab
    /// dans Lot-A/Elements, fait de boîtes (maillages enregistrés à côté du prefab) et de modèles
    /// des packs de meubles, avec à la racine l'InspectableElement et un collider qui englobe le
    /// modèle : le clic, le survol (lot B) et les repères (lot C) fonctionnent comme avec les cubes.
    /// Relancer un générateur écrase le prefab de l'élément.
    ///
    /// Organisation : un fichier par pièce (ElementsBuilder.Salon, .Chambre, .Cuisine, .Habillage),
    /// autour des outils communs de ce fichier (ouverture du prefab, mesure des pièces, placement)
    /// et de ElementsBuilder.Formes (boîtes, sols, modèles des packs, matériaux).
    ///
    /// Convention des prefabs : x le long du mur, y vers le haut, z vers l'intérieur de la pièce,
    /// dos du meuble en z = 0. Rotations pour poser un meuble dos à un mur : nord 180°, est -90°,
    /// sud 0°, ouest 90°.
    /// </summary>
    public static partial class ElementsBuilder
    {
        private const string Dir = "Assets/Lot-A/Elements";
        private const string MatDir = Dir + "/Materials";
        private const string AppartementPath = "Assets/Lot-A/Appartement/Appartement.prefab";
        /// <summary>Maillages des boîtes posées directement dans l'appartement (habillage, murs de la porte).</summary>
        private const string MaillagesAppartementDir = "Assets/Lot-A/Appartement/Maillages";

        // Les murs de la maquette sont coupés à cette hauteur : les modèles aussi.
        private const float HauteurMurs = 1.2f;

        /// <summary>
        /// Tous les générateurs, dans l'ordre : le salon d'abord (le canapé se cale sur le tapis,
        /// l'habillage contourne la fenêtre et les murs de la porte), puis l'habillage, la chambre
        /// et la cuisine. Le prefab Appartement n'est chargé et enregistré qu'une fois.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Tout l'appartement", priority = 0)]
        public static void ToutAppartement() => Modifier(a =>
        {
            Salon(a);
            HabillageMurs(a);
            Chambre(a);
            Cuisine(a);
        });

        /* ------------------------- Ouverture du prefab Appartement ------------------------- */

        /// <summary>Prefab Appartement ouvert par la génération en cours, null en dehors.</summary>
        private static GameObject appartementOuvert;

        /// <summary>
        /// Ouvre le prefab Appartement, lance le générateur et enregistre le prefab. Appelé pendant
        /// une autre génération (« Toute la cuisine », « Tout l'appartement »), il réutilise le
        /// prefab déjà ouvert : chargement et enregistrement n'ont lieu qu'une fois.
        /// </summary>
        private static void Modifier(System.Action<GameObject> generer)
        {
            if (appartementOuvert != null)
            {
                generer(appartementOuvert);
                return;
            }
            appartementOuvert = PrefabUtility.LoadPrefabContents(AppartementPath);
            try
            {
                generer(appartementOuvert);
                if (maillagesEnAttente.Count > 0)
                    throw new System.InvalidOperationException($"{maillagesEnAttente.Count} maillages non enregistrés : appelez EnregistrerMaillages pour les boîtes posées dans l'appartement.");
                PrefabUtility.SaveAsPrefabAsset(appartementOuvert, AppartementPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(appartementOuvert);
                appartementOuvert = null;
                maillagesEnAttente.Clear();
            }
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------- Mesure des pièces ------------------------------- */

        /// <summary>Intérieur d'une pièce, dans le repère de l'appartement : faces intérieures des murs.</summary>
        private struct Piece
        {
            public float xMin, xMax, zMin, zMax;
            /// <summary>Ouverture vers le salon, dans le mur ouest (chambre).</summary>
            public float zPorteBas, zPorteHaut;
            public Vector3 Centre => new Vector3((xMin + xMax) / 2f, 0f, (zMin + zMax) / 2f);
            public float Largeur => xMax - xMin;
            public float Profondeur => zMax - zMin;
        }

        /// <summary>Mesure une pièce entre ses quatre murs (et, si donnés, les deux murs qui bordent sa porte).</summary>
        private static Piece Mesurer(Transform t, string ouest, string est, string sud, string nord, string porteSud = null, string porteNord = null)
        {
            Bounds Mur(string nom) => Limites(t, t.Find("Murs/" + nom));
            var piece = new Piece { xMin = Mur(ouest).max.x, xMax = Mur(est).min.x, zMin = Mur(sud).max.z, zMax = Mur(nord).min.z };
            if (porteSud != null)
            {
                piece.zPorteBas = Mur(porteSud).max.z;
                piece.zPorteHaut = Mur(porteNord).min.z;
            }
            return piece;
        }

        private static Piece PieceSalon(Transform t) => Mesurer(t, "Mur_Ouest", "Cloison_Salon_Milieu", "Mur_Sud_Ouest", "Mur_Nord");
        private static Piece PieceChambre(Transform t) => Mesurer(t, "Cloison_Salon_Nord", "Mur_Est", "Cloison_Chambre_Cuisine", "Mur_Nord", "Cloison_Salon_Milieu", "Cloison_Salon_Nord");
        private static Piece PieceCuisine(Transform t) => Mesurer(t, "Cloison_Salon_Sud", "Mur_Est", "Mur_Sud_Est", "Cloison_Chambre_Cuisine");

        /// <summary>Limites d'un mur dans le repère de l'appartement (murs non tournés).</summary>
        private static Bounds Limites(Transform appartement, Transform mur)
        {
            var b = mur.GetComponent<Collider>().bounds;
            var min = appartement.InverseTransformPoint(b.min);
            var max = appartement.InverseTransformPoint(b.max);
            var limites = new Bounds((min + max) / 2f, Vector3.zero);
            limites.Encapsulate(min);
            limites.Encapsulate(max);
            return limites;
        }

        /// <summary>Position actuelle d'un élément (cube de test ou modèle) dans le repère de l'appartement.</summary>
        private static Vector3 PositionActuelle(GameObject appartement, string piece, string cle)
        {
            var element = appartement.transform.Find($"Elements/{piece}/{cle}");
            return appartement.transform.InverseTransformPoint(element.position);
        }

        /* ------------------------------ Éléments et décor ------------------------------ */

        /// <summary>
        /// Termine un élément inspectable : clé, prefab dans Lot-A/Elements/<paramref name="dossier"/>
        /// (par défaut le nom de la racine), puis remplacement du cube de test de la pièce (le début
        /// de la clé). Position et rotation dans le repère de l'appartement.
        /// </summary>
        private static void Element(GameObject appartement, GameObject root, string cle, Vector3 position, Quaternion rotation, string dossier = null)
        {
            Cle(root.AddComponent<InspectableElement>(), cle);
            var prefab = Enregistrer(root, dossier ?? root.name);
            Remplacer(appartement, cle.Substring(0, cle.IndexOf('.')), cle, prefab, position, rotation);
        }

        /// <summary>
        /// Prefab de décor (Lot-A/Elements/Decor/<paramref name="chemin"/>), sans clé, avec un collider
        /// par pièce du meuble (table, chaise, lampe...) plutôt qu'une boîte englobante : le vide
        /// entre les meubles laisse passer le clic jusqu'au sol, et un clic sur un meuble compte
        /// comme un clic dans le vide.
        /// </summary>
        private static GameObject Decor(string chemin, System.Action<Transform> construire)
        {
            string nom = chemin.Substring(chemin.LastIndexOf('/') + 1);
            var root = new GameObject(nom);
            construire(root.transform);
            foreach (Transform piece in root.transform)
            {
                var limites = LimitesLocales(piece, root.transform);
                var collider = root.AddComponent<BoxCollider>();
                collider.center = limites.center;
                collider.size = limites.size;
            }
            return Enregistrer(root, "Decor/" + chemin, nom);
        }

        /// <summary>Collider de la racine, sur toute la boîte de l'élément. Renvoie cette boîte.</summary>
        private static Bounds AjouterCollider(GameObject root)
        {
            var limites = LimitesLocales(root.transform, root.transform);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = limites.center;
            collider.size = limites.size;
            return limites;
        }

        /// <summary>Remplace l'élément de test (cube) par le modèle, au même endroit dans la hiérarchie.</summary>
        private static void Remplacer(GameObject appartement, string piece, string cle, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var groupe = appartement.transform.Find("Elements/" + piece);
            var ancien = groupe.Find(cle);
            int index = ancien != null ? ancien.GetSiblingIndex() : groupe.childCount;
            if (ancien != null) Object.DestroyImmediate(ancien.gameObject);
            var instance = Placer(appartement, prefab, groupe, cle, position, rotation);
            instance.transform.SetSiblingIndex(index);
        }

        /// <summary>Place (ou remplace) un décor dans le groupe Decor de l'appartement.</summary>
        private static void PlacerDecor(GameObject appartement, string nom, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var groupe = appartement.transform.Find("Decor");
            if (groupe == null) groupe = Groupe("Decor", appartement.transform);
            var ancien = groupe.Find(nom);
            if (ancien != null) Object.DestroyImmediate(ancien.gameObject);
            Placer(appartement, prefab, groupe, nom, position, rotation);
        }

        /// <summary>Instance du prefab dans <paramref name="groupe"/>, position et rotation dans le repère de l'appartement.</summary>
        private static GameObject Placer(GameObject appartement, GameObject prefab, Transform groupe, string nom, Vector3 position, Quaternion rotation)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, groupe);
            instance.name = nom;
            instance.transform.position = appartement.transform.TransformPoint(position);
            instance.transform.rotation = appartement.transform.rotation * rotation;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void Cle(InspectableElement element, string cle)
        {
            var so = new SerializedObject(element);
            so.FindProperty("key").stringValue = cle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Enregistrer(GameObject root, string nom) => Enregistrer(root, nom, nom);

        /// <summary>
        /// Enregistre <paramref name="root"/> en prefab Lot-A/Elements/dossier/nom.prefab, avec les
        /// maillages de ses boîtes dans nom_Maillages.asset, et détruit l'original.
        /// </summary>
        private static GameObject Enregistrer(GameObject root, string dossierNom, string nom)
        {
            var dossier = $"{Dir}/{dossierNom}";
            System.IO.Directory.CreateDirectory(dossier);
            EnregistrerMaillages($"{dossier}/{nom}_Maillages.asset", root.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{dossier}/{nom}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Transform Groupe(string nom, Transform parent)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /* ------------------------------- Pose des modèles ------------------------------- */

        /// <summary>Limites des rendus de <paramref name="objet"/>, dans le repère de <paramref name="repere"/>.</summary>
        private static Bounds LimitesLocales(Transform objet, Transform repere)
        {
            bool premier = true;
            var limites = new Bounds();
            foreach (var mf in objet.GetComponentsInChildren<MeshFilter>())
            {
                var b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var coin = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    var p = repere.InverseTransformPoint(mf.transform.TransformPoint(coin));
                    if (premier) { limites = new Bounds(p, Vector3.zero); premier = false; }
                    else limites.Encapsulate(p);
                }
            }
            return limites;
        }

        /// <summary>Échelle uniforme qui donne au modèle la largeur (x) voulue dans le repère.</summary>
        private static void MettreALargeur(Transform modele, Transform repere, float largeur) =>
            modele.localScale = Vector3.one * (largeur / LimitesLocales(modele, repere).size.x);

        /// <summary>Échelle uniforme qui donne au modèle la hauteur (y) voulue dans le repère.</summary>
        private static void MettreAHauteur(Transform modele, Transform repere, float hauteur) =>
            modele.localScale = Vector3.one * (hauteur / LimitesLocales(modele, repere).size.y);

        /// <summary>Centre l'objet en x et z sur l'origine de son parent, posé au sol (y = 0).</summary>
        private static void Centrer(Transform objet, Transform repere)
        {
            var l = LimitesLocales(objet, repere);
            objet.localPosition += new Vector3(-l.center.x, -l.min.y, -l.center.z);
        }

        /// <summary>Centre le modèle en x, le pose au sol, dos en z = 0.</summary>
        private static void Poser(Transform modele, Transform repere)
        {
            var l = LimitesLocales(modele, repere);
            modele.localPosition += new Vector3(-l.center.x, -l.min.y, -l.min.z);
        }

        /// <summary>
        /// Met le modèle aux dimensions voulues (x, y, z dans le repère du parent), puis le pose :
        /// centré en x, au sol, dos en z = 0.
        /// </summary>
        private static void Ajuster(Transform modele, Transform repere, float largeur, float hauteur, float profondeur)
        {
            // La racine des modèles du pack est souvent tournée (import FBX) : une échelle par
            // axe posée sur elle partirait de travers. On l'emballe dans un groupe non tourné,
            // et c'est le groupe qui prend l'échelle, dans les axes du repère.
            var groupe = Groupe(modele.name, modele.parent);
            modele.SetParent(groupe, true);
            var taille = LimitesLocales(groupe, repere).size;
            groupe.localScale = new Vector3(largeur / taille.x, hauteur / taille.y, profondeur / taille.z);
            Poser(groupe, repere);
        }

        /* --------------------------- Détails communs aux murs --------------------------- */

        /// <summary>Tableau du pack Low Poly Furniture contre le mur (z = 0), centré en x, à la hauteur voulue.</summary>
        private static void Tableau(Transform parent, float hauteur, float yCentre)
        {
            var tableau = InstancePack("SM_picture_3", parent);
            tableau.name = "Tableau";
            MettreAHauteur(tableau, parent, hauteur);
            var l = LimitesLocales(tableau, parent);
            tableau.localPosition += new Vector3(-l.center.x, yCentre - l.center.y, -l.min.z);
        }

        /// <summary>Prise électrique contre le mur (z = 0), centrée en (x, y).</summary>
        private static void Prise(Transform parent, float x, float y)
        {
            // Blanc cassé mat : un blanc pur et brillant ressort en halo avec le Bloom de la scène.
            var plaque = Materiau("Prise_Plaque", new Color(0.86f, 0.86f, 0.84f), 0.02f);
            var trou = Materiau("Prise_Trou", new Color(0.22f, 0.22f, 0.23f));
            var prise = Groupe("Prise", parent);
            Boite("Plaque", prise, new Vector3(x - 0.04f, y - 0.04f, 0f), new Vector3(x + 0.04f, y + 0.04f, 0.012f), plaque);
            Cylindre("Socle", prise, new Vector3(x, y, 0.012f), new Vector3(x, y, 0.016f), 0.028f, 8, plaque);
            // Trous en petites boîtes : un cylindre de 4 mm donne des normales invalides, que
            // les post-effets du WebGL étalent en un grand halo blanc.
            foreach (float dx in new[] { -0.01f, 0.01f })
                Boite(dx < 0 ? "Trou_Gauche" : "Trou_Droit", prise, new Vector3(x + dx - 0.003f, y - 0.003f, 0.016f), new Vector3(x + dx + 0.003f, y + 0.003f, 0.0185f), trou);
        }
    }
}
