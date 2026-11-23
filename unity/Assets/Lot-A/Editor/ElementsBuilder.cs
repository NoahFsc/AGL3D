using System.Collections.Generic;
using AGL3D.Bridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Génère les modèles low poly des éléments de l'état des lieux et les place dans le
    /// prefab Appartement, à la place des cubes de test. Chaque modèle est un prefab dans
    /// Lot-A/Elements, fait de boîtes ProBuilder (retouchables à la main), avec à la racine
    /// l'InspectableElement et un collider qui englobe tout le modèle : le clic, le survol
    /// (lot B) et les repères (lot C) fonctionnent comme avec les cubes.
    /// Relancer un générateur écrase le prefab de l'élément.
    /// </summary>
    public static partial class ElementsBuilder
    {
        private const string Dir = "Assets/Lot-A/Elements";
        private const string MatDir = Dir + "/Materials";
        private const string AppartementPath = "Assets/Lot-A/Appartement/Appartement.prefab";

        // Les murs de la maquette sont coupés à cette hauteur : les modèles aussi.
        private const float HauteurMurs = 1.2f;

        /* ------------------------------ Porte d'entrée ------------------------------ */

        [MenuItem("AGL3D/Lot A/Modèles/Porte d'entrée")]
        public static void PorteEntree()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var murs = appartement.transform.Find("Murs");
            // L'ouverture de l'entrée est l'espace entre les deux morceaux du mur sud.
            var ouest = Limites(appartement.transform, murs.Find("Mur_Sud_Ouest"));
            var est = Limites(appartement.transform, murs.Find("Mur_Sud_Est"));
            var centre = new Vector3((ouest.max.x + est.min.x) / 2f, 0f, ouest.center.z);

            // Cadre et battant du pack, réduits pour s'arrêter au sommet des murs (1,20 m).
            var root = new GameObject("PorteEntree");
            var cadre = InstancePack("SM_Frame_Door_5", root.transform);
            float echelle = HauteurMurs / LimitesLocales(cadre, root.transform).size.y;
            cadre.localScale = Vector3.one * echelle;
            Centrer(cadre, root.transform);
            var battant = InstancePack("SM_Door_5", root.transform);
            battant.localScale = Vector3.one * echelle;
            Centrer(battant, root.transform);

            // Collider englobant à la racine : il sert au clic, au survol et à placer le repère.
            var limites = LimitesLocales(root.transform, root.transform);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = limites.center;
            collider.size = limites.size;
            Cle(root.AddComponent<InspectableElement>(), "salon.porte-entree");

            // Le cadre réduit est moins large que l'ouverture : deux morceaux de mur comblent
            // les vides de chaque côté. Ils vont dans Murs (pas dans la porte) pour ne pas
            // s'illuminer au survol de la porte.
            float demiCadre = limites.size.x / 2f;
            var murMat = murs.Find("Mur_Sud_Ouest").GetComponent<MeshRenderer>().sharedMaterial;
            foreach (var (nom, x0, x1) in new[] { ("Mur_Sud_Porte_G", ouest.max.x, centre.x - demiCadre), ("Mur_Sud_Porte_D", centre.x + demiCadre, est.min.x) })
            {
                var ancien = murs.Find(nom);
                if (ancien != null) Object.DestroyImmediate(ancien.gameObject);
                if (x1 - x0 < 0.005f) continue;
                var bout = Boite(nom, murs, new Vector3(x0, 0f, ouest.min.z), new Vector3(x1, ouest.max.y, ouest.max.z), murMat);
                // Boite place dans le repère de Murs : on le recale dans celui de l'appartement.
                bout.position = appartement.transform.TransformPoint(new Vector3((x0 + x1) / 2f, ouest.max.y / 2f, ouest.center.z));
                bout.rotation = appartement.transform.rotation;
                bout.gameObject.AddComponent<BoxCollider>();
                bout.gameObject.AddComponent<CameraObstacle>();
            }

            var prefab = Enregistrer(root, "PorteEntree");
            // Rotation de 180° : la face avant du pack (+z) regarde l'intérieur du salon.
            Remplacer(appartement, "salon", "salon.porte-entree", prefab, centre, Quaternion.Euler(0f, 180f, 0f));
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------ Compteur d'eau ------------------------------ */

        [MenuItem("AGL3D/Lot A/Modèles/Compteur d'eau")]
        public static void CompteurEau()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            // Fixé sur la face intérieure du mur ouest, à l'endroit prévu par le plan.
            var mur = Limites(appartement.transform, appartement.transform.Find("Murs/Mur_Ouest"));
            var plan = PositionActuelle(appartement, "salon", "salon.compteur-eau");
            var position = new Vector3(mur.max.x, 0f, plan.z);

            var cuivre = Materiau("Compteur_Cuivre", new Color(0.78f, 0.50f, 0.32f), 0.45f);
            var laiton = Materiau("Compteur_Laiton", new Color(0.80f, 0.66f, 0.36f), 0.5f);
            var bleu = Materiau("Compteur_Bleu", new Color(0.24f, 0.42f, 0.70f), 0.3f);
            var cadran = Materiau("Compteur_Cadran", new Color(0.95f, 0.94f, 0.90f));
            var noir = Materiau("Compteur_Noir", new Color(0.12f, 0.12f, 0.13f));
            var rouge = Materiau("Compteur_Robinet", new Color(0.78f, 0.20f, 0.18f), 0.3f);
            var collier = Materiau("Compteur_Collier", new Color(0.70f, 0.70f, 0.70f), 0.4f);

            // Repère local : x le long du mur, y vers le haut, z vers l'intérieur de la pièce.
            var root = new GameObject("CompteurEau");
            const float rayonTuyau = 0.014f;
            const float zTuyau = 0.04f;           // axe de la conduite, à 4 cm du mur
            const float yCompteur = 0.75f;        // hauteur du compteur, comme sur le plan
            const float rayonCorps = 0.065f;

            // Conduite : arrivée depuis le sol, sortie qui remonte jusqu'à la coupe du mur.
            var tConduite = Groupe("Conduite", root.transform);
            float basCorps = yCompteur - rayonCorps, hautCorps = yCompteur + rayonCorps;
            Cylindre("Arrivee", tConduite, new Vector3(0f, 0f, zTuyau), new Vector3(0f, basCorps - 0.02f, zTuyau), rayonTuyau, 6, cuivre);
            Cylindre("Sortie", tConduite, new Vector3(0f, hautCorps + 0.02f, zTuyau), new Vector3(0f, HauteurMurs - 0.02f, zTuyau), rayonTuyau, 6, cuivre);
            foreach (float y in new[] { basCorps - 0.02f, hautCorps + 0.02f })
                Cylindre(y < yCompteur ? "Ecrou_Bas" : "Ecrou_Haut", tConduite, new Vector3(0f, y - 0.018f, zTuyau), new Vector3(0f, y + 0.018f, zTuyau), 0.022f, 6, laiton);
            // Colliers qui tiennent la conduite au mur.
            foreach (float y in new[] { 0.18f, 1.05f })
            {
                Boite("Collier_" + (y < 0.5f ? "Bas" : "Haut"), tConduite, new Vector3(-0.025f, y - 0.012f, 0f), new Vector3(0.025f, y + 0.012f, 0.012f), collier);
                Boite("Bride_" + (y < 0.5f ? "Bas" : "Haut"), tConduite, new Vector3(-0.02f, y - 0.01f, 0.012f), new Vector3(0.02f, y + 0.01f, zTuyau + rayonTuyau + 0.004f), collier);
            }

            // Robinet d'arrêt sous le compteur, levier rouge.
            var tRobinet = Groupe("Robinet", root.transform);
            const float yRobinet = 0.45f;
            Cylindre("Corps", tRobinet, new Vector3(0f, yRobinet - 0.03f, zTuyau), new Vector3(0f, yRobinet + 0.03f, zTuyau), 0.024f, 6, laiton);
            Cylindre("Axe", tRobinet, new Vector3(0f, yRobinet, zTuyau + 0.02f), new Vector3(0f, yRobinet, zTuyau + 0.045f), 0.007f, 6, laiton);
            Boite("Levier", tRobinet, new Vector3(-0.012f, yRobinet - 0.008f, zTuyau + 0.04f), new Vector3(0.07f, yRobinet + 0.008f, zTuyau + 0.055f), rouge);

            // Compteur : corps rond en laiton, bague bleue, cadran blanc tourné vers la pièce.
            var tCompteur = Groupe("Compteur", root.transform);
            float zFace = zTuyau + 0.045f;
            Cylindre("Corps", tCompteur, new Vector3(0f, yCompteur, 0.012f), new Vector3(0f, yCompteur, zFace), rayonCorps, 10, laiton);
            Cylindre("Bague", tCompteur, new Vector3(0f, yCompteur, zFace - 0.004f), new Vector3(0f, yCompteur, zFace + 0.014f), rayonCorps + 0.006f, 10, bleu);
            Cylindre("Cadran", tCompteur, new Vector3(0f, yCompteur, zFace + 0.014f), new Vector3(0f, yCompteur, zFace + 0.018f), rayonCorps - 0.012f, 10, cadran);
            Boite("Index", tCompteur, new Vector3(-0.028f, yCompteur + 0.012f, zFace + 0.018f), new Vector3(0.028f, yCompteur + 0.03f, zFace + 0.021f), noir);
            Boite("Aiguille", tCompteur, new Vector3(-0.003f, yCompteur - 0.04f, zFace + 0.018f), new Vector3(0.003f, yCompteur - 0.002f, zFace + 0.022f), rouge);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, HauteurMurs / 2f, 0.06f);
            collider.size = new Vector3(0.18f, HauteurMurs, 0.12f);
            Cle(root.AddComponent<InspectableElement>(), "salon.compteur-eau");

            var prefab = Enregistrer(root, "CompteurEau");
            // Mur ouest : la face du compteur regarde vers +x, l'intérieur du salon.
            Remplacer(appartement, "salon", "salon.compteur-eau", prefab, position, Quaternion.Euler(0f, 90f, 0f));
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* --------------------------- Compteur d'électricité --------------------------- */

        [MenuItem("AGL3D/Lot A/Modèles/Compteur d'électricité")]
        public static void CompteurElectricite()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            // Fixé sur la face intérieure du mur ouest, à l'endroit prévu par le plan.
            var mur = Limites(appartement.transform, appartement.transform.Find("Murs/Mur_Ouest"));
            var plan = PositionActuelle(appartement, "salon", "salon.compteur-electricite");
            var position = new Vector3(mur.max.x, 0f, plan.z);

            var platineMat = Materiau("Linky_Platine", new Color(0.93f, 0.93f, 0.91f));
            var anis = Materiau("Linky_Boitier", new Color(0.74f, 0.83f, 0.47f), 0.25f);
            var facade = Materiau("Linky_Facade", new Color(0.80f, 0.88f, 0.55f), 0.25f);
            var ecran = Materiau("Linky_Ecran", new Color(0.14f, 0.19f, 0.17f), 0.6f);
            var gris = Materiau("Linky_CacheBornes", new Color(0.66f, 0.68f, 0.66f));
            var bouton = Materiau("Linky_Bouton", new Color(0.30f, 0.33f, 0.31f));
            var voyant = Materiau("Linky_Voyant", new Color(0.95f, 0.75f, 0.20f), 0.5f);
            var gaine = Materiau("Linky_Gaine", new Color(0.55f, 0.56f, 0.58f));
            var collier = Materiau("Compteur_Collier", new Color(0.70f, 0.70f, 0.70f), 0.4f);

            // Repère local : x le long du mur, y vers le haut, z vers l'intérieur de la pièce.
            var root = new GameObject("CompteurElectricite");
            const float yCentre = 0.86f;

            // Platine blanche fixée au mur, qui porte le compteur.
            var tPlatine = Groupe("Platine", root.transform);
            const float platineL = 0.24f, platineH = 0.34f, platineE = 0.015f;
            float platineBas = yCentre - platineH / 2f - 0.03f, platineHaut = platineBas + platineH;
            Boite("Platine", tPlatine, new Vector3(-platineL / 2f, platineBas, 0f), new Vector3(platineL / 2f, platineHaut, platineE), platineMat);

            // Linky : boîtier vert anis, façade en léger relief, écran et boutons en haut.
            var tLinky = Groupe("Linky", root.transform);
            const float l = 0.15f, h = 0.2f, e = 0.065f;
            float z0 = platineE, z1 = platineE + e;
            float bas = yCentre - h / 2f + 0.02f, haut = bas + h;
            Boite("Boitier", tLinky, new Vector3(-l / 2f, bas, z0), new Vector3(l / 2f, haut, z1), anis);
            Boite("Facade", tLinky, new Vector3(-l / 2f + 0.01f, bas + 0.01f, z1), new Vector3(l / 2f - 0.01f, haut - 0.01f, z1 + 0.005f), facade);
            float zFace = z1 + 0.005f;
            float yEcran = haut - 0.055f;
            Boite("Ecran", tLinky, new Vector3(-0.045f, yEcran - 0.014f, zFace), new Vector3(0.03f, yEcran + 0.014f, zFace + 0.003f), ecran);
            foreach (var (nom, y) in new[] { ("Bouton_Plus", yEcran + 0.008f), ("Bouton_Moins", yEcran - 0.008f) })
                Cylindre(nom, tLinky, new Vector3(0.048f, y, zFace), new Vector3(0.048f, y, zFace + 0.006f), 0.006f, 6, bouton);
            Boite("Voyant", tLinky, new Vector3(-0.045f, yEcran - 0.035f, zFace), new Vector3(-0.037f, yEcran - 0.027f, zFace + 0.003f), voyant);

            // Cache-bornes gris sous le boîtier.
            const float cacheH = 0.06f;
            Boite("CacheBornes", tLinky, new Vector3(-l / 2f + 0.005f, bas - cacheH, z0), new Vector3(l / 2f - 0.005f, bas, z1 - 0.008f), gris);

            // Gaine électrique : arrivée depuis le sol sous le compteur, départ vers le haut.
            var tGaine = Groupe("Gaine", root.transform);
            const float rayonGaine = 0.012f, zGaine = 0.03f;
            Cylindre("Arrivee", tGaine, new Vector3(0.035f, 0f, zGaine), new Vector3(0.035f, platineBas, zGaine), rayonGaine, 6, gaine);
            Cylindre("Depart", tGaine, new Vector3(-0.06f, platineHaut, zGaine), new Vector3(-0.06f, HauteurMurs - 0.02f, zGaine), rayonGaine, 6, gaine);
            foreach (var (x, y) in new[] { (0.035f, 0.2f), (-0.06f, 1.12f) })
            {
                string nom = y < 0.5f ? "Bas" : "Haut";
                Boite("Collier_" + nom, tGaine, new Vector3(x - 0.022f, y - 0.01f, 0f), new Vector3(x + 0.022f, y + 0.01f, 0.01f), collier);
                Boite("Bride_" + nom, tGaine, new Vector3(x - 0.017f, y - 0.008f, 0.01f), new Vector3(x + 0.017f, y + 0.008f, zGaine + rayonGaine + 0.004f), collier);
            }

            // Collider limité à la platine et au compteur : le repère se pose juste au-dessus.
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, (platineBas + platineHaut) / 2f, (zFace + 0.003f) / 2f);
            collider.size = new Vector3(platineL, platineH, zFace + 0.003f);
            Cle(root.AddComponent<InspectableElement>(), "salon.compteur-electricite");

            var prefab = Enregistrer(root, "CompteurElectricite");
            // Mur ouest : la façade regarde vers +x, l'intérieur du salon.
            Remplacer(appartement, "salon", "salon.compteur-electricite", prefab, position, Quaternion.Euler(0f, 90f, 0f));
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* -------------------------------- Mur ouest -------------------------------- */

        /// <summary>
        /// Le mur lui-même est l'élément : l'InspectableElement se pose sur Mur_Ouest, et un
        /// tableau (pack Low Poly Furniture) et une prise, enfants du mur, permettent de le reconnaître. Au survol,
        /// tout le mur s'illumine ; les compteurs, devant le mur, restent cliquables à part.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Modèles/Mur ouest")]
        public static void MurOuest()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var murT = appartement.transform.Find("Murs/Mur_Ouest");
            var mur = Limites(appartement.transform, murT);
            // Le tableau prend la place du cube de test, sur la face intérieure du mur. Quand le
            // générateur est relancé, le cube a disparu : on garde la place du décor existant.
            var decorExistant = murT.Find("MurOuestDecor");
            var plan = decorExistant != null
                ? appartement.transform.InverseTransformPoint(decorExistant.position)
                : PositionActuelle(appartement, "salon", "salon.mur-ouest");

            // Blanc cassé mat : un blanc pur et brillant ressort en halo avec le Bloom de la scène.
            var plaque = Materiau("Prise_Plaque", new Color(0.86f, 0.86f, 0.84f), 0.02f);
            var trou = Materiau("Prise_Trou", new Color(0.22f, 0.22f, 0.23f));

            // Repère local du décor : x le long du mur, y vers le haut, z vers l'intérieur.
            var root = new GameObject("MurOuestDecor");

            // Tableau du pack, réduit à 46 cm de haut, centré à 72 cm du sol.
            var tTableau = InstancePack("SM_picture_3", root.transform);
            tTableau.name = "Tableau";
            tTableau.localScale = Vector3.one * (0.46f / LimitesLocales(tTableau, root.transform).size.y);
            var lt = LimitesLocales(tTableau, root.transform);
            tTableau.localPosition += new Vector3(-lt.center.x, 0.72f - lt.center.y, -lt.min.z);

            // Prise électrique près du sol, à 70 cm du tableau.
            var tPrise = Groupe("Prise", root.transform);
            const float xPrise = -0.7f, yPrise = 0.22f;
            Boite("Plaque", tPrise, new Vector3(xPrise - 0.04f, yPrise - 0.04f, 0f), new Vector3(xPrise + 0.04f, yPrise + 0.04f, 0.012f), plaque);
            Cylindre("Socle", tPrise, new Vector3(xPrise, yPrise, 0.012f), new Vector3(xPrise, yPrise, 0.016f), 0.028f, 8, plaque);
            // Trous en petites boîtes : un cylindre de 4 mm donne des normales invalides, que
            // les post-effets du WebGL étalent en un grand halo blanc.
            foreach (float dx in new[] { -0.01f, 0.01f })
                Boite(dx < 0 ? "Trou_Gauche" : "Trou_Droit", tPrise, new Vector3(xPrise + dx - 0.003f, yPrise - 0.003f, 0.016f), new Vector3(xPrise + dx + 0.003f, yPrise + 0.003f, 0.0185f), trou);

            var decor = Enregistrer(root, "MurOuest", "MurOuestDecor");

            // Le cube de test disparaît : c'est le mur qui porte la clé.
            var cube = appartement.transform.Find("Elements/salon/salon.mur-ouest");
            if (cube != null) Object.DestroyImmediate(cube.gameObject);
            var ancien = murT.Find("MurOuestDecor");
            if (ancien != null) Object.DestroyImmediate(ancien.gameObject);
            var element = murT.GetComponent<InspectableElement>();
            if (element == null) element = murT.gameObject.AddComponent<InspectableElement>();
            Cle(element, "salon.mur-ouest");

            // Décor enfant du mur : il s'illumine et se clique avec lui.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(decor, murT);
            instance.name = "MurOuestDecor";
            instance.transform.position = appartement.transform.TransformPoint(new Vector3(mur.max.x, 0f, plan.z));
            instance.transform.rotation = appartement.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
            instance.transform.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------- Sol du salon ------------------------------- */

        /// <summary>
        /// Le sol est l'élément : un objet sans rendu porte la clé et un collider fin sur tout
        /// le salon, avec en enfants le parquet et le tapis. Ainsi, au survol, c'est le parquet
        /// qui s'illumine (le sol d'origine, caché dessous, ne se verrait pas).
        /// </summary>
        [MenuItem("AGL3D/Lot A/Modèles/Sol du salon")]
        public static void SolSalon()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var t = appartement.transform;
            // Intérieur du salon : entre les faces intérieures des murs et de la cloison.
            float xMin = Limites(t, t.Find("Murs/Mur_Ouest")).max.x;
            float xMax = Limites(t, t.Find("Murs/Cloison_Salon_Milieu")).min.x;
            float zMin = Limites(t, t.Find("Murs/Mur_Sud_Ouest")).max.z;
            float zMax = Limites(t, t.Find("Murs/Mur_Nord")).min.z;
            var centre = new Vector3((xMin + xMax) / 2f, 0f, (zMin + zMax) / 2f);
            float largeur = xMax - xMin, profondeur = zMax - zMin;
            // Le tapis prend la place du cube de test (ou du tapis existant si on relance).
            var existant = t.Find("Elements/salon/salon.sol/Tapis");
            var plan = existant != null ? t.InverseTransformPoint(existant.position) : PositionActuelle(appartement, "salon", "salon.sol");

            var bois = new[]
            {
                Materiau("Parquet_Clair", new Color(0.74f, 0.57f, 0.40f)),
                Materiau("Parquet_Moyen", new Color(0.69f, 0.52f, 0.36f)),
                Materiau("Parquet_Fonce", new Color(0.64f, 0.47f, 0.32f)),
            };
            var bordure = Materiau("Tapis_Bordure", new Color(0.90f, 0.85f, 0.76f));
            var champ = Materiau("Tapis_Champ", new Color(0.70f, 0.40f, 0.32f));
            var motif = Materiau("Tapis_Motif", new Color(0.88f, 0.70f, 0.42f));

            // Repère local : centre de l'intérieur du salon, au niveau du sol.
            var root = new GameObject("SolSalon");

            // Parquet : lames de 14 cm le long de z, joints décalés, trois teintes, un seul maillage.
            const float lame = 0.14f, joint = 0.001f, epaisseur = 0.006f;
            float[] longueurs = { 1.2f, 0.9f, 1.5f, 1.1f, 1.35f };
            var lames = new List<(Vector3, Vector3, int)>();
            int rangee = 0;
            for (float x = -largeur / 2f; x < largeur / 2f - 0.01f; x += lame, rangee++)
            {
                float x1 = Mathf.Min(x + lame, largeur / 2f);
                // Chaque rangée commence à un endroit différent : les joints ne s'alignent pas.
                float z = -profondeur / 2f;
                int i = rangee * 3;
                float debut = longueurs[rangee % longueurs.Length] * ((rangee * 0.37f) % 1f);
                float fin = z + (debut > 0.2f ? debut : longueurs[i % longueurs.Length]);
                while (z < profondeur / 2f - 0.01f)
                {
                    fin = Mathf.Min(fin, profondeur / 2f);
                    lames.Add((new Vector3(x + joint, 0f, z + joint), new Vector3(x1 - joint, epaisseur, fin - joint), (rangee * 7 + i) % bois.Length));
                    z = fin;
                    i++;
                    fin = z + longueurs[i % longueurs.Length];
                }
            }
            var parquet = new GameObject("Parquet");
            parquet.transform.SetParent(root.transform, false);
            parquet.AddComponent<MeshFilter>().sharedMesh = Lames("SolSalon", "Parquet", lames, bois.Length);
            parquet.AddComponent<MeshRenderer>().sharedMaterials = bois;

            // Tapis terracotta à bordure crème et losange central, avec franges aux deux bouts.
            var tTapis = Groupe("Tapis", root.transform);
            // Le tapis est décalé de 30 cm à l'est du centre du salon : le canapé, centré sur
            // lui, laisse ainsi de la place devant la fenêtre.
            tTapis.localPosition = new Vector3(0.3f, epaisseur, plan.z - centre.z);
            const float tl = 1.7f, tp = 1.15f;
            Boite("Bordure", tTapis, new Vector3(-tl / 2f, 0f, -tp / 2f), new Vector3(tl / 2f, 0.008f, tp / 2f), bordure);
            Boite("Champ", tTapis, new Vector3(-tl / 2f + 0.09f, 0.008f, -tp / 2f + 0.09f), new Vector3(tl / 2f - 0.09f, 0.011f, tp / 2f - 0.09f), champ);
            Boite("Losange", tTapis, new Vector3(-0.2f, 0.011f, -0.2f), new Vector3(0.2f, 0.013f, 0.2f), motif).localRotation = Quaternion.Euler(0f, 45f, 0f);
            foreach (float cote in new[] { -1f, 1f })
                for (int f = 0; f < 9; f++)
                {
                    float zf = -tp / 2f + 0.08f + f * (tp - 0.16f) / 8f;
                    float xf = cote * (tl / 2f + 0.03f);
                    Boite($"Frange_{(cote < 0 ? "G" : "D")}{f}", tTapis, new Vector3(xf - 0.03f, 0f, zf - 0.012f), new Vector3(xf + 0.03f, 0.005f, zf + 0.012f), bordure);
                }

            // Collider fin sur tout le salon, juste au-dessus du sol d'origine.
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, epaisseur / 2f, 0f);
            collider.size = new Vector3(largeur, epaisseur + 0.004f, profondeur);
            Cle(root.AddComponent<InspectableElement>(), "salon.sol");

            var prefab = Enregistrer(root, "SolSalon");
            Remplacer(appartement, "salon", "salon.sol", prefab, centre, Quaternion.identity);
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* -------------------------------- Décor du salon -------------------------------- */

        /// <summary>Canapé (Furniture Mega Pack) contre le mur nord du salon, face au tapis. Décor : pas de clé.</summary>
        [MenuItem("AGL3D/Lot A/Décor/Canapé")]
        public static void Canape()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var t = appartement.transform;
            float zMur = Limites(t, t.Find("Murs/Mur_Nord")).min.z;
            var tapis = t.Find("Elements/salon/salon.sol/Tapis");
            float xCentre = tapis != null ? t.InverseTransformPoint(tapis.position).x : -2.2f;

            // Canapé bleu marine du Furniture Mega Pack, ramené à 2,07 m pour laisser la place
            // à la fenêtre. Son avant regarde -z : on le retourne pour que l'assise soit en +z.
            // Repère local : x le long du canapé, y vers le haut, z vers l'avant (assise), dos en z = 0.
            var root = new GameObject("Canape");
            var modele = InstancePrefab($"{MegaPackDir}/Sofas/Sofa17.prefab", "Furniture Mega Pack - Free", root.transform);
            modele.localRotation = Quaternion.Euler(0f, 180f, 0f);
            modele.localScale = Vector3.one * (2.07f / LimitesLocales(modele, root.transform).size.x);
            var lm = LimitesLocales(modele, root.transform);
            modele.localPosition += new Vector3(-lm.center.x, -lm.min.y, -lm.min.z);
            lm = LimitesLocales(modele, root.transform);

            // Collider : un clic sur le canapé compte comme un clic dans le vide.
            var collider = root.AddComponent<BoxCollider>();
            collider.center = lm.center;
            collider.size = lm.size;

            var prefab = Enregistrer(root, "Decor/Canape", "Canape");
            // Dos au mur nord : l'assise regarde vers le sud (-z de l'appartement).
            PlacerDecor(appartement, "Canape", prefab, new Vector3(xCentre, 0f, zMur - 0.02f), Quaternion.Euler(0f, 180f, 0f));
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Fenêtre sur le mur nord du salon, à gauche du canapé, fermée sous la coupe du mur :
        /// cadre complet des deux côtés du mur et vitre bleutée à deux battants.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Décor/Fenêtre du salon")]
        public static void Fenetre()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var t = appartement.transform;
            var mur = Limites(t, t.Find("Murs/Mur_Nord"));
            float xOuest = Limites(t, t.Find("Murs/Mur_Ouest")).max.x;

            var cadre = Materiau("Fenetre_Cadre", new Color(0.92f, 0.92f, 0.90f));
            var vitre = Materiau("Fenetre_Vitre", new Color(0.66f, 0.80f, 0.90f), 0.75f);
            var tablette = Materiau("Fenetre_Tablette", new Color(0.86f, 0.85f, 0.82f));

            // Repère local : x le long du mur, y vers le haut, z épaisseur (0 = milieu du mur).
            var root = new GameObject("Fenetre");
            const float l = 0.66f, bas = 0.55f, haut = 1.02f, montant = 0.05f;
            float d = l / 2f, e = mur.size.z / 2f;
            // La vitre traverse le mur et dépasse un peu de chaque côté : elle cache le mur.
            Boite("Vitre", root.transform, new Vector3(-d, bas, -e - 0.004f), new Vector3(d, haut, e + 0.004f), vitre);
            foreach (float face in new[] { -1f, 1f })
            {
                string n = face > 0 ? "Int" : "Ext";
                float z0 = face * (e + 0.004f), z1 = face * (e + 0.016f);
                var a = new Vector3(0f, 0f, Mathf.Min(z0, z1));
                var b = new Vector3(0f, 0f, Mathf.Max(z0, z1));
                Boite("Montant_G_" + n, root.transform, new Vector3(-d - montant, bas - montant, a.z), new Vector3(-d, haut + montant, b.z), cadre);
                Boite("Montant_D_" + n, root.transform, new Vector3(d, bas - montant, a.z), new Vector3(d + montant, haut + montant, b.z), cadre);
                Boite("Traverse_Haut_" + n, root.transform, new Vector3(-d, haut, a.z), new Vector3(d, haut + montant, b.z), cadre);
                Boite("Traverse_Bas_" + n, root.transform, new Vector3(-d, bas - montant, a.z), new Vector3(d, bas, b.z), cadre);
                Boite("Meneau_" + n, root.transform, new Vector3(-0.015f, bas, a.z), new Vector3(0.015f, haut, b.z), cadre);
            }
            // Tablette intérieure sous la fenêtre (côté salon, -z après rotation).
            Boite("Tablette", root.transform, new Vector3(-d - montant - 0.03f, bas - montant - 0.025f, e + 0.016f), new Vector3(d + montant + 0.03f, bas - montant, e + 0.1f), tablette);

            var prefab = Enregistrer(root, "Decor/Fenetre", "Fenetre");
            // Rotation de 180° : la tablette (+z local) passe côté salon.
            float x = xOuest + 0.12f + d + montant;
            PlacerDecor(appartement, "Fenetre", prefab, new Vector3(x, 0f, mur.center.z), Quaternion.Euler(0f, 180f, 0f));
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Place (ou remplace) un décor dans le groupe Decor de l'appartement.</summary>
        private static void PlacerDecor(GameObject appartement, string nom, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var groupe = appartement.transform.Find("Decor");
            if (groupe == null) groupe = Groupe("Decor", appartement.transform);
            var ancien = groupe.Find(nom);
            if (ancien != null) Object.DestroyImmediate(ancien.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, groupe);
            instance.name = nom;
            instance.transform.position = appartement.transform.TransformPoint(position);
            instance.transform.rotation = appartement.transform.rotation * rotation;
            instance.transform.localScale = Vector3.one;
        }

        /* --------------------------------- Outils --------------------------------- */

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
            mesh.indexFormat = sommets.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(sommets);
            mesh.subMeshCount = teintes;
            for (int i = 0; i < teintes; i++) mesh.SetTriangles(parTeinte[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private const string PackDir = "Assets/LowPolyFurniturePackFree/Mesh";

        private const string MegaPackDir = "Assets/Furniture Mega Pack/Prefabs";

        /// <summary>Instance d'un modèle du pack Low Poly Furniture (prefab imbriqué), sans collider.</summary>
        private static Transform InstancePack(string nom, Transform parent) =>
            InstancePrefab($"{PackDir}/{nom}.prefab", "Low Poly Furniture Pack Free", parent);

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

        /// <summary>Centre l'objet en x et z sur l'origine de son parent, posé au sol (y = 0).</summary>
        private static void Centrer(Transform objet, Transform repere)
        {
            var l = LimitesLocales(objet, repere);
            objet.localPosition += new Vector3(-l.center.x, -l.min.y, -l.center.z);
        }

        /// <summary>Position actuelle d'un élément (cube de test ou modèle) dans le repère de l'appartement.</summary>
        private static Vector3 PositionActuelle(GameObject appartement, string piece, string cle)
        {
            var element = appartement.transform.Find($"Elements/{piece}/{cle}");
            return appartement.transform.InverseTransformPoint(element.position);
        }

        /// <summary>Remplace l'élément de test (cube) par le modèle, au même endroit dans la hiérarchie.</summary>
        private static void Remplacer(GameObject appartement, string piece, string cle, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var groupe = appartement.transform.Find("Elements/" + piece);
            var ancien = groupe.Find(cle);
            int index = ancien != null ? ancien.GetSiblingIndex() : groupe.childCount;
            if (ancien != null) Object.DestroyImmediate(ancien.gameObject);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, groupe);
            instance.name = cle;
            instance.transform.SetSiblingIndex(index);
            // La position est calculée dans le repère de l'appartement.
            instance.transform.position = appartement.transform.TransformPoint(position);
            instance.transform.rotation = appartement.transform.rotation * rotation;
            instance.transform.localScale = Vector3.one;
        }

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

        private static void Cle(InspectableElement element, string cle)
        {
            var so = new SerializedObject(element);
            so.FindProperty("key").stringValue = cle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Enregistrer(GameObject root, string nom) => Enregistrer(root, nom, nom);

        private static GameObject Enregistrer(GameObject root, string dossierNom, string nom)
        {
            var dossier = $"{Dir}/{dossierNom}";
            System.IO.Directory.CreateDirectory(dossier);
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

        /// <summary>Boîte ProBuilder entre deux coins, dans le repère de <paramref name="parent"/>.</summary>
        private static Transform Boite(string nom, Transform parent, Vector3 min, Vector3 max, Material materiau)
        {
            var pb = ShapeGenerator.GenerateCube(PivotLocation.Center, max - min);
            pb.gameObject.name = nom;
            pb.transform.SetParent(parent, false);
            pb.transform.localPosition = (min + max) / 2f;
            pb.GetComponent<MeshRenderer>().sharedMaterial = materiau;
            Finaliser(pb);
            return pb.transform;
        }

        /// <summary>
        /// Cylindre ProBuilder à faces plates (effet low poly) entre deux points, dans le repère
        /// de <paramref name="parent"/>. <paramref name="cotes"/> est arrondi au nombre pair.
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

        /// <summary>
        /// Construit le maillage, retire les colliders (celui de la racine suffit) et vérifie les
        /// normales : une normale invalide (forme trop petite) donne des pixels NaN que les
        /// post-effets du WebGL étalent en halo blanc.
        /// </summary>
        private static void Finaliser(ProBuilderMesh pb)
        {
            pb.ToMesh();
            pb.Refresh();
            foreach (var c in pb.GetComponents<Collider>()) Object.DestroyImmediate(c);
            foreach (var n in pb.GetComponent<MeshFilter>().sharedMesh.normals)
            {
                if (float.IsNaN(n.x) || float.IsNaN(n.y) || float.IsNaN(n.z) || n.sqrMagnitude < 0.5f)
                    throw new System.InvalidOperationException($"Normales invalides sur « {pb.name} » : forme trop petite, agrandissez-la ou utilisez une boîte.");
            }
        }

        /// <summary>Prisme triangulaire (pointe en haut) entre deux coins, profondeur sur z.</summary>
        private static void Prisme(string nom, Transform parent, Vector3 min, Vector3 max, Material materiau)
        {
            var pb = ShapeGenerator.GeneratePrism(PivotLocation.Center, max - min);
            pb.gameObject.name = nom;
            pb.transform.SetParent(parent, false);
            pb.transform.localPosition = (min + max) / 2f;
            pb.GetComponent<MeshRenderer>().sharedMaterial = materiau;
            Finaliser(pb);
        }

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
