using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Condensador de una base de energía (Faro de Liria). Mientras la base tiene la batería se carga;
/// al retirarla, sostiene el puente de la base hasta descargarse y entonces lo repliega. Es la única
/// forma de cruzar un puente eléctrico llevando la batería, así que convierte cada cruce en una
/// carrera: cargar del todo, retirar y correr.
///
/// La carga se lee en las ocho barras de la torre: ámbar las llenas, cobre las vacías, y la última
/// parpadea con un pitido cuando queda poca. Si el puente se repliega con Nilo encima, cae al agua
/// (TemporaryBridge se encarga).
/// </summary>
public sealed class Condensador : MonoBehaviour
{
    public const float SegundosCarga = 3f;      // de vacío a lleno con la batería puesta
    public const float SegundosSosten = 3.2f;   // lo que aguanta el puente con la carga llena

    [SerializeField] EnergySocket baseEnergia;
    [SerializeField] List<Renderer> barras = new List<Renderer>();
    [SerializeField] Light luz;

    public float Carga { get; private set; }          // 0 a 1
    public bool Sosteniendo { get; private set; }     // la base ya no tiene batería y el puente aguanta

    float relojPitido;
    int encendidasPintadas = -1;
    bool parpadeoPintado;

    public void Configurar(EnergySocket baseEnergia, List<Renderer> barras, Light luz)
    {
        this.baseEnergia = baseEnergia;
        this.barras = barras;
        this.luz = luz;
        Vincular();
    }

    public EnergySocket Base { get { return baseEnergia; } }

    /// <summary>Vacío y en reposo; también al cargar una escena horneada.</summary>
    public void Vincular()
    {
        Carga = 0f;
        Sosteniendo = false;
        encendidasPintadas = -1;
        Pintar(false);
        enabled = true;
    }

    /// <summary>
    /// Lo llama la base al retirar la batería. Devuelve verdadero si el condensador se queda
    /// sosteniendo el puente (y por tanto la base no debe replegarlo).
    /// </summary>
    public bool AlRetirarBateria()
    {
        if (Carga <= 0.02f) { Carga = 0f; return false; }
        Sosteniendo = true;
        AudioDirector.Pedir(Tono.Mensaje);
        return true;
    }

    /// <summary>La batería vuelve a la base: deja de descargarse y vuelve a cargarse.</summary>
    public void AlInsertarBateria()
    {
        Sosteniendo = false;
    }

    void Update()
    {
        if (baseEnergia == null) return;

        if (baseEnergia.TieneBateria)
        {
            float antes = Carga;
            Carga = Mathf.Min(1f, Carga + Time.deltaTime / SegundosCarga);
            if (antes < 1f && Carga >= 1f) AudioDirector.Pedir(Tono.Insertar);
        }
        else if (Sosteniendo)
        {
            Carga = Mathf.Max(0f, Carga - Time.deltaTime / SegundosSosten);

            if (Carga < 0.35f)
            {
                relojPitido -= Time.deltaTime;
                if (relojPitido <= 0f)
                {
                    relojPitido = Mathf.Lerp(0.2f, 0.5f, Carga / 0.35f);
                    AudioDirector.Pedir(Tono.Alerta);
                }
            }

            if (Carga <= 0f)
            {
                Sosteniendo = false;
                if (baseEnergia.Puente != null && !baseEnergia.TieneBateria) baseEnergia.Puente.Activar(false);
                baseEnergia.PintarCondensadorVacio();
            }
        }

        Pintar(Sosteniendo && Carga < 0.35f);

        if (luz != null)
        {
            float objetivo = Carga * (Sosteniendo ? 2.6f : 1.6f);
            luz.intensity = Mathf.Lerp(luz.intensity, objetivo, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }

    void Pintar(bool parpadeo)
    {
        if (barras == null || barras.Count == 0) return;
        int encendidas = Mathf.CeilToInt(Carga * barras.Count - 0.001f);
        bool apagarUltima = parpadeo && Mathf.Repeat(Time.time * 6f, 1f) < 0.5f;
        if (encendidas == encendidasPintadas && apagarUltima == parpadeoPintado) return;
        encendidasPintadas = encendidas;
        parpadeoPintado = apagarUltima;

        Material ambar = RelevoPalette.MaterialDe(ClaveMaterial.Ambar);
        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        for (int i = 0; i < barras.Count; i++)
        {
            if (barras[i] == null) continue;
            bool on = i < encendidas && !(apagarUltima && i == encendidas - 1);
            barras[i].sharedMaterial = on ? ambar : cobre;
        }
    }
}
