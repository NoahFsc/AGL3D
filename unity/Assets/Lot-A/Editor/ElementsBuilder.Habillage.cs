using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AGL3D.LotA.Editor
{
    /// <summary>
    /// Habillage des murs de la maquette, pour qu'ils ne soient plus tout blancs :
    /// - chapeaux anthracite sur la coupe des murs, comme une maquette d'architecte ;
    /// - façade en enduit gris chaud sur les faces extérieures (la fenêtre reste dégagée) ;
    /// - une couleur par pièce : le salon prend la teinte de Mur.mat, la cuisine reçoit des
    ///   panneaux peints ; plinthes blanches dans le salon et la cuisine (la chambre a les
    ///   siennes, dans l'élément chambre.mur).
    /// Tout est rangé dans le groupe Habillage de l'appartement, sans collider : les murs, leurs
    /// colliders et les éléments ne changent pas. Relancer le générateur refait le groupe.
    /// </summary>
    public static partial class ElementsBuilder
    {
        private const string MurMatPath = "Assets/Lot-A/Appartement/Materials/Mur.mat";

        [MenuItem("AGL3D/Lot A/Murs/Habillage (chapeaux, façade, couleurs)")]
        public static void HabillageMurs()
        {
            var appartement = PrefabUtility.LoadPrefabContents(AppartementPath);
            var t = appartement.transform;

            var murs = new List<Bounds>();
            foreach (Transform m in t.Find("Murs"))
                if (m.GetComponent<Collider>() != null) murs.Add(Limites(t, m));

            var ancien = t.Find("Habillage");
            if (ancien != null) Object.DestroyImmediate(ancien.gameObject);
            var habillage = Groupe("Habillage", t);

            var chapeau = Materiau("Mur_Chapeau", new Color(0.23f, 0.24f, 0.26f), 0.1f);
            var facade = Materiau("Mur_Facade", new Color(0.76f, 0.72f, 0.66f), 0.05f);
            var cuisine = Materiau("MurCuisine_Peinture", new Color(0.77f, 0.83f, 0.86f), 0.05f);
            var plinthe = Materiau("MurChambre_Plinthe", new Color(0.90f, 0.90f, 0.87f), 0.1f);

            // Le salon garde le matériau des murs, passé d'un blanc froid à un greige chaud.
            var murMat = AssetDatabase.LoadAssetAtPath<Material>(MurMatPath);
            var greige = new Color(0.91f, 0.87f, 0.81f);
            murMat.SetColor("_BaseColor", greige);
            murMat.SetColor("_Color", greige);
            EditorUtility.SetDirty(murMat);

            const float e = 0.008f;                 // épaisseur des peintures
            // Façade plus fine : le cadre extérieur de la fenêtre (à 4 mm du mur) reste en relief.
            const float eFacade = 0.004f;
            const float hp = 0.07f, ep = 0.012f;    // plinthes
            float h = HauteurMurs;

            // Chapeaux : une plaque sombre sur le dessus de chaque mur, qui déborde de
            // l'épaisseur d'un panneau pour couvrir aussi la tranche des peintures et de la façade.
            var gChapeaux = Groupe("Chapeaux", habillage);
            int n = 0;
            foreach (var b in murs)
                Boite($"Chapeau_{n++}", gChapeaux, new Vector3(b.min.x - e, b.max.y, b.min.z - e), new Vector3(b.max.x + e, b.max.y + 0.006f, b.max.z + e), chapeau);

            // Façade : panneaux sur les faces extérieures, prolongés aux angles du bâtiment.
            var ext = murs[0];
            foreach (var b in murs) ext.Encapsulate(b);
            var vitres = new List<Bounds>();
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                if (r.name == "Vitre") vitres.Add(new Bounds(t.InverseTransformPoint(r.bounds.center), r.bounds.size));
            var gFacade = Groupe("Facade", habillage);
            foreach (var cote in Cotes(ext, exterieur: true))
                foreach (var (a0, a1) in Segments(murs, cote))
                {
                    float d0 = a0, d1 = a1;
                    if (cote.axeX && Mathf.Abs(a0 - ext.min.x) < 0.01f) d0 -= eFacade;
                    if (cote.axeX && Mathf.Abs(a1 - ext.max.x) < 0.01f) d1 += eFacade;
                    foreach (var (x0, x1, y0, y1) in Percer(d0, d1, h, cote, vitres))
                        Panneau($"Facade_{cote.nom}", gFacade, cote, x0, x1, y0, y1, 0f, eFacade, facade);
                }

            // Pièces : intérieur = sol réduit de la demi-épaisseur des murs.
            float demiMur = Limites(t, t.Find("Murs/Mur_Nord")).size.z / 2f;
            foreach (var (piece, sol, peinture) in new[] { ("Salon", "Sol_Salon", (Material)null), ("Cuisine", "Sol_Cuisine", cuisine) })
            {
                var s = Limites(t, t.Find("Sols/" + sol));
                var interieur = new Bounds(s.center, new Vector3(s.size.x - 2f * demiMur, h, s.size.z - 2f * demiMur));
                var gPiece = Groupe(piece, habillage);
                float decalage = peinture != null ? e : 0f;
                foreach (var cote in Cotes(interieur, exterieur: false))
                    foreach (var (a0, a1) in Segments(murs, cote))
                    {
                        // Les côtés le long de x couvrent les angles ; ceux le long de z s'arrêtent avant.
                        bool angle0 = Mathf.Abs(a0 - (cote.axeX ? interieur.min.x : interieur.min.z)) < 0.01f;
                        bool angle1 = Mathf.Abs(a1 - (cote.axeX ? interieur.max.x : interieur.max.z)) < 0.01f;
                        float r0 = !cote.axeX && angle0 ? decalage : 0f, r1 = !cote.axeX && angle1 ? decalage : 0f;
                        if (peinture != null)
                            Panneau($"Peinture_{cote.nom}", gPiece, cote, a0 + r0, a1 - r1, 0f, h, 0f, e, peinture);
                        float p0 = !cote.axeX && angle0 ? decalage + ep : 0f, p1 = !cote.axeX && angle1 ? decalage + ep : 0f;
                        Panneau($"Plinthe_{cote.nom}", gPiece, cote, a0 + p0, a1 - p1, 0f, hp, decalage, ep, plinthe);
                    }
            }

            PrefabUtility.SaveAsPrefabAsset(appartement, AppartementPath);
            PrefabUtility.UnloadPrefabContents(appartement);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Un côté d'un rectangle : la ligne (z pour un côté le long de x, x sinon) et le sens
        /// vers lequel la face visible regarde (+1 ou -1 sur l'axe perpendiculaire).
        /// </summary>
        private struct Cote
        {
            public string nom;
            public bool axeX;       // le côté court le long de x (nord, sud)
            public float ligne;
            public float sens;
            public float debut, fin;
        }

        /// <summary>
        /// Côtés d'un rectangle. Extérieur : faces qui regardent dehors. Intérieur : faces qui
        /// regardent dans la pièce.
        /// </summary>
        private static IEnumerable<Cote> Cotes(Bounds r, bool exterieur)
        {
            float s = exterieur ? 1f : -1f;
            yield return new Cote { nom = "Nord", axeX = true, ligne = r.max.z, sens = s, debut = r.min.x, fin = r.max.x };
            yield return new Cote { nom = "Sud", axeX = true, ligne = r.min.z, sens = -s, debut = r.min.x, fin = r.max.x };
            yield return new Cote { nom = "Est", axeX = false, ligne = r.max.x, sens = s, debut = r.min.z, fin = r.max.z };
            yield return new Cote { nom = "Ouest", axeX = false, ligne = r.min.x, sens = -s, debut = r.min.z, fin = r.max.z };
        }

        /// <summary>
        /// Portions du côté couvertes par un mur dont la face est sur la ligne du côté. Les
        /// ouvertures (portes) sont les trous entre ces portions.
        /// </summary>
        private static List<(float, float)> Segments(List<Bounds> murs, Cote cote)
        {
            var segments = new List<(float, float)>();
            foreach (var b in murs)
            {
                // Face du mur qui regarde dans le sens du côté.
                float face = cote.axeX ? (cote.sens > 0 ? b.max.z : b.min.z) : (cote.sens > 0 ? b.max.x : b.min.x);
                if (Mathf.Abs(face - cote.ligne) > 0.01f) continue;
                float a0 = Mathf.Max(cote.axeX ? b.min.x : b.min.z, cote.debut);
                float a1 = Mathf.Min(cote.axeX ? b.max.x : b.max.z, cote.fin);
                if (a1 - a0 > 0.005f) segments.Add((a0, a1));
            }
            // Fusion des portions qui se touchent (ex. mur sud et ses morceaux autour de la porte).
            segments.Sort((p, q) => p.Item1.CompareTo(q.Item1));
            var fusion = new List<(float, float)>();
            foreach (var sg in segments)
            {
                if (fusion.Count > 0 && sg.Item1 <= fusion[fusion.Count - 1].Item2 + 0.002f)
                    fusion[fusion.Count - 1] = (fusion[fusion.Count - 1].Item1, Mathf.Max(fusion[fusion.Count - 1].Item2, sg.Item2));
                else fusion.Add(sg);
            }
            return fusion;
        }

        /// <summary>Découpe un panneau de façade autour des vitres qui traversent ce côté.</summary>
        private static List<(float, float, float, float)> Percer(float a0, float a1, float h, Cote cote, List<Bounds> vitres)
        {
            var morceaux = new List<(float, float, float, float)>();
            float debut = a0;
            foreach (var v in vitres)
            {
                float vLigne = cote.axeX ? v.center.z : v.center.x;
                if (Mathf.Abs(vLigne - cote.ligne) > 0.2f) continue;
                float v0 = cote.axeX ? v.min.x : v.min.z, v1 = cote.axeX ? v.max.x : v.max.z;
                if (v1 <= debut || v0 >= a1) continue;
                morceaux.Add((debut, v0, 0f, h));
                morceaux.Add((v0, v1, 0f, v.min.y));
                morceaux.Add((v0, v1, v.max.y, h));
                debut = v1;
            }
            morceaux.Add((debut, a1, 0f, h));
            morceaux.RemoveAll(m => m.Item2 - m.Item1 < 0.005f || m.Item4 - m.Item3 < 0.005f);
            return morceaux;
        }

        /// <summary>
        /// Plaque posée contre un côté, de a0 à a1 le long du côté, de y0 à y1, entre
        /// <paramref name="decalage"/> et decalage + epaisseur devant la face.
        /// </summary>
        private static void Panneau(string nom, Transform parent, Cote cote, float a0, float a1, float y0, float y1, float decalage, float epaisseur, Material materiau)
        {
            if (a1 - a0 < 0.005f) return;
            float p0 = cote.ligne + cote.sens * decalage, p1 = cote.ligne + cote.sens * (decalage + epaisseur);
            float lo = Mathf.Min(p0, p1), hi = Mathf.Max(p0, p1);
            var min = cote.axeX ? new Vector3(a0, y0, lo) : new Vector3(lo, y0, a0);
            var max = cote.axeX ? new Vector3(a1, y1, hi) : new Vector3(hi, y1, a1);
            Boite(nom, parent, min, max, materiau);
        }
    }
}
