using UnityEngine;
using UnityEngine.UI;

/// <summary>Indicador de transporte del magnetófono y de sus dos contactos.</summary>
public sealed class MonitorDeCinta : MonoBehaviour
{
    static readonly Color Azul = new Color32(20,49,52,245);
    static readonly Color Papel = new Color32(247,238,210,255);
    static readonly Color Laton = new Color32(241,191,112,255);
    static readonly Color Verde = new Color32(91,224,196,255);
    static readonly Color Rojo = new Color32(251,129,99,255);
    static readonly Color Apagado = new Color32(64,90,90,255);
    Text modo, instruccion, reloj, controles;
    Image cursor, luzA, luzB;
    int decima = -1;
    bool mandoAnterior;

    public static MonitorDeCinta Crear(Transform padre)
    {
        var go = new GameObject("Panel de cinta",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        go.transform.SetParent(padre,false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = go.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1440,900);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var hud = go.AddComponent<MonitorDeCinta>();
        var superior=RelevoUI.Franja(go.transform,"Cabecera",new Vector2(0,1),new Vector2(0,1),new Vector2(22,-18),new Vector2(370,70),Azul);
        var cab=superior.transform;
        Texto(cab,"Lugar",0,17,334,21,14,Laton).text="LIRIA  /  CABINA DE RELEVOS";
        Texto(cab,"Título",0,-9,334,33,26,Papel).text="Eco de turno";

        var pie=RelevoUI.Franja(go.transform,"Transporte",new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0,16),new Vector2(1010,90),Azul);
        var p=pie.transform;
        hud.modo=Texto(p,"Modo",-361,16,240,32,20,Papel);
        hud.reloj=Texto(p,"Contador",-115,16,96,30,19,Laton);
        hud.instruccion=Texto(p,"Acción",64,-18,658,34,19,Papel);
        hud.controles=Texto(p,"Rebobinado",-383,-18,196,32,14,Laton);
        hud.controles.text="R  ·  Rebobinar";
        RelevoUI.Franja(p,"Cinta",Vector2.one*0.5f,Vector2.one*0.5f,new Vector2(86,17),new Vector2(276,5),Apagado);
        for(int i=0;i<=6;i++)
            RelevoUI.Franja(p,"Segundo",Vector2.one*0.5f,Vector2.one*0.5f,new Vector2(-52+i*46,22),new Vector2(2,8),Laton);
        hud.cursor=RelevoUI.Franja(p,"Cabezal",Vector2.one*0.5f,Vector2.one*0.5f,new Vector2(-52,17),new Vector2(8,20),Verde);
        hud.luzA=Testigo(p,403,"I");
        hud.luzB=Testigo(p,453,"II");
        return hud;
    }

    static Text Texto(Transform p,string n,float x,float y,float w,float h,int size,Color color)
    {
        var t=RelevoUI.Texto(p,n,Vector2.one*0.5f,Vector2.one*0.5f,new Vector2(x,y),new Vector2(w,h),size,TextAnchor.MiddleLeft,color);
        t.font=size>=24?RelevoUI.FuenteTitulo():RelevoUI.Fuente();
        return t;
    }

    static Image Testigo(Transform p,float x,string marca)
    {
        var img=RelevoUI.Franja(p,"Contacto "+marca,Vector2.one*0.5f,Vector2.one*0.5f,new Vector2(x,10),new Vector2(38,38),Apagado);
        var t=Texto(img.transform,"Trazo",0,0,38,38,20,Papel);
        t.alignment=TextAnchor.MiddleCenter;
        t.text=marca;
        return img;
    }

    public void Mostrar(string estado,string accion,bool grabando,bool eco,float fraccion,bool a,bool b,bool mando,float segundos)
    {
        if (modo.text!=estado) modo.text=estado;
        if (instruccion.text!=accion) instruccion.text=accion;
        modo.color=grabando?Rojo:(eco?Verde:Laton);
        cursor.color=modo.color;
        cursor.rectTransform.anchoredPosition=new Vector2(-52f+276f*fraccion,17f);
        int valor=Mathf.RoundToInt(segundos*10f);
        if (valor!=decima)
        {
            decima=valor;
            reloj.text=(valor/10)+"."+valor%10+" s";
        }
        luzA.color=a?new Color32(25,137,129,255):Apagado;
        luzB.color=b?new Color32(25,137,129,255):Apagado;
        if(mando!=mandoAnterior)
        {
            mandoAnterior=mando;
            controles.text=mando?"Select  ·  Rebobinar":"R  ·  Rebobinar";
        }
    }
}

