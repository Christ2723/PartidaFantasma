//Textura sin iluminación ni niebla (la luna lejana)
Shader "PartidaFantasma/SinNiebla" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Textura", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float luz : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
			v2f vert (appdata v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				float3 n = UnityObjectToWorldNormal(v.normal);
				o.luz = saturate(dot(n, normalize(float3(-0.6, 0.35, 0.7))) * 1.1 + 0.08);
				return o;
			}
			fixed4 frag (v2f i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				return tex2D(_MainTex, i.uv) * _Color * i.luz;
			}
			ENDCG
		}
	}
}
