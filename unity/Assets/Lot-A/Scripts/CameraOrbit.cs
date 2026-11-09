using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AGL3D.LotA
{
    /// <summary>
    /// Caméra en orbite autour de l'appartement : clic droit maintenu et glisser pour
    /// tourner, molette pour zoomer. Le mouvement est lissé et l'inclinaison reste bornée
    /// pour que la caméra garde une vue plongeante sur la maquette.
    /// La caméra reste au-dessus du sommet des murs marqués <see cref="CameraObstacle"/> :
    /// l'inclinaison minimale augmente quand on se rapproche, sans décentrer la cible.
    /// <see cref="MoveTo"/> l'amène vers une autre vue.
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
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 16f;
        [Tooltip("Part de la distance parcourue par cran de molette.")]
        [SerializeField] private float zoomStep = 0.15f;

        [Header("Murs")]
        [Tooltip("Marge entre la caméra et le sommet du plus haut mur (CameraObstacle).")]
        [SerializeField] private float wallClearance = 0.4f;

        [Header("Lissage")]
        [Tooltip("Temps de réponse en secondes : plus petit = plus nerveux.")]
        [SerializeField] private float smoothTime = 0.1f;

        private float currentYaw, currentPitch, currentDistance;
        private float? wallTop;

        // Déplacement en cours vers une autre vue (MoveTo).
        private bool moving;
        private float moveTime, moveDuration;
        private Vector3 fromTarget, toTarget;
        private float fromYaw, toYaw, fromPitch, toPitch, fromDistance, toDistance;
        private Action onArrived;

        /// <summary>Vrai pendant un déplacement lancé par <see cref="MoveTo"/>.</summary>
        public bool IsMoving => moving;

        /// <summary>Orientation horizontale visée, en degrés.</summary>
        public float Yaw => yaw;

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
            toDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
            toPitch = Mathf.Clamp(newPitch, MinPitchFor(toDistance, newTarget), maxPitch);
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
                pitch = Mathf.Clamp(pitch - delta.y * rotateSpeed, MinPitchFor(distance, target), maxPitch);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f) && IsPointerInside(mouse))
            {
                // L'Input System ramène un cran de molette à [-1, 1] sur toutes les plateformes
                // (InputSettings.scrollDeltaBehavior) ; les trackpads envoient des valeurs plus petites.
                float notches = Mathf.Clamp(scroll, -1f, 1f);
                distance = Mathf.Clamp(distance * (1f - notches * zoomStep), minDistance, maxDistance);
                // En se rapprochant, la caméra se redresse pour rester au-dessus des murs.
                pitch = Mathf.Max(pitch, MinPitchFor(distance, target));
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
            // Filet de sécurité pendant le lissage et les déplacements : on ne descend jamais
            // sous l'inclinaison qui garde la caméra au-dessus des murs.
            float effectivePitch = Mathf.Max(currentPitch, MinPitchFor(currentDistance, target));
            var rotation = Quaternion.Euler(effectivePitch, currentYaw, 0f);
            transform.SetPositionAndRotation(target + rotation * Vector3.back * currentDistance, rotation);
        }

        /// <summary>
        /// Inclinaison minimale pour que la caméra, à <paramref name="dist"/> de
        /// <paramref name="aim"/>, reste au-dessus des murs avec la marge voulue.
        /// </summary>
        private float MinPitchFor(float dist, Vector3 aim)
        {
            float height = WallTop + wallClearance - aim.y;
            if (height <= 0f || dist <= 0f) return minPitch;
            float pitchForHeight = Mathf.Asin(Mathf.Clamp01(height / dist)) * Mathf.Rad2Deg;
            return Mathf.Clamp(pitchForHeight, minPitch, maxPitch);
        }

        /// <summary>Sommet du plus haut <see cref="CameraObstacle"/>, lu une fois.</summary>
        private float WallTop
        {
            get
            {
                if (wallTop == null)
                {
                    float top = 0f;
                    foreach (var obstacle in FindObjectsByType<CameraObstacle>(FindObjectsSortMode.None))
                        top = Mathf.Max(top, obstacle.GetComponent<Collider>().bounds.max.y);
                    wallTop = top;
                }
                return wallTop.Value;
            }
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
            wallClearance = Mathf.Max(0f, wallClearance);
            wallTop = null;
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
