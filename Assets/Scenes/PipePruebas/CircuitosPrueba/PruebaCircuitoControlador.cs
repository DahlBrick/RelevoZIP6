using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PROTOTIPO - Circuitos de energia. Controlador de una red.
///
/// Modela puertos cardinales por casilla y busca por anchura la region alcanzable desde la fuente
/// activa. Solo la region alcanzada se pinta con el color de su propia fuente: turquesa cuando la
/// alimenta una placa o panel, ambar cuando la alimenta una base con bateria. El receptor responde
/// unicamente si su
/// termina de girar: nunca por fotograma y sin reconstruir geometria.
/// </summary>
public class PruebaCircuitoControlador : MonoBehaviour
{
    [SerializeField] PruebaCircuitoFuente fuente;
    [SerializeField] PruebaCircuitoReceptor receptor;
    [SerializeField] List<PruebaCircuitoPieza> tramos = new List<PruebaCircuitoPieza>();

    readonly Dictionary<Vector2Int, int> mascaras = new Dictionary<Vector2Int, int>();
    readonly HashSet<Vector2Int> alcanzadas = new HashSet<Vector2Int>();
    readonly Queue<Vector2Int> cola = new Queue<Vector2Int>();

    Material cobre;
    Material encendido;
    bool ultimaFuente;

    /// <summary>Verdadero si la red la alimenta una base con bateria (se pinta en ambar).</summary>
    bool EsFuenteDeBateria
    {
        get { return fuente != null && fuente.Tipo == PruebaCircuitoFuente.Clase.BaseEnergia; }
    }

    void Start()
    {
        cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);

        // El color de encendido lo decide el tipo de fuente, nunca una asignacion por color:
        // placa/panel -> turquesa, base con bateria -> ambar. El apagado es siempre cobre.
        encendido = RelevoPalette.MaterialDe(EsFuenteDeBateria ? ClaveMaterial.Ambar : ClaveMaterial.Turquesa);

        for (int i = 0; i < tramos.Count; i++)
            if (tramos[i] != null) tramos[i].Enganchar(this);

        ultimaFuente = fuente != null && fuente.Activa;
        Recalcular();
    }

    void Update()
    {
        if (fuente == null) return;

        bool activa = fuente.Activa;
        if (activa == ultimaFuente) return;

        ultimaFuente = activa;
        Recalcular();
    }

    /// <summary>La llama un tramo cuando termina de girar.</summary>
    public void AvisarGiro() { Recalcular(); }

    public void Recalcular()
    {
        mascaras.Clear();
        alcanzadas.Clear();
        cola.Clear();

        if (fuente == null || receptor == null)
        {
            for (int i = 0; i < tramos.Count; i++) Pintar(tramos[i], false);
            return;
        }

        for (int i = 0; i < tramos.Count; i++)
        {
            PruebaCircuitoPieza tramo = tramos[i];
            if (tramo == null) continue;

            int actual;
            mascaras.TryGetValue(tramo.Casilla, out actual);
            mascaras[tramo.Casilla] = actual | tramo.MascaraPuertos;
        }

        Vector2Int casillaFuente = fuente.Casilla;
        Vector2Int casillaReceptor = receptor.Casilla;
        AnadirPuerto(casillaFuente, fuente.Puerto);
        AnadirPuerto(casillaReceptor, receptor.Puerto);

        bool activa = fuente.Activa;
        if (activa) BuscarAlcanzables(casillaFuente);

        for (int i = 0; i < tramos.Count; i++)
        {
            PruebaCircuitoPieza tramo = tramos[i];
            if (tramo == null) continue;
            Pintar(tramo, activa && alcanzadas.Contains(tramo.Casilla));
        }

        receptor.Aplicar(activa && alcanzadas.Contains(casillaReceptor));
    }

    void BuscarAlcanzables(Vector2Int origen)
    {
        alcanzadas.Add(origen);
        cola.Enqueue(origen);

        while (cola.Count > 0)
        {
            Vector2Int actual = cola.Dequeue();

            int mascaraActual;
            if (!mascaras.TryGetValue(actual, out mascaraActual)) continue;

            for (int d = 0; d < 4; d++)
            {
                if ((mascaraActual & (1 << d)) == 0) continue;

                Vector2Int vecina = actual + Desplazamiento(d);
                if (alcanzadas.Contains(vecina)) continue;

                int mascaraVecina;
                if (!mascaras.TryGetValue(vecina, out mascaraVecina)) continue;
                if ((mascaraVecina & (1 << ((d + 2) & 3))) == 0) continue;

                alcanzadas.Add(vecina);
                cola.Enqueue(vecina);
            }
        }
    }

    void AnadirPuerto(Vector2Int casilla, int puerto)
    {
        int actual;
        mascaras.TryGetValue(casilla, out actual);
        mascaras[casilla] = actual | (1 << (puerto & 3));
    }

    static Vector2Int Desplazamiento(int direccion)
    {
        switch (direccion)
        {
            case 0: return new Vector2Int(1, 0);      // Este
            case 1: return new Vector2Int(0, 1);      // Sur
            case 2: return new Vector2Int(-1, 0);     // Oeste
            default: return new Vector2Int(0, -1);    // Norte
        }
    }

    void Pintar(PruebaCircuitoPieza tramo, bool alimentado)
    {
        if (tramo == null) return;

        Material material = alimentado ? encendido : cobre;
        if (material == null) return;

        Renderer[] trazos = tramo.Trazos;
        if (trazos == null) return;

        for (int i = 0; i < trazos.Length; i++)
            if (trazos[i] != null) trazos[i].sharedMaterial = material;
    }
}
