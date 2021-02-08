Shader "Custom/Roads"
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
        Tags { 
            "RenderType"="Opaque" 
            "Queue"="Geometry+1" // will ensure roads get drawn after terrain
        }
        LOD 200
        Offset -1,-1

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows decal:blend
        #pragma target 3.0

        #include "HexCellData.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float visibility;
        };

        void vert (inout appdata_full v, out Input data) {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            
            float4 cell0 = GetCellData(v, 0);
            float4 cell1 = GetCellData(v, 1);

            data.visibility = cell0.x * v.color.x + cell1.x * v.color.y;
            data.visibility = lerp(0.25, 1, data.visibility); // ensures that the min visibility is 0.25
        }

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 noise = tex2D(_MainTex, IN.worldPos.xz * 0.025);
            fixed4 c = _Color * ((noise.y * 0.75 + 0.25) * IN.visibility);
            float blend = IN.uv_MainTex.x;
            blend *= noise.x + 0.5;
            blend = smoothstep(0.2, 0.6, blend);
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = blend;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
