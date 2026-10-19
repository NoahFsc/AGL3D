using System.Collections.Generic;
using AGL3D.Bridge;
using UnityEngine;

namespace AGL3D.LotB
{
    /// <summary>
    /// Teinte un élément inspectable dans la couleur du survol, avec un court fondu.
    /// Ajouté automatiquement par <see cref="HoverHighlighter"/> sur l'élément survolé.
    /// Seuls les renderers de l'élément lui-même sont teintés (pas l'étiquette ni un
    /// éventuel repère enfant), via un MaterialPropertyBlock : les matériaux partagés
    /// ne sont jamais modifiés.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InspectableElement))]
    public class ElementHighlight : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private struct Target
        {
            public Renderer Renderer;
            public int PropertyId;
            public Color BaseColor;
        }

        private readonly List<Target> targets = new List<Target>();
        private MaterialPropertyBlock block;

        private Color color;
        private float strength;
        private float fadeDuration;
        private float amount;
        private bool hovered;

        /// <summary>Vrai tant que le pointeur est sur l'élément.</summary>
        public bool Hovered => hovered;

        public void Configure(Color highlightColor, float highlightStrength, float fade)
        {
            color = highlightColor;
            strength = highlightStrength;
            fadeDuration = fade;
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            enabled = true;
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            var renderers = GetComponents<Renderer>();
            if (renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();

            foreach (var r in renderers)
            {
                if (r.GetComponent<TextMesh>() != null) continue;
                var material = r.sharedMaterial;
                if (material == null) continue;
                int id = material.HasProperty(BaseColorId) ? BaseColorId
                    : material.HasProperty(ColorId) ? ColorId
                    : -1;
                if (id < 0) continue;
                targets.Add(new Target { Renderer = r, PropertyId = id, BaseColor = material.GetColor(id) });
            }
            enabled = false;
        }

        private void Update()
        {
            float goal = hovered ? 1f : 0f;
            amount = fadeDuration > 0f
                ? Mathf.MoveTowards(amount, goal, Time.unscaledDeltaTime / fadeDuration)
                : goal;
            Apply();
            if (Mathf.Approximately(amount, goal)) enabled = false;
        }

        private void OnDisable()
        {
            // Si l'objet est désactivé en plein fondu, on termine l'état visé.
            if (!hovered && amount > 0f)
            {
                amount = 0f;
                Apply();
            }
        }

        private void Apply()
        {
            foreach (var t in targets)
            {
                if (t.Renderer == null) continue;
                t.Renderer.GetPropertyBlock(block);
                block.SetColor(t.PropertyId, Color.Lerp(t.BaseColor, color, amount * strength));
                t.Renderer.SetPropertyBlock(block);
            }
        }
    }
}
