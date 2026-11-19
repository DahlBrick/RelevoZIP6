using UnityEngine;

/// <summary>La campana y su testigo repiten el cambio de estado del puente de su muelle.</summary>
public sealed class CampanaDePaso : MonoBehaviour
{
    [SerializeField] TemporaryBridge puente;
    [SerializeField] Transform yugo;
    [SerializeField] Renderer testigo;
    bool levantado;
    float oscilacion;
    public void Construir(TemporaryBridge destino)
    {
        puente=destino;
        for(int i=-1;i<=1;i+=2)
            PuertosDeSal.Caja(transform,"Horquilla",new Vector3(i*0.67f,1.05f,0),new Vector3(0.19f,2.1f,0.3f),ClaveMaterial.MaderaOscura);
        PuertosDeSal.Caja(transform,"Dintel",Vector3.up*2.12f,new Vector3(1.65f,0.21f,0.45f),ClaveMaterial.Cobre);
        yugo=DioramaBuilder.CrearAncla("Yugo de bronce",transform,Vector3.up*1.98f);
        for(int i=0;i<4;i++)
            DioramaBuilder.CrearCilindro("Bronce",yugo,Vector3.down*(0.16f+i*0.14f),0.24f+i*0.08f,0.17f,PuertosDeSal.Mat(ClaveMaterial.Ambar));
        DioramaBuilder.CrearEsfera("Badajo",yugo,Vector3.down*0.78f,0.28f,PuertosDeSal.Mat(ClaveMaterial.Cobre));
        testigo=PuertosDeSal.Caja(transform,"Testigo",new Vector3(0,2.30f,0),new Vector3(0.46f,0.16f,0.32f),ClaveMaterial.Cobre).GetComponent<Renderer>();
    }

    void Update()
    {
        if(puente.Desplegado!=levantado)
        {
            levantado=puente.Desplegado;
            testigo.sharedMaterial=PuertosDeSal.Mat(levantado?ClaveMaterial.Turquesa:ClaveMaterial.Cobre);
            oscilacion=1f;
            AudioDirector.Pedir(levantado?Tono.PlacaPisada:Tono.PlacaSuelta);
        }
        if(oscilacion<=0)return;
        oscilacion=Mathf.Max(0,oscilacion-Time.deltaTime*0.82f);
        yugo.localRotation=Quaternion.Euler(0,0,oscilacion*Mathf.Sin((1f-oscilacion)*14f)*30f);
    }
}
