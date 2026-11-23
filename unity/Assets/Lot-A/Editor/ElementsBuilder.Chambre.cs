using System.Collections.Generic;
using AGL3D.Bridge;
using UnityEditor;
using UnityEngine;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Générateurs de la chambre : sol (parquet et tapis), murs (peinture et plinthes),
    /// placard, et mobilier de décor. Les meubles viennent surtout du pack Low Poly Furniture
    /// (IGS), complété par le Furniture Mega Pack (lit, tables de chevet).
    ///
    /// Convention des prefabs de décor : x le long du mur, y vers le haut, z vers l'intérieur
    /// de la pièce, dos du meuble en z = 0. Rotations pour poser un meuble dos à un mur :
    /// nord 180°, est -90°, sud 0°, ouest 90°.
    /// </summary>
    public static partial class ElementsBuilder
    {
        /// <summary>Intérieur de la chambre, dans le repère de l'appartement.</summary>
        private struct PieceChambre
        {
            public float xMin, xMax, zMin, zMax;
            // Ouverture vers le salon, dans le mur ouest.
            public float zPorteBas, zPorteHaut;
            public Vector3 Centre => new Vector3((xMin + xMax) / 2f, 0f, (zMin + zMax) / 2f);
            public float Largeur => xMax - xMin;
            public float Profondeur => zMax - zMin;
        }

        private static PieceChambre MesurerChambre(Transform t)
        {
            return new PieceChambre
            {
                xMin = Limites(t, t.Find("Murs/Cloison_Salon_Nord")).max.x,
                xMax = Limites(t, t.Find("Murs/Mur_Est")).min.x,
                zMin = Limites(t, t.Find("Murs/Cloison_Chambre_Cuisine")).max.z,
                zMax = Limites(t, t.Find("Murs/Mur_Nord")).min.z,
                zPorteBas = Limites(t, t.Find("Murs/Cloison_Salon_Milieu")).max.z,
                zPorteHaut = Limites(t, t.Find("Murs/Cloison_Salon_Nord")).min.z,
            };
        }

        [MenuItem("AGL3D/Lot A/Chambre/Toute la chambre", priority = 0)]
        public static void ChambreComplete()
        {
            SolChambre();
            MurChambre();
            PlacardChambre();
            MobilierChambre();
        }

        /* ------------------------------- Sol de la chambre ------------------------------- */

        /// <summary>
        /// Même principe que le sol du salon : un objet sans rendu porte la clé et un collider
        /// fin sur toute la pièce, avec le parquet et le tapis en enfants. Le parquet de la
        /// chambre est en chêne plus clair, lames posées dans l'autre sens (le long de x).
        /// </summary>
        [MenuItem("AGL3D/Lot A/Chambre/Sol")]
        public static void SolChambre()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var piece = MesurerChambre(appartement.transform);
            var centre = piece.Centre;
            float largeur = piece.Largeur, profondeur = piece.Profondeur;

            var bois = new[]
            {
                Materiau("ParquetChambre_Clair", new Color(0.84f, 0.71f, 0.54f)),
                Materiau("ParquetChambre_Moyen", new Color(0.80f, 0.67f, 0.50f)),
                Materiau("ParquetChambre_Fonce", new Color(0.75f, 0.62f, 0.46f)),
            };
            var laine = Materiau("TapisChambre_Fond", new Color(0.88f, 0.86f, 0.81f));
            var bande = Materiau("TapisChambre_Bande", new Color(0.36f, 0.45f, 0.42f));

            var root = new GameObject("SolChambre");

            // Lames de 12 cm le long de x, joints décalés d'une rangée à l'autre.
            const float lame = 0.12f, joint = 0.001f, epaisseur = 0.006f;
            float[] longueurs = { 1.3f, 1.0f, 1.6f, 1.15f, 1.45f };
            var lames = new List<(Vector3, Vector3, int)>();
            int rangee = 0;
            for (float z = -profondeur / 2f; z < profondeur / 2f - 0.01f; z += lame, rangee++)
            {
                float z1 = Mathf.Min(z + lame, profondeur / 2f);
                float x = -largeur / 2f;
                int i = rangee * 2;
                float fin = x + 0.25f + longueurs[rangee % longueurs.Length] * ((rangee * 0.41f) % 1f);
                while (x < largeur / 2f - 0.01f)
                {
                    fin = Mathf.Min(fin, largeur / 2f);
                    lames.Add((new Vector3(x + joint, 0f, z + joint), new Vector3(fin - joint, epaisseur, z1 - joint), (rangee * 5 + i) % bois.Length));
                    x = fin;
                    i++;
                    fin = x + longueurs[i % longueurs.Length];
                }
            }
            var parquet = new GameObject("Parquet");
            parquet.transform.SetParent(root.transform, false);
            parquet.AddComponent<MeshFilter>().sharedMesh = Lames("SolChambre", "Parquet", lames, bois.Length);
            parquet.AddComponent<MeshRenderer>().sharedMaterials = bois;

            // Tapis écru à deux bandes vertes, au pied du lit, entre le bureau et l'armoire.
            var tTapis = Groupe("Tapis", root.transform);
            tTapis.localPosition = new Vector3(-0.55f, epaisseur, 0f);
            const float tl = 1.1f, tp = 1.3f;
            Boite("Fond", tTapis, new Vector3(-tl / 2f, 0f, -tp / 2f), new Vector3(tl / 2f, 0.01f, tp / 2f), laine);
            foreach (float zb in new[] { -tp / 2f + 0.12f, tp / 2f - 0.2f })
                Boite(zb < 0 ? "Bande_Sud" : "Bande_Nord", tTapis, new Vector3(-tl / 2f + 0.06f, 0.01f, zb), new Vector3(tl / 2f - 0.06f, 0.013f, zb + 0.08f), bande);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, epaisseur / 2f, 0f);
            collider.size = new Vector3(largeur, epaisseur + 0.004f, profondeur);
            Cle(root.AddComponent<InspectableElement>(), "chambre.sol");

            var prefab = Enregistrer(root, "SolChambre");
            Remplacer(appartement, "chambre", "chambre.sol", prefab, centre, Quaternion.identity);
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------- Murs de la chambre ------------------------------- */

        /// <summary>
        /// L'élément « mur de la chambre » est la peinture des quatre faces intérieures : des
        /// panneaux fins posés contre les murs, avec des plinthes blanches. Au survol, toute la
        /// peinture de la chambre s'illumine, sans toucher les murs partagés avec le salon et la
        /// cuisine. Le premier collider (racine) est celui du mur nord, où sont le tableau et la
        /// prise : le repère et le focus caméra s'y placent.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Chambre/Murs")]
        public static void MurChambre()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var piece = MesurerChambre(appartement.transform);
            var tableauExistant = appartement.transform.Find("Elements/chambre/chambre.mur/Decor");
            float xDecor = tableauExistant != null
                ? appartement.transform.InverseTransformPoint(tableauExistant.position).x
                : PositionActuelle(appartement, "chambre", "chambre.mur").x;

            var peinture = Materiau("MurChambre_Peinture", new Color(0.72f, 0.78f, 0.71f), 0.05f);
            // Mur d'accent derrière la tête de lit (est) : vert sapin, de la même famille que le sauge.
            var accent = Materiau("MurChambre_Accent", new Color(0.29f, 0.42f, 0.38f), 0.05f);
            var plinthe = Materiau("MurChambre_Plinthe", new Color(0.90f, 0.90f, 0.87f), 0.1f);
            var plaque = Materiau("Prise_Plaque", new Color(0.86f, 0.86f, 0.84f), 0.02f);
            var trou = Materiau("Prise_Trou", new Color(0.22f, 0.22f, 0.23f));

            // Repère de la racine = repère de l'appartement : les panneaux suivent les murs.
            var root = new GameObject("MurChambre");
            const float e = 0.008f;                 // épaisseur de la peinture
            const float hp = 0.07f, ep = 0.012f;    // hauteur et épaisseur des plinthes
            float h = HauteurMurs;
            float x0 = piece.xMin, x1 = piece.xMax, z0 = piece.zMin, z1 = piece.zMax;

            // (nom, min, max, plinthe min, plinthe max) ; nord et sud couvrent les coins.
            var panneaux = new List<(string, Vector3, Vector3, Vector3, Vector3)>
            {
                ("Nord", new Vector3(x0, 0f, z1 - e), new Vector3(x1, h, z1), new Vector3(x0, 0f, z1 - e - ep), new Vector3(x1, hp, z1 - e)),
                ("Est", new Vector3(x1 - e, 0f, z0 + e), new Vector3(x1, h, z1 - e), new Vector3(x1 - e - ep, 0f, z0 + e + ep), new Vector3(x1 - e, hp, z1 - e - ep)),
                ("Sud", new Vector3(x0, 0f, z0), new Vector3(x1, h, z0 + e), new Vector3(x0, 0f, z0 + e), new Vector3(x1, hp, z0 + e + ep)),
                ("Ouest_Sud", new Vector3(x0, 0f, z0 + e), new Vector3(x0 + e, h, piece.zPorteBas), new Vector3(x0 + e, 0f, z0 + e + ep), new Vector3(x0 + e + ep, hp, piece.zPorteBas)),
                ("Ouest_Nord", new Vector3(x0, 0f, piece.zPorteHaut), new Vector3(x0 + e, h, z1 - e), new Vector3(x0 + e, 0f, piece.zPorteHaut), new Vector3(x0 + e + ep, hp, z1 - e - ep)),
            };

            Transform nord = null;
            foreach (var (nom, min, max, pMin, pMax) in panneaux)
            {
                var panneau = Boite("Peinture_" + nom, root.transform, min, max, nom == "Est" ? accent : peinture);
                Boite("Plinthe_" + nom, root.transform, pMin, pMax, plinthe);
                if (nom == "Nord") { nord = panneau; continue; }
                // Les autres panneaux se cliquent aussi (collider enfant, remonté au parent).
                panneau.gameObject.AddComponent<BoxCollider>();
            }

            // Collider de la racine sur la peinture du mur nord.
            var collider = root.AddComponent<BoxCollider>();
            collider.center = nord.localPosition;
            collider.size = new Vector3(x1 - x0, h, e);

            // Tableau (pack Low Poly Furniture) et prise sur le mur nord, côté lit.
            var tDecor = Groupe("Decor", root.transform);
            tDecor.localPosition = new Vector3(xDecor, 0f, z1 - e);
            tDecor.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tTableau = InstancePack("SM_picture_3", tDecor);
            tTableau.name = "Tableau";
            tTableau.localScale = Vector3.one * (0.42f / LimitesLocales(tTableau, tDecor).size.y);
            var lt = LimitesLocales(tTableau, tDecor);
            tTableau.localPosition += new Vector3(-lt.center.x, 0.74f - lt.center.y, -lt.min.z);

            var tPrise = Groupe("Prise", tDecor);
            const float xPrise = 0.45f, yPrise = 0.22f;
            Boite("Plaque", tPrise, new Vector3(xPrise - 0.04f, yPrise - 0.04f, 0f), new Vector3(xPrise + 0.04f, yPrise + 0.04f, 0.012f), plaque);
            Cylindre("Socle", tPrise, new Vector3(xPrise, yPrise, 0.012f), new Vector3(xPrise, yPrise, 0.016f), 0.028f, 8, plaque);
            foreach (float dx in new[] { -0.01f, 0.01f })
                Boite(dx < 0 ? "Trou_Gauche" : "Trou_Droit", tPrise, new Vector3(xPrise + dx - 0.003f, yPrise - 0.003f, 0.016f), new Vector3(xPrise + dx + 0.003f, yPrise + 0.003f, 0.0185f), trou);

            Cle(root.AddComponent<InspectableElement>(), "chambre.mur");

            var prefab = Enregistrer(root, "MurChambre");
            Remplacer(appartement, "chambre", "chambre.mur", prefab, Vector3.zero, Quaternion.identity);
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* ---------------------------------- Placard ---------------------------------- */

        /// <summary>
        /// Armoire du pack Low Poly Furniture contre le mur nord, à l'emplacement du plan.
        /// Comme la porte d'entrée, elle s'arrête sous la coupe des murs : seule la hauteur est
        /// réduite, pour garder une largeur et une profondeur d'armoire.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Chambre/Placard")]
        public static void PlacardChambre()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var piece = MesurerChambre(appartement.transform);
            var plan = PositionActuelle(appartement, "chambre", "chambre.placard");

            var root = new GameObject("Placard");
            var modele = InstancePack("SM_Wardrobe_9", root.transform);
            Ajuster(modele, root.transform, largeur: 1.4f, hauteur: HauteurMurs - 0.05f, profondeur: 0.45f);

            var limites = LimitesLocales(root.transform, root.transform);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = limites.center;
            collider.size = limites.size;
            Cle(root.AddComponent<InspectableElement>(), "chambre.placard");

            var prefab = Enregistrer(root, "Placard");
            // Dos au mur nord, à 1 cm de la peinture.
            Remplacer(appartement, "chambre", "chambre.placard", prefab, new Vector3(plan.x, 0f, piece.zMax - 0.02f), Quaternion.Euler(0f, 180f, 0f));
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------ Mobilier de décor ------------------------------ */

        /// <summary>
        /// Lit double tête contre le mur est, deux tables de chevet avec lampe, bureau et chaise
        /// contre la cloison de la cuisine, avec un ordinateur et une plante.
        /// Décor : pas de clé, mais un collider pour qu'un clic dessus compte comme un clic dans le vide.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Chambre/Mobilier")]
        public static void MobilierChambre()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var piece = MesurerChambre(appartement.transform);
            const float jeu = 0.02f;   // espace entre un meuble et la peinture du mur

            // Lit (Furniture Mega Pack) : tête de lit en z = 0, pied vers +z.
            var lit = Decor("Lit", root =>
            {
                var m = InstancePrefab($"{MegaPackDir}/Beds/Bed02.prefab", "Furniture Mega Pack - Free", root);
                m.localRotation = Quaternion.Euler(0f, 180f, 0f);
                m.localScale = Vector3.one * (1.6f / LimitesLocales(m, root).size.x);
                Poser(m, root);
            });
            float zLit = piece.Centre.z;
            PlacerDecor(appartement, "Chambre_Lit", lit, new Vector3(piece.xMax - jeu, 0f, zLit), Quaternion.Euler(0f, -90f, 0f));
            const float demiLit = 0.8f;

            // Tables de chevet (Mega Pack) avec une lampe du pack Low Poly Furniture.
            var chevet = Decor("Chevet", root =>
            {
                var m = InstancePrefab($"{MegaPackDir}/Drawers/Drawer06.prefab", "Furniture Mega Pack - Free", root);
                m.localRotation = Quaternion.Euler(0f, 180f, 0f);
                m.localScale = Vector3.one * (0.36f / LimitesLocales(m, root).size.x);
                Poser(m, root);
                float dessus = LimitesLocales(m, root).max.y;
                var lampe = InstancePack("SM_lamp_5", root);
                lampe.localScale = Vector3.one * (0.4f / LimitesLocales(lampe, root).size.y);
                Centrer(lampe, root);
                var ll = LimitesLocales(lampe, root);
                lampe.localPosition += new Vector3(0f, dessus, LimitesLocales(m, root).center.z - ll.center.z);
            });
            const float demiChevet = 0.18f;
            foreach (var (nom, signe) in new[] { ("Chambre_Chevet_Nord", 1f), ("Chambre_Chevet_Sud", -1f) })
            {
                float z = zLit + signe * (demiLit + 0.01f + demiChevet);
                PlacerDecor(appartement, nom, chevet, new Vector3(piece.xMax - jeu, 0f, z), Quaternion.Euler(0f, -90f, 0f));
            }

            // Bureau et chaise (pack Low Poly Furniture) contre la cloison de la cuisine.
            var bureau = Decor("Bureau", root =>
            {
                var table = InstancePack("SM_Computer_desk_7", root);
                Ajuster(table, root, largeur: 1.1f, hauteur: 0.75f, profondeur: 0.55f);
                var chaise = InstancePack("SM_Chair_13", root);
                chaise.localRotation = Quaternion.Euler(0f, 180f, 0f);
                chaise.localScale = Vector3.one * (0.88f / LimitesLocales(chaise, root).size.y);
                Centrer(chaise, root);
                var lc = LimitesLocales(chaise, root);
                // La chaise est à moitié glissée sous le bureau.
                chaise.localPosition += new Vector3(0.15f, 0f, 0.42f - lc.min.z);
                // Plante en pot au bout du bureau.
                float dessus = LimitesLocales(table, root).max.y;
                var plante = InstancePack("SM_Pots_of_plant_9", root);
                plante.localScale = Vector3.one * 0.7f;
                Centrer(plante, root);
                plante.localPosition += new Vector3(-0.4f, dessus, 0.2f);
                Ordinateur(root, new Vector3(0.1f, dessus, 0f));
            });
            PlacerDecor(appartement, "Chambre_Bureau", bureau, new Vector3(piece.xMin + 1.15f, 0f, piece.zMin + jeu), Quaternion.identity);

            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Ordinateur en boîtes (aucun des deux packs n'en a) : écran sur pied, clavier et
        /// souris. <paramref name="origine"/> est le milieu du bord arrière du plateau ; l'écran
        /// regarde +z. Le haut de l'écran reste sous la coupe des murs.
        /// </summary>
        private static void Ordinateur(Transform parent, Vector3 origine)
        {
            var boitier = Materiau("Ordi_Boitier", new Color(0.14f, 0.14f, 0.15f), 0.3f);
            var dalle = Materiau("Ordi_Ecran", new Color(0.10f, 0.15f, 0.21f), 0.6f);
            var touches = Materiau("Ordi_Clavier", new Color(0.84f, 0.84f, 0.82f), 0.2f);

            var o = Groupe("Ordinateur", parent);
            o.localPosition = origine;
            // Pied : socle plat et colonne.
            Boite("Socle", o, new Vector3(-0.1f, 0f, 0.06f), new Vector3(0.1f, 0.012f, 0.2f), boitier);
            Boite("Colonne", o, new Vector3(-0.025f, 0.012f, 0.09f), new Vector3(0.025f, 0.16f, 0.12f), boitier);
            // Écran : coque sombre et dalle légèrement en avant.
            const float bas = 0.11f, haut = 0.4f, demi = 0.27f;
            Boite("Coque", o, new Vector3(-demi, bas, 0.12f), new Vector3(demi, haut, 0.145f), boitier);
            Boite("Dalle", o, new Vector3(-demi + 0.012f, bas + 0.012f, 0.145f), new Vector3(demi - 0.012f, haut - 0.012f, 0.149f), dalle);
            // Clavier et souris devant l'écran.
            Boite("Clavier", o, new Vector3(-0.21f, 0f, 0.27f), new Vector3(0.21f, 0.02f, 0.4f), touches);
            Boite("Souris", o, new Vector3(0.29f, 0f, 0.3f), new Vector3(0.35f, 0.022f, 0.39f), touches);
        }

        /* --------------------------------- Outils --------------------------------- */

        /// <summary>Construit un prefab de décor de la chambre, avec un collider par pièce du meuble.</summary>
        private static GameObject Decor(string nom, System.Action<Transform> construire)
        {
            var root = new GameObject(nom);
            construire(root.transform);
            // Un collider par pièce (table, chaise, lampe...) plutôt qu'une boîte englobante :
            // le vide entre les meubles laisse passer le clic jusqu'au sol de la chambre.
            foreach (Transform piece in root.transform)
            {
                var limites = LimitesLocales(piece, root.transform);
                var collider = root.AddComponent<BoxCollider>();
                collider.center = limites.center;
                collider.size = limites.size;
            }
            return Enregistrer(root, "Decor/Chambre/" + nom, nom);
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

        /// <summary>Centre le modèle en x, le pose au sol, dos en z = 0.</summary>
        private static void Poser(Transform modele, Transform repere)
        {
            var l = LimitesLocales(modele, repere);
            modele.localPosition += new Vector3(-l.center.x, -l.min.y, -l.min.z);
        }
    }
}
