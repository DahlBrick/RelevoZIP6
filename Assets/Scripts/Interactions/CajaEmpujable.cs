using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Caja de carga del puerto que Nilo empuja caminando contra ella. Se mueve de casilla en
/// casilla (como en un Sokoban) y solo en las cuatro direcciones del mapa. Sobre una placa de
/// presión la mantiene pisada.
///
/// Reglas de movimiento:
///  - Hay que empujar un momento (0,18 s) en la misma dirección: rozarla al pasar no la mueve.
///  - Solo entra en suelo firme: piedra, cobre, madera, placas y compuertas abiertas. Nunca en el
///    agua, en puentes o pasarelas, ni en casillas con piezas del puzle.
///  - Empujar gasta un poco de reserva de energía (ReservaEnergia).
///
/// Si una caja queda atascada en una esquina, R reinicia el nivel.
/// </summary>
public sealed class CajaEmpujable : MonoBehaviour
{
    public static readonly List<CajaEmpujable> Todas = new List<CajaEmpujable>();

    const float TiempoDeEmpuje = 0.18f;
    const float DuracionDeslizamiento = 0.32f;
    public const float CosteEmpuje = 0.5f;

    // Casillas por las que una caja puede pasar o detenerse.
    const string SueloValido = "#cmkoug";

    [SerializeField] Vector2Int casillaInicial;
    [SerializeField] Transform visual;

    public Vector2Int Casilla { get; private set; }
    public bool Moviendose { get { return progreso < 1f; } }

    Vector2Int destino;
    Vector3 desde, hasta;
    float progreso = 1f;
    float empuje;
    Vector2Int direccionEmpuje;
    int ultimoFotogramaEmpuje = -10;

    public void Configurar(Vector2Int casilla, Transform visual)
    {
        casillaInicial = casilla;
        this.visual = visual;
    }

    void OnEnable() { if (!Todas.Contains(this)) Todas.Add(this); }
    void OnDisable() { Todas.Remove(this); }

    /// <summary>Devuelve la caja a su casilla de inicio (también al cargar una escena horneada).</summary>
    public void Vincular(LevelLayout layout)
    {
        Casilla = casillaInicial;
        destino = casillaInicial;
        progreso = 1f;
        empuje = 0f;
        if (layout != null) transform.position = layout.CentroTile(Casilla.x, Casilla.y);
        if (visual != null) visual.localRotation = Quaternion.identity;
    }

    /// <summary>Caja que ocupa (o hacia la que se desliza) una casilla, o null.</summary>
    public static CajaEmpujable En(Vector2Int casilla)
    {
        for (int i = 0; i < Todas.Count; i++)
        {
            CajaEmpujable c = Todas[i];
            if (c != null && (c.Casilla == casilla || c.destino == casilla)) return c;
        }
        return null;
    }

    /// <summary>Caja asentada (no en movimiento) sobre una casilla: la que pisa una placa.</summary>
    public static CajaEmpujable AsentadaEn(Vector2Int casilla)
    {
        for (int i = 0; i < Todas.Count; i++)
        {
            CajaEmpujable c = Todas[i];
            if (c != null && !c.Moviendose && c.Casilla == casilla) return c;
        }
        return null;
    }

    /// <summary>
    /// Lo llama PlayerController cada fotograma que choca con la caja, con la dirección en la que
    /// el jugador intenta avanzar.
    /// </summary>
    public void Empujado(Vector3 intencion, Vector3 posicionJugador)
    {
        if (Moviendose) return;

        intencion.y = 0f;
        if (intencion.sqrMagnitude < 0.25f) return;

        // Dirección cardinal dominante; con mucha diagonal no se empuja.
        Vector2Int dir;
        if (Mathf.Abs(intencion.x) > Mathf.Abs(intencion.z))
        {
            if (Mathf.Abs(intencion.x) < intencion.magnitude * 0.75f) return;
            dir = new Vector2Int(intencion.x > 0f ? 1 : -1, 0);
        }
        else
        {
            if (Mathf.Abs(intencion.z) < intencion.magnitude * 0.75f) return;
            dir = new Vector2Int(0, intencion.z > 0f ? -1 : 1);   // la fila crece hacia -Z
        }

        // Nilo tiene que estar detrás de la caja en esa dirección, no de lado.
        Vector3 relativo = transform.position - posicionJugador;
        Vector3 mundo = new Vector3(dir.x, 0f, -dir.y);
        if (Vector3.Dot(relativo.normalized, mundo) < 0.6f) return;

        if (Time.frameCount - ultimoFotogramaEmpuje > 2 || dir != direccionEmpuje) empuje = 0f;
        ultimoFotogramaEmpuje = Time.frameCount;
        direccionEmpuje = dir;
        empuje += Time.deltaTime;

        if (empuje >= TiempoDeEmpuje)
        {
            empuje = 0f;
            IntentarMover(dir);
        }
    }

    void IntentarMover(Vector2Int dir)
    {
        LevelLayout layout = RelevoGame.Layout;
        if (layout == null) return;

        Vector2Int objetivo = Casilla + dir;
        if (!CasillaLibre(layout, objetivo))
        {
            AudioDirector.Pedir(Tono.Bloqueado);
            return;
        }

        destino = objetivo;
        desde = transform.position;
        hasta = layout.CentroTile(objetivo.x, objetivo.y);
        progreso = 0f;

        AudioDirector.Pedir(Tono.Empujar);
        if (RelevoGame.Reserva != null) RelevoGame.Reserva.Gastar(CosteEmpuje);
    }

    static bool CasillaLibre(LevelLayout layout, Vector2Int c)
    {
        if (c.x < 0 || c.y < 0 || c.x >= layout.Columnas || c.y >= layout.FilasCuenta) return false;
        char ch = layout.CharEn(c.x, c.y);
        if (SueloValido.IndexOf(ch) < 0) return false;

        Compuerta compuerta = Compuerta.En(c);
        if (compuerta != null && !compuerta.Abierta) return false;

        return En(c) == null;
    }

    void Update()
    {
        if (progreso >= 1f) return;

        progreso = Mathf.Min(1f, progreso + Time.deltaTime / DuracionDeslizamiento);
        float s = progreso * progreso * (3f - 2f * progreso);
        transform.position = Vector3.Lerp(desde, hasta, s);

        // Un pequeño cabeceo al arrastrarse: se lee como peso.
        if (visual != null)
        {
            Vector3 eje = Vector3.Cross(Vector3.up, (hasta - desde).normalized);
            visual.localRotation = Quaternion.AngleAxis(Mathf.Sin(progreso * Mathf.PI) * 4f, eje);
        }

        if (progreso >= 1f)
        {
            Casilla = destino;
            if (visual != null) visual.localRotation = Quaternion.identity;
        }
    }
}
