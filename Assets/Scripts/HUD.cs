using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//Interfaz: nombre y matrícula (esquina superior derecha), avisos y ayuda.
//En escritorio usa un lienzo de pantalla; en realidad virtual, un lienzo pegado a la cabeza.
public class HUD : MonoBehaviour {

	public static HUD I;

	public Canvas lienzoPantalla, lienzoVR;
	public Text mensaje, mensajeVR, ayuda;
	public GameObject panelAyuda, panelAyudaVR;
	public Image fundido;

	float tiempoMensaje;

	void Awake() {
		I = this;
		ModoVR(false);
		if (fundido != null) fundido.color = new Color(0f, 0f, 0f, 0f);
	}

	public void ModoVR(bool vr) {
		lienzoPantalla.gameObject.SetActive(!vr);
		lienzoVR.gameObject.SetActive(vr);
	}

	public void Mensaje(string texto, float segundos = 4.5f) {
		mensaje.text = texto;
		mensajeVR.text = texto;
		tiempoMensaje = segundos;
	}

	public void AlternarAyuda() {
		bool v = !panelAyuda.activeSelf;
		panelAyuda.SetActive(v);
		if (panelAyudaVR != null) panelAyudaVR.SetActive(v);
	}

	public IEnumerator Fundido(float hasta, float duracion) {
		if (fundido == null) yield break;
		float desde = fundido.color.a;
		for (float t = 0f; t < 1f; t += Time.deltaTime / duracion) {
			fundido.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, t));
			yield return null;
		}
		fundido.color = new Color(0f, 0f, 0f, hasta);
	}

	void Update() {
		if (tiempoMensaje > 0f) tiempoMensaje -= Time.deltaTime;
		float a = Mathf.Clamp01(tiempoMensaje / 0.8f);
		SetAlfa(mensaje, a);
		SetAlfa(mensajeVR, a);
	}

	static void SetAlfa(Text t, float a) {
		if (t == null) return;
		var c = t.color;
		c.a = a;
		t.color = c;
		var sombra = t.GetComponent<Shadow>();
		if (sombra != null) {
			var s = sombra.effectColor;
			s.a = a * 0.8f;
			sombra.effectColor = s;
		}
	}
}
