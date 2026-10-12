/// <summary>
/// Mapas de los tres niveles. Este archivo es la única fuente de verdad del
/// diseño espacial: para mover un muelle, una base o la salida basta editar
/// el texto del nivel correspondiente.
/// </summary>
public static class LevelLayouts
{
    public const float TamanoTile = 2f;

    public static LevelLayout Obtener(int indice)
    {
        switch (indice)
        {
            case 1: return Nivel1();
            case 2: return Nivel2();
            case 3: return Nivel3();
            default: return null;
        }
    }

    /// <summary>
    /// Nivel 1 - Muelle de Cobre. Enseña el ciclo completo: la batería en la base A
    /// abre el puente eléctrico hacia B, la palanca de B despliega la pasarela
    /// manual del sur (D-B), Nilo vuelve por el camino de piedra ya existente,
    /// recupera la batería y sale por el camino permanente.
    /// </summary>
    public static LevelLayout Nivel1()
    {
        return new LevelLayout("Nivel 1 - Muelle de Cobre", TamanoTile,
            "wwvwwwwwvwwvw",
            "wwwwwwwwwwwww",
            "wwwwvwwwwwwvw",
            "MY#wwwwwww##E",
            "N#ABBBBBBB###",
            "###wwwwwwwL##",
            "w#wPPPPPPPPww",
            "w#wmmmmwwwwww",
            "w##mmmmwwwwww",
            "#Xwmmmmwwwwww");
    }

    /// <summary>
    /// Nivel 2 - Patio de Bombas. Dos canales parten el patio en tres bandas —oeste (0-3), central
    /// (6-8) y este (11-12)— y cada canal se cruza de dos formas y solo dos: el oeste por el puente
    /// de la base A (fila 2) o por la pasarela de la palanca L (fila 10); el este por el puente de
    /// la base D (fila 3) o por la pasarela de la palanca K (fila 8).
    ///
    /// Con una sola batería, el orden es lo que manda: B se retrae en cuanto la batería sale de A,
    /// así que el cruce de la fila 2 se cierra solo y la banda oeste solo vuelve a conectar con la
    /// central por P, que es permanente. Igual al este: cuando la batería sale de D, C se retrae y
    /// la única salida es Q. Sin soft-lock, porque la batería siempre puede volver a una base.
    ///
    /// Las filas de cruce 10 y 8 se eligieron para que ningún poste decorativo de
    /// DioramaBuilder.AnadirFarolesDeMadera —(3,7) y (6,9), fijos y sin luz propia— quede dentro
    /// del volumen de una plataforma que asciende.
    ///</summary>
    public static LevelLayout Nivel2()
    {
        return new LevelLayout("Nivel 2 - Patio de Bombas", TamanoTile,
            "####ww###ww##",
            "###vww#v#ww##",
            "###ABB###ww##",
            "##ccww#cDCC#c",
            "#M##ww###ww##",
            "N#Y#ww###wwcE",
            "####ww#c#ww#K",
            "#c##wwL##ww#c",
            "#v##ww###QQcc",
            "###cww###ww##",
            "##ccPP#v#ww##",
            "####ww##Xww##");
    }

    /// <summary>
    /// Nivel 3 - Faro de Liria. Una sola banda al oeste (columnas 0-3), el canal en las 4-5 y la
    /// isla del faro en las 6-10: el mismo encuadre cerrado que el Muelle, sin una segunda banda
    /// que ensanche el plano.
    ///
    /// La isla tiene una única entrada —el puente de la base D, en la fila 2— y la salida por Q es
    /// obligatoria: al retirar la batería de D, C se retrae y la fila 2 se cierra, de modo que el
    /// regreso desde la isla pasa por la pasarela de la palanca K. La salida (E) es el faro y la
    /// batería se entrega en su cuna en lugar de cruzarse.
    ///
    /// Ejercita los mismos marcadores que el Nivel 2 (D, C, Q, K) con un solo juego de cada uno.
    ///</summary>
    public static LevelLayout Nivel3()
    {
        return new LevelLayout("Nivel 3 - Faro de Liria", TamanoTile,
            "####ww#####",
            "#v##ww##E##",
            "##cDCC#####",
            "M###ww###v#",
            "####ww#####",
            "N#Y#ww##K##",
            "##ccww#####",
            "###cQQ#####",
            "#v##QQ#c#c#",
            "##ccww###X#");
    }
}
