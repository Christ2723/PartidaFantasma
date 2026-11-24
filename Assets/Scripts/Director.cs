using System.Collections;
using System.Collections.Generic;
using Ajedrez;
using UnityEngine;
using UnityEngine.UI;

public enum Modo { Exhibicion, Analisis, Duelo }

//Dirige la escena: el fantasma recuerda partidas históricas, deja analizar o juega un duelo.
public class Director : MonoBehaviour {

	public static Director I;

	public TableroDGT tablero;
	public RelojDGT reloj;
	public ManoFantasma manoBlancas, manoNegras;
	public PantallaTransmision pantalla;
	public Planilla[] planillas;
	public Text placaBlancas, placaNegras, monitor;
	public float pausaEntreJugadas = 1.2f;
	public int profundidadMotor = 3;

	public Modo modo { get; private set; }
	public int indicePartida { get; private set; }
	public bool pausado { get; private set; }
	public bool pensando { get; private set; }
	public int plyHistorico { get; private set; }

	Coroutine rutina;
	string ultimoAviso = "";

	PartidaHistorica Actual => Partidas.TODAS[indicePartida];

	void Awake() {
		I = this;
	}

	void Start() {
		tablero.politicaTomar = PuedeTomar;
		tablero.AlJugar += AlJugar;
		tablero.AlRechazar += m => Avisar(m);
		pantalla.Conectar(tablero, reloj);
		foreach (var p in planillas) p.Conectar(tablero);
		IniciarExhibicion(0, false);
	}

	// ------------------------------------------------ Acciones (botones del árbitro, teclas, controles VR)

	public void AlternarFantasma() {
		if (modo == Modo.Exhibicion) {
			pausado = !pausado;
			Avisar(pausado ? "El fantasma se detiene a pensar (pausa)" : "El fantasma sigue recordando...");
			RefrescarMonitor();
			return;
		}
		//Desde el análisis o el duelo: vuelve a la partida histórica donde se quedó
		Detener();
		modo = Modo.Exhibicion;
		pausado = false;
		MostrarNombres();
		rutina = StartCoroutine(Exhibir(true));
		Avisar("El fantasma retoma \"" + Actual.titulo + "\"");
	}

	public void SiguientePartida() => IniciarExhibicion((indicePartida + 1) % Partidas.TODAS.Length, true);

	public void IniciarExhibicion(int indice, bool animar) {
		Detener();
		modo = Modo.Exhibicion;
		pausado = false;
		indicePartida = indice;
		plyHistorico = 0;
		MostrarNombres();
		reloj.Configurar(6000f);
		rutina = StartCoroutine(Exhibir(animar));
	}

	public void IniciarDuelo() {
		Detener();
		modo = Modo.Duelo;
		pausado = false;
		placaBlancas.text = "TÚ\n<size=60>JUGADOR</size>";
		placaNegras.text = "EL FANTASMA\n<size=60>RIVAL INVISIBLE</size>";
		pantalla.Nombres("TÚ", "EL FANTASMA", "DUELO CONTRA EL FANTASMA · 10 minutos");
		foreach (var p in planillas) p.Encabezado("Tú", "El Fantasma", "Sala fantasma, hoy");
		rutina = StartCoroutine(PrepararDuelo());
	}

	IEnumerator PrepararDuelo() {
		yield return EsperarTablero();
		yield return tablero.Sincronizar(Posicion.Inicial(), null);
		reloj.Configurar(600f);
		pantalla.Estado("Juegas con blancas. Mueve una pieza.");
		pantalla.Comentario("El fantasma acepta el reto...");
		Avisar("Duelo: juegas con las blancas. Toma una pieza y muévela.");
		RefrescarMonitor();
		rutina = null;
	}

	public void Reiniciar() {
		switch (modo) {
			case Modo.Duelo: IniciarDuelo(); break;
			case Modo.Exhibicion: IniciarExhibicion(indicePartida, true); break;
			default:
				Detener();
				rutina = StartCoroutine(ReiniciarAnalisis());
				break;
		}
	}

	IEnumerator ReiniciarAnalisis() {
		yield return EsperarTablero();
		yield return tablero.Sincronizar(Posicion.Inicial(), null);
		reloj.Configurar(6000f);
		Avisar("Tablero en la posición inicial");
		rutina = null;
	}

	public void AlternarPeon() {
		if (Jugador.I != null) Jugador.I.AlternarPeon();
	}

	// ------------------------------------------------ Exhibición

	void MostrarNombres() {
		var p = Actual;
		placaBlancas.text = p.blancas.ToUpper() + "\n<size=60>" + p.codBlancas + "</size>";
		placaNegras.text = p.negras.ToUpper() + "\n<size=60>" + p.codNegras + "</size>";
		pantalla.Nombres(p.blancas + "  (" + p.codBlancas + ")", p.negras + "  (" + p.codNegras + ")", p.titulo.ToUpper() + "  ·  " + p.lugar);
		foreach (var pl in planillas) pl.Encabezado(p.blancas, p.negras, p.lugar);
		RefrescarMonitor();
	}

	IEnumerator EsperarTablero() {
		while (tablero.Ocupado) yield return null;
	}

	//Posición de la partida histórica después de n medias jugadas
	Posicion PosicionHistorica(int n, List<string> historial) {
		var p = Posicion.Inicial();
		for (int i = 0; i < n; i++) {
			if (!p.DesdeSan(Actual.jugadas[i], out Jugada j)) break;
			historial?.Add(p.San(j));
			p.Aplicar(j);
		}
		return p;
	}

	IEnumerator Exhibir(bool sincronizar) {
		yield return EsperarTablero();
		var partida = Actual;
		if (sincronizar) {
			var hist = new List<string>();
			var p = PosicionHistorica(plyHistorico, hist);
			yield return tablero.Sincronizar(p, hist);
		}
		reloj.corriendo = true;
		pantalla.Estado("El fantasma recuerda: " + partida.titulo);
		pantalla.Comentario("\"" + partida.titulo + "\" · " + partida.blancas + " vs " + partida.negras, 6f);
		RefrescarMonitor();
		yield return Esperar(plyHistorico == 0 ? 2.5f : 1f);
		while (plyHistorico < partida.jugadas.Length) {
			yield return Esperar(pausaEntreJugadas);
			if (!tablero.pos.DesdeSan(partida.jugadas[plyHistorico], out Jugada j)) {
				Debug.LogError("Jugada no válida en " + partida.titulo + ": " + partida.jugadas[plyHistorico]);
				break;
			}
			yield return tablero.JugadaFantasma(j, j.pieza > 0 ? manoBlancas : manoNegras, reloj);
		}
		reloj.Detener();
		pantalla.Estado("Resultado " + partida.resultado + " · " + partida.final);
		Avisar("Fin de \"" + partida.titulo + "\" (" + partida.resultado + ")");
		yield return Esperar(14f);
		rutina = null;
		SiguientePartida();
	}

	//Espera que respeta la pausa
	IEnumerator Esperar(float segundos) {
		while (segundos > 0f || pausado) {
			if (!pausado) segundos -= Time.deltaTime;
			yield return null;
		}
	}

	void Detener() {
		if (rutina != null) StopCoroutine(rutina);
		rutina = null;
		pensando = false;
	}

	// ------------------------------------------------ Reglas de interacción

	bool PuedeTomar(PiezaFisica pz) {
		if (pensando) {
			Avisar("Espera... el fantasma está pensando");
			return false;
		}
		switch (modo) {
			case Modo.Exhibicion:
				Detener();
				modo = Modo.Analisis;
				pausado = false;
				pantalla.Estado("Análisis libre: mueves tú (blancas y negras)");
				Avisar("El fantasma se aparta y te deja analizar. Botón ▶ (o F) para que continúe.");
				RefrescarMonitor();
				return true;
			case Modo.Duelo:
				if (!pz.Blanca) {
					Avisar("En el duelo juegas con las blancas");
					return false;
				}
				if (!tablero.pos.blancasJuegan) {
					Avisar("Es el turno del fantasma");
					return false;
				}
				return true;
		}
		return true;
	}

	void AlJugar(Jugada j, string san, bool usuario) {
		if (!usuario && modo == Modo.Exhibicion) {
			if (Actual.comentarios.TryGetValue(plyHistorico, out string c)) pantalla.Comentario(c, 8f);
			plyHistorico++;
		}
		var s = tablero.pos.Situacion();
		bool blancasMovieron = j.pieza > 0;
		if (usuario && modo != Modo.Exhibicion) reloj.Pulsar(blancasMovieron);
		if (s == Posicion.Estado.Jaque && Sonidos.I != null) Sonidos.Tocar(Sonidos.I.jaque, tablero.transform.position, 0.6f);
		bool fin = s == Posicion.Estado.Mate || s == Posicion.Estado.Ahogado || s == Posicion.Estado.MaterialInsuficiente || s == Posicion.Estado.Regla50;
		if (fin && modo != Modo.Exhibicion) {
			reloj.Detener();
			string texto;
			if (s == Posicion.Estado.Mate) {
				if (modo == Modo.Duelo) texto = blancasMovieron ? "¡Jaque mate! Venciste al fantasma" : "Jaque mate... el fantasma gana";
				else texto = "¡Jaque mate! Ganan las " + (blancasMovieron ? "blancas" : "negras");
				if (Sonidos.I != null) Sonidos.Tocar(Sonidos.I.mate, tablero.transform.position);
			} else texto = "Tablas (" + (s == Posicion.Estado.Ahogado ? "rey ahogado" : s == Posicion.Estado.Regla50 ? "regla de 50 jugadas" : "material insuficiente") + ")";
			pantalla.Estado(texto);
			Avisar(texto);
			return;
		}
		if (s == Posicion.Estado.Mate && Sonidos.I != null) Sonidos.Tocar(Sonidos.I.mate, tablero.transform.position);
		if (modo == Modo.Duelo && usuario && !tablero.pos.blancasJuegan) rutina = StartCoroutine(RespuestaFantasma());
	}

	IEnumerator RespuestaFantasma() {
		pensando = true;
		pantalla.Estado("El fantasma piensa...");
		yield return new WaitForSeconds(0.9f);
		yield return EsperarTablero();
		if (Motor.MejorJugada(tablero.pos, profundidadMotor, out Jugada j)) {
			yield return tablero.JugadaFantasma(j, manoNegras, reloj);
			if (tablero.pos.Situacion() == Posicion.Estado.EnJuego || tablero.pos.Situacion() == Posicion.Estado.Jaque)
				pantalla.Estado("Tu turno (blancas)");
		}
		pensando = false;
		rutina = null;
	}

	// ------------------------------------------------ Avisos

	public void Avisar(string texto) {
		ultimoAviso = texto;
		if (HUD.I != null) HUD.I.Mensaje(texto);
		RefrescarMonitor();
	}

	void RefrescarMonitor() {
		if (monitor == null) return;
		string m = modo == Modo.Exhibicion ? (pausado ? "EXHIBICIÓN (EN PAUSA)" : "EXHIBICIÓN DEL FANTASMA")
			: modo == Modo.Duelo ? "DUELO CONTRA EL FANTASMA" : "ANÁLISIS LIBRE";
		monitor.text = "<b>MODO: " + m + "</b>\nPartida: " + Actual.titulo + " (" + (indicePartida + 1) + "/" + Partidas.TODAS.Length + ")\n\n" + ultimoAviso;
	}
}
