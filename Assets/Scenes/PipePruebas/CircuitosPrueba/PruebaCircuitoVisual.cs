using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PROTOTIPO - Circuitos de energia. Capa visual unica y continua.
///
/// Cada tramo rotable tiene su linea continua como HIJO de la propia pieza, construida en la
/// orientacion local de la solucion: por eso gira exactamente con el Transform de la pieza
/// (90 grados, misma posicion, misma elevacion) sin tocar colliders ni la logica del circuito.
///
/// La capa no decide nada: lee el material que PruebaCircuitoControlador ya escribio en los
/// trazos y los puertos que cada pieza declara (MascaraPuertos, ya girada por 'pasos').
///   cobre    -> 0 sin energia
///   turquesa -> 1 panel
///   ambar    -> 2 bateria
///
/// Una barra copia el estado de su propio tramo: si la corriente llega al tramo, la barra se ve
/// encendida aunque su extremo muera en una casilla vacia o en un tramo sin ese puerto. El color
/// no depende de que el otro lado este conectado; solo de que el tramo tenga energia.
///
/// Los materiales son copias locales (CircuitosPrueba/Materiales) clonadas al ejecutar, para no
/// ensuciar ningun material del juego.
/// </summary>
public sealed class PruebaCircuitoVisual : MonoBehaviour
{
    /// <summary>Nodo de una casilla con su juego de barras, una por puerto.</summary>
    [Serializable]
    public sealed class Tramo
    {
        public PruebaCircuitoPieza pieza;         // tramo rotable
        public PruebaCircuitoFuente fuente;       // nodo de entrada (sin pieza)
        public PruebaCircuitoReceptor receptor;   // nodo de salida (sin pieza)
        public Renderer nodo;
        public Renderer[] barras = new Renderer[0];
        public int[] puertos = new int[0];        // puerto base de cada barra (0..3)

        [NonSerialized] public int estado;
        [NonSerialized] public bool leido;
        [NonSerialized] public int[] estadosBarra = new int[0];
    }

    [Header("Materiales locales de la capa visual")]
    public Material cobre;
    public Material turquesa;
    public Material ambar;

    [Header("Contenido de la red")]
    public Tramo[] tramos = new Tramo[0];

    static Material _cobre;
    static Material _turquesa;
    static Material _ambar;

    readonly Dictionary<Vector2Int, int> indice = new Dictionary<Vector2Int, int>();
    bool indexado;

    void Awake()
    {
        Preparar();
        Indizar();
        Refrescar(true);
    }

    void Update()
    {
        Preparar();
        Indizar();
        Refrescar(false);
    }

    // ---------------------------------------------------------------- materiales de ejecucion

    void Preparar()
    {
        if (_cobre != null) return;

        _cobre = cobre != null ? new Material(cobre) : null;
        _turquesa = turquesa != null ? new Material(turquesa) : null;
        _ambar = ambar != null ? new Material(ambar) : null;

        if (_cobre != null && !_cobre.HasProperty("_EmissionColor")) _cobre = null;
        if (_turquesa != null && !_turquesa.HasProperty("_EmissionColor")) _turquesa = null;
        if (_ambar != null && !_ambar.HasProperty("_EmissionColor")) _ambar = null;

        // Sin aporte a GI: la linea no ilumina el escenario.
        if (_cobre != null) _cobre.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        if (_turquesa != null) _turquesa.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        if (_ambar != null) _ambar.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
    }

    // ---------------------------------------------------------------- indice de casillas

    void Indizar()
    {
        if (indexado) return;
        indice.Clear();

        if (tramos != null)
            for (int i = 0; i < tramos.Length; i++)
            {
                Tramo t = tramos[i];
                if (t == null) continue;

                Vector2Int c;
                if (t.pieza != null) c = t.pieza.Casilla;
                else if (t.fuente != null) c = t.fuente.Casilla;
                else if (t.receptor != null) c = t.receptor.Casilla;
                else continue;

                if (!indice.ContainsKey(c)) indice[c] = i;
            }

        indexado = true;
    }

    static Vector2Int Delta(int puerto)
    {
        switch (puerto & 3)
        {
            case 0: return new Vector2Int(1, 0);    // Este
            case 1: return new Vector2Int(0, 1);    // Sur
            case 2: return new Vector2Int(-1, 0);   // Oeste
            default: return new Vector2Int(0, -1);  // Norte
        }
    }

    static bool Tiene(int mascara, int puerto) { return (mascara & (1 << (puerto & 3))) != 0; }

    static int ClaseDe(PruebaCircuitoFuente f)
    {
        return f != null && f.Tipo == PruebaCircuitoFuente.Clase.BaseEnergia ? 2 : 1;
    }

    /// <summary>0 sin energia, 1 panel, 2 bateria. Se copia del material ya escrito en los trazos.</summary>
    static int EstadoMaterial(PruebaCircuitoPieza pieza)
    {
        if (pieza == null) return 0;

        Renderer[] trazos = pieza.Trazos;
        if (trazos == null) return 0;

        for (int i = 0; i < trazos.Length; i++)
        {
            if (trazos[i] == null) continue;

            Material m = trazos[i].sharedMaterial;
            if (m == null) return 0;
            if (m.name.Contains("Turquesa")) return 1;
            if (m.name.Contains("Ambar")) return 2;
            return 0;
        }

        return 0;
    }

    // ---------------------------------------------------------------- estado

    int EstadoDe(Tramo t)
    {
        if (t == null) return 0;
        if (t.pieza != null) return EstadoMaterial(t.pieza);
        if (t.fuente != null) return t.fuente.Activa ? ClaseDe(t.fuente) : 0;

        if (t.receptor != null)
        {
            // El receptor se enciende con el tramo que llega a su puerto.
            int puerto = t.receptor.Puerto & 3;
            int i;
            if (!indice.TryGetValue(t.receptor.Casilla + Delta(puerto), out i)) return 0;

            Tramo vecino = tramos[i];
            if (vecino == null || vecino.pieza == null) return 0;
            if (!Tiene(vecino.pieza.MascaraPuertos, (puerto + 2) & 3)) return 0;
            return EstadoMaterial(vecino.pieza);
        }

        return 0;
    }

    /// <summary>
    /// Estado de una barra. La corriente recorre el tramo entero: la barra se pinta con el estado
    /// del propio tramo aunque el extremo no llegue a nada, para que un tramo alimentado nunca
    /// muestre un tiron cobre en su punta.
    /// </summary>
    static int EstadoBarra(int estadoPropio)
    {
        return estadoPropio;
    }

    // ---------------------------------------------------------------- pintado

    void Refrescar(bool forzar)
    {
        if (tramos == null) return;

        for (int i = 0; i < tramos.Length; i++)
        {
            Tramo t = tramos[i];
            if (t == null) continue;

            int estado = EstadoDe(t);

            if (forzar || !t.leido || t.estado != estado)
            {
                t.estado = estado;
                t.leido = true;
                Pintar(t.nodo, estado);
            }

            if (t.barras == null) continue;

            if (t.estadosBarra == null || t.estadosBarra.Length != t.barras.Length)
            {
                t.estadosBarra = new int[t.barras.Length];
                for (int k = 0; k < t.estadosBarra.Length; k++) t.estadosBarra[k] = -1;
            }

            for (int j = 0; j < t.barras.Length; j++)
            {
                int estadoBarra = EstadoBarra(estado);

                if (estadoBarra == t.estadosBarra[j]) continue;
                t.estadosBarra[j] = estadoBarra;
                Pintar(t.barras[j], estadoBarra);
            }
        }
    }

    void Pintar(Renderer destino, int estado)
    {
        if (destino == null) return;

        Material m = estado == 1 ? _turquesa : (estado == 2 ? _ambar : _cobre);
        if (m == null) return;

        destino.sharedMaterial = m;
    }
}
