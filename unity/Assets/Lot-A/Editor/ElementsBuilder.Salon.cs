using AGL3D.Bridge;
using UnityEditor;
using UnityEngine;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Générateurs du salon : porte d'entrée, compteurs d'eau et d'électricité, mur ouest, sol
    /// (parquet et tapis), et décor (canapé, fenêtre).
    /// </summary>
    public static partial class ElementsBuilder
    {
        [MenuItem("AGL3D/Lot A/Salon/Tout le salon", priority = 0)]
        public static void SalonComplet() => Modifier(Salon);

        private static void Salon(GameObject appartement)
        {
            PorteEntree(appartement);
            CompteurEau(appartement);
            CompteurElectricite(appartement);
            MurOuest(appartement);
            SolSalon(appartement);
            // Après le sol : le canapé se cale sur le tapis.
            Canape(appartement);
            Fenetre(appartement);
        }

        [MenuItem("AGL3D/Lot A/Salon/Porte d'entrée")] public static void PorteEntree() => Modifier(PorteEntree);
        [MenuItem("AGL3D/Lot A/Salon/Compteur d'eau")] public static void CompteurEau() => Modifier(CompteurEau);
        [MenuItem("AGL3D/Lot A/Salon/Compteur d'électricité")] public static void CompteurElectricite() => Modifier(CompteurElectricite);
        [MenuItem("AGL3D/Lot A/Salon/Mur ouest")] public static void MurOuest() => Modifier(MurOuest);
        [MenuItem("AGL3D/Lot A/Salon/Sol")] public static void SolSalon() => Modifier(SolSalon);
        [MenuItem("AGL3D/Lot A/Salon/Canapé")] public static void Canape() => Modifier(Canape);
        [MenuItem("AGL3D/Lot A/Salon/Fenêtre")] public static void Fenetre() => Modifier(Fenetre);

        /* ------------------------------ Porte d'entrée ------------------------------ */

        private static void PorteEntree(GameObject appartement)
        {
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
            var limites = AjouterCollider(root);

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
            EnregistrerMaillages($"{MaillagesAppartementDir}/MursPorte.asset", murs);

            // Rotation de 180° : la face avant du pack (+z) regarde l'intérieur du salon.
            Element(appartement, root, "salon.porte-entree", centre, Quaternion.Euler(0f, 180f, 0f));
        }

        /* ------------------------------ Compteur d'eau ------------------------------ */

        private static void CompteurEau(GameObject appartement)
        {
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

            // Mur ouest : la face du compteur regarde vers +x, l'intérieur du salon.
            Element(appartement, root, "salon.compteur-eau", position, Quaternion.Euler(0f, 90f, 0f));
        }

        /* --------------------------- Compteur d'électricité --------------------------- */

        private static void CompteurElectricite(GameObject appartement)
        {
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

            // Mur ouest : la façade regarde vers +x, l'intérieur du salon.
            Element(appartement, root, "salon.compteur-electricite", position, Quaternion.Euler(0f, 90f, 0f));
        }

        /* -------------------------------- Mur ouest -------------------------------- */

        /// <summary>
        /// Le mur lui-même est l'élément : l'InspectableElement se pose sur Mur_Ouest, et un
        /// tableau et une prise, enfants du mur, permettent de le reconnaître. Au survol, tout le
        /// mur s'illumine ; les compteurs, devant le mur, restent cliquables à part.
        /// </summary>
        private static void MurOuest(GameObject appartement)
        {
            var murT = appartement.transform.Find("Murs/Mur_Ouest");
            var mur = Limites(appartement.transform, murT);
            // Le tableau prend la place du cube de test, sur la face intérieure du mur. Quand le
            // générateur est relancé, le cube a disparu : on garde la place du décor existant.
            var decorExistant = murT.Find("MurOuestDecor");
            var plan = decorExistant != null
                ? appartement.transform.InverseTransformPoint(decorExistant.position)
                : PositionActuelle(appartement, "salon", "salon.mur-ouest");

            // Tableau réduit à 46 cm de haut, centré à 72 cm du sol ; prise près du sol, à 70 cm.
            var root = new GameObject("MurOuestDecor");
            Tableau(root.transform, 0.46f, 0.72f);
            Prise(root.transform, -0.7f, 0.22f);
            var decor = Enregistrer(root, "MurOuest", "MurOuestDecor");

            // Le cube de test disparaît : c'est le mur qui porte la clé.
            var cube = appartement.transform.Find("Elements/salon/salon.mur-ouest");
            if (cube != null) Object.DestroyImmediate(cube.gameObject);
            if (decorExistant != null) Object.DestroyImmediate(decorExistant.gameObject);
            var element = murT.GetComponent<InspectableElement>();
            if (element == null) element = murT.gameObject.AddComponent<InspectableElement>();
            Cle(element, "salon.mur-ouest");

            // Décor enfant du mur : il s'illumine et se clique avec lui.
            Placer(appartement, decor, murT, "MurOuestDecor", new Vector3(mur.max.x, 0f, plan.z), Quaternion.Euler(0f, 90f, 0f));
        }

        /* ------------------------------- Sol du salon ------------------------------- */

        /// <summary>
        /// Le sol est l'élément : un objet sans rendu porte la clé et un collider fin sur tout
        /// le salon, avec en enfants le parquet et le tapis. Ainsi, au survol, c'est le parquet
        /// qui s'illumine (le sol d'origine, caché dessous, ne se verrait pas).
        /// </summary>
        private static void SolSalon(GameObject appartement)
        {
            var t = appartement.transform;
            var piece = PieceSalon(t);
            var centre = piece.Centre;
            float largeur = piece.Largeur, profondeur = piece.Profondeur;
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

            // Parquet : lames de 14 cm le long de z, joints décalés, trois teintes.
            const float epaisseur = 0.006f;
            float[] longueurs = { 1.2f, 0.9f, 1.5f, 1.1f, 1.35f };
            Surface("Parquet", root.transform, "SolSalon", new Calepinage
            {
                u0 = -profondeur / 2f, u1 = profondeur / 2f, v0 = -largeur / 2f, v1 = largeur / 2f,
                rangee = 0.14f, joint = 0.001f, longueurs = longueurs, decalage = 3, pasTeinte = 7,
                // Chaque rangée commence à un endroit différent : les joints ne s'alignent pas.
                premiere = (u, r) =>
                {
                    float debut = longueurs[r % longueurs.Length] * ((r * 0.37f) % 1f);
                    return u + (debut > 0.2f ? debut : longueurs[r * 3 % longueurs.Length]);
                },
            }, bois, (z0, z1, x0, x1) => (new Vector3(x0, 0f, z0), new Vector3(x1, epaisseur, z1)));

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

            Element(appartement, root, "salon.sol", centre, Quaternion.identity);
        }

        /* -------------------------------- Décor du salon -------------------------------- */

        /// <summary>Canapé (Furniture Mega Pack) contre le mur nord du salon, face au tapis. Décor : pas de clé.</summary>
        private static void Canape(GameObject appartement)
        {
            var t = appartement.transform;
            float zMur = Limites(t, t.Find("Murs/Mur_Nord")).min.z;
            var tapis = t.Find("Elements/salon/salon.sol/Tapis");
            float xCentre = tapis != null ? t.InverseTransformPoint(tapis.position).x : -2.2f;

            // Canapé bleu marine, ramené à 2,07 m pour laisser la place à la fenêtre.
            var canape = Decor("Canape", root =>
            {
                var modele = InstanceMega("Sofas/Sofa17", root);
                MettreALargeur(modele, root, 2.07f);
                Poser(modele, root);
            });
            // Dos au mur nord : l'assise regarde vers le sud (-z de l'appartement).
            PlacerDecor(appartement, "Canape", canape, new Vector3(xCentre, 0f, zMur - 0.02f), Quaternion.Euler(0f, 180f, 0f));
        }

        /// <summary>
        /// Fenêtre sur le mur nord du salon, à gauche du canapé, fermée sous la coupe du mur :
        /// cadre complet des deux côtés du mur et vitre bleutée à deux battants. Sans collider.
        /// </summary>
        private static void Fenetre(GameObject appartement)
        {
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
        }
    }
}
