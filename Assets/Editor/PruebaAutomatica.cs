using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ajedrez;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.UI;

//Prueba automática en modo Play: reglas del ajedrez (perft), las 5 partidas históricas jugada por jugada,
//el fantasma moviendo piezas con su mano, el análisis libre, el duelo contra el motor, el Modo Peón,
//los botones del árbitro, el nombre y matrícula en la esquina superior derecha y las capturas de pantalla.
//Uso: Unity -batchmode -projectPath . -executeMethod PruebaAutomatica.Ejecutar   (sin -quit)
public static class PruebaAutomatica {

	const string CAPTURAS = "Documentacion/capturas";

	static IEnumerator<float> pasos;
	static double siguiente;
	static readonly List<string> resultados = new List<string>();
	static readonly List<string> errores = new List<string>();
	static int fallos;
	static bool xrInicioOriginal;

	[MenuItem("Fantasma/Prueba automática")]
	public static void Ejecutar() {
		//Sin recarga de dominio al entrar en Play: si no, se pierden los pasos de la prueba
		EditorSettings.enterPlayModeOptionsEnabled = true;
		EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
		Directory.CreateDirectory(CAPTURAS);
		//Durante la prueba no se intenta abrir un visor (no hay ninguno conectado)
		var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
		if (xr != null) {
			xrInicioOriginal = xr.InitManagerOnStart;
			xr.InitManagerOnStart = false;
		}
		EditorSceneManager.OpenScene(ConstructorProyecto.RUTA_ESCENA);
		Application.logMessageReceived += (msg, pila, tipo) => {
			if ((tipo == LogType.Exception || tipo == LogType.Error) && !pila.Contains("UnityEditor.Search")) errores.Add(msg + "\n" + pila);
		};
		resultados.Clear();
		errores.Clear();
		fallos = 0;
		pasos = Pasos();
		siguiente = EditorApplication.timeSinceStartup + 0.5;
		EditorApplication.update += Avanzar;
		EditorApplication.EnterPlaymode();
	}

	static void Avanzar() {
		if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
		bool sigue;
		try {
			sigue = pasos.MoveNext();
		} catch (System.Exception e) {
			Revisar(false, "Excepción en la prueba: " + e);
			sigue = false;
		}
		if (sigue) {
			siguiente = EditorApplication.timeSinceStartup + pasos.Current;
			return;
		}
		EditorApplication.update -= Avanzar;
		foreach (var e in errores.Distinct()) Revisar(false, "Error en consola: " + e);
		resultados.Add(fallos == 0 ? "RESULTADO: TODO OK" : "RESULTADO: " + fallos + " FALLOS");
		Directory.CreateDirectory("Logs");
		File.WriteAllLines("Logs/prueba.txt", resultados);
		Debug.Log("PRUEBA\n" + string.Join("\n", resultados));
		Time.timeScale = 1f;
		EditorSettings.enterPlayModeOptionsEnabled = false;
		EditorApplication.ExitPlaymode();
		var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
		if (xr != null) {
			xr.InitManagerOnStart = xrInicioOriginal;
			EditorUtility.SetDirty(xr);
			AssetDatabase.SaveAssets();
		}
		if (Application.isBatchMode) EditorApplication.Exit(fallos == 0 ? 0 : 1);
	}

	static void Revisar(bool ok, string que) {
		resultados.Add((ok ? "[OK]    " : "[FALLA] ") + que);
		if (!ok) fallos++;
	}

	// ------------------------------------------------ Capturas

	public static void Foto(string nombre, int ancho = 1600, int alto = 900) {
		var cam = Jugador.I.camara;
		var rt = new RenderTexture(ancho, alto, 24) { antiAliasing = 4 };
		var lienzos = Object.FindObjectsByType<Canvas>().Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay && c.isActiveAndEnabled).ToList();
		var ordenes = lienzos.Select(l => l.sortingOrder).ToList();
		foreach (var l in lienzos) {
			l.renderMode = RenderMode.ScreenSpaceCamera;
			l.worldCamera = cam;
			l.planeDistance = Mathf.Max(cam.nearClipPlane * 3f, Mathf.Min(0.5f, cam.farClipPlane * 0.5f));
			l.sortingOrder = 1000;   //encima de los letreros 3D, como en el juego (lienzo superpuesto)
		}
		Canvas.ForceUpdateCanvases();
		cam.targetTexture = rt;
		cam.Render();
		cam.targetTexture = null;
		for (int i = 0; i < lienzos.Count; i++) {
			lienzos[i].renderMode = RenderMode.ScreenSpaceOverlay;
			lienzos[i].sortingOrder = ordenes[i];
		}
		RenderTexture.active = rt;
		var tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
		tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
		tex.Apply();
		RenderTexture.active = null;
		File.WriteAllBytes(CAPTURAS + "/" + nombre + ".png", tex.EncodeToPNG());
		Object.DestroyImmediate(tex);
		rt.Release();
	}

	// ------------------------------------------------ Pruebas de reglas (sin escena)

	static void PruebasReglas() {
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var inicial = Posicion.Inicial();
		Revisar(inicial.Perft(1) == 20 && inicial.Perft(2) == 400 && inicial.Perft(3) == 8902, "Perft posición inicial (20 / 400 / 8902)");
		var kiwi = Posicion.DesdeFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
		Revisar(kiwi.Perft(1) == 48 && kiwi.Perft(2) == 2039, "Perft 'Kiwipete' con enroques, al paso y clavadas (48 / 2039) = " + kiwi.Perft(2));
		var p3 = Posicion.DesdeFen("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
		Revisar(p3.Perft(3) == 2812 && p3.Perft(4) == 43238, "Perft final de torres (2812 / 43238)");
		var p5 = Posicion.DesdeFen("n1n5/PPPk4/8/8/8/8/4Kppp/5N1N b - - 0 1");
		Revisar(p5.Perft(1) == 24 && p5.Perft(2) == 496 && p5.Perft(3) == 9483, "Perft coronaciones (24 / 496 / 9483)");
		Revisar(Posicion.DesdeFen(Posicion.INICIAL).Fen() == Posicion.INICIAL, "FEN de ida y vuelta");

		//Partidas históricas: todas las jugadas son legales, la notación se regenera igual y el final es correcto
		int[] plies = { 87, 45, 33, 82, 37 };
		bool[] terminaEnMate = { false, true, true, true, false };
		for (int g = 0; g < Partidas.TODAS.Length; g++) {
			var partida = Partidas.TODAS[g];
			var p = Posicion.Inicial();
			int ok = 0;
			string primeraMala = null;
			foreach (var san in partida.jugadas) {
				if (!p.DesdeSan(san, out Jugada j)) { primeraMala = san; break; }
				string generado = p.San(j);
				if (generado.Replace("+", "").Replace("#", "") != san.Replace("+", "").Replace("#", "").Replace("!", "").Replace("?", "")) { primeraMala = san + " (generado " + generado + ")"; break; }
				if (san.EndsWith("#") != generado.EndsWith("#") || (san.EndsWith("+") != generado.EndsWith("+"))) { primeraMala = san + " (jaque: " + generado + ")"; break; }
				p.Aplicar(j);
				ok++;
			}
			bool mate = p.Situacion() == Posicion.Estado.Mate;
			Revisar(primeraMala == null && ok == partida.jugadas.Length && ok == plies[g] && mate == terminaEnMate[g],
				"Partida '" + partida.titulo + "': " + ok + "/" + partida.jugadas.Length + " jugadas legales" + (terminaEnMate[g] ? ", termina en mate" : "") + (primeraMala != null ? " — problema en " + primeraMala : ""));
		}
		//Comentarios en jugadas que existen
		foreach (var partida in Partidas.TODAS) Revisar(partida.comentarios.Keys.All(k => k >= 0 && k < partida.jugadas.Length), "Comentarios dentro de la partida '" + partida.titulo + "'");

		//Motor: encuentra un mate en 1 y responde rápido
		var mateEn1 = Posicion.DesdeFen("6k1/5ppp/8/8/8/8/5PPP/3R2K1 w - - 0 1");
		Motor.MejorJugada(mateEn1, 3, out Jugada mj);
		Revisar(mateEn1.San(mj) == "Rd8#", "El motor del fantasma encuentra el mate en 1 (Td8#): " + mateEn1.San(mj));
		var t0 = sw.ElapsedMilliseconds;
		var tras = Posicion.Inicial();
		tras.DesdeSan("e4", out Jugada e4);
		tras.Aplicar(e4);
		Motor.MejorJugada(tras, 3, out Jugada resp);
		long ms = sw.ElapsedMilliseconds - t0;
		Revisar(ms < 3000, "El motor responde a 1.e4 con " + tras.San(resp) + " en " + ms + " ms (" + Motor.nodos + " nodos)");
	}

	// ------------------------------------------------ Pasos en modo Play

	static IEnumerator<float> Pasos() {
		PruebasReglas();
		yield return 1.5f;

		var d = Director.I;
		var t = TableroDGT.I;
		var j = Jugador.I;
		Revisar(d != null && t != null && j != null && HUD.I != null, "La escena carga con director, tablero, jugador y HUD");

		//Nombre y matrícula arriba a la derecha
		var nombre = HUD.I.lienzoPantalla.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.name == "NombreMatricula");
		Revisar(nombre != null && nombre.text.Contains(ConstructorProyecto.NOMBRE) && nombre.text.Contains(ConstructorProyecto.MATRICULA)
			&& nombre.rectTransform.anchorMin == Vector2.one && nombre.alignment == TextAnchor.UpperRight, "Nombre y matrícula en la esquina superior derecha");
		var nombreVR = HUD.I.lienzoVR.GetComponentsInChildren<Text>(true).FirstOrDefault(x => x.name == "NombreMatricula");
		Revisar(nombreVR != null && nombreVR.text.Contains(ConstructorProyecto.MATRICULA) && nombreVR.rectTransform.localPosition.x > 0 && nombreVR.rectTransform.localPosition.y > 0,
			"Nombre y matrícula también arriba a la derecha dentro del visor VR");
		Revisar(!j.EnVR && j.manoIzq.gameObject.activeSelf == false, "Sin visor conectado arranca en modo escritorio");

		Revisar(t.PiezasEnTablero().Count() == 32 && t.PiezaEn(4) != null && t.PiezaEn(4).pieza == P.REY && t.PiezaEn(59).pieza == -P.DAMA, "32 piezas en la posición inicial (rey en e1, dama negra en d8)");
		var rey = t.PiezaEn(4);
		Revisar(Vector3.Distance(rey.transform.position, t.Centro(4)) < 0.001f, "Las piezas están sobre el centro de sus casillas");
		Foto("01_inicio");
		HUD.I.AlternarAyuda();   //el resto de capturas sin el panel de ayuda

		//Exhibición: el fantasma juega Kasparov - Topalov
		Time.timeScale = 3f;
		float limite = 60f;
		while (t.historial.Count < 4 && limite > 0f) { limite -= 0.5f; yield return 0.5f; }
		Revisar(d.modo == Modo.Exhibicion && t.historial.Count >= 4 && t.historial[0] == "e4" && t.historial[1] == "d6" && t.historial[2] == "d4",
			"El fantasma reproduce la partida histórica (" + string.Join(" ", t.historial.Take(4)) + ")");
		var pant = d.pantalla;
		Revisar(pant.colBlancas.text.Contains("e4") && pant.colNegras.text.Contains("d6") && pant.nombreBlancas.text.Contains("Kasparov"), "La pantalla gigante transmite las jugadas y los nombres");
		Revisar(d.reloj.corriendo && d.reloj.tiempoBlancas < 6000f && d.reloj.tiempoNegras < 6000f, "El reloj corre y el fantasma lo pulsa en cada jugada");
		Revisar(d.planillas[0].colBlancas.text.Contains("e4"), "La planilla se escribe sola");
		//Foto con la mano del fantasma en el aire
		Time.timeScale = 1f;
		limite = 10f;
		while (!t.Ocupado && limite > 0f) { limite -= 0.05f; yield return 0.05f; }
		yield return 0.55f;
		j.Ubicar(new Vector3(-0.55f, 0.2f, -1.25f), 22f, 30f);
		yield return 0.1f;
		Foto("02_mano_fantasma");
		Time.timeScale = 3f;

		//Vistas generales
		j.Ubicar(new Vector3(-7.5f, 0f, -3.2f), 62f, 8f);
		yield return 0.2f;
		Foto("03_sala_publico");
		j.Ubicar(new Vector3(0.9f, 0.2f, -2.0f), 55f, 2f);
		yield return 0.2f;
		Foto("04_pantalla_gigante");
		j.Ubicar(new Vector3(-0.3f, 0.2f, -0.95f), 8f, 52f);
		j.camara.fieldOfView = 45f;
		yield return 0.2f;
		Foto("05_tablero_cerca");
		j.camara.fieldOfView = 60f;
		j.Ubicar(new Vector3(-1.05f, 0.2f, -2.05f), -32f, 30f);
		yield return 0.2f;
		Foto("06_consola_arbitro");
		j.Ubicar(new Vector3(9f, 0.0f, 9f), -135f, -12f);
		yield return 0.2f;
		Foto("07_vacio");

		//Análisis libre: tomar una pieza detiene al fantasma
		while (t.Ocupado) yield return 0.05f;
		yield return 0.1f;
		while (t.Ocupado) yield return 0.05f;
		var raton = new Apuntador("PruebaRaton");
		int n = t.historial.Count;
		var jugadas = t.pos.Legales();
		var jugada = jugadas.First(x => !x.EsCaptura && x.corona == 0 && !x.enroque);
		var pz = t.PiezaEn(jugada.desde);
		bool tomo = t.Tomar(pz, raton);
		Revisar(tomo && d.modo == Modo.Analisis, "Al tomar una pieza el fantasma se aparta (modo análisis)");
		Revisar(t.MarcadoresVisibles() > 0, "Se marcan las casillas legales de la pieza tomada (" + t.MarcadoresVisibles() + ")");
		raton.sostenida = null;
		t.Soltar(pz, jugada.hasta);
		yield return 0.5f;
		Revisar(t.historial.Count == n + 1 && t.PiezaEn(jugada.hasta) == pz, "La persona mueve la pieza y el tablero la registra (" + t.historial.Last() + ")");
		//Jugada ilegal: la pieza vuelve
		var otra = t.PiezasEnTablero().First(x => x.Blanca == t.pos.blancasJuegan && x.Tipo == P.REY);
		int origen = otra.casilla;
		int destinoMalo = Enumerable.Range(0, 64).First(c => t.PiezaEn(c) == null && !t.pos.LegalesDesde(origen).Any(x => x.hasta == c));
		t.Tomar(otra, raton);
		raton.sostenida = null;
		t.Soltar(otra, destinoMalo);
		yield return 0.6f;
		Revisar(t.historial.Count == n + 1 && otra.casilla == origen && Vector3.Distance(otra.transform.position, t.Centro(origen)) < 0.002f, "Una jugada ilegal se rechaza y la pieza vuelve a su casilla");

		//Retomar la exhibición: el tablero vuelve a la partida histórica
		d.AlternarFantasma();
		limite = 20f;
		while ((d.modo != Modo.Exhibicion || t.Ocupado || t.historial.Count != d.plyHistorico) && limite > 0f) { limite -= 0.2f; yield return 0.2f; }
		var esperado = Posicion.Inicial();
		foreach (var s in Partidas.TODAS[0].jugadas.Take(d.plyHistorico)) { esperado.DesdeSan(s, out Jugada jj); esperado.Aplicar(jj); }
		Revisar(t.pos.Fen() == esperado.Fen(), "Al seguir, las piezas vuelan solas a la posición histórica");

		//Siguiente partida con el botón del árbitro
		var boton = Object.FindObjectsByType<BotonFisico>().First(b => b.accion == AccionBoton.SiguientePartida);
		boton.Presionar(raton);
		yield return 0.3f;
		Revisar(d.indicePartida == 1 && d.pantalla.titulo.text.Contains("INMORTAL"), "El botón 'siguiente partida' cambia a La Partida Inmortal");
		limite = 30f;
		while (t.historial.Count < 2 && limite > 0f) { limite -= 0.5f; yield return 0.5f; }
		Revisar(t.historial.Count >= 2 && t.historial[0] == "e4" && t.historial[1] == "e5", "El fantasma juega la Inmortal (1.e4 e5)");

		//Duelo contra el fantasma
		d.IniciarDuelo();
		limite = 20f;
		while ((t.Ocupado || t.historial.Count > 0 || t.pos.Fen() != Posicion.INICIAL) && limite > 0f) { limite -= 0.2f; yield return 0.2f; }
		yield return 0.5f;
		Revisar(t.pos.Fen() == Posicion.INICIAL && d.modo == Modo.Duelo, "El duelo empieza en la posición inicial");
		Revisar(!t.Tomar(t.PiezaEn(52), raton), "En el duelo no se pueden tomar las piezas negras");
		var peonE = t.PiezaEn(12);
		t.Tomar(peonE, raton);
		raton.sostenida = null;
		t.Soltar(peonE, 28);
		limite = 20f;
		while ((t.historial.Count < 2 || t.Ocupado) && limite > 0f) { limite -= 0.25f; yield return 0.25f; }
		Revisar(t.historial.Count == 2 && t.historial[0] == "e4" && t.pos.blancasJuegan, "El fantasma responde en el duelo: 1.e4 " + (t.historial.Count > 1 ? t.historial[1] : "?"));

		//Modo Peón
		Time.timeScale = 1f;
		j.Ubicar(new Vector3(-0.72f, 0.2f, -1.95f), 20f, 15f);
		j.AlternarPeon();
		yield return 1.5f;
		Revisar(j.EnPeon && Mathf.Abs(j.Escala - Jugador.ESCALA_PEON) < 1e-5f && t.CasillaEn(j.transform.position) >= 0, "Modo Peón: el visitante se encoge y aparece sobre el tablero");
		Foto("08_modo_peon");
		j.Ubicar(t.Centro(19) - t.transform.forward * 0.02f, 0f, -2f);
		yield return 0.2f;
		Foto("09_modo_peon_2");
		j.AlternarPeon();
		yield return 1.5f;
		Revisar(!j.EnPeon && Mathf.Abs(j.Escala - 1f) < 1e-5f, "Modo Peón: vuelve a su tamaño normal");

		//Sentarse
		j.Sentarse();
		yield return 0.3f;
		Revisar(j.Sentado, "El visitante puede sentarse frente al tablero");
		Foto("10_sentado");
		j.Levantarse();
		Time.timeScale = 1f;
		yield return 0.5f;
	}
}
