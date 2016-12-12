Shader "Custom/Dome" {
	Properties{
		_Color1("Color1", Color) = (1,1,1,1)
		_Color2("Color2", Color) = (1,1,1,1)
		_Test("Test", Range(0, 0.001)) = 0
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
		Tags{ "Queue" = "Background" }
		LOD 200
		Cull Front
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Mine alpha:fade

		float4 LightingMine(SurfaceOutput s, float3 lightDir, float atten) {
			float4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}
		
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 4.0

		sampler2D _MainTex;
		//uniform sampler2D _CameraDepthTexture;
		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		float _Factor;
		float _Factor2;
		float _Factor3;
		float _Test;
		float _MieFactor;
		float _ColorFactor;
		float _AddFactor;
		fixed4 _Color1;
		fixed4 _Color2;
		fixed4 _Mie;
		float4 _LightDir;
		float4 _UpDir;

		void surf(Input IN, inout SurfaceOutput o) {
			
			// Albedo comes from a texture tinted by color
			
			float3 localPixel = normalize(IN.worldPos - _WorldSpaceCameraPos);
			//float rawAngle = abs(asin(localPixel.y / length(localPixel))) * 57.2958;
			float3 horizon = localPixel;
			horizon.y = -pow((_WorldSpaceCameraPos.y * _Test),0.7);
			//horizon.y = 0;
			//horizon.y += _Test;
			//horizon = normalize(horizon);
			float rawAngle = acos(dot(localPixel, horizon)/(length(localPixel)*length(horizon))) * 57.2958;
			//rawAngle += (3.1415926 / 2) - atan(1000 / _WorldSpaceCameraPos.y);
			//rawAngle += _Test;
			float day = dot(_UpDir, _LightDir * -1) - 0.05;

			float lightAngle = acos(dot(localPixel, _LightDir) / (length(localPixel) * length(_LightDir))) / (3.14159 / 2);
			lightAngle = dot(localPixel, _LightDir * -1);
			
			float angleFraction = (rawAngle / 90);
			
			float a = clamp((1 - (clamp((rawAngle)/90, 0, 1) + _Factor)) * _Factor2, 0, 1);
			//a = (angleFraction + _Factor)*_Factor2;
			//a = ((pow(_Factor, angleFraction) - (_Factor3 + day)) * _Factor2)+0;
			
			//o.Alpha = a + clamp(dot(normalize(localPixel), normalize(_LightDir) * -1), -1, 1) * 0.4 + _AddFactor;
			a = pow(_Factor, angleFraction * _Factor3)*_Factor2 + 0.1;
			
			fixed4 c = lerp(_Color1, _Color2, clamp(a-_ColorFactor, 0, 1));
			//a = a + (1 - pow(_AddFactor, dot(normalize(localPixel), normalize(_LightDir)) * -0.35));
			
			
			//c = lerp(c, clamp(c*_Mie*5, 0, 1), clamp(lightAngle - 0.2, 0, 1) * (1-day*0.5));
			c = lerp(c, _Mie, clamp(lightAngle - 0.1, 0, 1) * (1 - day*0.5));
			o.Albedo = c;

			// Metallic and smoothness come from slider variables
			o.Alpha = clamp(pow(clamp((a + lightAngle*0.3)*clamp(day+1,0,1), 0, 1), 1.5), 0, 1);
			//o.Albedo = lerp(_Color1, _Color2, (1 - pow(_Factor, angleFraction * _Factor3)*_Factor2));
			//o.Albedo = _Color1;
		}
		ENDCG
	}
		FallBack "Diffuse"
}