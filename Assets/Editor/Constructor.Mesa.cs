using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

//La mesa de juego: tablero electrónico de madera, reloj digital, planillas, placas con los nombres,
//sillas vacías, manos fantasma, consola del árbitro y el cartel de bienvenida.
public static partial class ConstructorProyecto {

	const float ALTO_MESA = 0.95f;        //superficie de la mesa (la tarima mide 0.2)
	const float GROSOR_TABLERO = 0.018f;
	const float CASILLA = 0.055f;

	// ------------------------------------------------ Mesa y sillas

	static Transform CrearMesa() {
		var mesa = Vacio("MesaDeJuego", null, new Vector3(0f, 0.2f, 0f)).transform;
		Cubo("Tapa", mesa, new Vector3(0f, 0.73f, 0f), new Vector3(1.5f, 0.04f, 0.9f), mMesa);
		foreach (var px in new[] { -0.68f, 0.68f }) {
			foreach (var pz in new[] { -0.38f, 0.38f }) Cubo("Pata", mesa, new Vector3(px, 0.355f, pz), new Vector3(0.06f, 0.71f, 0.06f), mMesa, null, 0);
		}
		//Faldones de tela en los lados que ve el público
		Cubo("FaldonPublico", mesa, new Vector3(-0.752f, 0.42f, 0f), new Vector3(0.008f, 0.6f, 0.92f), mTela, null, 0);
		Cubo("FaldonPantalla", mesa, new Vector3(0.752f, 0.42f, 0f), new Vector3(0.008f, 0.6f, 0.92f), mTela, null, 0);
		var col = mesa.gameObject.AddComponent<BoxCollider>();
		col.center = new Vector3(0f, 0.4f, 0f);
		col.size = new Vector3(1.5f, 0.8f, 0.9f);

		Silla(mesa, new Vector3(0f, 0f, -0.78f), 0f, "SillaBlancas");
		Silla(mesa, new Vector3(0f, 0f, 0.78f), 180f, "SillaNegras");

		//Vasos de agua
		foreach (var pz in new[] { -0.3f, 0.3f }) {
			var vaso = Cilindro("Vaso", mesa, new Vector3(0.62f, 0.8f, pz), new Vector3(0.07f, 0.05f, 0.07f), mVidrio, null, 0);
			Cilindro("Agua", vaso.transform, new Vector3(0f, -0.25f, 0f), new Vector3(0.9f, 0.62f, 0.9f), mAgua, null, 0);
			SinSombras(vaso);
		}
		//Presencia del fantasma sobre cada silla vacía
		foreach (var pz in new[] { -0.85f, 0.85f }) {
			var aura = Vacio("AuraFantasma", mesa, new Vector3(0f, 0.95f, pz));
			var ps = Particulas(aura, new Color(0.5f, 0.85f, 1f, 0.45f), 0.025f, 10f, 3.5f, 0.03f);
			var forma = ps.shape;
			forma.shapeType = ParticleSystemShapeType.Box;
			forma.scale = new Vector3(0.45f, 0.7f, 0.3f);
		}
		return mesa;
	}

	static void Silla(Transform mesa, Vector3 pos, float giro, string nombre) {
		var s = Vacio(nombre, mesa, pos, new Vector3(0f, giro, 0f)).transform;
		Cubo("Asiento", s, new Vector3(0f, 0.47f, 0f), new Vector3(0.5f, 0.07f, 0.48f), mCuero, null, 0);
		Cubo("Respaldo", s, new Vector3(0f, 0.86f, -0.24f), new Vector3(0.5f, 0.62f, 0.07f), mCuero, new Vector3(-10f, 0f, 0f), 0);
		Cilindro("Columna", s, new Vector3(0f, 0.26f, 0f), new Vector3(0.06f, 0.2f, 0.06f), mMetal, null, 0);
		for (int i = 0; i < 5; i++) {
			var pata = Cubo("Pata", s, Quaternion.Euler(0f, i * 72f, 0f) * new Vector3(0f, 0.05f, 0.16f), new Vector3(0.04f, 0.03f, 0.32f), mMetal, null, 0);
			pata.transform.localRotation = Quaternion.Euler(0f, i * 72f, 0f);
			Esfera("Rueda", s, Quaternion.Euler(0f, i * 72f, 0f) * new Vector3(0f, 0.03f, 0.3f), Vector3.one * 0.05f, mPlasticoNegro, 0);
		}
		var c = s.gameObject.AddComponent<BoxCollider>();
		c.center = new Vector3(0f, 0.55f, -0.05f);
		c.size = new Vector3(0.55f, 1.1f, 0.55f);
	}

	// ------------------------------------------------ Tablero electrónico

	//64 casillas en una malla con dos submallas (claras y oscuras). Cada casilla toma un trozo distinto de la chapa.
	static Mesh mallaCasillas;
	static Mesh MallaCasillas() {
		if (mallaCasillas != null) return mallaCasillas;
		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var claras = new List<int>();
		var oscuras = new List<int>();
		var r = new System.Random(64);
		for (int fila = 0; fila < 8; fila++) {
			for (int col = 0; col < 8; col++) {
				float x0 = (col - 4) * CASILLA, z0 = (fila - 4) * CASILLA;
				int k = verts.Count;
				verts.Add(new Vector3(x0, 0f, z0));
				verts.Add(new Vector3(x0, 0f, z0 + CASILLA));
				verts.Add(new Vector3(x0 + CASILLA, 0f, z0 + CASILLA));
				verts.Add(new Vector3(x0 + CASILLA, 0f, z0));
				float u = (float)r.NextDouble() * 0.7f, v = (float)r.NextDouble() * 0.7f, t = 0.28f;
				bool girar = r.Next(2) == 0;
				if (girar) {
					uvs.Add(new Vector2(u, v)); uvs.Add(new Vector2(u + t, v)); uvs.Add(new Vector2(u + t, v + t)); uvs.Add(new Vector2(u, v + t));
				} else {
					uvs.Add(new Vector2(u, v)); uvs.Add(new Vector2(u, v + t)); uvs.Add(new Vector2(u + t, v + t)); uvs.Add(new Vector2(u + t, v));
				}
				var lista = (col + fila) % 2 == 0 ? oscuras : claras;
				lista.Add(k); lista.Add(k + 1); lista.Add(k + 2);
				lista.Add(k); lista.Add(k + 2); lista.Add(k + 3);
			}
		}
		var m = new Mesh { name = "Casillas" };
		m.SetVertices(verts);
		m.SetUVs(0, uvs);
		m.subMeshCount = 2;
		m.SetTriangles(claras, 0);
		m.SetTriangles(oscuras, 1);
		m.RecalculateNormals();
		m.RecalculateTangents();
		m.RecalculateBounds();
		mallaCasillas = GuardarMalla(m);
		return mallaCasillas;
	}

	static TableroDGT CrearTablero(Transform mesa) {
		var go = Vacio("TableroElectronico", null, new Vector3(0f, ALTO_MESA + GROSOR_TABLERO, 0f));
		go.layer = CAPA_TABLERO;
		var marco = Cubo("Marco", go.transform, new Vector3(0f, -GROSOR_TABLERO * 0.5f - 0.0003f, 0f), new Vector3(0.51f, GROSOR_TABLERO, 0.51f), mMarco, null, 0);
		marco.layer = CAPA_TABLERO;
		var casillas = ConMalla("Casillas", go.transform, MallaCasillas(), mClara, Vector3.zero);
		casillas.GetComponent<MeshRenderer>().sharedMaterials = new[] { mClara, mOscura };
		casillas.layer = CAPA_TABLERO;
		var col = go.AddComponent<BoxCollider>();
		col.center = new Vector3(0f, -GROSOR_TABLERO * 0.5f, 0f);
		col.size = new Vector3(0.51f, GROSOR_TABLERO, 0.51f);
		//Filete claro entre casillas y marco
		foreach (var (p, e) in new[] { (new Vector3(0f, 0.0001f, -0.2215f), new Vector3(0.446f, 0.0004f, 0.003f)), (new Vector3(0f, 0.0001f, 0.2215f), new Vector3(0.446f, 0.0004f, 0.003f)),
			(new Vector3(-0.2215f, 0.0001f, 0f), new Vector3(0.003f, 0.0004f, 0.446f)), (new Vector3(0.2215f, 0.0001f, 0f), new Vector3(0.003f, 0.0004f, 0.446f)) })
			Cubo("Filete", go.transform, p, e, mClara, null, 0);

		//Coordenadas en el marco
		var coord = Lienzo("Coordenadas", go.transform, new Vector3(0f, 0.0006f, 0f), new Vector3(90f, 0f, 0f), new Vector2(1020f, 1020f), 0.0005f);
		var colorLetra = new Color(0.92f, 0.82f, 0.62f);
		for (int i = 0; i < 8; i++) {
			string letra = ((char)('a' + i)).ToString();
			string num = (i + 1).ToString();
			float p = (i - 3.5f) * 110f;
			Texto("Col_" + letra, coord.transform, letra, 24, colorLetra, new Vector2(p, -476f), new Vector2(60f, 40f), TextAnchor.MiddleCenter, FontStyle.Bold);
			var arriba = Texto("ColN_" + letra, coord.transform, letra, 24, colorLetra, new Vector2(p, 476f), new Vector2(60f, 40f), TextAnchor.MiddleCenter, FontStyle.Bold);
			arriba.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
			Texto("Fila_" + num, coord.transform, num, 24, colorLetra, new Vector2(-476f, p), new Vector2(40f, 40f), TextAnchor.MiddleCenter, FontStyle.Bold);
			var der = Texto("FilaD_" + num, coord.transform, num, 24, colorLetra, new Vector2(476f, p), new Vector2(40f, 40f), TextAnchor.MiddleCenter, FontStyle.Bold);
			der.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
		}
		Texto("Marca", coord.transform, "TABLERO ELECTRÓNICO · 64 SENSORES", 13, new Color(0.85f, 0.75f, 0.55f, 0.8f), new Vector2(330f, -497f), new Vector2(340f, 20f));

		//Luz del sensor y cable al reloj
		var led = Cubo("LedSensor", go.transform, new Vector3(0.2375f, 0.0006f, -0.2445f), new Vector3(0.005f, 0.0015f, 0.003f), mLuzVerde, null, 0);
		Cable(go.transform, new[] { new Vector3(0.255f, -0.012f, 0.12f), new Vector3(0.29f, -0.016f, 0.14f), new Vector3(0.33f, -0.016f, 0.12f), new Vector3(0.36f, -0.016f, 0.07f) });

		//Zonas para las piezas comidas
		var zonaNegras = Vacio("CapturasNegras", go.transform, new Vector3(-0.40f, -GROSOR_TABLERO, -0.32f));
		var zonaBlancas = Vacio("CapturasBlancas", go.transform, new Vector3(-0.40f, -GROSOR_TABLERO, 0.32f), new Vector3(0f, 180f, 0f));

		//Marcador de jugadas legales
		var marcador = ConMalla("Marcador", null, mallaDisco, mMarcador, Vector3.zero);
		marcador.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
		var prefabMarcador = PrefabUtility.SaveAsPrefabAsset(marcador, "Assets/Prefabs/Marcador.prefab");
		Object.DestroyImmediate(marcador);

		var t = go.AddComponent<TableroDGT>();
		t.casilla = CASILLA;
		t.prefabs = (PiezaFisica[])prefabsPieza.Clone();
		t.capturasBlancas = zonaBlancas.transform;
		t.capturasNegras = zonaNegras.transform;
		t.prefabMarcador = prefabMarcador;
		t.luzSensor = led.transform;

		//Vista previa en el editor: piezas en la posición inicial (se reemplazan al empezar)
		return t;
	}

	static void Cable(Transform padre, Vector3[] puntos) {
		for (int i = 0; i < puntos.Length - 1; i++) {
			var a = puntos[i];
			var b = puntos[i + 1];
			var c = Cilindro("Cable", padre, (a + b) * 0.5f, new Vector3(0.004f, Vector3.Distance(a, b) * 0.5f + 0.001f, 0.004f), mCable, null, 0);
			c.transform.up = (b - a).normalized;
		}
	}

	// ------------------------------------------------ Reloj digital

	static RelojDGT CrearReloj(Transform tablero) {
		var r = Vacio("RelojDigital", tablero, new Vector3(0.42f, -GROSOR_TABLERO, 0f)).transform;
		Cubo("Cuerpo", r, new Vector3(0.008f, 0.024f, 0f), new Vector3(0.075f, 0.048f, 0.18f), mPlasticoNegro, null, 0);
		Cubo("Frente", r, new Vector3(-0.031f, 0.026f, 0f), new Vector3(0.012f, 0.05f, 0.18f), mPlasticoNegro, new Vector3(0f, 0f, -12f), 0);
		Cubo("Pantallas", r, new Vector3(-0.0375f, 0.031f, 0f), new Vector3(0.002f, 0.024f, 0.15f), mLCD, new Vector3(0f, 0f, -12f), 0);
		var pulsB = Cubo("PulsadorBlancas", r, new Vector3(0.01f, 0.054f, -0.055f), new Vector3(0.04f, 0.012f, 0.045f), mPlasticoNegro, null, 0);
		var pulsN = Cubo("PulsadorNegras", r, new Vector3(0.01f, 0.054f, 0.055f), new Vector3(0.04f, 0.012f, 0.045f), mPlasticoNegro, null, 0);
		for (int i = 0; i < 5; i++) Cilindro("Boton", r, new Vector3(-0.036f, 0.009f, (i - 2) * 0.016f), new Vector3(0.007f, 0.002f, 0.007f), mMetalOscuro, new Vector3(0f, 0f, 90f), 0);
		var col = r.gameObject.AddComponent<BoxCollider>();
		col.center = new Vector3(0f, 0.03f, 0f);
		col.size = new Vector3(0.09f, 0.06f, 0.18f);

		var lienzo = Lienzo("LCD", r, new Vector3(-0.0388f, 0.0312f, 0f), Vector3.zero, new Vector2(1500f, 240f), 0.0001f);
		lienzo.transform.localRotation = Quaternion.Euler(0f, 0f, -12f) * Quaternion.Euler(0f, 90f, 0f);
		var colorDigito = new Color(0.06f, 0.07f, 0.06f);
		var tB = Texto("TiempoBlancas", lienzo.transform, "1:40:00", 150, colorDigito, new Vector2(370f, -5f), new Vector2(700f, 220f), TextAnchor.MiddleCenter, FontStyle.Bold);
		var tN = Texto("TiempoNegras", lienzo.transform, "1:40:00", 150, colorDigito, new Vector2(-370f, -5f), new Vector2(700f, 220f), TextAnchor.MiddleCenter, FontStyle.Bold);
		var iB = Imagen("TurnoBlancas", lienzo.transform, colorDigito, new Vector2(70f, 85f), new Vector2(40f, 40f));
		var iN = Imagen("TurnoNegras", lienzo.transform, colorDigito, new Vector2(-70f, 85f), new Vector2(40f, 40f));
		Imagen("Separador", lienzo.transform, new Color(0.1f, 0.1f, 0.1f, 0.6f), Vector2.zero, new Vector2(6f, 220f));

		var reloj = r.gameObject.AddComponent<RelojDGT>();
		reloj.pantallaBlancas = tB;
		reloj.pantallaNegras = tN;
		reloj.pulsadorBlancas = pulsB.transform;
		reloj.pulsadorNegras = pulsN.transform;
		reloj.indicadorBlancas = iB;
		reloj.indicadorNegras = iN;
		return reloj;
	}

	// ------------------------------------------------ Planillas y placas

	static Planilla[] CrearPlanillas(Transform tablero) {
		var res = new Planilla[2];
		for (int i = 0; i < 2; i++) {
			bool blancas = i == 0;
			var p = Vacio(blancas ? "PlanillaBlancas" : "PlanillaNegras", tablero, new Vector3(-0.54f, -GROSOR_TABLERO, blancas ? -0.26f : 0.26f), new Vector3(0f, blancas ? 8f : 188f, 0f)).transform;
			Cubo("Hoja", p, new Vector3(0f, 0.0005f, 0f), new Vector3(0.15f, 0.001f, 0.21f), mPapel, null, 0);
			var l = Lienzo("Texto", p, new Vector3(0f, 0.0012f, 0f), new Vector3(90f, 0f, 0f), new Vector2(750f, 1050f), 0.0002f);
			var tinta = new Color(0.08f, 0.1f, 0.3f);
			var enc = Texto("Encabezado", l.transform, "PLANILLA", 30, new Color(0.1f, 0.1f, 0.15f), new Vector2(0f, 420f), new Vector2(700f, 200f), TextAnchor.UpperLeft, FontStyle.Bold);
			var cn = Texto("Numeros", l.transform, "", 30, new Color(0.35f, 0.35f, 0.4f), new Vector2(-290f, -100f), new Vector2(110f, 820f), TextAnchor.UpperRight);
			var cb = Texto("Blancas", l.transform, "", 32, tinta, new Vector2(-90f, -100f), new Vector2(260f, 820f), TextAnchor.UpperLeft, FontStyle.Italic);
			var cg = Texto("Negras", l.transform, "", 32, tinta, new Vector2(180f, -100f), new Vector2(260f, 820f), TextAnchor.UpperLeft, FontStyle.Italic);
			cn.lineSpacing = cb.lineSpacing = cg.lineSpacing = 1.07f;
			var boli = Vacio("Boligrafo", p, new Vector3(0.05f, 0.005f, -0.02f), new Vector3(0f, 25f, 0f)).transform;
			Cilindro("Cuerpo", boli, Vector3.zero, new Vector3(0.009f, 0.07f, 0.009f), mPlasticoNegro, new Vector3(90f, 0f, 0f), 0);
			Cilindro("Punta", boli, new Vector3(0f, 0f, 0.075f), new Vector3(0.005f, 0.006f, 0.005f), mOro, new Vector3(90f, 0f, 0f), 0);
			var pl = p.gameObject.AddComponent<Planilla>();
			pl.encabezado = enc;
			pl.colNumeros = cn;
			pl.colBlancas = cb;
			pl.colNegras = cg;
			pl.boligrafo = boli;
			res[i] = pl;
		}
		return res;
	}

	static Text[] CrearPlacas(Transform mesa) {
		var res = new Text[2];
		for (int i = 0; i < 2; i++) {
			bool blancas = i == 0;
			var p = Vacio(blancas ? "PlacaBlancas" : "PlacaNegras", mesa, new Vector3(-0.68f, 0.75f, blancas ? -0.22f : 0.22f), new Vector3(0f, 0f, 0f)).transform;
			Cubo("Placa", p, new Vector3(0f, 0.03f, 0f), new Vector3(0.004f, 0.065f, 0.2f), mBlanco, new Vector3(0f, 0f, -18f), 0);
			Cubo("Soporte", p, new Vector3(0.012f, 0.006f, 0f), new Vector3(0.03f, 0.012f, 0.2f), mMetalOscuro, null, 0);
			var l = Lienzo("Nombre", p, new Vector3(-0.0024f, 0.0307f, 0f), Vector3.zero, new Vector2(1000f, 320f), 0.0002f);
			l.transform.localRotation = Quaternion.Euler(0f, 0f, -18f) * Quaternion.Euler(0f, 90f, 0f);
			Imagen("Franja", l.transform, blancas ? new Color(0.92f, 0.9f, 0.85f) : new Color(0.08f, 0.08f, 0.1f), new Vector2(0f, 135f), new Vector2(1000f, 50f));
			res[i] = Texto("Texto", l.transform, blancas ? "BLANCAS" : "NEGRAS", 92, new Color(0.08f, 0.1f, 0.25f), new Vector2(0f, -25f), new Vector2(980f, 260f), TextAnchor.MiddleCenter, FontStyle.Bold);
			res[i].resizeTextForBestFit = true;
			res[i].resizeTextMinSize = 30;
			res[i].resizeTextMaxSize = 92;
			res[i].verticalOverflow = VerticalWrapMode.Truncate;
		}
		return res;
	}

	// ------------------------------------------------ Manos fantasma

	static ManoFantasma[] CrearManosFantasma() {
		var res = new ManoFantasma[2];
		for (int i = 0; i < 2; i++) {
			bool blancas = i == 0;
			float z = blancas ? -0.5f : 0.5f;
			var reposo = Vacio(blancas ? "ReposoManoBlancas" : "ReposoManoNegras", null, new Vector3(blancas ? 0.12f : -0.12f, 1.22f, z), new Vector3(0f, blancas ? 0f : 180f, 0f));
			var mano = Vacio(blancas ? "ManoFantasmaBlancas" : "ManoFantasmaNegras", null, reposo.transform.position, reposo.transform.eulerAngles).transform;
			var palma = Esfera("Palma", mano, new Vector3(0f, 0.07f, -0.075f), new Vector3(0.085f, 0.03f, 0.1f), mEspectral, 0);
			palma.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
			Capsula("Muneca", mano, new Vector3(0f, 0.11f, -0.16f), new Vector3(0.06f, 0.075f, 0.05f), mEspectral, new Vector3(65f, 0f, 0f), 0);
			Capsula("Antebrazo", mano, new Vector3(0f, 0.17f, -0.27f), new Vector3(0.07f, 0.1f, 0.06f), mEspectral, new Vector3(60f, 0f, 0f), 0);
			var dedos = new List<Transform>();
			float[] xs = { 0.028f, 0.009f, -0.01f, -0.028f };
			float[] largos = { 0.075f, 0.082f, 0.076f, 0.062f };
			for (int d = 0; d < 4; d++) {
				var nudillo = Vacio("Dedo" + d, mano, new Vector3(xs[d], 0.065f, -0.03f)).transform;
				var objetivo = new Vector3(xs[d] * 0.35f, 0.004f + d * 0.004f, 0.004f - d * 0.008f) - nudillo.localPosition;
				nudillo.localRotation = Quaternion.LookRotation(objetivo.normalized, Vector3.up);
				float largo = Mathf.Min(largos[d], objetivo.magnitude + 0.006f);
				Capsula("Falange", nudillo, new Vector3(0f, 0f, largo * 0.5f), new Vector3(0.017f, largo * 0.5f, 0.017f), mEspectral, new Vector3(90f, 0f, 0f), 0);
				dedos.Add(nudillo);
			}
			var pulgar = Vacio("Pulgar", mano, new Vector3(-0.045f, 0.05f, -0.07f)).transform;
			var objP = new Vector3(-0.004f, 0.003f, -0.004f) - pulgar.localPosition;
			pulgar.localRotation = Quaternion.LookRotation(objP.normalized, Vector3.up);
			Capsula("Falange", pulgar, new Vector3(0f, 0f, objP.magnitude * 0.5f), new Vector3(0.02f, objP.magnitude * 0.5f, 0.02f), mEspectral, new Vector3(90f, 0f, 0f), 0);
			dedos.Add(pulgar);
			SinSombras(mano.gameObject);
			var mf = mano.gameObject.AddComponent<ManoFantasma>();
			mf.reposo = reposo.transform;
			mf.dedos = dedos.ToArray();
			res[i] = mf;
		}
		return res;
	}

	// ------------------------------------------------ Consola del árbitro (botones grandes)

	static Text CrearConsola() {
		var consola = Vacio("ConsolaArbitro", null, new Vector3(-1.4f, 0.2f, -1.35f)).transform;
		var usuario = new Vector3(-0.75f, 0.2f, -1.95f);
		var frente = consola.position - usuario;
		frente.y = 0f;
		consola.rotation = Quaternion.LookRotation(frente.normalized);
		Cubo("Mueble", consola, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.9f, 0.42f), mPlasticoNegro);
		Cubo("Franja", consola, new Vector3(0f, 0.86f, -0.212f), new Vector3(0.95f, 0.02f, 0.004f), mBordeLuz, null, 0);
		var sup = Vacio("Superficie", consola, new Vector3(0f, 0.93f, 0f), new Vector3(-25f, 0f, 0f)).transform;
		Cubo("Panel", sup, new Vector3(0f, -0.02f, 0f), new Vector3(1f, 0.04f, 0.5f), mMetalOscuro);
		(AccionBoton accion, string texto, Material mat, Color color)[] botones = {
			(AccionBoton.Fantasma, "PAUSAR / SEGUIR\nAL FANTASMA  (F)", mBotonAzul, new Color(0.3f, 0.75f, 1f)),
			(AccionBoton.SiguientePartida, "SIGUIENTE PARTIDA\nHISTÓRICA  (N)", mBotonAmbar, new Color(1f, 0.7f, 0.25f)),
			(AccionBoton.Duelo, "JUGAR CONTRA\nEL FANTASMA  (J)", mBotonRojo, new Color(1f, 0.3f, 0.25f)),
			(AccionBoton.Reiniciar, "REINICIAR\nTABLERO  (R)", mBotonBlanco, new Color(0.9f, 0.9f, 0.95f)),
			(AccionBoton.ModoPeon, "MODO PEÓN\nENCOGERSE  (P)", mBotonMorado, new Color(0.75f, 0.4f, 1f)),
			(AccionBoton.Sentarse, "SENTARSE /\nLEVANTARSE  (C)", mBotonVerde, new Color(0.35f, 1f, 0.5f)) };
		for (int i = 0; i < botones.Length; i++) {
			var b = botones[i];
			float x = (i % 3 - 1) * 0.3f, z = i < 3 ? 0.08f : -0.13f;
			var raiz = Vacio("Boton_" + b.accion, sup, new Vector3(x, 0f, z)).transform;
			Cilindro("Aro", raiz, new Vector3(0f, 0.008f, 0f), new Vector3(0.11f, 0.008f, 0.11f), mMetal, null, 0);
			var tapa = Cilindro("Tapa", raiz, new Vector3(0f, 0.022f, 0f), new Vector3(0.085f, 0.012f, 0.085f), b.mat, null, 0);
			var c = raiz.gameObject.AddComponent<BoxCollider>();
			c.center = new Vector3(0f, 0.02f, 0f);
			c.size = new Vector3(0.12f, 0.05f, 0.12f);
			Capa(raiz.gameObject, CAPA_INTERACTUABLE);
			var bf = raiz.gameObject.AddComponent<BotonFisico>();
			bf.accion = b.accion;
			bf.tapa = tapa.transform;
			bf.luz = tapa.GetComponent<Renderer>();
			bf.color = b.color;
			var l = Lienzo("Etiqueta", sup, new Vector3(x, 0.0015f, z - 0.085f), new Vector3(90f, 0f, 0f), new Vector2(560f, 110f), 0.0005f);
			Texto("Texto", l.transform, b.texto, 34, new Color(0.92f, 0.94f, 1f), Vector2.zero, new Vector2(560f, 110f), TextAnchor.MiddleCenter, FontStyle.Bold);
		}
		//Monitor del árbitro
		var mon = Vacio("Monitor", consola, new Vector3(0f, 1.28f, 0.2f), new Vector3(-8f, 0f, 0f)).transform;
		Cubo("Marco", mon, Vector3.zero, new Vector3(0.66f, 0.42f, 0.03f), mPlasticoNegro, null, 0);
		Cubo("Pie", mon, new Vector3(0f, -0.3f, 0.03f), new Vector3(0.05f, 0.25f, 0.03f), mMetalOscuro, null, 0);
		var lm = Lienzo("Pantalla", mon, new Vector3(0f, 0f, -0.0155f), Vector3.zero, new Vector2(1240f, 760f), 0.0005f);
		Imagen("Fondo", lm.transform, new Color(0.02f, 0.05f, 0.1f), Vector2.zero, new Vector2(1240f, 760f));
		Texto("Titulo", lm.transform, "MESA DEL ÁRBITRO", 54, new Color(0.4f, 0.85f, 1f), new Vector2(0f, 310f), new Vector2(1200f, 80f), TextAnchor.MiddleCenter, FontStyle.Bold);
		var texto = Texto("Estado", lm.transform, "", 44, Color.white, new Vector2(0f, -40f), new Vector2(1160f, 560f), TextAnchor.UpperLeft);
		return texto;
	}

	// ------------------------------------------------ Cartel flotante de bienvenida

	static void CrearCartel() {
		var c = Lienzo("CartelBienvenida", null, new Vector3(0.6f, 2.75f, 2.9f), new Vector3(0f, 8f, 0f), new Vector2(2400f, 900f), 0.0011f);
		Texto("Titulo", c.transform, "LA PARTIDA FANTASMA", 230, new Color(1f, 0.85f, 0.45f), new Vector2(0f, 230f), new Vector2(2400f, 300f), TextAnchor.MiddleCenter, FontStyle.Bold);
		Texto("Sub", c.transform, "Torneo Súper Élite · Ronda ∞ · Una sala de ajedrez flotando en el vacío", 70, new Color(0.7f, 0.85f, 1f), new Vector2(0f, 40f), new Vector2(2400f, 100f));
		Texto("Ayuda", c.transform, "Un rival invisible juega sobre el tablero electrónico.  Toma una pieza para analizar, usa la mesa del árbitro\no presiona P para encogerte al tamaño de un peón y caminar entre las piezas.", 52, new Color(0.85f, 0.88f, 0.95f), new Vector2(0f, -150f), new Vector2(2400f, 200f));
		foreach (var t in c.GetComponentsInChildren<Text>()) Sombra(t, 4f);
	}
}
