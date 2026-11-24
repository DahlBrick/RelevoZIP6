using UnityEngine;
using UnityEngine.UI;

public sealed partial class EcoDeTurno
{
    [SerializeField] int puerto;
    [SerializeField] TransbordadorDeMaroma transbordador;
    [SerializeField] Renderer receptorRemoto;
    [SerializeField] float capacidad = 6f;
    GameObject visor;
    bool senalAnterior;
    Text lectura;
    Image cintaRestante;
    bool Campana { get { return puerto > 0; } }
    public bool PuedeGrabar { get { return estado != Estado.Grabando && estado != Estado.Abierto && !transbordador.Navegando; } }
    public bool OcupaInteraccion { get { return estado == Estado.Grabando || transbordador.Navegando; } }
    public bool SenalDeSalida { get { return estado == Estado.Abierto || (estado == Estado.Reproduciendo && EnPlaca(eco,placaNorte)); } }
    static bool BateriaEnBase { get { return RelevoGame.Bateria != null && RelevoGame.Bateria.BaseActual != null; } }

    public string Objetivo
    {
        get
        {
            if ((puerto == 9 || puerto == 10) && RelevoGame.Reserva != null && RelevoGame.Reserva.Fraccion < 0.35f && !RelevoGame.Reserva.EnZona)
                return "¡La reserva se agota! Vuelve junto a la batería.";
            if (transbordador.Navegando) return "Sujeta la batería mientras cruza el transbordador.";
            if (puerto == 12 && transbordador.EnDestino)
                return RelevoGame.Layout.Casilla(nilo.transform.position).x >= 18 ? "Lleva la batería al pórtico o abre el regreso." : "Carga la torre D y cruza con la batería.";
            if (puerto == 10 && estado == Estado.Abierto)
                return BateriaEnBase ? "Retira la batería de la base A y cruza la pasarela." : "Cruza la pasarela hacia la casa de señales.";
            if (puerto == 10 && transbordador.EnDestino && estado == Estado.Reproduciendo)
                return BateriaEnBase ? "Cruza al receptor II mientras el eco pisa II." : "Coloca la batería en la base A y cruza al receptor II antes de que acabe la cinta.";
            if (puerto == 10 && transbordador.EnDestino && BateriaEnBase)
                return "La cinta se acabó: retira la batería de la base A y vuelve en el pontón.";
            if (transbordador.EnDestino)
                return puerto == 9 ? (RelevoGame.Nivel.Puentes.Exists(p => p.Desplegado) ? "Lleva la batería por el puente de carga hasta la grada." : "Deja la caja en el rombo para tender el puente de carga.") :
                    estado == Estado.Reproduciendo ? "Pisa II en esta orilla mientras el eco pisa II en la otra." : "Vuelve en el pontón para grabar otro turno.";
            if (puerto == 9 && estado == Estado.Reproduciendo && BateriaEnBase) return "Recupera la batería de la base A y embarca mientras el eco pisa I.";
            if (puerto == 9 && estado == Estado.Espera && !BateriaEnBase)
                return RelevoGame.Bateria != null && RelevoGame.Bateria.EnManos ? "Coloca la batería en la base A: su puente lleva a la placa I." : "Recoge la batería.";
            if (puerto == 9 && estado == Estado.Espera && BateriaEnBase) return "Graba un turno en el magnetófono y cruza el puente hasta la placa I.";
            if (estado == Estado.Grabando) return puerto != 10 ? "Graba una espera sobre I." : "Graba una espera en I y termina sobre II.";
            if (estado == Estado.Reproduciendo) return "Embarca cuando se encienda el torno.";
            return "Busca el magnetófono de dos carretes.";
        }
    }

    public string Pista
    {
        get
        {
            if (puerto == 12) return transbordador.EnDestino
                ? "La última campana mira al puente de D. Carga todas las barras antes de retirar la batería. El cajón de esta orilla abre la reja del almacén si quieres entrar."
                : "Deja que la cinta espere sobre I. Durante la repetición, toma el torno del norte con la batería.";
            if (estado == Estado.Abierto) return "El cerrojo ya cayó, Nilo. La pasarela de la casa de señales se queda tendida.";
            if (puerto == 9 && transbordador.EnDestino) return "En la grada empujábamos el cajón sobre el rombo. Así el puente de carga se quedaba tendido hasta acabar el turno.";
            if (puerto == 10 && transbordador.EnDestino) return "El receptor gemelo está en el islote, Nilo. Su puente solo se tiende con la batería en la base A. Si la cinta se acaba, recupera la batería antes de volver. ¿Ves los dos trazos? Quédate en esta placa mientras la cinta pisa su gemela. Si oyes el clic final demasiado pronto, el torno de vuelta tiene contrapeso.";
            return puerto == 9 ? "La placa I está en el islote: deja la batería en la base A para tender su puente. Graba tu paseo hasta la placa y espera allí. La cinta te devuelve al magnetófono: corre a por la batería y embarca mientras el eco pisa I. La cinta es corta." :
                "Dale tiempo al primer relevo, Nilo. Cuenta cuatro sobre I antes de ir a II y deja que la cinta se termine allí. Al otro lado tendrás que colocar la batería en la base A y cruzar al receptor antes de que el eco se vaya de II.";
        }
    }

    public void IniciarToma() { if (PuedeGrabar) Grabar(); }

    public void ConstruirCampana(int numero, LevelLayout plano, PlayerController jugador, PermanentWalkway pasarela)
    {
        puerto = numero;
        capacidad = numero == 10 ? 11f : numero == 9 ? 6f : 8f;
        nilo = jugador;
        puente = pasarela;
        consola = DioramaBuilder.CrearAncla("Magnetófono de guardia",transform,plano.PrimerMarcador('F',Vector3.zero));
        pilotoConsola = Magnetofono(consola);
        consola.gameObject.AddComponent<PuestoDeRelevo>().Configurar(this,null,false);
        Vector3 ida = plano.PrimerMarcador('H',Vector3.zero);
        Vector3 vuelta = plano.PrimerMarcador('I',Vector3.zero);
        placaNorte = Placa(transform,plano.PrimerMarcador('1',Vector3.zero),true);
        Vector3 contacto = placaNorte.transform.position;
        Vector3 codo = new Vector3(contacto.x,0.14f,ida.z);
        contacto.y = 0.14f;
        DioramaBuilder.CrearCajaEntre("Línea de embrague",transform,contacto,codo,0.09f,M(ClaveMaterial.Cobre));
        DioramaBuilder.CrearCajaEntre("Derivación al torno",transform,codo,ida+Vector3.up*0.14f,0.09f,M(ClaveMaterial.Cobre));
        if (puerto == 10)
        {
            placaSur = Placa(transform,plano.PrimerMarcador('2',Vector3.zero),false);
            receptorRemoto = Placa(transform,plano.PrimerMarcador('3',Vector3.zero),false);
            Vector3 orilla = plano.CentroTile(10,2);
            Cable(transform,receptorRemoto.transform.position,orilla,0.22f);
        }
        transbordador = DioramaBuilder.CrearAncla("Transbordador de maroma",transform,Vector3.zero).gameObject.AddComponent<TransbordadorDeMaroma>();
        transbordador.Construir(this,ida,vuelta);
        if (puerto == 9 || puerto == 10) DecorarGuardia(plano);
        CrearEco(transform);
    }

    void DecorarGuardia(LevelLayout plano)
    {
        var kit = KitRelevo.Activo;
        if (kit == null) return;
        if (puerto == 9)
        {
            var amarre = DioramaBuilder.CrearAncla("Barca en reparación",transform,plano.CentroTile(7,6));
            if (kit.botes != null && kit.botes.Length > 0) Atrezo(kit.botes[0],amarre,"Casco amarrado",Vector3.down*0.42f,30,3.2f);
            Atrezo(kit.boya,amarre,"Boya de amarre",new Vector3(-1.65f,-0.3f,0.6f),0,0.7f);
            var grada = DioramaBuilder.CrearAncla("Grada de reparación",transform,plano.CentroTile(17,2));
            Atrezo(kit.barril,grada,"Brea",new Vector3(0.65f,0,0),0,1.05f);
            Atrezo(kit.poste,grada,"Pescante",new Vector3(0.95f,0,0.65f),0,1.8f);
        }
        else
        {
            var cabina = DioramaBuilder.CrearAncla("Casa de señales",transform,plano.CentroTile(2,3));
            Atrezo(kit.antenaMaquina,cabina,"Antena de escucha",Vector3.zero,20,2.5f);
            Atrezo(kit.radio,cabina,"Receptor de guardia",new Vector3(-0.95f,0,0.32f),-15,1.0f);
            var archivo = DioramaBuilder.CrearAncla("Archivo de guardia",transform,plano.CentroTile(16,0));
            Atrezo(kit.radio,archivo,"Radio del registro",Vector3.zero,30,1.15f);
            Atrezo(kit.barril,archivo,"Tonel estanco",new Vector3(0.8f,0,0),10,0.9f);
        }
    }

    static void Atrezo(GameObject modelo,Transform padre,string nombre,Vector3 lugar,float giro,float dimension)
    {
        if (modelo == null) return;
        var talla = KitRelevo.Tamano(modelo);
        KitRelevo.ColocarCentrado(modelo,padre,nombre,lugar,giro,Vector3.one*(dimension/Mathf.Max(0.01f,Mathf.Max(talla.x,Mathf.Max(talla.y,talla.z)))));
    }

    void CrearIndicador()
    {
        var go = new GameObject("Visor del magnetófono",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        go.transform.SetParent(transform,false);
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440,900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var borde = RelevoUI.Franja(go.transform,"Tapa de latón",new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(0,-12),new Vector2(370,53),new Color32(179,141,88,255));
        var fondo = RelevoUI.Franja(borde.transform,"Esmalte",Vector2.one*0.5f,Vector2.one*0.5f,Vector2.zero,new Vector2(366,49),new Color32(23,55,59,255));
        lectura = RelevoUI.Texto(fondo.transform,"Contador",Vector2.one*0.5f,Vector2.one*0.5f,new Vector2(0,4),new Vector2(356,35),17,TextAnchor.MiddleCenter,new Color32(253,234,188,255));
        cintaRestante = RelevoUI.Franja(fondo.transform,"Longitud",new Vector2(0,0),new Vector2(0,0),new Vector2(7,5),new Vector2(350,4),Color.white);
        visor = go;
        visor.SetActive(false);
    }

    void PanelCampana(bool grabando,bool reproduciendo,string tecla)
    {
        bool senal = reproduciendo && SenalDeSalida;
        if (senal && !senalAnterior) AudioDirector.Pedir(Tono.PlacaPisada);
        senalAnterior = senal;
        bool visible = (grabando || reproduciendo) && !RelevoGame.UI.BloqueaJuego;
        if (visor.activeSelf != visible) visor.SetActive(visible);
        if (!visible) return;
        float restante = Mathf.Max(0,(grabando ? capacidad : Longitud)-reloj);
        string valor = restante.ToString("0.0") + " s";
        string texto = grabando ? "●  GRABANDO    " + valor + "    " + tecla + " · cortar" : "▶  ECO    " + valor + (SenalDeSalida ? "    I · torno listo" : "    I · en espera");
        if (lectura.text != texto) lectura.text = texto;
        cintaRestante.rectTransform.sizeDelta = new Vector2(350f*restante/(grabando ? capacidad : Longitud),4);
        cintaRestante.color = grabando ? new Color32(247,139,112,255) : new Color32(101,220,195,255);
    }
}
