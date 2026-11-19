using UnityEngine;

/// <summary>Escenografía del mercado y el varadero. Las marcas del plano sitúan cada instalación.</summary>
public static class PuertosDeSal
{
    public static void Montar(int nivel, LevelLayout mapa, Transform actores)
    {
        if (nivel != 11 && nivel != 12) return;
        var kit = KitRelevo.Activo;
        var barrio = DioramaBuilder.CrearAncla(nivel == 11 ? "Puestos del mercado" : "Talleres de la fundición",actores,Vector3.zero);
        foreach (var c in mapa.Casillas('a'))
        {
            var p = DioramaBuilder.CrearAncla(nivel == 11 ? "Mesa de salazón" : "Banco del campanero",barrio,mapa.CentroTile(c.x,c.y));
            for (int i=-1;i<=1;i+=2)
            {
                Caja(p,"Pata",new Vector3(i*0.63f,0.3f,0),new Vector3(0.16f,0.6f,0.64f),ClaveMaterial.MaderaOscura);
                Caja(p,"Poste del toldo",new Vector3(i*0.86f,1.05f,0.52f),new Vector3(0.12f,2.1f,0.12f),ClaveMaterial.Cobre);
            }
            Caja(p,"Tablero",Vector3.up*0.66f,new Vector3(1.76f,0.13f,0.94f),ClaveMaterial.MaderaOscura);
            for (int i=-2;i<=2;i++)
            {
                var lona=Caja(p,"Paño listado",new Vector3(i*0.39f,1.94f,0.19f),new Vector3(0.39f,0.07f,1.08f),i%2==0 ? (nivel==11?ClaveMaterial.MaiaCarcasa:ClaveMaterial.Coral) : ClaveMaterial.PiedraCrema);
                lona.transform.localRotation=Quaternion.Euler(-12,0,0);
            }
            for (int i=-1;i<=1;i++)
                DioramaBuilder.CrearCilindro("Medida",p,new Vector3(i*0.41f,0.85f,0.03f),0.19f,0.22f,Mat(nivel==11?ClaveMaterial.PiedraCrema:ClaveMaterial.Cobre));
            if (kit!=null) Objeto(kit.barril,p,"Tonel",new Vector3(1.31f,0,0.35f),15,1.05f);
        }
        foreach(var c in mapa.Casillas('b'))
        {
            var p=DioramaBuilder.CrearAncla(nivel==11?"Bancal de evaporación":"Moldes de campana",barrio,mapa.CentroTile(c.x,c.y));
            Caja(p,"Bastidor",Vector3.up*0.12f,new Vector3(1.8f,0.22f,1.55f),ClaveMaterial.Cobre);
            Caja(p,"Lecho",Vector3.up*0.245f,new Vector3(1.61f,0.05f,1.38f),nivel==11?ClaveMaterial.Agua:ClaveMaterial.MaderaOscura);
            for(int i=0;i<3;i++)
            {
                var v=new Vector3(-0.51f+i*0.47f,0.29f,(i%2-0.5f)*0.34f);
                if(nivel==11) DioramaBuilder.CrearEsfera("Sal gruesa",p,v,0.48f,Mat(ClaveMaterial.PiedraCrema)).transform.localScale=new Vector3(0.51f,0.32f,0.48f);
                else DioramaBuilder.CrearCilindro("Molde de barro",p,v+Vector3.up*0.12f,0.22f,0.28f,Mat(ClaveMaterial.Coral));
            }
        }
        if(kit!=null)
        {
            foreach(var c in mapa.Casillas('h'))
            {
                var p=DioramaBuilder.CrearAncla("Patio de acopio",barrio,mapa.CentroTile(c.x,c.y));
                Objeto(kit.barril,p,"Barrica",new Vector3(-0.45f,0,0),-12,1.15f);
                Objeto(kit.barril,p,"Barrica baja",new Vector3(0.34f,0,-0.27f),8,0.83f);
                Objeto(kit.farol,p,"Farol",new Vector3(0.34f,0.82f,-0.27f),15,0.48f);
            }
        }
        if(nivel==12)
        {
            var puentes=actores.GetComponentsInChildren<TemporaryBridge>();
            foreach(var puente in puentes)
            {
                char sitio=puente.Identificador=="R"?'j':'s';
                var p=DioramaBuilder.CrearAncla("Campana del puente "+puente.Identificador,barrio,mapa.PrimerMarcador(sitio,Vector3.zero));
                p.gameObject.AddComponent<CampanaDePaso>().Construir(puente);
            }
        }
    }

    internal static Material Mat(ClaveMaterial clave) { return RelevoPalette.MaterialDe(clave); }
    internal static GameObject Caja(Transform padre,string nombre,Vector3 pos,Vector3 tam,ClaveMaterial clave)
    {
        return DioramaBuilder.CrearCaja(nombre,padre,pos,tam,Mat(clave),false);
    }
    static void Objeto(GameObject modelo,Transform padre,string nombre,Vector3 pos,float giro,float medida)
    {
        if(modelo==null)return;
        var t=KitRelevo.Tamano(modelo);
        KitRelevo.ColocarCentrado(modelo,padre,nombre,pos,giro,Vector3.one*medida/Mathf.Max(t.x,t.y,t.z,0.01f));
    }
}
