using System.Collections.Generic;
using AGL3D.Bridge;
using UnityEngine;

namespace AGL3D.LotC
{
    /// <summary>
    /// Mode Comparer (C6) : les éléments que <c>setPoints</c> marque <c>degrade</c> passent
    /// progressivement au rouge, l'un après l'autre, pour un effet « avant / après ».
    ///
    /// Le rouge est un voile transparent posé sur le maillage de l'élément (matériau
    /// <c>Resources/VoileDegradation</c>), et non une teinte de son matériau : la surbrillance
    /// du survol (lot B) teinte la couleur de l'élément, et les deux effets ne s'effacent pas
    /// l'un l'autre. Le voile n'a pas de collider : le clic passe toujours à l'élément.
    /// Hors du mode Comparer, ou quand un élément n'est plus dégradé, le voile s'efface.
    /// </summary>
    [DisallowMultipleComponent]
    public class PassageAuRouge : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Color rouge = new Color(0.87f, 0.2f, 0.22f);
        [Tooltip("Opacité du voile une fois l'élément passé au rouge.")]
        [SerializeField, Range(0f, 1f)] private float opacite = 0.85f;
        [Tooltip("Durée du passage au rouge d'un élément, en secondes.")]
        [SerializeField] private float duree = 1.2f;
        [Tooltip("Délai entre deux éléments qui passent au rouge, en secondes.")]
        [SerializeField] private float decalage = 0.35f;
        [Tooltip("Durée de l'effacement du voile, en secondes.")]
        [SerializeField] private float dureeEffacement = 0.3f;

        /// <summary>Voile d'un élément : ses renderers et l'avancement de son fondu.</summary>
        private class Voile
        {
            public readonly List<Renderer> Renderers = new List<Renderer>();
            /// <summary>De 0 (invisible) à 1 (rouge complet).</summary>
            public float Valeur;
            public bool Rouge;
            /// <summary>Instant (Time.time) où le passage au rouge commence.</summary>
            public float Debut;
        }

        private readonly Dictionary<string, Voile> voiles = new Dictionary<string, Voile>();
        private readonly List<string> aRetirer = new List<string>();
        private MaterialPropertyBlock bloc;
        private Material materiau;
        private WebBridge bridge;

        private void Awake()
        {
            bloc = new MaterialPropertyBlock();
            materiau = Resources.Load<Material>("VoileDegradation");
            if (materiau == null) Debug.LogWarning("[PassageAuRouge] Matériau VoileDegradation introuvable.", this);
        }

        private void Start()
        {
            bridge = WebBridge.Instance;
            if (bridge == null) return;
            bridge.InitReceived += OnInit;
            bridge.SetPointsReceived += OnSetPoints;
        }

        private void OnDestroy()
        {
            if (bridge == null) return;
            bridge.InitReceived -= OnInit;
            bridge.SetPointsReceived -= OnSetPoints;
        }

        /// <summary>Un changement de mode efface les voiles ; Comparer les relance avec setPoints.</summary>
        private void OnInit(InitPayload payload)
        {
            foreach (var voile in voiles.Values) voile.Rouge = false;
            enabled = true;
        }

        private void OnSetPoints(SetPointsPayload payload)
        {
            var degrades = new HashSet<string>();
            float debut = Time.time;
            foreach (var point in payload.points ?? new PointData[0])
            {
                if (point == null || !point.degrade || string.IsNullOrEmpty(point.key)) continue;
                degrades.Add(point.key);

                if (!voiles.TryGetValue(point.key, out var voile))
                {
                    var element = InspectableElement.Find(point.key);
                    if (element == null || materiau == null) continue;
                    voile = Creer(element);
                    voiles[point.key] = voile;
                }
                if (voile.Rouge) continue;
                // Les nouveaux éléments dégradés passent au rouge l'un après l'autre.
                voile.Rouge = true;
                voile.Debut = debut;
                debut += decalage;
            }

            foreach (var entree in voiles)
            {
                if (!degrades.Contains(entree.Key)) entree.Value.Rouge = false;
            }
            enabled = true;
        }

        /// <summary>Double chaque maillage de l'élément d'un voile, enfant de ce maillage.</summary>
        private Voile Creer(InspectableElement element)
        {
            var voile = new Voile();
            foreach (var filtre in element.GetComponentsInChildren<MeshFilter>())
            {
                // Pas de voile sur l'étiquette ni sur un voile déjà posé.
                if (filtre.GetComponentInParent<PointMarker>() != null) continue;
                if (filtre.name == "Voile" || filtre.sharedMesh == null) continue;

                var objet = new GameObject("Voile", typeof(MeshFilter), typeof(MeshRenderer));
                objet.transform.SetParent(filtre.transform, false);
                objet.GetComponent<MeshFilter>().sharedMesh = filtre.sharedMesh;
                var rendu = objet.GetComponent<MeshRenderer>();
                rendu.sharedMaterial = materiau;
                rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rendu.receiveShadows = false;
                rendu.enabled = false;
                voile.Renderers.Add(rendu);
            }
            return voile;
        }

        private void Update()
        {
            bool enCours = false;
            aRetirer.Clear();
            foreach (var entree in voiles)
            {
                var voile = entree.Value;
                if (voile.Rouge)
                {
                    if (Time.time < voile.Debut) { enCours = true; continue; }
                    voile.Valeur = Mathf.MoveTowards(voile.Valeur, 1f, Time.deltaTime / Mathf.Max(duree, 0.01f));
                }
                else
                {
                    voile.Valeur = Mathf.MoveTowards(voile.Valeur, 0f, Time.deltaTime / Mathf.Max(dureeEffacement, 0.01f));
                }
                Appliquer(voile);
                if (voile.Rouge ? voile.Valeur < 1f : voile.Valeur > 0f) enCours = true;
                if (!voile.Rouge && voile.Valeur <= 0f) aRetirer.Add(entree.Key);
            }

            foreach (var key in aRetirer)
            {
                foreach (var rendu in voiles[key].Renderers)
                {
                    if (rendu != null) Destroy(rendu.gameObject);
                }
                voiles.Remove(key);
            }
            // Rien ne bouge : on s'arrête jusqu'au prochain message.
            if (!enCours) enabled = false;
        }

        private void Appliquer(Voile voile)
        {
            var couleur = rouge;
            couleur.a = opacite * Mathf.SmoothStep(0f, 1f, voile.Valeur);
            foreach (var rendu in voile.Renderers)
            {
                if (rendu == null) continue;
                rendu.enabled = couleur.a > 0f;
                rendu.GetPropertyBlock(bloc);
                bloc.SetColor(ColorId, couleur);
                rendu.SetPropertyBlock(bloc);
            }
        }
    }
}
