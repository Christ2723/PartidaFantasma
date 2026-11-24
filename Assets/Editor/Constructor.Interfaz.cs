using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

//Pantalla gigante de transmisión, interfaz (HUD), el jugador con su equipo de VR y el director de la escena
public static partial class ConstructorProyecto {

	static Sprite Circulo => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

	// ------------------------------------------------ Pantalla gigante

	static PantallaTransmision CrearPantalla(Transform sala) {
		var raiz = Vacio("PantallaGigante", sala, new Vector3(4.7f, 0f, 0f)).transform;
		Cubo("Marco", raiz, new Vector3(0.08f, 3.1f, 0f), new Vector3(0.16f, 3.75f, 6.6f), mPlasticoNegro, null, 0);
		Cubo("PataN", raiz, new Vector3(0.1f, 0.6f, 2.6f), new Vector3(0.12f, 1.2f, 0.12f), mTruss);
		Cubo("PataS", raiz, new Vector3(0.1f, 0.6f, -2.6f), new Vector3(0.12f, 1.2f, 0.12f), mTruss);
		var l = Lienzo("Transmision", raiz, new Vector3(-0.005f, 3.1f, 0f), new Vector3(0f, 90f, 0f), new Vector2(1920f, 1080f), 6.4f / 1920f);
		var t = l.transform;
		var fondo = new Color(0.025f, 0.035f, 0.08f);
		Imagen("Fondo", t, fondo, Vector2.zero, new Vector2(1920f, 1080f));
		Imagen("BarraSuperior", t, new Color(0.06f, 0.09f, 0.2f), new Vector2(0f, 495f), new Vector2(1920f, 90f));
		Imagen("BarraInferior", t, new Color(0.06f, 0.09f, 0.2f), new Vector2(0f, -495f), new Vector2(1920f, 90f));
		Imagen("LineaOro", t, new Color(1f, 0.78f, 0.35f), new Vector2(0f, 449f), new Vector2(1920f, 3f));
		var punto = Imagen("PuntoVivo", t, new Color(1f, 0.15f, 0.12f), new Vector2(-900f, 495f), new Vector2(30f, 30f), null, Circulo);
		Texto("EnVivo", t, "EN VIVO", 36, Color.white, new Vector2(-790f, 495f), new Vector2(170f, 60f), TextAnchor.MiddleLeft, FontStyle.Bold);
		Texto("Torneo", t, "TORNEO SÚPER ÉLITE  ·  RONDA ∞", 44, new Color(1f, 0.85f, 0.5f), new Vector2(0f, 495f), new Vector2(1000f, 70f), TextAnchor.MiddleCenter, FontStyle.Bold);
		Texto("Fuente", t, "Transmisión del tablero electrónico", 28, new Color(0.6f, 0.75f, 1f), new Vector2(700f, 495f), new Vector2(480f, 60f), TextAnchor.MiddleRight);

		//Diagrama (las negras arriba)
		var p = l.gameObject.AddComponent<PantallaTransmision>();
		const float TAM = 98f;
		var centro = new Vector2(-470f, -5f);
		Imagen("MarcoDiagrama", t, new Color(0.2f, 0.12f, 0.06f), centro, new Vector2(TAM * 8f + 24f, TAM * 8f + 24f));
		for (int i = 0; i < 64; i++) {
			int col = i % 8, fila = i / 8;
			var pos = centro + new Vector2((col - 3.5f) * TAM, (fila - 3.5f) * TAM);
			p.casillas[i] = Imagen("Casilla" + i, t, Color.gray, pos, new Vector2(TAM, TAM));
		}
		for (int i = 0; i < 64; i++) {
			int col = i % 8, fila = i / 8;
			var pos = centro + new Vector2((col - 3.5f) * TAM, (fila - 3.5f) * TAM);
			var im = Imagen("Pieza" + i, t, Color.white, pos, new Vector2(TAM * 0.98f, TAM * 0.98f));
			im.preserveAspect = true;
			im.enabled = false;
			p.piezas[i] = im;
		}
		for (int i = 0; i < 8; i++) {
			Texto("Letra" + i, t, ((char)('a' + i)).ToString(), 26, new Color(0.7f, 0.75f, 0.85f), centro + new Vector2((i - 3.5f) * TAM, -4f * TAM - 30f), new Vector2(40f, 30f));
			Texto("Numero" + i, t, (i + 1).ToString(), 26, new Color(0.7f, 0.75f, 0.85f), centro + new Vector2(-4f * TAM - 30f, (i - 3.5f) * TAM), new Vector2(30f, 40f));
		}
		p.sprites = (Sprite[])iconos.Clone();

		//Panel derecho
		float x0 = 480f;
		Imagen("PanelNegras", t, new Color(0.08f, 0.08f, 0.1f), new Vector2(x0, 375f), new Vector2(860f, 90f));
		Imagen("PanelBlancas", t, new Color(0.85f, 0.83f, 0.78f), new Vector2(x0, -405f), new Vector2(860f, 90f));
		p.nombreNegras = Texto("NombreNegras", t, "Negras", 40, Color.white, new Vector2(x0 - 110f, 375f), new Vector2(600f, 80f), TextAnchor.MiddleLeft, FontStyle.Bold);
		p.relojNegras = Texto("RelojNegras", t, "1:40:00", 54, Color.white, new Vector2(x0 + 300f, 375f), new Vector2(240f, 80f), TextAnchor.MiddleRight, FontStyle.Bold);
		p.nombreBlancas = Texto("NombreBlancas", t, "Blancas", 40, new Color(0.08f, 0.08f, 0.12f), new Vector2(x0 - 110f, -405f), new Vector2(600f, 80f), TextAnchor.MiddleLeft, FontStyle.Bold);
		p.relojBlancas = Texto("RelojBlancas", t, "1:40:00", 54, new Color(0.08f, 0.08f, 0.12f), new Vector2(x0 + 300f, -405f), new Vector2(240f, 80f), TextAnchor.MiddleRight, FontStyle.Bold);
		p.nombreNegras.resizeTextForBestFit = p.nombreBlancas.resizeTextForBestFit = true;
		p.nombreNegras.resizeTextMaxSize = p.nombreBlancas.resizeTextMaxSize = 40;
		p.nombreNegras.resizeTextMinSize = p.nombreBlancas.resizeTextMinSize = 20;
		p.turnoNegras = Imagen("TurnoNegras", t, new Color(0.3f, 0.9f, 1f), new Vector2(x0 - 420f, 375f), new Vector2(14f, 70f));
		p.turnoBlancas = Imagen("TurnoBlancas", t, new Color(0.1f, 0.5f, 0.9f), new Vector2(x0 - 420f, -405f), new Vector2(14f, 70f));
		p.numeroJugada = Texto("NumeroJugada", t, "", 28, new Color(0.65f, 0.7f, 0.8f), new Vector2(x0, 305f), new Vector2(860f, 40f));
		Imagen("FondoJugadas", t, new Color(0.04f, 0.06f, 0.13f), new Vector2(x0, 25f), new Vector2(860f, 520f));
		p.colNumeros = Texto("ColNumeros", t, "", 30, new Color(0.55f, 0.6f, 0.75f), new Vector2(x0 - 330f, 25f), new Vector2(110f, 500f), TextAnchor.UpperRight);
		p.colBlancas = Texto("ColBlancas", t, "", 30, Color.white, new Vector2(x0 - 110f, 25f), new Vector2(260f, 500f), TextAnchor.UpperLeft, FontStyle.Bold);
		p.colNegras = Texto("ColNegras", t, "", 30, Color.white, new Vector2(x0 + 170f, 25f), new Vector2(260f, 500f), TextAnchor.UpperLeft, FontStyle.Bold);
		p.comentario = Texto("Comentario", t, "", 34, new Color(0.45f, 0.9f, 1f), new Vector2(x0, -265f), new Vector2(860f, 90f), TextAnchor.MiddleCenter, FontStyle.Italic);
		p.estado = Texto("Estado", t, "", 28, new Color(1f, 0.85f, 0.5f), new Vector2(x0, -335f), new Vector2(860f, 50f));
		p.titulo = Texto("Titulo", t, "", 34, new Color(0.85f, 0.9f, 1f), new Vector2(0f, -495f), new Vector2(1800f, 70f), TextAnchor.MiddleCenter, FontStyle.Bold);
		p.puntoVivo = punto;
		p.titulo.resizeTextForBestFit = true;
		p.titulo.resizeTextMaxSize = 34;
		p.titulo.resizeTextMinSize = 18;
		p.comentario.resizeTextForBestFit = true;
		p.comentario.resizeTextMaxSize = 34;
		p.comentario.resizeTextMinSize = 20;
		p.comentario.verticalOverflow = VerticalWrapMode.Truncate;
		return p;
	}

	// ------------------------------------------------ HUD

	static HUD CrearHUD() {
		var raiz = new GameObject("HUD");
		var hud = raiz.AddComponent<HUD>();

		//Lienzo de pantalla (escritorio)
		var lp = new GameObject("LienzoPantalla", typeof(RectTransform));
		lp.layer = 5;
		lp.transform.SetParent(raiz.transform, false);
		var c = lp.AddComponent<Canvas>();
		c.renderMode = RenderMode.ScreenSpaceOverlay;
		c.sortingOrder = 10;
		var esc = lp.AddComponent<CanvasScaler>();
		esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		esc.referenceResolution = new Vector2(1920f, 1080f);
		esc.matchWidthOrHeight = 0.5f;
		var t = lp.transform;

		var arribaDer = new Vector2(1f, 1f);
		Imagen("FondoNombre", t, new Color(0.01f, 0.025f, 0.07f, 1f), new Vector2(-20f, -20f), new Vector2(470f, 96f), arribaDer);
		var nombre = Texto("NombreMatricula", t, NOMBRE + "\nMatrícula: " + MATRICULA, 30, Color.white, new Vector2(-34f, -26f), new Vector2(440f, 86f), TextAnchor.UpperRight, FontStyle.Bold, arribaDer);
		Sombra(nombre);

		var arribaIzq = new Vector2(0f, 1f);
		Imagen("FondoTitulo", t, new Color(0.01f, 0.025f, 0.07f, 1f), new Vector2(20f, -14f), new Vector2(560f, 56f), arribaIzq);
		var titulo = Texto("Titulo", t, "LA PARTIDA FANTASMA  <size=22>· realidad virtual</size>", 34, new Color(1f, 0.85f, 0.45f), new Vector2(24f, -20f), new Vector2(900f, 50f), TextAnchor.UpperLeft, FontStyle.Bold, arribaIzq);
		Sombra(titulo);
		var panel = Imagen("PanelAyuda", t, new Color(0.01f, 0.025f, 0.07f, 1f), new Vector2(20f, -76f), new Vector2(560f, 470f), arribaIzq);
		var ayuda = Texto("Ayuda", panel.transform, TextoAyuda(), 23, new Color(0.88f, 0.92f, 1f), new Vector2(16f, -12f), new Vector2(530f, 450f), TextAnchor.UpperLeft, FontStyle.Normal, arribaIzq);
		var mensaje = Texto("Mensaje", t, "", 34, Color.white, new Vector2(0f, 70f), new Vector2(1500f, 120f), TextAnchor.MiddleCenter, FontStyle.Bold, new Vector2(0.5f, 0f));
		Sombra(mensaje, 3f);
		var mira = Imagen("Punto", t, new Color(1f, 1f, 1f, 0f), Vector2.zero, new Vector2(4f, 4f));
		var fundido = Imagen("Fundido", t, new Color(0f, 0f, 0f, 0f), Vector2.zero, new Vector2(4000f, 3000f));

		hud.lienzoPantalla = c;
		hud.mensaje = mensaje;
		hud.ayuda = ayuda;
		hud.panelAyuda = panel.gameObject;
		hud.fundido = fundido;
		return hud;
	}

	static string TextoAyuda() =>
		"<b>CONTROLES (sin visor)</b>\n" +
		"WASD / flechas: caminar   ·   Shift: correr\n" +
		"Clic derecho + arrastrar: mirar   ·   Q / E: girar\n" +
		"Rueda: acercar la vista\n" +
		"Clic izquierdo: tomar pieza / soltar en casilla\n" +
		"   (clic derecho mientras la llevas = cancelar)\n" +
		"F o Espacio: pausar / seguir al fantasma\n" +
		"N: siguiente partida histórica\n" +
		"J: jugar contra el fantasma   ·   R: reiniciar\n" +
		"<color=#C9A0FF>P: MODO PEÓN (encogerse)</color>   ·   C: sentarse\n" +
		"V: entrar en realidad virtual   ·   H: ocultar ayuda\n\n" +
		"<b>CON VISOR VR</b>\n" +
		"Palanca izq.: caminar · Palanca der.: girar\n" +
		"Gatillo: láser · Agarre: tomar la pieza cercana\n" +
		"A: Modo Peón · B: pausa · X: otra partida · Y: ayuda";

	// ------------------------------------------------ Jugador (escritorio + VR)

	static Jugador CrearJugador(HUD hud) {
		var inicio = new Vector3(-1.3f, 0.2f, -1.45f);
		var mira = new Vector3(0f, 0.97f, 0f) - inicio;
		float yaw = Mathf.Atan2(mira.x, mira.z) * Mathf.Rad2Deg;
		var raiz = Vacio("Jugador", null, inicio, new Vector3(0f, yaw, 0f));
		var cc = raiz.AddComponent<CharacterController>();
		cc.height = 1.75f;
		cc.radius = 0.22f;
		cc.center = new Vector3(0f, 0.875f, 0f);
		cc.stepOffset = 0.3f;
		cc.slopeLimit = 50f;
		cc.skinWidth = 0.02f;

		var cabeza = Vacio("Cabeza", raiz.transform, new Vector3(0f, 1.62f, 0f)).transform;
		cabeza.gameObject.AddComponent<AudioListener>();
		Camera NuevaCamara(string nombre, bool principal) {
			var go = Vacio(nombre, cabeza, Vector3.zero);
			var cam = go.AddComponent<Camera>();
			cam.nearClipPlane = 0.02f;
			cam.farClipPlane = 900f;
			cam.fieldOfView = 60f;
			cam.clearFlags = CameraClearFlags.Skybox;
			cam.allowHDR = false;
			cam.allowMSAA = true;
			if (principal) {
				go.tag = "MainCamera";
				cam.stereoTargetEye = StereoTargetEyeMask.Both;
			} else {
				cam.stereoTargetEye = StereoTargetEyeMask.None;
				cam.enabled = false;
			}
			return cam;
		}
		var principal = NuevaCamara("CamaraPrincipal", true);
		var camI = NuevaCamara("CamaraVRIzquierda", false);
		var camD = NuevaCamara("CamaraVRDerecha", false);
		var camARI = NuevaCamara("CamaraARIzquierda", false);
		var camARD = NuevaCamara("CamaraARDerecha", false);
		var webxr = cabeza.gameObject.AddComponent<WebXR.WebXRCamera>();
		var so = new SerializedObject(webxr);
		so.FindProperty("cameraMain").objectReferenceValue = principal;
		so.FindProperty("cameraL").objectReferenceValue = camI;
		so.FindProperty("cameraR").objectReferenceValue = camD;
		so.FindProperty("cameraARL").objectReferenceValue = camARI;
		so.FindProperty("cameraARR").objectReferenceValue = camARD;
		so.ApplyModifiedPropertiesWithoutUndo();
		webxr.enabled = false;

		//HUD pegado a la cabeza para VR (nombre y matrícula arriba a la derecha de la vista)
		var lvr = Lienzo("HUDVR", cabeza, new Vector3(0f, 0f, 1.2f), Vector3.zero, new Vector2(1600f, 1100f), 0.001f);
		Imagen("FondoNombre", lvr.transform, new Color(0f, 0f, 0f, 0.5f), new Vector2(520f, 400f), new Vector2(520f, 110f));
		Texto("NombreMatricula", lvr.transform, NOMBRE + "\nMatrícula: " + MATRICULA, 34, Color.white, new Vector2(515f, 400f), new Vector2(500f, 100f), TextAnchor.MiddleRight, FontStyle.Bold);
		var msgVR = Texto("Mensaje", lvr.transform, "", 36, Color.white, new Vector2(0f, -330f), new Vector2(1300f, 140f), TextAnchor.MiddleCenter, FontStyle.Bold);
		Sombra(msgVR, 2f);
		var ayudaVR = Imagen("AyudaVR", lvr.transform, new Color(0f, 0.02f, 0.06f, 0.75f), new Vector2(-470f, 150f), new Vector2(560f, 300f));
		Texto("Texto", ayudaVR.transform, "<b>CONTROLES VR</b>\nPalanca izq.: caminar\nPalanca der.: girar 30°\nGatillo: láser (tomar / botones)\nAgarre: tomar pieza cercana\nA: Modo Peón · B: pausa\nX: otra partida · Y: ayuda", 26, Color.white, Vector2.zero, new Vector2(520f, 280f), TextAnchor.MiddleLeft);
		lvr.GetComponent<Canvas>().sortingOrder = 10;
		foreach (var g in lvr.GetComponentsInChildren<Graphic>(true)) g.material = mUIEncima;
		hud.lienzoVR = lvr;
		hud.mensajeVR = msgVR;
		hud.panelAyudaVR = ayudaVR.gameObject;

		var izq = CrearMano(raiz.transform, true);
		var der = CrearMano(raiz.transform, false);

		var asiento = Vacio("AsientoBlancas", null, new Vector3(0f, 0.2f, -0.6f));
		var j = raiz.AddComponent<Jugador>();
		j.cabeza = cabeza;
		j.camara = principal;
		j.manoIzq = izq;
		j.manoDer = der;
		j.cc = cc;
		j.camaraWebXR = webxr;
		j.asiento = asiento.transform;
		return j;
	}

	static ManoVR CrearMano(Transform rig, bool izquierda) {
		var go = Vacio(izquierda ? "ManoIzquierda" : "ManoDerecha", rig, new Vector3(izquierda ? -0.2f : 0.2f, 1.1f, 0.3f));
		var modelo = Vacio("Modelo", go.transform, Vector3.zero).transform;
		Capsula("Mango", modelo, new Vector3(0f, -0.03f, -0.02f), new Vector3(0.035f, 0.055f, 0.035f), mPlasticoNegro, new Vector3(-30f, 0f, 0f), 0);
		Cilindro("Aro", modelo, new Vector3(0f, 0.02f, 0.03f), new Vector3(0.07f, 0.006f, 0.07f), mBordeLuz, new Vector3(60f, 0f, 0f), 0);
		var punta = Vacio("Punta", go.transform, new Vector3(0f, 0f, 0.06f)).transform;
		Esfera("Yema", punta, Vector3.zero, Vector3.one * 0.015f, mEspectral, 0);
		SinSombras(modelo.gameObject);
		SinSombras(punta.gameObject);
		var laserGo = Vacio("Laser", go.transform, Vector3.zero);
		var lr = laserGo.AddComponent<LineRenderer>();
		lr.useWorldSpace = true;
		lr.positionCount = 2;
		lr.sharedMaterial = mLaser;
		lr.startWidth = 0.004f;
		lr.endWidth = 0.001f;
		lr.shadowCastingMode = ShadowCastingMode.Off;
		lr.receiveShadows = false;
		lr.enabled = false;
		var punto = Esfera("PuntoLaser", go.transform, Vector3.zero, Vector3.one * 0.012f, mLaser, 0);
		SinSombras(punto);
		punto.SetActive(false);
		var m = go.AddComponent<ManoVR>();
		m.izquierda = izquierda;
		m.modelo = modelo;
		m.laser = lr;
		m.puntoLaser = punto.transform;
		m.punta = punta;
		return m;
	}

	// ------------------------------------------------ Director

	static void CrearDirector(TableroDGT tablero, RelojDGT reloj, Planilla[] planillas, Text[] placas, ManoFantasma[] manos, PantallaTransmision pantalla, Text monitor) {
		var go = new GameObject("Director");
		var d = go.AddComponent<Director>();
		d.tablero = tablero;
		d.reloj = reloj;
		d.planillas = planillas;
		d.placaBlancas = placas[0];
		d.placaNegras = placas[1];
		d.manoBlancas = manos[0];
		d.manoNegras = manos[1];
		d.pantalla = pantalla;
		d.monitor = monitor;

		var s = go.AddComponent<Sonidos>();
		s.pieza = sPieza;
		s.captura = sCaptura;
		s.tomar = sTomar;
		s.reloj = sReloj;
		s.ilegal = sIlegal;
		s.fantasma = sFantasma;
		s.susurro = sSusurro;
		s.boton = sBoton;
		s.encoger = sEncoger;
		s.crecer = sCrecer;
		s.jaque = sJaque;
		s.mate = sMate;
		var musica = go.AddComponent<AudioSource>();
		musica.clip = sMusica;
		musica.loop = true;
		musica.playOnAwake = true;
		musica.volume = 0.32f;
		musica.spatialBlend = 0f;
		s.musica = musica;
	}
}
