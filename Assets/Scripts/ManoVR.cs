using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using XRDispositivo = UnityEngine.XR.InputDevice;
using XRUsos = UnityEngine.XR.CommonUsages;
using XRNodo = UnityEngine.XR.XRNode;
using XRDispositivos = UnityEngine.XR.InputDevices;
#if UNITY_WEBGL && !UNITY_EDITOR
using WebXR;
#endif

//Un control de realidad virtual (mano izquierda o derecha).
// - En el .exe (OpenXR) lee la pose y los botones con el Input System (y la API XR clásica como respaldo).
// - En el navegador (WebXR) los recibe del paquete WebXR Export.
//Gatillo = láser para tomar piezas y presionar botones. Agarre = tomar la pieza que está cerca de la mano.
public class ManoVR : MonoBehaviour {

	public bool izquierda;
	public Transform modelo;
	public LineRenderer laser;
	public Transform puntoLaser;
	public Transform punta;      //punta del dedo para tocar botones

	public Apuntador apuntador;
	public bool Conectada { get; private set; }
	public Vector2 Palanca { get; private set; }
	public bool BotonA { get; private set; }
	public bool BotonB { get; private set; }
	public bool BotonABajo { get; private set; }
	public bool BotonBBajo { get; private set; }
	public bool Gatillo { get; private set; }
	public bool Agarre { get; private set; }

	Vector3 posicion, rayoPos;
	Quaternion rotacion = Quaternion.identity, rayoRot = Quaternion.identity;
	bool gatilloAntes, agarreAntes, aAntes, bAntes;
	Vector3 desfaseAgarre;

	InputAction aPos, aRot, aRayoPos, aRayoRot, aGatillo, aAgarre, aPalanca, aBotonA, aBotonB;
	XRDispositivo dispositivo;

	public static readonly List<ManoVR> Todas = new List<ManoVR>();

	void Awake() {
		apuntador = new Apuntador(izquierda ? "ManoIzquierda" : "ManoDerecha", transform);
		string m = izquierda ? "{LeftHand}" : "{RightHand}";
		aPos = Accion("<XRController>" + m + "/devicePosition", "Vector3");
		aRot = Accion("<XRController>" + m + "/deviceRotation", "Quaternion");
		aRayoPos = Accion("<XRController>" + m + "/pointerPosition", "Vector3");
		aRayoRot = Accion("<XRController>" + m + "/pointerRotation", "Quaternion");
		aGatillo = Accion("<XRController>" + m + "/{TriggerButton}", null, InputActionType.Button);
		aAgarre = Accion("<XRController>" + m + "/{GripButton}", null, InputActionType.Button);
		aPalanca = Accion("<XRController>" + m + "/{Primary2DAxis}", "Vector2");
		aBotonA = Accion("<XRController>" + m + "/{PrimaryButton}", null, InputActionType.Button);
		aBotonB = Accion("<XRController>" + m + "/{SecondaryButton}", null, InputActionType.Button);
		Todas.Add(this);
	}

	void OnDestroy() {
		Todas.Remove(this);
		foreach (var a in new[] { aPos, aRot, aRayoPos, aRayoRot, aGatillo, aAgarre, aPalanca, aBotonA, aBotonB }) a?.Dispose();
	}

	static InputAction Accion(string ruta, string tipoControl, InputActionType tipo = InputActionType.Value) {
		var a = new InputAction(null, tipo, ruta, null, null, tipoControl);
		a.Enable();
		return a;
	}

	//Lee directamente el control enlazado (más fiable que el estado de la acción con poses XR)
	public static T Valor<T>(InputAction a) where T : struct {
		foreach (var c in a.controls) if (c is InputControl<T> ic) return ic.ReadValue();
		return default;
	}

	public static bool Presionado(InputAction a) {
		foreach (var c in a.controls) if (c is InputControl<float> f && f.ReadValue() > 0.5f) return true;
		return false;
	}

#if UNITY_WEBGL && !UNITY_EDITOR
	WebXRControllerData datosWeb;
	bool hayDatosWeb;

	void OnEnable() {
		WebXRManager.OnControllerUpdate += AlActualizarWeb;
	}

	void OnDisable() {
		WebXRManager.OnControllerUpdate -= AlActualizarWeb;
	}

	void AlActualizarWeb(WebXRControllerData d) {
		if ((WebXRControllerHand)d.hand != (izquierda ? WebXRControllerHand.LEFT : WebXRControllerHand.RIGHT)) return;
		datosWeb = d;
		hayDatosWeb = d.enabled;
	}
#endif

	//Lee la pose y los botones (llamado por el Jugador en cada cuadro de VR)
	public void Leer() {
		bool ok = false;
#if UNITY_WEBGL && !UNITY_EDITOR
		if (hayDatosWeb) {
			ok = true;
			posicion = datosWeb.gripPosition;
			rotacion = datosWeb.gripRotation;
			rayoPos = datosWeb.position;
			rayoRot = datosWeb.rotation;
			Gatillo = datosWeb.trigger > 0.6f;
			Agarre = datosWeb.squeeze > 0.6f;
			Palanca = new Vector2(datosWeb.thumbstickX, datosWeb.thumbstickY);
			if (Palanca.sqrMagnitude < 0.01f) Palanca = new Vector2(datosWeb.touchpadX, datosWeb.touchpadY);
			BotonA = datosWeb.buttonA > 0.5f;
			BotonB = datosWeb.buttonB > 0.5f;
		}
#else
		if (aRot.controls.Count > 0) {
			ok = true;
			posicion = Valor<Vector3>(aPos);
			rotacion = Valor<Quaternion>(aRot);
			bool hayRayo = aRayoRot.controls.Count > 0;
			rayoPos = hayRayo ? Valor<Vector3>(aRayoPos) : posicion;
			rayoRot = hayRayo ? Valor<Quaternion>(aRayoRot) : rotacion * Quaternion.Euler(35f, 0f, 0f);
			Gatillo = Presionado(aGatillo);
			Agarre = Presionado(aAgarre);
			Palanca = Valor<Vector2>(aPalanca);
			BotonA = Presionado(aBotonA);
			BotonB = Presionado(aBotonB);
		} else {
			//Respaldo: API XR clásica
			if (!dispositivo.isValid) dispositivo = XRDispositivos.GetDeviceAtXRNode(izquierda ? XRNodo.LeftHand : XRNodo.RightHand);
			if (dispositivo.isValid && dispositivo.TryGetFeatureValue(XRUsos.devicePosition, out posicion)) {
				ok = true;
				dispositivo.TryGetFeatureValue(XRUsos.deviceRotation, out rotacion);
				rayoPos = posicion;
				rayoRot = rotacion * Quaternion.Euler(35f, 0f, 0f);
				dispositivo.TryGetFeatureValue(XRUsos.triggerButton, out bool g);
				dispositivo.TryGetFeatureValue(XRUsos.gripButton, out bool ag);
				dispositivo.TryGetFeatureValue(XRUsos.primary2DAxis, out Vector2 pal);
				dispositivo.TryGetFeatureValue(XRUsos.primaryButton, out bool a);
				dispositivo.TryGetFeatureValue(XRUsos.secondaryButton, out bool b);
				Gatillo = g; Agarre = ag; Palanca = pal; BotonA = a; BotonB = b;
			}
		}
#endif
		Conectada = ok;
		BotonABajo = BotonA && !aAntes;
		BotonBBajo = BotonB && !bAntes;
		aAntes = BotonA;
		bAntes = BotonB;
	}

	//Aplica la pose, el láser y las interacciones
	public void Procesar() {
		modelo.gameObject.SetActive(Conectada);
		apuntador.activo = Conectada;
		if (!Conectada) {
			laser.enabled = false;
			puntoLaser.gameObject.SetActive(false);
			if (apuntador.sostenida != null) Interaccion.Soltar(apuntador);
			return;
		}
		transform.localPosition = posicion;
		transform.localRotation = rotacion;
		var rig = transform.parent;
		Vector3 origen = rig.TransformPoint(rayoPos);
		Vector3 dir = rig.rotation * (rayoRot * Vector3.forward);
		float escala = rig.lossyScale.x;
		apuntador.rayo = new Ray(origen, dir);
		apuntador.distanciaMaxima = 12f * escala;
		Interaccion.Actualizar(apuntador);

		//Gatillo: tomar con el láser (se suelta al soltar el gatillo) o presionar botones
		if (Gatillo && !gatilloAntes && apuntador.sostenida == null) Interaccion.Presionar(apuntador);
		if (!Gatillo && gatilloAntes && apuntador.sostenida != null && !apuntador.agarreDirecto) Interaccion.Soltar(apuntador);
		gatilloAntes = Gatillo;

		//Agarre: tomar la pieza más cercana a la mano
		if (Agarre && !agarreAntes && apuntador.sostenida == null) AgarrarCercana(escala);
		if (apuntador.sostenida != null && apuntador.agarreDirecto) {
			apuntador.sostenida.transform.position = punta.position + punta.rotation * desfaseAgarre;
			if (!Agarre) Interaccion.Soltar(apuntador);
		}
		agarreAntes = Agarre;

		//Dibuja el láser
		bool mostrar = apuntador.sostenida == null || !apuntador.agarreDirecto;
		laser.enabled = mostrar;
		float largo = apuntador.hayImpacto ? apuntador.impacto.distance : 2.5f * escala;
		laser.startWidth = 0.004f * escala;
		laser.endWidth = 0.001f * escala;
		laser.SetPosition(0, origen);
		laser.SetPosition(1, origen + dir * largo);
		puntoLaser.gameObject.SetActive(mostrar && apuntador.hayImpacto);
		if (apuntador.hayImpacto) {
			puntoLaser.position = apuntador.impacto.point;
			puntoLaser.localScale = Vector3.one * 0.012f;
		}
		var color = apuntador.apuntado != null || apuntador.sostenida != null ? new Color(1f, 0.85f, 0.3f) : new Color(0.4f, 0.9f, 1f);
		laser.startColor = color;
		laser.endColor = new Color(color.r, color.g, color.b, 0.1f);
	}

	void AgarrarCercana(float escala) {
		PiezaFisica mejor = null;
		float mejorD = 0.07f * escala;
		foreach (var col in Physics.OverlapSphere(punta.position, 0.07f * escala, 1 << Capas.INTERACTUABLE, QueryTriggerInteraction.Collide)) {
			var pz = col.GetComponentInParent<PiezaFisica>();
			if (pz == null || pz.casilla < 0) continue;
			float d = Vector3.Distance(col.ClosestPoint(punta.position), punta.position);
			if (d < mejorD) { mejorD = d; mejor = pz; }
		}
		if (mejor == null) return;
		apuntador.agarreDirecto = true;
		if (!TableroDGT.I.Tomar(mejor, apuntador)) {
			apuntador.agarreDirecto = false;
			return;
		}
		desfaseAgarre = Quaternion.Inverse(punta.rotation) * (mejor.transform.position - punta.position);
	}

	public void Vibrar(float fuerza, float segundos) {
#if !(UNITY_WEBGL && !UNITY_EDITOR)
		if (dispositivo.isValid) dispositivo.SendHapticImpulse(0, fuerza, segundos);
#endif
	}
}
