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
    /// Nivel 2 - Patio de Bombas. Tres muelles separados por dos canales: oeste (0-3), central (6-8)
    /// y este (11-13). Añade la reserva de energía de Nilo (12 casillas lejos de la batería) y las
    /// cajas con placas de presión.
    ///
    ///  1. Oeste: la compuerta g separa el sur del norte. Se abre empujando la caja k sobre la
    ///     placa o (tutorial de cajas).
    ///  2. La batería en la base A despliega el puente B. Lejos de la batería, la reserva cuenta:
    ///     hay que cruzar, accionar la palanca L y volver por la pasarela P, que es corta.
    ///  3. Con la pasarela abierta, la batería viaja en manos de Nilo al muelle central y entra en
    ///     la base D, que despliega el puente C al muelle este.
    ///  4. En el este, la caja se empuja dos casillas hasta la placa u, que despliega el puente R
    ///     (manual, turquesa). La vuelta a D tiene que ser por C, el camino corto: volver por R
    ///     agota la reserva y Nilo se apaga.
    ///  5. Se retira la batería de D (C se repliega) y se cruza por R con ella hasta la salida.
    /// </summary>
    public static LevelLayout Nivel2()
    {
        return new LevelLayout("Nivel 2 - Patio de Bombas", TamanoTile,
            "v##vww#v#ww#v#",
            "####ww###ww###",
            "###ABB##DCC###",
            "####ww###ww###",
            "###cPPc#Lwwk##",
            "####ww###ww###",
            "wwgwww###wwu##",
            "####ww###ww###",
            "#ok#ww###ww###",
            "M###ww##cRRc##",
            "NY##ww#v#ww##E",
            "v###wwX##ww#v#");
    }

    /// <summary>
    /// Nivel 3 - Faro de Liria. Una sola banda al oeste (columnas 0-3), el canal en las 4-5 y la
    /// isla del faro en las 6-10: el mismo encuadre cerrado que el Muelle, sin una segunda banda
    /// que ensanche el plano.
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
