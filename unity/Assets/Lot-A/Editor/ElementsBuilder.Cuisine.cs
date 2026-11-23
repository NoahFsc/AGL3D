using UnityEditor;
using UnityEngine;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Générateurs de la cuisine : carrelage au sol, crédence, évier, plan de travail et mobilier.
    /// Les meubles viennent de la cuisine modulaire du Furniture Mega Pack (meubles bleu marine
    /// à plan blanc, assortis aux murs bleu-gris). La peinture et les plinthes des murs sont
    /// posées par l'habillage (menu AGL3D > Lot A > Murs).
    ///
    /// La cuisine est alignée contre le mur est, du nord au sud : réfrigérateur, évier
    /// (cuisine.evier), puis plan de travail avec plaque et micro-ondes (cuisine.plan-de-travail),
    /// posés dos au mur est (-90°).
    /// </summary>
    public static partial class ElementsBuilder
    {
        /// <summary>Hauteur du plan de travail, en mètres.</summary>
        private const float HauteurPlan = 0.86f;
        /// <summary>Profondeur des meubles bas.</summary>
        private const float ProfondeurMeubles = 0.56f;
        /// <summary>Espace entre un meuble et le mur peint (peinture et plinthe comprises).</summary>
        private const float JeuMurCuisine = 0.025f;

        /// <summary>
        /// Découpage du mur est, du nord au sud : (début, fin) en z de chaque meuble. Les mêmes
        /// valeurs servent au placement des meubles et à la crédence.
        /// </summary>
        private struct RangeeCuisine
        {
            public float x;                       // dos des meubles, contre le mur est
            public float frigoNord, frigoSud;
            public float evierNord, evierSud;
            public float planNord, planSud;
        }

        private static RangeeCuisine Rangee(Piece piece)
        {
            const float frigo = 0.63f, evier = 0.79f, jeu = 0.01f;
            var r = new RangeeCuisine { x = piece.xMax - JeuMurCuisine };
            r.frigoNord = piece.zMax - JeuMurCuisine;
            r.frigoSud = r.frigoNord - frigo;
            r.evierNord = r.frigoSud - jeu;
            r.evierSud = r.evierNord - evier;
            r.planNord = r.evierSud - jeu;
            r.planSud = piece.zMin + JeuMurCuisine;
            return r;
        }

        [MenuItem("AGL3D/Lot A/Cuisine/Toute la cuisine", priority = 0)]
        public static void CuisineComplete() => Modifier(Cuisine);

        private static void Cuisine(GameObject appartement)
        {
            SolCuisine(appartement);
            EvierCuisine(appartement);
            PlanDeTravailCuisine(appartement);
            MobilierCuisine(appartement);
            VueCuisine(appartement);
        }

        [MenuItem("AGL3D/Lot A/Cuisine/Vue de la caméra")] public static void VueCuisine() => Modifier(VueCuisine);
        [MenuItem("AGL3D/Lot A/Cuisine/Sol (carrelage)")] public static void SolCuisine() => Modifier(SolCuisine);
        [MenuItem("AGL3D/Lot A/Cuisine/Évier")] public static void EvierCuisine() => Modifier(EvierCuisine);
        [MenuItem("AGL3D/Lot A/Cuisine/Plan de travail")] public static void PlanDeTravailCuisine() => Modifier(PlanDeTravailCuisine);
        [MenuItem("AGL3D/Lot A/Cuisine/Mobilier")] public static void MobilierCuisine() => Modifier(MobilierCuisine);

        /// <summary>
        /// Vue de la caméra pour le bouton Cuisine : depuis l'ouest (la porte du salon), face aux
        /// meubles du mur est, avec la table au premier plan.
        /// </summary>
        private static void VueCuisine(GameObject appartement)
        {
            var piece = PieceCuisine(appartement.transform);
            var vue = appartement.transform.Find("Vues/Vue_cuisine");
            vue.position = appartement.transform.TransformPoint(new Vector3(piece.xMax - 1.3f, 0.4f, (piece.zMin + piece.zMax) / 2f));

            var so = new SerializedObject(vue.GetComponent<CameraView>());
            so.FindProperty("distance").floatValue = 4.2f;
            so.FindProperty("yaw").floatValue = 90f;
            so.FindProperty("pitch").floatValue = 55f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /* ------------------------------- Sol de la cuisine ------------------------------- */

        /// <summary>
        /// Carrelage de 30 cm en damier doux (deux tons de grès clair) sur un fond couleur joint :
        /// les carreaux, un peu plus petits que la grille, laissent voir les joints. La cuisine
        /// n'a pas d'élément « sol » : le carrelage est du décor, sans collider.
        /// Avec la crédence, petits carreaux blancs posés en quinconce sur le mur est, au-dessus
        /// de l'évier et du plan de travail, sous la coupe des murs.
        /// </summary>
        private static void SolCuisine(GameObject appartement)
        {
            var piece = PieceCuisine(appartement.transform);

            var carreaux = new[]
            {
                Materiau("CarrelageCuisine_Clair", new Color(0.90f, 0.88f, 0.84f), 0.35f),
                Materiau("CarrelageCuisine_Fonce", new Color(0.80f, 0.77f, 0.72f), 0.35f),
            };
            var joint = Materiau("CarrelageCuisine_Joint", new Color(0.62f, 0.61f, 0.58f), 0.05f);

            // Repère de la racine = repère de l'appartement.
            var root = new GameObject("SolCuisine");
            const float carreau = 0.3f, epaisseur = 0.006f;
            Boite("Joints", root.transform, new Vector3(piece.xMin, 0f, piece.zMin), new Vector3(piece.xMax, 0.004f, piece.zMax), joint);
            // Rangées le long de x, carreaux le long de z, en damier.
            Surface("Carrelage", root.transform, "Decor/Cuisine", new Calepinage
            {
                u0 = piece.zMin, u1 = piece.zMax, v0 = piece.xMin, v1 = piece.xMax,
                rangee = carreau, joint = 0.002f, longueurs = new[] { carreau }, decalage = 0, pasTeinte = 1,
                premiere = (u, r) => u + carreau,
            }, carreaux, (z0, z1, x0, x1) => (new Vector3(x0, 0f, z0), new Vector3(x1, epaisseur, z1)));

            // Crédence : rangées de carreaux de 15 × 7,5 cm, décalées d'un demi-carreau une rangée sur deux.
            var rangee = Rangee(piece);
            var blanc = new[]
            {
                Materiau("Credence_Carreau", new Color(0.95f, 0.95f, 0.93f), 0.6f),
                Materiau("Credence_Carreau_Nuance", new Color(0.91f, 0.92f, 0.91f), 0.6f),
            };
            var jointCredence = Materiau("Credence_Joint", new Color(0.78f, 0.78f, 0.76f), 0.05f);
            const float peinture = 0.008f, lCarreau = 0.15f, hCarreau = 0.075f, ep = 0.006f;
            float xMur = piece.xMax - peinture;
            float bas = HauteurPlan, haut = HauteurMurs - 0.04f;
            Boite("Credence_Joints", root.transform, new Vector3(xMur - 0.003f, bas, rangee.planSud), new Vector3(xMur, haut, rangee.evierNord), jointCredence);
            Surface("Credence", root.transform, "Decor/Cuisine", new Calepinage
            {
                u0 = rangee.planSud, u1 = rangee.evierNord, v0 = bas, v1 = haut,
                rangee = hCarreau, joint = 0.0015f, longueurs = new[] { lCarreau }, decalage = 0, pasTeinte = 1,
                premiere = (u, r) => u - (r % 2) * lCarreau / 2f + lCarreau,
            }, blanc, (z0, z1, y0, y1) => (new Vector3(xMur - 0.003f - ep, y0, z0), new Vector3(xMur - 0.003f, y1, z1)));

            var prefab = Enregistrer(root, "Decor/Cuisine/SolCuisine", "SolCuisine");
            PlacerDecor(appartement, "Cuisine_Sol", prefab, Vector3.zero, Quaternion.identity);
        }

        /* ------------------------------------ Évier ------------------------------------ */

        /// <summary>
        /// Meuble évier à deux portes (Mega Pack), avec le bac en inox et le robinet du modèle
        /// Sink03 encastrés dans son plan. Élément cuisine.evier, entre le réfrigérateur et le
        /// plan de travail.
        /// </summary>
        private static void EvierCuisine(GameObject appartement)
        {
            var rangee = Rangee(PieceCuisine(appartement.transform));

            var root = new GameObject("Evier");
            var meuble = InstanceMega("Kitchen/CabinetA_Sink", root.transform);
            Ajuster(meuble, root.transform, largeur: rangee.evierNord - rangee.evierSud, hauteur: HauteurPlan, profondeur: ProfondeurMeubles);

            // Bac et robinet : leur origine est au niveau du rebord du bac.
            var lm = LimitesLocales(root.transform, root.transform);
            var bac = InstanceMega("Kitchen/Sink03", root.transform);
            bac.localScale = Vector3.one * (HauteurPlan / 2.02f);
            var lb = LimitesLocales(bac, root.transform);
            bac.localPosition = new Vector3(-lb.center.x, lm.max.y + 0.002f, lm.center.z - lb.center.z + 0.02f);

            AjouterCollider(root);
            Element(appartement, root, "cuisine.evier",
                new Vector3(rangee.x, 0f, (rangee.evierNord + rangee.evierSud) / 2f), Quaternion.Euler(0f, -90f, 0f));
        }

        /* ------------------------------- Plan de travail ------------------------------- */

        /// <summary>
        /// Deux meubles bas (une porte et un tiroir, puis trois tiroirs) dans l'angle sud-est,
        /// avec la plaque de cuisson et le micro-ondes posés dessus. Élément
        /// cuisine.plan-de-travail.
        /// </summary>
        private static void PlanDeTravailCuisine(GameObject appartement)
        {
            var rangee = Rangee(PieceCuisine(appartement.transform));
            float largeur = rangee.planNord - rangee.planSud;
            float demi = largeur / 2f;

            var root = new GameObject("PlanDeTravail");
            // x local = le long du mur, vers le nord une fois posé : le meuble à porte est côté
            // évier, celui à tiroirs dans l'angle.
            var porte = MeubleBas("CabinetA04", root.transform, demi, demi / 2f);
            MeubleBas("CabinetA03", root.transform, demi, -demi / 2f);

            float dessus = LimitesLocales(porte, root.transform).max.y;
            float milieu = ProfondeurMeubles / 2f;

            // Plaque de cuisson sur le meuble à porte.
            var plaque = InstanceMega("Kitchen/GasStove04", root.transform);
            MettreALargeur(plaque, root.transform, 0.36f);
            var lp = LimitesLocales(plaque, root.transform);
            plaque.localPosition += new Vector3(demi / 2f - lp.center.x, dessus - lp.min.y, milieu - lp.center.z);

            // Micro-ondes sur le meuble à tiroirs, contre la crédence.
            var micro = InstanceMega("Kitchen/MicrowaveOven04", root.transform);
            MettreALargeur(micro, root.transform, 0.4f);
            var lmo = LimitesLocales(micro, root.transform);
            micro.localPosition += new Vector3(-demi / 2f - lmo.center.x, dessus - lmo.min.y, 0.03f - lmo.min.z);

            AjouterCollider(root);
            Element(appartement, root, "cuisine.plan-de-travail",
                new Vector3(rangee.x, 0f, (rangee.planNord + rangee.planSud) / 2f), Quaternion.Euler(0f, -90f, 0f));
        }

        /// <summary>Meuble bas du Mega Pack, mis à la largeur voulue et centré en <paramref name="x"/>.</summary>
        private static Transform MeubleBas(string modele, Transform root, float largeur, float x)
        {
            var m = InstanceMega("Kitchen/" + modele, root);
            Ajuster(m, root, largeur: largeur - 0.004f, hauteur: HauteurPlan, profondeur: ProfondeurMeubles);
            // Ajuster a emballé le modèle dans un groupe : c'est le groupe qu'on décale.
            var groupe = m.parent;
            groupe.localPosition += new Vector3(x, 0f, 0f);
            return groupe;
        }

        /* ------------------------------ Mobilier de décor ------------------------------ */

        /// <summary>
        /// Réfrigérateur dans l'angle nord-est, table carrée et deux chaises au milieu de la
        /// pièce, hors du passage de la porte, avec une corbeille de fruits.
        /// </summary>
        private static void MobilierCuisine(GameObject appartement)
        {
            var piece = PieceCuisine(appartement.transform);
            var rangee = Rangee(piece);

            // Réfrigérateur blanc, sous la coupe des murs.
            var frigo = Decor("Cuisine/Refrigerateur", root =>
            {
                var m = InstanceMega("Kitchen/Refrigerator07", root);
                Ajuster(m, root, largeur: rangee.frigoNord - rangee.frigoSud, hauteur: HauteurMurs - 0.08f, profondeur: 0.6f);
            });
            PlacerDecor(appartement, "Cuisine_Refrigerateur", frigo,
                new Vector3(rangee.x, 0f, (rangee.frigoNord + rangee.frigoSud) / 2f), Quaternion.Euler(0f, -90f, 0f));

            // Table carrée de 75 cm et deux chaises, de part et d'autre (nord et sud).
            var table = Decor("Cuisine/Table", root =>
            {
                var t = InstanceMega("Tables/Table02", root, lacet: null);
                MettreAHauteur(t, root, 0.74f);
                Centrer(t, root);
                float dessus = LimitesLocales(t, root).max.y;
                foreach (var (nom, signe) in new[] { ("Chaise_Nord", 1f), ("Chaise_Sud", -1f) })
                {
                    // La chaise nord garde l'orientation du modèle (vers -z) : elle regarde la table.
                    var c = InstanceMega("Chairs/Chair01", root, lacet: signe > 0f ? 0f : 180f);
                    c.name = nom;
                    MettreAHauteur(c, root, 0.86f);
                    Centrer(c, root);
                    c.localPosition += new Vector3(0f, 0f, signe * 0.5f);
                }
                var fruits = InstancePack("SM_Fruit_Bowl", root);
                MettreALargeur(fruits, root, 0.26f);
                Centrer(fruits, root);
                fruits.localPosition += new Vector3(0.08f, dessus, -0.05f);
            });
            // Au milieu de la pièce, entre la porte (ouest) et la cuisine (est).
            float xTable = (piece.xMin + rangee.x - ProfondeurMeubles) / 2f + 0.2f;
            float zTable = (piece.zMin + piece.zMax) / 2f;
            PlacerDecor(appartement, "Cuisine_Table", table, new Vector3(xTable, 0f, zTable), Quaternion.identity);
        }
    }
}
