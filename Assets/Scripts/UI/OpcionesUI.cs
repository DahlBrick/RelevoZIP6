using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Subpanel de Opciones de la pausa: volumen general, pantalla completa y volver.
///
/// Igual que el resto de la interfaz, no crea Canvas ni EventSystem ni se hace persistente: lo
/// construye UIManager DENTRO del lienzo del HUD (encima del panel de pausa) y es UIManager quien
/// le pasa la entrada, así que en la escena sigue habiendo un solo lector del asset de entrada.
///
/// Los ajustes se guardan en PlayerPrefs con claves propias del juego. No toca AudioDirector ni
/// ningún script de audio: el volumen general se aplica a AudioListener, que es el nivel maestro
/// por encima de las fuentes concretas.
/// </summary>
public class OpcionesUI : MonoBehaviour
{
    const string ClaveVolumen = "relevo_volumen";
    const string ClavePantalla = "relevo_pantalla";

    const float AnchoTarjeta = 820f;
    const float AltoTarjeta = 520f;

    const int PasosVolumen = 10;

    /// <summary>Lo engancha UIManager para volver a la pausa.</summary>
    public System.Action AlCerrar;

    public bool Abierto { get; private set; }

    static readonly string[] Nombres = { "Volumen general", "Pantalla", "Volver" };

    class Fila
    {
        public Image fondo;
        public Image filo;
        public Text etiqueta;
        public Text valor;
    }

    GameObject panel;
    Fila[] filas;
    Text nota;

    int seleccion;
    int volumenEscalon;
    bool pantallaCompleta;
    bool flanco;

    enum Opcion { Volumen = 0, Pantalla = 1, Volver = 2 }

    // ------------------------------------------------------------------ API

    public void Construir(Transform lienzo)
    {
        if (panel != null || lienzo == null) return;

        volumenEscalon = Mathf.Clamp(PlayerPrefs.GetInt(ClaveVolumen, Mathf.RoundToInt(AudioListener.volume * PasosVolumen)),
            0, PasosVolumen);
        pantallaCompleta = PlayerPrefs.GetInt(ClavePantalla, Screen.fullScreen ? 1 : 0) == 1;

        panel = RelevoUI.Panel(lienzo, "PanelOpciones", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(4000f, 4000f), RelevoUI.Velo);

        GameObject tarjeta = RelevoUI.Panel(panel.transform, "Tarjeta opciones", new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(AnchoTarjeta, AltoTarjeta), RelevoUI.FondoHondo);
        RelevoUI.Marco(tarjeta.transform, RelevoUI.CobreTenue, 2f);
        RelevoUI.Esquinas(tarjeta.transform, 34f, 2f, RelevoUI.CobreTenue);

        RelevoUI.Franja(tarjeta.transform, "Filo superior", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -16f), new Vector2(AnchoTarjeta - 80f, 2f), RelevoUI.CobreTenue);

        Text titulo = RelevoUI.Texto(tarjeta.transform, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -40f), new Vector2(AnchoTarjeta - 80f, 40f), 34, TextAnchor.MiddleCenter, RelevoUI.Tinta);
        titulo.fontStyle = FontStyle.Bold;
        titulo.text = "OPCIONES";

        Text subtitulo = RelevoUI.Texto(tarjeta.transform, "Subtitulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -86f), new Vector2(AnchoTarjeta - 80f, 26f), 17, TextAnchor.MiddleCenter, RelevoUI.Turquesa);
        subtitulo.text = "radio y energía de Liria";

        filas = new Fila[Nombres.Length];

        for (int i = 0; i < Nombres.Length; i++)
        {
            filas[i] = CrearFila(tarjeta.transform, i, Nombres[i]);
        }

        nota = RelevoUI.Texto(tarjeta.transform, "Nota", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 74f), new Vector2(AnchoTarjeta - 80f, 24f), 15, TextAnchor.MiddleCenter,
            RelevoUI.ConAlfa(RelevoUI.Tinta, 0.55f));
        nota.text = "← →  cambiar      ·      Esc  volver a la pausa";

        panel.SetActive(false);
    }

    public void Abrir()
    {
        if (panel == null) return;

        Abierto = true;
        seleccion = 0;
        flanco = false;
        Refrescar();
        RelevoUIAnim.Entrada(panel, 0.16f, 0f, true);
    }

    public void Cerrar()
    {
        Abierto = false;
        if (panel == null) return;
        RelevoUIAnim.Salida(panel, 0.14f, null);
    }

    /// <summary>UIManager le cede la entrada mientras el subpanel está abierto.</summary>
    public void Manejar(RelevoInput entrada)
    {
        if (entrada == null || !Abierto) return;

        if (entrada.Pulsado(entrada.Cancelar) || entrada.Pulsado(entrada.Pausa))
        {
            Volver();
            return;
        }

        if (entrada.Pulsado(entrada.Confirmar) && seleccion == (int)Opcion.Volver)
        {
            Volver();
            return;
        }

        Vector2 mover = entrada.LeerMovimiento();
        bool vertical = Mathf.Abs(mover.y) >= Mathf.Abs(mover.x);
        float eje = vertical ? mover.y : mover.x;

        if (Mathf.Abs(eje) < 0.5f)
        {
            flanco = false;
            return;
        }

        if (flanco) return;
        flanco = true;

        if (vertical)
        {
            seleccion += eje > 0f ? -1 : 1;
            if (seleccion < 0) seleccion = Nombres.Length - 1;
            if (seleccion >= Nombres.Length) seleccion = 0;
            Refrescar();
            return;
        }

        Cambiar(eje > 0f ? 1 : -1);
    }

    // ------------------------------------------------------------------ interno

    Fila CrearFila(Transform padre, int indice, string nombre)
    {
        Fila fila = new Fila();

        fila.fondo = RelevoUI.Franja(padre, "Fila " + indice, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -150f - indice * 78f), new Vector2(AnchoTarjeta - 80f, 66f), RelevoUI.FondoSeleccion);

        fila.filo = RelevoUI.Franja(fila.fondo.transform, "Filo", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(4f, 66f), RelevoUI.Cobre);

        fila.etiqueta = RelevoUI.Texto(fila.fondo.transform, "Etiqueta", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(28f, 0f), new Vector2(440f, 34f), 22, TextAnchor.MiddleLeft, RelevoUI.Tinta);
        fila.etiqueta.text = nombre;

        fila.valor = RelevoUI.Texto(fila.fondo.transform, "Valor", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-28f, 0f), new Vector2(400f, 34f), 20, TextAnchor.MiddleRight, RelevoUI.Ambar);

        return fila;
    }

    void Cambiar(int paso)
    {
        switch ((Opcion)seleccion)
        {
            case Opcion.Volumen:
                volumenEscalon = Mathf.Clamp(volumenEscalon + paso, 0, PasosVolumen);
                AudioListener.volume = volumenEscalon / (float)PasosVolumen;
                PlayerPrefs.SetInt(ClaveVolumen, volumenEscalon);
                PlayerPrefs.Save();
                break;

            case Opcion.Pantalla:
                pantallaCompleta = !pantallaCompleta;
                Screen.fullScreen = pantallaCompleta;
                PlayerPrefs.SetInt(ClavePantalla, pantallaCompleta ? 1 : 0);
                PlayerPrefs.Save();
                break;
        }

        Refrescar();
    }

    void Refrescar()
    {
        for (int i = 0; i < filas.Length; i++)
        {
            bool elegida = i == seleccion;

            filas[i].fondo.color = elegida ? RelevoUI.FondoAlto : RelevoUI.FondoSeleccion;
            filas[i].filo.color = elegida ? RelevoUI.Cobre : RelevoUI.ConAlfa(RelevoUI.Cobre, 0.25f);
            filas[i].etiqueta.color = elegida ? RelevoUI.Tinta : RelevoUI.ConAlfa(RelevoUI.Tinta, 0.7f);
        }

        filas[(int)Opcion.Volumen].valor.text = volumenEscalon * 10 + " %";
        filas[(int)Opcion.Volumen].valor.color = seleccion == (int)Opcion.Volumen
            ? RelevoUI.Ambar
            : RelevoUI.ConAlfa(RelevoUI.Ambar, 0.7f);

        filas[(int)Opcion.Pantalla].valor.text = pantallaCompleta ? "completa" : "ventana";
        filas[(int)Opcion.Pantalla].valor.color = seleccion == (int)Opcion.Pantalla
            ? RelevoUI.Ambar
            : RelevoUI.ConAlfa(RelevoUI.Ambar, 0.7f);

        filas[(int)Opcion.Volver].valor.text = seleccion == (int)Opcion.Volver ? "Enter" : "";
    }

    void Volver()
    {
        Cerrar();
        if (AlCerrar != null) AlCerrar();
    }
}
