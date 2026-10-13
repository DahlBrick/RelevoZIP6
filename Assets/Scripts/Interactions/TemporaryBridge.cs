using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puente eléctrico temporal: solo es transitable mientras su base tiene la batería.
/// Nilo no tiene gravedad, así que un hueco sin colisión sería caminable: por eso cada
/// tramo mantiene un bloqueo sólido mientras su plataforma no está asentada. El
/// bloqueo se desactiva únicamente cuando la plataforma ya es sólida, de modo que
/// cruzar el puente retirado es imposible por construcción.
/// </summary>
public class TemporaryBridge : MonoBehaviour
{
    [System.Serializable]
    class Tramo
    {
        public Transform plataforma;
        public BoxCollider colisionPlataforma;
        public BoxCollider bloqueo;
        public Vector3 destino;
        public Vector3 escalaDestino;
        public float retardo;
        public float progreso;
    }

    [SerializeField] float duracion = 0.3f;
    [SerializeField] float hundimiento = 1.3f;

    public string Identificador = "B";
    public bool Desplegado { get; private set; }

    // Serializada a propósito: una escena horneada no ejecuta RegistrarTramo(), así que los
    // tramos (plataformas, colliders y medidas de destino) deben venir del propio escenario.
    [SerializeField] List<Tramo> tramos = new List<Tramo>();

    /// <summary>Registra una plataforma ya colocada en su posición final y su bloqueo asociado.</summary>
    public void RegistrarTramo(Transform plataforma, BoxCollider colisionPlataforma, BoxCollider bloqueo, float retardo)
    {
        Tramo nuevo = new Tramo();
        nuevo.plataforma = plataforma;
        nuevo.colisionPlataforma = colisionPlataforma;
        nuevo.bloqueo = bloqueo;
        nuevo.destino = plataforma.localPosition;
        nuevo.escalaDestino = plataforma.localScale;
        nuevo.retardo = retardo;
        nuevo.progreso = 0f;
        tramos.Add(nuevo);

        Aplicar(nuevo);
    }

    /// <summary>
    /// Devuelve el puente a su estado retraído. 'Desplegado' y el progreso de cada tramo son
    /// estado de ejecución y no sobreviven al horneado; los tramos, en cambio, llegan
    /// serializados desde la escena. La llama LevelBootstrap.Enlazar().
    /// </summary>
    public void Vincular()
    {
        Desplegado = false;

        for (int i = 0; i < tramos.Count; i++)
        {
            tramos[i].progreso = 0f;
            Aplicar(tramos[i]);
        }

        // Update() se autodesactiva al asentarse: hay que reactivarlo para que pueda animar.
        enabled = true;
    }

    /// <summary>Verdadero si el punto (en el plano) está sobre alguno de los tramos del puente.</summary>
    public bool Contiene(Vector3 punto)
    {
        for (int i = 0; i < tramos.Count; i++)
        {
            Tramo tramo = tramos[i];
            if (tramo.plataforma == null) continue;
            Transform padre = tramo.plataforma.parent;
            Vector3 centro = padre != null ? padre.TransformPoint(tramo.destino) : tramo.destino;
            float mx = tramo.escalaDestino.x * 0.5f + 0.3f;
            float mz = tramo.escalaDestino.z * 0.5f + 0.3f;
            if (Mathf.Abs(punto.x - centro.x) <= mx && Mathf.Abs(punto.z - centro.z) <= mz) return true;
        }
        return false;
    }

    public void Activar(bool activo)
    {
        if (Desplegado == activo) return;

        Desplegado = activo;
        for (int i = 0; i < tramos.Count; i++)
        {
            tramos[i].retardo = Desplegado ? tramos[i].retardo : 0f;
        }

        // El componente se autodesactiva cuando ya no queda nada que animar (ver Update()),
        // así que hay que reactivarlo aquí: si no, el despliegue nunca se ejecutaría.
        enabled = true;

        GameEvents.RaisePuenteCambiado(this, activo);
        AudioDirector.Pedir(activo ? Tono.PuenteActivo : Tono.PuenteRetraido);
    }

    void Update()
    {
        float paso = Time.deltaTime / Mathf.Max(0.05f, duracion);
        float objetivo = Desplegado ? 1f : 0f;
        bool cambio = false;

        for (int i = 0; i < tramos.Count; i++)
        {
            Tramo tramo = tramos[i];
            if (tramo.plataforma == null) continue;
            if (Mathf.Approximately(tramo.progreso, objetivo)) continue;

            if (Desplegado && tramo.retardo > 0f)
            {
                tramo.retardo -= Time.deltaTime;
                continue;
            }

            tramo.progreso = Mathf.MoveTowards(tramo.progreso, objetivo, paso);
            Aplicar(tramo);
            cambio = true;
        }

        // Si ya está asentado no hay nada que animar.
        if (!cambio) enabled = false;
    }

    void Aplicar(Tramo tramo)
    {
        if (tramo.plataforma != null)
        {
            tramo.plataforma.localPosition = Vector3.Lerp(tramo.destino + Vector3.down * hundimiento, tramo.destino, tramo.progreso);
            tramo.plataforma.localScale = new Vector3(
                tramo.escalaDestino.x,
                Mathf.Max(0.001f, tramo.escalaDestino.y * tramo.progreso),
                tramo.escalaDestino.z);
        }

        float solidez = float.IsNaN(tramo.progreso) ? 0f : tramo.progreso;
        bool asentado = Desplegado && solidez > 0.97f;

        if (tramo.colisionPlataforma != null) tramo.colisionPlataforma.enabled = asentado;
        if (tramo.bloqueo != null) tramo.bloqueo.enabled = !asentado;
    }

    void OnEnable()
    {
        // Al reactivarse (o al crearse ya retirado) el estado debe ser coherente de inmediato.
        for (int i = 0; i < tramos.Count; i++) Aplicar(tramos[i]);
    }
}
