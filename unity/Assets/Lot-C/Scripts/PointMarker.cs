using UnityEngine;
using UnityEngine.UI;

namespace AGL3D.LotC
{
    /// <summary>
    /// Étiquette posée sur un élément noté : « 4 · Évier » dans un cadre arrondi, reliée à
    /// l'élément par une tige. Verte (bon), orange (usage) ou rouge (mauvais) ; bleue avec une
    /// goutte ou un éclair pour les compteurs. Toujours tournée vers la caméra, à taille fixe à
    /// l'écran. Enfant de l'élément, elle se clique comme lui : le clic remonte jusqu'à son
    /// <c>InspectableElement</c> et rouvre la fiche.
    /// </summary>
    [DisallowMultipleComponent]
    public class PointMarker : MonoBehaviour
    {
        public enum Genre { Element, CompteurEau, CompteurElectricite }

        [SerializeField] private RectTransform etiquette;
        [SerializeField] private RectTransform cadre;
        [SerializeField] private Image bordure;
        [SerializeField] private Image fond;
        [SerializeField] private Image tige;
        [SerializeField] private Image icone;
        [SerializeField] private Text texte;
        [SerializeField] private BoxCollider zoneClic;
        [SerializeField] private Sprite goutte;
        [SerializeField] private Sprite eclair;
        [Tooltip("Taille à l'écran : 1 = une unité du cadre pour un pixel CSS de la page.")]
        [SerializeField] private float echelleEcran = 1.3f;

        private struct Teinte
        {
            public Color Fond, Bordure, Texte;
            public Teinte(string fond, string bordure, string texte)
            {
                ColorUtility.TryParseHtmlString(fond, out Fond);
                ColorUtility.TryParseHtmlString(bordure, out Bordure);
                ColorUtility.TryParseHtmlString(texte, out Texte);
            }
        }

        private static readonly Teinte Bon = new Teinte("#1C3A1A", "#2E5C27", "#5DBB3F");
        private static readonly Teinte Usage = new Teinte("#3A2310", "#6B3F17", "#F0A54A");
        private static readonly Teinte Mauvais = new Teinte("#3D1517", "#6E242A", "#F0656C");
        private static readonly Teinte Compteur = new Teinte("#12284A", "#1F4275", "#7FB2F5");

        private Camera cam;

        /// <summary>État affiché : "bon", "usage" ou "mauvais".</summary>
        public string Etat { get; private set; }

        /// <summary>Met à jour le texte, l'icône et les couleurs. Un état inconnu est signalé et ignoré.</summary>
        public void Afficher(int numero, string libelle, string etat, Genre genre)
        {
            Teinte teinte;
            if (genre != Genre.Element) teinte = Compteur;
            else if (etat == "bon") teinte = Bon;
            else if (etat == "usage") teinte = Usage;
            else if (etat == "mauvais") teinte = Mauvais;
            else
            {
                Debug.LogWarning($"[PointMarker] État inconnu : \"{etat}\".", this);
                return;
            }
            Etat = etat;

            texte.text = $"{numero} · {libelle}";
            texte.color = teinte.Texte;
            fond.color = teinte.Fond;
            bordure.color = teinte.Bordure;
            tige.color = teinte.Texte;

            icone.gameObject.SetActive(genre != Genre.Element);
            icone.sprite = genre == Genre.CompteurEau ? goutte : eclair;
            icone.color = teinte.Texte;
        }

        /// <summary>Tourne l'étiquette vers la caméra et garde sa taille à l'écran, puis cale la zone de clic.</summary>
        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            etiquette.rotation = cam.transform.rotation;
            float distance = Mathf.Max(Vector3.Dot(etiquette.position - cam.transform.position, cam.transform.forward), 0.01f);
            float hauteurVisible = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            // Sur un écran Retina, Screen.height compte les pixels physiques : Screen.dpi / 96
            // ramène l'étiquette à la même taille que le reste de la page.
            float densite = Screen.dpi > 0f ? Mathf.Max(Screen.dpi / 96f, 1f) : 1f;
            etiquette.localScale = Vector3.one * (hauteurVisible / Mathf.Max(Screen.height, 1) * densite * echelleEcran);

            Rect zone = cadre.rect;
            Vector2 centre = (Vector2)cadre.localPosition + zone.center;
            zoneClic.center = new Vector3(centre.x, centre.y, 0f);
            zoneClic.size = new Vector3(zone.width, zone.height, 1f);
        }
    }
}
