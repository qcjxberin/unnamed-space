// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/FoggyTerrain" {
	Properties{
		_TerrainTex("TerrainTex", 2D) = "white"{}
		_TerrainTile("TerrainTex Tiling", Range(1,2000)) = 1
		_MaxDistance("Distance Fog Max", Range(100, 10000)) = 400
		_DistanceExponent("Distance Fog Exponent", Range(0.1, 10)) = 1
		_HeightExponent("Height Fog Exponent", Range(0.1, 1)) = 1
		_MaxHeight("Max Fog Height", Range(0, 1000)) = 500
		_MinHeight("Min Fog Height", Range(-500, 500)) = 0
		_FogAtten("Fog Attenuation", Range(0, 1)) = 1
		_Color1("Color1", Color) = (1,1,1,1)
		_Color2("Color2", Color) = (1,1,1,1)

		_Mie("Mie", Color) = (0,0,0, 0)
		_Factor("Power Base", Range(0,0.001)) = 0.5
		_Factor2("Multiplicative", Range(0,1)) = 0.5
		_Factor3("Subtractive", Range(0,3)) = 0.5
		_AddFactor("Additive", Range(0,100)) = 0.1
		_MieFactor("Mie Amount", Range(0,1)) = 0.5
		_ColorFactor("Color Factor", Range(0,4)) = 0.5
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_LightDir("Sun Direction", Vector) = (0, 0, 0, 0)
		_UpDir("Up Direction", Vector) = (0, 1, 0, 0)
	}
	SubShader{
		Tags{ "Queue" = "Geometry" "RenderType" = "Opaque" }
		CGPROGRAM
			
			#pragma target 4.0
			#pragma surface surf Standard
			#include "UnityCG.cginc"
			struct Input {
				float2 uv_TerrainTex;
			};

			sampler2D _TerrainTex;
			float _TerrainTile;
			void surf(Input IN, inout SurfaceOutputStandard o) {
				o.Albedo = tex2D(_TerrainTex, IN.uv_TerrainTex * _TerrainTile).rgb;
				//o.Albedo = 1;
				o.Alpha = 1;
				o.Smoothness = 0;
				o.Metallic = 0;
			}
		ENDCG
		
		Blend OneMinusDstColor One
		Pass{
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;

			//uniform sampler2D _CameraDepthTexture;
			float _MaxDistance;
			float _DistanceExponent;
			float _MaxHeight;
			float _MinHeight;
			float _FogAtten;
			float _HeightExponent;
			float _Factor;
			float _Factor2;
			float _Factor3;
			float _TexTile;
			float _MieFactor;
			float _ColorFactor;
			float _AddFactor;
			fixed4 _Color1;
			fixed4 _Color2;
			fixed4 _Mie;
			float4 _LightDir;
			float4 _UpDir;

			struct v2f {
				
				float4 worldPos : TEXCOORD1;
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata_base v) {
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				return o;
			}

			half4 frag(v2f i) : SV_TARGET{
				// Albedo comes from a texture tinted by color

				float3 localPixel = normalize(i.worldPos - _WorldSpaceCameraPos);
				float rawAngle = abs(asin(localPixel.y / length(localPixel))) * 57.2958;

				float day = dot(normalize(_UpDir), normalize(_LightDir * -1));

				float angleFraction = (rawAngle / 90);

				float a = clamp((1 - (clamp((rawAngle) / 90, 0, 1) + _Factor)) * _Factor2, 0, 1);
				a = (angleFraction + _Factor)*_Factor2;
				a = ((pow(_Factor, angleFraction) - (_Factor3 + day)) * _Factor2) + 0;

				//o.Alpha = a + clamp(dot(normalize(localPixel), normalize(_LightDir) * -1), -1, 1) * 0.4 + _AddFactor;
				a = pow(_Factor, angleFraction * _Factor3)*_Factor2 + 0.1;

				fixed4 c = lerp(_Color1, _Color2, clamp(a - _ColorFactor, 0, 1));
				//a = a + (1 - pow(_AddFactor, dot(normalize(localPixel), normalize(_LightDir)) * -0.35));
				float lightAngle = acos(dot(localPixel, _LightDir) / (length(localPixel) * length(_LightDir))) / (3.14159 / 2);
				lightAngle = dot(localPixel, _LightDir * -1);

				//c = lerp(c, clamp(c*_Mie*5, 0, 1), clamp(lightAngle - 0.2, 0, 1) * (1-day*0.5));
				c = lerp(c, _Mie, clamp(lightAngle - 0.2, 0, 1) * (1 - day*0.4));
				c *= pow(1-dot(localPixel, _UpDir * -1)-0.1, 1.8);
				c *=clamp(lightAngle, 0.2, 1);
				//o.Albedo = c * clamp((length(IN.worldPos - _WorldSpaceCameraPos)/500), 0, 1);
				
				//float4 r = lerp(tex2D(_TerrainTex, i.uv_TerrainTex*500), c, clamp(length(i.worldPos - _WorldSpaceCameraPos) / 500, 0, 1));
				float4 r = c * clamp(pow(clamp(length(i.worldPos - _WorldSpaceCameraPos) / _MaxDistance, 0, 1), _DistanceExponent), 0, 1);
				//return 0;
				return r * (1-pow(clamp((i.worldPos.y+_MinHeight)/_MaxHeight, 0, 1), _HeightExponent)) * _FogAtten *clamp(day + 1.1, 0, 1);
				//o.Albedo = lerp(tex2D(_TestTex, IN.uv_TestTex), c, 1);


				// Metallic and smoothness come from slider variables
				//o.Alpha = clamp(a + lightAngle*0.2, 0, 1);
				//o.Albedo = lerp(_Color1, _Color2, (1 - pow(_Factor, angleFraction * _Factor3)*_Factor2));
				//o.Albedo = _Color1;
			}
			ENDCG
		}
		
	}
	Fallback "Diffuse"
}
