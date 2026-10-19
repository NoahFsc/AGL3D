using AGL3D.Bridge;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AGL3D.LotB
{
    /// <summary>
    /// Illumine l'élément inspectable sous le pointeur (fonctionnalité 1, tâche B2).
    /// Le survol reste dans Unity : aucun message n'est envoyé à la page.
    /// La couleur du survol est réservée : elle ne doit pas ressembler aux couleurs
    /// d'état (vert, orange, rouge).
    /// </summary>
    [DisallowMultipleComponent]
    public class HoverHighlighter : MonoBehaviour
    {
        [Tooltip("Couleur réservée au survol, distincte des couleurs d'état.")]
        [SerializeField] private Color hoverColor = new Color(0.25f, 0.62f, 1f);

        [Tooltip("Part de la couleur de survol mélangée à la couleur de l'élément.")]
        [Range(0f, 1f)]
        [SerializeField] private float strength = 0.6f;

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

        private InspectableElement FindHovered()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return null;

            // Pas de survol pendant la rotation de la caméra au clic droit.
            if (mouse.rightButton.isPressed) return null;

            Vector2 position = mouse.position.ReadValue();
            if (!cam.pixelRect.Contains(position)) return null;

            var ray = cam.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out var hit, maxDistance, layers, QueryTriggerInteraction.Ignore)) return null;
            return hit.collider.GetComponentInParent<InspectableElement>();
        }

        private void SetCurrent(InspectableElement element)
        {
            ElementHighlight next = null;
            if (element != null && element.isActiveAndEnabled)
            {
                if (!element.TryGetComponent(out next)) next = element.gameObject.AddComponent<ElementHighlight>();
                next.Configure(hoverColor, strength, fadeDuration);
            }

            if (next == current) return;
            if (current != null) current.SetHovered(false);
            current = next;
            if (current != null) current.SetHovered(true);
        }
    }
}
