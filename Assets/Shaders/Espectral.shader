//Brillo fantasmal aditivo con borde (fresnel). Lo usan las manos del fantasma, el láser, los marcadores y las partículas.
Shader "PartidaFantasma/Espectral" {
	Properties {
		_Color ("Color", Color) = (0.35, 0.85, 1, 1)
		_MainTex ("Textura", 2D) = "white" {}
		_Intensidad ("Intensidad", Float) = 1
		_Borde ("Fuerza del borde", Float) = 2.5
		_Nucleo ("Núcleo", Float) = 0.25
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Blend SrcAlpha One
		ZWrite Off
		Cull Back
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Borde, _Nucleo;

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _Intensidad)
			UNITY_INSTANCING_BUFFER_END(Props)

			struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; float3 v : TEXCOORD2; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };

			v2f vert (appdata v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.n = UnityObjectToWorldNormal(v.normal);
				o.v = normalize(WorldSpaceViewDir(v.vertex));
				o.color = v.color;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float intensidad = UNITY_ACCESS_INSTANCED_PROP(Props, _Intensidad);
				float nl = length(i.n);
				float f = nl > 0.01 ? 1.0 - saturate(abs(dot(normalize(i.n), normalize(i.v)))) : 0.5;
				float a = saturate(_Nucleo + pow(f, _Borde) * 1.4);
				fixed4 t = tex2D(_MainTex, i.uv);
				fixed4 c = _Color * i.color * t;
				c.a *= a * intensidad;
				return c;
			}
			ENDCG
		}
	}
	Fallback Off
}
