using System.IO;
using UnityEditor;
using UnityEngine;

//Texturas procedurales: maderas del tablero y de las piezas, mesa, alfombra, mármol del suelo, papel y luna
public static partial class ConstructorProyecto {

	static Texture2D texMaderaClara, texMaderaOscura, texMarco, texMesa, texBoj, texEbano, texAlfombra, texSuelo, texRoca, texLuna, texTerciopelo, texPapel, texPunto;

	// ------------------------------------------------ Ruido

	static float Hash(int x, int y, int semilla) {
		unchecked {
			int h = x * 374761393 + y * 668265263 + semilla * 982451653;
			h = (h ^ (h >> 13)) * 1274126177;
			return ((h ^ (h >> 16)) & 0xffffff) / (float)0xffffff;
		}
	}

	//Ruido de valor que se repite cada "periodo" celdas (para texturas que se repiten sin costura)
	static float RuidoP(float x, float y, int periodo, int semilla) {
		int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
		float fx = x - x0, fy = y - y0;
		fx = fx * fx * (3f - 2f * fx);
		fy = fy * fy * (3f - 2f * fy);
		int M(int v) => ((v % periodo) + periodo) % periodo;
		float a = Hash(M(x0), M(y0), semilla), b = Hash(M(x0 + 1), M(y0), semilla);
		float c = Hash(M(x0), M(y0 + 1), semilla), d = Hash(M(x0 + 1), M(y0 + 1), semilla);
		return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
	}

	static float FbmP(float u, float v, int periodo, int octavas, int semilla) {
		float s = 0f, a = 0.5f, total = 0f;
		for (int o = 0; o < octavas; o++) {
			int p = periodo << o;
			s += a * RuidoP(u * p, v * p, p, semilla + o * 17);
			total += a;
			a *= 0.5f;
		}
		return s / total;
	}

	static Texture2D Guardar(string nombre, Texture2D tex, bool normal = false, bool repetir = true, int maxTam = 1024) {
		string ruta = "Assets/Texturas/" + nombre + ".png";
		File.WriteAllBytes(ruta, tex.EncodeToPNG());
		Object.DestroyImmediate(tex);
		AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
		var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
		imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
		imp.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
		imp.anisoLevel = 8;
		imp.mipmapEnabled = true;
		imp.maxTextureSize = maxTam;
		imp.textureCompression = TextureImporterCompression.CompressedHQ;
		imp.SaveAndReimport();
		return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
	}

	//Madera con vetas a lo largo de U (para chapas y tablas)
	static Texture2D Madera(string nombre, int ancho, int alto, Color claro, Color oscuro, float anillos, float poros, int semilla, float contraste = 0.75f) {
		var tex = new Texture2D(ancho, alto, TextureFormat.RGB24, true);
		var px = new Color[ancho * alto];
		for (int y = 0; y < alto; y++) {
			for (int x = 0; x < ancho; x++) {
				float u = x / (float)ancho, v = y / (float)alto;
				float torcido = FbmP(u * 0.5f, v * 2f, 2, 4, semilla) * 2.2f;
				float vetas = v * anillos + torcido * 3f + FbmP(u, v, 4, 3, semilla + 5) * 1.5f;
				float t = Mathf.Sin(vetas * Mathf.PI * 2f) * 0.5f + 0.5f;
				t = Mathf.Pow(t, 2.6f);
				float fibra = FbmP(u * 0.25f, v * 16f, 8, 3, semilla + 9);
				float poro = RuidoP(u * 90f, v * 700f, 2, semilla + 3) > 0.82f ? poros : 0f;
				float mezcla = Mathf.Clamp01(t * contraste + (fibra - 0.5f) * 0.35f + poro);
				var c = Color.Lerp(claro, oscuro, mezcla);
				float brillo = 0.94f + 0.12f * FbmP(u * 0.5f, v * 0.5f, 2, 2, semilla + 11);
				px[y * ancho + x] = c * brillo;
			}
		}
		tex.SetPixels(px);
		tex.Apply();
		return Guardar(nombre, tex);
	}

	static Texture2D Ruidosa(string nombre, int tam, Color a, Color b, int periodo, int octavas, int semilla, float fibra = 0f) {
		var tex = new Texture2D(tam, tam, TextureFormat.RGB24, true);
		var px = new Color[tam * tam];
		for (int y = 0; y < tam; y++) {
			for (int x = 0; x < tam; x++) {
				float u = x / (float)tam, v = y / (float)tam;
				float n = FbmP(u, v, periodo, octavas, semilla);
				if (fibra > 0f) n = Mathf.Lerp(n, Hash(x, y, semilla), fibra);
				px[y * tam + x] = Color.Lerp(a, b, n);
			}
		}
		tex.SetPixels(px);
		tex.Apply();
		return Guardar(nombre, tex);
	}

	static Texture2D Marmol(string nombre, int tam, Color fondo, Color veta, int semilla) {
		var tex = new Texture2D(tam, tam, TextureFormat.RGB24, true);
		var px = new Color[tam * tam];
		for (int y = 0; y < tam; y++) {
			for (int x = 0; x < tam; x++) {
				float u = x / (float)tam, v = y / (float)tam;
				float n = FbmP(u, v, 3, 5, semilla);
				float vetas = Mathf.Abs(Mathf.Sin((u + v + n * 1.8f) * Mathf.PI * 6f));
				vetas = Mathf.Pow(1f - vetas, 14f);
				float manchas = FbmP(u, v, 6, 3, semilla + 3);
				var c = Color.Lerp(fondo, fondo * 1.6f, manchas * 0.5f);
				c = Color.Lerp(c, veta, vetas * 0.8f);
				//Juntas de las losas (4 x 4 losas por textura)
				float ju = Mathf.Repeat(u * 4f, 1f), jv = Mathf.Repeat(v * 4f, 1f);
				if (ju < 0.006f || jv < 0.006f) c *= 0.45f;
				px[y * tam + x] = c;
			}
		}
		tex.SetPixels(px);
		tex.Apply();
		return Guardar(nombre, tex);
	}

	static void CrearTexturas() {
		//Chapas del tablero: arce (claras) y nogal (oscuras), marco de palisandro
		texMaderaClara = Madera("MaderaClara", 512, 512, new Color(0.93f, 0.80f, 0.58f), new Color(0.80f, 0.62f, 0.40f), 9f, 0.08f, 11, 0.55f);
		texMaderaOscura = Madera("MaderaOscura", 512, 512, new Color(0.46f, 0.28f, 0.15f), new Color(0.24f, 0.13f, 0.06f), 7f, 0.12f, 23);
		texMarco = Madera("MaderaMarco", 512, 512, new Color(0.30f, 0.15f, 0.08f), new Color(0.13f, 0.06f, 0.03f), 11f, 0.1f, 37);
		texMesa = Madera("MaderaMesa", 1024, 512, new Color(0.36f, 0.21f, 0.11f), new Color(0.18f, 0.09f, 0.04f), 14f, 0.1f, 41);
		//Piezas: boj (blancas) y ébano (negras)
		texBoj = Madera("Boj", 256, 256, new Color(0.96f, 0.86f, 0.64f), new Color(0.86f, 0.70f, 0.47f), 5f, 0.04f, 53, 0.5f);
		texEbano = Madera("Ebano", 256, 256, new Color(0.11f, 0.07f, 0.05f), new Color(0.03f, 0.02f, 0.015f), 6f, 0.05f, 61);
		texAlfombra = Ruidosa("Alfombra", 512, new Color(0.10f, 0.12f, 0.26f), new Color(0.05f, 0.06f, 0.15f), 8, 4, 71, 0.35f);
		texTerciopelo = Ruidosa("Terciopelo", 256, new Color(0.38f, 0.04f, 0.07f), new Color(0.20f, 0.02f, 0.04f), 4, 4, 73, 0.2f);
		texSuelo = Marmol("Marmol", 1024, new Color(0.07f, 0.07f, 0.09f), new Color(0.55f, 0.52f, 0.5f), 79);
		texRoca = Ruidosa("Roca", 512, new Color(0.16f, 0.15f, 0.17f), new Color(0.04f, 0.04f, 0.06f), 6, 5, 83, 0.15f);
		texLuna = CrearLuna();
		texPapel = CrearPapel();
		texPunto = CrearPunto();
	}

	static Texture2D CrearLuna() {
		int w = 1024, h = 512;
		var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
		var px = new Color[w * h];
		var r = new System.Random(5);
		var crateres = new Vector3[140];
		for (int i = 0; i < crateres.Length; i++) crateres[i] = new Vector3((float)r.NextDouble(), 0.1f + 0.8f * (float)r.NextDouble(), 0.005f + 0.05f * Mathf.Pow((float)r.NextDouble(), 3f));
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				float u = x / (float)w, v = y / (float)h;
				float n = FbmP(u, v * 0.5f, 4, 5, 91);
				float mares = FbmP(u, v * 0.5f, 2, 3, 97);
				float c = 0.55f + n * 0.35f - Mathf.Clamp01(mares * 2.2f - 1.1f) * 0.35f;
				foreach (var k in crateres) {
					float du = Mathf.Abs(u - k.x);
					du = Mathf.Min(du, 1f - du) * 2f;
					float d = Mathf.Sqrt(du * du + (v - k.y) * (v - k.y)) / k.z;
					if (d < 1.15f) c += d < 0.85f ? -0.12f * (1f - d) : 0.12f * (1f - Mathf.Abs(d - 1f) / 0.15f);
				}
				px[y * w + x] = new Color(c, c * 0.98f, c * 0.95f);
			}
		}
		tex.SetPixels(px);
		tex.Apply();
		return Guardar("Luna", tex);
	}

	static Texture2D CrearPapel() {
		int tam = 512;
		var tex = new Texture2D(tam, tam, TextureFormat.RGB24, true);
		var px = new Color[tam * tam];
		for (int y = 0; y < tam; y++) {
			for (int x = 0; x < tam; x++) {
				float n = Hash(x, y, 7) * 0.03f;
				var c = new Color(0.95f - n, 0.94f - n, 0.9f - n);
				//Renglones y columnas de la planilla
				bool cabecera = y > tam * 0.78f;
				if (!cabecera && (y % 18 == 0)) c = new Color(0.6f, 0.68f, 0.8f);
				if (!cabecera && (x == 70 || x == 71 || x == 260 || x == 261)) c = new Color(0.6f, 0.68f, 0.8f);
				if (y == (int)(tam * 0.78f) || y == (int)(tam * 0.78f) + 1) c = new Color(0.25f, 0.3f, 0.45f);
				px[y * tam + x] = c;
			}
		}
		tex.SetPixels(px);
		tex.Apply();
		return Guardar("Papel", tex, false, false);
	}

	//Punto suave para partículas y marcadores
	static Texture2D CrearPunto() {
		int tam = 128;
		var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, true);
		var px = new Color[tam * tam];
		for (int y = 0; y < tam; y++) {
			for (int x = 0; x < tam; x++) {
				float dx = (x + 0.5f) / tam * 2f - 1f, dy = (y + 0.5f) / tam * 2f - 1f;
				float d = Mathf.Sqrt(dx * dx + dy * dy);
				float a = Mathf.Clamp01(1f - d);
				a = a * a;
				px[y * tam + x] = new Color(1f, 1f, 1f, a);
			}
		}
		tex.SetPixels(px);
		tex.Apply();
		var t = Guardar("Punto", tex, false, false, 128);
		var imp = (TextureImporter)AssetImporter.GetAtPath("Assets/Texturas/Punto.png");
		imp.alphaIsTransparency = true;
		imp.SaveAndReimport();
		return t;
	}
}
