Shader "Custom/depth"
{
	Properties{
		_MainTex("", 2D) = "white" {}
		_Color1("Color", Color) = (0,0,0,0)
		_Tex2("Atmo", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque"}

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			uniform sampler2D _CameraDepthTexture;
			sampler2D _Tex2;
			sampler2D _MainTex;
			half4 _Color1;
			struct v2f
			{
				float4 projPos : TEXCOORD1;
				float4 pos : SV_POSITION;
				half2 uv : TEXCOORD0;
			};

			v2f vert (appdata_base v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.projPos = ComputeScreenPos(o.pos);
				o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o;
			}
			
			

			half4 frag (v2f i) : COLOR
			{
				//clip(-1);
				half4 scene = tex2D(_MainTex, i.uv);
				//half4 atmo = tex2D(_Tex2, i.uv);
				//return atmo;
				
				float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, i.projPos).r);
				//return depth;
				
				
				
				half4 c;
				c.r = depth;
				c.g = depth;
				c.b = depth;
				c.a = 1;
				//scene.a = 1;
				//return 1 - depth;
				//return depth;
				//return lerp(scene, _Color1*1, depth);
				return scene + _Color1*depth;
				
			}
			ENDCG
		}
	}
}
