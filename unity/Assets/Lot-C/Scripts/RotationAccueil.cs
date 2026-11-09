using AGL3D.Bridge;
using AGL3D.LotA;
using UnityEngine;

namespace AGL3D.LotC
{
    /// <summary>
    /// Fait tourner lentement la maquette en mode <c>accueil</c> (message <c>init</c>).
    /// Dans tout autre mode, la ramène à sa position de départ en environ une seconde, pour
    /// que les vues de caméra de <c>focusRoom</c> restent alignées sur les pièces.
    /// Pendant la rotation, les murs (<see cref="CameraObstacle"/>) sont ignorés par les rayons :
    /// sinon les cloisons qui défilent devant la caméra la font plonger vers la maquette.
    /// </summary>
    [DisallowMultipleComponent]
    public class RotationAccueil : MonoBehaviour
    {
        [Tooltip("Maquette à faire tourner (prefab Appartement).")]
        [SerializeField] private Transform maquette;
        [Tooltip("Vitesse de rotation en mode accueil, en degrés par seconde.")]
        [SerializeField] private float vitesse = 8f;
        [Tooltip("Durée du retour à la position de départ, en secondes.")]
        [SerializeField] private float dureeRetour = 1f;

        private WebBridge bridge;
        private Vector3 pivot;
        private Vector3 positionDepart;
        private Quaternion rotationDepart;
        private float angle;
        private bool enAccueil;
        private float angleDebutRetour;
        private float tempsRetour;
        private CameraObstacle[] murs;
        private int[] calquesMurs;

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
            murs = maquette.GetComponentsInChildren<CameraObstacle>(true);
            calquesMurs = new int[murs.Length];
            for (int i = 0; i < murs.Length; i++) calquesMurs[i] = murs[i].gameObject.layer;

            bridge = WebBridge.Instance;
            if (bridge != null) bridge.InitReceived += OnInitReceived;
        }

        private void OnDestroy()
        {
            if (bridge != null) bridge.InitReceived -= OnInitReceived;
        }

        private void OnInitReceived(InitPayload payload)
        {
            DefinirMode(payload.mode);
        }

        /// <summary>Lance la rotation en mode accueil, sinon ramène la maquette en place.</summary>
        public void DefinirMode(string mode)
        {
            bool accueil = mode == "accueil";
            if (accueil == enAccueil) return;
            enAccueil = accueil;
            if (accueil)
            {
                IgnorerMurs(true);
                return;
            }
            angleDebutRetour = Mathf.DeltaAngle(0f, angle);
            tempsRetour = 0f;
            if (angle == 0f) IgnorerMurs(false);
        }

        private void Update()
        {
            if (enAccueil)
            {
                angle = Mathf.Repeat(angle + vitesse * Time.deltaTime, 360f);
            }
            else if (angle != 0f)
            {
                tempsRetour += Time.deltaTime;
                float t = dureeRetour > 0f ? Mathf.Clamp01(tempsRetour / dureeRetour) : 1f;
                angle = Mathf.Lerp(angleDebutRetour, 0f, Mathf.SmoothStep(0f, 1f, t));
                if (t >= 1f) IgnorerMurs(false);
            }
            else
            {
                return;
            }
            Appliquer();
        }

        /// <summary>Tourne la maquette de <see cref="angle"/> autour de l'axe vertical de son centre.</summary>
        private void Appliquer()
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            maquette.SetPositionAndRotation(
                pivot + rotation * (positionDepart - pivot),
                rotation * rotationDepart);
        }

        /// <summary>Place les murs sur le calque Ignore Raycast, ou leur rend leur calque d'origine.</summary>
        private void IgnorerMurs(bool ignorer)
        {
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            for (int i = 0; i < murs.Length; i++)
            {
                murs[i].gameObject.layer = ignorer ? ignoreRaycast : calquesMurs[i];
            }
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
