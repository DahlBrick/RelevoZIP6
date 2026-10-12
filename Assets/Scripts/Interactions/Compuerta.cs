using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Compuerta de barrotes turquesa: es un mecanismo manual, no eléctrico. Cierra el paso de su
/// casilla mientras su placa de presión no está pisada y se hunde en el suelo cuando lo está.
///
/// Nunca se cierra encima de Nilo ni de una caja: si al soltarse la placa hay algo en la casilla,
/// espera a que quede libre.
/// </summary>
public sealed class Compuerta : MonoBehaviour
{
    public static readonly List<Compuerta> Todas = new List<Compuerta>();

    const float Duracion = 0.45f;
    const float Hundimiento = 1.25f;

    [SerializeField] Vector2Int casilla;
    [SerializeField] Transform barrotes;
    [SerializeField] BoxCollider bloqueo;

    public bool Abierta { get; private set; }
    public Vector2Int Casilla { get { return casilla; } }

    bool quiereAbrir;
    float progreso;      // 0 cerrada, 1 abierta

    public void Configurar(Vector2Int casilla, Transform barrotes, BoxCollider bloqueo)
    {
        this.casilla = casilla;
        this.barrotes = barrotes;
        this.bloqueo = bloqueo;
    }

    void OnEnable() { if (!Todas.Contains(this)) Todas.Add(this); }
    void OnDisable() { Todas.Remove(this); }

    public static Compuerta En(Vector2Int c)
    {
        for (int i = 0; i < Todas.Count; i++)
            if (Todas[i] != null && Todas[i].casilla == c) return Todas[i];
        return null;
    }

    public void Vincular()
    {
        quiereAbrir = false;
        Abierta = false;
        progreso = 0f;
        Aplicar();
    }

    public void Abrir(bool abrir)
    {
        if (quiereAbrir == abrir) return;
        quiereAbrir = abrir;
        AudioDirector.Pedir(abrir ? Tono.CompuertaAbre : Tono.CompuertaCierra);
    }

    bool Ocupada()
    {
        if (CajaEmpujable.En(casilla) != null) return true;
        LevelLayout layout = RelevoGame.Layout;
        PlayerController nilo = RelevoGame.Jugador;
        if (layout == null || nilo == null) return false;
        Vector3 centro = layout.CentroTile(casilla.x, casilla.y);
        Vector3 d = nilo.transform.position - centro;
        d.y = 0f;
        return Mathf.Abs(d.x) < layout.TamanoTile * 0.5f + 0.35f && Mathf.Abs(d.z) < layout.TamanoTile * 0.5f + 0.35f;
    }

    void Update()
    {
        float objetivo = quiereAbrir ? 1f : 0f;
        if (!quiereAbrir && progreso > 0f && Ocupada()) return;   // espera a que la casilla quede libre

        if (Mathf.Approximately(progreso, objetivo)) return;
        progreso = Mathf.MoveTowards(progreso, objetivo, Time.deltaTime / Duracion);
        Aplicar();
    }

    void Aplicar()
    {
        float s = progreso * progreso * (3f - 2f * progreso);
        if (barrotes != null) barrotes.localPosition = Vector3.down * Hundimiento * s;

        // Pasable solo cuando los barrotes ya están casi abajo; bloquea en cuanto empiezan a subir.
        Abierta = progreso > 0.85f;
        if (bloqueo != null) bloqueo.enabled = !Abierta;
    }
}
