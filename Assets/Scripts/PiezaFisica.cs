using Ajedrez;
using UnityEngine;

//Una pieza de madera del tablero electrónico. Se puede tomar con el ratón, el láser o la mano VR.
public class PiezaFisica : MonoBehaviour, IInteractuable {

	public int pieza;               //código con signo (+ blancas, - negras)
	public float altura = 0.05f;    //alto de la pieza en metros (para la mano fantasma)
	public float radio = 0.016f;    //radio de la base
	[HideInInspector] public int casilla = -1;

	public bool Blanca => pieza > 0;
	public int Tipo => P.Tipo(pieza);

	Renderer[] renderers;
	MaterialPropertyBlock bloque;
	static readonly int EMISION = Shader.PropertyToID("_EmissionColor");
	bool apuntada;
	float brilloFantasma;
	Color colorFantasma = new Color(0.35f, 0.85f, 1f);

	[HideInInspector] public bool sostenida;
	[HideInInspector] public bool enAnimacion;
	Vector3 objetivoLlevar;
	bool llevando;

	void Awake() {
		renderers = GetComponentsInChildren<Renderer>();
		bloque = new MaterialPropertyBlock();
	}

	public void AlApuntar(bool si) {
		apuntada = si;
		ActualizarBrillo();
	}

	public void Presionar(Apuntador quien) {
		TableroDGT.I.Tomar(this, quien);
	}

	//Brillo azul cuando la mueve el fantasma (0..1)
	public float BrilloFantasma {
		get => brilloFantasma;
		set {
			brilloFantasma = value;
			ActualizarBrillo();
		}
	}

	public void ActualizarBrillo() {
		if (renderers == null) Awake();
		Color c = Color.black;
		if (brilloFantasma > 0f) c += colorFantasma * (brilloFantasma * 1.6f);
		if (apuntada && !sostenida) c += new Color(0.35f, 0.28f, 0.08f);
		if (sostenida) c += new Color(0.25f, 0.45f, 0.2f);
		foreach (var r in renderers) {
			r.GetPropertyBlock(bloque);
			bloque.SetColor(EMISION, c);
			r.SetPropertyBlock(bloque);
		}
	}

	//La pieza sigue un punto (cuando la llevan el ratón o el láser)
	public void Llevar(Vector3 punto) {
		objetivoLlevar = punto;
		if (!llevando) transform.position = Vector3.Lerp(transform.position, punto, 0.5f);
		llevando = true;
	}

	void LateUpdate() {
		if (sostenida && llevando) {
			transform.position = Vector3.Lerp(transform.position, objetivoLlevar, 1f - Mathf.Exp(-25f * Time.deltaTime));
		}
		if (!sostenida) llevando = false;
	}

	public void Tomada(bool si) {
		sostenida = si;
		llevando = false;
		ActualizarBrillo();
	}
}
