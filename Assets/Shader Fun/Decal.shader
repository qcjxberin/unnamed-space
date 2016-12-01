Shader "Example/Decal" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_NormalTex("Normal", 2D) = "bump" {}
		_Factor("Factor", Range(0,1)) = 0.5
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+1" "ForceNoShadowCasting" = "True" }
		LOD 200
		//Offset - 1, -1

		CGPROGRAM
		#pragma surface surf Lambert decal:blend

		sampler2D _MainTex;
		sampler2D _NormalTex;
		struct Input {
			float2 uv_MainTex;
			float2 uv_NormalTex;
		};

		half _Factor;

		void surf(Input IN, inout SurfaceOutput o) {
			half4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Normal = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex));
			o.Alpha = _Factor;
		}
		ENDCG
	}
}