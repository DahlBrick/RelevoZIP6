using System.Collections.Generic;
using UnityEngine;

/// <summary>Maquetación de las redes del canal con los mismos tramos, fuentes y receptores del prototipo.
/// El recorrido parte de la fuente y termina en un borne fijo en la orilla del puente.</summary>
public static class CircuitosDelCanal
{
    const string Rosa = "ESON";
    static readonly Vector2Int[] Delta = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
    static readonly Vector3[] Eje = { Vector3.right, Vector3.back, Vector3.left, Vector3.forward };

    public static void Montar(int indice, Transform actores, LevelLayout mapa, EnergySocket baseA,
        TemporaryBridge puenteB, PlacaPresion panel, TemporaryBridge puenteR)
    {
        string redPanel, redBase;
        switch (indice)
        {
            case 4: redPanel = "ESE"; redBase = "ONONO"; break;
            case 5: redPanel = "ENONE"; redBase = "SOO"; break;
            case 6: redPanel = "NOOS"; redBase = "ESENE"; break;
            default: return;
        }
        MontarRed(actores, mapa, redPanel, panel.Casilla, panel, null, puenteR, 'R');
        MontarRed(actores, mapa, redBase, mapa.Casilla(baseA.transform.position), null, baseA, puenteB, 'B');
    }

    static void MontarRed(Transform actores, LevelLayout mapa, string recorrido, Vector2Int casilla,
        PlacaPresion panel, EnergySocket toma, TemporaryBridge puente, char letraPuente)
    {
        var raiz = DioramaBuilder.CrearAncla(panel != null ? "Circuito del panel" : "Circuito de batería", actores, Vector3.zero);
        var fuente = raiz.gameObject.AddComponent<PruebaCircuitoFuente>();
        int puerto = Rosa.IndexOf(recorrido[0]);
        fuente.Configurar(panel, toma, casilla, puerto);

        Material cobre = RelevoPalette.MaterialDe(ClaveMaterial.Cobre);
        Material asiento = RelevoPalette.MaterialDe(ClaveMaterial.MaderaOscura);
        var inicio = DioramaBuilder.CrearAncla("Salida de corriente", raiz, mapa.CentroTile(casilla.x, casilla.y));
        Brazo(inicio, puerto, cobre);
        var piezas = new List<PruebaCircuitoPieza>();
        for (int i = 0; i < recorrido.Length - 1; i++)
        {
            casilla += Delta[puerto];
            int entrada = (puerto + 2) % 4;
            puerto = Rosa.IndexOf(recorrido[i + 1]);
            int mascara = (1 << entrada) | (1 << puerto);
            bool recta = mascara == 5 || mascara == 10;
            var pieza = DioramaBuilder.CrearAncla("Tramo " + casilla.x + "," + casilla.y, raiz, mapa.CentroTile(casilla.x, casilla.y));
            DioramaBuilder.CrearCilindro("Disco", pieza, Vector3.up * 0.095f, 0.63f, 0.07f, asiento);
            Renderer nodo = DioramaBuilder.CrearCilindro("Contacto", pieza, Vector3.up * 0.19f, 0.24f, 0.09f, cobre).GetComponent<Renderer>();
            var tramo = pieza.gameObject.AddComponent<PruebaCircuitoPieza>();
            int giro = recta ? 1 : 1 + (casilla.x + casilla.y) % 3;
            tramo.Configurar(casilla, recta, mascara, giro, new[] { nodo, Brazo(pieza, entrada, cobre), Brazo(pieza, puerto, cobre) });
            piezas.Add(tramo);
        }

        casilla += Delta[puerto];
        Vector3 posicion = mapa.CentroTile(casilla.x, casilla.y);
        var borne = DioramaBuilder.CrearAncla("Borne del puente " + letraPuente, raiz, posicion);
        Material marca = RelevoPalette.MaterialDe(panel != null ? ClaveMaterial.Turquesa : ClaveMaterial.Ambar);
        DioramaBuilder.CrearCaja("Zapata", borne, Vector3.up * 0.09f, new Vector3(0.82f, 0.12f, 0.82f), asiento, false);
        DioramaBuilder.CrearCilindro("Terminal", borne, Vector3.up * 0.20f, 0.25f, 0.12f, marca);
        int entradaBorne = (puerto + 2) % 4;
        Brazo(borne, entradaBorne, cobre);
        for (int d = 0; d < 4; d++)
        {
            Vector2Int vecina = casilla + Delta[d];
            if (mapa.CharEn(vecina.x, vecina.y) == letraPuente) Brazo(borne, d, cobre);
        }
        var receptor = borne.gameObject.AddComponent<PruebaCircuitoReceptor>();
        receptor.Configurar(puente, casilla, entradaBorne);
        raiz.gameObject.AddComponent<PruebaCircuitoControlador>().Configurar(fuente, receptor, piezas);
    }

    static Renderer Brazo(Transform padre, int puerto, Material cobre)
    {
        Vector3 centro = Eje[puerto] * 0.5f + Vector3.up * 0.17f;
        Vector3 forma = puerto % 2 == 0 ? new Vector3(1f, 0.08f, 0.19f) : new Vector3(0.19f, 0.08f, 1f);
        return DioramaBuilder.CrearCaja("Conductor", padre, centro, forma, cobre, false).GetComponent<Renderer>();
    }
}
