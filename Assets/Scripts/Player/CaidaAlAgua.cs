using System.Collections;
using UnityEngine;

/// <summary>
/// Nilo cae al agua cuando un puente se repliega bajo sus pies (un condensador que se vacía a
/// mitad del cruce, una placa que se suelta). Se hunde con un chapoteo, Maia lo cuenta y el nivel
/// vuelve a empezar al cerrar el diálogo, igual que al apagarse por falta de reserva.
/// </summary>
public sealed class CaidaAlAgua : MonoBehaviour
{
    public static bool Activa { get; private set; }

    public static void Empezar(PlayerController nilo)
    {
        if (Activa || nilo == null) return;
        if (RelevoGame.Reserva != null && RelevoGame.Reserva.Apagado) return;
        Activa = true;
        nilo.gameObject.AddComponent<CaidaAlAgua>();
    }

    void OnDestroy() { Activa = false; }

    IEnumerator Start()
    {
        PlayerController nilo = GetComponent<PlayerController>();
        if (nilo != null) nilo.PuedeMoverse = false;
        if (RelevoGame.UI != null) RelevoGame.UI.RefrescarBloqueo();

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        AudioDirector.Pedir(Tono.PuenteRetraido);

        Vector3 inicio = transform.position;
        Quaternion giro = transform.rotation;
        float t = 0f;
        const float duracion = 1.1f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float s = Mathf.Clamp01(t / duracion);
            transform.position = inicio + Vector3.down * (1.9f * s * s);
            transform.rotation = giro * Quaternion.Euler(18f * s, 0f, 9f * Mathf.Sin(s * 9f) * (1f - s));
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        DialogoUI dialogo = RelevoGame.UI != null ? RelevoGame.UI.Dialogo : null;
        const string texto = "¡Nilo! El puente se replegó contigo encima. Espera a que el condensador esté lleno y cruza sin detenerte.";
        if (dialogo != null)
        {
            dialogo.Encolar(new DialogoUI.Mensaje
            {
                titulo = "Maia",
                texto = texto,
                acento = RelevoUI.Verde,
                alCerrar = delegate { if (RelevoGame.Nivel != null) RelevoGame.Nivel.ReiniciarNivel(); }
            });
        }
        else if (RelevoGame.Nivel != null)
        {
            RelevoGame.Nivel.ReiniciarNivel();
        }
    }
}
