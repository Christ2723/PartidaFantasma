using UnityEngine;

public static class Capas {
	public const int INTERACTUABLE = 8;
	public const int TABLERO = 9;
	public const int MASCARA = (1 << INTERACTUABLE) | (1 << TABLERO);
}

//Todo lo que se puede señalar y presionar con el ratón, con el láser del control VR o tocar con la mano
public interface IInteractuable {
	void AlApuntar(bool apuntado);
	void Presionar(Apuntador quien);
}

//Quien interactúa: el ratón (escritorio) o una de las dos manos de realidad virtual
public class Apuntador {
	public string nombre;
	public Ray rayo;
	public bool activo;
	public Transform mano;                //null = ratón
	public PiezaFisica sostenida;
	public bool agarreDirecto;            //la pieza sigue a la mano (botón de agarre) en vez del láser
	public IInteractuable apuntado;
	public RaycastHit impacto;
	public bool hayImpacto;
	public float distanciaMaxima = 12f;

	public Apuntador(string nombre, Transform mano = null) {
		this.nombre = nombre;
		this.mano = mano;
	}
}

//Lógica común de interacción por rayo (ratón o láser)
public static class Interaccion {

	//Actualiza qué se está señalando y mueve la pieza sostenida con el rayo
	public static void Actualizar(Apuntador a) {
		a.hayImpacto = false;
		IInteractuable nuevo = null;
		if (a.activo && Physics.Raycast(a.rayo, out a.impacto, a.distanciaMaxima, Capas.MASCARA, QueryTriggerInteraction.Collide)) {
			a.hayImpacto = true;
			nuevo = a.impacto.collider.GetComponentInParent<IInteractuable>();
			if (a.sostenida != null && nuevo is PiezaFisica pz && pz == a.sostenida) nuevo = null;
		}
		if (a.sostenida != null) nuevo = null;
		if (!ReferenceEquals(nuevo, a.apuntado)) {
			if (a.apuntado != null && (a.apuntado as Object) != null) a.apuntado.AlApuntar(false);
			a.apuntado = nuevo;
			if (nuevo != null) nuevo.AlApuntar(true);
		}
		if (a.sostenida != null && !a.agarreDirecto) {
			var tablero = TableroDGT.I;
			Vector3 punto;
			if (tablero.PuntoEnPlano(a.rayo, out punto)) {
				a.sostenida.Llevar(punto + tablero.transform.up * tablero.Escala(0.035f));
			}
		}
	}

	//Clic / gatillo
	public static void Presionar(Apuntador a) {
		if (a.sostenida != null) {
			Soltar(a);
			return;
		}
		if (a.apuntado != null) a.apuntado.Presionar(a);
	}

	//Suelta la pieza sostenida en la casilla señalada
	public static void Soltar(Apuntador a) {
		var pz = a.sostenida;
		if (pz == null) return;
		var tablero = TableroDGT.I;
		int destino = -1;
		if (a.agarreDirecto) {
			destino = tablero.CasillaBajo(pz.transform.position);
		} else {
			if (a.hayImpacto) {
				var otra = a.impacto.collider.GetComponentInParent<PiezaFisica>();
				if (otra != null && otra != pz && otra.casilla >= 0) destino = otra.casilla;
				else destino = tablero.CasillaEn(a.impacto.point);
			}
			if (destino < 0 && tablero.PuntoEnPlano(a.rayo, out Vector3 punto)) destino = tablero.CasillaEn(punto);
		}
		a.sostenida = null;
		a.agarreDirecto = false;
		tablero.Soltar(pz, destino);
	}
}
