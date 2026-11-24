//Cielo del vacío: estrellas que titilan y nebulosas, calculadas por dirección (sin costuras y nítidas en VR)
Shader "PartidaFantasma/Cielo" {
	Properties {
		_ColorArriba ("Color arriba", Color) = (0.02, 0.025, 0.06, 1)
		_ColorAbajo ("Color abajo", Color) = (0.0, 0.0, 0.01, 1)
		_Nebulosa1 ("Nebulosa 1", Color) = (0.25, 0.08, 0.45, 1)
		_Nebulosa2 ("Nebulosa 2", Color) = (0.05, 0.25, 0.45, 1)
		_Densidad ("Densidad de estrellas", Float) = 120
	}
	SubShader {
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off ZWrite Off
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			fixed4 _ColorArriba, _ColorAbajo, _Nebulosa1, _Nebulosa2;
			float _Densidad;

			struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

			v2f vert (appdata v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.dir = v.vertex.xyz;
				return o;
			}

			float hash3(float3 p) {
				p = frac(p * 0.3183099 + 0.1);
				p *= 17.0;
				return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
			}

			float ruido(float3 x) {
				float3 i = floor(x);
				float3 f = frac(x);
				f = f * f * (3.0 - 2.0 * f);
				return lerp(lerp(lerp(hash3(i + float3(0,0,0)), hash3(i + float3(1,0,0)), f.x),
				                 lerp(hash3(i + float3(0,1,0)), hash3(i + float3(1,1,0)), f.x), f.y),
				            lerp(lerp(hash3(i + float3(0,0,1)), hash3(i + float3(1,0,1)), f.x),
				                 lerp(hash3(i + float3(0,1,1)), hash3(i + float3(1,1,1)), f.x), f.y), f.z);
			}

			float fbm(float3 p) {
				float v = 0.0, a = 0.5;
				for (int k = 0; k < 4; k++) { v += a * ruido(p); p *= 2.03; a *= 0.5; }
				return v;
			}

			float capaEstrellas(float3 d, float escala, float umbral) {
				float3 p = d * escala;
				float3 celda = floor(p);
				float h = hash3(celda);
				if (h < umbral) return 0.0;
				float3 centro = celda + 0.5 + (float3(hash3(celda + 7.1), hash3(celda + 3.7), hash3(celda + 1.3)) - 0.5) * 0.6;
				float dist = length(p - centro);
				float brillo = (h - umbral) / (1.0 - umbral);
				float tit = 0.75 + 0.25 * sin(_Time.y * (1.5 + h * 4.0) + h * 40.0);
				return smoothstep(0.09 + brillo * 0.06, 0.0, dist) * brillo * tit;
			}

			fixed4 frag (v2f i) : SV_Target {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				float3 d = normalize(i.dir);
				float3 col = lerp(_ColorAbajo.rgb, _ColorArriba.rgb, saturate(d.y * 0.5 + 0.6));
				float n = fbm(d * 2.2 + float3(3.1, 0.0, 1.7));
				float n2 = fbm(d * 4.5 + float3(-2.0, 5.0, 0.3));
				float banda = exp(-pow((d.y - 0.15 + (n - 0.5) * 0.6) * 2.4, 2.0));
				col += _Nebulosa1.rgb * pow(saturate(n * 1.3 - 0.35), 2.0) * 0.9 * (0.4 + banda);
				col += _Nebulosa2.rgb * pow(saturate(n2 * 1.4 - 0.55), 2.0) * 0.8;
				float s = capaEstrellas(d, _Densidad, 0.86) + capaEstrellas(d, _Densidad * 2.3, 0.80) * 0.6 + capaEstrellas(d, _Densidad * 0.45, 0.965) * 1.6;
				float3 tinte = lerp(float3(0.75, 0.85, 1.0), float3(1.0, 0.85, 0.7), hash3(floor(d * _Densidad)));
				col += tinte * s * (0.7 + banda * 0.6);
				return fixed4(col, 1.0);
			}
			ENDCG
		}
	}
	Fallback Off
}
