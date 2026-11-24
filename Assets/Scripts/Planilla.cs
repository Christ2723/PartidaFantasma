using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//Planilla de anotación sobre la mesa. El bolígrafo se mueve solo y escribe cada jugada.
public class Planilla : MonoBehaviour {

	public Text encabezado, colNumeros, colBlancas, colNegras;
	public Transform boligrafo;

	Vector3 baseBoli;
	Quaternion rotBoli;

	void Awake() {
		if (boligrafo != null) {
			baseBoli = boligrafo.localPosition;
			rotBoli = boligrafo.localRotation;
		}
	}

	public void Conectar(TableroDGT t) {
		t.AlJugar += (j, san, usuario) => { Refrescar(t); Escribir(); };
		t.AlColocar += () => Refrescar(t);
		Refrescar(t);
	}

	public void Encabezado(string blancas, string negras, string lugar) {
		encabezado.text = "PLANILLA OFICIAL  ·  Ronda ∞\nBlancas: " + blancas + "\nNegras: " + negras + "\n" + lugar;
	}

	void Refrescar(TableroDGT t) => PantallaTransmision.Lista(t.historial, 22, colNumeros, colBlancas, colNegras);

	void Escribir() {
		if (boligrafo != null && isActiveAndEnabled) StartCoroutine(Garabato());
	}

	IEnumerator Garabato() {
		for (float t = 0f; t < 0.7f; t += Time.deltaTime) {
			boligrafo.localPosition = baseBoli + new Vector3(Mathf.Sin(t * 40f) * 0.006f, 0.004f + Mathf.Abs(Mathf.Sin(t * 13f)) * 0.004f, t * 0.02f);
			boligrafo.localRotation = rotBoli * Quaternion.Euler(Mathf.Sin(t * 30f) * 4f, 0f, 0f);
			yield return null;
		}
		boligrafo.localPosition = baseBoli;
		boligrafo.localRotation = rotBoli;
	}
}
