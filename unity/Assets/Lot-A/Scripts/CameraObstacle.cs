using UnityEngine;

namespace AGL3D.LotA
{
    /// <summary>
    /// Marque un mur ou une cloison. <see cref="CameraOrbit"/> garde la caméra au-dessus du
    /// sommet du plus haut de ces objets, pour qu'elle ne traverse jamais un mur.
    /// Un Collider est requis.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class CameraObstacle : MonoBehaviour
    {
    }
}
