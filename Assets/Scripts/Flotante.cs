using UnityEngine;

//Objetos que flotan en el vacío (piezas gigantes, la luna...): giran despacio y suben y bajan
public class Flotante : MonoBehaviour {

	public Vector3 giro = new Vector3(0f, 4f, 0f);
	public float amplitud = 1.5f;
	public float periodo = 14f;
	public float fase;

	Vector3 inicio;

	void Start() {
		inicio = transform.position;
	}

	void Update() {
		transform.Rotate(giro * Time.deltaTime, Space.Self);
		transform.position = inicio + Vector3.up * (Mathf.Sin((Time.time / periodo + fase) * Mathf.PI * 2f) * amplitud);
	}
}
