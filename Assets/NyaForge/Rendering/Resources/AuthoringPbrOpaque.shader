Shader "NyaForge/AuthoringPbrOpaque"
{
    Properties
    {
        _MainTex ("Base color", 2D) = "white" {}
        _TintLinear ("Linear tint", Vector) = (1,1,1,1)
        _EmissionLinear ("Linear emission", Vector) = (0,0,0,0)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow
        #include "AuthoringPbr.cginc"
        ENDCG
    }
    Fallback Off
}
