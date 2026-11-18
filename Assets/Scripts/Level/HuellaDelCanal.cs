using UnityEngine;

public enum DestinoCorriente { Ninguno, Mercado, Radio }

/// <summary>Elementos del puerto que cambian según el ramal elegido en la Lonja.</summary>
public sealed class HuellaDelCanal : MonoBehaviour
{
    [SerializeField] GameObject mercado;
    [SerializeField] GameObject radio;
    [SerializeField] int nivel;

    public static void Montar(int indice, LevelLayout mapa, Transform padre)
    {
        if (indice != 7 && indice != 8) return;
        var raiz = new GameObject("Rastros de la noche");
        raiz.transform.SetParent(padre, false);
        var huella = raiz.AddComponent<HuellaDelCanal>();
        huella.nivel = indice;
        var kit = KitRelevo.Activo;
        if (kit == null) return;
        Vector3 lonja = indice == 7 ? mapa.CentroTile(10, 0) : mapa.CentroTile(0, 10);
        Vector3 cabina = indice == 7 ? mapa.CentroTile(11, 13) : mapa.CentroTile(2, 10);
        var muelle = DioramaBuilder.CrearAncla("Lonja", raiz.transform, lonja);
        var puesto = DioramaBuilder.CrearAncla("Repetidor", raiz.transform, cabina);
        Objeto(kit.antenaMaquina, puesto, "Antena", new Vector3(0.65f, 0f, 0.3f), 1.6f);
        Objeto(kit.radio, puesto, "Receptor", new Vector3(-0.4f, 0f, -0.4f), 0.75f);
        Objeto(kit.barril, muelle, "Barril", new Vector3(0.8f, 0f, 0.2f), 0.95f);
        huella.mercado = DioramaBuilder.CrearAncla("Puesto alumbrado", muelle, Vector3.zero).gameObject;
        huella.radio = DioramaBuilder.CrearAncla("Emisora al aire", puesto, Vector3.zero).gameObject;
        Objeto(kit.caja, huella.mercado.transform, "Mercancía a salvo", new Vector3(-0.45f, 0f, -0.3f), 1.15f);
        Lampara(huella.mercado.transform, new Vector3(-0.3f, 1.2f, 0f), ClaveMaterial.Turquesa);
        Lampara(huella.radio.transform, new Vector3(0.65f, 1.65f, 0.3f), ClaveMaterial.Ambar);
    }

    static void Objeto(GameObject modelo, Transform padre, string nombre, Vector3 posicion, float dimension)
    {
        if (modelo == null) return;
        var tam = KitRelevo.Tamano(modelo);
        KitRelevo.ColocarCentrado(modelo, padre, nombre, posicion, 12f,
            Vector3.one * dimension / Mathf.Max(tam.x, tam.y, tam.z, 0.01f));
    }

    static void Lampara(Transform padre, Vector3 punta, ClaveMaterial color)
    {
        DioramaBuilder.CrearCilindro("Mástil", padre, new Vector3(punta.x, punta.y / 2f, punta.z), 0.08f, punta.y, RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        var lente = DioramaBuilder.CrearEsfera("Piloto", padre, punta, 0.45f, RelevoPalette.MaterialDe(color));
        var luz = lente.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = RelevoPalette.ColorDe(color);
        luz.range = 5f;
        luz.intensity = 2.2f;
    }

    void Start()
    {
        Mostrar(nivel == 8 ? SaveManager.Datos.ramalLonja : DestinoCorriente.Ninguno);
    }

    public void Mostrar(DestinoCorriente destino)
    {
        if (mercado != null) mercado.SetActive(destino == DestinoCorriente.Mercado);
        if (radio != null) radio.SetActive(destino == DestinoCorriente.Radio);
    }

    public static string NotaDeViaje(int indice, string texto)
    {
        if (indice != 7 && indice != 8) return texto;
        var nivelActual = RelevoGame.Nivel;
        var destino = indice == 7 && nivelActual != null && nivelActual.IndiceNivel == 7
            && nivelActual.RamalLonja != DestinoCorriente.Ninguno ? nivelActual.RamalLonja : SaveManager.Datos.ramalLonja;
        if (destino == DestinoCorriente.Ninguno) return texto;
        return Anotacion(destino);
    }

    public static string Anotacion(DestinoCorriente destino)
    {
        if (destino == DestinoCorriente.Mercado) return "El farol de la lonja sigue encendido. La mercancía del puesto está seca. En la radio solo se oye agua.";
        if (destino == DestinoCorriente.Radio) return "El piloto de la emisora sigue encendido. Del puesto solo queda un barril. Maia vuelve a anunciar la misma hora.";
        return "";
    }
}
