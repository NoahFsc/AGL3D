using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.Text.RegularExpressions;
#endif

namespace AGL3D.Bridge
{
    /// <summary>
    /// Marque un objet de la maquette comme élément d'état des lieux.
    /// <see cref="Key"/> est la clé stable partagée avec les données JSON côté Vue
    /// (ex. "salon.mur-ouest") : ne jamais la renommer sans mettre à jour le JSON.
    /// Un Collider est requis pour le picking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class InspectableElement : MonoBehaviour
    {
#if UNITY_EDITOR
        private static readonly Regex KeyFormat = new Regex(@"^[a-z0-9]+(-[a-z0-9]+)*\.[a-z0-9]+(-[a-z0-9]+)*$");
#endif
        private static readonly List<InspectableElement> Registry = new List<InspectableElement>();

        [Tooltip("Clé stable <piece>.<element> en kebab-case, ex. salon.mur-ouest")]
        [SerializeField] private string key;

        public string Key => key;

        /// <summary>Clé de la pièce (partie avant le point).</summary>
        public string RoomKey => string.IsNullOrEmpty(key) ? string.Empty : key.Split('.')[0];

        /// <summary>Éléments actifs dans la scène.</summary>
        public static IReadOnlyList<InspectableElement> All => Registry;

        public static InspectableElement Find(string elementKey)
        {
            return Registry.Find(e => e.key == elementKey);
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
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(key) && !KeyFormat.IsMatch(key))
            {
                Debug.LogWarning($"[InspectableElement] Clé \"{key}\" invalide sur {name} : attendu <piece>.<element> en kebab-case.", this);
            }
        }
#endif
    }
}
