using UnityEngine;
using UnityEngine.UI;

/// <summary>Láminas del inventario de Liria, ordenadas por puerto en un único pliego.</summary>
public static class LaminaDeLiria
{
    static Texture2D pliego;
    public static RawImage Crear(Transform padre,Vector2 ancla,Vector2 pivote,Vector2 posicion,float lado)
    {
        if (pliego==null) pliego=Resources.Load<Texture2D>("Bitacora/LaminasDelCanal");
        var go=new GameObject("Lámina del recuerdo",typeof(RectTransform),typeof(RawImage));
        var tinta=go.GetComponent<RawImage>();
        var r=tinta.rectTransform;
        r.SetParent(padre,false); r.anchorMin=r.anchorMax=ancla; r.pivot=pivote;
        r.anchoredPosition=posicion; r.sizeDelta=new Vector2(lado,lado);
        tinta.texture=pliego; tinta.raycastTarget=false;
        return tinta;
    }

    public static void Mostrar(RawImage imagen,int puerto)
    {
        if(imagen==null)return;
        int i=puerto-1;
        imagen.enabled=imagen.texture!=null && i>=0 && i<12;
        if(imagen.enabled)imagen.uvRect=new Rect((i%4)/4f,(2-i/4)/3f,0.25f,1f/3f);
    }

    public static void Miniatura(Transform entrada,Text numero,int puerto)
    {
        var marco=RelevoUI.Franja(entrada,"Montura de papel",new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(15,0),new Vector2(98,98),new Color32(170,143,98,255));
        var lamina=Crear(marco.transform,Vector2.one*0.5f,Vector2.one*0.5f,Vector2.zero,92);
        Mostrar(lamina,puerto);
        var placa=RelevoUI.Franja(marco.transform,"Folio",new Vector2(1,0),new Vector2(1,0),Vector2.zero,new Vector2(37,24),new Color32(26,63,63,255));
        numero.rectTransform.SetParent(placa.transform,false);
        numero.rectTransform.anchorMin=numero.rectTransform.anchorMax=numero.rectTransform.pivot=Vector2.one*0.5f;
        numero.rectTransform.anchoredPosition=Vector2.zero;
        numero.rectTransform.sizeDelta=new Vector2(37,24);
        numero.fontSize=17;
    }

    public static string PistaDeHallazgo(int nivel)
    {
        if(nivel==11)return "Busca en el secadero de madera, al sur de los bancales.";
        if(nivel==12)return "El almacén del badajo queda tras la reja, al oeste de la fundición.";
        var plano=LevelLayouts.Obtener(nivel);
        var punto=plano.Casilla(plano.PrimerMarcador('X',Vector3.zero));
        float x=(punto.x+0.5f)/plano.Columnas, y=(punto.y+0.5f)/plano.FilasCuenta;
        string vertical=y<0.34f?"norte":(y>0.66f?"sur":"");
        string horizontal=x<0.34f?"oeste":(x>0.66f?"este":"");
        if(vertical!="" && horizontal!="")return "Busca el destello en la esquina "+(vertical=="norte"?(horizontal=="este"?"noreste":"noroeste"):(horizontal=="este"?"sureste":"suroeste"))+" del puerto.";
        string lado=vertical!=""?vertical:horizontal;
        return lado==""?"Busca el destello en los muelles centrales.":"Busca el destello en el muelle del "+lado+".";
    }
}
