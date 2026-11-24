using UnityEngine;
using UnityEngine.UI;

//Reloj digital de torneo con dos pantallas LCD y dos pulsadores
public class RelojDGT : MonoBehaviour {

	public Text pantallaBlancas, pantallaNegras;
	public Transform pulsadorBlancas, pulsadorNegras;
	public Image indicadorBlancas, indicadorNegras;

	public float tiempoBlancas = 6000f, tiempoNegras = 6000f;
	public bool corriendo;
	public bool turnoBlancas = true;

	float hundidoBlancas, hundidoNegras;
	Vector3 baseBlancas, baseNegras;

	void Awake() {
		baseBlancas = pulsadorBlancas.localPosition;
		baseNegras = pulsadorNegras.localPosition;
	}

	public void Configurar(float segundos, bool empiezanBlancas = true) {
		tiempoBlancas = tiempoNegras = segundos;
		turnoBlancas = empiezanBlancas;
		corriendo = true;
		hundidoBlancas = empiezanBlancas ? 0f : 1f;
		hundidoNegras = empiezanBlancas ? 1f : 0f;
		Refrescar();
	}

	public void Detener() {
		corriendo = false;
		Refrescar();
	}

	//Posición (mundo) del pulsador que aprieta ese bando al terminar su jugada
	public Vector3 Boton(bool blancas) => (blancas ? pulsadorBlancas : pulsadorNegras).position;

	//El bando que acaba de jugar aprieta su pulsador y arranca el tiempo del rival
	public void Pulsar(bool blancasPulsan) {
		turnoBlancas = !blancasPulsan;
		corriendo = true;
		if (blancasPulsan) { hundidoBlancas = 1f; hundidoNegras = 0f; }
		else { hundidoNegras = 1f; hundidoBlancas = 0f; }
		if (Sonidos.I != null) Sonidos.Tocar(Sonidos.I.reloj, Boton(blancasPulsan), 0.9f);
		Refrescar();
	}

	void Update() {
		if (corriendo) {
			if (turnoBlancas) tiempoBlancas = Mathf.Max(0f, tiempoBlancas - Time.deltaTime);
			else tiempoNegras = Mathf.Max(0f, tiempoNegras - Time.deltaTime);
		}
		pulsadorBlancas.localPosition = Vector3.Lerp(pulsadorBlancas.localPosition, baseBlancas - Vector3.up * (0.006f * hundidoBlancas), 0.4f);
		pulsadorNegras.localPosition = Vector3.Lerp(pulsadorNegras.localPosition, baseNegras - Vector3.up * (0.006f * hundidoNegras), 0.4f);
		Refrescar();
	}

	public static string Formato(float s) {
		int total = Mathf.CeilToInt(s);
		int h = total / 3600, m = (total / 60) % 60, seg = total % 60;
		return h > 0 ? h + ":" + m.ToString("00") + ":" + seg.ToString("00") : m + ":" + seg.ToString("00");
	}

	void Refrescar() {
		if (pantallaBlancas != null) pantallaBlancas.text = Formato(tiempoBlancas);
		if (pantallaNegras != null) pantallaNegras.text = Formato(tiempoNegras);
		bool parpadeo = Mathf.Repeat(Time.time, 1f) < 0.5f;
		if (indicadorBlancas != null) indicadorBlancas.enabled = corriendo && turnoBlancas && parpadeo;
		if (indicadorNegras != null) indicadorNegras.enabled = corriendo && !turnoBlancas && parpadeo;
	}
}
