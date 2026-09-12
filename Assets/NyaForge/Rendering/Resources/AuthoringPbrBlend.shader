Shader "NyaForge/AuthoringPbrBlend"
{
    Properties
    {
        _MainTex ("Base color", 2D) = "white" {}
        _TintLinear ("Linear tint", Vector) = (1,1,1,1)
        _EmissionLinear ("Linear emission", Vector) = (0,0,0,0)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        [HideInInspector] _ColorMask ("Target channels", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        // Preview preserves its composited background alpha; imported materials write RGBA.
        ColorMask [_ColorMask]
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #include "AuthoringPbr.cginc"
        ENDCG
    }
    Fallback Off
}
