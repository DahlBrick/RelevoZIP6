using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Ensayo independiente. La cinta captura el recorrido real y lo reproduce una vez.</summary>
public sealed partial class EcoDeTurno : MonoBehaviour
{
    float Longitud { get { return cinta.Count == 0 ? capacidad : cinta[cinta.Count-1].tiempo; } }
    const float Intervalo = 0.04f;
    const float Contacto = 0.65f;

    [SerializeField] InputActionAsset controles;
    [SerializeField] PlayerController nilo;
    [SerializeField] CameraRig camara;
    [SerializeField] PermanentWalkway puente;
    [SerializeField] Transform consola, salida, eco;
    [SerializeField] Renderer pilotoConsola, pilotoSalida, placaNorte, placaSur;
    [SerializeField] Transform[] carretes;

    struct Muestra
    {
        public float tiempo;
        public Vector3 posicion;
        public Quaternion giro;
        public Muestra(float tiempo, Transform figura)
        {
            this.tiempo = tiempo;
            posicion = figura.position;
            giro = figura.rotation;
        }
    }

    enum Estado { Espera, Grabando, Reproduciendo, Abierto, Recibido }
    Estado estado;
    readonly List<Muestra> cinta = new List<Muestra>(160);
    RelevoInput entrada;
    InputActionAsset copiaControles;
    MonitorDeCinta monitor;
    LineRenderer hilo;
    float reloj, sosten;
    int indice;
    bool intentoPrevio;
    Vector3 origen;
    Quaternion rumbo;

    static LevelLayout Plano()
    {
        return new LevelLayout("Cabina de relevos", 2f,
            "v#####wwwwv##", "######wwww###", "m#####wwww###", "m#####wwww###",
            "######PPPP###", "m#####wwww###", "m#####wwww###", "######wwww###", "v#####wwwwv##");
    }

    public void Construir(InputActionAsset acciones)
    {
        if (nilo != null) return;
        controles = acciones;
        var plano = Plano();
        var maqueta = DioramaBuilder.CrearAncla("Diques", transform, Vector3.zero);
        maqueta.gameObject.AddComponent<AmbienteRelevo>();
        ActorFactory.ConfigurarEntorno();
        var limites = DioramaBuilder.Construir(plano, maqueta);
        var actores = DioramaBuilder.CrearAncla("Cabina y boyas", transform, Vector3.zero);
        ActorFactory.CrearSol(actores);
        camara = DioramaBuilder.CrearAncla("Cámara", transform, Vector3.zero).gameObject.AddComponent<CameraRig>();
        camara.Configurar(limites);
        puente = ActorFactory.CrearPasarela(actores, plano, 'P', "P");
        consola = DioramaBuilder.CrearAncla("Magnetófono", actores, plano.CentroTile(1,4));
        pilotoConsola = Magnetofono(consola);
        salida = DioramaBuilder.CrearAncla("Receptor de guardia", actores, plano.CentroTile(11,4));
        Receptor(salida);
        placaNorte = Placa(actores, plano.CentroTile(4,1), true);
        placaSur = Placa(actores, plano.CentroTile(4,7), false);
        Vector3 borne = plano.CentroTile(5,4);
        Cable(actores, placaNorte.transform.position, borne, -0.28f);
        Cable(actores, placaSur.transform.position, borne, 0.28f);
        nilo = ActorFactory.CrearNilo(actores, plano.CentroTile(1.67f,4), 90f);
        CrearEco(actores);
    }

    void CrearEco(Transform actores)
    {
        eco = DioramaBuilder.CrearAncla("Eco de Nilo", actores, nilo.transform.position);
        var kit = KitRelevo.Activo;
        if (kit != null && kit.niloModelo != null)
        {
            var modelo = KitRelevo.Colocar(kit.niloModelo, eco, Vector3.zero, 0, kit.niloEscala, false);
            foreach (var r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                    mats[m] = M(m % 2 == 0 ? ClaveMaterial.Turquesa : ClaveMaterial.MaiaCarcasa);
                r.sharedMaterials = mats;
            }
        }
        DioramaBuilder.CrearAro("Huella de la cinta", eco, Vector3.up * 0.12f, 0.73f, M(ClaveMaterial.Turquesa));
        eco.gameObject.SetActive(false);
    }

    static Material M(ClaveMaterial clave) { return RelevoPalette.MaterialDe(clave); }

    static GameObject Caja(Transform p, string n, Vector3 centro, Vector3 escala, ClaveMaterial clave)
    {
        return DioramaBuilder.CrearCaja(n,p,centro,escala,M(clave),false);
    }

    Renderer Magnetofono(Transform p)
    {
        Caja(p,"Mueble de radio", Vector3.up*0.35f, new Vector3(1.52f,0.67f,1.10f), ClaveMaterial.MaderaOscura);
        Caja(p,"Platina", Vector3.up*0.73f, new Vector3(1.42f,0.11f,1.02f), ClaveMaterial.PiedraCrema);
        carretes = new Transform[2];
        for (int i=0; i<2; i++)
        {
            float x = i==0 ? -0.36f : 0.36f;
            carretes[i] = DioramaBuilder.CrearAncla("Carrete",p,new Vector3(x,0.84f,0.08f));
            DioramaBuilder.CrearCilindro("Cinta",carretes[i],Vector3.zero,0.27f,0.08f,M(ClaveMaterial.NiloPantalla));
            for(int j=0; j<3; j++)
            {
                var a=Quaternion.Euler(0,j*120f,0);
                Caja(carretes[i],"Radio",a*(Vector3.forward*0.11f)+Vector3.up*0.06f,new Vector3(0.07f,0.03f,0.22f),ClaveMaterial.PiedraCrema).transform.localRotation=a;
            }
        }
        return Caja(p,"Piloto", new Vector3(0,0.80f,-0.36f),new Vector3(0.41f,0.07f,0.14f),ClaveMaterial.Ambar).GetComponent<Renderer>();
    }

    void Receptor(Transform p)
    {
        Caja(p,"Pie",Vector3.up*0.28f,new Vector3(1.32f,0.56f,1.12f),ClaveMaterial.MaderaOscura);
        var kit=KitRelevo.Activo;
        if (kit!=null && kit.radio!=null)
        {
            float escala=1.45f/Mathf.Max(KitRelevo.Tamano(kit.radio).x,0.01f);
            KitRelevo.ColocarCentrado(kit.radio,p,"Radio de onda corta",Vector3.up*0.56f,0,Vector3.one*escala);
        }
        pilotoSalida=DioramaBuilder.CrearAro("Sintonía",p,Vector3.up*0.10f,0.92f,M(ClaveMaterial.Cobre)).GetComponent<Renderer>();
    }

    static Renderer Placa(Transform p, Vector3 centro, bool norte)
    {
        var basePlaca=DioramaBuilder.CrearAncla(norte?"Placa de un trazo":"Placa de dos trazos",p,centro);
        Caja(basePlaca,"Marco",Vector3.up*0.085f,new Vector3(1.68f,0.16f,1.68f),ClaveMaterial.Cobre);
        var luz=Caja(basePlaca,"Contacto",Vector3.up*0.175f,new Vector3(1.42f,0.065f,1.42f),ClaveMaterial.NiloExtremidades);
        for(int i=0; i<(norte?1:2); i++)
            Caja(basePlaca,"Marca",new Vector3(norte?0:i*0.40f-0.20f,0.224f,0),new Vector3(0.16f,0.032f,0.76f),ClaveMaterial.PiedraCrema);
        return luz.GetComponent<Renderer>();
    }

    static void Cable(Transform p, Vector3 inicio, Vector3 fin, float separacion)
    {
        inicio.y=fin.y=0.11f;
        fin.z += separacion;
        var esquina=new Vector3(fin.x+separacion,0.11f,inicio.z);
        var terminal=new Vector3(esquina.x,0.11f,fin.z);
        DioramaBuilder.CrearCajaEntre("Cable de señal",p,inicio,esquina,0.075f,M(ClaveMaterial.Cobre));
        DioramaBuilder.CrearCajaEntre("Retorno",p,esquina,terminal,0.075f,M(ClaveMaterial.Cobre));
    }

    void Start()
    {
        if (Campana)
        {
            entrada = RelevoGame.Input;
            CrearIndicador();
        }
        else
        {
            if (nilo == null || controles == null) { enabled = false; return; }
            copiaControles = Instantiate(controles);
            entrada = new RelevoInput(copiaControles);
            camara.Configurar(Plano().Limites());
            nilo.Inicializar(entrada,camara.Camara);
            monitor = MonitorDeCinta.Crear(transform);
        }
        origen = nilo.transform.position;
        rumbo = nilo.transform.rotation;
        var linea=new GameObject("Pista de la cinta");
        linea.transform.SetParent(transform,false);
        hilo=linea.AddComponent<LineRenderer>();
        hilo.sharedMaterial=M(ClaveMaterial.Turquesa);
        hilo.useWorldSpace=true;
        hilo.alignment=LineAlignment.View;
        hilo.widthMultiplier=0.075f;
        hilo.numCornerVertices=2;
        hilo.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        if (!Campana)
        {
            gameObject.AddComponent<AudioDirector>().IniciarMusicaNivel();
            Reiniciar();
        }
    }

    void OnDestroy()
    {
        if(!Campana && entrada!=null) entrada.Desactivar();
        if(copiaControles!=null) Destroy(copiaControles);
    }

    void LateUpdate()
    {
        if (entrada==null) return;
        if (Campana && (Time.timeScale == 0 || RelevoGame.UI.BloqueaJuego))
        {
            if (visor != null && visor.activeSelf) visor.SetActive(false);
            return;
        }
        if (Campana && Cerca(nilo.transform.position,transbordador.Embarque,4f) && TutorialMaia.PrimeraVez(109))
        {
            TutorialMaia.ExplicarMaroma();
            return;
        }
        if (!Campana && (entrada.Pulsado(entrada.Reiniciar) || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame))) { Reiniciar(); return; }
        bool usar=entrada.Pulsado(entrada.Interactuar);
        if (!Campana && (estado==Estado.Espera || estado==Estado.Reproduciendo) && Cerca(nilo.transform.position,consola.position,1.85f) && usar) Grabar();
        else if (!Campana && estado==Estado.Abierto && Cerca(nilo.transform.position,salida.position,1.8f) && usar)
        {
            estado=Estado.Recibido;
            AudioDirector.Pedir(Tono.Recuerdo);
        }
        float dt=Time.deltaTime;
        if (estado==Estado.Grabando)
        {
            reloj=Mathf.Min(capacidad,reloj+dt);
            bool cortar = usar && reloj >= 0.75f;
            if(reloj-cinta[cinta.Count-1].tiempo>=Intervalo || cortar || reloj>=capacidad) Anotar();
            if(cortar || reloj>=capacidad) Reproducir();
        }
        else if (estado==Estado.Reproduciendo)
        {
            reloj=Mathf.Min(Longitud,reloj+dt);
            while(indice+1<cinta.Count-1 && cinta[indice+1].tiempo<=reloj) indice++;
            var a=cinta[indice];
            var b=cinta[Mathf.Min(indice+1,cinta.Count-1)];
            float f=Mathf.InverseLerp(a.tiempo,b.tiempo,reloj);
            eco.SetPositionAndRotation(Vector3.Lerp(a.posicion,b.posicion,f),Quaternion.Slerp(a.giro,b.giro,f));
            bool pareja = Campana
                ? puerto == 10 && EnPlaca(eco,placaSur) && EnPlaca(nilo.transform,receptorRemoto) && !transbordador.Navegando
                : EnPlaca(nilo.transform,placaNorte)&&EnPlaca(eco,placaSur) || EnPlaca(nilo.transform,placaSur)&&EnPlaca(eco,placaNorte);
            sosten=pareja?sosten+dt:0f;
            if(sosten>=Contacto)
            {
                estado=Estado.Abierto;
                puente.Abrir();
                eco.gameObject.SetActive(false);
                hilo.positionCount=0;
            }
            else if(reloj>=Longitud)
            {
                estado=Estado.Espera;
                intentoPrevio=true;
                eco.gameObject.SetActive(false);
                hilo.positionCount=0;
            }
        }
        ActualizarPanel();
    }

    static bool Cerca(Vector3 a,Vector3 b,float radio)
    {
        a.y=b.y=0;
        return (a-b).sqrMagnitude<=radio*radio;
    }

    static bool EnPlaca(Transform figura,Renderer placa)
    {
        if (placa == null) return false;
        var d=figura.position-placa.transform.position;
        return Mathf.Abs(d.x)<0.79f && Mathf.Abs(d.z)<0.79f;
    }

    void Anotar()
    {
        cinta.Add(new Muestra(reloj,nilo.transform));
        Vector3 p=nilo.transform.position+Vector3.up*0.17f;
        if(hilo.positionCount>0 && (hilo.GetPosition(hilo.positionCount-1)-p).sqrMagnitude<0.045f) return;
        int i=hilo.positionCount++;
        hilo.SetPosition(i,p);
    }

    void Grabar()
    {
        eco.gameObject.SetActive(false);
        estado=Estado.Grabando;
        cinta.Clear();
        reloj=sosten=0;
        hilo.positionCount=0;
        hilo.sharedMaterial=M(ClaveMaterial.AmbarSuave);
        Anotar();
        AudioDirector.Pedir(Tono.Insertar);
    }

    void Reproducir()
    {
        estado=Estado.Reproduciendo;
        reloj=0; indice=0;
        eco.gameObject.SetActive(true);
        var inicio=cinta[0];
        eco.SetPositionAndRotation(inicio.posicion,inicio.giro);
        nilo.Colocar(inicio.posicion);
        nilo.transform.rotation=inicio.giro;
        hilo.sharedMaterial=M(ClaveMaterial.Turquesa);
        AudioDirector.Pedir(Tono.Retirar);
    }

    void Reiniciar()
    {
        estado=Estado.Espera;
        intentoPrevio=false;
        reloj=sosten=0;
        cinta.Clear();
        eco.gameObject.SetActive(false);
        hilo.positionCount=0;
        puente.Vincular();
        nilo.Colocar(origen);
        nilo.transform.rotation=rumbo;
        ActualizarPanel();
    }

    void ActualizarPanel()
    {
        bool reproduciendo=estado==Estado.Reproduciendo;
        bool grabando=estado==Estado.Grabando;
        bool abierto=estado==Estado.Abierto||estado==Estado.Recibido;
        bool norte=EnPlaca(nilo.transform,placaNorte)||(reproduciendo && EnPlaca(eco,placaNorte));
        bool sur=EnPlaca(nilo.transform,placaSur)||(reproduciendo && EnPlaca(eco,placaSur));
        Pintar(placaNorte,norte?ClaveMaterial.Turquesa:ClaveMaterial.NiloExtremidades);
        Pintar(placaSur,sur?ClaveMaterial.Turquesa:ClaveMaterial.NiloExtremidades);
        Pintar(pilotoConsola,grabando?ClaveMaterial.Coral:(reproduciendo?ClaveMaterial.Turquesa:ClaveMaterial.Ambar));
        Pintar(pilotoSalida,abierto?ClaveMaterial.Turquesa:ClaveMaterial.Cobre);
        if(grabando||reproduciendo)
            foreach(var reel in carretes) reel.Rotate(Vector3.up,Time.deltaTime*(grabando?150f:-150f));
        var mando=Gamepad.current;
        bool usaMando=mando!=null && (Keyboard.current==null || mando.lastUpdateTime>Keyboard.current.lastUpdateTime);
        string tecla=usaMando?mando.buttonNorth.displayName:"E";
        if (Campana)
        {
            if (receptorRemoto != null) Pintar(receptorRemoto, abierto || EnPlaca(nilo.transform,receptorRemoto) ? ClaveMaterial.Turquesa : ClaveMaterial.NiloExtremidades);
            PanelCampana(grabando,reproduciendo,tecla);
            return;
        }
        string accion;
        switch(estado)
        {
            case Estado.Grabando: accion="Quédate en la placa. Usa "+tecla+" para cortar antes."; break;
            case Estado.Reproduciendo: accion="Pisa la otra placa mientras el eco ocupa la primera."; break;
            case Estado.Abierto: accion=Cerca(nilo.transform.position,salida.position,1.8f)?tecla+" · Recibir la emisión":"Cruza la pasarela y sintoniza la radio."; break;
            case Estado.Recibido: accion="«Recibido, turno de noche». La misma voz suena en las dos orillas."; break;
            default: accion=Cerca(nilo.transform.position,consola.position,1.85f)?tecla+" · Grabar seis segundos":"Vuelve al magnetófono para grabar otra toma."; break;
        }
        string modo=grabando?"GRABANDO":(reproduciendo?"ECO EN EL AIRE":(abierto?(estado==Estado.Recibido?"SEÑAL RECIBIDA":"PASARELA ABIERTA"):(intentoPrevio?"CINTA AGOTADA":"CINTA LISTA")));
        monitor.Mostrar(modo,accion,grabando,reproduciendo,abierto?1f:reloj/(reproduciendo ? Longitud : capacidad),norte,sur,usaMando,abierto ? Longitud : reloj);
    }

    static void Pintar(Renderer r,ClaveMaterial clave)
    {
        if (r == null) return;
        var m=M(clave);
        if(r.sharedMaterial!=m) r.sharedMaterial=m;
    }
}

