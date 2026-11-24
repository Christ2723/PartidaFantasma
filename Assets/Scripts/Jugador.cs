using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using XRUsos = UnityEngine.XR.CommonUsages;
using XRNodo = UnityEngine.XR.XRNode;
using XRDispositivos = UnityEngine.XR.InputDevices;
#if UNITY_WEBGL && !UNITY_EDITOR
using WebXR;
#endif

//El visitante de la sala. Funciona de dos formas:
// - Realidad virtual: la cabeza y las manos siguen al visor (OpenXR en el .exe, WebXR en el navegador).
// - Escritorio (sin visor): WASD para caminar, clic derecho para mirar y clic izquierdo para tomar piezas.
//Además tiene el "Modo Peón": el visitante se encoge al tamaño de una pieza y camina sobre el tablero.
public class Jugador : MonoBehaviour {

	public static Jugador I;

	public Transform cabeza;
	public Camera camara;
	public ManoVR manoIzq, manoDer;
	public CharacterController cc;
	public Behaviour camaraWebXR;           //componente WebXRCamera (solo se activa en el navegador)
	public Transform asiento;               //silla de las blancas (punto en el suelo, mirando al tablero)
	public float alturaOjos = 1.62f, alturaSentado = 1.2f;
	public float radioPlataforma = 15f;
	public float velocidad = 1.5f, sensibilidad = 0.14f;

	public bool EnVR { get; private set; }
	public bool EnPeon { get; private set; }
	public bool Sentado { get; private set; }
	public float Escala => transform.localScale.x;
	public Apuntador Raton => raton;

	public const float ESCALA_PEON = 1f / 28f;
	const int CAPA_SUELO = 10;

	readonly Apuntador raton = new Apuntador("Raton");
	float yaw, pitch, velY;
	Vector3 posAntesPeon;
	float yawAntesPeon;
	bool giroListo = true, cambiandoEscala;
	float siguienteRevision;
	InputAction cabezaPos, cabezaRot;
	static readonly List<UnityEngine.XR.XRDisplaySubsystem> pantallasXR = new List<UnityEngine.XR.XRDisplaySubsystem>();

	void Awake() {
		I = this;
		cabezaPos = new InputAction(null, InputActionType.Value, "<XRHMD>/centerEyePosition", null, null, "Vector3");
		cabezaRot = new InputAction(null, InputActionType.Value, "<XRHMD>/centerEyeRotation", null, null, "Quaternion");
		cabezaPos.Enable();
		cabezaRot.Enable();
	}

	void OnDestroy() {
		cabezaPos?.Dispose();
		cabezaRot?.Dispose();
	}

	void OnEnable() => Application.onBeforeRender += PoseCabeza;
	void OnDisable() => Application.onBeforeRender -= PoseCabeza;

	void Start() {
		yaw = transform.eulerAngles.y;
		pitch = 12f;
		AplicarEscritorio();
		ModoVR(false, true);
	}

	// ------------------------------------------------ Detección de VR

	static bool PantallaXRActiva() {
		SubsystemManager.GetSubsystems(pantallasXR);
		foreach (var d in pantallasXR) if (d.running) return true;
		return false;
	}

	void ModoVR(bool vr, bool forzar = false) {
		if (vr == EnVR && !forzar) return;
		EnVR = vr;
		if (HUD.I != null) HUD.I.ModoVR(vr);
		manoIzq.gameObject.SetActive(vr);
		manoDer.gameObject.SetActive(vr);
		if (raton.sostenida != null) Interaccion.Soltar(raton);
		if (raton.apuntado != null) { raton.apuntado.AlApuntar(false); raton.apuntado = null; }
		cc.enabled = !vr && !EnPeon && !Sentado;
		if (vr) {
			pitch = 0f;
			cabeza.localPosition = Vector3.zero;
			cabeza.localRotation = Quaternion.identity;
			if (Sentado) Levantarse();
			if (HUD.I != null) HUD.I.Mensaje("Realidad virtual activada. Gatillo = láser · Agarre = tomar pieza · A = Modo Peón");
		} else {
			AplicarEscritorio();
		}
	}

#if UNITY_WEBGL && !UNITY_EDITOR
	bool webxrListo;

	void RevisarWebXR() {
		if (webxrListo || WebXRManager.Instance == null) return;
		webxrListo = true;
		if (camaraWebXR != null) camaraWebXR.enabled = true;
		WebXRManager.OnXRChange += (estado, vistas, izq, der) => ModoVR(estado == WebXRState.VR);
	}
#endif

	public void EntrarVR() {
#if UNITY_WEBGL && !UNITY_EDITOR
		if (WebXRManager.Instance != null && WebXRManager.Instance.isSupportedVR) WebXRManager.Instance.ToggleVR();
		else if (HUD.I != null) HUD.I.Mensaje("Este navegador no tiene un visor de realidad virtual conectado");
#else
		if (HUD.I != null) HUD.I.Mensaje(EnVR ? "Ya estás en realidad virtual" : "Conecta un visor compatible con OpenXR (Quest Link, SteamVR...) y abre el programa otra vez");
#endif
	}

	//Pose de la cabeza leída justo antes de dibujar (menos retraso en el visor)
	void PoseCabeza() {
		if (!EnVR) return;
		if (cabezaRot.controls.Count > 0) {
			cabeza.SetLocalPositionAndRotation(ManoVR.Valor<Vector3>(cabezaPos), ManoVR.Valor<Quaternion>(cabezaRot));
			return;
		}
		var d = XRDispositivos.GetDeviceAtXRNode(XRNodo.CenterEye);
		if (!d.isValid) d = XRDispositivos.GetDeviceAtXRNode(XRNodo.Head);
		if (d.isValid && d.TryGetFeatureValue(XRUsos.devicePosition, out Vector3 p) && d.TryGetFeatureValue(XRUsos.deviceRotation, out Quaternion r)) {
			cabeza.SetLocalPositionAndRotation(p, r);
		}
	}

	// ------------------------------------------------ Bucle

	void Update() {
#if UNITY_WEBGL && !UNITY_EDITOR
		RevisarWebXR();
#else
		if (Time.unscaledTime > siguienteRevision) {
			siguienteRevision = Time.unscaledTime + 1f;
			ModoVR(PantallaXRActiva());
		}
#endif
		if (EnVR) ActualizarVR();
		else ActualizarEscritorio();
		Teclas();
	}

	void Teclas() {
		var kb = Keyboard.current;
		if (kb == null) return;
		var d = Director.I;
		if (kb.pKey.wasPressedThisFrame) AlternarPeon();
		if (kb.hKey.wasPressedThisFrame && HUD.I != null) HUD.I.AlternarAyuda();
		if (kb.vKey.wasPressedThisFrame) EntrarVR();
		if (d == null) return;
		if (kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) d.AlternarFantasma();
		if (kb.nKey.wasPressedThisFrame) d.SiguientePartida();
		if (kb.jKey.wasPressedThisFrame) d.IniciarDuelo();
		if (kb.rKey.wasPressedThisFrame) d.Reiniciar();
		if (kb.cKey.wasPressedThisFrame && !EnVR && !EnPeon) {
			if (Sentado) Levantarse();
			else Sentarse();
		}
	}

	//Coloca al visitante en un punto mirando hacia una dirección (capturas y pruebas)
	public void Ubicar(Vector3 pos, float nuevoYaw, float nuevoPitch) {
		bool c = cc.enabled;
		cc.enabled = false;
		transform.position = pos;
		yaw = nuevoYaw;
		pitch = nuevoPitch;
		AplicarEscritorio();
		cc.enabled = c;
	}

	// ------------------------------------------------ Escritorio

	void AplicarEscritorio() {
		transform.rotation = Quaternion.Euler(0f, yaw, 0f);
		cabeza.localPosition = new Vector3(0f, Sentado ? alturaSentado : alturaOjos, 0f);
		cabeza.localRotation = Quaternion.Euler(pitch, 0f, 0f);
	}

	void ActualizarEscritorio() {
		var kb = Keyboard.current;
		var mouse = Mouse.current;
		if (kb == null || mouse == null) return;
		float dt = Time.deltaTime;

		//Mirar: clic derecho + arrastrar, o flechas izquierda/derecha
		if (mouse.rightButton.isPressed) {
			var delta = mouse.delta.ReadValue();
			yaw += delta.x * sensibilidad;
			pitch -= delta.y * sensibilidad;
		}
		if (kb.leftArrowKey.isPressed || kb.qKey.isPressed) yaw -= 85f * dt;
		if (kb.rightArrowKey.isPressed || kb.eKey.isPressed) yaw += 85f * dt;
		if (kb.pageUpKey.isPressed) pitch -= 60f * dt;
		if (kb.pageDownKey.isPressed) pitch += 60f * dt;
		pitch = Mathf.Clamp(pitch, -80f, 85f);
		float scroll = mouse.scroll.ReadValue().y;
		if (Mathf.Abs(scroll) > 0.01f) camara.fieldOfView = Mathf.Clamp(camara.fieldOfView - Mathf.Sign(scroll) * 4f, 25f, 75f);

		//Caminar
		Vector2 m = Vector2.zero;
		if (kb.wKey.isPressed || kb.upArrowKey.isPressed) m.y += 1f;
		if (kb.sKey.isPressed || kb.downArrowKey.isPressed) m.y -= 1f;
		if (kb.dKey.isPressed) m.x += 1f;
		if (kb.aKey.isPressed) m.x -= 1f;
		if (m.sqrMagnitude > 0f && Sentado) Levantarse();
		float vel = velocidad * (kb.leftShiftKey.isPressed ? 2f : 1f);
		AplicarEscritorio();
		Vector3 dir = transform.rotation * new Vector3(m.x, 0f, m.y);
		if (dir.sqrMagnitude > 1f) dir.Normalize();
		if (EnPeon) {
			MoverPeon(dir * vel * Escala * dt);
		} else if (cc.enabled) {
			velY = cc.isGrounded ? -1f : velY - 9.8f * dt;
			cc.Move(dir * vel * dt + Vector3.up * velY * dt);
			LimitarPlataforma();
		}

		//Señalar y tomar con el ratón
		raton.activo = !cambiandoEscala;
		raton.rayo = camara.ScreenPointToRay(mouse.position.ReadValue());
		raton.distanciaMaxima = 12f * Mathf.Max(Escala, 0.08f);
		Interaccion.Actualizar(raton);
		if (mouse.leftButton.wasPressedThisFrame) Interaccion.Presionar(raton);
		if (mouse.rightButton.wasPressedThisFrame && raton.sostenida != null) {
			var pz = raton.sostenida;
			raton.sostenida = null;
			TableroDGT.I.Soltar(pz, -1);
		}
	}

	void LimitarPlataforma() {
		var p = transform.position;
		var h = new Vector2(p.x, p.z);
		if (h.magnitude > radioPlataforma) {
			h = h.normalized * radioPlataforma;
			transform.position = new Vector3(h.x, p.y, h.y);
		}
	}

	public void Sentarse() {
		if (asiento == null) return;
		Sentado = true;
		cc.enabled = false;
		transform.position = asiento.position;
		yaw = asiento.eulerAngles.y;
		pitch = 38f;
		AplicarEscritorio();
		if (HUD.I != null) HUD.I.Mensaje("Te sientas frente al tablero (C o caminar para levantarte)");
	}

	public void Levantarse() {
		if (!Sentado) return;
		Sentado = false;
		transform.position = asiento.position - asiento.forward * 0.55f + Vector3.up * 0.05f;
		pitch = 20f;
		AplicarEscritorio();
		cc.enabled = !EnVR && !EnPeon;
	}

	// ------------------------------------------------ Realidad virtual

	void ActualizarVR() {
		manoIzq.Leer();
		manoDer.Leer();
		float dt = Time.deltaTime;

		//Caminar con la palanca izquierda (hacia donde mira la cabeza)
		Vector2 m = manoIzq.Palanca;
		if (m.magnitude > 0.15f) {
			Vector3 adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up).normalized;
			Vector3 derecha = Vector3.ProjectOnPlane(cabeza.right, Vector3.up).normalized;
			Vector3 mov = (adelante * m.y + derecha * m.x) * (1.4f * Escala * dt);
			if (EnPeon) MoverPeon(mov);
			else {
				transform.position += mov;
				SeguirSuelo();
				LimitarPlataforma();
			}
		}

		//Giro por pasos con la palanca derecha
		float g = manoDer.Palanca.x;
		if (Mathf.Abs(g) > 0.7f && giroListo) {
			transform.RotateAround(cabeza.position, Vector3.up, 30f * Mathf.Sign(g));
			giroListo = false;
		}
		if (Mathf.Abs(g) < 0.3f) giroListo = true;

		//Botones: A = Modo Peón, B = pausar/seguir al fantasma, X = siguiente partida, Y = ayuda
		if (manoDer.BotonABajo) AlternarPeon();
		if (manoDer.BotonBBajo && Director.I != null) Director.I.AlternarFantasma();
		if (manoIzq.BotonABajo && Director.I != null) Director.I.SiguientePartida();
		if (manoIzq.BotonBBajo && HUD.I != null) HUD.I.AlternarAyuda();

		manoIzq.Procesar();
		manoDer.Procesar();
	}

	void SeguirSuelo() {
		var origen = new Vector3(cabeza.position.x, transform.position.y + 1.2f, cabeza.position.z);
		if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 3f, 1 << CAPA_SUELO, QueryTriggerInteraction.Ignore)) {
			var p = transform.position;
			p.y = Mathf.Lerp(p.y, hit.point.y, 0.3f);
			transform.position = p;
		}
	}

	// ------------------------------------------------ Modo Peón

	public void AlternarPeon() {
		if (!cambiandoEscala) StartCoroutine(CambiarPeon(!EnPeon));
	}

	IEnumerator CambiarPeon(bool entrar) {
		cambiandoEscala = true;
		foreach (var a in new[] { raton, manoIzq.apuntador, manoDer.apuntador }) {
			if (a != null && a.sostenida != null) Interaccion.Soltar(a);
		}
		if (Sonidos.I != null) Sonidos.Plano(entrar ? Sonidos.I.encoger : Sonidos.I.crecer, 0.8f);
		if (!EnVR && HUD.I != null) yield return HUD.I.Fundido(1f, 0.3f);
		var tablero = TableroDGT.I;
		if (entrar) {
			if (Sentado) Levantarse();
			posAntesPeon = transform.position;
			yawAntesPeon = yaw;
			cc.enabled = false;
			transform.localScale = Vector3.one * ESCALA_PEON;
			transform.position = PuntoDeLlegada(tablero);
			yaw = tablero.transform.eulerAngles.y;
			pitch = -8f;
			if (EnVR) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
			camara.nearClipPlane = 0.0006f;
			camara.farClipPlane = 900f;
		} else {
			transform.localScale = Vector3.one;
			transform.position = posAntesPeon;
			yaw = yawAntesPeon;
			pitch = 15f;
			if (EnVR) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
			camara.nearClipPlane = 0.02f;
			camara.farClipPlane = 900f;
			cc.enabled = !EnVR;
		}
		foreach (var c in cabeza.GetComponentsInChildren<Camera>(true)) {
			c.nearClipPlane = camara.nearClipPlane;
			c.farClipPlane = camara.farClipPlane;
		}
		EnPeon = entrar;
		if (!EnVR) AplicarEscritorio();
		if (HUD.I != null) {
			HUD.I.Mensaje(entrar ? "MODO PEÓN: ahora mides lo mismo que una pieza. Camina entre ellas (P para crecer)"
				: "Vuelves a tu tamaño normal");
			if (!EnVR) yield return HUD.I.Fundido(0f, 0.45f);
		}
		cambiandoEscala = false;
	}

	//Casilla vacía del centro del tablero más cercana a las blancas
	static Vector3 PuntoDeLlegada(TableroDGT t) {
		int[] preferidas = { 27, 28, 35, 36, 26, 29, 34, 37, 19, 20, 43, 44, 18, 21, 42, 45 };
		foreach (int c in preferidas) {
			if (t.PiezaEn(c) == null) return t.Centro(c) - t.transform.forward * (t.casilla * 0.3f);
		}
		return t.transform.TransformPoint(new Vector3(0f, 0f, -4.3f * t.casilla));
	}

	//Movimiento sobre el tablero sin atravesar las piezas
	void MoverPeon(Vector3 mov) {
		var t = TableroDGT.I;
		Vector3 cabezaH = new Vector3(cabeza.position.x, 0f, cabeza.position.z);
		Vector3 rig = transform.position;
		Vector3 desfase = new Vector3(cabeza.position.x - rig.x, 0f, cabeza.position.z - rig.z);
		Vector3 nueva = cabezaH + mov;
		float radioYo = 0.006f;
		foreach (var pz in t.PiezasEnTablero()) {
			var c = pz.transform.position;
			var d = new Vector3(nueva.x - c.x, 0f, nueva.z - c.z);
			float min = pz.radio + radioYo;
			if (d.sqrMagnitude < min * min) nueva = new Vector3(c.x, 0f, c.z) + (d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward) * min;
		}
		var local = t.transform.InverseTransformPoint(new Vector3(nueva.x, t.transform.position.y, nueva.z));
		float lim = 4f * t.casilla + 0.028f;
		local.x = Mathf.Clamp(local.x, -lim, lim);
		local.z = Mathf.Clamp(local.z, -lim, lim);
		local.y = 0f;
		var mundo = t.transform.TransformPoint(local);
		transform.position = new Vector3(mundo.x - desfase.x, t.transform.position.y, mundo.z - desfase.z);
	}
}
