using UnityEngine;

/// <summary>
/// Recuerdo opcional en un desvío breve. Al examinarlo se guarda en el cuaderno de
/// viaje y su texto se muestra como mensaje. Nunca bloquea la salida.
/// </summary>
public class MemoryInteractable : MonoBehaviour, IInteractable
{
    public string Identificador = "nivel1_recuerdo";
    public string Titulo = "Recuerdo de Liria";
    public string Texto = "";

    [Header("Conexiones")]
    [SerializeField] Transform visual;
    [SerializeField] Transform ancla;

    Vector3 posicionBase;
    bool recogido;
    float fase;

    public bool Recogido { get { return recogido; } }
    public Transform Ancla { get { return ancla != null ? ancla : transform; } }
    public bool Disponible { get { return !recogido; } }
    public string Verbo { get { return "Examinar recuerdo"; } }
    public string Motivo { get { return "Ya guardaste este recuerdo"; } }

    void OnEnable() { PlayerInteractor.Registrar(this); }
    void OnDisable() { PlayerInteractor.Desregistrar(this); }

    public void Configurar(string identificador, string titulo, string texto, Transform visual, Transform ancla)
    {
        Identificador = identificador;
        Titulo = titulo;
        Texto = texto;
        this.visual = visual;
        this.ancla = ancla;
        recogido = false;

        if (visual != null) posicionBase = visual.localPosition;
    }

    /// <summary>
    /// Reengancha la animación del orbe al cargar una escena horneada: 'visual' llega
    /// serializado, pero la posición base y el vaivén son estado de ejecución.
    /// </summary>
    public void Vincular()
    {
        recogido = false;
        fase = 0f;

        if (visual == null) return;

        posicionBase = visual.localPosition;
        visual.localRotation = Quaternion.identity;
        visual.gameObject.SetActive(true);
    }

    void Update()
    {
        if (recogido || visual == null) return;

        fase += Time.deltaTime;
        visual.localPosition = posicionBase + new Vector3(0f, Mathf.Sin(fase * 1.6f) * 0.12f, 0f);
        visual.localRotation = Quaternion.Euler(0f, fase * 35f, 0f);
    }

    public void Interactuar(PlayerController jugador)
    {
        if (recogido) return;

        recogido = true;
        if (visual != null) visual.gameObject.SetActive(false);
        PlayerInteractor.Desregistrar(this);

        AudioDirector.Pedir(Tono.Recuerdo);
        if (RelevoGame.Nivel != null) RelevoGame.Nivel.RegistrarRecuerdo(this);
    }
}
