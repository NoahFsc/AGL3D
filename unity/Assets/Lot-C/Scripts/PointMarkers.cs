using System.Collections.Generic;
using AGL3D.Bridge;
using UnityEngine;

namespace AGL3D.LotC
{
    /// <summary>
    /// Reçoit <c>setPoints</c> et tient à jour une étiquette <see cref="PointMarker"/> par élément
    /// de la liste (grise s'il n'est pas encore noté), numérotée dans l'ordre de la liste : ajoute les nouvelles, met à jour les autres et
    /// retire celles qui ne sont plus dans la liste. Une clé inconnue est signalée et ignorée
    /// (docs/contrat-messages.md). Tant qu'un élément a son étiquette, son petit libellé
    /// (objet « Label » du lot A) est masqué pour ne pas se superposer.
    /// À chaque image, les étiquettes qui se chevauchent à l'écran sont écartées : la plus basse
    /// reste en place, les autres montent au-dessus (tige allongée). Une étiquette qui sort du
    /// bord de l'écran glisse vers l'intérieur. Après <c>focusRoom</c> vers une pièce, seules les
    /// étiquettes de cette pièce restent affichées ; la vue d'ensemble et l'accueil les montrent toutes. Cela passe avant le LateUpdate de chaque
    /// <see cref="PointMarker"/>, qui cale ensuite sa zone de clic sur le cadre déplacé.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public class PointMarkers : MonoBehaviour
    {
        [Tooltip("Prefab de l'étiquette (Assets/Lot-C/Reperes/PointMarker).")]
        [SerializeField] private PointMarker modele;

        [Tooltip("Espace laissé entre deux étiquettes écartées, en unités de l'étiquette.")]
        [SerializeField] private float ecart = 4f;
        [Tooltip("Marge gardée entre une étiquette et le bord de l'écran, en pixels CSS.")]
        [SerializeField] private float marge = 6f;

        private readonly Dictionary<string, PointMarker> reperes = new Dictionary<string, PointMarker>();
        private readonly List<Placement> aPlacer = new List<Placement>();
        private readonly List<Rect> places = new List<Rect>();

        /// <summary>Une étiquette visible : son point d'ancrage à l'écran et ses pixels par unité.</summary>
        private struct Placement
        {
            public PointMarker Repere;
            public Vector2 Ancre;
            public float Echelle;
        }
        private WebBridge bridge;
        /// <summary>Pièce dont on montre les étiquettes ; vide pour toutes.</summary>
        private string pieceAffichee = string.Empty;

        private void Start()
        {
            bridge = WebBridge.Instance;
            if (bridge == null) return;
            bridge.SetPointsReceived += OnSetPoints;
            bridge.FocusRoomReceived += OnFocusRoom;
            bridge.InitReceived += OnInit;
        }

        private void OnDestroy()
        {
            if (bridge == null) return;
            bridge.SetPointsReceived -= OnSetPoints;
            bridge.FocusRoomReceived -= OnFocusRoom;
            bridge.InitReceived -= OnInit;
        }

        private void OnFocusRoom(FocusRoomPayload payload)
        {
            pieceAffichee = payload.room == "overview" ? string.Empty : payload.room ?? string.Empty;
            AppliquerPiece();
        }

        private void OnInit(InitPayload payload)
        {
            if (payload.mode != "accueil") return;
            pieceAffichee = string.Empty;
            AppliquerPiece();
        }

        /// <summary>Montre les étiquettes de la pièce affichée (toutes si aucune) et masque les autres.</summary>
        private void AppliquerPiece()
        {
            foreach (var entree in reperes)
            {
                if (entree.Value != null) entree.Value.gameObject.SetActive(DansPiece(entree.Key));
            }
        }

        private bool DansPiece(string key)
        {
            return pieceAffichee.Length == 0 || key.StartsWith(pieceAffichee + ".");
        }

        private void OnSetPoints(SetPointsPayload payload)
        {
            var notes = new HashSet<string>();
            int numero = 0;
            foreach (var point in payload.points ?? new PointData[0])
            {
                var element = InspectableElement.Find(point.key);
                if (element == null)
                {
                    Debug.LogWarning($"[PointMarkers] Élément inconnu : \"{point.key}\".", this);
                    continue;
                }
                if (!reperes.TryGetValue(point.key, out var repere) || repere == null)
                {
                    repere = Creer(element);
                    reperes[point.key] = repere;
                }
                repere.gameObject.SetActive(DansPiece(point.key));
                numero++;
                string libelle = string.IsNullOrEmpty(point.label) ? point.key : point.label;
                repere.Afficher(numero, libelle, point.etat, GenreDe(point.key));
                notes.Add(point.key);
            }

            var aRetirer = new List<string>();
            foreach (var entree in reperes)
            {
                if (!notes.Contains(entree.Key)) aRetirer.Add(entree.Key);
            }
            foreach (var key in aRetirer)
            {
                var element = InspectableElement.Find(key);
                if (element != null) AfficherLibelle(element, true);
                if (reperes[key] != null) Destroy(reperes[key].gameObject);
                reperes.Remove(key);
            }
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || reperes.Count == 0) return;

            aPlacer.Clear();
            foreach (var repere in reperes.Values)
            {
                if (repere == null || !repere.isActiveAndEnabled) continue;
                Vector3 ecran = cam.WorldToScreenPoint(repere.transform.position);
                if (ecran.z <= 0f) continue;
                float distance = Vector3.Dot(repere.transform.position - cam.transform.position, cam.transform.forward);
                aPlacer.Add(new Placement
                {
                    Repere = repere,
                    Ancre = ecran,
                    Echelle = repere.PixelsParUnite(Mathf.Max(distance, 0.01f)),
                });
            }

            // De bas en haut : une étiquette ne monte que pour éviter celles déjà placées sous elle.
            aPlacer.Sort((a, b) => a.Ancre.y != b.Ancre.y ? a.Ancre.y.CompareTo(b.Ancre.y) : a.Repere.Numero.CompareTo(b.Repere.Numero));
            places.Clear();
            foreach (var p in aPlacer) places.Add(Placer(p));
        }

        /// <summary>Choisit la place du cadre à l'écran, l'applique à l'étiquette et la renvoie.</summary>
        private Rect Placer(Placement p)
        {
            float k = p.Echelle;
            Vector2 taille = p.Repere.TailleCadre * k;
            float xCentre = p.Ancre.x - taille.x / 2f;
            float yRepos = p.Ancre.y + p.Repere.HauteurTige * k;

            // Rentre dans l'écran, la tige restant sous le cadre. Un élément juste sorti du bord
            // garde son étiquette entière à l'écran ; plus il s'éloigne, plus elle sort avec lui,
            // pour que les éléments des autres pièces ne s'empilent pas sur les bords.
            float pixelMarge = marge * PointMarker.Densite;
            float x = Mathf.Clamp(xCentre, pixelMarge, Screen.width - pixelMarge - taille.x);
            float debord = Mathf.Max(-p.Ancre.x, p.Ancre.x - Screen.width, 0f);
            float jeu = Mathf.Max(taille.x / 2f - 10f * k, 0f) + Mathf.Max(taille.x - debord, 0f);
            x = Mathf.Clamp(x, xCentre - jeu, xCentre + jeu);

            var zone = new Rect(x, yRepos, taille.x, taille.y);
            float espace = ecart * k;
            // Chaque passage monte au-dessus d'au moins une étiquette : places.Count passages suffisent.
            for (int passage = 0; passage <= places.Count; passage++)
            {
                bool monte = false;
                foreach (var autre in places)
                {
                    if (zone.xMin < autre.xMax + espace && autre.xMin < zone.xMax + espace
                        && zone.yMin < autre.yMax + espace && autre.yMin < zone.yMax + espace)
                    {
                        zone.y = autre.yMax + espace;
                        monte = true;
                    }
                }
                if (!monte) break;
            }

            p.Repere.Decaler(new Vector2((zone.x - xCentre) / k, (zone.y - yRepos) / k));
            return zone;
        }

        /// <summary>Les compteurs se reconnaissent à leur clé (salon.compteur-eau, salon.compteur-electricite).</summary>
        private static PointMarker.Genre GenreDe(string key)
        {
            if (key.EndsWith(".compteur-eau")) return PointMarker.Genre.CompteurEau;
            if (key.EndsWith(".compteur-electricite")) return PointMarker.Genre.CompteurElectricite;
            return PointMarker.Genre.Element;
        }

        /// <summary>
        /// Pose l'étiquette au sommet de l'élément, en enfant pour qu'elle le suive et se clique
        /// comme lui. L'échelle inverse de l'élément évite que l'étiquette, qui tourne vers la
        /// caméra, soit déformée par un élément étiré (mur, plan de travail).
        /// </summary>
        private PointMarker Creer(InspectableElement element)
        {
            var repere = Instantiate(modele, element.transform);
            repere.name = "Repere";
            Bounds limites = Limites(element);
            repere.transform.position = new Vector3(limites.center.x, limites.max.y, limites.center.z);
            repere.transform.localRotation = Quaternion.identity;
            Vector3 echelle = element.transform.lossyScale;
            repere.transform.localScale = new Vector3(
                1f / Mathf.Max(echelle.x, 0.0001f),
                1f / Mathf.Max(echelle.y, 0.0001f),
                1f / Mathf.Max(echelle.z, 0.0001f));
            AfficherLibelle(element, false);
            return repere;
        }

        private static void AfficherLibelle(InspectableElement element, bool visible)
        {
            var libelle = element.transform.Find("Label");
            if (libelle != null) libelle.gameObject.SetActive(visible);
        }

        private static Bounds Limites(InspectableElement element)
        {
            var collider = element.GetComponent<Collider>();
            if (collider != null) return collider.bounds;
            var rendu = element.GetComponent<Renderer>();
            return rendu != null ? rendu.bounds : new Bounds(element.transform.position, Vector3.zero);
        }
    }
}
