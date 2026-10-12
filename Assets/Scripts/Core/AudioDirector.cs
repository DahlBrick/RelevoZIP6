using System.Collections.Generic;
using UnityEngine;

/// <summary>Identificadores de los avisos sonoros del juego.</summary>
public enum Tono
{
    Recoger,
    Insertar,
    Retirar,
    PuenteActivo,
    PuenteRetraido,
    Palanca,
    Recuerdo,
    Salida,
    Faro,
    Bloqueado,
    Mensaje,
    Empujar,
    PlacaPisada,
    PlacaSuelta,
    CompuertaAbre,
    CompuertaCierra,
    Alerta,
    Apagado
}

/// <summary>
/// Audio placeholder. Si los campos de clip están vacíos genera tonos simples
/// en tiempo de ejecución, así el proyecto nunca se bloquea por falta de sonido.
/// Los campos serializados son los puntos claros de reemplazo.
/// </summary>
public class AudioDirector : MonoBehaviour
{
    public static AudioDirector Instancia;

    [Header("Puntos de reemplazo (opcional: asigna clips reales)")]
    [SerializeField] AudioClip clipRecoger;
    [SerializeField] AudioClip clipInsertar;
    [SerializeField] AudioClip clipRetirar;
    [SerializeField] AudioClip clipPuenteActivo;
    [SerializeField] AudioClip clipPuenteRetraido;
    [SerializeField] AudioClip clipPalanca;
    [SerializeField] AudioClip clipRecuerdo;
    [SerializeField] AudioClip clipSalida;
    [SerializeField] AudioClip clipFaro;
    [SerializeField] AudioClip clipBloqueado;
    [SerializeField] AudioClip clipMensaje;

    [SerializeField, Range(0f, 1f)] float volumen = 0.45f;

    AudioSource fuente;
    readonly Dictionary<Tono, AudioClip> tonosGenerados = new Dictionary<Tono, AudioClip>();

    void Awake()
    {
        Instancia = this;
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        fuente.volume = volumen;
        GenerarTonos();
        GameEvents.SonidoSolicitado += Reproducir;
    }

    void OnDestroy()
    {
        GameEvents.SonidoSolicitado -= Reproducir;
        if (Instancia == this) Instancia = null;
    }

    public static void Pedir(Tono tono)
    {
        GameEvents.RaiseSonidoSolicitado(tono);
    }

    void Reproducir(Tono tono)
    {
        AudioClip clip = ObtenerClip(tono);
        if (clip != null && fuente != null) fuente.PlayOneShot(clip);
    }

    AudioClip ObtenerClip(Tono tono)
    {
        switch (tono)
        {
            case Tono.Recoger: if (clipRecoger != null) return clipRecoger; break;
            case Tono.Insertar: if (clipInsertar != null) return clipInsertar; break;
            case Tono.Retirar: if (clipRetirar != null) return clipRetirar; break;
            case Tono.PuenteActivo: if (clipPuenteActivo != null) return clipPuenteActivo; break;
            case Tono.PuenteRetraido: if (clipPuenteRetraido != null) return clipPuenteRetraido; break;
            case Tono.Palanca: if (clipPalanca != null) return clipPalanca; break;
            case Tono.Recuerdo: if (clipRecuerdo != null) return clipRecuerdo; break;
            case Tono.Salida: if (clipSalida != null) return clipSalida; break;
            case Tono.Faro: if (clipFaro != null) return clipFaro; break;
            case Tono.Bloqueado: if (clipBloqueado != null) return clipBloqueado; break;
            case Tono.Mensaje: if (clipMensaje != null) return clipMensaje; break;
        }

        AudioClip generado;
        return tonosGenerados.TryGetValue(tono, out generado) ? generado : null;
    }

    void GenerarTonos()
    {
        tonosGenerados[Tono.Recoger] = GenerarTono("tono_recoger", 587f, 0.12f, 0.5f, 1.5f);
        tonosGenerados[Tono.Insertar] = GenerarTono("tono_insertar", 392f, 0.22f, 0.5f, 1.0f);
        tonosGenerados[Tono.Retirar] = GenerarTono("tono_retirar", 294f, 0.18f, 0.45f, 1.0f);
        tonosGenerados[Tono.PuenteActivo] = GenerarTono("tono_puente_activo", 494f, 0.30f, 0.45f, 2.0f);
        tonosGenerados[Tono.PuenteRetraido] = GenerarTono("tono_puente_retraido", 220f, 0.30f, 0.35f, 1.0f);
        tonosGenerados[Tono.Palanca] = GenerarTono("tono_palanca", 330f, 0.26f, 0.5f, 2.5f);
        tonosGenerados[Tono.Recuerdo] = GenerarTono("tono_recuerdo", 880f, 0.35f, 0.4f, 1.5f);
        tonosGenerados[Tono.Salida] = GenerarTono("tono_salida", 523f, 0.40f, 0.45f, 2.0f);
        tonosGenerados[Tono.Faro] = GenerarTono("tono_faro", 174f, 1.20f, 0.5f, 2.0f);
        tonosGenerados[Tono.Bloqueado] = GenerarTono("tono_bloqueado", 160f, 0.16f, 0.4f, 1.0f);
        tonosGenerados[Tono.Mensaje] = GenerarTono("tono_mensaje", 700f, 0.10f, 0.28f, 1.0f);
        tonosGenerados[Tono.Empujar] = GenerarBarrido("tono_empujar", 140f, 95f, 0.22f, 0.35f, true);
        tonosGenerados[Tono.PlacaPisada] = GenerarTono("tono_placa_pisada", 415f, 0.16f, 0.4f, 2.0f);
        tonosGenerados[Tono.PlacaSuelta] = GenerarTono("tono_placa_suelta", 262f, 0.14f, 0.32f, 2.0f);
        tonosGenerados[Tono.CompuertaAbre] = GenerarBarrido("tono_compuerta_abre", 180f, 360f, 0.4f, 0.32f, false);
        tonosGenerados[Tono.CompuertaCierra] = GenerarBarrido("tono_compuerta_cierra", 360f, 160f, 0.35f, 0.3f, false);
        tonosGenerados[Tono.Alerta] = GenerarTono("tono_alerta", 988f, 0.12f, 0.3f, 1.5f);
        tonosGenerados[Tono.Apagado] = GenerarBarrido("tono_apagado", 620f, 55f, 1.3f, 0.5f, false);
    }

    /// <summary>
    /// Barrido de frecuencia: el "apagado" de un robot que se queda sin energía (de agudo a grave,
    /// cada vez más lento) o el roce de una caja (con ruido).
    /// </summary>
    static AudioClip GenerarBarrido(string nombre, float desde, float hasta, float duracion, float volumen, bool ruido)
    {
        const int frecuenciaMuestreo = 44100;
        int muestras = Mathf.Max(256, Mathf.RoundToInt(frecuenciaMuestreo * duracion));
        float[] datos = new float[muestras];
        float fase = 0f;
        System.Random azar = new System.Random(7);

        for (int i = 0; i < muestras; i++)
        {
            float progreso = (float)i / muestras;
            float frecuencia = Mathf.Lerp(desde, hasta, 1f - (1f - progreso) * (1f - progreso));
            fase += 2f * Mathf.PI * frecuencia / frecuenciaMuestreo;
            float envolvente = Mathf.Clamp01(progreso * 25f) * (1f - progreso);
            float onda = Mathf.Sin(fase) + 0.3f * Mathf.Sign(Mathf.Sin(fase * 0.5f));
            if (ruido) onda = onda * 0.4f + ((float)azar.NextDouble() * 2f - 1f) * 0.6f;
            datos[i] = Mathf.Clamp(onda * envolvente * volumen, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(nombre, muestras, 1, frecuenciaMuestreo, false);
        clip.SetData(datos, 0);
        return clip;
    }

    /// <summary>Genera un tono con envolvente de ataque/decadencia y un armónico suave.</summary>
    static AudioClip GenerarTono(string nombre, float frecuencia, float duracion, float volumen, float armonico)
    {
        const int frecuenciaMuestreo = 44100;
        int muestras = Mathf.Max(256, Mathf.RoundToInt(frecuenciaMuestreo * duracion));
        float[] datos = new float[muestras];
        float fase = 0f;

        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / frecuenciaMuestreo;
            float progreso = (float)i / muestras;
            float envolvente = Mathf.Sin(Mathf.PI * Mathf.Clamp01(progreso)) * Mathf.Exp(-3.2f * progreso);
            fase += 2f * Mathf.PI * frecuencia / frecuenciaMuestreo;
            float onda = Mathf.Sin(fase) + 0.18f * Mathf.Sin(fase * armonico);
            datos[i] = Mathf.Clamp(onda * envolvente * volumen, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(nombre, muestras, 1, frecuenciaMuestreo, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
