using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fondo vivo de la pantalla de título: el muelle del Puerto 1 de noche, Nilo junto a la baranda con
/// la radio de Maia y, al otro lado del agua, la Liria real (los bloques de Liria_Prueba). Cada
/// puerto restaurado enciende una tanda de ventanas; la baranda lleva una raya por puerto.
/// No termina nunca: TituloRelevo cambia de escena por su cuenta.
/// </summary>
public sealed class FondoTitulo : CinematicaBase
{
    static readonly Vector3 PosLiria = new Vector3(-6f, 0f, 92f);
    static readonly Vector3 PosNilo = new Vector3(0.7f, 0f, 4.3f);

    PlayerController nilo;
    Material piloto;
    List<Material> ventanas;
    readonly List<float> encendido = new List<float>();

    protected override float Fin { get { return float.MaxValue; } }
    protected override string Destino() { return TituloRelevo.Escena; }
    protected override void Saltar() { }

    protected override void Construir()
    {
        aviso.text = "";
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("Banda")) t.gameObject.SetActive(false);

        int puertos = 0;
        for (int i = 1; i <= LevelLayouts.Cantidad; i++) if (SaveManager.NivelCompletado(i)) puertos++;

        Transform antena;
        Muelle(Vector3.zero, puertos, out piloto, out antena);
        nilo = CrearNilo(transform, PosNilo, 0f);

        List<Vector3> centros;
        ventanas = Ciudad(Raiz("Liria", PosLiria), LevelLayouts.Cantidad, 3, out centros);
        // Los barrios de los puertos restaurados se encienden uno tras otro al abrir el título.
        for (int i = 0; i < ventanas.Count; i++) encendido.Add(i < puertos ? 1.2f + i * 0.25f : -1f);

        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 220f;
    }

    protected override void Evaluar(float previo, float t, float dt)
    {
        Poner(nilo, PosNilo, 0f);
        Luna(1.25f, new Color(0.66f, 0.74f, 1f), new Vector3(32f, 200f, 0f));
        Fundido(1f - Tramo(t, 0f, 1.6f));
        agua.volume = 0.22f * Tramo(t, 0f, 2f);
        vineta.intensity.Override(0.32f);

        // Cámara baja detrás del muelle: Nilo y la radio a la derecha, Liria al fondo, el menú a la izquierda.
        float d = Mathf.Sin(t * 0.07f);
        Colocar(new Vector3(-2.2f + d * 0.5f, 2.1f + d * 0.1f, -3.2f), new Vector3(3.6f + d * 0.8f, 3.4f, 40f), 44f, 0.35f);

        float latido = 0.5f + 0.5f * Mathf.Sin(t * 2.4f);
        piloto.SetColor("_EmissionColor", RelevoUI.Verde * (0.6f + 1.2f * latido));

        for (int i = 0; i < ventanas.Count; i++)
        {
            float a = encendido[i] < 0f ? 0f : Tramo(t, encendido[i], encendido[i] + 0.8f);
            EncenderBarrio(ventanas[i], a * (0.92f + 0.08f * Mathf.Sin(t * 1.3f + i)));
        }
    }
}
