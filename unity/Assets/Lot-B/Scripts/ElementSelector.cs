using AGL3D.Bridge;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AGL3D.LotB
{
    /// <summary>
    /// Sélection d'un élément au clic (tâche B3).
    /// <list type="bullet">
    /// <item>Clic gauche sur un élément, ou sur un objet enfant comme son repère : l'élément
    /// reste en surbrillance et Unity envoie <c>pointSelected</c>.</item>
    /// <item>Clic gauche dans le vide : la sélection est retirée et Unity envoie <c>deselected</c>.</item>
    /// <item>Message <c>select</c> reçu de la page : la sélection suit, sans message en retour.</item>
    /// <item>Message <c>init</c> reçu : la sélection est retirée.</item>
    /// </list>
    /// Un clic qui a glissé de plus de <see cref="dragThreshold"/> pixels n'est pas un clic.
    /// Le rapprochement de la caméra au clic est fait par le lot A (A7).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HoverHighlighter))]
    public class ElementSelector : MonoBehaviour
    {
        [Tooltip("Déplacement maximal de la souris entre l'appui et le relâchement, en pixels.")]
        [SerializeField] private float dragThreshold = 6f;

        private HoverHighlighter highlighter;
        private WebBridge bridge;
        private InspectableElement selected;

        private bool pressing;
        private Vector2 pressPosition;

        /// <summary>Élément sélectionné, ou null.</summary>
        public InspectableElement Selected => selected;

        private void Awake()
        {
            highlighter = GetComponent<HoverHighlighter>();
        }

        private void Start()
        {
            // WebBridge s'initialise dans son Awake : il est prêt au Start de tous les objets.
            bridge = WebBridge.Instance;
            if (bridge == null)
            {
                Debug.LogWarning("[ElementSelector] Aucun WebBridge dans la scène : select et init sont ignorés.", this);
                return;
            }
            bridge.SelectReceived += OnSelectReceived;
            bridge.InitReceived += OnInitReceived;
        }

        private void OnDestroy()
        {
            if (bridge == null) return;
            bridge.SelectReceived -= OnSelectReceived;
            bridge.InitReceived -= OnInitReceived;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var button = mouse.leftButton;
            if (button.wasPressedThisFrame)
            {
                // Un appui hors du canvas (sur la fiche, un bouton de la page) ne compte pas.
                pressing = CanvasPointer.IsInside && !mouse.rightButton.isPressed;
                pressPosition = mouse.position.ReadValue();
            }

            if (!button.wasReleasedThisFrame || !pressing) return;
            pressing = false;

            Vector2 position = mouse.position.ReadValue();
            if ((position - pressPosition).sqrMagnitude > dragThreshold * dragThreshold) return;
            if (!CanvasPointer.IsInside) return;

            var element = highlighter.Raycast();
            if (element != null)
            {
                SetSelected(element);
                WebBridge.EmitPointSelected(element.Key, position);
            }
            else
            {
                SetSelected(null);
                WebBridge.EmitDeselected();
            }
        }

        private void OnSelectReceived(SelectPayload payload)
        {
            string key = payload?.key;
            if (string.IsNullOrEmpty(key))
            {
                SetSelected(null);
                return;
            }

            var element = InspectableElement.Find(key);
            if (element == null) Debug.LogWarning($"[ElementSelector] select : clé inconnue \"{key}\".", this);
            SetSelected(element);
        }

        private void OnInitReceived(InitPayload payload)
        {
            SetSelected(null);
        }

        private void SetSelected(InspectableElement element)
        {
            if (element == selected) return;
            if (selected != null) highlighter.HighlightFor(selected).SetSelected(false);
            selected = element;
            if (selected != null) highlighter.HighlightFor(selected).SetSelected(true);
        }
    }
}
