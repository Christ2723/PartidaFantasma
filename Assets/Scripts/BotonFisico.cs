using UnityEngine;

public enum AccionBoton { Fantasma, SiguientePartida, Duelo, Reiniciar, ModoPeon, Sentarse }

//Botón grande de la mesa del árbitro: se presiona con el ratón, con el láser VR o tocándolo con la mano
public class BotonFisico : MonoBehaviour, IInteractuable {

	public AccionBoton accion;
	public Transform tapa;
	public Renderer luz;
	public Color color = new Color(0.3f, 0.8f, 1f);

	Vector3 baseTapa;
	float hundido, espera;
	bool apuntado;
	MaterialPropertyBlock bloque;

	void Awake() {
		baseTapa = tapa.localPosition;
		bloque = new MaterialPropertyBlock();
	}

	public void AlApuntar(bool si) => apuntado = si;

	public void Presionar(Apuntador quien) {
		if (espera > 0f) return;
		espera = 0.45f;
		hundido = 1f;
		if (Sonidos.I != null) Sonidos.Tocar(Sonidos.I.boton, transform.position, 0.9f);
		foreach (var m in ManoVR.Todas) if (m.isActiveAndEnabled && quien == m.apuntador) m.Vibrar(0.4f, 0.05f);
		Ejecutar();
	}

	public void Ejecutar() {
		var d = Director.I;
		switch (accion) {
			case AccionBoton.Fantasma: d.AlternarFantasma(); break;
			case AccionBoton.SiguientePartida: d.SiguientePartida(); break;
			case AccionBoton.Duelo: d.IniciarDuelo(); break;
			case AccionBoton.Reiniciar: d.Reiniciar(); break;
			case AccionBoton.ModoPeon: d.AlternarPeon(); break;
			case AccionBoton.Sentarse:
				if (Jugador.I.Sentado) Jugador.I.Levantarse();
				else Jugador.I.Sentarse();
				break;
		}
	}

	void Update() {
		espera -= Time.deltaTime;
		//Tocar con el dedo en VR
		foreach (var m in ManoVR.Todas) {
			if (!m.isActiveAndEnabled || !m.Conectada || m.punta == null) continue;
			float escala = m.transform.lossyScale.x;
			if (Vector3.Distance(m.punta.position, tapa.position) < 0.045f * Mathf.Max(escala, 0.2f)) Presionar(m.apuntador);
		}
		hundido = Mathf.MoveTowards(hundido, 0f, Time.deltaTime * 3f);
		tapa.localPosition = baseTapa - Vector3.up * (hundido * 0.012f);
		if (luz != null) {
			float pulso = 0.55f + 0.45f * Mathf.Sin(Time.time * 2.2f + transform.position.x * 3f);
			float k = apuntado ? 2.2f : pulso;
			k += hundido * 2f;
			luz.GetPropertyBlock(bloque);
			bloque.SetColor("_EmissionColor", color * k);
			luz.SetPropertyBlock(bloque);
		}
	}
}
