using System.Collections.Generic;
using System.Text;
using Ajedrez;
using UnityEngine;
using UnityEngine.UI;

//Pantalla gigante de la sala: muestra en vivo lo que "transmite" el tablero electrónico
//(diagrama, relojes, lista de jugadas y comentarios del fantasma).
public class PantallaTransmision : MonoBehaviour {

	public Image[] casillas = new Image[64];
	public Image[] piezas = new Image[64];
	public Sprite[] sprites = new Sprite[12];
	public Text nombreBlancas, nombreNegras, relojBlancas, relojNegras, estado, comentario, titulo, numeroJugada;
	public Text colNumeros, colBlancas, colNegras;
	public Image turnoBlancas, turnoNegras, puntoVivo;
	public Color claro = new Color(0.93f, 0.85f, 0.7f), oscuro = new Color(0.55f, 0.36f, 0.22f);
	public Color resaltado = new Color(0.95f, 0.8f, 0.25f), jaque = new Color(0.9f, 0.2f, 0.15f);

	TableroDGT tablero;
	RelojDGT reloj;
	float tiempoComentario;

	public void Conectar(TableroDGT t, RelojDGT r) {
		tablero = t;
		reloj = r;
		t.AlJugar += (j, san, usuario) => Refrescar();
		t.AlColocar += Refrescar;
		Refrescar();
	}

	public void Nombres(string blancas, string negras, string tituloPartida) {
		nombreBlancas.text = blancas;
		nombreNegras.text = negras;
		titulo.text = tituloPartida;
	}

	public void Estado(string texto) => estado.text = texto;

	public void Comentario(string texto, float segundos = 7f) {
		comentario.text = texto;
		tiempoComentario = segundos;
		comentario.color = new Color(comentario.color.r, comentario.color.g, comentario.color.b, 1f);
	}

	//Notación en español para el público: R rey, D dama, T torre, A alfil, C caballo
	public static string Es(string san) {
		var sb = new StringBuilder(san.Length);
		for (int i = 0; i < san.Length; i++) {
			char ch = san[i];
			bool pieza = (i == 0 || san[i - 1] == '=') && "KQRBN".IndexOf(ch) >= 0;
			sb.Append(pieza ? "RDTAC"["KQRBN".IndexOf(ch)] : ch);
		}
		return sb.ToString();
	}

	//Lista de jugadas en tres columnas (número, blancas, negras), solo las últimas líneas
	public static void Lista(List<string> historial, int maxLineas, Text numeros, Text blancas, Text negras) {
		var sn = new StringBuilder();
		var sb = new StringBuilder();
		var sg = new StringBuilder();
		int lineas = (historial.Count + 1) / 2;
		int desde = Mathf.Max(0, lineas - maxLineas);
		for (int l = desde; l < lineas; l++) {
			sn.Append(l + 1).Append(".\n");
			sb.Append(Es(historial[l * 2])).Append('\n');
			sg.Append(l * 2 + 1 < historial.Count ? Es(historial[l * 2 + 1]) : "").Append('\n');
		}
		if (numeros != null) numeros.text = sn.ToString();
		if (blancas != null) blancas.text = sb.ToString();
		if (negras != null) negras.text = sg.ToString();
	}

	public void Refrescar() {
		if (tablero == null) return;
		var p = tablero.pos;
		int jaqueEn = p.EnJaque() ? p.Rey(p.blancasJuegan) : -1;
		for (int i = 0; i < 64; i++) {
			bool esClara = (P.Columna(i) + P.Fila(i)) % 2 == 1;
			Color c = esClara ? claro : oscuro;
			if (tablero.ultima.HasValue && (tablero.ultima.Value.desde == i || tablero.ultima.Value.hasta == i)) c = Color.Lerp(c, resaltado, 0.55f);
			if (i == jaqueEn) c = Color.Lerp(c, jaque, 0.75f);
			casillas[i].color = c;
			int pz = p.c[i];
			piezas[i].enabled = pz != 0;
			if (pz != 0) piezas[i].sprite = sprites[pz > 0 ? pz - 1 : 6 + (-pz - 1)];
		}
		Lista(tablero.historial, 15, colNumeros, colBlancas, colNegras);
		numeroJugada.text = "Jugada " + p.numero + (p.blancasJuegan ? " · juegan blancas" : " · juegan negras");
	}

	void Update() {
		if (reloj != null) {
			relojBlancas.text = RelojDGT.Formato(reloj.tiempoBlancas);
			relojNegras.text = RelojDGT.Formato(reloj.tiempoNegras);
			turnoBlancas.enabled = reloj.corriendo && reloj.turnoBlancas;
			turnoNegras.enabled = reloj.corriendo && !reloj.turnoBlancas;
		}
		if (puntoVivo != null) puntoVivo.enabled = Mathf.Repeat(Time.time, 1.2f) < 0.8f;
		if (tiempoComentario > 0f) {
			tiempoComentario -= Time.deltaTime;
			var c = comentario.color;
			c.a = Mathf.Clamp01(tiempoComentario / 1.2f);
			comentario.color = c;
		}
	}
}
