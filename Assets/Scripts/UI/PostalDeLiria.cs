using UnityEngine;
using UnityEngine.UI;

/// <summary>Ficha de puerto con una fotografía tomada desde la cámara de su propia escena.</summary>
public sealed class PostalDeLiria : MonoBehaviour
{
    RawImage foto, lamina;
    Text serie, lugar;
    int nivel;
    static readonly Color Petrol = new Color32(25, 64, 65, 255);
    static readonly Color Marfil = new Color32(255, 242, 208, 255);
    static readonly Color Laton = new Color32(240, 186, 99, 255);

    public static PostalDeLiria Crear(Image soporte,bool inventario=false)
    {
        var postal = soporte.gameObject.AddComponent<PostalDeLiria>();
        soporte.color = Petrol;
        var imagen = new GameObject("Vista del puerto", typeof(RectTransform), typeof(RawImage));
        postal.foto = imagen.GetComponent<RawImage>();
        postal.foto.raycastTarget = false;
        var rt = postal.foto.rectTransform;
        rt.SetParent(soporte.transform, false);
        rt.anchorMin = new Vector2(0.39f, 0f);
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0, 3);
        rt.offsetMax = new Vector2(-3, -3);
        var origen = new Vector2(0f, 1f);
        postal.serie = RelevoUI.Texto(soporte.transform, "Índice", origen, origen, new Vector2(16f,-16f),
            new Vector2(188f, 25f), 16, TextAnchor.UpperLeft, Laton);
        postal.lugar = RelevoUI.Texto(soporte.transform, "Puerto", new Vector2(0,0.5f), new Vector2(0,0.5f),
            new Vector2(16f,-4f), new Vector2(182f, 95f), 26, TextAnchor.MiddleLeft, Marfil);
        postal.lugar.font = RelevoUI.FuenteTitulo();
        postal.lugar.resizeTextForBestFit = true;
        postal.lugar.resizeTextMinSize = 19;
        postal.lugar.resizeTextMaxSize = 26;
        RelevoUI.Franja(soporte.transform, "Línea de latón", Vector2.zero, Vector2.zero, new Vector2(16f,14f),
            new Vector2(55f,3f), Laton);
        if (inventario)
        {
            float lado = Mathf.Min(soporte.rectTransform.sizeDelta.y-8f,soporte.rectTransform.sizeDelta.x*0.37f);
            postal.lamina=LaminaDeLiria.Crear(soporte.transform,new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(4,0),lado);
            rt.anchorMin=new Vector2(0,0);
            rt.offsetMin=new Vector2(lado+8,4);
            rt.offsetMax=new Vector2(-4,-4);
            var pie=RelevoUI.Franja(soporte.transform,"Pie de fotografía",new Vector2(1,0),new Vector2(1,0),new Vector2(-4,4),new Vector2(soporte.rectTransform.sizeDelta.x-lado-12,34),new Color32(22,52,54,246));
            postal.lugar.rectTransform.SetParent(pie.transform,false);
            postal.lugar.rectTransform.anchorMin=postal.lugar.rectTransform.anchorMax=postal.lugar.rectTransform.pivot=new Vector2(0,0.5f);
            postal.lugar.rectTransform.anchoredPosition=new Vector2(11,0);
            postal.lugar.rectTransform.sizeDelta=new Vector2(pie.rectTransform.sizeDelta.x-18,32);
            postal.lugar.resizeTextMinSize=16; postal.lugar.resizeTextMaxSize=22;
            postal.serie.gameObject.SetActive(false);
        }
        return postal;
    }

    public void Mostrar(LevelDefinition ficha)
    {
        if (ficha == null || nivel == ficha.indice) return;
        nivel = ficha.indice;
        LaminaDeLiria.Mostrar(lamina,nivel);
        serie.text = "LIRIA   /   " + nivel.ToString("00");
        lugar.text = ficha.tituloIntro;
        var tex = Resources.Load<Texture2D>("Bitacora/Puerto" + nivel);
        foto.texture = tex;
        foto.enabled = tex != null;
        if (tex == null) return;
        var rect = foto.rectTransform.rect;
        float aspect = rect.width / Mathf.Max(1,rect.height);
        float uvX = Mathf.Min(1f, aspect * tex.height / tex.width);
        float uvY = Mathf.Min(1f, tex.width / (aspect * tex.height));
        foto.uvRect = new Rect((1-uvX)*0.5f,(1-uvY)*0.5f,uvX,uvY);
    }
}
