Shader "Custom/Rivers"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Specular ("Specular", Color) = (0.2, 0.2, 0.2)
		_BackgroundColor ("Background Color", Color) = (0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        LOD 200

        CGPROGRAM
        #pragma surface surf StandardSpecular alpha vertex:vert
        #pragma multi_compile _ HEX_MAP_EDIT_MODE
        #pragma target 3.0
        
        #include "WaterShared.cginc"
        #include "HexCellData.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float4 colour : COLOR;
            float2 visibility;
        };

        void vert (inout appdata_full v, out Input data) {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            
            float4 cell0 = GetCellData(v, 0);
            float4 cell1 = GetCellData(v, 1);

            data.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y;
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
			data.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y;
        }

        half _Glossiness;
        fixed3 _Specular;
        half3 _BackgroundColor;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            float explored = IN.visibility.y;
            fixed4 c = saturate(_Color + River(IN.uv_MainTex, _MainTex));
            o.Albedo = c.rgb * IN.visibility.x;
			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;
			o.Alpha = c.a * explored;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
