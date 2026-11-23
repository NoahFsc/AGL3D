// Voile du mode Comparer (C6) : une couleur unie et transparente posée sur le maillage d'un
// élément dégradé. Le décalage de profondeur le fait passer devant l'élément sans scintiller ;
// les murs devant l'élément le cachent normalement.
Shader "AGL3D/Voile degradation"
{
    Properties
    {
        _Color ("Couleur", Color) = (0.87, 0.2, 0.22, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Back
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            fixed4 frag() : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
