using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Un plano del prólogo: mientras dura su clip en la Timeline, la cámara recorre su raíl (Spline
/// Dolly) de principio a fin y la mirada pasa de "Mira inicio" a "Mira fin".
///
/// El tiempo del plano sale del propio clip de la pista de Cinemachine: si alargas, acortas o
/// mueves el clip en la Timeline, el movimiento se ajusta solo. Funciona también en el editor al
/// arrastrar el cursor de la Timeline, para encuadrar sin darle Play.
///
/// Para retocar un plano: mueve los puntos del raíl ("Rail"), las miras, el FOV de la cámara o la
/// fuerza del temblor (Noise) en el inspector.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(CinemachineCamera))]
public sealed class PlanoPrologo : MonoBehaviour
{
    public PlayableDirector director;
    [Tooltip("Hacia dónde mira la cámara al empezar y al terminar el plano.")]
    public Transform miraInicio, miraFin;
    [Tooltip("El objetivo que sigue la cámara (se coloca solo entre las dos miras).")]
    public Transform mira;
    [Tooltip("0 = velocidad constante; 1 = arranca y frena suave.")]
    [Range(0f, 1f)] public float suavizado = 1f;

    CinemachineCamera cam;
    CinemachineSplineDolly dolly;

    /// <summary>Progreso 0..1 del plano en el instante actual de la Timeline (-1 si no está).</summary>
    public float Progreso { get; private set; }
    public double Inicio { get; private set; }
    public double Fin { get; private set; }

    void OnEnable() { cam = GetComponent<CinemachineCamera>(); dolly = GetComponent<CinemachineSplineDolly>(); }

    void Update() { Aplicar(); }

    public void Aplicar()
    {
        if (director == null) return;
        if (cam == null) OnEnable();
        double ini, fin;
        if (!Tiempos(out ini, out fin)) return;
        Inicio = ini; Fin = fin;
        float u = Mathf.Clamp01((float)((director.time - ini) / System.Math.Max(0.01, fin - ini)));
        float s = Mathf.Lerp(u, u * u * (3f - 2f * u), suavizado);
        Progreso = s;
        if (dolly != null) dolly.CameraPosition = s;
        if (mira != null && miraInicio != null && miraFin != null)
            mira.position = Vector3.Lerp(miraInicio.position, miraFin.position, s);
    }

    bool Tiempos(out double ini, out double fin)
    {
        ini = fin = 0;
        TimelineAsset tl = director.playableAsset as TimelineAsset;
        if (tl == null) return false;
        foreach (TrackAsset pista in tl.GetOutputTracks())
        {
            if (!(pista is CinemachineTrack)) continue;
            foreach (TimelineClip c in pista.GetClips())
            {
                CinemachineShot plano = c.asset as CinemachineShot;
                if (plano == null) continue;
                bool valido;
                Object v = director.GetReferenceValue(plano.VirtualCamera.exposedName, out valido);
                if (valido && v == cam) { ini = c.start; fin = c.end; return true; }
            }
        }
        return false;
    }
}
