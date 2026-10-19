using AGL3D.Bridge;
using UnityEngine;

namespace AGL3D.LotA
{
    /// <summary>
    /// Reçoit <c>focusElement</c> : rapproche la caméra de l'<see cref="InspectableElement"/>
    /// en environ une seconde (docs/contrat-messages.md). La caméra se place du côté de la
    /// pièce de l'élément, pour ne pas le regarder à travers un mur.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraOrbit))]
    public class ElementFocus : MonoBehaviour
    {
        [Tooltip("Durée du déplacement vers un élément, en secondes.")]
        [SerializeField] private float duration = 1f;
        [SerializeField] private float distance = 2.5f;
        [SerializeField] private float pitch = 50f;

        private CameraOrbit orbit;
        private WebBridge bridge;

        private void Awake()
        {
            orbit = GetComponent<CameraOrbit>();
        }

        private void Start()
        {
            bridge = WebBridge.Instance;
            if (bridge != null) bridge.FocusElementReceived += OnFocusElement;
        }

        private void OnDestroy()
        {
            if (bridge != null) bridge.FocusElementReceived -= OnFocusElement;
        }

        private void OnFocusElement(FocusElementPayload payload)
        {
            Focus(payload.key);
        }

        /// <summary>Rapproche la caméra de l'élément. Sert aussi au clic sur un élément (lot B).</summary>
        public void Focus(string key)
        {
            var element = InspectableElement.Find(key);
            if (element == null)
            {
                Debug.LogWarning($"[ElementFocus] Élément inconnu : \"{key}\".", this);
                return;
            }
            var target = element.GetComponent<Collider>().bounds.center;
            orbit.MoveTo(target, distance, YawFacing(element, target), pitch, duration);
        }

        /// <summary>Orientation qui place la caméra entre l'élément et le centre de sa pièce.</summary>
        private float YawFacing(InspectableElement element, Vector3 target)
        {
            var room = CameraView.Find(element.RoomKey);
            if (room == null) return orbit.Yaw;
            var toRoom = room.Target - target;
            toRoom.y = 0f;
            // Élément au milieu de la pièce : on garde l'orientation actuelle.
            if (toRoom.sqrMagnitude < 0.5f * 0.5f) return orbit.Yaw;
            // La caméra est en arrière de son axe avant : elle regarde donc dans le sens opposé.
            return Mathf.Atan2(-toRoom.x, -toRoom.z) * Mathf.Rad2Deg;
        }
    }
}
