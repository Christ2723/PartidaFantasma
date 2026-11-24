using UnityEditor;
using UnityEngine;

//Materiales (shader Standard del pipeline integrado + shaders propios del cielo y del brillo fantasmal)
public static partial class ConstructorProyecto {

	static Material mClara, mOscura, mMarco, mMesa, mBoj, mEbano, mAlfombra, mSuelo, mRoca, mBordeLuz, mTela, mCuero, mMetal, mMetalOscuro,
		mPlasticoNegro, mLCD, mPapel, mVidrio, mAgua, mLuzVerde, mLuzRoja, mPantallaNegra, mTerciopelo, mCuerda, mOro, mBlanco, mLuna,
		mEspectral, mLaser, mMarcador, mParticula, mCielo, mLente, mTruss, mCable, mBotonRojo, mBotonAzul, mBotonVerde, mBotonAmbar, mBotonMorado, mBotonBlanco, mUIEncima;

	static Material Mat(string nombre, Color color, float suavidad = 0.4f, float metal = 0f, Texture2D tex = null, Vector2? repetir = null) {
		string ruta = "Assets/Materiales/" + nombre + ".mat";
		var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
		if (m == null) {
			m = new Material(Shader.Find("Standard"));
			AssetDatabase.CreateAsset(m, ruta);
		}
		m.shader = Shader.Find("Standard");
		m.color = color;
		m.SetFloat("_Glossiness", suavidad);
		m.SetFloat("_Metallic", metal);
		m.mainTexture = tex;
		m.mainTextureScale = repetir ?? Vector2.one;
		//Emisión activada (negra) para poder iluminar las piezas desde el código
		m.EnableKeyword("_EMISSION");
		m.SetColor("_EmissionColor", Color.black);
		m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
		EditorUtility.SetDirty(m);
		return m;
	}

	static Material Emisivo(string nombre, Color color, float fuerza) {
		var m = Mat(nombre, color * 0.3f, 0.6f);
		m.SetColor("_EmissionColor", color * fuerza);
		return m;
	}

	static Material Transparente(string nombre, Color color, float suavidad) {
		var m = Mat(nombre, color, suavidad);
		m.SetFloat("_Mode", 3f);
		m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
		m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		m.SetInt("_ZWrite", 0);
		m.DisableKeyword("_ALPHATEST_ON");
		m.DisableKeyword("_ALPHABLEND_ON");
		m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
		m.renderQueue = 3000;
		return m;
	}

	static Material Propio(string nombre, string shader) {
		string ruta = "Assets/Materiales/" + nombre + ".mat";
		var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
		var s = Shader.Find(shader);
		if (m == null) {
			m = new Material(s);
			AssetDatabase.CreateAsset(m, ruta);
		}
		m.shader = s;
		m.enableInstancing = true;
		EditorUtility.SetDirty(m);
		return m;
	}

	static void CrearMateriales() {
		AssetDatabase.ImportAsset("Assets/Shaders/Cielo.shader", ImportAssetOptions.ForceSynchronousImport);
		AssetDatabase.ImportAsset("Assets/Shaders/Espectral.shader", ImportAssetOptions.ForceSynchronousImport);
		AssetDatabase.ImportAsset("Assets/Shaders/SinNiebla.shader", ImportAssetOptions.ForceSynchronousImport);
		AssetDatabase.ImportAsset("Assets/Shaders/UIEncima.shader", ImportAssetOptions.ForceSynchronousImport);

		mClara = Mat("CasillaClara", Color.white, 0.62f, 0f, texMaderaClara);
		mOscura = Mat("CasillaOscura", Color.white, 0.62f, 0f, texMaderaOscura);
		mMarco = Mat("MarcoTablero", Color.white, 0.68f, 0f, texMarco);
		mMesa = Mat("Mesa", Color.white, 0.55f, 0f, texMesa);
		mBoj = Mat("PiezaBlanca", new Color(1f, 0.97f, 0.92f), 0.58f, 0f, texBoj);
		mEbano = Mat("PiezaNegra", Color.white, 0.72f, 0f, texEbano);
		mAlfombra = Mat("Alfombra", Color.white, 0.05f, 0f, texAlfombra, new Vector2(6f, 5f));
		mSuelo = Mat("Suelo", Color.white, 0.82f, 0.1f, texSuelo, new Vector2(8f, 8f));
		mRoca = Mat("Roca", Color.white, 0.15f, 0f, texRoca, new Vector2(6f, 3f));
		mBordeLuz = Emisivo("BordeLuz", new Color(0.3f, 0.75f, 1f), 2.2f);
		mTela = Mat("Faldon", new Color(0.05f, 0.07f, 0.16f), 0.15f);
		mCuero = Mat("Cuero", new Color(0.05f, 0.05f, 0.055f), 0.55f);
		mMetal = Mat("Metal", new Color(0.75f, 0.76f, 0.78f), 0.75f, 0.9f);
		mMetalOscuro = Mat("MetalOscuro", new Color(0.12f, 0.12f, 0.13f), 0.6f, 0.8f);
		mPlasticoNegro = Mat("PlasticoNegro", new Color(0.035f, 0.035f, 0.04f), 0.7f);
		mLCD = Emisivo("LCD", new Color(0.62f, 0.68f, 0.58f), 0.35f);
		mLCD.color = new Color(0.62f, 0.68f, 0.58f);
		mPapel = Mat("Papel", Color.white, 0.1f, 0f, texPapel);
		mVidrio = Transparente("Vidrio", new Color(0.75f, 0.85f, 0.95f, 0.22f), 0.95f);
		mAgua = Transparente("Agua", new Color(0.6f, 0.8f, 0.95f, 0.35f), 0.95f);
		mLuzVerde = Emisivo("LuzVerde", new Color(0.1f, 1f, 0.3f), 0.25f);
		mLuzRoja = Emisivo("LuzRoja", new Color(1f, 0.08f, 0.05f), 3f);
		mPantallaNegra = Mat("PantallaNegra", new Color(0.01f, 0.01f, 0.015f), 0.9f);
		mTerciopelo = Mat("Terciopelo", Color.white, 0.2f, 0f, texTerciopelo);
		mCuerda = Mat("Cuerda", new Color(0.45f, 0.03f, 0.06f), 0.35f);
		mOro = Mat("Oro", new Color(0.85f, 0.65f, 0.3f), 0.8f, 1f);
		mBlanco = Mat("Blanco", new Color(0.9f, 0.9f, 0.88f), 0.3f);
		mTruss = Mat("Truss", new Color(0.2f, 0.2f, 0.22f), 0.6f, 0.8f);
		mCable = Mat("Cable", new Color(0.02f, 0.02f, 0.02f), 0.5f);
		mLente = Emisivo("Lente", new Color(1f, 0.85f, 0.6f), 2.5f);
		mBotonRojo = Emisivo("BotonRojo", new Color(1f, 0.2f, 0.2f), 0.2f);
		mBotonAzul = Emisivo("BotonAzul", new Color(0.3f, 0.7f, 1f), 0.2f);
		mBotonVerde = Emisivo("BotonVerde", new Color(0.3f, 1f, 0.45f), 0.2f);
		mBotonAmbar = Emisivo("BotonAmbar", new Color(1f, 0.7f, 0.2f), 0.2f);
		mBotonMorado = Emisivo("BotonMorado", new Color(0.75f, 0.35f, 1f), 0.2f);
		mBotonBlanco = Emisivo("BotonBlanco", new Color(0.9f, 0.9f, 0.95f), 0.2f);

		mLuna = Propio("Luna", "PartidaFantasma/SinNiebla");
		mLuna.mainTexture = texLuna;
		mLuna.color = new Color(0.85f, 0.88f, 0.95f);

		mEspectral = Propio("Espectral", "PartidaFantasma/Espectral");
		mEspectral.SetColor("_Color", new Color(0.35f, 0.85f, 1f, 0.9f));
		mEspectral.SetFloat("_Borde", 2.2f);
		mEspectral.SetFloat("_Nucleo", 0.12f);

		mLaser = Propio("Laser", "PartidaFantasma/Espectral");
		mLaser.SetColor("_Color", Color.white);
		mLaser.SetFloat("_Nucleo", 1f);

		mMarcador = Propio("Marcador", "PartidaFantasma/Espectral");
		mMarcador.mainTexture = texPunto;
		mMarcador.SetColor("_Color", new Color(0.3f, 1f, 0.6f, 0.9f));
		mMarcador.SetFloat("_Nucleo", 1f);

		mParticula = Propio("Particula", "PartidaFantasma/Espectral");
		mParticula.mainTexture = texPunto;
		mParticula.SetColor("_Color", new Color(0.5f, 0.85f, 1f, 0.8f));
		mParticula.SetFloat("_Nucleo", 1f);

		mCielo = Propio("Cielo", "PartidaFantasma/Cielo");
		mUIEncima = Propio("UIEncima", "PartidaFantasma/UIEncima");
		AssetDatabase.SaveAssets();
	}
}
