using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace AGL3D.LotB
{
    /// <summary>
    /// Dit si le pointeur est sur le canvas Unity. Dans la page, il est faux quand la souris
    /// sort du canvas ou passe au-dessus d'un élément Vue (fiche, boutons d'accueil) : Unity ne
    /// reçoit plus les mouvements et garderait sinon la dernière position connue.
    /// Dans l'Éditeur, il suit seulement le focus de la fenêtre Game.
    /// </summary>
    public static class CanvasPointer
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int AGL3D_PointerInside();

        public static bool IsInside => AGL3D_PointerInside() != 0;
#else
        public static bool IsInside => Application.isFocused;
#endif
    }
}
