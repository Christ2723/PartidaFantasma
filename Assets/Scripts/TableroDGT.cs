using System.Collections;
using System.Collections.Generic;
using Ajedrez;
using UnityEngine;

//El tablero electrónico de madera (como los que se usan en los torneos de élite): sabe en todo momento
//qué pieza hay en cada una de sus 64 casillas y "transmite" cada jugada a la pantalla gigante.
//El origen local está en el centro de la superficie de juego; las columnas a..h van por +X y las filas 1..8 por +Z.
public class TableroDGT : MonoBehaviour {

	public static TableroDGT I;

	public float casilla = 0.055f;                  //55 mm por casilla, como en los tableros de torneo
	public PiezaFisica[] prefabs = new PiezaFisica[12];   //0..5 blancas (P C A T D R), 6..11 negras
	public Transform capturasBlancas, capturasNegras;     //donde se dejan las piezas comidas
	public GameObject prefabMarcador;
	public Transform luzSensor;                     //lucecita del tablero que parpadea al transmitir

	public Posicion pos = Posicion.Inicial();
	public readonly List<string> historial = new List<string>();   //SAN en inglés
	public Jugada? ultima;

	public bool Ocupado { get; private set; }

	public System.Func<PiezaFisica, bool> politicaTomar;
	public System.Func<Jugada, bool> politicaJugar;
	public event System.Action<Jugada, string, bool> AlJugar;      //jugada, SAN, ¿la hizo el usuario?
	public event System.Action AlColocar;
	public event System.Action<string> AlRechazar;
	public event System.Action<PiezaFisica> AlTomar;

	readonly PiezaFisica[] enCasilla = new PiezaFisica[64];
	readonly List<PiezaFisica> comidasBlancas = new List<PiezaFisica>();
	readonly List<PiezaFisica> comidasNegras = new List<PiezaFisica>();
	readonly List<GameObject> marcadores = new List<GameObject>();
	readonly HashSet<PiezaFisica> extras = new HashSet<PiezaFisica>();

	void Awake() {
		I = this;
		for (int i = 0; i < 28 && prefabMarcador != null; i++) {
			var m = Instantiate(prefabMarcador, transform);
			m.SetActive(false);
			marcadores.Add(m);
		}
		Colocar(Posicion.Inicial());
	}

	// ------------------------------------------------ Geometría

	public float Escala(float metros) => metros * transform.lossyScale.y;

	public Vector3 Centro(int c) => transform.TransformPoint(new Vector3((P.Columna(c) - 3.5f) * casilla, 0f, (P.Fila(c) - 3.5f) * casilla));

	public int CasillaEn(Vector3 mundo) {
		var l = transform.InverseTransformPoint(mundo);
		if (Mathf.Abs(l.y) > 0.2f) return -1;
		int col = Mathf.FloorToInt(l.x / casilla + 4f), fila = Mathf.FloorToInt(l.z / casilla + 4f);
		if (col < 0 || col > 7 || fila < 0 || fila > 7) return -1;
		return P.Casilla(col, fila);
	}

	//Casilla debajo de una pieza que la mano sostiene en el aire
	public int CasillaBajo(Vector3 mundo) {
		var l = transform.InverseTransformPoint(mundo);
		if (l.y > 0.3f || l.y < -0.05f) return -1;
		return CasillaEn(transform.TransformPoint(new Vector3(l.x, 0f, l.z)));
	}

	public bool PuntoEnPlano(Ray r, out Vector3 punto) {
		var plano = new Plane(transform.up, transform.position);
		if (plano.Raycast(r, out float d) && d < 30f) {
			punto = r.GetPoint(d);
			return true;
		}
		punto = Vector3.zero;
		return false;
	}

	public Quaternion Rotacion(PiezaFisica pz) => transform.rotation * Quaternion.Euler(0f, pz.Blanca ? 0f : 180f, 0f);

	public PiezaFisica PiezaEn(int c) => enCasilla[c];

	public IEnumerable<PiezaFisica> PiezasEnTablero() {
		foreach (var p in enCasilla) if (p != null) yield return p;
	}

	Vector3 Hueco(bool blanca, int indice) {
		var zona = blanca ? capturasBlancas : capturasNegras;
		int fila = indice / 8, k = indice % 8;
		return zona.TransformPoint(new Vector3(fila * 0.045f, 0f, k * 0.042f));
	}

	// ------------------------------------------------ Colocar posiciones

	PiezaFisica Crear(int pieza, Vector3 donde) {
		int idx = pieza > 0 ? pieza - 1 : 6 + (-pieza - 1);
		var pz = Instantiate(prefabs[idx], donde, Quaternion.identity, transform);
		pz.pieza = pieza;
		pz.transform.rotation = Rotacion(pz);
		pz.name = (pieza > 0 ? "Blanca_" : "Negra_") + P.NOMBRES[P.Tipo(pieza)];
		return pz;
	}

	//Coloca una posición al instante (sin animación)
	public void Colocar(Posicion p, List<string> nuevoHistorial = null) {
		foreach (var pz in GetComponentsInChildren<PiezaFisica>(true)) Destroy(pz.gameObject);
		System.Array.Clear(enCasilla, 0, 64);
		comidasBlancas.Clear();
		comidasNegras.Clear();
		extras.Clear();
		for (int i = 0; i < 64; i++) {
			if (p.c[i] == 0) continue;
			var pz = Crear(p.c[i], Centro(i));
			pz.casilla = i;
			enCasilla[i] = pz;
		}
		pos = p.Copia();
		historial.Clear();
		if (nuevoHistorial != null) historial.AddRange(nuevoHistorial);
		ultima = null;
		AlColocar?.Invoke();
	}

	//Lleva las piezas, volando, hasta otra posición (reutiliza las piezas que ya están en su sitio)
	public IEnumerator Sincronizar(Posicion destino, List<string> nuevoHistorial, float duracion = 0.9f) {
		Ocupado = true;
		var quedan = new List<PiezaFisica>();
		var nuevas = new PiezaFisica[64];
		for (int i = 0; i < 64; i++) {
			var pz = enCasilla[i];
			if (pz == null) continue;
			if (pz.pieza == destino.c[i]) nuevas[i] = pz;
			else quedan.Add(pz);
		}
		quedan.AddRange(comidasBlancas);
		quedan.AddRange(comidasNegras);
		comidasBlancas.Clear();
		comidasNegras.Clear();
		var vuelos = new List<Coroutine>();
		for (int i = 0; i < 64; i++) {
			if (destino.c[i] == 0 || nuevas[i] != null) continue;
			PiezaFisica elegida = null;
			float mejor = float.MaxValue;
			foreach (var q in quedan) {
				if (q.pieza != destino.c[i]) continue;
				float d = (q.transform.position - Centro(i)).sqrMagnitude;
				if (d < mejor) { mejor = d; elegida = q; }
			}
			if (elegida != null) quedan.Remove(elegida);
			else {
				elegida = Crear(destino.c[i], Centro(i) + transform.up * 0.4f);
				extras.Add(elegida);
			}
			nuevas[i] = elegida;
			elegida.casilla = i;
			vuelos.Add(StartCoroutine(Volar(elegida, Centro(i), duracion * Random.Range(0.8f, 1.1f), 0.12f, true)));
		}
		foreach (var q in quedan) {
			q.casilla = -1;
			if (extras.Contains(q)) {
				extras.Remove(q);
				StartCoroutine(Desvanecer(q));
				continue;
			}
			var lista = q.Blanca ? comidasBlancas : comidasNegras;
			lista.Add(q);
			vuelos.Add(StartCoroutine(Volar(q, Hueco(q.Blanca, lista.Count - 1), duracion, 0.1f, true)));
		}
		System.Array.Copy(nuevas, enCasilla, 64);
		pos = destino.Copia();
		historial.Clear();
		if (nuevoHistorial != null) historial.AddRange(nuevoHistorial);
		ultima = null;
		if (vuelos.Count > 0) Sonidos.Tocar(Sonidos.I != null ? Sonidos.I.fantasma : null, transform.position, 0.8f, 0.8f);
		foreach (var v in vuelos) yield return v;
		Ocupado = false;
		AlColocar?.Invoke();
	}

	// ------------------------------------------------ Animaciones

	static float Suave(float t) => t * t * (3f - 2f * t);

	public IEnumerator Volar(PiezaFisica pz, Vector3 destino, float duracion, float arco, bool brillo = false) {
		if (pz == null) yield break;
		pz.enAnimacion = true;
		Vector3 a = pz.transform.position;
		Quaternion ra = pz.transform.rotation, rb = Rotacion(pz);
		for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duracion)) {
			if (pz == null) yield break;
			float s = Suave(t);
			pz.transform.position = Vector3.Lerp(a, destino, s) + transform.up * (Mathf.Sin(s * Mathf.PI) * Escala(arco));
			pz.transform.rotation = Quaternion.Slerp(ra, rb, s);
			if (brillo) pz.BrilloFantasma = Mathf.Sin(s * Mathf.PI);
			yield return null;
		}
		if (pz == null) yield break;
		pz.transform.SetPositionAndRotation(destino, rb);
		if (brillo) pz.BrilloFantasma = 0f;
		pz.enAnimacion = false;
	}

	IEnumerator Desvanecer(PiezaFisica pz) {
		pz.enAnimacion = true;
		Vector3 e = pz.transform.localScale;
		for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f) {
			pz.transform.localScale = e * (1f - t);
			pz.transform.position += transform.up * Time.deltaTime * 0.1f;
			pz.BrilloFantasma = 1f;
			yield return null;
		}
		Destroy(pz.gameObject);
	}

	IEnumerator AlCementerio(PiezaFisica pz) {
		if (pz == null) yield break;
		pz.casilla = -1;
		var lista = pz.Blanca ? comidasBlancas : comidasNegras;
		lista.Add(pz);
		yield return Volar(pz, Hueco(pz.Blanca, lista.Count - 1), 0.7f, 0.15f, true);
	}

	IEnumerator Aparecer(PiezaFisica pz) {
		Vector3 e = pz.transform.localScale;
		for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f) {
			pz.transform.localScale = e * Suave(t);
			pz.BrilloFantasma = 1f - t;
			yield return null;
		}
		pz.transform.localScale = e;
		pz.BrilloFantasma = 0f;
	}

	// ------------------------------------------------ Aplicar jugadas (piezas secundarias)

	//Mueve la torre del enroque, quita el peón al paso y cambia el peón coronado
	IEnumerator Complementos(Jugada j) {
		bool b = j.pieza > 0;
		if (j.enroque) {
			int baseF = b ? 0 : 56;
			int de = P.Columna(j.hasta) == 6 ? baseF + 7 : baseF;
			int a = P.Columna(j.hasta) == 6 ? baseF + 5 : baseF + 3;
			var torre = enCasilla[de];
			enCasilla[de] = null;
			enCasilla[a] = torre;
			if (torre != null) {
				torre.casilla = a;
				yield return Volar(torre, Centro(a), 0.5f, 0.06f, true);
				Sonidos.Tocar(Sonidos.I.pieza, Centro(a), 0.8f, 1.1f);
			}
		}
		if (j.corona != 0) {
			var peon = enCasilla[j.hasta];
			int nueva = b ? j.corona : -j.corona;
			var lista = b ? comidasBlancas : comidasNegras;
			PiezaFisica reina = lista.Find(x => x.pieza == nueva);
			if (reina != null) {
				lista.Remove(reina);
				OrdenarCementerio(b);
			}
			if (peon != null) StartCoroutine(AlCementerio(peon));
			if (reina == null) {
				reina = Crear(nueva, Centro(j.hasta));
				extras.Add(reina);
				enCasilla[j.hasta] = reina;
				reina.casilla = j.hasta;
				yield return Aparecer(reina);
			} else {
				enCasilla[j.hasta] = reina;
				reina.casilla = j.hasta;
				yield return Volar(reina, Centro(j.hasta), 0.6f, 0.1f, true);
			}
			Sonidos.Tocar(Sonidos.I.pieza, Centro(j.hasta));
		}
	}

	void OrdenarCementerio(bool blancas) {
		var lista = blancas ? comidasBlancas : comidasNegras;
		for (int i = 0; i < lista.Count; i++) StartCoroutine(Volar(lista[i], Hueco(blancas, i), 0.3f, 0.02f));
	}

	int CasillaCapturada(Jugada j) => j.alPaso ? j.hasta + (j.pieza > 0 ? -8 : 8) : j.hasta;

	void Registrar(Jugada j, string san, bool usuario) {
		pos.Aplicar(j);
		historial.Add(san);
		ultima = j;
		if (luzSensor != null) StartCoroutine(Destello());
		AlJugar?.Invoke(j, san, usuario);
	}

	IEnumerator Destello() {
		var r = luzSensor.GetComponent<Renderer>();
		if (r == null) yield break;
		var bloque = new MaterialPropertyBlock();
		for (int i = 0; i < 3; i++) {
			bloque.SetColor("_EmissionColor", new Color(0.2f, 2.5f, 0.6f));
			r.SetPropertyBlock(bloque);
			yield return new WaitForSeconds(0.08f);
			bloque.SetColor("_EmissionColor", new Color(0.02f, 0.2f, 0.05f));
			r.SetPropertyBlock(bloque);
			yield return new WaitForSeconds(0.08f);
		}
	}

	// ------------------------------------------------ Jugada del fantasma (con su mano invisible)

	public IEnumerator JugadaFantasma(Jugada j, ManoFantasma mano, RelojDGT reloj) {
		Ocupado = true;
		string san = pos.San(j);
		var pz = enCasilla[j.desde];
		Vector3 arriba = transform.up;
		Vector3 a = Centro(j.desde), b = Centro(j.hasta);
		float h = pz.altura;
		Sonidos.Tocar(Sonidos.I.susurro, a, 0.5f, Random.Range(0.9f, 1.1f));
		yield return mano.Ir(a + arriba * (h + 0.11f), 0.55f);
		yield return mano.Ir(a + arriba * (h + 0.004f), 0.22f);
		mano.Cerrar(true);
		pz.enAnimacion = true;
		Sonidos.Tocar(Sonidos.I.tomar, a, 0.5f);
		//Levantar
		yield return Transportar(pz, mano, a + arriba * 0.045f, 0.22f, 0f);
		if (j.EsCaptura) {
			int cc = CasillaCapturada(j);
			var comida = enCasilla[cc];
			enCasilla[cc] = null;
			if (comida != null) {
				StartCoroutine(AlCementerio(comida));
				Sonidos.Tocar(Sonidos.I.fantasma, Centro(cc), 0.7f, 1.3f);
			}
		}
		yield return Transportar(pz, mano, b + arriba * 0.045f, Mathf.Lerp(0.45f, 0.8f, Vector3.Distance(a, b) / 0.4f), 0.03f);
		yield return Transportar(pz, mano, b, 0.2f, 0f);
		pz.transform.rotation = Rotacion(pz);
		pz.BrilloFantasma = 0f;
		pz.enAnimacion = false;
		mano.Cerrar(false);
		Sonidos.Tocar(j.EsCaptura ? Sonidos.I.captura : Sonidos.I.pieza, b, 1f, Random.Range(0.95f, 1.05f));
		enCasilla[j.desde] = null;
		enCasilla[j.hasta] = pz;
		pz.casilla = j.hasta;
		yield return Complementos(j);
		Registrar(j, san, false);
		yield return mano.Ir(b + arriba * (h + 0.1f), 0.2f);
		if (reloj != null) {
			bool blancas = j.pieza > 0;
			Vector3 boton = reloj.Boton(blancas);
			yield return mano.Ir(boton + arriba * 0.07f, 0.4f);
			yield return mano.Ir(boton + arriba * 0.004f, 0.1f);
			reloj.Pulsar(blancas);
			yield return mano.Ir(boton + arriba * 0.08f, 0.15f);
		}
		mano.Descansar();
		Ocupado = false;
	}

	IEnumerator Transportar(PiezaFisica pz, ManoFantasma mano, Vector3 destinoPieza, float duracion, float arco) {
		Vector3 a = pz.transform.position;
		Vector3 arriba = transform.up;
		for (float t = 0f; t < 1f; t += Time.deltaTime / duracion) {
			float s = Suave(t);
			Vector3 p = Vector3.Lerp(a, destinoPieza, s) + arriba * (Mathf.Sin(s * Mathf.PI) * arco);
			pz.transform.position = p;
			pz.BrilloFantasma = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f);
			mano.Colocar(p + arriba * pz.altura);
			yield return null;
		}
		pz.transform.position = destinoPieza;
		mano.Colocar(destinoPieza + arriba * pz.altura);
	}

	// ------------------------------------------------ Jugadas de la persona (ratón o manos VR)

	public bool Tomar(PiezaFisica pz, Apuntador quien) {
		if (Ocupado || pz == null || pz.casilla < 0 || pz.enAnimacion || pz.sostenida || quien.sostenida != null) return false;
		if (politicaTomar != null && !politicaTomar(pz)) return false;
		quien.sostenida = pz;
		pz.Tomada(true);
		Sonidos.Tocar(Sonidos.I.tomar, pz.transform.position, 0.6f);
		MostrarMarcadores(pz);
		AlTomar?.Invoke(pz);
		return true;
	}

	public void Soltar(PiezaFisica pz, int destino) {
		OcultarMarcadores();
		if (pz == null) return;
		pz.Tomada(false);
		int origen = pz.casilla;
		if (destino < 0 || destino == origen) {
			StartCoroutine(Volar(pz, Centro(origen), 0.25f, 0.02f));
			StartCoroutine(SonidoAlLlegar(pz, Sonidos.I.pieza, 0.25f, 0.5f));
			return;
		}
		Jugada elegida = default;
		bool hay = false;
		foreach (var j in pos.Legales()) {
			if (j.desde != origen || j.hasta != destino) continue;
			if (!hay || j.corona == P.DAMA) elegida = j;
			hay = true;
		}
		string motivo = null;
		if (!hay) motivo = (pz.Blanca != pos.blancasJuegan) ? "No es el turno de " + (pz.Blanca ? "las blancas" : "las negras") : "Jugada ilegal";
		else if (politicaJugar != null && !politicaJugar(elegida)) motivo = "Ahora no puedes mover esa pieza";
		if (motivo != null) {
			StartCoroutine(Volar(pz, Centro(origen), 0.35f, 0.05f));
			Sonidos.Tocar(Sonidos.I.ilegal, pz.transform.position, 0.8f);
			AlRechazar?.Invoke(motivo);
			return;
		}
		StartCoroutine(EjecutarUsuario(elegida, pz));
	}

	IEnumerator SonidoAlLlegar(PiezaFisica pz, AudioClip clip, float espera, float volumen) {
		yield return new WaitForSeconds(espera);
		if (pz != null) Sonidos.Tocar(clip, pz.transform.position, volumen);
	}

	IEnumerator EjecutarUsuario(Jugada j, PiezaFisica pz) {
		Ocupado = true;
		string san = pos.San(j);
		if (j.EsCaptura) {
			int cc = CasillaCapturada(j);
			var comida = enCasilla[cc];
			enCasilla[cc] = null;
			if (comida != null) StartCoroutine(AlCementerio(comida));
		}
		enCasilla[j.desde] = null;
		enCasilla[j.hasta] = pz;
		pz.casilla = j.hasta;
		yield return Volar(pz, Centro(j.hasta), 0.14f, 0f);
		Sonidos.Tocar(j.EsCaptura ? Sonidos.I.captura : Sonidos.I.pieza, Centro(j.hasta));
		yield return Complementos(j);
		Ocupado = false;
		Registrar(j, san, true);
	}

	// ------------------------------------------------ Marcadores de jugadas legales

	void MostrarMarcadores(PiezaFisica pz) {
		OcultarMarcadores();
		if (pz.Blanca != pos.blancasJuegan) return;
		int n = 0;
		var vistos = new HashSet<int>();
		foreach (var j in pos.LegalesDesde(pz.casilla)) {
			if (!vistos.Add(j.hasta) || n >= marcadores.Count) continue;
			var m = marcadores[n++];
			m.transform.position = Centro(j.hasta) + transform.up * Escala(0.0015f);
			m.transform.localScale = Vector3.one * (j.EsCaptura ? 1.5f : 1f);
			m.SetActive(true);
		}
	}

	void OcultarMarcadores() {
		foreach (var m in marcadores) m.SetActive(false);
	}

	public int MarcadoresVisibles() {
		int n = 0;
		foreach (var m in marcadores) if (m.activeSelf) n++;
		return n;
	}
}
