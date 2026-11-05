/// <summary>
/// Lo que Maia cuenta en el Patio de Bombas. Cada mecánica nueva se explica con una tarjeta
/// ilustrada animada, y al final se puede pedir que lo repita. Hablar con ella después abre un
/// menú para repasar cada mecánica o pedir una pista.
/// </summary>
public static class TutorialMaia
{
    static readonly System.Collections.Generic.HashSet<int> contados = new System.Collections.Generic.HashSet<int>();

    /// <summary>Verdadero la primera vez que se pregunta por un nivel en esta sesión.</summary>
    public static bool PrimeraVez(int nivel) { return contados.Add(nivel); }

    static bool ConCondensadores { get { return RelevoGame.Definicion != null && RelevoGame.Definicion.tutorialCondensadores; } }

    static DialogoUI Dialogo { get { return RelevoGame.UI != null ? RelevoGame.UI.Dialogo : null; } }

    static void Decir(string texto, IlustracionTutorial.Tipo ilustracion)
    {
        DialogoUI d = Dialogo;
        if (d == null) return;
        d.Encolar(new DialogoUI.Mensaje { titulo = "Maia", texto = texto, acento = RelevoUI.Verde, ilustracion = ilustracion });
    }

    /// <summary>Explicación completa al entrar al patio.</summary>
    public static void Presentacion()
    {
        ExplicarReserva();
        ExplicarCajas();

        DialogoUI d = Dialogo;
        if (d == null) return;
        d.Encolar(new DialogoUI.Mensaje
        {
            titulo = "Maia",
            texto = "¿Lo tienes claro, Nilo?",
            acento = RelevoUI.Verde,
            opciones = new[] { "Entendido, Maia", "Explícamelo otra vez" },
            alElegir = delegate (int i) { if (i == 1) Presentacion(); }
        });
    }

    static void ExplicarReserva()
    {
        Decir("Nilo, aquí tu reserva es limitada. Cerca de la batería te recargas. Lejos de ella, cada casilla que caminas gasta un segmento.",
            IlustracionTutorial.Tipo.Reserva);
        Decir("El aro ámbar alrededor de la batería es tu zona segura. Abajo a la derecha verás cuánta reserva te queda.",
            IlustracionTutorial.Tipo.Reserva);
        Decir("Si llegas a cero lejos de la batería, te apagarás y tendremos que empezar el patio de nuevo. Planea la ruta antes de alejarte.",
            IlustracionTutorial.Tipo.Apagado);
    }

    static void ExplicarCajas()
    {
        Decir("Las cajas se empujan caminando contra ellas, una casilla cada vez. Sobre una placa, mantienen abierta una compuerta o un puente sin gastar la batería.",
            IlustracionTutorial.Tipo.Cajas);
        Decir("Tú también puedes pisar una placa, pero mientras estés encima no podrás cruzar. Y si una caja se atasca, R reinicia el patio.",
            IlustracionTutorial.Tipo.Cajas);
    }

    /// <summary>Explicación al llegar al Faro de Liria: los condensadores.</summary>
    public static void PresentacionCondensadores()
    {
        ExplicarCondensadores();

        DialogoUI d = Dialogo;
        if (d == null) return;
        d.Encolar(new DialogoUI.Mensaje
        {
            titulo = "Maia",
            texto = "¿Lo tienes claro, Nilo?",
            acento = RelevoUI.Verde,
            opciones = new[] { "Entendido, Maia", "Explícamelo otra vez" },
            alElegir = delegate (int i) { if (i == 1) PresentacionCondensadores(); }
        });
    }

    static void ExplicarCondensadores()
    {
        Decir("Junto a estas bases hay condensadores: torres con ocho barras. Con la batería puesta, las barras se llenan de ámbar.",
            IlustracionTutorial.Tipo.Ninguna);
        Decir("Si retiras la batería, el condensador sostiene el puente mientras se vacía. Es la única forma de cruzar un puente llevando la batería contigo.",
            IlustracionTutorial.Tipo.Ninguna);
        Decir("Espera a que esté lleno y corre sin pararte. Si el puente se repliega contigo encima, caerás al agua. Tu reserva sigue contando lejos de la batería.",
            IlustracionTutorial.Tipo.Reserva);
    }

    /// <summary>Al hablar con Maia: menú para repasar cada mecánica o pedir una pista.</summary>
    public static void Menu()
    {
        DialogoUI d = Dialogo;
        if (d == null) return;
        d.Encolar(new DialogoUI.Mensaje
        {
            titulo = "Maia",
            texto = "¿En qué te ayudo, Nilo?",
            acento = RelevoUI.Verde,
            opciones = ConCondensadores
                ? new[] { "Los condensadores", "La reserva de energía", "Las cajas y las placas", "Dame una pista", "Nada, gracias" }
                : new[] { "La reserva de energía", "Las cajas y las placas", "Dame una pista", "Nada, gracias" },
            alElegir = delegate (int i)
            {
                if (ConCondensadores)
                {
                    if (i == 0) { ExplicarCondensadores(); return; }
                    i--;
                }
                switch (i)
                {
                    case 0: ExplicarReserva(); break;
                    case 1: ExplicarCajas(); break;
                    case 2:
                        string pista = RelevoGame.Definicion != null ? RelevoGame.Definicion.pistaMaia : "";
                        Decir(pista, IlustracionTutorial.Tipo.Ninguna);
                        break;
                }
            }
        });
    }
}
