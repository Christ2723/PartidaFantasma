using UnityEngine;

//Luz roja de cámara / "EN VIVO" que parpadea
public class Parpadeo : MonoBehaviour {

	public float periodo = 1.2f;
	public float encendido = 0.7f;
	Renderer r;
	Light luz;

	void Awake() {
		r = GetComponent<Renderer>();
		luz = GetComponent<Light>();
	}

	void Update() {
		bool on = Mathf.Repeat(Time.time + transform.position.x, periodo) < periodo * encendido;
		if (r != null) r.enabled = on;
		if (luz != null) luz.enabled = on;
	}
}
