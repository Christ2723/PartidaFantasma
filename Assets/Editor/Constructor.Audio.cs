using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

//Sonidos sintetizados aquí: madera de las piezas, reloj, susurros del fantasma y la música ambiente (composición original)
public static partial class ConstructorProyecto {

	const int HZ = 22050;
	static AudioClip sPieza, sCaptura, sTomar, sReloj, sIlegal, sFantasma, sSusurro, sBoton, sEncoger, sCrecer, sJaque, sMate, sMusica;

	static AudioClip GuardarClip(string nombre, float[] datos, bool musica, float volumen = 0.9f) {
		float pico = datos.Max(m => Mathf.Abs(m));
		float escala = pico > 0f ? volumen / pico : 1f;
		string ruta = "Assets/Audio/" + nombre + ".wav";
		using (var bw = new BinaryWriter(File.Create(ruta))) {
			int bytes = datos.Length * 2;
			bw.Write("RIFF".ToCharArray());
			bw.Write(36 + bytes);
			bw.Write("WAVE".ToCharArray());
			bw.Write("fmt ".ToCharArray());
			bw.Write(16);
			bw.Write((short)1);
			bw.Write((short)1);
			bw.Write(HZ);
			bw.Write(HZ * 2);
			bw.Write((short)2);
			bw.Write((short)16);
			bw.Write("data".ToCharArray());
			bw.Write(bytes);
			foreach (float m in datos) bw.Write((short)(Mathf.Clamp(m * escala, -1f, 1f) * short.MaxValue));
		}
		AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
		var imp = (AudioImporter)AssetImporter.GetAtPath(ruta);
		var conf = imp.defaultSampleSettings;
		conf.loadType = musica ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
		conf.compressionFormat = AudioCompressionFormat.Vorbis;
		conf.quality = musica ? 0.5f : 0.8f;
		imp.defaultSampleSettings = conf;
		imp.SaveAndReimport();
		return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
	}

	static float Seno(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
	static float Diente(float fase) => 2f * (fase - Mathf.Floor(fase + 0.5f));
	static float Midi(float nota) => 440f * Mathf.Pow(2f, (nota - 69f) / 12f);
	static float R(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

	class PasoBajo {
		float y;
		readonly float a;
		public PasoBajo(float corte) => a = 1f - Mathf.Exp(-2f * Mathf.PI * corte / HZ);
		public float this[float x] => y += a * (x - y);
	}

	//Filtro pasa banda resonante (para el "cuerpo" de la madera)
	class Resonador {
		float y1, y2;
		readonly float a1, a2, g;
		public Resonador(float f, float q) {
			float w = 2f * Mathf.PI * f / HZ;
			float r = Mathf.Exp(-w / (2f * q));
			a1 = 2f * r * Mathf.Cos(w);
			a2 = -r * r;
			g = 1f - r;
		}
		public float this[float x] {
			get {
				float y = g * x + a1 * y1 + a2 * y2;
				y2 = y1;
				y1 = y;
				return y;
			}
		}
	}

	static AudioClip Efecto(string nombre, float segundos, Func<float, System.Random, float> muestra, float volumen = 0.9f) {
		var r = new System.Random(nombre.GetHashCode());
		int n = (int)(segundos * HZ);
		var d = new float[n];
		for (int i = 0; i < n; i++) d[i] = muestra(i / (float)HZ, r);
		int f = Mathf.Min(400, n / 4);
		for (int i = 0; i < f; i++) d[n - 1 - i] *= i / (float)f;
		return GuardarClip(nombre, d, false, volumen);
	}

	//Golpe de pieza de madera sobre el tablero
	static Func<float, System.Random, float> Madera(float tono) {
		var r1 = new Resonador(1150f * tono, 9f);
		var r2 = new Resonador(2350f * tono, 12f);
		var r3 = new Resonador(3900f * tono, 14f);
		var r4 = new Resonador(420f * tono, 6f);
		return (t, r) => {
			float golpe = t < 0.004f ? R(r) * (1f - t / 0.004f) : R(r) * 0.04f * Mathf.Exp(-t * 90f);
			float cuerpo = r1[golpe] * 1.2f + r2[golpe] * 0.8f + r3[golpe] * 0.5f + r4[golpe] * 1.4f;
			float sordo = Seno(170f * tono, t) * Mathf.Exp(-t * 45f) * 0.25f;
			return cuerpo * 6f + sordo + golpe * 0.15f;
		};
	}

	static void CrearSonidos() {
		sPieza = Efecto("Pieza", 0.22f, Madera(1f));
		var segunda = Madera(1.12f);
		var primera = Madera(0.9f);
		sCaptura = Efecto("Captura", 0.3f, (t, r) => primera(t, r) * 0.6f + (t > 0.07f ? segunda(t - 0.07f, r) : 0f));
		var pbTomar = new PasoBajo(2500f);
		var tic = Madera(1.4f);
		sTomar = Efecto("Tomar", 0.16f, (t, r) => tic(t, r) * 0.25f + pbTomar[R(r)] * Mathf.Exp(-t * 25f) * 0.5f, 0.5f);
		var clic1 = new Resonador(4200f, 20f);
		var clic2 = new Resonador(7100f, 25f);
		var toc = new Resonador(900f, 8f);
		sReloj = Efecto("Reloj", 0.1f, (t, r) => {
			float g = t < 0.002f ? R(r) : 0f;
			float g2 = t > 0.018f && t < 0.021f ? R(r) * 0.6f : 0f;
			return clic1[g + g2] * 5f + clic2[g] * 3f + toc[g2] * 4f;
		}, 0.8f);
		var pbIlegal = new PasoBajo(500f);
		sIlegal = Efecto("Ilegal", 0.4f, (t, r) => {
			float nota = t < 0.14f ? 330f : 247f;
			float tri = Mathf.Abs(Diente(nota * t)) * 2f - 1f;
			return tri * 0.35f * Mathf.Exp(-(t % 0.14f) * 8f) + pbIlegal[R(r)] * Mathf.Exp(-t * 20f) * 1.5f;
		}, 0.6f);
		var pbF = new PasoBajo(900f);
		var pbF2 = new PasoBajo(2600f);
		sFantasma = Efecto("Fantasma", 1.1f, (t, r) => {
			float env = Mathf.Sin(Mathf.Clamp01(t / 1.1f) * Mathf.PI);
			float ruido = Mathf.Lerp(pbF[R(r)], pbF2[R(r)], 0.5f + 0.5f * Mathf.Sin(t * 5f));
			float brillo = (Seno(1568f, t) + Seno(2093f * (1f + 0.003f * Mathf.Sin(t * 30f)), t) * 0.7f + Seno(2637f, t) * 0.5f) * 0.06f * (0.6f + 0.4f * Mathf.Sin(t * 22f));
			return (ruido * 2.2f + brillo) * env;
		}, 0.55f);
		var pbS = new PasoBajo(3000f);
		var forma1 = new Resonador(800f, 4f);
		var forma2 = new Resonador(1900f, 5f);
		sSusurro = Efecto("Susurro", 0.9f, (t, r) => {
			float env = Mathf.Pow(Mathf.Sin(Mathf.Clamp01(t / 0.9f) * Mathf.PI), 1.5f) * (0.6f + 0.4f * Mathf.Sin(t * 17f));
			float n = pbS[R(r)];
			return (forma1[n] * 3f + forma2[n] * 2f) * env;
		}, 0.45f);
		sBoton = Efecto("Boton", 0.18f, (t, r) => (Seno(880f, t) + Seno(1320f, t) * 0.4f) * Mathf.Exp(-t * 18f) * Mathf.Clamp01(t * 400f), 0.6f);
		sEncoger = Efecto("Encoger", 1.1f, (t, r) => {
			float f = Mathf.Lerp(900f, 110f, Mathf.Pow(t / 1.1f, 0.7f)) * (1f + 0.04f * Mathf.Sin(t * 40f));
			return (Seno(f, t) * 0.5f + Seno(f * 1.5f, t) * 0.2f) * Mathf.Sin(t / 1.1f * Mathf.PI);
		}, 0.6f);
		sCrecer = Efecto("Crecer", 1.1f, (t, r) => {
			float f = Mathf.Lerp(110f, 900f, Mathf.Pow(t / 1.1f, 1.4f)) * (1f + 0.04f * Mathf.Sin(t * 40f));
			return (Seno(f, t) * 0.5f + Seno(f * 1.5f, t) * 0.2f) * Mathf.Sin(t / 1.1f * Mathf.PI);
		}, 0.6f);
		sJaque = Efecto("Jaque", 1.4f, (t, r) => (Seno(1046f, t) + Seno(1046f * 2.76f, t) * 0.4f * Mathf.Exp(-t * 3f) + Seno(1046f * 5.4f, t) * 0.15f * Mathf.Exp(-t * 6f))
			* Mathf.Exp(-t * 2.6f) * Mathf.Clamp01(t * 300f), 0.45f);
		sMate = Efecto("Mate", 3.2f, (t, r) => {
			float s = 0f;
			float[] parc = { 1f, 1.47f, 2.09f, 2.56f, 3.21f, 4.1f };
			for (int i = 0; i < parc.Length; i++) s += Seno(98f * parc[i], t) * Mathf.Exp(-t * (0.8f + i * 0.5f)) / (1f + i * 0.6f);
			return s * Mathf.Clamp01(t * 200f);
		}, 0.7f);
		sMusica = CrearMusica();
	}

	//Música ambiente: colchón oscuro en La menor con notas de "armónica de cristal" (32 s, bucle sin costura)
	static AudioClip CrearMusica() {
		const float L = 32f, F = 3f;
		int n = (int)((L + F) * HZ), nl = (int)(L * HZ), nf = (int)(F * HZ);
		var buf = new float[n];
		//Acordes (MIDI): Am(add9) - Fmaj7 - Dm9 - Esus4
		int[][] acordes = {
			new[] { 45, 52, 57, 59, 64 }, new[] { 41, 48, 57, 60, 64 }, new[] { 38, 50, 53, 57, 64 }, new[] { 40, 47, 52, 57, 59 } };
		var r = new System.Random(3);
		var pb = new PasoBajo(700f);
		var fases = new float[5, 3];
		for (int i = 0; i < n; i++) {
			float t = i / (float)HZ;
			float pos = (t % L) / 8f;
			int a = (int)pos % 4;
			int sig = (a + 1) % 4;
			float dentro = pos - Mathf.Floor(pos);
			float mezcla = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dentro - 0.8f) / 0.2f));
			float pad = 0f;
			for (int v = 0; v < 5; v++) {
				float nota = Mathf.Lerp(acordes[a][v], acordes[sig][v], mezcla);
				float f = Midi(nota);
				for (int d = 0; d < 3; d++) {
					float det = 1f + (d - 1) * 0.0045f;
					fases[v, d] += f * det / HZ;
					pad += Diente(fases[v, d]) * (v == 0 ? 0.9f : 0.45f);
				}
			}
			float lfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * t / 8f);
			buf[i] = pb[pad] * 0.18f * lfo;
			//Bajo profundo
			buf[i] += Seno(Midi(33f), t) * 0.22f * (0.8f + 0.2f * Mathf.Sin(t * 0.7f));
		}
		//Notas sueltas de cristal (pentatónica de La menor)
		int[] escala = { 69, 72, 74, 76, 79, 81, 84 };
		for (float t0 = 1.5f; t0 < L + F - 4f; t0 += 2.2f + (float)r.NextDouble() * 2.6f) {
			float f = Midi(escala[r.Next(escala.Length)]);
			int i0 = (int)(t0 * HZ);
			for (int k = 0; k < (int)(3.5f * HZ) && i0 + k < n; k++) {
				float t = k / (float)HZ;
				float env = Mathf.Clamp01(t / 0.35f) * Mathf.Exp(-t * 1.1f);
				buf[i0 + k] += (Seno(f, t) + Seno(f * 2f, t) * 0.25f + Seno(f * 3.01f, t) * 0.08f) * env * 0.11f * (1f + 0.15f * Seno(5f, t));
			}
		}
		var salida = new float[nl];
		for (int i = 0; i < nl; i++) salida[i] = buf[i];
		for (int i = 0; i < nf; i++) {
			float k = i / (float)nf;
			salida[i] = buf[i] * k + buf[nl + i] * (1f - k);
		}
		return GuardarClip("MusicaAmbiente", salida, true, 0.8f);
	}
}
