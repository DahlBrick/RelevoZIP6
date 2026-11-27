using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.Timeline;

/// <summary>
/// Construye el prólogo con Timeline + Cinemachine en Assets/Scenes/Juego/Cinematica_Liria.unity:
/// Liria colocada en el editor, una cámara de Cinemachine con raíl por plano, la Timeline con los
/// cortes, la ropa tendida, el humo, los cables de la antena y el título 3D.
///
/// OJO: vuelve a crear la escena desde cero. Úsalo solo para regenerarla; los retoques hechos a mano
/// en la escena (cámaras, raíles, tiempos) se pierden. La versión anterior del prólogo está en
/// Cinematica_Liria_Antigua.unity.
/// </summary>
public static class ConstruirPrologo
{
    const string RutaEscena = "Assets/Scenes/Juego/Cinematica_Liria.unity";
    const string Carpeta = "Assets/Cinematicas";
    const string RutaTimeline = Carpeta + "/Prologo.playable";
    const string RutaRuido = "Packages/com.unity.cinemachine/Presets/Noise/Handheld_normal_mild.asset";
    const float AlturaTitulo = 34f;

    struct Plano
    {
        public string nombre; public float inicio, fin, fov, temblor;
        public Vector3[] rail; public Vector3 miraA, miraB;
        public Plano(string n, float a, float b, float fov, float temblor, Vector3 miraA, Vector3 miraB, params Vector3[] rail)
        { nombre = n; inicio = a; fin = b; this.fov = fov; this.temblor = temblor; this.miraA = miraA; this.miraB = miraB; this.rail = rail; }
    }

    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    // Lista de planos (docs/narrativa/prologo_planos.md). Coordenadas del mundo de Liria: el mar
    // abierto hacia -z, el muelle B01 en z -14, la calle de piedra a lo largo de x en z = 35, el canal
    // con la rueda en x = 25, la antena en (6, 25, 60).
    static readonly Plano[] Planos =
    {
        new Plano("01 General de Liria", 0f, 5f, 40f, 0.25f, V(1, 3, 20), V(2, 4, 22), V(-20, 26, -56), V(-14, 19, -47), V(-10, 13, -38)),
        new Plano("02 Rueda de la bomba", 5f, 8f, 45f, 0.4f, V(24.8f, -0.3f, 6), V(24.8f, -0.1f, 6), V(26.6f, 0.1f, 0.8f), V(27.2f, 0.4f, 2.4f)),
        new Plano("03 Ropa y humo", 8f, 11f, 50f, 0.3f, V(6, 4.5f, 36), V(10, 4.8f, 36), V(-14, 2.3f, 34.6f), V(-8, 2.5f, 35.2f)),
        new Plano("04 Antena de la red", 11f, 14f, 42f, 0.35f, V(6, 20, 60), V(6, 23, 60), V(-2, 5, 42), V(0, 4, 44)),
        new Plano("05 General desde el faro", 14f, 17f, 42f, 0.25f, V(4, 3, 28), V(6, 3, 30), V(-50, 16, -6), V(-44, 14, 2)),
        new Plano("06 Apagon al fondo", 17f, 20f, 44f, 0.3f, V(1, 5, 34), V(1, 4, 32), V(-2, 7, -34), V(-1.5f, 6, -30)),
        new Plano("07 Farolas de la calle", 20f, 22f, 50f, 0.45f, V(-10, 3.4f, 35.6f), V(-12, 3.2f, 35.6f), V(14, 2.4f, 35), V(10, 2.6f, 35.3f)),
        new Plano("08 La rueda se para", 22f, 23f, 38f, 0.5f, V(24.8f, -0.2f, 6), V(24.8f, -0.2f, 6), V(26, 0.3f, 3), V(26.1f, 0.3f, 3.2f)),
        new Plano("09 Ropa a oscuras", 23f, 25f, 46f, 0.3f, V(6, 4.6f, 36), V(7, 4.6f, 36), V(-9, 2.4f, 35), V(-8.4f, 2.5f, 35.1f)),
        new Plano("10 Ultimas luces", 25f, 27f, 44f, 0.35f, V(-1, 2, -6), V(-2, 1.5f, -10), V(-3, 5, -30), V(-2.6f, 4.6f, -27.5f)),
        new Plano("11 El agua sube", 27f, 30.2f, 40f, 0.3f, V(-1.5f, -0.5f, -12), V(-1.5f, -0.3f, -12), V(-7, -0.9f, -21), V(-6, -0.8f, -19.5f)),
        new Plano("12 Titulo", 30.2f, 38f, 42f, 0.15f, V(0, 10, 80), V(0, 12, 100), V(0, 30, -96), V(0, 24, -82)),
    };

    // Centro (en alturas de letra) y ancho de cada letra de RELEVO, de la imagen generada.
    static readonly float[] CentroLetra = { -1.2115f, -0.7375f, -0.3231f, 0.0913f, 0.5923f, 1.1712f };
    static readonly float[] AnchoLetra = { 0.5538f, 0.4788f, 0.4346f, 0.4788f, 0.6077f, 0.6346f };

    [MenuItem("Relevo/Construir prólogo (Timeline + Cinemachine)")]
    public static void Construir()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Sal de Play Mode."); return; }
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "Cinematicas");
        if (!AssetDatabase.IsValidFolder(Carpeta + "/Materiales")) AssetDatabase.CreateFolder(Carpeta, "Materiales");

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        KitRelevo kit = KitRelevo.Activo;

        GameObject raiz = new GameObject("Prologo");
        Prologo prologo = raiz.AddComponent<Prologo>();
        PlayableDirector director = raiz.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.Hold;

        // Liria, colocada como en Liria_Prueba (raíz girada 180°).
        Transform liria = new GameObject("Liria").transform;
        liria.rotation = Quaternion.Euler(0f, 180f, 0f);
        foreach (GameObject b in kit.bloquesLiria)
        {
            if (b == null || b.name.Contains("Limites")) continue;
            ((GameObject)PrefabUtility.InstantiatePrefab(b, liria)).name = b.name;
        }

        Light luna = new GameObject("Luna").AddComponent<Light>();
        luna.type = LightType.Directional;
        luna.transform.rotation = Quaternion.Euler(28f, 200f, 0f);
        luna.color = new Color(0.55f, 0.65f, 0.95f);
        luna.intensity = 0.8f;
        luna.shadows = LightShadows.Soft;
        luna.shadowStrength = 0.6f;

        // Cámara del juego: el cerebro de Cinemachine elige qué cámara virtual se ve.
        GameObject goCam = new GameObject("Camara de cine") { tag = "MainCamera" };
        Camera cam = goCam.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 1500f;
        goCam.AddComponent<AudioListener>();
        CinemachineBrain cerebro = goCam.AddComponent<CinemachineBrain>();
        cerebro.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        // Timeline con una pista de cámaras.
        AssetDatabase.DeleteAsset(RutaTimeline);
        TimelineAsset tl = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(tl, RutaTimeline);
        CinemachineTrack pista = tl.CreateTrack<CinemachineTrack>(null, "Camaras");
        director.playableAsset = tl;
        director.SetGenericBinding(pista, cerebro);

        NoiseSettings ruido = AssetDatabase.LoadAssetAtPath<NoiseSettings>(RutaRuido);
        Transform planos = new GameObject("Planos").transform;
        foreach (Plano p in Planos)
        {
            CinemachineCamera vc = CrearPlano(planos, p, director, ruido);
            TimelineClip clip = pista.CreateClip<CinemachineShot>();
            clip.start = p.inicio;
            clip.duration = p.fin - p.inicio;
            clip.displayName = p.nombre;
            CinemachineShot shot = (CinemachineShot)clip.asset;
            shot.VirtualCamera.exposedName = GUID.Generate().ToString();
            director.SetReferenceValue(shot.VirtualCamera.exposedName, vc);
        }
        goCam.transform.position = Planos[0].rail[0];
        goCam.transform.LookAt(Planos[0].miraA);

        List<Transform> prendas = Tendederos();
        List<ParticleSystem> humos = Humos(kit);
        Cables();
        List<Transform> letras; List<Renderer> frentes;
        Titulo(out letras, out frentes);

        SerializedObject so = new SerializedObject(prologo);
        so.FindProperty("director").objectReferenceValue = director;
        so.FindProperty("camara").objectReferenceValue = cam;
        so.FindProperty("bloquesLiria").objectReferenceValue = liria;
        so.FindProperty("luna").objectReferenceValue = luna;
        so.FindProperty("acciones").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/Relevo.inputactions");
        so.FindProperty("cielo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("3d119cfd4539681448893b6f7281ad18"));
        so.FindProperty("cancion").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musica/musica_prologo.mp3");
        so.FindProperty("alturaTitulo").floatValue = AlturaTitulo;
        Lista(so.FindProperty("prendas"), prendas);
        Lista(so.FindProperty("humos"), humos);
        Lista(so.FindProperty("letras"), letras);
        Lista(so.FindProperty("frentesTitulo"), frentes);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(tl);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(escena, RutaEscena);
        Debug.Log("RELEVO: prólogo construido en " + RutaEscena);
    }

    static void Lista<T>(SerializedProperty prop, List<T> objetos) where T : Object
    {
        prop.arraySize = objetos.Count;
        for (int i = 0; i < objetos.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = objetos[i];
    }

    static CinemachineCamera CrearPlano(Transform padre, Plano p, PlayableDirector director, NoiseSettings ruido)
    {
        Transform raiz = new GameObject("Plano " + p.nombre).transform;
        raiz.SetParent(padre, false);

        SplineContainer rail = new GameObject("Rail").AddComponent<SplineContainer>();
        rail.transform.SetParent(raiz, false);
        Spline s = rail.Spline;
        s.Clear();
        foreach (Vector3 punto in p.rail) s.Add(new BezierKnot((float3)punto), TangentMode.AutoSmooth);

        Transform miraA = new GameObject("Mira inicio").transform; miraA.SetParent(raiz, false); miraA.position = p.miraA;
        Transform miraB = new GameObject("Mira fin").transform; miraB.SetParent(raiz, false); miraB.position = p.miraB;
        Transform mira = new GameObject("Mira").transform; mira.SetParent(raiz, false); mira.position = p.miraA;

        GameObject go = new GameObject("Camara");
        go.transform.SetParent(raiz, false);
        go.transform.position = p.rail[0];
        CinemachineCamera vc = go.AddComponent<CinemachineCamera>();
        LensSettings lente = vc.Lens;
        lente.FieldOfView = p.fov;
        lente.NearClipPlane = 0.05f;
        lente.FarClipPlane = 1500f;
        vc.Lens = lente;
        vc.LookAt = mira;

        CinemachineSplineDolly dolly = go.AddComponent<CinemachineSplineDolly>();
        dolly.Spline = rail;
        dolly.PositionUnits = PathIndexUnit.Normalized;
        dolly.CameraPosition = 0f;
        dolly.CameraRotation = CinemachineSplineDolly.RotationMode.Default;

        CinemachineRotationComposer composer = go.AddComponent<CinemachineRotationComposer>();
        composer.Damping = new Vector2(0.25f, 0.25f);

        CinemachineBasicMultiChannelPerlin temblor = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
        temblor.NoiseProfile = ruido;
        temblor.AmplitudeGain = p.temblor;
        temblor.FrequencyGain = 0.5f;

        PlanoPrologo plano = go.AddComponent<PlanoPrologo>();
        plano.director = director;
        plano.miraInicio = miraA;
        plano.miraFin = miraB;
        plano.mira = mira;
        return vc;
    }

    // ------------------------------------------------------------------ vida de la ciudad

    static Material Mat(string nombre, Color c, string shader = "Universal Render Pipeline/Lit")
    {
        string ruta = Carpeta + "/Materiales/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, ruta); }
        m.shader = Shader.Find(shader);
        m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static LineRenderer Cuerda(Transform padre, string nombre, Vector3 a, Vector3 b, float comba, float ancho, Material m)
    {
        LineRenderer l = new GameObject(nombre).AddComponent<LineRenderer>();
        l.transform.SetParent(padre, false);
        l.useWorldSpace = true;
        const int n = 16;
        l.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float u = i / (n - 1f);
            l.SetPosition(i, Vector3.Lerp(a, b, u) + Vector3.down * comba * 4f * u * (1f - u));
        }
        l.widthMultiplier = ancho;
        l.sharedMaterial = m;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return l;
    }

    /// <summary>Dos tendederos cruzando la calle del barrio de piedra, con ropa que mueve el viento.</summary>
    static List<Transform> Tendederos()
    {
        var prendas = new List<Transform>();
        Transform raiz = new GameObject("Ropa tendida").transform;
        Material cuerda = Mat("Cuerda", new Color(0.12f, 0.1f, 0.08f), "Universal Render Pipeline/Unlit");
        Material[] telas =
        {
            Mat("Tela crema", new Color(0.86f, 0.8f, 0.68f)), Mat("Tela roja", new Color(0.62f, 0.26f, 0.22f)),
            Mat("Tela azul", new Color(0.32f, 0.42f, 0.62f)), Mat("Tela ocre", new Color(0.8f, 0.6f, 0.3f)),
        };
        float[] xs = { -2f, 4.5f };
        int k = 0;
        foreach (float x in xs)
        {
            Vector3 a = new Vector3(x, 5.2f, 32.2f), b = new Vector3(x + 0.4f, 5.0f, 37.9f);
            Cuerda(raiz, "Cuerda", a, b, 0.35f, 0.035f, cuerda);
            for (int i = 0; i < 4; i++)
            {
                float u = 0.2f + i * 0.2f;
                Vector3 punto = Vector3.Lerp(a, b, u) + Vector3.down * 0.35f * 4f * u * (1f - u);
                Transform pivote = new GameObject("Prenda").transform;
                pivote.SetParent(raiz, false);
                pivote.position = punto;
                pivote.rotation = Quaternion.Euler(0f, 90f, 0f);
                GameObject tela = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(tela.GetComponent<Collider>());
                tela.name = "Tela";
                tela.transform.SetParent(pivote, false);
                float alto = 0.55f + 0.25f * ((i + k) % 3) / 2f;
                tela.transform.localScale = new Vector3(0.04f, alto, 0.5f + 0.15f * (i % 2));
                tela.transform.localPosition = new Vector3(0f, -alto * 0.5f, 0f);
                tela.GetComponent<Renderer>().sharedMaterial = telas[(i + k) % telas.Length];
                prendas.Add(pivote);
            }
            k++;
        }
        return prendas;
    }

    static List<ParticleSystem> Humos(KitRelevo kit)
    {
        var lista = new List<ParticleSystem>();
        Transform raiz = new GameObject("Humo de chimeneas").transform;
        Vector3[] puntos = { new Vector3(-6f, 11f, 39f), new Vector3(3f, 11.5f, 40f), new Vector3(10f, 10f, 31f) };
        foreach (Vector3 p in puntos)
        {
            GameObject go = new GameObject("Humo");
            go.transform.SetParent(raiz, false);
            go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startColor = new Color(0.45f, 0.47f, 0.55f, 0.35f);
            main.gravityModifier = -0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.2f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.35f, 0.6f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var tam = ps.sizeOverLifetime; tam.enabled = true;
            tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.4f));
            var col = ps.colorOverLifetime; col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = kit != null ? kit.particula : null;
            lista.Add(ps);
        }
        return lista;
    }

    /// <summary>Cables que bajan de la antena de la red hacia la ciudad.</summary>
    static void Cables()
    {
        Transform raiz = new GameObject("Cables de la antena").transform;
        Material m = Mat("Cable", new Color(0.05f, 0.05f, 0.07f), "Universal Render Pipeline/Unlit");
        Vector3 punta = new Vector3(6f, 24.5f, 60f);
        Vector3[] destinos = { new Vector3(-6f, 9f, 40f), new Vector3(10f, 9f, 38f), new Vector3(20f, 8f, 44f), new Vector3(-14f, 8f, 46f) };
        foreach (Vector3 d in destinos) Cuerda(raiz, "Cable", punta, d, 1.4f, 0.07f, m);
    }

    // ------------------------------------------------------------------ título

    /// <summary>
    /// RELEVO en letras gigantes detrás de la ciudad. Cada letra es un frente ámbar con tinta y una
    /// pila de siluetas cobrizas detrás que le dan volumen. Empiezan bajo el agua; Prologo las sube.
    /// </summary>
    static void Titulo(out List<Transform> letras, out List<Renderer> frentes)
    {
        letras = new List<Transform>();
        frentes = new List<Renderer>();
        Transform raiz = new GameObject("Titulo 3D").transform;
        raiz.position = new Vector3(0f, -1.5f, 135f);
        const int Capas = 16;
        for (int i = 0; i < 6; i++)
        {
            Texture2D frente = Resources.Load<Texture2D>("Cinematica/Titulo/letra_" + i);
            Texture2D silueta = Resources.Load<Texture2D>("Cinematica/Titulo/silueta_" + i);
            Transform letra = new GameObject("Letra " + "RELEVO"[i]).transform;
            letra.SetParent(raiz, false);
            letra.localPosition = new Vector3(CentroLetra[i] * AlturaTitulo, -AlturaTitulo * 1.15f, 0f);
            Vector3 escala = new Vector3(AnchoLetra[i] * AlturaTitulo, AlturaTitulo, 1f);

            Material mf = Recorte("Titulo frente " + i, frente, Color.white);
            Material ms = Recorte("Titulo canto " + i, silueta, new Color(0.42f, 0.24f, 0.1f));
            for (int c = Capas; c >= 0; c--)
            {
                GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.name = c == 0 ? "Frente" : "Canto";
                q.transform.SetParent(letra, false);
                q.transform.localPosition = new Vector3(0f, AlturaTitulo * 0.5f, c * 0.22f);
                q.transform.localScale = escala;
                Renderer r = q.GetComponent<Renderer>();
                r.sharedMaterial = c == 0 ? mf : ms;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (c == 0) frentes.Add(r);
            }
            letras.Add(letra);
        }
    }

    static Material Recorte(string nombre, Texture2D tex, Color c)
    {
        Material m = Mat(nombre, c, "Universal Render Pipeline/Unlit");
        m.SetTexture("_BaseMap", tex);
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        m.renderQueue = 2450;
        EditorUtility.SetDirty(m);
        return m;
    }
}
