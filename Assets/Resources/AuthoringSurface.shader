Shader "NyaForge/AuthoringSurface"
{
    Properties { _Color ("Color", Color) = (0.22,0.72,0.69,1) _MainTex ("Base color", 2D) = "white" {} _DepthBias ("Depth bias", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Offset [_DepthBias], [_DepthBias]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Color;
            sampler2D _MainTex;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varying { float4 position : SV_POSITION; float3 viewPosition : TEXCOORD0; float2 uv : TEXCOORD1; };
            Varying vert(Input v)
            {
                Varying o; o.position = UnityObjectToClipPos(v.vertex);
                o.viewPosition = UnityObjectToViewPos(v.vertex); o.uv = v.uv; return o;
            }
            fixed4 frag(Varying i) : SV_Target
            {
                // Geometric face shading is preview-only; authored normals stay untouched.
                float3 n = normalize(cross(ddx(i.viewPosition), ddy(i.viewPosition)));
                if (dot(n, -i.viewPosition) < 0) n = -n;
                float diffuse = saturate(dot(n, normalize(float3(-0.4,0.7,0.6))));
                fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;
                // This profile is opaque, including when sampled by the UI viewport.
                // Keep authored alpha in the texture/export, not in the render target.
                return fixed4(baseColor.rgb * (0.35 + 0.65 * diffuse), 1);
            }
            ENDCG
        }
    }
}
