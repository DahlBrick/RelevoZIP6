using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pasarela manual permanente. Empieza retraída y se despliega una sola vez al
/// accionar su palanca: el camino de vuelta no depende de la energía.
/// Igual que el puente eléctrico, cada tramo conserva un bloqueo sólido hasta que
/// su plataforma está asentada, así que no se puede cruzar antes de tiempo.
/// </summary>
public class PermanentWalkway : MonoBehaviour
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

    [SerializeField] float duracion = 0.26f;
    [SerializeField] float hundimiento = 1.4f;

    public string Identificador = "P";
    public bool Abierta { get; private set; }

    // Serializada a propósito: una escena horneada no ejecuta RegistrarTramo(), así que los
    // tramos (plataformas, colliders y medidas de destino) deben venir del propio escenario.
    [SerializeField] List<Tramo> tramos = new List<Tramo>();

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
    /// Devuelve la pasarela a su estado retraído. 'Abierta' y el progreso de cada tramo son
    /// estado de ejecución y no sobreviven al horneado; los tramos, en cambio, llegan
    /// serializados desde la escena. La llama LevelBootstrap.Enlazar().
    /// </summary>
    public void Vincular()
    {
        Abierta = false;

        for (int i = 0; i < tramos.Count; i++)
        {
            tramos[i].progreso = 0f;
            Aplicar(tramos[i]);
        }

        // Update() se autodesactiva al asentarse: hay que reactivarlo para que pueda animar.
        enabled = true;
    }

    public void Abrir()
    {
        if (Abierta) return;

        Abierta = true;
        AudioDirector.Pedir(Tono.Palanca);
        GameEvents.RaisePasarelaAbierta(this);
    }

    void Update()
    {
        if (!Abierta) return;

        float paso = Time.deltaTime / Mathf.Max(0.05f, duracion);
        bool cambio = false;

        for (int i = 0; i < tramos.Count; i++)
        {
            Tramo tramo = tramos[i];
            if (tramo.plataforma == null) continue;
            if (tramo.progreso >= 1f) continue;

            if (tramo.retardo > 0f)
            {
                tramo.retardo -= Time.deltaTime;
                continue;
            }

            tramo.progreso = Mathf.MoveTowards(tramo.progreso, 1f, paso);
            Aplicar(tramo);
            cambio = true;
        }

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

        bool asentado = Abierta && tramo.progreso > 0.97f;

        if (tramo.colisionPlataforma != null) tramo.colisionPlataforma.enabled = asentado;
        if (tramo.bloqueo != null) tramo.bloqueo.enabled = !asentado;
    }

    void OnEnable()
    {
        for (int i = 0; i < tramos.Count; i++) Aplicar(tramos[i]);
    }
}
