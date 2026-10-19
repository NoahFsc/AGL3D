using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AGL3D.LotA
{
    /// <summary>
    /// Caméra en orbite autour de l'appartement : clic droit maintenu et glisser pour
    /// tourner, molette pour zoomer. Le mouvement est lissé et l'inclinaison reste bornée
    /// pour que la caméra garde une vue plongeante sur la maquette.
    /// La caméra ne traverse pas les murs marqués <see cref="CameraObstacle"/> et reste
    /// au-dessus de leur sommet. <see cref="MoveTo"/> l'amène vers une autre vue.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraOrbit : MonoBehaviour
    {
        [Header("Cible")]
        [Tooltip("Point autour duquel la caméra tourne (centre de l'appartement).")]
        [SerializeField] private Vector3 target = Vector3.zero;

        [Header("Rotation")]
        [SerializeField] private float yaw;
        [SerializeField] private float pitch = 52f;
        [SerializeField] private float minPitch = 30f;
        [SerializeField] private float maxPitch = 88f;
        [Tooltip("Degrés par pixel de déplacement de la souris.")]
        [SerializeField] private float rotateSpeed = 0.25f;

        [Header("Zoom")]
        [SerializeField] private float distance = 10f;
        [SerializeField] private float minDistance = 4f;
        [SerializeField] private float maxDistance = 16f;
        [Tooltip("Part de la distance parcourue par cran de molette.")]
        [SerializeField] private float zoomStep = 0.15f;

        [Header("Murs")]
        [Tooltip("Hauteur minimale de la caméra : sommet des murs (1,20 m) plus une marge.")]
        [SerializeField] private float minHeight = 1.6f;
        [Tooltip("Rayon de la sphère testée contre les murs, pour garder la caméra à distance.")]
        [SerializeField] private float collisionRadius = 0.25f;

        [Header("Lissage")]
        [Tooltip("Temps de réponse en secondes : plus petit = plus nerveux.")]
        [SerializeField] private float smoothTime = 0.1f;

        private float currentYaw, currentPitch, currentDistance;
        private readonly RaycastHit[] hits = new RaycastHit[16];

        // Déplacement en cours vers une autre vue (MoveTo).
        private bool moving;
        private float moveTime, moveDuration;
        private Vector3 fromTarget, toTarget;
        private float fromYaw, toYaw, fromPitch, toPitch, fromDistance, toDistance;
        private Action onArrived;

        /// <summary>Vrai pendant un déplacement lancé par <see cref="MoveTo"/>.</summary>
        public bool IsMoving => moving;

        /// <summary>
        /// Amène la caméra vers une autre vue en <paramref name="duration"/> secondes, avec un
        /// départ et une arrivée en douceur. La souris est ignorée pendant le déplacement.
        /// Un nouvel appel remplace le précédent, dont <paramref name="arrived"/> n'est pas appelé.
        /// </summary>
        public void MoveTo(Vector3 newTarget, float newDistance, float newYaw, float newPitch, float duration, Action arrived = null)
        {
            fromTarget = target;
            fromYaw = currentYaw;
            fromPitch = currentPitch;
            fromDistance = currentDistance;
            toTarget = newTarget;
            toYaw = currentYaw + Mathf.DeltaAngle(currentYaw, newYaw); // chemin le plus court
            toPitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
            toDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
            moveDuration = Mathf.Max(0.01f, duration);
            moveTime = 0f;
            onArrived = arrived;
            moving = true;
        }

        private void Awake()
        {
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            currentYaw = yaw;
            currentPitch = pitch;
            currentDistance = distance;
            Apply();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || moving) return;

            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * rotateSpeed;
                pitch = Mathf.Clamp(pitch - delta.y * rotateSpeed, minPitch, maxPitch);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f) && IsPointerInside(mouse))
            {
                // L'Input System ramène un cran de molette à [-1, 1] sur toutes les plateformes
                // (InputSettings.scrollDeltaBehavior) ; les trackpads envoient des valeurs plus petites.
                float notches = Mathf.Clamp(scroll, -1f, 1f);
                distance = Mathf.Clamp(distance * (1f - notches * zoomStep), minDistance, maxDistance);
            }
        }

        private void LateUpdate()
        {
            if (moving)
            {
                Move();
                return;
            }
            float t = smoothTime <= 0f ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothTime);
            currentYaw = Mathf.LerpAngle(currentYaw, yaw, t);
            currentPitch = Mathf.Lerp(currentPitch, pitch, t);
            currentDistance = Mathf.Lerp(currentDistance, distance, t);
            Apply();
        }

        private void Move()
        {
            moveTime += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, moveTime / moveDuration);
            target = Vector3.Lerp(fromTarget, toTarget, k);
            yaw = currentYaw = Mathf.Lerp(fromYaw, toYaw, k);
            pitch = currentPitch = Mathf.Lerp(fromPitch, toPitch, k);
            distance = currentDistance = Mathf.Lerp(fromDistance, toDistance, k);
            Apply();

            if (moveTime >= moveDuration)
            {
                moving = false;
                var arrived = onArrived;
                onArrived = null;
                arrived?.Invoke();
            }
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            var back = rotation * Vector3.back;
            // Pendant un MoveTo, la caméra reste au-dessus des murs : la détection ferait plonger
            // la caméra quand la cible passe près d'une cloison. Les vues d'arrivée sont dégagées.
            float d = moving ? currentDistance : DistanceBeforeWall(back, currentDistance);
            var position = target + back * d;
            position.y = Mathf.Max(position.y, minHeight);
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>Distance jusqu'au premier mur entre la cible et la caméra, sans dépasser <paramref name="distance"/>.</summary>
        private float DistanceBeforeWall(Vector3 direction, float distance)
        {
            // La sphère garde la caméra à distance des murs. Quand la cible est collée à un mur,
            // la sphère part déjà dedans et ne le voit pas : le rayon fin le détecte quand même.
            int count = Physics.SphereCastNonAlloc(target, collisionRadius, direction, hits, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = Nearest(count, distance);
            count = Physics.RaycastNonAlloc(target, direction, hits, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return Nearest(count, nearest);
        }

        private float Nearest(int count, float nearest)
        {
            for (int i = 0; i < count; i++)
            {
                // distance == 0 : le test part déjà dans le collider, il ne donne pas de distance utile.
                if (hits[i].distance > 0f && hits[i].distance < nearest
                    && hits[i].collider.GetComponentInParent<CameraObstacle>() != null)
                {
                    nearest = hits[i].distance;
                }
            }
            return nearest;
        }

        private static bool IsPointerInside(Mouse mouse)
        {
            var p = mouse.position.ReadValue();
            return p.x >= 0f && p.y >= 0f && p.x <= Screen.width && p.y <= Screen.height;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxPitch = Mathf.Clamp(maxPitch, minPitch, 89f);
            maxDistance = Mathf.Max(maxDistance, minDistance);
            collisionRadius = Mathf.Max(0f, collisionRadius);
            if (!Application.isPlaying)
            {
                currentYaw = yaw;
                currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
                Apply();
            }
        }
#endif
    }
}
