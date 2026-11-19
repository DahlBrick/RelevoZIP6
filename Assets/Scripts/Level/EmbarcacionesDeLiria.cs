using System.Collections.Generic;
using UnityEngine;

/// <summary>Barcos de trabajo amarrados en claros de agua de los ocho puertos.</summary>
public static class EmbarcacionesDeLiria
{
    public const string Grupo = "Embarcación del puerto";
    static readonly Vector3[] Amarres =
    {
        new Vector3(5, 7.5f, -9), new Vector3(10.5f, 6, 2), new Vector3(2.5f, 8, 75),
        new Vector3(7, 4, -7), new Vector3(2.5f, 2, 65), new Vector3(2, 11, 15),
        new Vector3(4.5f, 12, 75), new Vector3(13, 8, -7)
    };
    static readonly string[] Nombres = { "La barquera", "Draga del patio", "Lancha de guardia", "Correo de la aduana",
        "Barca del cordelero", "Grúa de la esclusa", "Puesto de la lonja", "Radio del archivo" };
    static Mesh casco;

    static Material M(ClaveMaterial clave) { return RelevoPalette.MaterialDe(clave); }

    public static Transform Montar(int indice, LevelLayout mapa, Transform padre)
    {
        if (indice < 1 || indice > Amarres.Length || padre.Find(Grupo) != null) return null;
        var kit = KitRelevo.Activo;
        var raiz = DioramaBuilder.CrearAncla(Grupo, padre, Vector3.zero);
        var amarre = Amarres[indice - 1];
        var barca = DioramaBuilder.CrearAncla(Nombres[indice - 1], raiz, (mapa.CentroTile(amarre.x, 0) + Vector3.back * (amarre.y * mapa.TamanoTile)) + Vector3.down * 0.45f);
        barca.localRotation = Quaternion.Euler(0, amarre.z, 0);
        barca.localScale = Vector3.one * (indice == 2 ? 0.84f : 1.1f);
        barca.gameObject.AddComponent<Flotador>().Configurar(0.055f, 4.7f, 1.8f, 2.1f);
        var pintura = indice == 2 || indice == 3 || indice == 8 ? ClaveMaterial.NiloExtremidades : ClaveMaterial.MaiaCarcasa;
        var cascoGo = new GameObject("Casco pintado", typeof(MeshFilter), typeof(MeshRenderer));
        cascoGo.transform.SetParent(barca, false);
        cascoGo.GetComponent<MeshFilter>().sharedMesh = MallaCasco();
        cascoGo.GetComponent<MeshRenderer>().sharedMaterials = new[] { M(pintura), M(ClaveMaterial.PiedraCrema), M(ClaveMaterial.MaderaOscura) };
        for (int i = -3; i <= 3; i++)
            Caja(barca, "Junta de cubierta", 0, 0.345f, i * 0.46f, i == 3 ? 1.35f : 1.66f, 0.015f, 0.032f, ClaveMaterial.Cobre);

        switch (indice)
        {
            case 1:
                Vela(barca);
                break;
            case 2:
                Cabina(barca, ClaveMaterial.Vegetacion);
                Rueda(barca, -0.91f, 0.7f, 0.60f, 0.44f);
                Rueda(barca, 0.91f, 0.7f, 0.60f, 0.44f);
                break;
            case 3:
                Cabina(barca, ClaveMaterial.PiedraCrema);
                Poste(barca, 0, 1.35f, 1.0f);
                DioramaBuilder.CrearEsfera("Lente de guardia", barca, new Vector3(0, 1.35f, 1), 0.38f, M(ClaveMaterial.AmbarSuave));
                break;
            case 4:
                Toldo(barca, ClaveMaterial.NiloExtremidades, 1.45f);
                Bulto(kit == null ? null : kit.caja, barca, "Saca de correo", -0.34f, 1.03f, 0.64f);
                Bulto(kit == null ? null : kit.barril, barca, "Barril lacrado", 0.37f, 1.15f, 0.56f);
                break;
            case 5:
                Rueda(barca, 0, 0.84f, -0.71f, 0.52f);
                Rueda(barca, 0, 0.84f, 0.58f, 0.46f);
                Poste(barca, -0.6f, 1.35f, 1.32f);
                break;
            case 6:
                Grua(barca);
                Bulto(kit == null ? null : kit.caja, barca, "Caja de repuestos", 0.16f, 1.16f, 0.74f);
                break;
            case 7:
                Toldo(barca, ClaveMaterial.NiloBufanda, 2.20f);
                Bulto(kit == null ? null : kit.caja, barca, "Cajón de mercado", -0.31f, 1.16f, 0.61f);
                Bulto(kit == null ? null : kit.barril, barca, "Tonel", 0.40f, 1.23f, 0.48f);
                break;
            case 8:
                Cabina(barca, ClaveMaterial.MaiaCarcasa);
                Poste(barca, 0, 2.55f, 0.60f);
                for (int i = 0; i < 3; i++)
                    Caja(barca, "Antena de onda corta", 0, 2.13f + i * 0.18f, 0.60f, 1.0f - 0.24f * i, 0.055f, 0.07f, ClaveMaterial.PiedraCrema);
                Bulto(kit == null ? null : kit.radio, barca, "Receptor", 0, 1.22f, 0.62f);
                break;
        }
        return raiz;
    }

    static Mesh MallaCasco()
    {
        if (casco != null) return casco;
        float[] z = { -2.2f, -1.65f, 0.75f, 1.56f, 2.35f };
        float[] ancho = { 0.50f, 0.93f, 0.98f, 0.73f, 0.09f };
        var vertices = new List<Vector3>();
        var caras = new[] { new List<int>(), new List<int>(), new List<int>() };
        for (int i = 0; i < z.Length - 1; i++)
        {
            for (int lado = -1; lado <= 1; lado += 2)
            {
                var a = new Vector3(lado * ancho[i], 0.34f, z[i]);
                var b = new Vector3(lado * ancho[i + 1], 0.34f, z[i + 1]);
                var c = new Vector3(b.x * 0.95f, 0.19f, b.z);
                var d = new Vector3(a.x * 0.95f, 0.19f, a.z);
                Cara(vertices, caras[1], a, b, c, d, lado < 0);
                Cara(vertices, caras[0], d, c, new Vector3(c.x * 0.7f, -0.29f, c.z), new Vector3(d.x * 0.7f, -0.29f, d.z), lado < 0);
            }
            Cara(vertices, caras[2], new Vector3(-ancho[i],0.34f,z[i]), new Vector3(-ancho[i+1],0.34f,z[i+1]),
                new Vector3(ancho[i+1],0.34f,z[i+1]), new Vector3(ancho[i],0.34f,z[i]), false);
        }
        for (int i = 0; i < z.Length; i += z.Length - 1)
            Cara(vertices, caras[0], new Vector3(-ancho[i],0.34f,z[i]),new Vector3(ancho[i],0.34f,z[i]),
                new Vector3(ancho[i]*0.7f,-0.29f,z[i]),new Vector3(-ancho[i]*0.7f,-0.29f,z[i]), i != 0);
        casco = new Mesh { name = "RELEVO_CascoDeFaena", subMeshCount = 3 };
        casco.SetVertices(vertices);
        for (int i = 0; i < caras.Length; i++) casco.SetTriangles(caras[i], i);
        casco.RecalculateNormals();
        casco.RecalculateBounds();
        return casco;
    }

    static void Cara(List<Vector3> v, List<int> indices, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool vuelta)
    {
        int n = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        if (vuelta) indices.AddRange(new[] { n, n+2, n+1, n, n+3, n+2 });
        else indices.AddRange(new[] { n, n+1, n+2, n, n+2, n+3 });
    }

    static Transform Caja(Transform p, string nombre, float x, float y, float z, float ancho, float alto, float largo, ClaveMaterial material)
    {
        return DioramaBuilder.CrearCaja(nombre, p, new Vector3(x,y,z), new Vector3(ancho,alto,largo), M(material), false).transform;
    }

    static void Poste(Transform p, float x, float alto, float z)
    {
        DioramaBuilder.CrearCilindro("Palo",p,new Vector3(x,(alto+0.34f)*0.5f,z),0.055f,alto-0.34f,M(ClaveMaterial.Cobre));
    }

    static void Toldo(Transform p, ClaveMaterial tela, float largo)
    {
        for (int x = -1; x <= 1; x += 2)
        {
            Poste(p, x*0.76f,1.46f,-0.33f-largo*0.43f);
            Poste(p, x*0.76f,1.46f,-0.33f+largo*0.43f);
            for (int j=0; j<7; j++)
            {
                var color = j % 2 == 0 ? tela : ClaveMaterial.PiedraCrema;
                float z=-0.33f+(j-3)*largo/7f;
                Caja(p,"Lona",x*0.46f,1.58f,z,0.98f,0.07f,largo/7f+0.012f,color).localRotation=Quaternion.Euler(0,0,-x*17f);
                Caja(p,"Faldón",x*0.90f,1.37f,z,0.055f,0.16f,largo/7f,color);
            }
        }
    }

    static void Cabina(Transform p, ClaveMaterial tejado)
    {
        Caja(p,"Cabina",0,0.76f,-0.65f,1.39f,0.80f,1.48f,ClaveMaterial.PiedraCrema);
        Caja(p,"Ventanas",0,0.98f,-0.65f,1.42f,0.25f,1.51f,ClaveMaterial.NiloPantalla);
        Caja(p,"Tejadillo",0,1.19f,-0.65f,1.58f,0.12f,1.69f,tejado);
        Caja(p,"Escotilla",0,1.28f,-0.88f,0.51f,0.07f,0.56f,ClaveMaterial.Cobre);
    }

    static void Grua(Transform p)
    {
        var pie = new Vector3(0,0.36f,-0.83f);
        var codo = new Vector3(0,1.55f,-0.85f);
        var punta = new Vector3(0,2.03f,0.84f);
        var m = M(ClaveMaterial.NiloBufanda);
        DioramaBuilder.CrearCajaEntre("Columna",p,pie,codo,0.23f,m);
        DioramaBuilder.CrearCajaEntre("Pluma",p,codo,punta,0.15f,m);
        DioramaBuilder.CrearCajaEntre("Tirante",p,pie+Vector3.up*0.40f,punta,0.075f,M(ClaveMaterial.Cobre));
        DioramaBuilder.CrearCajaEntre("Cable de izado",p,punta,punta-Vector3.up*0.98f,0.038f,M(ClaveMaterial.NiloPantalla));
        Caja(p,"Motón",0,1.02f,0.84f,0.2f,0.2f,0.14f,ClaveMaterial.PiedraCrema);
    }

    static void Rueda(Transform p, float x, float y, float z, float radio)
    {
        var centro = new Vector3(x,y,z);
        var eje=DioramaBuilder.CrearCilindro("Tambor",p,centro,radio*0.78f,0.56f,M(ClaveMaterial.Cobre)).transform;
        eje.localRotation=Quaternion.Euler(0,0,90f);
        for(int i=-1;i<=1;i+=2)
        {
            var aro=DioramaBuilder.CrearCilindro("Brida",p,centro+Vector3.right*(0.27f*i),radio,0.07f,M(ClaveMaterial.NiloBufanda)).transform;
            aro.localRotation=eje.localRotation;
        }
        for(int i=0;i<6;i++)
        {
            var giro=Quaternion.AngleAxis(i*60f,Vector3.right);
            var tabla=DioramaBuilder.CrearCaja("Paleta",p,centro+giro*Vector3.up*radio,new Vector3(0.61f,0.08f,0.19f),M(ClaveMaterial.PiedraCrema),false);
            tabla.transform.localRotation=giro;
        }
    }

    static void Vela(Transform p)
    {
        Poste(p,0,2.45f,0.04f);
        for(int i=0;i<6;i++)
        {
            float ancho=1.43f-0.19f*i;
            var pano=Caja(p,"Paño de vela",ancho*0.42f-0.5f,0.86f+i*0.26f,0.03f,ancho,0.275f,0.08f,
                i==4?ClaveMaterial.NiloBufanda:ClaveMaterial.PiedraCrema);
            pano.localRotation=Quaternion.Euler(-10,24,-5);
        }
        DioramaBuilder.CrearCajaEntre("Botavara",p,new Vector3(-0.56f,0.74f,0.2f),new Vector3(0.91f,0.74f,-0.32f),0.08f,M(ClaveMaterial.Cobre));
    }

    static void Bulto(GameObject modelo, Transform p, string nombre, float x, float z, float lado)
    {
        if (modelo == null) return;
        var tam=KitRelevo.Tamano(modelo);
        KitRelevo.ColocarCentrado(modelo,p,nombre,new Vector3(x,0.36f,z),15f,Vector3.one*(lado/Mathf.Max(tam.x,tam.y,tam.z,0.01f)));
    }
}

