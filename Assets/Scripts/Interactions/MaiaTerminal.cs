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

    // La radio se apaga poco a poco a medida que Nilo recuerda (ver Historia.Interferencia):
    // sus luces bajan y, desde el Puerto 7, parpadean como una emisora que pierde la señal.
    Light[] luces;
    float[] intensidadBase;
    int interferencia;

    void Start()
    {
        int puerto = RelevoGame.Nivel != null ? RelevoGame.Nivel.IndiceNivel : 1;
        interferencia = Historia.Interferencia(puerto);
        if (GetComponentInChildren<Light>(true) == null)
        {
            var piloto = new GameObject("Piloto de la radio").AddComponent<Light>();
            piloto.transform.SetParent(transform, false);
            piloto.transform.position = Ancla.position + Vector3.up * 1.4f;
            piloto.type = LightType.Point;
            piloto.color = RelevoUI.Verde;
            piloto.range = 3.2f;
            piloto.intensity = 2.2f;
            piloto.shadows = LightShadows.None;
        }
        luces = GetComponentsInChildren<Light>(true);
        intensidadBase = new float[luces.Length];
        for (int i = 0; i < luces.Length; i++) intensidadBase[i] = luces[i].intensity;
    }

    void Update()
    {
        if (interferencia == 0 || luces == null) return;
        float brillo = 1f - 0.17f * interferencia;
        if (interferencia >= 2)
        {
            // Cortes breves y aleatorios, más frecuentes cuanto peor es la señal.
            float ruido = Mathf.PerlinNoise(Time.time * (2f + interferencia), 0.37f);
            if (ruido < 0.12f * interferencia) brillo *= 0.15f;
        }
        for (int i = 0; i < luces.Length; i++)
            if (luces[i] != null) luces[i].intensity = intensidadBase[i] * brillo;
    }

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
        if (RelevoGame.Nivel != null && RelevoGame.Nivel.IndiceNivel >= 4 && RelevoGame.Nivel.IndiceNivel <= LevelLayouts.Cantidad)
            return RelevoGame.Nivel.PistaActual();
        if (RelevoGame.Nivel != null && RelevoGame.Nivel.TodasLasPalancasAccionadas) return TextoFinal;
        return TextoPista;
    }

    public void Interactuar(PlayerController jugador)
    {
        PresentacionMostrada = true;
        AudioDirector.Pedir(Tono.Mensaje);

        if (RelevoGame.Nivel != null && RelevoGame.Nivel.Turno != null)
        {
            TutorialMaia.MenuGuardia();
            return;
        }

        // En el Patio de Bombas, Maia abre un menú para repasar las mecánicas nuevas.
        if (RelevoGame.Definicion != null && (RelevoGame.Definicion.tutorialPatio || RelevoGame.Definicion.tutorialCondensadores))
        {
            TutorialMaia.Menu();
            return;
        }

        GameEvents.RaiseMensajeMostrado("Maia", TextoActual());
    }
}
