using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

//La sala de torneo flotando en el vacío: plataforma, escenario, público vacío, cámaras, luces,
//piezas gigantes a la deriva, un tablero gigante y la luna.
public static partial class ConstructorProyecto {

	static void CrearEscena() {
		var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

		RenderSettings.skybox = mCielo;
		RenderSettings.ambientMode = AmbientMode.Trilight;
		RenderSettings.ambientSkyColor = new Color(0.10f, 0.11f, 0.19f);
		RenderSettings.ambientEquatorColor = new Color(0.06f, 0.06f, 0.11f);
		RenderSettings.ambientGroundColor = new Color(0.02f, 0.02f, 0.03f);
		RenderSettings.fog = true;
		RenderSettings.fogMode = FogMode.Linear;
		RenderSettings.fogColor = new Color(0.015f, 0.018f, 0.04f);
		RenderSettings.fogStartDistance = 30f;
		RenderSettings.fogEndDistance = 260f;
		RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
		RenderSettings.reflectionIntensity = 0.6f;

		var sala = new GameObject("Sala").transform;
		CrearPlataforma(sala);
		CrearEscenario(sala);
		CrearPublico(sala);
		CrearLuces(sala);
		CrearVacio(sala);
		foreach (var t in sala.GetComponentsInChildren<Transform>(true)) {
			if (t.GetComponent<Flotante>() == null && t.GetComponentInParent<Flotante>() == null && t.GetComponent<Parpadeo>() == null)
				GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
		}

		var mesa = CrearMesa();
		var tablero = CrearTablero(mesa);
		var reloj = CrearReloj(tablero.transform);
		var planillas = CrearPlanillas(tablero.transform);
		var placas = CrearPlacas(mesa);
		var manos = CrearManosFantasma();
		var pantalla = CrearPantalla(sala);
		var monitor = CrearConsola();
		CrearCartel();
		var hud = CrearHUD();
		var jugador = CrearJugador(hud);
		CrearDirector(tablero, reloj, planillas, placas, manos, pantalla, monitor);

		EditorSceneManager.SaveScene(escena, RUTA_ESCENA);
	}

	// ------------------------------------------------ Plataforma flotante

	static void CrearPlataforma(Transform sala) {
		var piso = Cilindro("Piso", sala, new Vector3(0f, -0.06f, 0f), new Vector3(32f, 0.06f, 32f), mSuelo, null, 0);
		var col = piso.AddComponent<BoxCollider>();
		col.size = new Vector3(1f, 2f, 1f);
		piso.layer = CAPA_SUELO;
		ConMalla("BordeDeLuz", sala, mallaAnillo, mBordeLuz, Vector3.zero);
		var isla = ConMalla("IslaFlotante", sala, mallaIsla, mRoca, Vector3.zero);
		isla.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
		//Alfombra bajo el escenario y el público
		Cubo("Alfombra", sala, new Vector3(-3.2f, 0.004f, 0f), new Vector3(15f, 0.008f, 11f), mAlfombra, null, 0);
		//Polvo que cae lentamente desde el borde de la isla (partículas)
		var polvo = Vacio("PolvoDelVacio", sala, new Vector3(0f, -2f, 0f));
		var ps = Particulas(polvo, new Color(0.45f, 0.7f, 1f, 0.35f), 0.05f, 40f, 9f, 0.25f);
		var forma = ps.shape;
		forma.shapeType = ParticleSystemShapeType.Donut;
		forma.radius = 15.8f;
		forma.donutRadius = 0.4f;
		forma.rotation = new Vector3(90f, 0f, 0f);
		var vel = ps.velocityOverLifetime;
		vel.enabled = true;
		vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
		vel.y = new ParticleSystem.MinMaxCurve(-0.6f, -0.2f);
		vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
	}

	static ParticleSystem Particulas(GameObject go, Color color, float tam, float tasa, float vida, float velocidad) {
		var ps = go.AddComponent<ParticleSystem>();
		var main = ps.main;
		main.startColor = color;
		main.startSize = new ParticleSystem.MinMaxCurve(tam * 0.5f, tam);
		main.startLifetime = vida;
		main.startSpeed = velocidad;
		main.maxParticles = 400;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.prewarm = true;
		main.loop = true;
		var em = ps.emission;
		em.rateOverTime = tasa;
		var col = ps.colorOverLifetime;
		col.enabled = true;
		var g = new Gradient();
		g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
			new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
		col.color = g;
		var r = go.GetComponent<ParticleSystemRenderer>();
		r.sharedMaterial = mParticula;
		r.shadowCastingMode = ShadowCastingMode.Off;
		r.receiveShadows = false;
		return ps;
	}

	// ------------------------------------------------ Escenario de juego

	static void CrearEscenario(Transform sala) {
		var esc = Vacio("Escenario", sala, Vector3.zero).transform;
		var tarima = Cubo("Tarima", esc, new Vector3(0.25f, 0.1f, 0f), new Vector3(6.5f, 0.2f, 5f), mTela);
		tarima.layer = CAPA_SUELO;
		Cubo("AlfombraTarima", esc, new Vector3(0.25f, 0.2025f, 0f), new Vector3(6.4f, 0.005f, 4.9f), mAlfombra, null, 0);
		//Escalón delantero
		var escalon = Cubo("Escalon", esc, new Vector3(-3.2f, 0.05f, 0f), new Vector3(0.4f, 0.1f, 2f), mTela);
		escalon.layer = CAPA_SUELO;

		//Postes y cuerdas de terciopelo (separan al público del escenario)
		var cuerdas = Vacio("Cordones", esc, Vector3.zero).transform;
		Vector3[] postes = {
			new Vector3(-2.75f, 0.2f, -2.3f), new Vector3(-2.75f, 0.2f, -1.15f), new Vector3(-2.75f, 0.2f, 1.15f), new Vector3(-2.75f, 0.2f, 2.3f),
			new Vector3(-1.0f, 0.2f, 2.3f), new Vector3(0.75f, 0.2f, 2.3f), new Vector3(2.5f, 0.2f, 2.3f),
			new Vector3(-1.0f, 0.2f, -2.3f), new Vector3(0.75f, 0.2f, -2.3f), new Vector3(2.5f, 0.2f, -2.3f) };
		foreach (var p in postes) {
			Cilindro("Poste", cuerdas, p + Vector3.up * 0.45f, new Vector3(0.05f, 0.45f, 0.05f), mOro, null, 0);
			Cilindro("BasePoste", cuerdas, p + Vector3.up * 0.015f, new Vector3(0.25f, 0.015f, 0.25f), mOro, null, 0);
			Esfera("Bola", cuerdas, p + Vector3.up * 0.93f, Vector3.one * 0.075f, mOro, 0);
		}
		int[][] tramos = { new[] { 0, 1 }, new[] { 2, 3 }, new[] { 3, 4 }, new[] { 4, 5 }, new[] { 5, 6 }, new[] { 0, 7 }, new[] { 7, 8 }, new[] { 8, 9 } };
		foreach (var tr in tramos) Cuerda(cuerdas, postes[tr[0]] + Vector3.up * 0.86f, postes[tr[1]] + Vector3.up * 0.86f, 0.16f);
		var colCuerda = cuerdas.gameObject.AddComponent<BoxCollider>();
		colCuerda.center = new Vector3(-2.75f, 0.7f, 0f);
		colCuerda.size = new Vector3(0.1f, 1f, 4.6f);

		//Estructura de luces (truss) sobre la mesa
		var truss = Vacio("Truss", esc, Vector3.zero).transform;
		float y = 3.7f;
		Cubo("VigaN", truss, new Vector3(0f, y, 2f), new Vector3(5.2f, 0.22f, 0.22f), mTruss, null, 0);
		Cubo("VigaS", truss, new Vector3(0f, y, -2f), new Vector3(5.2f, 0.22f, 0.22f), mTruss, null, 0);
		Cubo("VigaE", truss, new Vector3(2.5f, y, 0f), new Vector3(0.22f, 0.22f, 4.2f), mTruss, null, 0);
		Cubo("VigaO", truss, new Vector3(-2.5f, y, 0f), new Vector3(0.22f, 0.22f, 4.2f), mTruss, null, 0);
		Cubo("VigaCentro", truss, new Vector3(0f, y, 0f), new Vector3(5.2f, 0.16f, 0.16f), mTruss, null, 0);
		foreach (var px in new[] { -2.5f, 2.5f }) {
			foreach (var pz in new[] { -2f, 2f }) Cubo("Columna", truss, new Vector3(px, (y + 0.2f) * 0.5f, pz), new Vector3(0.18f, y - 0.2f, 0.18f), mTruss);
		}
		foreach (var (pos, mira) in new[] {
			(new Vector3(0f, y - 0.25f, 0f), new Vector3(0f, 0.97f, 0f)),
			(new Vector3(-1.6f, y - 0.25f, 0f), new Vector3(0f, 0.97f, 0f)),
			(new Vector3(1.6f, y - 0.25f, 0f), new Vector3(0f, 0.97f, 0f)),
			(new Vector3(-2.3f, y - 0.25f, -2f), new Vector3(0f, 1f, -0.7f)),
			(new Vector3(-2.3f, y - 0.25f, 2f), new Vector3(0f, 1f, 0.7f)) }) {
			var foco = Vacio("Foco", truss, pos).transform;
			foco.LookAt(mira);
			Cilindro("Cuerpo", foco, new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.16f, 0.22f), mPlasticoNegro, new Vector3(90f, 0f, 0f), 0);
			Cilindro("Lente", foco, new Vector3(0f, 0f, 0.165f), new Vector3(0.18f, 0.005f, 0.18f), mLente, new Vector3(90f, 0f, 0f), 0);
			Cubo("Soporte", foco, new Vector3(0f, 0.16f, 0f), new Vector3(0.04f, 0.2f, 0.04f), mTruss, null, 0);
		}

		//Cámara de televisión con trípode y luz roja de grabación
		var cam = Vacio("CamaraTV", esc, new Vector3(-3.6f, 0f, 2.6f)).transform;
		cam.LookAt(new Vector3(0f, 0f, 0f));
		for (int i = 0; i < 3; i++) {
			float a = i * 120f;
			var pata = Cilindro("Pata", cam, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.65f, 0.25f), new Vector3(0.035f, 0.68f, 0.035f), mMetalOscuro, new Vector3(-20f, 0f, 0f), 0);
			pata.transform.localRotation = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(-20f, 0f, 0f);
		}
		var cuerpo = Vacio("Cuerpo", cam, new Vector3(0f, 1.45f, 0f), new Vector3(8f, 0f, 0f)).transform;
		Cubo("Caja", cuerpo, new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.26f, 0.45f), mPlasticoNegro, null, 0);
		Cilindro("Objetivo", cuerpo, new Vector3(0f, 0f, 0.33f), new Vector3(0.16f, 0.12f, 0.16f), mMetalOscuro, new Vector3(90f, 0f, 0f), 0);
		Cilindro("Vidrio", cuerpo, new Vector3(0f, 0f, 0.455f), new Vector3(0.13f, 0.005f, 0.13f), mVidrio, new Vector3(90f, 0f, 0f), 0);
		Cubo("Visor", cuerpo, new Vector3(-0.15f, 0.08f, -0.1f), new Vector3(0.08f, 0.1f, 0.14f), mPlasticoNegro, null, 0);
		var tally = Esfera("LuzGrabando", cuerpo, new Vector3(0f, 0.16f, 0.15f), Vector3.one * 0.03f, mLuzRoja, 0);
		tally.AddComponent<Parpadeo>();
	}

	static void Cuerda(Transform padre, Vector3 a, Vector3 b, float caida) {
		const int SEG = 6;
		for (int i = 0; i < SEG; i++) {
			float t0 = i / (float)SEG, t1 = (i + 1) / (float)SEG;
			var p0 = Vector3.Lerp(a, b, t0) - Vector3.up * (caida * 4f * t0 * (1f - t0));
			var p1 = Vector3.Lerp(a, b, t1) - Vector3.up * (caida * 4f * t1 * (1f - t1));
			var c = Cilindro("Cuerda", padre, (p0 + p1) * 0.5f, new Vector3(0.035f, Vector3.Distance(p0, p1) * 0.5f, 0.035f), mCuerda, null, 0);
			c.transform.up = (p1 - p0).normalized;
		}
	}

	// ------------------------------------------------ Público (sillas vacías)

	static void CrearPublico(Transform sala) {
		var pub = Vacio("PublicoVacio", sala, Vector3.zero).transform;
		for (int fila = 0; fila < 5; fila++) {
			for (int k = 0; k < 9; k++) {
				var p = new Vector3(-4.6f - fila * 1.05f, 0f, (k - 4) * 0.82f);
				var silla = Vacio("Butaca", pub, p, new Vector3(0f, 90f, 0f)).transform;
				Cubo("Asiento", silla, new Vector3(0f, 0.45f, 0f), new Vector3(0.52f, 0.1f, 0.5f), mTerciopelo, null, 0);
				Cubo("Respaldo", silla, new Vector3(0f, 0.8f, -0.24f), new Vector3(0.52f, 0.62f, 0.08f), mTerciopelo, new Vector3(-8f, 0f, 0f), 0);
				Cubo("PataI", silla, new Vector3(-0.24f, 0.2f, 0f), new Vector3(0.04f, 0.4f, 0.45f), mMetalOscuro, null, 0);
				Cubo("PataD", silla, new Vector3(0.24f, 0.2f, 0f), new Vector3(0.04f, 0.4f, 0.45f), mMetalOscuro, null, 0);
				var c = silla.gameObject.AddComponent<BoxCollider>();
				c.center = new Vector3(0f, 0.55f, -0.05f);
				c.size = new Vector3(0.55f, 1.1f, 0.55f);
			}
		}
		//Un espectador fantasma: solo su brillo en una butaca de la primera fila
		var espectro = Vacio("EspectadorFantasma", pub, new Vector3(-4.6f, 0.75f, 1.64f));
		var ps = Particulas(espectro, new Color(0.5f, 0.8f, 1f, 0.5f), 0.03f, 14f, 3f, 0.04f);
		var forma = ps.shape;
		forma.shapeType = ParticleSystemShapeType.Box;
		forma.scale = new Vector3(0.35f, 0.6f, 0.3f);
	}

	// ------------------------------------------------ Luces

	static void CrearLuces(Transform sala) {
		var luces = Vacio("Luces", sala, Vector3.zero).transform;
		var dirLuna = new Vector3(-0.55f, 0.32f, 0.77f).normalized;
		var luna = Vacio("LuzDeLuna", luces, Vector3.zero).transform;
		luna.rotation = Quaternion.LookRotation(-dirLuna);
		var l = luna.gameObject.AddComponent<Light>();
		l.type = LightType.Directional;
		l.color = new Color(0.62f, 0.72f, 1f);
		l.intensity = 0.42f;
		l.shadows = LightShadows.Soft;
		l.shadowStrength = 0.7f;

		var contra = Vacio("LuzDesdeAbajo", luces, Vector3.zero).transform;
		contra.rotation = Quaternion.Euler(-35f, 60f, 0f);
		var lc = contra.gameObject.AddComponent<Light>();
		lc.type = LightType.Directional;
		lc.color = new Color(0.55f, 0.3f, 0.85f);
		lc.intensity = 0.25f;
		lc.shadows = LightShadows.None;

		var foco = Vacio("FocoMesa", luces, new Vector3(0f, 3.45f, 0f)).transform;
		foco.LookAt(new Vector3(0f, 0.97f, 0f));
		var lf = foco.gameObject.AddComponent<Light>();
		lf.type = LightType.Spot;
		lf.color = new Color(1f, 0.93f, 0.84f);
		lf.intensity = 2.4f;
		lf.range = 7f;
		lf.spotAngle = 58f;
		lf.innerSpotAngle = 30f;
		lf.shadows = LightShadows.Soft;
		lf.shadowStrength = 0.85f;
		lf.shadowBias = 0.01f;
		lf.shadowNormalBias = 0.2f;
		lf.shadowNearPlane = 0.5f;

		var frio = Vacio("FocoFrio", luces, new Vector3(2.4f, 3.3f, 1.4f)).transform;
		frio.LookAt(new Vector3(0f, 0.97f, 0f));
		var lr = frio.gameObject.AddComponent<Light>();
		lr.type = LightType.Spot;
		lr.color = new Color(0.45f, 0.7f, 1f);
		lr.intensity = 1.1f;
		lr.range = 7f;
		lr.spotAngle = 45f;
		lr.shadows = LightShadows.None;

		foreach (var (pos, color, inten, rango) in new[] {
			(new Vector3(-6.5f, 2.8f, 0f), new Color(0.4f, 0.5f, 1f), 1.4f, 9f),
			(new Vector3(4f, 1.5f, 0f), new Color(0.4f, 0.6f, 1f), 1.2f, 6f),
			(new Vector3(-1.2f, 1.8f, -1.6f), new Color(1f, 0.85f, 0.65f), 0.7f, 3.5f) }) {
			var p = Vacio("LuzRelleno", luces, pos);
			var lp = p.AddComponent<Light>();
			lp.type = LightType.Point;
			lp.color = color;
			lp.intensity = inten;
			lp.range = rango;
			lp.shadows = LightShadows.None;
		}
	}

	// ------------------------------------------------ El vacío: piezas gigantes, tablero gigante y luna

	static void CrearVacio(Transform sala) {
		var vacio = Vacio("ElVacio", sala, Vector3.zero).transform;
		var dirLuna = new Vector3(-0.55f, 0.32f, 0.77f).normalized;
		var luna = Esfera("Luna", vacio, dirLuna * 520f, Vector3.one * 110f, mLuna, 0);
		luna.transform.rotation = Quaternion.Euler(10f, 40f, 0f);
		luna.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
		luna.AddComponent<Flotante>().giro = new Vector3(0f, 0.6f, 0f);
		luna.GetComponent<Flotante>().amplitud = 0f;

		var r = new System.Random(12);
		(int tipo, bool blanca, Vector3 pos, float escala)[] gigantes = {
			(5, true, new Vector3(-70f, 18f, 95f), 260f),
			(1, false, new Vector3(85f, 32f, 60f), 300f),
			(3, true, new Vector3(110f, -25f, -70f), 280f),
			(4, false, new Vector3(-120f, 45f, -40f), 240f),
			(2, true, new Vector3(-30f, -40f, -130f), 300f),
			(0, false, new Vector3(40f, 60f, 140f), 340f),
			(1, true, new Vector3(-150f, -10f, 60f), 260f),
		};
		foreach (var g in gigantes) {
			var go = ConMalla("PiezaGigante_" + NOMBRES_PIEZA[g.tipo], vacio, mallasPieza[g.tipo], g.blanca ? mBoj : mEbano, g.pos,
				new Vector3((float)r.NextDouble() * 360f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 360f), Vector3.one * g.escala);
			go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
			var f = go.AddComponent<Flotante>();
			f.giro = new Vector3((float)r.NextDouble() * 4f - 2f, (float)r.NextDouble() * 6f - 3f, (float)r.NextDouble() * 4f - 2f);
			f.amplitud = 2f + (float)r.NextDouble() * 3f;
			f.periodo = 18f + (float)r.NextDouble() * 12f;
			f.fase = (float)r.NextDouble();
		}
		//Tablero gigante a la deriva
		var gigante = ConMalla("TableroGigante", vacio, MallaCasillas(), mClara, new Vector3(30f, -30f, -150f), new Vector3(-30f, 20f, 15f), Vector3.one * 90f);
		gigante.GetComponent<MeshRenderer>().sharedMaterials = new[] { mClara, mOscura };
		gigante.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
		var marco = Cubo("Marco", gigante.transform, new Vector3(0f, -0.0095f, 0f), new Vector3(0.51f, 0.018f, 0.51f), mMarco, null, 0);
		marco.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
		var fg = gigante.AddComponent<Flotante>();
		fg.giro = new Vector3(0f, 1.2f, 0f);
		fg.amplitud = 3f;
		fg.periodo = 26f;
	}
}
