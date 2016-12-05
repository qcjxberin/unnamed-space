Shader "Custom/depth"
{
	Properties{
		_MainTex("", 2D) = "white" {}
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
			
			sampler2D _MainTex;
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
			
			

			fixed4 frag (v2f i) : COLOR
			{
				float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, i.projPos).r);
				half4 scene = tex2D(_MainTex, i.uv);
				half4 c;
				c.r = depth;
				c.g = depth;
				c.b = depth;
				c.a = 1;
				scene.a = 1;
				
				return depth;
			}
			ENDCG
		}
	}
}
