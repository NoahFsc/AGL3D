using AGL3D.Bridge;
using UnityEngine;

namespace AGL3D.LotA
{
    /// <summary>
    /// Reçoit <c>focusRoom</c> : déplace la caméra vers la <see cref="CameraView"/> de la
    /// pièce en environ une seconde, puis envoie <c>roomChanged</c> (docs/contrat-messages.md).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraOrbit))]
    public class RoomFocus : MonoBehaviour
    {
        [Tooltip("Durée du déplacement vers une pièce, en secondes.")]
        [SerializeField] private float duration = 1f;

        private CameraOrbit orbit;
        private WebBridge bridge;

        private void Awake()
        {
            orbit = GetComponent<CameraOrbit>();
        }

        private void Start()
        {
            // WebBridge.Instance est défini dans son Awake, donc avant ce Start.
            bridge = WebBridge.Instance;
            if (bridge != null) bridge.FocusRoomReceived += OnFocusRoom;
        }

        private void OnDestroy()
        {
            if (bridge != null) bridge.FocusRoomReceived -= OnFocusRoom;
        }

        private void OnFocusRoom(FocusRoomPayload payload)
        {
            Focus(payload.room);
        }

        /// <summary>Déplace la caméra vers une pièce ou "overview", puis envoie roomChanged.</summary>
        public void Focus(string room)
        {
            var view = CameraView.Find(room);
            if (view == null)
            {
                Debug.LogWarning($"[RoomFocus] Pièce inconnue : \"{room}\".", this);
                return;
            }
            orbit.MoveTo(view.Target, view.Distance, view.Yaw, view.Pitch, duration,
                () => WebBridge.EmitRoomChanged(room));
        }
    }
}
