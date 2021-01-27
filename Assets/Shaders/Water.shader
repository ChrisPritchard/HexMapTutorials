﻿Shader "Custom/Water"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv1 = IN.worldPos.xz;
            uv1.y += _Time.y;
            float4 noise1 = tex2D(_MainTex, uv1 * 0.025);

            float2 uv2 = IN.worldPos.xz;
            uv2.x += _Time.y;
            float4 noise2 = tex2D(_MainTex, uv2 * 0.025);

            float blendwave = sin((IN.worldPos.x + IN.worldPos.z) * 0.1 + noise1.y + noise2.z + _Time.y);
            blendwave *= blendwave;

            float waves = 
                lerp(noise1.z, noise1.w, blendwave*0.5) + 
                lerp(noise2.x, noise2.y, blendwave*0.5);
            waves = smoothstep(0.75, 2, waves);

            fixed4 c = saturate(_Color + waves);
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
