using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>Reproduce la Timeline enlazada en esta escena al llegar o al completar el puerto.</summary>
[RequireComponent(typeof(PlayableDirector)), DisallowMultipleComponent, DefaultExecutionOrder(-500)]
public sealed class SecuenciaDePuerto : MonoBehaviour
{
    public enum Momento { Llegada, Cierre }
    [SerializeField] Momento momento;
    static SecuenciaDePuerto activa;
    static int ultimoCierre = -1;
    public static bool EnCurso { get { return activa != null; } }
    public static bool BloqueaEntrada { get { return EnCurso || Time.frameCount==ultimoCierre; } }

    PlayableDirector director;
    DirectorWrapMode modoAnterior;
    Action alTerminar;

    void Reset() { GetComponent<PlayableDirector>().playOnAwake=false; }
    void Awake()
    {
        director=GetComponent<PlayableDirector>();
        director.playOnAwake=false;
    }

    public static void Reproducir(Scene escena,Momento cuando,Action continuar)
    {
        foreach(var secuencia in FindObjectsByType<SecuenciaDePuerto>(FindObjectsSortMode.None))
        {
            if(secuencia.gameObject.scene!=escena || !secuencia.isActiveAndEnabled || secuencia.momento!=cuando)continue;
            var d=secuencia.GetComponent<PlayableDirector>();
            if(!d.isActiveAndEnabled || d.playableAsset==null || d.duration<=0)continue;
            secuencia.director=d;
            secuencia.alTerminar=continuar;
            secuencia.modoAnterior=d.extrapolationMode;
            activa=secuencia;
            if(RelevoGame.UI!=null)RelevoGame.UI.RefrescarBloqueo();
            d.extrapolationMode=DirectorWrapMode.None;
            d.stopped+=secuencia.AlDetener;
            d.time=0;
            d.Play();
            return;
        }
        continuar();
    }

    void Update()
    {
        if(activa!=this)return;
        var input=RelevoGame.Input;
        if(input!=null && (input.Pulsado(input.Cancelar) || input.Pulsado(input.Pausa)))director.Stop();
    }

    void AlDetener(PlayableDirector origen)
    {
        Action continuar=alTerminar;
        Soltar();
        if(continuar!=null && gameObject.scene.isLoaded)continuar();
    }

    void Soltar()
    {
        if (director != null)
        {
            director.stopped-=AlDetener;
            director.extrapolationMode=modoAnterior;
        }
        alTerminar=null;
        activa=null;
        ultimoCierre=Time.frameCount;
        if(RelevoGame.UI!=null)RelevoGame.UI.RefrescarBloqueo();
    }

    void OnDestroy() { if(activa==this)Soltar(); }
}
