using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

//Construye automáticamente toda la escena de realidad virtual "La Partida Fantasma":
//texturas de madera, piezas Staunton torneadas, tablero electrónico, reloj, sala de torneo flotando en el vacío,
//sonidos, interfaz, configuración de VR (OpenXR para Windows y WebXR para el navegador) y compilaciones.
//Se ejecuta desde el menú "Fantasma" de Unity o por línea de comandos (-executeMethod).
public static partial class ConstructorProyecto {

	public const string NOMBRE = "Christopher Payero Estrella";
	public const string MATRICULA = "1-21-0909";
	public const string ESCENA = "01 Sala de Torneo";
	public const string RUTA_ESCENA = "Assets/Scenes/" + ESCENA + ".unity";

	const int CAPA_INTERACTUABLE = 8, CAPA_TABLERO = 9, CAPA_SUELO = 10;

	static Font fuente;

	[MenuItem("Fantasma/Construir todo")]
	public static void ConstruirTodo() {
		foreach (var carpeta in new[] { "Scenes", "Prefabs", "Texturas", "Materiales", "Audio", "Mallas", "Sprites" }) {
			if (!AssetDatabase.IsValidFolder("Assets/" + carpeta)) AssetDatabase.CreateFolder("Assets", carpeta);
		}
		fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

		ConfigurarCapas();
		CrearTexturas();
		CrearMateriales();
		CrearMallas();
		CrearPrefabsPiezas();
		CrearIconos();
		CrearSonidos();
		CrearEscena();

		EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RUTA_ESCENA, true) };

		PlayerSettings.companyName = NOMBRE;
		PlayerSettings.productName = "Partida Fantasma VR";
		PlayerSettings.defaultScreenWidth = 1280;
		PlayerSettings.defaultScreenHeight = 720;
		PlayerSettings.defaultWebScreenWidth = 1280;
		PlayerSettings.defaultWebScreenHeight = 720;
		PlayerSettings.runInBackground = true;
		PlayerSettings.colorSpace = ColorSpace.Linear;
		PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
		PlayerSettings.WebGL.decompressionFallback = false;
		PlayerSettings.SplashScreen.show = false;
		//Pantalla completa sin bordes: así el nombre y la matrícula siempre quedan visibles arriba a la derecha
		PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
		PlayerSettings.defaultIsNativeResolution = true;
		PlayerSettings.resizableWindow = true;
		ConfigurarCalidad();
		ConfigurarXR();
		CrearPlantillaWeb();

		AssetDatabase.SaveAssets();
		Debug.Log("ConstructorProyecto: todo construido");
	}

	[MenuItem("Fantasma/Compilar WebGL")]
	public static void CompilarWebGL() {
		PlayerSettings.WebGL.template = "PROJECT:PartidaFantasma";
		//Capa de compatibilidad de WebXR con Emscripten 4 (ver Assets/Plugins/WebGL/WebXRCompat.jspre)
		AssetDatabase.ImportAsset("Assets/Plugins/WebGL/WebXRCompat.jspre", ImportAssetOptions.ForceSynchronousImport);
		if (AssetImporter.GetAtPath("Assets/Plugins/WebGL/WebXRCompat.jspre") is PluginImporter compat) {
			compat.SetCompatibleWithAnyPlatform(false);
			compat.SetCompatibleWithPlatform(BuildTarget.WebGL, true);
			compat.SaveAndReimport();
		}
		Compilar(BuildTarget.WebGL, "Builds/WebGL");
	}

	[MenuItem("Fantasma/Compilar Windows")]
	public static void CompilarWindows() => Compilar(BuildTarget.StandaloneWindows64, "Builds/Windows/PartidaFantasmaVR.exe");

	static void Compilar(BuildTarget destino, string ruta) {
		var opciones = new BuildPlayerOptions {
			scenes = new[] { RUTA_ESCENA },
			locationPathName = ruta,
			target = destino,
			options = BuildOptions.None
		};
		var reporte = BuildPipeline.BuildPlayer(opciones);
		Debug.Log("ConstructorProyecto: build " + destino + " " + reporte.summary.result + " " + reporte.summary.totalSize / (1024 * 1024) + " MB");
		if (reporte.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
	}

	// ------------------------------------------------ Configuración del proyecto

	static void ConfigurarCapas() {
		var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		var capas = tagManager.FindProperty("layers");
		for (int i = 8; i < 32; i++) capas.GetArrayElementAtIndex(i).stringValue = "";
		capas.GetArrayElementAtIndex(CAPA_INTERACTUABLE).stringValue = "Interactuable";
		capas.GetArrayElementAtIndex(CAPA_TABLERO).stringValue = "Tablero";
		capas.GetArrayElementAtIndex(CAPA_SUELO).stringValue = "Suelo";
		tagManager.ApplyModifiedProperties();
		AssetDatabase.SaveAssets();
	}

	static void ConfigurarCalidad() {
		var nombres = QualitySettings.names;
		int actual = QualitySettings.GetQualityLevel();
		for (int i = 0; i < nombres.Length; i++) {
			QualitySettings.SetQualityLevel(i, false);
			QualitySettings.shadows = ShadowQuality.All;
			QualitySettings.shadowResolution = i >= 3 ? ShadowResolution.VeryHigh : ShadowResolution.High;
			QualitySettings.shadowDistance = 18f;
			QualitySettings.shadowCascades = i >= 3 ? 2 : 1;
			QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 4);
			QualitySettings.antiAliasing = i >= 2 ? 4 : 0;
			QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
			QualitySettings.vSyncCount = 1;
		}
		QualitySettings.SetQualityLevel(actual, false);
	}

	//Realidad virtual: OpenXR (visores de PC) para Windows y WebXR para el navegador
	static void ConfigurarXR() {
		if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
		EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget porDestino);
		if (porDestino == null) {
			porDestino = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
			AssetDatabase.CreateAsset(porDestino, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
			EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, porDestino, true);
		}
		var destinos = new[] { (BuildTargetGroup.Standalone, "UnityEngine.XR.OpenXR.OpenXRLoader"), (BuildTargetGroup.WebGL, "WebXR.WebXRLoader") };
		foreach (var (grupo, cargador) in destinos) {
			if (!porDestino.HasSettingsForBuildTarget(grupo)) porDestino.CreateDefaultSettingsForBuildTarget(grupo);
			if (!porDestino.HasManagerSettingsForBuildTarget(grupo)) porDestino.CreateDefaultManagerSettingsForBuildTarget(grupo);
			var general = porDestino.SettingsForBuildTarget(grupo);
			general.InitManagerOnStart = true;
			EditorUtility.SetDirty(general);
			var manager = porDestino.ManagerSettingsForBuildTarget(grupo);
			if (!manager.activeLoaders.Any(l => l != null && l.GetType().FullName == cargador)) {
				bool ok = XRPackageMetadataStore.AssignLoader(manager, cargador, grupo);
				Debug.Log("XR: cargador " + cargador + " para " + grupo + " = " + ok);
			}
			EditorUtility.SetDirty(manager);
		}
		EditorUtility.SetDirty(porDestino);
		AssetDatabase.SaveAssets();

		//WebXR con el pipeline integrado (varias cámaras en lugar del subsistema de pantalla)
		EditorBuildSettings.TryGetConfigObject("WebXR.Settings", out WebXR.WebXRSettings web);
		if (web == null) {
			if (!AssetDatabase.IsValidFolder("Assets/XR/Settings")) AssetDatabase.CreateFolder("Assets/XR", "Settings");
			web = ScriptableObject.CreateInstance<WebXR.WebXRSettings>();
			AssetDatabase.CreateAsset(web, "Assets/XR/Settings/WebXRSettings.asset");
			EditorBuildSettings.AddConfigObject("WebXR.Settings", web, true);
		}
		web.DisableXRDisplaySubsystem = true;
		web.AutoLoadWebXRInputSystem = false;
		web.AutoLoadWebXRManager = true;
		web.VRRequiredReferenceSpace = WebXR.WebXRSettings.ReferenceSpaceTypes.local_floor;
		EditorUtility.SetDirty(web);

		//OpenXR: perfiles de controles de los visores más comunes
		UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
		var openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
		if (openxr != null) {
			foreach (var f in openxr.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRInteractionFeature>()) {
				if (f is OculusTouchControllerProfile || f is ValveIndexControllerProfile || f is HTCViveControllerProfile
					|| f is KHRSimpleControllerProfile || f is MicrosoftMotionControllerProfile || f is MetaQuestTouchPlusControllerProfile) {
					f.enabled = true;
					EditorUtility.SetDirty(f);
				}
			}
			openxr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
			EditorUtility.SetDirty(openxr);
		} else Debug.LogWarning("XR: no se encontraron los ajustes de OpenXR");
		AssetDatabase.SaveAssets();
	}

	//Plantilla web con el botón "Entrar en VR" (WebXR)
	static void CrearPlantillaWeb() {
		string origen = Directory.GetDirectories("Library/PackageCache", "com.de-panther.webxr@*").FirstOrDefault();
		string destino = "Assets/WebGLTemplates/PartidaFantasma";
		Directory.CreateDirectory(destino + "/TemplateData");
		if (origen != null) {
			foreach (var f in Directory.GetFiles(origen + "/Hidden~/WebGLTemplates/WebXRFullView2020/TemplateData")) {
				if (f.EndsWith(".meta")) continue;
				File.Copy(f, destino + "/TemplateData/" + Path.GetFileName(f), true);
			}
		}
		File.WriteAllText(destino + "/index.html", PlantillaHtml());
		File.AppendAllText(destino + "/TemplateData/style.css", EstiloExtra());
		AssetDatabase.Refresh();
	}

	// ------------------------------------------------ Utilidades de objetos

	static GameObject Vacio(string nombre, Transform padre, Vector3 pos, Vector3? rot = null) {
		var go = new GameObject(nombre);
		if (padre != null) go.transform.SetParent(padre, false);
		go.transform.localPosition = pos;
		go.transform.localRotation = Quaternion.Euler(rot ?? Vector3.zero);
		if (padre != null) go.layer = padre.gameObject.layer;
		return go;
	}

	//Primitiva con material. colision: 0 = sin colisionador, 1 = el de la primitiva
	static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat, Vector3? rot = null, int colision = 1) {
		var go = GameObject.CreatePrimitive(tipo);
		go.name = nombre;
		if (padre != null) go.transform.SetParent(padre, false);
		go.transform.localPosition = pos;
		go.transform.localScale = escala;
		go.transform.localRotation = Quaternion.Euler(rot ?? Vector3.zero);
		go.layer = padre != null ? padre.gameObject.layer : 0;
		go.GetComponent<Renderer>().sharedMaterial = mat;
		if (colision == 0) Object.DestroyImmediate(go.GetComponent<Collider>());
		return go;
	}

	static GameObject Cubo(string n, Transform p, Vector3 pos, Vector3 esc, Material m, Vector3? rot = null, int col = 1) => Pieza(PrimitiveType.Cube, n, p, pos, esc, m, rot, col);
	static GameObject Cilindro(string n, Transform p, Vector3 pos, Vector3 esc, Material m, Vector3? rot = null, int col = 1) => Pieza(PrimitiveType.Cylinder, n, p, pos, esc, m, rot, col);
	static GameObject Esfera(string n, Transform p, Vector3 pos, Vector3 esc, Material m, int col = 1) => Pieza(PrimitiveType.Sphere, n, p, pos, esc, m, null, col);
	static GameObject Capsula(string n, Transform p, Vector3 pos, Vector3 esc, Material m, Vector3? rot = null, int col = 1) => Pieza(PrimitiveType.Capsule, n, p, pos, esc, m, rot, col);

	static GameObject ConMalla(string nombre, Transform padre, Mesh malla, Material mat, Vector3 pos, Vector3? rot = null, Vector3? escala = null) {
		var go = Vacio(nombre, padre, pos, rot);
		go.transform.localScale = escala ?? Vector3.one;
		go.AddComponent<MeshFilter>().sharedMesh = malla;
		go.AddComponent<MeshRenderer>().sharedMaterial = mat;
		return go;
	}

	static void Capa(GameObject go, int capa) {
		foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = capa;
	}

	static void SinSombras(GameObject go) {
		foreach (var r in go.GetComponentsInChildren<Renderer>(true)) {
			r.shadowCastingMode = ShadowCastingMode.Off;
			r.receiveShadows = false;
		}
	}

	static void SinColision(GameObject go) {
		foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
	}

	// ------------------------------------------------ Utilidades de interfaz

	//Lienzo en el mundo: ancho y alto en "píxeles" de interfaz y metros por píxel
	static Canvas Lienzo(string nombre, Transform padre, Vector3 pos, Vector3 rot, Vector2 tam, float metrosPorPixel) {
		var go = Vacio(nombre, padre, pos, rot);
		go.layer = 5;
		var c = go.AddComponent<Canvas>();
		c.renderMode = RenderMode.WorldSpace;
		var rt = go.GetComponent<RectTransform>();
		rt.sizeDelta = tam;
		go.transform.localScale = Vector3.one * metrosPorPixel;
		var esc = go.AddComponent<CanvasScaler>();
		esc.dynamicPixelsPerUnit = 4f;
		return c;
	}

	static RectTransform Rect(string nombre, Transform padre, Vector2 pos, Vector2 tam, Vector2? ancla = null) {
		var go = new GameObject(nombre, typeof(RectTransform));
		go.layer = 5;
		go.transform.SetParent(padre, false);
		var rt = go.GetComponent<RectTransform>();
		var a = ancla ?? new Vector2(0.5f, 0.5f);
		rt.anchorMin = rt.anchorMax = a;
		rt.pivot = a;
		rt.anchoredPosition = pos;
		rt.sizeDelta = tam;
		return rt;
	}

	static Text Texto(string nombre, Transform padre, string contenido, int tam, Color color, Vector2 pos, Vector2 caja,
		TextAnchor alineacion = TextAnchor.MiddleCenter, FontStyle estilo = FontStyle.Normal, Vector2? ancla = null) {
		var rt = Rect(nombre, padre, pos, caja, ancla);
		var t = rt.gameObject.AddComponent<Text>();
		t.font = fuente;
		t.text = contenido;
		t.fontSize = tam;
		t.color = color;
		t.alignment = alineacion;
		t.fontStyle = estilo;
		t.supportRichText = true;
		t.horizontalOverflow = HorizontalWrapMode.Wrap;
		t.verticalOverflow = VerticalWrapMode.Overflow;
		t.raycastTarget = false;
		return t;
	}

	static Image Imagen(string nombre, Transform padre, Color color, Vector2 pos, Vector2 tam, Vector2? ancla = null, Sprite sprite = null) {
		var rt = Rect(nombre, padre, pos, tam, ancla);
		var im = rt.gameObject.AddComponent<Image>();
		im.color = color;
		im.sprite = sprite;
		im.raycastTarget = false;
		return im;
	}

	static void Sombra(Text t, float distancia = 2f) {
		var s = t.gameObject.AddComponent<Shadow>();
		s.effectColor = new Color(0f, 0f, 0f, 0.8f);
		s.effectDistance = new Vector2(distancia, -distancia);
	}
}
