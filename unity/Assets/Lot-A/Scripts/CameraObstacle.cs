using UnityEngine;

namespace AGL3D.LotA
{
    /// <summary>
    /// Marque un mur ou une cloison que la caméra ne doit pas traverser.
    /// <see cref="CameraOrbit"/> rapproche la caméra quand un de ces objets se trouve
    /// entre elle et le point visé. Un Collider est requis.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class CameraObstacle : MonoBehaviour
    {
    }
}
