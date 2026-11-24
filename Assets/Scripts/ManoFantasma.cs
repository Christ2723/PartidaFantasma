using System.Collections;
using UnityEngine;

//La mano espectral del rival invisible: flota sobre la silla vacía, toma las piezas y aprieta el reloj.
//El origen del objeto es el punto de pinza (entre el pulgar y el índice).
public class ManoFantasma : MonoBehaviour {

	public Transform reposo;
	public Transform[] dedos;
	public float intensidadReposo = 0.35f;

	Renderer[] renderers;
	MaterialPropertyBlock bloque;
	static readonly int INTENSIDAD = Shader.PropertyToID("_Intensidad");
	float intensidad, intensidadObjetivo;
	Coroutine regreso;
	bool cerrada;
	Quaternion[] dedosAbiertos;

	void Awake() {
		renderers = GetComponentsInChildren<Renderer>();
		bloque = new MaterialPropertyBlock();
		dedosAbiertos = new Quaternion[dedos.Length];
		for (int i = 0; i < dedos.Length; i++) dedosAbiertos[i] = dedos[i].localRotation;
		if (reposo != null) transform.SetPositionAndRotation(reposo.position, reposo.rotation);
		intensidad = intensidadObjetivo = intensidadReposo;
	}

	void Update() {
		bool activa = regreso == null && Vector3.Distance(transform.position, reposo.position) > 0.05f;
		intensidadObjetivo = activa ? 1f : intensidadReposo;
		intensidad = Mathf.MoveTowards(intensidad, intensidadObjetivo, Time.deltaTime * 2f);
		float parpadeo = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 3f, transform.position.x * 10f);
		foreach (var r in renderers) {
			r.GetPropertyBlock(bloque);
			bloque.SetFloat(INTENSIDAD, intensidad * parpadeo);
			r.SetPropertyBlock(bloque);
		}
		for (int i = 0; i < dedos.Length; i++) {
			var objetivo = dedosAbiertos[i] * Quaternion.Euler(cerrada ? 28f : 0f, 0f, 0f);
			dedos[i].localRotation = Quaternion.Slerp(dedos[i].localRotation, objetivo, 1f - Mathf.Exp(-18f * Time.deltaTime));
		}
		//Flota suavemente mientras espera
		if (regreso == null && !activa && reposo != null) {
			transform.position = reposo.position + reposo.up * (Mathf.Sin(Time.time * 1.3f + reposo.position.z) * 0.012f);
		}
	}

	public void Colocar(Vector3 punto) {
		Detener();
		transform.position = punto;
		transform.rotation = Quaternion.Slerp(transform.rotation, reposo.rotation, 0.2f);
	}

	public IEnumerator Ir(Vector3 destino, float duracion) {
		Detener();
		Vector3 a = transform.position;
		for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duracion)) {
			float s = t * t * (3f - 2f * t);
			transform.position = Vector3.Lerp(a, destino, s);
			yield return null;
		}
		transform.position = destino;
	}

	public void Cerrar(bool si) => cerrada = si;

	//Vuelve sola a su sitio sobre la silla (sin bloquear al que la llamó)
	public void Descansar() {
		Detener();
		cerrada = false;
		regreso = StartCoroutine(Regresar());
	}

	IEnumerator Regresar() {
		Vector3 a = transform.position;
		for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f) {
			float s = t * t * (3f - 2f * t);
			transform.position = Vector3.Lerp(a, reposo.position, s);
			yield return null;
		}
		regreso = null;
	}

	void Detener() {
		if (regreso != null) {
			StopCoroutine(regreso);
			regreso = null;
		}
	}
}
