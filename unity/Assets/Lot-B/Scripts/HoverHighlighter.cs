using AGL3D.Bridge;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AGL3D.LotB
{
    /// <summary>
    /// Illumine l'élément inspectable sous le pointeur (fonctionnalité 1, tâche B2).
    /// Le survol reste dans Unity : aucun message n'est envoyé à la page.
    /// La couleur du survol est réservée : elle ne doit pas ressembler aux couleurs
    /// d'état (vert, orange, rouge). L'élément sélectionné (<see cref="ElementSelector"/>)
    /// garde cette couleur, plus marquée. Rien n'est illuminé quand le pointeur est hors du
    /// canvas ou au-dessus d'un élément de la page (<see cref="CanvasPointer"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class HoverHighlighter : MonoBehaviour
    {
        [Tooltip("Couleur réservée au survol, distincte des couleurs d'état.")]
        [SerializeField] private Color hoverColor = new Color(0.25f, 0.62f, 1f);

        [Tooltip("Part de la couleur de survol mélangée à la couleur de l'élément survolé.")]
        [Range(0f, 1f)]
        [SerializeField] private float strength = 0.6f;

        [Tooltip("Part de la couleur de survol mélangée à la couleur de l'élément sélectionné.")]
        [Range(0f, 1f)]
        [SerializeField] private float selectedStrength = 0.85f;

        [Tooltip("Durée du fondu, en secondes.")]
        [SerializeField] private float fadeDuration = 0.12f;

        [Tooltip("Distance maximale du rayon depuis la caméra.")]
        [SerializeField] private float maxDistance = 200f;

        [SerializeField] private LayerMask layers = Physics.DefaultRaycastLayers;

        private ElementHighlight current;

        /// <summary>Élément survolé, ou null.</summary>
        public InspectableElement Current => current != null ? current.GetComponent<InspectableElement>() : null;

        private void Update()
        {
            SetCurrent(FindHovered());
        }

        private void OnDisable()
        {
            SetCurrent(null);
        }

        /// <summary>
        /// Teinte de l'élément, créée et configurée au besoin. Partagée avec
        /// <see cref="ElementSelector"/> pour que survol et sélection gardent la même couleur.
        /// </summary>
        public ElementHighlight HighlightFor(InspectableElement element)
        {
            if (element == null) return null;
            if (!element.TryGetComponent(out ElementHighlight highlight))
                highlight = element.gameObject.AddComponent<ElementHighlight>();
            highlight.Configure(hoverColor, strength, selectedStrength, fadeDuration);
            return highlight;
        }

        /// <summary>
        /// Élément inspectable sous le pointeur, ou null si le pointeur est hors du canvas,
        /// au-dessus d'un élément de la page, ou ne touche aucun élément.
        /// </summary>
        public InspectableElement Raycast()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || !CanvasPointer.IsInside) return null;

            Vector2 position = mouse.position.ReadValue();
            if (!cam.pixelRect.Contains(position)) return null;

            var ray = cam.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out var hit, maxDistance, layers, QueryTriggerInteraction.Ignore)) return null;
            var element = hit.collider.GetComponentInParent<InspectableElement>();
            return element != null && element.isActiveAndEnabled ? element : null;
        }

        private InspectableElement FindHovered()
        {
            var mouse = Mouse.current;
            // Pas de survol pendant la rotation de la caméra au clic droit.
            if (mouse == null || mouse.rightButton.isPressed) return null;
            return Raycast();
        }

        private void SetCurrent(InspectableElement element)
        {
            ElementHighlight next = HighlightFor(element);

            if (next == current) return;
            if (current != null) current.SetHovered(false);
            current = next;
            if (current != null) current.SetHovered(true);
        }
    }
}
