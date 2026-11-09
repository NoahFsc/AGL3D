using UnityEngine;

namespace AGL3D.LotC
{
    /// <summary>
    /// Repère posé sur un élément noté : une pastille verte (bon), orange (usage) ou rouge
    /// (mauvais). Enfant de l'élément, il se clique comme lui : le clic remonte jusqu'à son
    /// <c>InspectableElement</c> et rouvre la fiche.
    /// </summary>
    [DisallowMultipleComponent]
    public class PointMarker : MonoBehaviour
    {
        [SerializeField] private Renderer pastille;
        [SerializeField] private Material bon;
        [SerializeField] private Material usage;
        [SerializeField] private Material mauvais;

        /// <summary>État affiché : "bon", "usage" ou "mauvais".</summary>
        public string Etat { get; private set; }

        /// <summary>Colore la pastille selon l'état. Un état inconnu est signalé et ignoré.</summary>
        public void DefinirEtat(string etat)
        {
            Material materiau = etat switch
            {
                "bon" => bon,
                "usage" => usage,
                "mauvais" => mauvais,
                _ => null,
            };
            if (materiau == null)
            {
                Debug.LogWarning($"[PointMarker] État inconnu : \"{etat}\".", this);
                return;
            }
            Etat = etat;
            pastille.sharedMaterial = materiau;
        }
    }
}
