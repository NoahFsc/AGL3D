using System.Collections.Generic;
using AGL3D.Bridge;
using UnityEngine;

namespace AGL3D.LotC
{
    /// <summary>
    /// Reçoit <c>setPoints</c> et tient à jour une étiquette <see cref="PointMarker"/> par élément
    /// noté, numérotée dans l'ordre de la liste : ajoute les nouvelles, met à jour les autres et
    /// retire celles qui ne sont plus dans la liste. Une clé inconnue est signalée et ignorée
    /// (docs/contrat-messages.md). Tant qu'un élément a son étiquette, son petit libellé
    /// (objet « Label » du lot A) est masqué pour ne pas se superposer.
    /// </summary>
    [DisallowMultipleComponent]
    public class PointMarkers : MonoBehaviour
    {
        [Tooltip("Prefab de l'étiquette (Assets/Lot-C/Reperes/PointMarker).")]
        [SerializeField] private PointMarker modele;

        private readonly Dictionary<string, PointMarker> reperes = new Dictionary<string, PointMarker>();
        private WebBridge bridge;

        private void Start()
        {
            bridge = WebBridge.Instance;
            if (bridge != null) bridge.SetPointsReceived += OnSetPoints;
        }

        private void OnDestroy()
        {
            if (bridge != null) bridge.SetPointsReceived -= OnSetPoints;
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
