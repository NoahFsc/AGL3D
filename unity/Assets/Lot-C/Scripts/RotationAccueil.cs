using AGL3D.Bridge;
using AGL3D.LotA;
using AGL3D.LotB;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AGL3D.LotC
{
    /// <summary>
    /// Fait tourner lentement la maquette en mode <c>accueil</c> (message <c>init</c>).
    /// La rotation se fige dès qu'on interagit avec la 3D : l'élément sous la souris ne bouge
    /// plus. Si le mode change pendant qu'elle tourne, la maquette revient en place en une
    /// seconde. Avant <c>focusRoom</c> et <c>focusElement</c>, elle est remise en place d'un coup
    /// pour que la caméra vise les bons endroits : d'où l'ordre d'exécution, qui l'abonne avant
    /// <see cref="RoomFocus"/> et <see cref="ElementFocus"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class RotationAccueil : MonoBehaviour
    {
        [Tooltip("Maquette à faire tourner (prefab Appartement).")]
        [SerializeField] private Transform maquette;
        [Tooltip("Vitesse de rotation en mode accueil, en degrés par seconde.")]
        [SerializeField] private float vitesse = 8f;
        [Tooltip("Durée du retour à la position de départ quand le mode change, en secondes.")]
        [SerializeField] private float dureeRetour = 1f;

        private WebBridge bridge;
        private Vector3 pivot;
        private Vector3 positionDepart;
        private Quaternion rotationDepart;
        private float angle;
        private bool enAccueil;
        private bool retourEnCours;
        private float angleDebutRetour;
        private float tempsRetour;

        private void Start()
        {
            if (maquette == null)
            {
                Debug.LogWarning("[RotationAccueil] Aucune maquette assignée.", this);
                enabled = false;
                return;
            }
            positionDepart = maquette.position;
            rotationDepart = maquette.rotation;
            pivot = CentreMaquette();

            bridge = WebBridge.Instance;
            if (bridge == null) return;
            bridge.InitReceived += OnInitReceived;
            bridge.FocusRoomReceived += OnFocusRoom;
            bridge.FocusElementReceived += OnFocusElement;
        }

        private void OnDestroy()
        {
            if (bridge == null) return;
            bridge.InitReceived -= OnInitReceived;
            bridge.FocusRoomReceived -= OnFocusRoom;
            bridge.FocusElementReceived -= OnFocusElement;
        }

        private void OnInitReceived(InitPayload payload) => DefinirMode(payload.mode);

        private void OnFocusRoom(FocusRoomPayload payload) => RemettreEnPlace();

        private void OnFocusElement(FocusElementPayload payload) => RemettreEnPlace();

        /// <summary>Lance la rotation en mode accueil ; ailleurs, ramène en douceur une maquette qui tourne.</summary>
        public void DefinirMode(string mode)
        {
            if (mode == "accueil")
            {
                enAccueil = true;
                retourEnCours = false;
                return;
            }
            if (!enAccueil) return;
            enAccueil = false;
            retourEnCours = true;
            angleDebutRetour = Mathf.DeltaAngle(0f, angle);
            tempsRetour = 0f;
        }

        private void Update()
        {
            if (enAccueil)
            {
                if (Interaction())
                {
                    enAccueil = false;
                    return;
                }
                angle = Mathf.Repeat(angle + vitesse * Time.deltaTime, 360f);
                Appliquer();
            }
            else if (retourEnCours)
            {
                tempsRetour += Time.deltaTime;
                float t = dureeRetour > 0f ? Mathf.Clamp01(tempsRetour / dureeRetour) : 1f;
                angle = Mathf.Lerp(angleDebutRetour, 0f, Mathf.SmoothStep(0f, 1f, t));
                Appliquer();
                if (t >= 1f) retourEnCours = false;
            }
        }

        /// <summary>Vrai si l'utilisateur clique, tourne la vue ou zoome sur la 3D. Le survol ne compte pas.</summary>
        private static bool Interaction()
        {
            var souris = Mouse.current;
            if (souris == null || !CanvasPointer.IsInside) return false;
            return souris.leftButton.wasPressedThisFrame
                || souris.rightButton.wasPressedThisFrame
                || souris.middleButton.wasPressedThisFrame
                || souris.scroll.ReadValue().y != 0f;
        }

        /// <summary>Remet la maquette à sa position de départ d'un coup et arrête toute rotation.</summary>
        private void RemettreEnPlace()
        {
            enAccueil = false;
            retourEnCours = false;
            angle = 0f;
            Appliquer();
        }

        /// <summary>Tourne la maquette de <see cref="angle"/> autour de l'axe vertical de son centre.</summary>
        private void Appliquer()
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            maquette.SetPositionAndRotation(
                pivot + rotation * (positionDepart - pivot),
                rotation * rotationDepart);
            Physics.SyncTransforms();
        }

        /// <summary>Centre des rendus de la maquette, pour qu'elle tourne sur elle-même.</summary>
        private Vector3 CentreMaquette()
        {
            var rendus = maquette.GetComponentsInChildren<Renderer>();
            if (rendus.Length == 0) return maquette.position;
            Bounds limites = rendus[0].bounds;
            foreach (var rendu in rendus) limites.Encapsulate(rendu.bounds);
            return limites.center;
        }

        [ContextMenu("Simuler le mode accueil")]
        private void SimulerAccueil() => DefinirMode("accueil");

        [ContextMenu("Simuler le mode entree")]
        private void SimulerEntree() => DefinirMode("entree");
    }
}
