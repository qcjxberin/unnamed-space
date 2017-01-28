Shader "Custom/MyShader" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_PaintTex("Paint (RGB)", 2D) = "white" {}
		_AOTex("Occlusion (R)", 2D) = "white" {}
		_EmissionTex("Emission (RGBA)", 2D) = "black" {}
		_CustomTex("Custom (RGB)", 2D) = "white" {}
		_MetalTex("Metallic (A)", 2D) = "black" {}
		_SmoothnessM("Smoothness of metal", Range(0,1)) = 0.5
		_SmoothnessA("Smoothness of nonmetal", Range(0,1)) = 0
		[Toggle] _AllowMetalPaint("Allow Metal to be painted?", Float) = 0
		[HideInInspector]_Normal("Target Normal Vector", Vector) = (0, 0, 0)
		[HideInInspector]_Target("Target Transform", Vector) = (0, 0, 0)
		
		_MinDist("Minimum Distance", Float) = 1
		_MaxDist("Maximum Distance", Float) = 10
		_MaxBlur("Maximum Blur", Range(0,1)) = 1
		_Interval("Interval", Range(0, 0.1)) = 0.005
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		
		#pragma target 4.0

		

		sampler2D _MainTex;
		sampler2D _PaintTex;
		sampler2D _CustomTex;
		sampler2D _MetalTex;
		sampler2D _AOTex;
		sampler2D _EmissionTex;
		struct Input {
			float2 uv_MainTex;
			float2 uv_PaintTex;
			float2 uv_CustomTex;
			float2 uv_MetalTex;
			float2 uv_AOTex;
			float2 uv_EmissionTex;
			float3 worldPos;
			
		};

		

		half _SmoothnessM;
		half _AllowMetalPaint;
		float3 _Target;
		float3 _Normal;
		float _MinDist;
		float _MaxDist;
		float _MaxBlur;
		float _Interval;
		//float _WorldSpaceCameraPos;

		


		void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 planeToPoint = -1 * (_Target - IN.worldPos);
			
			float dist = dot(_Normal, planeToPoint) / length(_Normal);
			float firstDist = dist;
			firstDist = clamp(dist, _MinDist, _MaxDist);
			float distFraction = clamp((firstDist - _MinDist) / _MaxDist, 0, 1);
			float distFraction2 = (floor(distFraction / _Interval) * _Interval) * _MaxBlur;

			//float fin = floor(distFraction2 / interval) * interval;
			float fin = distFraction2;
			
			//dist = floor(distFraction2 / interval) * interval;
			//dist = max()
			
			//float roundTo = max(round(distance(_Target, IN.worldPos)* initialMultiplier - distanceOffset), 0.01) / divisor;
			//float roundTo = max(round(distance(_Target.xyz, IN.worldPos.xyz)), 0.0001);
			//float roundTo = IN.screenPos.z;

			

			IN.uv_MainTex = ceil(IN.uv_MainTex / (fin + 0.0001)) * (fin + 0.0001);
			IN.uv_PaintTex = ceil(IN.uv_PaintTex / (fin + 0.0001)) * (fin + 0.0001);
			IN.uv_CustomTex = ceil(IN.uv_CustomTex / (fin + 0.0001)) * (fin + 0.0001);
			IN.uv_MetalTex = ceil(IN.uv_MetalTex / (fin + 0.0001)) * (fin + 0.0001);
			
			
			_AllowMetalPaint = (1 - _AllowMetalPaint);
			// Albedo comes from a texture tinted by color
			fixed3 temp;
			fixed4 main = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 paint = tex2D(_PaintTex, IN.uv_PaintTex);
			fixed4 custom = tex2D(_CustomTex, IN.uv_CustomTex);
			fixed4 metal = tex2D(_MetalTex, IN.uv_MetalTex);
			fixed4 ao = tex2D(_AOTex, IN.uv_AOTex);
			fixed4 em = tex2D(_EmissionTex, IN.uv_EmissionTex);
			temp = lerp(paint.rgb, custom.rgb, min(custom.a, 1 - metal.a * _AllowMetalPaint));
			temp = lerp(temp, main.rgb, main.a);
			//temp = temp * main.rgba;
			

			o.Albedo = temp;
			
			
			// Metallic and smoothness come from slider variables
			o.Metallic = metal.a;
			o.Smoothness = metal.a * _SmoothnessM;
			o.Emission = em;
			o.Alpha = ao.a;
			o.Occlusion = ao;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
