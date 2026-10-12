using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Prepara el kit visual de RELEVO a partir de los packs CC0 de Assets/ThirdParty:
///  1. Crea los materiales propios (agua, follaje con viento, piedra del muelle, Nilo).
///  2. Reasigna los materiales de los modelos importados a esos materiales (remap del importador),
///     de modo que cualquier instancia de un árbol o del robot ya sale con el aspecto de RELEVO.
///  3. Configura las animaciones del robot (bucles) y crea el Animator de Nilo.
///  4. Crea o actualiza Assets/Resources/KitRelevo.asset con todas las referencias.
///
/// Es idempotente: ejecutarlo otra vez ajusta lo existente sin duplicar nada. Así el kit se puede
/// regenerar entero si se cambian colores o se añaden modelos, y queda documentado en código.
/// </summary>
public static class PrepararKitVisual
{
    const string Terceros = "Assets/ThirdParty";
    const string Naturaleza = Terceros + "/Quaternius_Nature/Models";
    const string Texturas = Terceros + "/Quaternius_Nature/Textures";
    const string Pirata = Terceros + "/Kenney_Pirate";
    const string Barcos = Terceros + "/Kenney_Watercraft";
    const string Robot = Terceros + "/Quaternius_Robot/Robot.fbx";

    const string CarpetaMateriales = "Assets/Materials/Kit";
    const string CarpetaAnimacion = "Assets/Animation";
    const string RutaKit = "Assets/Resources/KitRelevo.asset";

    [MenuItem("Relevo/Preparar kit visual")]
    public static string Preparar()
    {
        Directory.CreateDirectory(CarpetaMateriales);
        Directory.CreateDirectory(CarpetaAnimacion);
        Directory.CreateDirectory("Assets/Resources");

        // --- materiales propios
        Material agua = MaterialAgua("RELEVO_AguaCanal",
            Hex("5FB7AE"), Hex("1C5560"), 0.55f, 0.26f, 0.55f, 0.86f);
        Material aguaMar = MaterialAgua("RELEVO_AguaMar",
            Hex("5DB3AE"), Hex("174A57"), 2.4f, 0.4f, 0.32f, 0.9f);

        Dictionary<string, Material> follaje = new Dictionary<string, Material>
        {
            // Las texturas "_C" de Quaternius son máscaras de color para el shader de pago (la del
            // árbol retorcido es roja): se usan las versiones blancas y se tiñen aquí, con los verdes
            // apagados de la paleta del GDD.
            { "Leaves_NormalTree", Follaje("Leaves_NormalTree", Hex("7FA650"), 0.10f, 2.2f, 0.5f) },
            { "Leaves_TwistedTree", Follaje("Leaves_TwistedTree", Hex("98B55A"), 0.10f, 2.2f, 0.5f) },
            { "Leaves_Pine", Follaje("Leaf_Pine", Hex("5E8A4A"), 0.07f, 2.4f, 0.5f) },
            { "Leaves", Follaje("Leaves", Color.white, 0.10f, 1.2f, 0.5f) },
            { "Bark_NormalTree", Follaje("Bark_NormalTree", Color.white, 0.10f, 2.2f, 0.3f) },
            { "Bark_TwistedTree", Follaje("Bark_TwistedTree", Color.white, 0.10f, 2.2f, 0.3f) },
            { "Bark_DeadTree", Follaje("Bark_DeadTree", Color.white, 0.06f, 2.2f, 0.3f) },
            { "Grass", Follaje("Grass", Color.white, 0.09f, 0.55f, 0.55f) },
            { "Flowers", Follaje("Flowers", Color.white, 0.08f, 0.55f, 0.55f) },
            { "Mushrooms", Follaje("Mushrooms", Color.white, 0.0f, 1f, 0.4f) },
        };

        // La piedra del camino se tiñe al crema de la paleta: los adoquines grises de Quaternius
        // con el tinte cálido leen como la piedra de Liria del GDD.
        Material piedraCamino = Lit("RELEVO_PiedraCamino", Hex("F2E6CF"), TexturaNaturaleza("PathRocks_Diffuse"), 0.18f);
        Material rocas = Lit("RELEVO_Rocas", Hex("D9D2C2"), TexturaNaturaleza("Rocks_Diffuse"), 0.12f);
        Material piedraJunta = Lit("RELEVO_PiedraJunta", Hex("8C7F6E"), null, 0.1f);
        Material piedraMuro = Lit("RELEVO_PiedraMuro", Hex("BFAF94"), null, 0.08f);
        Material maderaMuelle = Lit("RELEVO_MaderaMuelle", Hex("5A3F2C"), null, 0.15f);

        follaje["PathRocks"] = piedraCamino;
        follaje["Rocks"] = rocas;

        // Nilo: carcasa clara, extremidades azul oscuro (GDD, sección 6).
        Material niloCarcasa = Lit("RELEVO_NiloModeloCarcasa", Hex("EEE7DA"), null, 0.55f);
        Material niloExtremidades = Lit("RELEVO_NiloModeloExtremidades", Hex("2C4566"), null, 0.35f);
        Material niloOscuro = Lit("RELEVO_NiloModeloOscuro", Hex("17202E"), null, 0.6f);

        ConfigurarTexturasDeFollaje();
        ConfigurarSpritesUI();

        // --- remap de los modelos importados
        int remapeados = 0;
        foreach (string ruta in Modelos(Naturaleza))
        {
            remapeados += Remapear(ruta, follaje);
        }

        // La tarima de Kenney comparte la textura 'colormap' con todo el pack y sale rosada al
        // lado de la piedra crema: solo en ella se cambia por la misma textura teñida a madera.
        Material maderaKenney = Lit("RELEVO_TarimaMadera", Hex("C9A587"),
            AssetDatabase.LoadAssetAtPath<Texture2D>(Pirata + "/Textures/colormap.png"), 0.2f);
        remapeados += Remapear(Pirata + "/platform.fbx", new Dictionary<string, Material> { { "colormap", maderaKenney } });

        Material particula = MaterialParticula();

        remapeados += Remapear(Robot, new Dictionary<string, Material>
        {
            { "Main", niloCarcasa },
            { "Grey", niloExtremidades },
            { "Black", niloOscuro },
        });

        ConfigurarAnimacionesRobot();
        AnimatorController animador = CrearAnimadorNilo();

        // --- kit
        KitRelevo kit = AssetDatabase.LoadAssetAtPath<KitRelevo>(RutaKit);
        if (kit == null)
        {
            kit = ScriptableObject.CreateInstance<KitRelevo>();
            AssetDatabase.CreateAsset(kit, RutaKit);
        }

        kit.agua = agua;
        kit.aguaMar = aguaMar;

        kit.losasDePiedra = Cargar(Naturaleza, "RockPath_Square_Wide");
        kit.tablones = Uno(Pirata, "platform");
        kit.piedraJunta = piedraJunta;
        kit.piedraMuro = piedraMuro;
        kit.maderaMuelle = maderaMuelle;

        kit.arboles = Cargar(Naturaleza, "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5",
            "TwistedTree_1", "TwistedTree_3");
        kit.arbustos = Cargar(Naturaleza, "Bush_Common", "Bush_Common_Flowers", "Plant_1", "Plant_7", "Fern_1");
        kit.flores = Cargar(Naturaleza, "Flower_3_Group", "Flower_4_Group", "Flower_3_Single", "Flower_4_Single", "Clover_1");
        kit.hierbas = Cargar(Naturaleza, "Grass_Common_Short", "Grass_Wispy_Short", "Grass_Common_Tall", "Grass_Wispy_Tall");
        kit.rocas = Cargar(Naturaleza, "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3");
        kit.guijarros = Cargar(Naturaleza, "Pebble_Round_1", "Pebble_Round_2", "Pebble_Round_3", "Pebble_Square_1", "Pebble_Square_2");

        kit.barril = Uno(Pirata, "barrel");
        kit.caja = Uno(Pirata, "crate");
        kit.botes = new[] { Uno(Barcos, "boat-row-small"), Uno(Barcos, "boat-fishing-small"), Uno(Barcos, "boat-row-large"), Uno(Barcos, "boat-sail-a") };
        kit.boya = Uno(Barcos, "buoy");
        kit.muelle = Uno(Pirata, "structure-platform-dock");
        kit.poste = Uno(Pirata, "mast");

        kit.particula = particula;

        // --- piezas del puzle (Space Kit y Fantasy Town de Kenney)
        const string Espacio = Terceros + "/Kenney_Space";
        const string Pueblo = Terceros + "/Kenney_FantasyTown";
        kit.bateria = Uno(Espacio, "machine_barrel");
        kit.maquina = Uno(Espacio, "machine_generator");
        kit.antenaMaquina = Uno(Espacio, "machine_wireless");
        kit.pantalla = Uno(Espacio, "desk_computerScreen");
        kit.pilarPiedra = Uno(Pueblo, "pillar-stone");
        kit.pilarMadera = Uno(Pueblo, "pillar-wood");
        kit.farol = Uno(Pueblo, "lantern");
        kit.estandarte = Uno(Pueblo, "banner-green");
        kit.jardinera = Uno(Pueblo, "fountain-round");

        kit.cobre = Metal("RELEVO_KitCobre", Hex("B06A3A"), 0.35f, 0.45f);
        kit.ambarMate = Lit("RELEVO_KitAmbarMate", Hex("E9A23B"), null, 0.4f);
        kit.ambarBrillante = Emisivo("RELEVO_KitAmbar", Hex("F2B544"), 1.5f);
        kit.verdeMaia = Lit("RELEVO_KitVerdeMaia", Hex("6FA06A"), null, 0.35f);
        kit.pantallaMaia = Emisivo("RELEVO_KitPantallaMaia", Hex("163C35"), 0.6f);
        kit.oscuro = Lit("RELEVO_KitOscuro", Hex("2A3340"), null, 0.4f);
        kit.piedraPueblo = Lit("RELEVO_PiedraPueblo", Hex("FFF0D8"),
            AssetDatabase.LoadAssetAtPath<Texture2D>(Pueblo + "/Textures/colormap.png"), 0.15f);

        kit.niloModelo = AssetDatabase.LoadAssetAtPath<GameObject>(Robot);
        kit.niloAnimador = animador;
        kit.niloEscala = 0.4f;

        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();

        return "Kit preparado: " + remapeados + " materiales reasignados.";
    }

    // ------------------------------------------------------------------ materiales

    static Material MaterialAgua(string nombre, Color somero, Color profundo, float transicion,
        float espuma, float escala, float opacidad)
    {
        Material m = Obtener(nombre, Shader.Find("RELEVO/Agua"));
        m.SetColor("_ColorSomero", somero);
        m.SetColor("_ColorProfundo", profundo);
        m.SetColor("_ColorEspuma", Hex("F4F1E6"));
        m.SetFloat("_Profundidad", transicion);
        m.SetFloat("_AnchoEspuma", espuma);
        m.SetFloat("_EscalaOndas", escala);
        m.SetFloat("_Opacidad", opacidad);
        m.SetFloat("_Destellos", 0.8f);
        return m;
    }

    static Material Follaje(string textura, Color tinte, float viento, float alturaReferencia, float recorte)
    {
        Material m = Obtener("RELEVO_Follaje_" + textura, Shader.Find("RELEVO/Follaje"));
        m.SetTexture("_BaseMap", TexturaNaturaleza(textura));
        m.SetColor("_BaseColor", tinte);
        m.SetFloat("_Viento", viento);
        m.SetFloat("_AlturaReferencia", alturaReferencia);
        m.SetFloat("_Cutoff", recorte);
        m.enableInstancing = true;
        return m;
    }

    static Material Lit(string nombre, Color color, Texture textura, float brillo)
    {
        Material m = Obtener(nombre, Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        m.SetTexture("_BaseMap", textura);
        m.SetFloat("_Smoothness", brillo);
        m.SetFloat("_Metallic", 0f);
        m.enableInstancing = true;
        return m;
    }

    /// <summary>
    /// Material de partícula con un punto suave y redondo (textura generada aquí mismo): el
    /// material de partícula por defecto dibuja cuadrados, que leen como píxeles sueltos.
    /// </summary>
    static Material MaterialParticula()
    {
        string rutaTextura = CarpetaMateriales + "/RELEVO_PuntoSuave.png";
        if (!File.Exists(rutaTextura))
        {
            const int lado = 64;
            Texture2D punto = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    float dx = (x + 0.5f) / lado * 2f - 1f;
                    float dy = (y + 0.5f) / lado * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    punto.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            File.WriteAllBytes(rutaTextura, punto.EncodeToPNG());
            Object.DestroyImmediate(punto);
            AssetDatabase.ImportAsset(rutaTextura);
        }

        TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(rutaTextura);
        if (!importador.alphaIsTransparency)
        {
            importador.alphaIsTransparency = true;
            importador.SaveAndReimport();
        }

        Material m = Obtener("RELEVO_ParticulaSuave", Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTextura));
        m.SetColor("_BaseColor", Color.white);
        // Transparente con mezcla alfa: el polen tiene que verse sobre el agua clara y la oscura.
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }

    static Material Metal(string nombre, Color color, float metalico, float brillo)
    {
        Material m = Lit(nombre, color, null, brillo);
        m.SetFloat("_Metallic", metalico);
        return m;
    }

    static Material Emisivo(string nombre, Color color, float intensidad)
    {
        Material m = Lit(nombre, color, null, 0.5f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * intensidad);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return m;
    }

    static Material Obtener(string nombre, Shader shader)
    {
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        else if (m.shader != shader)
        {
            m.shader = shader;
        }

        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D TexturaNaturaleza(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + "/" + nombre + ".png");
    }

    /// <summary>
    /// Las texturas de hojas y hierba de Quaternius guardan color rojo en sus zonas transparentes.
    /// De cerca no se nota, pero la cámara del juego ve las copas pequeñas y lee los mipmaps, donde
    /// ese rojo se mezcla con el verde: los árboles salían rojos. 'alphaIsTransparency' rellena el
    /// color de las zonas transparentes con el de sus vecinas opacas, y 'preservar cobertura' evita
    /// que las copas se vuelvan ralas al alejarse con recorte por alfa.
    /// </summary>
    static void ConfigurarTexturasDeFollaje()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Texturas }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null || !importador.DoesSourceTextureHaveAlpha()) continue;
            if (importador.alphaIsTransparency && importador.mipMapsPreserveCoverage) continue;

            importador.alphaIsTransparency = true;
            importador.mipMapsPreserveCoverage = true;
            importador.alphaTestReferenceValue = 0.5f;
            importador.SaveAndReimport();
        }
    }

    /// <summary>
    /// Sprites del HUD (Kenney UI Pack e Input Prompts) en Resources/UI. Los paneles llevan bordes
    /// de 9-slice para estirarse sin deformar las esquinas redondeadas; el borde inferior es más
    /// alto porque ahí está el relieve del botón.
    /// </summary>
    static void ConfigurarSpritesUI()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/UI" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null) continue;

            Vector4 borde = Vector4.zero;
            if (ruta.Contains("/Paneles/panel_")) borde = new Vector4(24f, 34f, 24f, 24f);
            else if (ruta.Contains("/Paneles/marco")) borde = new Vector4(24f, 24f, 24f, 24f);

            bool cambia = importador.textureType != TextureImporterType.Sprite
                || importador.spriteBorder != borde
                || importador.mipmapEnabled;
            if (!cambia) continue;

            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.spriteBorder = borde;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------ modelos

    static int Remapear(string rutaModelo, Dictionary<string, Material> mapa)
    {
        ModelImporter importador = AssetImporter.GetAtPath(rutaModelo) as ModelImporter;
        if (importador == null) return 0;

        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
        HashSet<string> nombres = new HashSet<string>();
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m != null) nombres.Add(m.name);
            }
        }

        // Los nombres de material ya remapeados son los nuestros: se recuperan los originales del
        // importador para que una segunda pasada no los pierda.
        foreach (var par in importador.GetExternalObjectMap())
        {
            if (par.Key.type == typeof(Material)) nombres.Add(par.Key.name);
        }

        int cambios = 0;
        foreach (string nombre in nombres)
        {
            Material destino;
            if (!mapa.TryGetValue(nombre, out destino) || destino == null) continue;

            importador.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), nombre), destino);
            cambios++;
        }

        if (cambios > 0) importador.SaveAndReimport();
        return cambios;
    }

    static IEnumerable<string> Modelos(string carpeta)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { carpeta }))
        {
            yield return AssetDatabase.GUIDToAssetPath(guid);
        }
    }

    static GameObject Uno(string carpeta, string nombre)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(carpeta + "/" + nombre + ".fbx");
        if (go == null) Debug.LogWarning("RELEVO kit: no se encontró " + carpeta + "/" + nombre + ".fbx");
        return go;
    }

    static GameObject[] Cargar(string carpeta, params string[] nombres)
    {
        List<GameObject> lista = new List<GameObject>();
        foreach (string n in nombres)
        {
            GameObject go = Uno(carpeta, n);
            if (go != null) lista.Add(go);
        }
        return lista.ToArray();
    }

    // ------------------------------------------------------------------ animación de Nilo

    static readonly string[] ClipsEnBucle = { "Robot_Idle", "Robot_Walking", "Robot_Running", "Robot_Dance" };

    static void ConfigurarAnimacionesRobot()
    {
        ModelImporter importador = (ModelImporter)AssetImporter.GetAtPath(Robot);
        importador.animationType = ModelImporterAnimationType.Generic;

        ModelImporterClipAnimation[] clips = importador.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importador.defaultClipAnimations;

        for (int i = 0; i < clips.Length; i++)
        {
            bool bucle = false;
            foreach (string n in ClipsEnBucle)
            {
                if (clips[i].name.EndsWith(n)) bucle = true;
            }

            clips[i].loopTime = bucle;
            clips[i].loopPose = bucle;
            clips[i].lockRootHeightY = true;
            clips[i].lockRootPositionXZ = true;
            clips[i].lockRootRotation = true;
            clips[i].keepOriginalPositionY = true;
            clips[i].keepOriginalPositionXZ = true;
            clips[i].keepOriginalOrientation = true;
        }

        importador.clipAnimations = clips;
        importador.SaveAndReimport();
    }

    static AnimationClip Clip(string sufijo)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Robot))
        {
            AnimationClip c = o as AnimationClip;
            if (c != null && !c.name.StartsWith("__preview") && c.name.EndsWith(sufijo)) return c;
        }
        Debug.LogWarning("RELEVO kit: falta el clip " + sufijo);
        return null;
    }

    /// <summary>
    /// Locomoción en un árbol de mezcla por velocidad (reposo, paso, carrera) y seis gestos que se
    /// disparan desde AnimadorNiloModelo. Cada gesto vuelve a la locomoción al terminar y se
    /// interrumpe si Nilo echa a andar, para que el control nunca se sienta pegajoso.
    /// </summary>
    static AnimatorController CrearAnimadorNilo()
    {
        string ruta = CarpetaAnimacion + "/Nilo.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta) != null) AssetDatabase.DeleteAsset(ruta);

        AnimatorController controlador = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        controlador.AddParameter("Velocidad", AnimatorControllerParameterType.Float);

        AnimatorStateMachine maquina = controlador.layers[0].stateMachine;

        BlendTree arbol;
        AnimatorState locomocion = controlador.CreateBlendTreeInController("Locomocion", out arbol, 0);
        arbol.blendType = BlendTreeType.Simple1D;
        arbol.blendParameter = "Velocidad";
        arbol.useAutomaticThresholds = false;
        arbol.AddChild(Clip("Robot_Idle"), 0f);
        arbol.AddChild(Clip("Robot_Walking"), 2.2f);
        arbol.AddChild(Clip("Robot_Running"), 5.5f);
        maquina.defaultState = locomocion;

        Gesto(controlador, maquina, locomocion, "Si", "Robot_Yes", 0.85f);
        Gesto(controlador, maquina, locomocion, "No", "Robot_No", 0.85f);
        Gesto(controlador, maquina, locomocion, "Pulgar", "Robot_ThumbsUp", 0.85f);
        Gesto(controlador, maquina, locomocion, "Golpe", "Robot_Punch", 0.8f);
        Gesto(controlador, maquina, locomocion, "Saludo", "Robot_Wave", 0.85f);
        Gesto(controlador, maquina, locomocion, "Celebrar", "Robot_Dance", 0.95f);

        AssetDatabase.SaveAssets();
        return controlador;
    }

    static void Gesto(AnimatorController controlador, AnimatorStateMachine maquina, AnimatorState locomocion,
        string disparador, string clip, float salida)
    {
        controlador.AddParameter(disparador, AnimatorControllerParameterType.Trigger);

        AnimatorState estado = maquina.AddState(disparador);
        estado.motion = Clip(clip);

        AnimatorStateTransition entrada = maquina.AddAnyStateTransition(estado);
        entrada.AddCondition(AnimatorConditionMode.If, 0f, disparador);
        entrada.duration = 0.12f;
        entrada.canTransitionToSelf = false;

        AnimatorStateTransition alTerminar = estado.AddTransition(locomocion);
        alTerminar.hasExitTime = true;
        alTerminar.exitTime = salida;
        alTerminar.duration = 0.2f;

        AnimatorStateTransition alAndar = estado.AddTransition(locomocion);
        alAndar.hasExitTime = false;
        alAndar.AddCondition(AnimatorConditionMode.Greater, 1.2f, "Velocidad");
        alAndar.duration = 0.15f;
    }

    static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }
}
