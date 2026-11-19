using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Carta de navegación compartida por la apertura, la pausa y el atlas.</summary>
public sealed class CartaDePuertos : MonoBehaviour
{
    static readonly Color Agua = new Color32(20, 51, 55, 255);
    static readonly Color Papel = new Color32(247, 235, 205, 255);
    static readonly Color Tinta = new Color32(35, 54, 53, 255);
    static readonly Color Gris = new Color32(72, 87, 78, 255);
    static readonly Color Oro = new Color32(242, 190, 103, 255);
    static readonly Color Crema = new Color32(227, 214, 182, 255);
    readonly Image[] filas = new Image[LevelLayouts.Cantidad];
    readonly Text[] numeros = new Text[LevelLayouts.Cantidad];
    readonly Text[] nombres = new Text[LevelLayouts.Cantidad];
    readonly Text[] marcas = new Text[LevelLayouts.Cantidad];
    const int PuertosPorTramo=4;
    static readonly string[] Tramos={ "Costa de Cobre", "Canales de la Lonja", "Muelles de Guardia" };
    readonly Image[] tramos=new Image[Tramos.Length];
    readonly Text[] rotulosTramo=new Text[Tramos.Length];
    Text titulo, leyenda, accion, estado, retorno, instrucciones;
    RawImage foto;
    Image boton, volver;
    RelevoInput entrada;
    Action<int> partir;
    Action cancelar, verPrologo;
    int origen, seleccion, primerFrame;
    bool confirmar, saliendo, terminado, flanco;
    public int Seleccion { get { return seleccion; } }

    public static CartaDePuertos Abrir(RelevoInput controles, int actual, bool nivelTerminado, Action<int> alPartir, Action alCancelar, Action alVerPrologo = null)
    {
        var raiz = new GameObject("Carta de puertos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;
        var escala = raiz.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1200, 760);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var carta = raiz.AddComponent<CartaDePuertos>();
        carta.origen = actual;
        carta.terminado = nivelTerminado;
        carta.seleccion = actual > 0 ? actual : SaveManager.PuertoParaContinuar();
        carta.partir = alPartir;
        carta.cancelar = alCancelar;
        carta.verPrologo = alVerPrologo;
        carta.entrada = controles;
        carta.primerFrame = Time.frameCount;
        carta.Dibujar();
        carta.Refrescar();
        return carta;
    }

    static Image Banda(Transform padre, string nombre, float x, float y, float ancho, float alto, Color color)
    {
        return RelevoUI.Franja(padre,nombre,Vector2.one * 0.5f,Vector2.one * 0.5f,
            new Vector2(x,y),new Vector2(ancho,alto),color);
    }

    static Text Rotulo(Transform padre, string nombre, float x, float y, float ancho, float alto, int puntos,
        Color color, TextAnchor alineacion = TextAnchor.MiddleLeft)
    {
        var t = RelevoUI.Texto(padre,nombre,Vector2.one * 0.5f,Vector2.one * 0.5f,
            new Vector2(x,y),new Vector2(ancho,alto),puntos,alineacion,color);
        t.fontStyle = FontStyle.Normal;
        return t;
    }

    void Dibujar()
    {
        var fondo = Banda(transform,"Agua",0,0,1200,760,Agua).rectTransform;
        fondo.anchorMin = Vector2.zero;
        fondo.anchorMax = Vector2.one;
        fondo.offsetMin = fondo.offsetMax = Vector2.zero;
        var hoja = Banda(transform,"Carta",0,-6,1112,672,Papel).transform;
        Banda(hoja,"Lomo",-544,0,4,672,Oro);
        var cabecera = Banda(hoja,"Cabecera",0,274,1112,124,Agua).transform;
        Rotulo(cabecera,"Juego",-477,31,108,24,18,Oro).text = "RELEVO";
        Rotulo(cabecera,"Carta",-290,-10,482,60,44,Papel).text = "Carta de puertos";
        int completos = 0, recuerdos = 0;
        for (int i=1; i<=filas.Length; i++)
        {
            if (SaveManager.NivelCompletado(i)) completos++;
            if (SaveManager.RecuerdosDelNivel(i)>0) recuerdos++;
        }
        Rotulo(cabecera,"Avance",340,-10,374,50,18,Papel,TextAnchor.MiddleRight).text =
            completos + " de " + filas.Length + " puertos restaurados\n" + recuerdos + " recuerdos en el cuaderno";
        for(int i=0;i<tramos.Length;i++)
        {
            tramos[i]=Banda(hoja,"Tramo "+(i+1),(i-1)*355,174,345,49,Crema);
            rotulosTramo[i]=Rotulo(tramos[i].transform,"Nombre",0,0,335,44,21,Tinta,TextAnchor.MiddleCenter);
            rotulosTramo[i].text=(i*PuertosPorTramo+1).ToString("00")+"–"+Mathf.Min((i+1)*PuertosPorTramo,filas.Length).ToString("00")+"   "+Tramos[i];
        }
        for (int i=0; i<filas.Length; i++)
        {
            int id=i+1;
            var fila=Banda(hoja,"Puerto "+id,-286,100-(i%PuertosPorTramo)*80,484,73,Crema);
            var mini=new GameObject("Vista de puerto",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
            mini.rectTransform.SetParent(fila.transform,false);
            mini.rectTransform.anchoredPosition=new Vector2(-180,0);
            mini.rectTransform.sizeDelta=new Vector2(110,63);
            mini.texture=Resources.Load<Texture2D>("Bitacora/Puerto"+id);
            mini.raycastTarget=false;
            filas[i]=fila;
            numeros[i]=Rotulo(fila.transform,"Número",-87,10,54,32,21,Gris,TextAnchor.MiddleCenter);
            numeros[i].text=id.ToString("00");
            nombres[i]=Rotulo(fila.transform,"Nombre",81,10,285,35,21,Tinta);
            nombres[i].resizeTextForBestFit=true; nombres[i].resizeTextMinSize=17; nombres[i].resizeTextMaxSize=21;
            nombres[i].text=DefinicionesPorDefecto.FichaDelCuaderno(id).tituloIntro;
            marcas[i]=Rotulo(fila.transform,"Estado",64,-18,340,23,15,Gris);
        }

        var marco=Banda(hoja,"Postal",252,45,546,189,Agua);
        var imagen=new GameObject("Fotografía",typeof(RectTransform),typeof(RawImage));
        foto=imagen.GetComponent<RawImage>();
        foto.raycastTarget=false;
        foto.rectTransform.SetParent(marco.transform,false);
        foto.rectTransform.anchorMin=Vector2.zero;
        foto.rectTransform.anchorMax=Vector2.one;
        foto.rectTransform.offsetMin=Vector2.one*3;
        foto.rectTransform.offsetMax=Vector2.one*-3;
        estado=Rotulo(hoja,"Registro",251,-67,546,26,16,Gris);
        titulo=Rotulo(hoja,"Puerto elegido",251,-111,546,48,30,Tinta);
        leyenda=Rotulo(hoja,"Detalle",251,-161,546,60,18,Tinta);
        boton=Banda(hoja,"Partir",252,-228,546,51,Agua);
        accion=Rotulo(boton.transform,"Acción",0,0,536,44,22,Papel,TextAnchor.MiddleCenter);
        volver=Banda(hoja,"Regresar",-286,-228,484,51,Crema);
        retorno=Rotulo(volver.transform,"Acción",0,0,468,44,19,Tinta,TextAnchor.MiddleCenter);
        Banda(hoja,"Filete",0,-269,1054,2,Crema);
        instrucciones=Rotulo(hoja,"Controles",0,-294,1044,38,16,Gris,TextAnchor.MiddleCenter);
    }

    bool Disponible(int id)
    {
        return id == origen || SaveManager.NivelDesbloqueado(id);
    }

    void Refrescar()
    {
        int tramo=(seleccion-1)/PuertosPorTramo;
        for(int j=0;j<tramos.Length;j++)
        {
            tramos[j].color=j==tramo?Agua:Crema;
            rotulosTramo[j].color=j==tramo?Oro:Tinta;
        }
        for (int i=0;i<filas.Length;i++)
        {
            filas[i].gameObject.SetActive(i/PuertosPorTramo==tramo);
            int n=i+1;
            bool elegido=n==seleccion;
            bool abierto=Disponible(n);
            filas[i].color=elegido?Agua:(abierto?new Color32(236,225,197,255):Crema);
            numeros[i].color=elegido?Oro:Gris;
            nombres[i].color=elegido?Papel:(abierto?Tinta:Gris);
            marcas[i].color=elegido?Oro:Gris;
            marcas[i].text=n==origen?"AQUÍ":(SaveManager.NivelCompletado(n)?"LISTO":(abierto?"ABIERTO":"CERRADO"));
        }
        var ficha=DefinicionesPorDefecto.FichaDelCuaderno(seleccion);
        var tex=Resources.Load<Texture2D>("Bitacora/Puerto"+seleccion);
        foto.texture=tex;
        foto.enabled=tex!=null;
        if(tex!=null)
        {
            float alto=Mathf.Min(1f,(float)tex.width/tex.height/(540f/183f));
            float ancho=Mathf.Min(1f,(540f/183f)*tex.height/tex.width);
            foto.uvRect=new Rect((1-ancho)*0.5f,(1-alto)*0.5f,ancho,alto);
        }
        bool abiertoSel=Disponible(seleccion);
        titulo.text=ficha.tituloIntro;
        estado.text="PUERTO "+seleccion.ToString("00")+" / "+filas.Length.ToString("00");
        bool enCurso=origen>0 && seleccion==origen && !terminado;
        var recuerdo=SaveManager.TieneRecuerdo(ficha.recuerdoIdentificador);
        leyenda.text=confirmar?"Al zarpar dejarás este intento. Los recuerdos recogidos quedan en tu cuaderno."
            :(!abiertoSel?"Restaura "+DefinicionesPorDefecto.FichaDelCuaderno(seleccion-1).tituloIntro+" para abrir este puerto."
            :(recuerdo?"En el cuaderno · "+ficha.recuerdoTitulo:"Queda un recuerdo por encontrar en este puerto."));
        if (confirmar) accion.text = "Confirmar viaje";
        else if (!abiertoSel) accion.text = "Puerto cerrado";
        else if (enCurso) accion.text = "Reanudar tramo";
        else if (origen == 0 && seleccion == 1 && !SaveManager.NivelCompletado(1)) accion.text = "Comenzar el viaje";
        else accion.text = SaveManager.NivelCompletado(seleccion) ? "Volver a este puerto" : "Continuar el viaje";
        boton.color=abiertoSel?Agua:Crema;
        accion.color=abiertoSel?Papel:Gris;
        bool repetir = verPrologo != null && SaveManager.NivelCompletado(1);
        volver.gameObject.SetActive(origen > 0 || repetir);
        retorno.text = repetir ? "Ver el prólogo" : (confirmar ? "Cancelar viaje" : "Volver");
        bool mando=Gamepad.current!=null && (Mouse.current==null || Gamepad.current.lastUpdateTime>Mouse.current.lastUpdateTime)
            && (Keyboard.current==null || Gamepad.current.lastUpdateTime>Keyboard.current.lastUpdateTime);
        instrucciones.text=mando?"↑ ↓ Puerto   ·   ← → Tramo   ·   A Aceptar"+(origen>0?"    ·    B para volver":"")
            :"↑ ↓  Puerto     ·     ← →  Tramo     ·     Enter  Aceptar"+(origen>0?"      ·      Esc  Volver":"");
        if (repetir) instrucciones.text += mando ? "    ·    Botón oeste para el prólogo" : "      ·      P  Prólogo";
    }

    void Update()
    {
        if (saliendo || Time.frameCount==primerFrame) return;
        var raton=Mouse.current;
        if(raton!=null && raton.leftButton.wasPressedThisFrame)
        {
            var pos=raton.position.ReadValue();
            if (volver.gameObject.activeSelf && Contiene(volver,pos)) { AccionSecundaria(); return; }
            if (Contiene(boton,pos)) { Aceptar(); return; }
            if(!confirmar)
                for(int j=0;j<tramos.Length;j++)
                    if(Contiene(tramos[j],pos)){Seleccionar(Mathf.Min(j*PuertosPorTramo+1+(seleccion-1)%PuertosPorTramo,filas.Length));return;}
            if (!confirmar)
                for(int i=0;i<filas.Length;i++)
                    if(filas[i].gameObject.activeSelf && Contiene(filas[i],pos)) { Seleccionar(i+1); return; }
        }
        if(!confirmar && raton!=null && Mathf.Abs(raton.scroll.ReadValue().y)>0.01f)
        { Seleccionar(Mathf.Clamp(seleccion+(raton.scroll.ReadValue().y<0?1:-1),1,filas.Length)); return; }
        var teclado=Keyboard.current;
        var mando=Gamepad.current;
        if (verPrologo != null && SaveManager.NivelCompletado(1) &&
            ((teclado != null && teclado.pKey.wasPressedThisFrame) || (mando != null && mando.buttonWest.wasPressedThisFrame)))
        { AccionSecundaria(); return; }
        if((entrada!=null && (entrada.Pulsado(entrada.Cancelar)||entrada.Pulsado(entrada.Pausa))) ||
            (teclado!=null && teclado.escapeKey.wasPressedThisFrame) || (mando!=null && mando.buttonEast.wasPressedThisFrame))
        { Atras(); return; }
        if((entrada!=null && entrada.Pulsado(entrada.Confirmar)) || (teclado!=null && teclado.enterKey.wasPressedThisFrame)
            || (mando!=null && mando.buttonSouth.wasPressedThisFrame)) { Aceptar(); return; }
        float eje=entrada!=null?entrada.LeerMovimiento().y:0;
        if (teclado!=null && teclado.upArrowKey.isPressed) eje=1;
        if (teclado!=null && teclado.downArrowKey.isPressed) eje=-1;
        if (mando!=null && Mathf.Abs(mando.dpad.y.ReadValue())>0.5f) eje=mando.dpad.y.ReadValue();
        float horizontal=entrada!=null ? entrada.LeerMovimiento().x : 0;
        if(teclado!=null && teclado.leftArrowKey.isPressed)horizontal=-1;
        if(teclado!=null && teclado.rightArrowKey.isPressed)horizontal=1;
        if(mando!=null && Mathf.Abs(mando.dpad.x.ReadValue())>0.5f)horizontal=mando.dpad.x.ReadValue();
        if(Mathf.Abs(horizontal)>0.5f && Mathf.Abs(horizontal)>Mathf.Abs(eje))
        {
            if(!flanco && !confirmar)
            {
                int tramo=Mathf.Clamp((seleccion-1)/PuertosPorTramo+(horizontal>0?1:-1),0,tramos.Length-1);
                Seleccionar(Mathf.Min(tramo*PuertosPorTramo+(seleccion-1)%PuertosPorTramo+1,filas.Length));
            }
            flanco=true; return;
        }
        if (Mathf.Abs(eje)<0.45f) { flanco=false; return; }
        if (!flanco && !confirmar) Seleccionar(Mathf.Clamp(seleccion+(eje>0?-1:1),1,filas.Length));
        flanco=true;
    }

    static bool Contiene(Image imagen, Vector2 pantalla)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(imagen.rectTransform,pantalla,null);
    }

    void Seleccionar(int puerto)
    {
        seleccion=puerto;
        Refrescar();
    }

    void AccionSecundaria()
    {
        if (verPrologo == null) { Atras(); return; }
        saliendo = true;
        verPrologo();
    }

    void Atras()
    {
        if(confirmar) { confirmar=false; Refrescar(); return; }
        if(cancelar!=null) { saliendo=true; cancelar(); }
    }

    void Aceptar()
    {
        if(saliendo || !Disponible(seleccion)) return;
        if(origen>0 && origen!=seleccion && !terminado && !confirmar)
        {
            confirmar=true;
            Refrescar();
            return;
        }
        if(!Application.CanStreamedLevelBeLoaded(LevelManager.NombreEscenaNivel(seleccion)))
        {
            leyenda.text="Este puerto no está disponible en esta instalación.";
            return;
        }
        saliendo=true;
        partir(seleccion);
    }
}

