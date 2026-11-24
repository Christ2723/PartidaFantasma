using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

//Piezas de ajedrez estilo Staunton hechas "en el torno": cada pieza es un perfil (radio, altura en mm)
//que se gira 360°. El caballo es una silueta de cabeza tallada con bisel sobre una base torneada.
public static partial class ConstructorProyecto {

	static readonly string[] NOMBRES_PIEZA = { "Peon", "Caballo", "Alfil", "Torre", "Dama", "Rey" };
	static readonly float[] ALTURAS = { 0.043f, 0.064f, 0.074f, 0.054f, 0.0858f, 0.098f };   //metros
	static readonly float[] RADIOS = { 0.014f, 0.0165f, 0.016f, 0.0165f, 0.018f, 0.019f };
	static Mesh[] mallasPieza = new Mesh[6];
	static PiezaFisica[] prefabsPieza = new PiezaFisica[12];
	static Sprite[] iconos = new Sprite[12];
	static Mesh mallaAnillo, mallaIsla, mallaDisco;

	// ------------------------------------------------ Torno

	static Vector2 V(float r, float y) => new Vector2(r, y);

	static IEnumerable<Vector2> Arco(float cr, float cy, float radio, float desdeGrados, float hastaGrados, int pasos) {
		for (int i = 0; i <= pasos; i++) {
			float a = Mathf.Lerp(desdeGrados, hastaGrados, i / (float)pasos) * Mathf.Deg2Rad;
			yield return new Vector2(Mathf.Max(0f, cr + radio * Mathf.Sin(a)), cy + radio * Mathf.Cos(a));
		}
	}

	//Gira un perfil (en milímetros, de abajo-centro a arriba-centro) y devuelve la malla en metros
	static Mesh Torno(string nombre, List<Vector2> perfil, int lados = 48, float anguloDuro = 38f, float escala = 0.001f, bool guardar = true) {
		var verts = new List<Vector3>();
		var norms = new List<Vector3>();
		var uvs = new List<Vector2>();
		var tris = new List<int>();
		int n = perfil.Count;
		var normSeg = new Vector2[n - 1];
		for (int i = 0; i < n - 1; i++) {
			var t = perfil[i + 1] - perfil[i];
			normSeg[i] = t.sqrMagnitude > 0f ? new Vector2(t.y, -t.x).normalized : Vector2.up;
		}
		Vector2 NormalEn(int i, int seg) {
			if (i == 0 || i == n - 1) return normSeg[seg];
			var a = normSeg[i - 1];
			var b = normSeg[i];
			return Vector2.Angle(a, b) > anguloDuro ? normSeg[seg] : (a + b).normalized;
		}
		float total = 0f;
		var largo = new float[n];
		for (int i = 1; i < n; i++) {
			total += Vector2.Distance(perfil[i], perfil[i - 1]);
			largo[i] = total;
		}
		float vEscala = total / 60f;
		for (int s = 0; s < n - 1; s++) {
			int baseIdx = verts.Count;
			for (int extremo = 0; extremo < 2; extremo++) {
				int i = s + extremo;
				var p = perfil[i];
				var nn = NormalEn(i, s);
				for (int k = 0; k <= lados; k++) {
					float a = k / (float)lados * Mathf.PI * 2f;
					float c = Mathf.Cos(a), si = Mathf.Sin(a);
					verts.Add(new Vector3(p.x * c, p.y, p.x * si) * escala);
					norms.Add(new Vector3(nn.x * c, nn.y, nn.x * si).normalized);
					uvs.Add(new Vector2(k / (float)lados * 2f, largo[i] / total * vEscala));
				}
			}
			for (int k = 0; k < lados; k++) {
				int a0 = baseIdx + k, a1 = a0 + 1, b0 = baseIdx + lados + 1 + k, b1 = b0 + 1;
				tris.Add(a0); tris.Add(b0); tris.Add(b1);
				tris.Add(a0); tris.Add(b1); tris.Add(a1);
			}
		}
		var m = new Mesh { name = nombre };
		m.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
		m.SetVertices(verts);
		m.SetNormals(norms);
		m.SetUVs(0, uvs);
		m.SetTriangles(tris, 0);
		m.RecalculateBounds();
		m.RecalculateTangents();
		if (guardar) GuardarMalla(m);
		return m;
	}

	static Mesh GuardarMalla(Mesh m) {
		string ruta = "Assets/Mallas/" + m.name + ".asset";
		AssetDatabase.DeleteAsset(ruta);
		AssetDatabase.CreateAsset(m, ruta);
		return m;
	}

	// ------------------------------------------------ Silueta extruida con bisel (cabeza del caballo)

	static float AreaFirmada(List<Vector2> p) {
		float a = 0f;
		for (int i = 0; i < p.Count; i++) {
			var x = p[i];
			var y = p[(i + 1) % p.Count];
			a += x.x * y.y - y.x * x.y;
		}
		return a * 0.5f;
	}

	static List<int> Triangular(List<Vector2> p) {
		var res = new List<int>();
		var idx = Enumerable.Range(0, p.Count).ToList();
		int guardia = 0;
		while (idx.Count > 3 && guardia++ < 5000) {
			bool corto = false;
			for (int i = 0; i < idx.Count; i++) {
				int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
				var A = p[a];
				var B = p[b];
				var C = p[c];
				float cruz = (B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x);
				if (cruz <= 1e-6f) continue;
				bool dentro = false;
				foreach (int o in idx) {
					if (o == a || o == b || o == c) continue;
					if (EnTriangulo(p[o], A, B, C)) { dentro = true; break; }
				}
				if (dentro) continue;
				res.Add(a); res.Add(b); res.Add(c);
				idx.RemoveAt(i);
				corto = true;
				break;
			}
			if (!corto) break;
		}
		if (idx.Count == 3) { res.Add(idx[0]); res.Add(idx[1]); res.Add(idx[2]); }
		return res;
	}

	static bool EnTriangulo(Vector2 p, Vector2 a, Vector2 b, Vector2 c) {
		float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
		float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
		float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
		bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
		return !(neg && pos);
	}

	//Polígono (mm, en el plano XY, mirando a +X) extruido en Z con grosor y bisel
	static Mesh Extruir(string nombre, List<Vector2> poli, float grosor, float bisel) {
		if (AreaFirmada(poli) < 0f) poli.Reverse();
		int n = poli.Count;
		var interior = new List<Vector2>();
		for (int i = 0; i < n; i++) {
			var a = poli[(i + n - 1) % n];
			var b = poli[i];
			var c = poli[(i + 1) % n];
			var n1 = new Vector2(-(b - a).y, (b - a).x).normalized;   //normal hacia dentro (polígono antihorario)
			var n2 = new Vector2(-(c - b).y, (c - b).x).normalized;
			var dir = (n1 + n2).normalized;
			float cos = Mathf.Max(0.35f, Vector2.Dot(dir, n1));
			interior.Add(b + dir * (bisel / cos));
		}
		float h = grosor * 0.5f - bisel, hf = grosor * 0.5f;
		var verts = new List<Vector3>();
		var tris = new List<int>();
		var uvs = new List<Vector2>();
		void Quad(Vector3 A, Vector3 B, Vector3 C, Vector3 D) {
			int k = verts.Count;
			verts.Add(A); verts.Add(B); verts.Add(C); verts.Add(D);
			foreach (var v in new[] { A, B, C, D }) uvs.Add(new Vector2(v.x, v.y) * 0.02f);
			tris.Add(k); tris.Add(k + 2); tris.Add(k + 1);
			tris.Add(k); tris.Add(k + 3); tris.Add(k + 2);
		}
		Vector3 P(Vector2 v, float z) => new Vector3(v.x, v.y, z);
		for (int i = 0; i < n; i++) {
			int j = (i + 1) % n;
			Quad(P(poli[i], h), P(poli[j], h), P(poli[j], -h), P(poli[i], -h));                    //costado
			Quad(P(interior[i], hf), P(interior[j], hf), P(poli[j], h), P(poli[i], h));            //bisel delantero
			Quad(P(poli[i], -h), P(poli[j], -h), P(interior[j], -hf), P(interior[i], -hf));        //bisel trasero
		}
		var tapa = Triangular(interior);
		int frente = verts.Count;
		foreach (var v in interior) { verts.Add(P(v, hf)); uvs.Add(v * 0.02f); }
		int atras = verts.Count;
		foreach (var v in interior) { verts.Add(P(v, -hf)); uvs.Add(v * 0.02f); }
		for (int t = 0; t < tapa.Count; t += 3) {
			tris.Add(frente + tapa[t]); tris.Add(frente + tapa[t + 1]); tris.Add(frente + tapa[t + 2]);
			tris.Add(atras + tapa[t]); tris.Add(atras + tapa[t + 2]); tris.Add(atras + tapa[t + 1]);
		}
		var m = new Mesh { name = nombre };
		m.SetVertices(verts.Select(v => v * 0.001f).ToList());
		m.SetUVs(0, uvs);
		m.SetTriangles(tris, 0);
		m.RecalculateNormals();
		m.RecalculateBounds();
		return m;
	}

	// ------------------------------------------------ Perfiles Staunton

	static List<Vector2> Base(float r) => new List<Vector2> {
		V(0, 0), V(r, 0), V(r, 2.8f), V(r - 0.7f, 3.8f), V(r - 1.5f, 4.4f), V(r - 1.5f, 5.2f),
		V(r - 0.9f, 6.1f), V(r - 0.6f, 7.2f), V(r - 1f, 8.3f), V(r - 1.9f, 9f), V(r - 3.5f, 9.8f)
	};

	static Mesh Combinar(string nombre, params (Mesh malla, Matrix4x4 m)[] partes) {
		var ci = partes.Select(p => new CombineInstance { mesh = p.malla, transform = p.m }).ToArray();
		var res = new Mesh { name = nombre };
		res.CombineMeshes(ci, true, true);
		res.RecalculateBounds();
		res.RecalculateTangents();
		return GuardarMalla(res);
	}

	static Matrix4x4 TRS(Vector3 p, Vector3 rot, Vector3 esc) => Matrix4x4.TRS(p, Quaternion.Euler(rot), esc);

	static void CrearMallas() {
		var cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
		var esfera = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

		//Peón
		var peon = Base(14f);
		peon.AddRange(new[] { V(9.4f, 10.2f), V(8.2f, 12.5f), V(7.2f, 15.5f), V(6.4f, 19f), V(5.8f, 22.5f),
			V(6.2f, 23.2f), V(9.2f, 23.8f), V(9.6f, 24.6f), V(9.2f, 25.4f), V(6f, 26f), V(5.2f, 26.9f) });
		peon.AddRange(Arco(0f, 34.5f, 8.5f, 140f, 0f, 14));
		mallasPieza[0] = Torno("Peon", peon);

		//Torre (con almenas)
		var torre = Base(16.5f);
		torre.AddRange(new[] { V(11.8f, 11.5f), V(11.2f, 14f), V(11f, 20f), V(11.2f, 28f), V(11.8f, 34f), V(12.6f, 38f),
			V(13.8f, 39f), V(14.2f, 40.2f), V(13.4f, 41.2f), V(12.8f, 41.8f), V(13.2f, 42.5f), V(14f, 46f), V(14.4f, 49f),
			V(11f, 49f), V(11f, 47.5f), V(0f, 47.5f) });
		var cuerpoTorre = Torno("TorreCuerpo", torre, 48, 38f, 0.001f, false);
		var partesTorre = new List<(Mesh, Matrix4x4)> { (cuerpoTorre, Matrix4x4.identity) };
		for (int i = 0; i < 6; i++) {
			float a = i * 60f + 30f;
			var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
			partesTorre.Add((cubo, TRS(dir * 0.0127f + Vector3.up * 0.0515f, new Vector3(0f, a, 0f), new Vector3(0.0066f, 0.005f, 0.0034f))));
		}
		mallasPieza[3] = Combinar("Torre", partesTorre.ToArray());

		//Alfil
		var alfil = Base(16f);
		alfil.AddRange(new[] { V(11f, 11.6f), V(9.8f, 14.5f), V(8.6f, 19f), V(7.6f, 25f), V(6.9f, 31f), V(6.5f, 36f),
			V(7.2f, 37f), V(10.8f, 37.8f), V(11.2f, 39f), V(10.6f, 40f), V(7.6f, 40.6f), V(8.6f, 41.4f), V(9.4f, 42.4f), V(8.4f, 43.4f), V(6.2f, 44f),
			V(7.2f, 45.5f), V(8.8f, 48f), V(9.8f, 51f), V(10f, 54f), V(9.5f, 57f), V(8.3f, 60f), V(6.4f, 63f), V(4.2f, 65.5f), V(2.4f, 67f),
			V(1.6f, 67.6f) });
		alfil.AddRange(Arco(0f, 71f, 3f, 125f, 0f, 8));
		var cuerpoAlfil = Torno("AlfilCuerpo", alfil, 48, 38f, 0.001f, false);
		mallasPieza[2] = Combinar("Alfil", (cuerpoAlfil, Matrix4x4.identity));

		//Dama (corona con perlas)
		var dama = Base(18f);
		dama.AddRange(new[] { V(12.4f, 12.6f), V(11f, 15.5f), V(9.7f, 20f), V(8.6f, 27f), V(7.7f, 35f), V(7.2f, 43f), V(7f, 49f),
			V(7.6f, 50f), V(11.8f, 51f), V(12.2f, 52.3f), V(11.5f, 53.4f), V(8.2f, 54f), V(9.4f, 54.9f), V(10.2f, 56f), V(9.2f, 57f), V(7.4f, 57.6f),
			V(7.8f, 59f), V(9f, 62f), V(10.6f, 66f), V(12.2f, 70f), V(13f, 72.5f),
			V(13.4f, 73.4f), V(13.2f, 74.4f), V(11.5f, 75f), V(10.5f, 75.4f), V(10.2f, 76f), V(8.8f, 78f), V(6.5f, 79.5f), V(3.5f, 80.2f), V(2.2f, 80.6f) });
		dama.AddRange(Arco(0f, 82.8f, 3f, 135f, 0f, 8));
		var cuerpoDama = Torno("DamaCuerpo", dama, 48, 38f, 0.001f, false);
		var partesDama = new List<(Mesh, Matrix4x4)> { (cuerpoDama, Matrix4x4.identity) };
		for (int i = 0; i < 10; i++) {
			var dir = Quaternion.Euler(0f, i * 36f, 0f) * Vector3.forward;
			partesDama.Add((esfera, TRS(dir * 0.0124f + Vector3.up * 0.0756f, Vector3.zero, Vector3.one * 0.0034f)));
		}
		mallasPieza[4] = Combinar("Dama", partesDama.ToArray());

		//Rey (con cruz)
		var rey = Base(19f);
		rey.AddRange(new[] { V(13.1f, 13f), V(11.6f, 16f), V(10.2f, 21f), V(9f, 29f), V(8f, 38f), V(7.5f, 47f), V(7.3f, 53f),
			V(8f, 54f), V(12.4f, 55f), V(12.8f, 56.4f), V(12f, 57.5f), V(8.6f, 58.1f), V(9.8f, 59f), V(10.6f, 60.1f), V(9.6f, 61.1f), V(7.8f, 61.7f),
			V(8.2f, 63.2f), V(9.5f, 66.5f), V(11.2f, 71f), V(12.8f, 75.5f), V(13.6f, 78f),
			V(14f, 79f), V(13.6f, 80.2f), V(11.6f, 80.8f), V(10.4f, 81.4f), V(9.6f, 82.6f), V(7.2f, 84.2f), V(4.5f, 85.2f), V(3.4f, 85.6f),
			V(3.4f, 86.6f), V(4.2f, 87.2f), V(0f, 87.2f) });
		var cuerpoRey = Torno("ReyCuerpo", rey, 48, 38f, 0.001f, false);
		mallasPieza[5] = Combinar("Rey", (cuerpoRey, Matrix4x4.identity),
			(cubo, TRS(new Vector3(0f, 0.0925f, 0f), Vector3.zero, new Vector3(0.0038f, 0.0115f, 0.0038f))),
			(cubo, TRS(new Vector3(0f, 0.0935f, 0f), Vector3.zero, new Vector3(0.0105f, 0.0036f, 0.0038f))));

		//Caballo: base torneada + cabeza tallada
		var baseCaballo = Base(16.5f);
		baseCaballo.AddRange(new[] { V(12f, 11f), V(11.6f, 12.5f), V(12.6f, 13.4f), V(13f, 14.6f), V(12.4f, 15.6f), V(0f, 15.6f) });
		var cuerpoCaballo = Torno("CaballoBase", baseCaballo, 48, 38f, 0.001f, false);
		var silueta = new List<Vector2> {
			V(-11f, 0f), V(12f, 0f), V(12.5f, 4f), V(11f, 10f), V(9.5f, 16f), V(11.5f, 19f), V(17f, 21.5f), V(22f, 23.5f),
			V(24f, 26f), V(24.5f, 29f), V(23f, 32f), V(19f, 34f), V(14f, 37.5f), V(10f, 41f), V(8.5f, 44f), V(8f, 48.5f),
			V(4.5f, 45f), V(0.5f, 44f), V(-4.5f, 41.5f), V(-8.5f, 36.5f), V(-11.5f, 29f), V(-13f, 20f), V(-13.5f, 10f), V(-12.5f, 3f) };
		var cabeza = Extruir("CaballoCabeza", silueta, 14f, 2.4f);
		mallasPieza[1] = Combinar("Caballo", (cuerpoCaballo, Matrix4x4.identity),
			(cabeza, TRS(new Vector3(0f, 0.0154f, -0.002f), new Vector3(0f, -90f, 0f), Vector3.one)));

		//Mallas del escenario: borde de luz de la plataforma, isla flotante y disco de marcadores
		mallaAnillo = Torno("AnilloBorde", new List<Vector2> { V(15800f, -60f), V(16100f, -60f), V(16180f, 20f), V(16100f, 90f), V(15800f, 90f) }, 128, 30f);
		var isla = new List<Vector2> { V(0f, -14000f), V(1800f, -13200f), V(4200f, -10500f), V(6800f, -7200f), V(9800f, -4600f),
			V(12600f, -2600f), V(14800f, -1200f), V(16050f, -300f), V(16100f, -60f), V(0f, -60f) };
		mallaIsla = Torno("Isla", isla, 96, 20f);
		mallaDisco = Torno("Disco", new List<Vector2> { V(0f, 0f), V(9f, 0f), V(9f, 0.6f), V(0f, 0.6f) }, 24, 30f);
		AssetDatabase.SaveAssets();
	}

	// ------------------------------------------------ Prefabs de piezas

	static void CrearPrefabsPiezas() {
		for (int color = 0; color < 2; color++) {
			for (int t = 0; t < 6; t++) {
				var go = new GameObject(NOMBRES_PIEZA[t] + (color == 0 ? "Blanco" : "Negro"));
				go.layer = CAPA_INTERACTUABLE;
				go.AddComponent<MeshFilter>().sharedMesh = mallasPieza[t];
				var r = go.AddComponent<MeshRenderer>();
				r.sharedMaterial = color == 0 ? mBoj : mEbano;
				var col = go.AddComponent<CapsuleCollider>();
				col.radius = RADIOS[t] * 0.9f;
				col.height = ALTURAS[t];
				col.center = new Vector3(0f, ALTURAS[t] * 0.5f, 0f);
				var pf = go.AddComponent<PiezaFisica>();
				pf.pieza = color == 0 ? t + 1 : -(t + 1);
				pf.altura = ALTURAS[t];
				pf.radio = RADIOS[t];
				string ruta = "Assets/Prefabs/" + go.name + ".prefab";
				var prefab = PrefabUtility.SaveAsPrefabAsset(go, ruta);
				Object.DestroyImmediate(go);
				prefabsPieza[color * 6 + t] = prefab.GetComponent<PiezaFisica>();
			}
		}
	}

	// ------------------------------------------------ Iconos 2D para la pantalla de transmisión

	static void CrearIconos() {
		const int TAM = 256;
		var escena = EditorSceneManager.NewPreviewScene();
		var camGo = new GameObject("CamIcono");
		SceneManager.MoveGameObjectToScene(camGo, escena);
		var cam = camGo.AddComponent<Camera>();
		cam.scene = escena;
		cam.orthographic = true;
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
		cam.nearClipPlane = 0.01f;
		cam.farClipPlane = 5f;
		var rt = new RenderTexture(TAM, TAM, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
		cam.targetTexture = rt;
		foreach (var (dir, intensidad) in new[] { (new Vector3(35f, -30f, 0f), 1.25f), (new Vector3(10f, 150f, 0f), 0.7f), (new Vector3(60f, 60f, 0f), 0.4f) }) {
			var lg = new GameObject("Luz");
			SceneManager.MoveGameObjectToScene(lg, escena);
			var l = lg.AddComponent<Light>();
			l.type = LightType.Directional;
			l.intensity = intensidad;
			l.transform.rotation = Quaternion.Euler(dir);
		}
		for (int i = 0; i < 12; i++) {
			int t = i % 6;
			var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabsPieza[i].gameObject, escena);
			go.transform.position = Vector3.zero;
			go.transform.rotation = Quaternion.Euler(0f, t == 1 ? -90f : 25f, 0f);
			float alto = ALTURAS[t];
			float tamVista = Mathf.Max(alto * 0.56f, 0.036f);
			cam.orthographicSize = tamVista;
			cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
			cam.transform.position = new Vector3(0f, tamVista - 0.005f, 0f) - cam.transform.forward * 0.5f;
			cam.Render();
			RenderTexture.active = rt;
			var tex = new Texture2D(TAM, TAM, TextureFormat.RGBA32, false);
			tex.ReadPixels(new Rect(0, 0, TAM, TAM), 0, 0);
			tex.Apply();
			RenderTexture.active = null;
			Contorno(tex, i < 6 ? new Color(0.2f, 0.12f, 0.05f, 1f) : new Color(0.95f, 0.9f, 0.8f, 1f));
			string ruta = "Assets/Sprites/" + prefabsPieza[i].name + ".png";
			File.WriteAllBytes(ruta, tex.EncodeToPNG());
			Object.DestroyImmediate(tex);
			Object.DestroyImmediate(go);
			AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
			var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
			imp.textureType = TextureImporterType.Sprite;
			imp.alphaIsTransparency = true;
			imp.mipmapEnabled = true;
			imp.SaveAndReimport();
			iconos[i] = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
		}
		cam.targetTexture = null;
		rt.Release();
		EditorSceneManager.ClosePreviewScene(escena);
	}

	//Contorno de 2-3 px alrededor de la silueta para que se vea bien sobre cualquier casilla
	static void Contorno(Texture2D tex, Color color) {
		int w = tex.width, h = tex.height;
		var px = tex.GetPixels();
		var res = (Color[])px.Clone();
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				if (px[y * w + x].a > 0.5f) {
					var c = px[y * w + x];
					res[y * w + x] = new Color(c.r, c.g, c.b, 1f);
					continue;
				}
				float max = 0f;
				for (int dy = -3; dy <= 3; dy++) {
					for (int dx = -3; dx <= 3; dx++) {
						if (dx * dx + dy * dy > 9) continue;
						int xx = x + dx, yy = y + dy;
						if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
						max = Mathf.Max(max, px[yy * w + xx].a);
					}
				}
				if (max > 0.3f) res[y * w + x] = new Color(color.r, color.g, color.b, Mathf.Clamp01(max * 1.3f));
			}
		}
		tex.SetPixels(res);
		tex.Apply();
	}
}
