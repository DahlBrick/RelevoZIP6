using UnityEngine;

/// <summary>La señal acciona el embrague de ida. El contrapeso permite volver sin corriente.</summary>
public sealed class TransbordadorDeMaroma : MonoBehaviour
{
    [SerializeField] EcoDeTurno cinta;
    [SerializeField] Transform casco, origen, destino;
    [SerializeField] Renderer pilotoIda, pilotoVuelta;
    bool enDestino, navegando, vuelta;
    float curso;
    PlayerController pasajero;
    Vector3 pieInicial, pieFinal;

    public bool EnDestino { get { return enDestino; } }
    public bool Navegando { get { return navegando; } }
    public Vector3 Embarque { get { return origen.position; } }
    public bool PuedeZarpar(bool regresar)
    {
        return !navegando && regresar == enDestino && (regresar || cinta.SenalDeSalida);
    }

    public string Motivo(bool regresar)
    {
        if (navegando) return "Transbordador en marcha";
        if (regresar != enDestino) return "Transbordador en la otra orilla";
        return "El eco debe pisar la placa I";
    }

    static GameObject Pieza(Transform padre, string nombre, Vector3 centro, Vector3 escala, ClaveMaterial material)
    {
        return DioramaBuilder.CrearCaja(nombre, padre, centro, escala, RelevoPalette.MaterialDe(material), false);
    }

    public void Construir(EcoDeTurno grabadora, Vector3 a, Vector3 b)
    {
        cinta = grabadora;
        origen = DioramaBuilder.CrearAncla("Torno de ida", transform, a);
        destino = DioramaBuilder.CrearAncla("Torno de retorno", transform, b);
        pilotoIda = Torno(origen, false);
        pilotoVuelta = Torno(destino, true);
        casco = DioramaBuilder.CrearAncla("Pontón de maroma", transform, Amarre(false));
        casco.rotation = Quaternion.LookRotation((b - a).normalized);
        for (int i = -1; i <= 1; i += 2)
        {
            Pieza(casco,"Flotador",new Vector3(i*0.94f,-0.23f,0),new Vector3(0.43f,0.46f,2.78f),ClaveMaterial.NiloExtremidades);
            Pieza(casco,"Regala",new Vector3(i*1.04f,0.12f,0),new Vector3(0.12f,0.21f,2.75f),ClaveMaterial.Cobre);
        }
        for (int i = -3; i <= 3; i++)
            Pieza(casco,"Tablón",new Vector3(0,0.02f,i*0.38f),new Vector3(2.09f,0.16f,0.34f),i%2==0 ? ClaveMaterial.MaderaOscura : ClaveMaterial.Cobre);
        Pieza(casco,"Franja de embarque",new Vector3(0,0.12f,0),new Vector3(0.16f,0.04f,2.54f),ClaveMaterial.PiedraCrema);
        Vector3 lateral = Vector3.Cross((b-a).normalized,Vector3.up)*1.18f;
        DioramaBuilder.CrearCajaEntre("Maroma tensa",transform,a+lateral+Vector3.up*0.42f,b+lateral+Vector3.up*0.42f,0.07f,RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
    }

    Renderer Torno(Transform puesto, bool regresar)
    {
        puesto.gameObject.AddComponent<PuestoDeRelevo>().Configurar(cinta,this,regresar);
        var lateral = Vector3.Cross((destino.position-origen.position).normalized,Vector3.up)*0.78f;
        Pieza(puesto,"Bancada",lateral+Vector3.up*0.26f,new Vector3(0.58f,0.52f,0.58f),ClaveMaterial.MaderaOscura);
        DioramaBuilder.CrearCilindro("Cabrestante",puesto,lateral+Vector3.up*0.59f,0.35f,0.20f,RelevoPalette.MaterialDe(ClaveMaterial.Cobre));
        return Pieza(puesto,"Piloto del embrague",lateral+Vector3.up*0.82f,new Vector3(0.33f,0.23f,0.33f),ClaveMaterial.Ambar).GetComponent<Renderer>();
    }

    Vector3 Amarre(bool lejano)
    {
        return (lejano ? destino.position : origen.position) + (destino.position-origen.position).normalized*(lejano ? -1.75f : 1.75f);
    }

    public void Zarpar(PlayerController jugador, bool regresar)
    {
        if (!PuedeZarpar(regresar)) return;
        vuelta = regresar;
        pasajero = jugador;
        pieInicial = jugador.transform.position;
        pieFinal = regresar ? origen.position : destino.position;
        pieFinal.y = pieInicial.y;
        curso = 0;
        navegando = true;
        jugador.GetComponent<CharacterController>().enabled = false;
        GameEvents.RaisePromptCambiado("",true);
        AudioDirector.Pedir(Tono.Insertar);
    }

    void LateUpdate()
    {
        pilotoIda.sharedMaterial = RelevoPalette.MaterialDe(PuedeZarpar(false) ? ClaveMaterial.Turquesa : ClaveMaterial.Cobre);
        pilotoVuelta.sharedMaterial = RelevoPalette.MaterialDe(PuedeZarpar(true) ? ClaveMaterial.Turquesa : ClaveMaterial.Cobre);
        if (!navegando || (RelevoGame.UI != null && RelevoGame.UI.BloqueaJuego)) return;
        curso = Mathf.Clamp01(curso + Time.deltaTime/2.65f);
        float tramo = Mathf.SmoothStep(0,1,Mathf.InverseLerp(0.16f,0.84f,curso));
        casco.position = Vector3.Lerp(Amarre(vuelta),Amarre(!vuelta),tramo);
        Vector3 cubierta = casco.position + Vector3.up*(pieInicial.y+0.16f);
        pasajero.transform.position = curso < 0.16f ? Vector3.Lerp(pieInicial,cubierta,curso/0.16f)
            : Vector3.Lerp(cubierta,pieFinal,Mathf.InverseLerp(0.84f,1,curso));
        if (curso < 1) return;
        pasajero.Colocar(pieFinal);
        enDestino = !vuelta;
        navegando = false;
        pasajero = null;
        AudioDirector.Pedir(Tono.Retirar);
    }
}
