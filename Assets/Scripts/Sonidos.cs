using UnityEngine;

//Efectos de sonido 3D (madera, reloj, fantasma...) con un grupo de fuentes reutilizables
public class Sonidos : MonoBehaviour {

	public static Sonidos I;

	public AudioClip pieza, captura, tomar, reloj, ilegal, fantasma, boton, encoger, crecer, jaque, mate, susurro;
	public AudioSource musica;

	AudioSource[] fuentes;
	int siguiente;

	void Awake() {
		I = this;
		fuentes = new AudioSource[14];
		for (int i = 0; i < fuentes.Length; i++) {
			var go = new GameObject("Fuente" + i);
			go.transform.SetParent(transform, false);
			var f = go.AddComponent<AudioSource>();
			f.playOnAwake = false;
			f.spatialBlend = 0.85f;
			f.rolloffMode = AudioRolloffMode.Linear;
			f.minDistance = 1.5f;
			f.maxDistance = 40f;
			f.dopplerLevel = 0f;
			fuentes[i] = f;
		}
	}

	public static void Tocar(AudioClip clip, Vector3 donde, float volumen = 1f, float tono = 1f) {
		if (I == null || clip == null) return;
		var f = I.fuentes[I.siguiente];
		I.siguiente = (I.siguiente + 1) % I.fuentes.Length;
		f.transform.position = donde;
		f.pitch = tono;
		f.volume = volumen;
		f.clip = clip;
		f.Play();
	}

	//Sonido sin posición (interfaz)
	public static void Plano(AudioClip clip, float volumen = 1f, float tono = 1f) {
		if (I == null || clip == null) return;
		var f = I.fuentes[I.siguiente];
		I.siguiente = (I.siguiente + 1) % I.fuentes.Length;
		f.spatialBlend = 0f;
		f.pitch = tono;
		f.volume = volumen;
		f.PlayOneShot(clip);
		I.StartCoroutine(Restaurar(f, clip.length / Mathf.Max(0.1f, tono) + 0.05f));
	}

	static System.Collections.IEnumerator Restaurar(AudioSource f, float t) {
		yield return new WaitForSeconds(t);
		f.spatialBlend = 0.85f;
	}
}
