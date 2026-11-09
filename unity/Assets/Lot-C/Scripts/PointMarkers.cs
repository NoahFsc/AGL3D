using System.Collections.Generic;
using AGL3D.Bridge;
using UnityEngine;

namespace AGL3D.LotC
{
    /// <summary>
    /// Reçoit <c>setPoints</c> et tient à jour un <see cref="PointMarker"/> par élément noté :
    /// ajoute les nouveaux, recolore les autres et retire ceux qui ne sont plus dans la liste.
    /// Une clé inconnue est signalée et ignorée (docs/contrat-messages.md).
    /// </summary>
    [DisallowMultipleComponent]
    public class PointMarkers : MonoBehaviour
    {
        [Tooltip("Prefab du repère (Assets/Lot-C/Reperes/PointMarker).")]
        [SerializeField] private PointMarker modele;
        [Tooltip("Diamètre de la pastille, en mètres.")]
        [SerializeField] private float diametre = 0.16f;
        [Tooltip("Espace entre le haut de l'élément et la pastille, en mètres.")]
        [SerializeField] private float ecart = 0.06f;

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
                repere.DefinirEtat(point.etat);
                notes.Add(point.key);
            }

            var aRetirer = new List<string>();
            foreach (var entree in reperes)
            {
                if (!notes.Contains(entree.Key)) aRetirer.Add(entree.Key);
            }
            foreach (var key in aRetirer)
            {
                if (reperes[key] != null) Destroy(reperes[key].gameObject);
                reperes.Remove(key);
            }
        }

        /// <summary>Pose un repère au-dessus de l'élément, en enfant pour qu'il le suive et se clique comme lui.</summary>
        private PointMarker Creer(InspectableElement element)
        {
            var repere = Instantiate(modele, element.transform);
            repere.name = "Repere";
            Bounds limites = Limites(element);
            repere.transform.SetPositionAndRotation(
                new Vector3(limites.center.x, limites.max.y + ecart + diametre / 2f, limites.center.z),
                Quaternion.identity);

            // Taille fixe dans le monde, quelle que soit l'échelle de l'élément.
            Vector3 echelle = element.transform.lossyScale;
            repere.transform.localScale = new Vector3(
                diametre / Mathf.Max(echelle.x, 0.0001f),
                diametre / Mathf.Max(echelle.y, 0.0001f),
                diametre / Mathf.Max(echelle.z, 0.0001f));
            return repere;
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
