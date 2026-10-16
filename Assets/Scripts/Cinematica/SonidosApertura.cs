using UnityEngine;

/// <summary>
/// Sonido de la cinemática de apertura, generado en ejecución: no hay ni un archivo de audio.
///
///  - La canción provisional: una caja de música en La menor, a tres tiempos, mientras se compone
///    la definitiva. Es corta (16 compases) y se repite. La cinemática la pasa por filtros de
///    Unity para que suene a radio lejana y para deformarla durante el apagón.
///  - Estática de radio, rumor de agua con goteo, zumbido eléctrico, pitidos de terminal, el
///    "clonc" de un relé al cortar la luz y un golpe grave para el título.
///
/// Igual que AudioDirector, todo son puntos de reemplazo: cuando exista la canción de verdad basta
/// con asignar su clip en Apertura y esta clase deja de usarse para la música.
/// </summary>
public static class SonidosApertura
{
    const int Muestreo = 44100;

    /// <summary>Tempo de la canción provisional (negras por minuto).</summary>
    public const float Pulsos = 84f;
    public static float DuracionPulso { get { return 60f / Pulsos; } }

    // Melodía: (nota MIDI, pulsos). 0 es silencio. 16 compases de 3/4.
    static readonly int[,] Melodia =
    {
        { 69, 1 }, { 72, 1 }, { 76, 1 },     // La Do Mi
        { 74, 2 }, { 72, 1 },                // Re · Do
        { 71, 1 }, { 67, 1 }, { 71, 1 },     // Si Sol Si
        { 72, 3 },                           // Do
        { 69, 1 }, { 72, 1 }, { 76, 1 },     // La Do Mi
        { 79, 2 }, { 77, 1 },                // Sol · Fa
        { 76, 1 }, { 74, 1 }, { 71, 1 },     // Mi Re Si
        { 69, 3 },                           // La
        { 77, 2 }, { 76, 1 },                // Fa · Mi
        { 74, 1 }, { 72, 1 }, { 74, 1 },     // Re Do Re
        { 76, 2 }, { 72, 1 },                // Mi · Do
        { 69, 3 },                           // La
        { 65, 1 }, { 69, 1 }, { 72, 1 },     // Fa La Do
        { 71, 2 }, { 68, 1 },                // Si · Sol#
        { 69, 3 },                           // La
        { 0, 3 },                            // (respira)
    };

    // Un acorde por compás, como tres notas MIDI graves.
    static readonly int[,] Acordes =
    {
        { 45, 52, 57 }, { 50, 57, 62 }, { 43, 50, 55 }, { 48, 55, 60 },
        { 45, 52, 57 }, { 48, 55, 60 }, { 40, 47, 52 }, { 45, 52, 57 },
        { 41, 48, 53 }, { 50, 57, 62 }, { 45, 52, 57 }, { 45, 52, 57 },
        { 41, 48, 53 }, { 40, 47, 56 }, { 45, 52, 57 }, { 45, 52, 57 },
    };

    static float Hz(int midi) { return 440f * Mathf.Pow(2f, (midi - 69) / 12f); }

    /// <summary>La canción provisional, 16 compases (unos 34 s a 84 negras por minuto).</summary>
    public static AudioClip Cancion()
    {
        float pulso = DuracionPulso;
        int compases = Acordes.GetLength(0);
        float duracion = compases * 3f * pulso + 2.5f;
        int n = Mathf.CeilToInt(duracion * Muestreo);
        float[] datos = new float[n];

        // Caja de música: senoidal con armónicos inarmónicos de campanilla y caída exponencial.
        float t0 = 0f;
        for (int i = 0; i < Melodia.GetLength(0); i++)
        {
            int nota = Melodia[i, 0];
            float largo = Melodia[i, 1] * pulso;
            if (nota > 0) Campanilla(datos, t0, Hz(nota), 2.6f, 0.34f);
            t0 += largo;
        }

        // Colchón: el acorde de cada compás, muy suave, y un bajo en la primera parte.
        for (int c = 0; c < compases; c++)
        {
            float inicio = c * 3f * pulso;
            for (int k = 0; k < 3; k++) Colchon(datos, inicio, 3f * pulso + 0.4f, Hz(Acordes[c, k]), 0.035f);
            Campanilla(datos, inicio, Hz(Acordes[c, 0] - 12), 1.8f, 0.12f);
        }

        Normalizar(datos, 0.82f);
        return Clip("cancion_provisional", datos);
    }

    static void Campanilla(float[] datos, float inicio, float frecuencia, float caida, float volumen)
    {
        int a = Mathf.FloorToInt(inicio * Muestreo);
        int largo = Mathf.Min(datos.Length - a, Mathf.CeilToInt(caida * 1.6f * Muestreo));
        for (int i = 0; i < largo; i++)
        {
            float t = i / (float)Muestreo;
            float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * 3.2f / caida * 2f);
            float w = 2f * Mathf.PI * frecuencia * t;
            float s = Mathf.Sin(w) + 0.35f * Mathf.Sin(w * 2.76f) * Mathf.Exp(-t * 6f) + 0.18f * Mathf.Sin(w * 5.4f) * Mathf.Exp(-t * 12f);
            datos[a + i] += s * env * volumen;
        }
    }

    static void Colchon(float[] datos, float inicio, float duracion, float frecuencia, float volumen)
    {
        int a = Mathf.FloorToInt(inicio * Muestreo);
        int largo = Mathf.Min(datos.Length - a, Mathf.CeilToInt(duracion * Muestreo));
        for (int i = 0; i < largo; i++)
        {
            float t = i / (float)Muestreo;
            float p = i / (float)largo;
            float env = Mathf.Sin(Mathf.PI * p);
            float w = 2f * Mathf.PI * frecuencia * t;
            datos[a + i] += (Mathf.Sin(w) + 0.3f * Mathf.Sin(w * 2f + 0.4f * Mathf.Sin(t * 3f))) * env * volumen;
        }
    }

    /// <summary>Estática de radio: soplido filtrado con chasquidos sueltos. Se repite sin costura.</summary>
    public static AudioClip Estatica()
    {
        int n = Muestreo * 4;
        float[] datos = new float[n];
        System.Random azar = new System.Random(11);
        float pb = 0f;
        for (int i = 0; i < n; i++)
        {
            float r = (float)azar.NextDouble() * 2f - 1f;
            pb += (r - pb) * 0.35f;              // pasa bajos
            float t = i / (float)Muestreo;
            float ola = 0.75f + 0.25f * Mathf.Sin(t * 2f * Mathf.PI * 0.5f) * Mathf.Sin(t * 7.3f);
            float s = (pb * 0.8f + r * 0.2f) * 0.35f * ola;
            if (azar.NextDouble() < 0.0009) s += (float)(azar.NextDouble() * 2.0 - 1.0) * 0.9f;
            datos[i] = s;
        }
        Fundir(datos, 0.05f);
        return Clip("estatica", datos);
    }

    /// <summary>Rumor de agua quieta en un cuarto cerrado, con gotas que caen de vez en cuando.</summary>
    public static AudioClip Agua()
    {
        int n = Muestreo * 8;
        float[] datos = new float[n];
        System.Random azar = new System.Random(5);
        float pb = 0f;
        for (int i = 0; i < n; i++)
        {
            float r = (float)azar.NextDouble() * 2f - 1f;
            pb += (r - pb) * 0.02f;
            float t = i / (float)Muestreo;
            datos[i] = pb * 0.9f * (0.7f + 0.3f * Mathf.Sin(t * 0.8f));
        }
        float[] gotas = { 0.6f, 2.1f, 2.45f, 4.0f, 5.3f, 6.9f };
        foreach (float g in gotas) Gota(datos, g, 900f + g * 70f);
        Fundir(datos, 0.3f);
        Normalizar(datos, 0.5f);
        return Clip("agua", datos);
    }

    static void Gota(float[] datos, float inicio, float frecuencia)
    {
        int a = Mathf.FloorToInt(inicio * Muestreo);
        int largo = Mathf.Min(datos.Length - a, Muestreo / 6);
        for (int i = 0; i < largo; i++)
        {
            float t = i / (float)Muestreo;
            float f = frecuencia * (1f + 1.4f * Mathf.Exp(-t * 60f));
            datos[a + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 40f) * 0.5f;
        }
    }

    /// <summary>Zumbido eléctrico: la red de la ciudad, viva. Se repite sin costura.</summary>
    public static AudioClip Zumbido()
    {
        int n = Muestreo * 2;
        float[] datos = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Muestreo;
            float w = 2f * Mathf.PI * 50f * t;
            datos[i] = (Mathf.Sin(w) * 0.5f + Mathf.Sin(w * 2f) * 0.3f + Mathf.Sin(w * 3f) * 0.15f) * 0.25f;
        }
        return Clip("zumbido", datos);
    }

    /// <summary>Pitido de terminal al escribir una línea.</summary>
    public static AudioClip Pitido(float frecuencia)
    {
        int n = Muestreo / 14;
        float[] datos = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Muestreo;
            float env = Mathf.Min(1f, t * 300f) * Mathf.Exp(-t * 30f);
            datos[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frecuencia * t)) * 0.18f * env;
        }
        return Clip("pitido", datos);
    }

    /// <summary>Un relé que salta y corta la luz: golpe seco y chisporroteo.</summary>
    public static AudioClip Rele()
    {
        int n = Mathf.CeilToInt(Muestreo * 0.6f);
        float[] datos = new float[n];
        System.Random azar = new System.Random(3);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Muestreo;
            float golpe = Mathf.Sin(2f * Mathf.PI * (90f - 40f * t) * t) * Mathf.Exp(-t * 18f);
            float chispa = ((float)azar.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 9f) * (azar.NextDouble() < 0.25 ? 1f : 0.15f);
            datos[i] = golpe * 0.8f + chispa * 0.35f;
        }
        return Clip("rele", datos);
    }

    /// <summary>Golpe grave y largo para el título.</summary>
    public static AudioClip Golpe()
    {
        int n = Muestreo * 4;
        float[] datos = new float[n];
        System.Random azar = new System.Random(9);
        float pb = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Muestreo;
            float r = (float)azar.NextDouble() * 2f - 1f;
            pb += (r - pb) * 0.01f;
            float tono = Mathf.Sin(2f * Mathf.PI * (48f - 8f * Mathf.Min(1f, t)) * t);
            datos[i] = (tono * 0.8f + pb * 2.5f) * Mathf.Min(1f, t * 60f) * Mathf.Exp(-t * 0.9f);
        }
        Normalizar(datos, 0.9f);
        return Clip("golpe", datos);
    }

    /// <summary>Arranque de Nilo: un barrido de grave a agudo, como un motor que despierta.</summary>
    public static AudioClip Arranque()
    {
        int n = Mathf.CeilToInt(Muestreo * 1.6f);
        float[] datos = new float[n];
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float p = i / (float)n;
            float f = Mathf.Lerp(60f, 520f, p * p);
            fase += 2f * Mathf.PI * f / Muestreo;
            float env = Mathf.Sin(Mathf.PI * p) * (0.6f + 0.4f * p);
            datos[i] = (Mathf.Sin(fase) + 0.25f * Mathf.Sin(fase * 2f)) * env * 0.35f;
        }
        return Clip("arranque", datos);
    }

    // ------------------------------------------------------------------ utilidades

    static void Normalizar(float[] datos, float pico)
    {
        float max = 0.0001f;
        for (int i = 0; i < datos.Length; i++) max = Mathf.Max(max, Mathf.Abs(datos[i]));
        float k = pico / max;
        for (int i = 0; i < datos.Length; i++) datos[i] *= k;
    }

    /// <summary>Funde principio y final para que el bucle no haga clic.</summary>
    static void Fundir(float[] datos, float segundos)
    {
        int m = Mathf.Min(datos.Length / 2, Mathf.CeilToInt(segundos * Muestreo));
        for (int i = 0; i < m; i++)
        {
            float k = i / (float)m;
            datos[i] *= k;
            datos[datos.Length - 1 - i] *= k;
        }
    }

    static AudioClip Clip(string nombre, float[] datos)
    {
        AudioClip clip = AudioClip.Create(nombre, datos.Length, 1, Muestreo, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
