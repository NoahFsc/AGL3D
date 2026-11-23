using System.Collections.Generic;
using AGL3D.Bridge;
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
    /// (cuisine.evier), puis plan de travail avec plaque et micro-ondes (cuisine.plan-de-travail).
    /// Les modèles du Mega Pack regardent vers -z : ils sont tournés de 180° pour suivre la
    /// convention des prefabs (dos en z = 0, façade vers +z), puis posés dos au mur est (-90°).
    /// </summary>
    public static partial class ElementsBuilder
    {
        private const string MegaKitchenDir = MegaPackDir + "/Kitchen";
        private const string MegaPack = "Furniture Mega Pack - Free";

        /// <summary>Hauteur du plan de travail, en mètres.</summary>
        private const float HauteurPlan = 0.86f;
        /// <summary>Profondeur des meubles bas.</summary>
        private const float ProfondeurMeubles = 0.56f;
        /// <summary>Espace entre un meuble et le mur peint (peinture et plinthe comprises).</summary>
        private const float JeuMurCuisine = 0.025f;

        /// <summary>Intérieur de la cuisine, dans le repère de l'appartement.</summary>
        private struct PieceCuisine
        {
            public float xMin, xMax, zMin, zMax;
            // Ouverture vers le salon, dans la cloison ouest.
            public float zPorteBas, zPorteHaut;
            public float Largeur => xMax - xMin;
            public float Profondeur => zMax - zMin;
        }

        private static PieceCuisine MesurerCuisine(Transform t)
        {
            return new PieceCuisine
            {
                xMin = Limites(t, t.Find("Murs/Cloison_Salon_Sud")).max.x,
                xMax = Limites(t, t.Find("Murs/Mur_Est")).min.x,
                zMin = Limites(t, t.Find("Murs/Mur_Sud_Est")).max.z,
                zMax = Limites(t, t.Find("Murs/Cloison_Chambre_Cuisine")).min.z,
                zPorteBas = Limites(t, t.Find("Murs/Cloison_Salon_Sud")).max.z,
                zPorteHaut = Limites(t, t.Find("Murs/Cloison_Salon_Milieu")).min.z,
            };
        }

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

        private static RangeeCuisine Rangee(PieceCuisine piece)
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
        public static void CuisineComplete()
        {
            SolCuisine();
            EvierCuisine();
            PlanDeTravailCuisine();
            MobilierCuisine();
            VueCuisine();
        }

        /// <summary>
        /// Vue de la caméra pour le bouton Cuisine : depuis l'ouest (la porte du salon), face aux
        /// meubles du mur est, avec la table au premier plan.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Cuisine/Vue de la caméra")]
        public static void VueCuisine()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var piece = MesurerCuisine(appartement.transform);
            var vue = appartement.transform.Find("Vues/Vue_cuisine");
            vue.position = appartement.transform.TransformPoint(new Vector3(piece.xMax - 1.3f, 0.4f, (piece.zMin + piece.zMax) / 2f));

            var so = new SerializedObject(vue.GetComponent<CameraView>());
            so.FindProperty("distance").floatValue = 4.2f;
            so.FindProperty("yaw").floatValue = 90f;
            so.FindProperty("pitch").floatValue = 55f;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------- Sol de la cuisine ------------------------------- */

        /// <summary>
        /// Carrelage de 30 cm en damier doux (deux tons de grès clair) sur un fond couleur joint :
        /// les carreaux, un peu plus petits que la grille, laissent voir les joints. La cuisine
        /// n'a pas d'élément « sol » : le carrelage est du décor, sans collider.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Cuisine/Sol (carrelage)")]
        public static void SolCuisine()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var piece = MesurerCuisine(appartement.transform);

            var carreaux = new[]
            {
                Materiau("CarrelageCuisine_Clair", new Color(0.90f, 0.88f, 0.84f), 0.35f),
                Materiau("CarrelageCuisine_Fonce", new Color(0.80f, 0.77f, 0.72f), 0.35f),
            };
            var joint = Materiau("CarrelageCuisine_Joint", new Color(0.62f, 0.61f, 0.58f), 0.05f);

            // Repère de la racine = repère de l'appartement.
            var root = new GameObject("SolCuisine");
            const float carreau = 0.3f, demiJoint = 0.002f, epaisseur = 0.006f;
            Boite("Joints", root.transform, new Vector3(piece.xMin, 0f, piece.zMin), new Vector3(piece.xMax, 0.004f, piece.zMax), joint);

            var dalles = new List<(Vector3, Vector3, int)>();
            int i = 0;
            for (float x = piece.xMin; x < piece.xMax - 0.01f; x += carreau, i++)
            {
                int j = 0;
                for (float z = piece.zMin; z < piece.zMax - 0.01f; z += carreau, j++)
                {
                    float x1 = Mathf.Min(x + carreau, piece.xMax), z1 = Mathf.Min(z + carreau, piece.zMax);
                    dalles.Add((new Vector3(x + demiJoint, 0f, z + demiJoint), new Vector3(x1 - demiJoint, epaisseur, z1 - demiJoint), (i + j) % 2));
                }
            }
            var carrelage = new GameObject("Carrelage");
            carrelage.transform.SetParent(root.transform, false);
            carrelage.AddComponent<MeshFilter>().sharedMesh = Lames("Decor/Cuisine", "Carrelage", dalles, carreaux.Length);
            carrelage.AddComponent<MeshRenderer>().sharedMaterials = carreaux;

            // Crédence : petits carreaux blancs posés en quinconce sur le mur est, au-dessus de
            // l'évier et du plan de travail, sous la coupe des murs.
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
            var faience = new List<(Vector3, Vector3, int)>();
            int rang = 0;
            for (float y = bas; y < haut - 0.005f; y += hCarreau, rang++)
            {
                float y1 = Mathf.Min(y + hCarreau, haut);
                float z = rangee.planSud - (rang % 2) * lCarreau / 2f;
                for (int k = 0; z < rangee.evierNord - 0.005f; z += lCarreau, k++)
                {
                    float z0 = Mathf.Max(z, rangee.planSud), z1 = Mathf.Min(z + lCarreau, rangee.evierNord);
                    if (z1 - z0 < 0.01f) continue;
                    faience.Add((new Vector3(xMur - 0.003f - ep, y + 0.0015f, z0 + 0.0015f), new Vector3(xMur - 0.003f, y1 - 0.0015f, z1 - 0.0015f), (rang + k) % 2));
                }
            }
            var credence = new GameObject("Credence");
            credence.transform.SetParent(root.transform, false);
            credence.AddComponent<MeshFilter>().sharedMesh = Lames("Decor/Cuisine", "Credence", faience, blanc.Length);
            credence.AddComponent<MeshRenderer>().sharedMaterials = blanc;

            var prefab = Enregistrer(root, "Decor/Cuisine/SolCuisine", "SolCuisine");
            PlacerDecor(appartement, "Cuisine_Sol", prefab, Vector3.zero, Quaternion.identity);
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------------ Évier ------------------------------------ */

        /// <summary>
        /// Meuble évier à deux portes (Mega Pack), avec le bac en inox et le robinet du modèle
        /// Sink03 encastrés dans son plan. Élément cuisine.evier, entre le réfrigérateur et le
        /// plan de travail.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Cuisine/Évier")]
        public static void EvierCuisine()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var rangee = Rangee(MesurerCuisine(appartement.transform));

            var root = new GameObject("Evier");
            var meuble = InstancePrefab($"{MegaKitchenDir}/CabinetA_Sink.prefab", MegaPack, root.transform);
            meuble.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Ajuster(meuble, root.transform, largeur: rangee.evierNord - rangee.evierSud, hauteur: HauteurPlan, profondeur: ProfondeurMeubles);

            // Bac et robinet : leur origine est au niveau du rebord du bac.
            var lm = LimitesLocales(root.transform, root.transform);
            var bac = InstancePrefab($"{MegaKitchenDir}/Sink03.prefab", MegaPack, root.transform);
            bac.localRotation = Quaternion.Euler(0f, 180f, 0f);
            bac.localScale = Vector3.one * (HauteurPlan / 2.02f);
            var lb = LimitesLocales(bac, root.transform);
            bac.localPosition = new Vector3(-lb.center.x, lm.max.y + 0.002f, lm.center.z - lb.center.z + 0.02f);

            AjouterCollider(root);
            Cle(root.AddComponent<InspectableElement>(), "cuisine.evier");

            var prefab = Enregistrer(root, "Evier");
            Remplacer(appartement, "cuisine", "cuisine.evier", prefab,
                new Vector3(rangee.x, 0f, (rangee.evierNord + rangee.evierSud) / 2f), Quaternion.Euler(0f, -90f, 0f));
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* ------------------------------- Plan de travail ------------------------------- */

        /// <summary>
        /// Deux meubles bas (une porte et un tiroir, puis trois tiroirs) dans l'angle sud-est,
        /// avec la plaque de cuisson et le micro-ondes posés dessus. Élément
        /// cuisine.plan-de-travail.
        /// </summary>
        [MenuItem("AGL3D/Lot A/Cuisine/Plan de travail")]
        public static void PlanDeTravailCuisine()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var rangee = Rangee(MesurerCuisine(appartement.transform));
            float largeur = rangee.planNord - rangee.planSud;
            float demi = largeur / 2f;

            var root = new GameObject("PlanDeTravail");
            // x local = le long du mur, vers le nord une fois posé : le meuble à porte est côté
            // évier, celui à tiroirs dans l'angle.
            var porte = MeubleBas("CabinetA04", root.transform, demi, demi / 2f);
            var tiroirs = MeubleBas("CabinetA03", root.transform, demi, -demi / 2f);

            float dessus = LimitesLocales(porte, root.transform).max.y;
            float milieu = ProfondeurMeubles / 2f;

            // Plaque de cuisson sur le meuble à porte.
            var plaque = InstancePrefab($"{MegaKitchenDir}/GasStove04.prefab", MegaPack, root.transform);
            plaque.localRotation = Quaternion.Euler(0f, 180f, 0f);
            plaque.localScale = Vector3.one * (0.36f / LimitesLocales(plaque, root.transform).size.x);
            var lp = LimitesLocales(plaque, root.transform);
            plaque.localPosition += new Vector3(demi / 2f - lp.center.x, dessus - lp.min.y, milieu - lp.center.z);

            // Micro-ondes sur le meuble à tiroirs, contre la crédence.
            var micro = InstancePrefab($"{MegaKitchenDir}/MicrowaveOven04.prefab", MegaPack, root.transform);
            micro.localRotation = Quaternion.Euler(0f, 180f, 0f);
            micro.localScale = Vector3.one * (0.4f / LimitesLocales(micro, root.transform).size.x);
            var lmo = LimitesLocales(micro, root.transform);
            micro.localPosition += new Vector3(-demi / 2f - lmo.center.x, dessus - lmo.min.y, 0.03f - lmo.min.z);

            AjouterCollider(root);
            Cle(root.AddComponent<InspectableElement>(), "cuisine.plan-de-travail");

            var prefab = Enregistrer(root, "PlanDeTravail");
            Remplacer(appartement, "cuisine", "cuisine.plan-de-travail", prefab,
                new Vector3(rangee.x, 0f, (rangee.planNord + rangee.planSud) / 2f), Quaternion.Euler(0f, -90f, 0f));
            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Meuble bas du Mega Pack, mis à la largeur voulue et centré en <paramref name="x"/>.</summary>
        private static Transform MeubleBas(string modele, Transform root, float largeur, float x)
        {
            var m = InstancePrefab($"{MegaKitchenDir}/{modele}.prefab", MegaPack, root);
            m.localRotation = Quaternion.Euler(0f, 180f, 0f);
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
        /// Décor : pas de clé, mais un collider par meuble (un clic dessus compte comme un clic
        /// dans le vide).
        /// </summary>
        [MenuItem("AGL3D/Lot A/Cuisine/Mobilier")]
        public static void MobilierCuisine()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var piece = MesurerCuisine(appartement.transform);
            var rangee = Rangee(piece);

            // Réfrigérateur blanc, sous la coupe des murs.
            var frigo = DecorCuisine("Refrigerateur", root =>
            {
                var m = InstancePrefab($"{MegaKitchenDir}/Refrigerator07.prefab", MegaPack, root);
                m.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Ajuster(m, root, largeur: rangee.frigoNord - rangee.frigoSud, hauteur: HauteurMurs - 0.08f, profondeur: 0.6f);
            });
            PlacerDecor(appartement, "Cuisine_Refrigerateur", frigo,
                new Vector3(rangee.x, 0f, (rangee.frigoNord + rangee.frigoSud) / 2f), Quaternion.Euler(0f, -90f, 0f));

            // Table carrée de 75 cm et deux chaises, de part et d'autre (nord et sud).
            var table = DecorCuisine("Table", root =>
            {
                var t = InstancePrefab($"{MegaPackDir}/Tables/Table02.prefab", MegaPack, root);
                t.localScale = Vector3.one * (0.74f / LimitesLocales(t, root).size.y);
                Centrer(t, root);
                float dessus = LimitesLocales(t, root).max.y;
                foreach (var (nom, signe) in new[] { ("Chaise_Nord", 1f), ("Chaise_Sud", -1f) })
                {
                    var c = InstancePrefab($"{MegaPackDir}/Chairs/Chair01.prefab", MegaPack, root);
                    c.name = nom;
                    // Les modèles du Mega Pack regardent vers -z : la chaise nord regarde la table.
                    c.localRotation = Quaternion.Euler(0f, signe > 0f ? 0f : 180f, 0f);
                    c.localScale = Vector3.one * (0.86f / LimitesLocales(c, root).size.y);
                    Centrer(c, root);
                    c.localPosition += new Vector3(0f, 0f, signe * 0.5f);
                }
                var fruits = InstancePack("SM_Fruit_Bowl", root);
                fruits.localScale = Vector3.one * (0.26f / LimitesLocales(fruits, root).size.x);
                Centrer(fruits, root);
                fruits.localPosition += new Vector3(0.08f, dessus, -0.05f);
            });
            // Au milieu de la pièce, entre la porte (ouest) et la cuisine (est).
            float xTable = (piece.xMin + rangee.x - ProfondeurMeubles) / 2f + 0.2f;
            float zTable = (piece.zMin + piece.zMax) / 2f;
            PlacerDecor(appartement, "Cuisine_Table", table, new Vector3(xTable, 0f, zTable), Quaternion.identity);

            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /* --------------------------------- Outils --------------------------------- */

        /// <summary>Construit un prefab de décor de la cuisine, avec un collider par pièce du meuble.</summary>
        private static GameObject DecorCuisine(string nom, System.Action<Transform> construire)
        {
            var root = new GameObject(nom);
            construire(root.transform);
            foreach (Transform piece in root.transform)
            {
                var limites = LimitesLocales(piece, root.transform);
                var collider = root.AddComponent<BoxCollider>();
                collider.center = limites.center;
                collider.size = limites.size;
            }
            return Enregistrer(root, "Decor/Cuisine/" + nom, nom);
        }

        /// <summary>Collider de la racine, sur toute la boîte de l'élément.</summary>
        private static void AjouterCollider(GameObject root)
        {
            var limites = LimitesLocales(root.transform, root.transform);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = limites.center;
            collider.size = limites.size;
        }
    }
}
