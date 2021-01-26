Shader "Custom/Terrain" // default standard shader with two lines assigning the vertex colour to the albedo
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5

        UNITY_DECLARE_TEX2DARRAY(_MainTex);

        struct Input
        {
            float4 colour : COLOR; // this gets populated automatically because of its type, with the assigned colour for the vertex in the mesh
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.worldPos.xz * 0.02;
            fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uv, 0));
            o.Albedo = c.rgb * _Color;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
