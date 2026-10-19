using UnityEngine;
using UnityEngine.InputSystem;

namespace AGL3D.LotA
{
    /// <summary>
    /// Caméra en orbite autour de l'appartement : clic droit maintenu et glisser pour
    /// tourner, molette pour zoomer. Le mouvement est lissé et l'inclinaison reste bornée
    /// pour que la caméra garde une vue plongeante sur la maquette.
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

        [Header("Lissage")]
        [Tooltip("Temps de réponse en secondes : plus petit = plus nerveux.")]
        [SerializeField] private float smoothTime = 0.1f;

        private float currentYaw, currentPitch, currentDistance;

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
            if (mouse == null) return;

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
            float t = smoothTime <= 0f ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothTime);
            currentYaw = Mathf.LerpAngle(currentYaw, yaw, t);
            currentPitch = Mathf.Lerp(currentPitch, pitch, t);
            currentDistance = Mathf.Lerp(currentDistance, distance, t);
            Apply();
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            transform.SetPositionAndRotation(target - rotation * Vector3.forward * currentDistance, rotation);
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
