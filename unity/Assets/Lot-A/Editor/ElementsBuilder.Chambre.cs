using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Générateurs de la chambre : sol (parquet et tapis), murs (peinture et plinthes),
    /// placard, et mobilier de décor. Les meubles viennent surtout du pack Low Poly Furniture
    /// (IGS), complété par le Furniture Mega Pack (lit, tables de chevet).
    /// </summary>
    public static partial class ElementsBuilder
    {
        [MenuItem("AGL3D/Lot A/Chambre/Toute la chambre", priority = 0)]
        public static void ChambreComplete() => Modifier(Chambre);

        private static void Chambre(GameObject appartement)
        {
            SolChambre(appartement);
            MurChambre(appartement);
            PlacardChambre(appartement);
            MobilierChambre(appartement);
        }

        [MenuItem("AGL3D/Lot A/Chambre/Sol")] public static void SolChambre() => Modifier(SolChambre);
        [MenuItem("AGL3D/Lot A/Chambre/Murs")] public static void MurChambre() => Modifier(MurChambre);
        [MenuItem("AGL3D/Lot A/Chambre/Placard")] public static void PlacardChambre() => Modifier(PlacardChambre);
        [MenuItem("AGL3D/Lot A/Chambre/Mobilier")] public static void MobilierChambre() => Modifier(MobilierChambre);

        /* ------------------------------- Sol de la chambre ------------------------------- */

        /// <summary>
        /// Même principe que le sol du salon : un objet sans rendu porte la clé et un collider
        /// fin sur toute la pièce, avec le parquet et le tapis en enfants. Le parquet de la
        /// chambre est en chêne plus clair, lames posées dans l'autre sens (le long de x).
        /// </summary>
        private static void SolChambre(GameObject appartement)
        {
            var piece = PieceChambre(appartement.transform);
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
            const float epaisseur = 0.006f;
            float[] longueurs = { 1.3f, 1.0f, 1.6f, 1.15f, 1.45f };
            Surface("Parquet", root.transform, "SolChambre", new Calepinage
            {
                u0 = -largeur / 2f, u1 = largeur / 2f, v0 = -profondeur / 2f, v1 = profondeur / 2f,
                rangee = 0.12f, joint = 0.001f, longueurs = longueurs, decalage = 2, pasTeinte = 5,
                premiere = (u, r) => u + 0.25f + longueurs[r % longueurs.Length] * ((r * 0.41f) % 1f),
            }, bois, (x0, x1, z0, z1) => (new Vector3(x0, 0f, z0), new Vector3(x1, epaisseur, z1)));

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

            Element(appartement, root, "chambre.sol", piece.Centre, Quaternion.identity);
        }

        /* ------------------------------- Murs de la chambre ------------------------------- */

        /// <summary>
        /// L'élément « mur de la chambre » est la peinture des quatre faces intérieures : des
        /// panneaux fins posés contre les murs, avec des plinthes blanches. Au survol, toute la
        /// peinture de la chambre s'illumine, sans toucher les murs partagés avec le salon et la
        /// cuisine. Le premier collider (racine) est celui du mur nord, où sont le tableau et la
        /// prise : le repère et le focus caméra s'y placent.
        /// </summary>
        private static void MurChambre(GameObject appartement)
        {
            var piece = PieceChambre(appartement.transform);
            var tableauExistant = appartement.transform.Find("Elements/chambre/chambre.mur/Decor");
            float xDecor = tableauExistant != null
                ? appartement.transform.InverseTransformPoint(tableauExistant.position).x
                : PositionActuelle(appartement, "chambre", "chambre.mur").x;

            var peinture = Materiau("MurChambre_Peinture", new Color(0.72f, 0.78f, 0.71f), 0.05f);
            // Mur d'accent derrière la tête de lit (est) : vert sapin, de la même famille que le sauge.
            var accent = Materiau("MurChambre_Accent", new Color(0.29f, 0.42f, 0.38f), 0.05f);
            var plinthe = Materiau("MurChambre_Plinthe", new Color(0.90f, 0.90f, 0.87f), 0.1f);

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
            Tableau(tDecor, 0.42f, 0.74f);
            Prise(tDecor, 0.45f, 0.22f);

            Element(appartement, root, "chambre.mur", Vector3.zero, Quaternion.identity);
        }

        /* ---------------------------------- Placard ---------------------------------- */

        /// <summary>
        /// Armoire du pack Low Poly Furniture contre le mur nord, à l'emplacement du plan.
        /// Comme la porte d'entrée, elle s'arrête sous la coupe des murs : seule la hauteur est
        /// réduite, pour garder une largeur et une profondeur d'armoire.
        /// </summary>
        private static void PlacardChambre(GameObject appartement)
        {
            var piece = PieceChambre(appartement.transform);
            var plan = PositionActuelle(appartement, "chambre", "chambre.placard");

            var root = new GameObject("Placard");
            var modele = InstancePack("SM_Wardrobe_9", root.transform);
            Ajuster(modele, root.transform, largeur: 1.4f, hauteur: HauteurMurs - 0.05f, profondeur: 0.45f);
            AjouterCollider(root);

            // Dos au mur nord, à 1 cm de la peinture.
            Element(appartement, root, "chambre.placard", new Vector3(plan.x, 0f, piece.zMax - 0.02f), Quaternion.Euler(0f, 180f, 0f));
        }

        /* ------------------------------ Mobilier de décor ------------------------------ */

        /// <summary>
        /// Lit double tête contre le mur est, deux tables de chevet avec lampe, bureau et chaise
        /// contre la cloison de la cuisine, avec un ordinateur et une plante.
        /// </summary>
        private static void MobilierChambre(GameObject appartement)
        {
            var piece = PieceChambre(appartement.transform);
            const float jeu = 0.02f;   // espace entre un meuble et la peinture du mur

            // Lit (Furniture Mega Pack) : tête de lit en z = 0, pied vers +z.
            var lit = Decor("Chambre/Lit", root =>
            {
                var m = InstanceMega("Beds/Bed02", root);
                MettreALargeur(m, root, 1.6f);
                Poser(m, root);
            });
            float zLit = piece.Centre.z;
            PlacerDecor(appartement, "Chambre_Lit", lit, new Vector3(piece.xMax - jeu, 0f, zLit), Quaternion.Euler(0f, -90f, 0f));
            const float demiLit = 0.8f;

            // Tables de chevet (Mega Pack) avec une lampe du pack Low Poly Furniture.
            var chevet = Decor("Chambre/Chevet", root =>
            {
                var m = InstanceMega("Drawers/Drawer06", root);
                MettreALargeur(m, root, 0.36f);
                Poser(m, root);
                float dessus = LimitesLocales(m, root).max.y;
                var lampe = InstancePack("SM_lamp_5", root);
                MettreAHauteur(lampe, root, 0.4f);
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
            var bureau = Decor("Chambre/Bureau", root =>
            {
                var table = InstancePack("SM_Computer_desk_7", root);
                Ajuster(table, root, largeur: 1.1f, hauteur: 0.75f, profondeur: 0.55f);
                var chaise = InstancePack("SM_Chair_13", root);
                chaise.localRotation = Quaternion.Euler(0f, 180f, 0f);
                MettreAHauteur(chaise, root, 0.88f);
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
    }
}
