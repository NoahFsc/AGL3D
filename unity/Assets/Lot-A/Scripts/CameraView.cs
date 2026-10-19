using System.Collections.Generic;
using UnityEngine;

namespace AGL3D.LotA
{
    /// <summary>
    /// Vue de caméra nommée : une pièce ("salon", "chambre", "cuisine") ou "overview"
    /// pour tout l'appartement. La position de l'objet est le point visé.
    /// <see cref="RoomFocus"/> y amène la caméra à la réception de <c>focusRoom</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraView : MonoBehaviour
    {
        private static readonly List<CameraView> Registry = new List<CameraView>();

        [Tooltip("Clé de pièce du contrat (salon, chambre, cuisine) ou overview.")]
        [SerializeField] private string key;
        [SerializeField] private float distance = 5f;
        [SerializeField] private float yaw;
        [SerializeField] private float pitch = 60f;

        public string Key => key;
        public Vector3 Target => transform.position;
        public float Distance => distance;
        public float Yaw => yaw;
        public float Pitch => pitch;

        public static CameraView Find(string viewKey)
        {
            return Registry.Find(v => v.key == viewKey);
        }

        private void OnEnable()
        {
            Registry.Add(this);
        }

        private void OnDisable()
        {
            Registry.Remove(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var camera = Target + Quaternion.Euler(pitch, yaw, 0f) * Vector3.back * distance;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Target, 0.15f);
            Gizmos.DrawLine(Target, camera);
            Gizmos.DrawWireCube(camera, Vector3.one * 0.3f);
        }
#endif
    }
}
