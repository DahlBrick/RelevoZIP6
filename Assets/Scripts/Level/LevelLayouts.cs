/// <summary>
/// Mapas de los niveles. Este archivo es la única fuente de verdad del
/// diseño espacial: para mover un muelle, una base o la salida basta editar
/// el texto del nivel correspondiente.
/// </summary>
public static class LevelLayouts
{
    public const int Cantidad = 10;
    public const float TamanoTile = 2f;

    public static LevelLayout Obtener(int indice)
    {
        switch (indice)
        {
            case 1: return Nivel1();
            case 2: return Nivel2();
            case 3: return Nivel3();
            case 4: return Nivel4();
            case 5: return Nivel5();
            case 6: return Nivel6();
            case 7: return Nivel7();
            case 8: return Nivel8();
            case 9: return Nivel9();
            case 10: return Nivel10();
            default: return null;
        }
    }

    /// <summary>
    /// Nivel 1 - Muelle de Cobre. Tres orillas (oeste, isla central y este) y el ciclo completo
    /// dos veces, la segunda sin ayuda:
    ///  1. La batería en la base A despliega el puente B hacia la isla central. La palanca L abre
    ///     la pasarela P de vuelta al oeste.
    ///  2. Al retirar la batería B se repliega: la batería cruza en manos de Nilo por P hasta la base
    ///     D, que despliega el puente C hacia el este.
    ///  3. La palanca K del este abre la pasarela Q. Se recupera la batería de D y se cruza por Q
    ///     hasta la salida. El recuerdo espera en el embarcadero del este.
    /// Verificado con un solucionador por casillas: 50 acciones, sin atajos.
    /// </summary>
    public static LevelLayout Nivel1()
    {
        return new LevelLayout("Nivel 1 - Muelle de Cobre", TamanoTile,
            "wwvwwwwwwwwwwvw",
            "w##wwww#Lwww##w",
            "MYABBBB###www#E",
            "N##wwwv##wwww##",
            "###wwww##DCCC##",
            "###PPPP###www#K",
            "w##wwww#v#www##",
            "wwmwwww###QQQ##",
            "wwmwwwwwwwwwwX#",
            "wwwwwwwwwwwwwww");
    }

    /// <summary>
    /// Nivel 2 - Patio de Bombas. Oeste (0-3), patio central (6-9), muelle este (12-14) y el
    /// islote de la salida (12-14, filas 9-11). Reserva de 12 casillas.
    ///
    ///  1. Bolsa del suroeste: la caja k va a la placa o (dos empujes al este y dos al sur) para
    ///     abrir la compuerta g. Empujarla hacia el norte la deja atascada contra el agua.
    ///  2. Batería en A, se cruza por B y se acciona L lejos de la batería: la reserva solo da para
    ///     ir directo y volver (por B o por P). La batería pasa por P al patio central.
    ///  3. Batería en D: C lleva al muelle este, donde la segunda caja se empuja tres casillas al
    ///     sur hasta la placa u. Con la reserva justa hay que hacerlo sin rodeos o en dos viajes,
    ///     volviendo a recargar junto a D entre uno y otro.
    ///  4. La placa sostiene el puente R. Se retira la batería de D y se cruza por R hasta la salida.
    /// Verificado con el solucionador: reserva mínima 10, 66 acciones.
    /// </summary>
    public static LevelLayout Nivel2()
    {
        return new LevelLayout("Nivel 2 - Patio de Bombas", TamanoTile,
            "v###ww#v##ww##X",
            "####ww####ww###",
            "###ABB####ww###",
            "####ww###DCC###",
            "###cPPc###wwk##",
            "####ww##L#ww###",
            "####ww####ww###",
            "wgwwww####wwu##",
            "####ww####wwwww",
            "#k##ww###cRR###",
            "N#Y#ww#v##ww##E",
            "M##oww####ww#vv");
    }

    /// <summary>
    /// Nivel 3 - Faro de Liria. Introduce los condensadores (z): junto a una base, se cargan
    /// mientras la batería está puesta y, al retirarla, sostienen el puente unos segundos hasta
    /// descargarse. Es la única forma de cruzar un puente eléctrico llevando la batería.
    ///
    ///  1. Oeste: batería en A, esperar la carga, retirarla y cruzar B antes de que se descargue.
    ///  2. Isla central: la caja k va a la placa o (un empuje al este y dos al norte) para abrir la
    ///     compuerta g del pasillo este. Empujarla dos veces al este la atasca en el pasillo.
    ///  3. Con la batería en la mano (lejos de ella la reserva no alcanza) se baja por el pasillo
    ///     hasta la palanca K, que abre la pasarela Q junto a la base D.
    ///  4. Batería en D, carga completa, retirarla y correr por Q y C hasta el islote del faro.
    ///     Sin Q el camino es demasiado largo para lo que aguanta el condensador.
    /// Verificado con el solucionador: imposible sin condensadores y sin Q; 41 acciones.
    /// </summary>
    public static LevelLayout Nivel3()
    {
        return new LevelLayout("Nivel 3 - Faro de Liria", TamanoTile,
            "v##zww##o#wwvvv",
            "###ABB#####ww##",
            "M#v#ww#k##ww###",
            "NY##ww####ww###",
            "####ww#wwgww###",
            "#v##ww#ww#ww###",
            "wwwwww#wwKww#E#",
            "wwwwww#ww#ww###",
            "wwwwww#ww#CC###",
            "wwwwwwDQQ#ww###",
            "wwwwwwzww#ww#Xv");
    }

    public static LevelLayout Nivel4()
    {
        return new LevelLayout("Nivel 4 - Aduana de la Marea", TamanoTile,
            "v#####CCCDzL###ww",
            "##E###www#####vww",
            "####v#wwwBwwwPwww",
            "mmmwwwwwwBwwwPwww",
            "wwwwwwwwwtt###www",
            "wv####www#tt##v#w",
            "###ut#www##tA###w",
            "####ttRRR#######w",
            "##k###www#######w",
            "NY####www#######w",
            "M####vwwwwwwwmmmw",
            "mmmwwwwwwwwwwmmXw");
    }

    public static LevelLayout Nivel5()
    {
        return new LevelLayout("Nivel 5 - Dársena del Torno", TamanoTile,
            "wwwwwwv######w#E#",
            "wwwwww#NY####w###",
            "wwwwwwM##Dz#vw##v",
            "wwwwwwwww####wwRw",
            "wwwwwwwwwCwwwwwRw",
            "#v#wwwwwwCwwwwwRw",
            "###ww######ww#tt#",
            "L##ww##A###ww#tt#",
            "###BBttt###ww#ut#",
            "###ww######PP####",
            "mmwww#####vww##k#",
            "Xmwwwmmmwwwww###v");
    }

    public static LevelLayout Nivel6()
    {
        return new LevelLayout("Nivel 6 - Esclusa de las Dos Llaves", TamanoTile,
            "wwwwwwwwwwwwwwwwwmm",
            "wwwwwM###wwwww#####",
            "###ww#NY##vwww##E#v",
            "L##wwttt###www#####",
            "###RRt#u###www#####",
            "###ww###k##wwwv####",
            "##vww####o#wwwwwCww",
            "mXwwwv#####wwwwwCww",
            "wwwwwwwPwwwwww##Dz#",
            "wwwwwwwPwwwwww#####",
            "wwwwwwwgwwwwww#####",
            "wwwww######www#K###",
            "wwwww##AtttBBB#####",
            "wwwww###tt#www#####",
            "wwwwwv#####QQQ####v");
    }

    public static LevelLayout Nivel7()
    {
        return new LevelLayout("Nivel 7 - Lonja de las Dos Voces", TamanoTile,
            "wwwwww#L####vwwwww",
            "wwwwww#######wwwww",
            "wwwwwwwwRwwwwwwwww",
            "wwwwwwwwRwwwwwwv##",
            "v##www##t####PP###",
            "##zwww##tt###ww###",
            "##DCCC##ut###ww##E",
            "###www###k###ww###",
            "NY#www####Attww###",
            "M##wwwv####ttQQ##v",
            "mmwwwwwwwwwBwww###",
            "wwwwwwwwwwwBwwwmmw",
            "wwwwwwww#####wwmXw",
            "wwwwwwww#K###wwwww",
            "wwwwwwww####vwwwww");
    }

    public static LevelLayout Nivel8()
    {
        return new LevelLayout("Nivel 8 - Archivo de la Frecuencia", TamanoTile,
            "##vwwww##v##wwwz###L",
            "#E#mmmm#####CCCD####",
            "###wwww#####www####v",
            "wwwwwwwwwwwwwwwPwBww",
            "wwwwwwwwwwwwwwwPwBww",
            "######w##tttRRR##t##",
            "NY##k#w##tu#www##tt#",
            "M#####g#####www###A#",
            "##k###w#####www#####",
            "###o##w#####www####v",
            "#v####wv####www#####",
            "mmmwwwwwwwwwwwwwwwmX");
    }

    public static LevelLayout Nivel9()
    {
        return new LevelLayout("Nivel 9 - Astillero de la Maroma", TamanoTile,
            "v####wwwwwv###wwwww",
            "##1##wwwww####ww#Xv",
            "m#####wwww##k##w###",
            "##F##HwwwwI###uw###",
            "#####wwwww#####w###",
            "MYN##wwwww####RR#E#",
            "#####wwwww####ww###",
            "mmv##wwwwwv###wwv##");
    }

    public static LevelLayout Nivel10()
    {
        return new LevelLayout("Nivel 10 - Casa de las Dos Guardias", TamanoTile,
            "v###wwwwwwv#######",
            "##E#wwwwww##3###X#",
            "####PPPPPP########",
            "v###wwwwww#I###v##",
            "wwwwwwwwwwwwwwwwww",
            "wwwwwwwwwwwwwwwwww",
            "wwwwwwwwwwwwwwwwww",
            "wwwww######H##wwww",
            "wwwww##2######wwww",
            "wwwww##v#F###1wwww",
            "wwwww#m#######wwww",
            "wwwww#M#NY###vwwww");
    }
}
