#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Herramienta de pruebas (solo editor): juega un nivel siguiendo una ruta escrita como la del
/// solucionador por casillas ("n e2 pick insA e5 pullL ..."). Nilo se mueve con su propio
/// CharacterController, así que los puentes retraídos, las compuertas y el agua lo frenan de
/// verdad, y la reserva y los condensadores corren en tiempo real. Las interacciones se hacen sobre
/// el actor más cercano del tipo pedido.
///
/// Uso desde la CLI: RecorridoPrueba.Empezar("ruta") en Play y leer RecorridoPrueba.Estado.
/// </summary>
public sealed class RecorridoPrueba : MonoBehaviour
{
    public static string Estado = "";
    public static string Registro = "";
    public static float MinReserva = 999f;

    void Update()
    {
        // Los diálogos de Maia bloquean el juego (y con él el gasto de reserva): se cierran solos.
        if (!Fallo() && RelevoGame.UI != null && RelevoGame.UI.Dialogo != null && RelevoGame.UI.Dialogo.Abierto)
            RelevoGame.UI.Dialogo.Cerrar();
        if (RelevoGame.Reserva != null && !RelevoGame.Reserva.EnZona) MinReserva = Mathf.Min(MinReserva, RelevoGame.Reserva.Actual);
    }

    string ruta;

    public static string Empezar(string ruta)
    {
        Estado = "en curso";
        Registro = "";
        MinReserva = 999f;
        var go = new GameObject("Recorrido de prueba");
        go.AddComponent<RecorridoPrueba>().ruta = ruta;
        return "ok";
    }

    void Log(string s) { Registro += s + "\n"; }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        PlayerController nilo = RelevoGame.Jugador;
        LevelLayout layout = RelevoGame.Layout;
        CharacterController cc = nilo.GetComponent<CharacterController>();
        nilo.Colocar(nilo.transform.position);

        string[] pasos = ruta.Split(' ');
        for (int i = 0; i < pasos.Length; i++)
        {
            string p = pasos[i];
            if (p.Length == 0) continue;
            if (Fallo()) { Estado = "FALLO en paso " + i + " (" + p + "): " + Motivo(); yield break; }

            char c = p[0];
            if ((c == 'n' || c == 's' || c == 'e' || c == 'w') && (p.Length == 1 || char.IsDigit(p[1])))
            {
                int veces = p.Length > 1 ? int.Parse(p.Substring(1)) : 1;
                Vector2Int dir = c == 'n' ? new Vector2Int(0, -1) : c == 's' ? new Vector2Int(0, 1) : c == 'e' ? new Vector2Int(1, 0) : new Vector2Int(-1, 0);
                for (int k = 0; k < veces; k++)
                {
                    Vector2Int actual = layout.Casilla(nilo.transform.position);
                    Vector2Int destino = actual + dir;
                    CajaEmpujable caja = CajaEmpujable.En(destino);
                    if (caja != null)
                    {
                        typeof(CajaEmpujable).GetMethod("IntentarMover", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(caja, new object[] { dir });
                        yield return new WaitForSeconds(0.4f);
                    }
                    Vector3 meta = layout.CentroTile(destino.x, destino.y);
                    meta.y = nilo.transform.position.y;
                    float t = 0f;
                    while ((Plano(nilo.transform.position - meta)).magnitude > 0.08f && t < 1.5f)
                    {
                        Vector3 d = Plano(meta - nilo.transform.position);
                        Vector3 paso = Vector3.ClampMagnitude(d, 6f * Time.deltaTime);
                        if (cc.enabled) cc.Move(paso);
                        if (d.sqrMagnitude > 0.001f) nilo.transform.rotation = Quaternion.LookRotation(d);
                        t += Time.deltaTime;
                        yield return null;
                        if (Fallo()) { Estado = "FALLO en paso " + i + " (" + p + "): " + Motivo(); yield break; }
                    }
                    if (t >= 1.5f) { Estado = "ATASCO en paso " + i + " (" + p + ") hacia " + destino + ", Nilo en " + layout.Casilla(nilo.transform.position); yield break; }
                }
            }
            else if (p == ".")
            {
                yield return new WaitForSeconds(0.34f);
            }
            else
            {
                IInteractable objetivo = Buscar(p);
                if (objetivo == null) { Estado = "SIN OBJETIVO para " + p + " en paso " + i; yield break; }
                float dist = Plano(objetivo.Ancla.position - nilo.transform.position).magnitude;
                if (dist > 3.6f) { Estado = "FUERA DE ALCANCE " + p + " (" + dist.ToString("0.0") + " m) en paso " + i; yield break; }
                if (!objetivo.Disponible) { Estado = "NO DISPONIBLE " + p + ": " + objetivo.Motivo; yield break; }
                objetivo.Interactuar(nilo);
                Log(p + " ok, objetivo: " + (RelevoGame.UI != null ? RelevoGame.UI.ObjetivoActual : ""));
                if (p.StartsWith("ins")) yield return new WaitForSeconds(0.45f);
                else yield return new WaitForSeconds(0.15f);
            }
        }

        // Salida (o faro) al final de la ruta.
        ExitInteractable salida = FindFirstObjectByType<ExitInteractable>();
        yield return new WaitForSeconds(0.2f);
        if (Fallo()) { Estado = "FALLO al final: " + Motivo(); yield break; }
        if (salida != null) salida.Interactuar(nilo);
        Estado = RelevoGame.Nivel != null && RelevoGame.Nivel.Completado ? "COMPLETADO" : "NO COMPLETADO";
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }

    static bool Fallo()
    {
        return CaidaAlAgua.Activa || (RelevoGame.Reserva != null && RelevoGame.Reserva.Apagado);
    }

    static string Motivo()
    {
        if (CaidaAlAgua.Activa) return "Nilo cayó al agua";
        return "Nilo se apagó (reserva)";
    }

    IInteractable Buscar(string p)
    {
        if (p == "pick") return RelevoGame.Bateria;
        if (p.StartsWith("ins") || p.StartsWith("rem"))
        {
            string id = p.Substring(3);
            foreach (EnergySocket b in FindObjectsByType<EnergySocket>(FindObjectsSortMode.None))
                if (b.Identificador == id) return b;
        }
        if (p.StartsWith("pull"))
        {
            string id = p.Substring(4);
            foreach (LeverInteractable l in FindObjectsByType<LeverInteractable>(FindObjectsSortMode.None))
                if (l.Identificador == id) return l;
        }
        return null;
    }
}
#endif
