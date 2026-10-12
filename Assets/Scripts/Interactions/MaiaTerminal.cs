using UnityEngine;

/// <summary>
/// Maia, el terminal de la red de Liria. Da el mensaje de presentación, pistas según
/// el estado del nivel y el cierre. Nunca bloquea: siempre se puede hablar con ella.
/// </summary>
public class MaiaTerminal : MonoBehaviour, IInteractable
{
    public string TextoIntroduccion = "";
    public string TextoPista = "";
    public string TextoFinal = "";

    public bool PresentacionMostrada { get; private set; }

    [SerializeField] Transform ancla;

    public Transform Ancla { get { return ancla != null ? ancla : transform; } }
    public bool Disponible { get { return true; } }
    public string Verbo { get { return "Hablar con Maia"; } }
    public string Motivo { get { return ""; } }

    void OnEnable() { PlayerInteractor.Registrar(this); }
    void OnDisable() { PlayerInteractor.Desregistrar(this); }

    public void Configurar(string introduccion, string pista, string final, Transform ancla)
    {
        TextoIntroduccion = introduccion;
        TextoPista = pista;
        TextoFinal = final;
        this.ancla = ancla;
        PresentacionMostrada = false;
    }

    /// <summary>Marca la presentación como vista (la intro también se muestra al empezar el nivel).</summary>
    public void MarcarPresentacion() { PresentacionMostrada = true; }

    public string TextoActual()
    {
        if (!PresentacionMostrada) return TextoIntroduccion;
        if (RelevoGame.Nivel != null && RelevoGame.Nivel.TodasLasPalancasAccionadas) return TextoFinal;
        return TextoPista;
    }

    public void Interactuar(PlayerController jugador)
    {
        PresentacionMostrada = true;
        AudioDirector.Pedir(Tono.Mensaje);

        // En el Patio de Bombas, Maia abre un menú para repasar las mecánicas nuevas.
        if (RelevoGame.Definicion != null && RelevoGame.Definicion.tutorialPatio)
        {
            TutorialMaia.Menu();
            return;
        }

        GameEvents.RaiseMensajeMostrado("Maia", TextoActual());
    }
}
