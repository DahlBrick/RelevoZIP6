using System.Text;
using UnityEngine;

/// <summary>
/// Reglas de la historia de RELEVO que cruzan todos los niveles:
/// - Los cuatro finales y cuál toca según las dos decisiones y los recuerdos.
/// - Las dos decisiones (tras el Puerto 6 y tras el 12). Hoy se toman en un diálogo provisional;
///   las cinemáticas de decisión las sustituirán llamando a SaveManager.Decidir igual.
/// - La voz de Maia, que se rompe a medida que Nilo recuerda (InterferenciaMaia).
/// Guion completo en docs/narrativa/historia_final.md.
/// </summary>
public static class Historia
{
    public const int RecuerdosTotales = 12;

    public enum Final { ElRelevo, EncenderLiria, FueElla, Estatica }

    public static int RecuerdosEncontrados
    {
        get
        {
            int n = 0;
            for (int i = 1; i <= RecuerdosTotales; i++)
                if (SaveManager.TieneRecuerdo("nivel" + i + "_recuerdo")) n++;
            return n;
        }
    }

    /// <summary>
    /// Recordar + ciudad + todos los recuerdos: El relevo. Recordar en otro caso: Encender Liria.
    /// Borrar + ciudad: Fue ella. Borrar + Maia: Estática. Sin decisiones tomadas cuenta como recordar
    /// y ciudad.
    /// </summary>
    public static Final FinalActual
    {
        get
        {
            bool borrar = SaveManager.DecisionMemoria == "borrar";
            bool maia = SaveManager.DecisionEnergia == "maia";
            if (borrar) return maia ? Final.Estatica : Final.FueElla;
            if (!maia && RecuerdosEncontrados >= RecuerdosTotales) return Final.ElRelevo;
            return Final.EncenderLiria;
        }
    }

    public static string TituloFinal(Final f)
    {
        switch (f)
        {
            case Final.ElRelevo: return "Final · El relevo";
            case Final.FueElla: return "Final · Fue ella";
            case Final.Estatica: return "Final · Estática";
            default: return "Final · Encender Liria";
        }
    }

    public static string TextoFinal(Final f)
    {
        switch (f)
        {
            case Final.ElRelevo:
                return "El Eco se deshace en la luz. Nilo deja la batería en el núcleo y ya no la vuelve a sacar. Liria se enciende entera, barrio a barrio. La radio emite por última vez: «Apagarse también es parte de la canción». Nilo se apaga despacio, con la ciudad iluminada a sus pies. Esta vez, el relevo lo lleva él.";
            case Final.FueElla:
                return "El Eco se deshace en la luz y Liria se enciende. Luego Nilo corta la señal de la emisora con un golpe seco. Fue la radio, se dice. Fue ella quien se lo pidió. La ciudad brilla. La radio no vuelve a sonar, y Nilo no vuelve a preguntarse por qué.";
            case Final.Estatica:
                return "El Eco se deshace, pero Nilo desvía la energía a la emisora y vuelve al banco de fundición. Ruido. Un muelle. Una radio que dice: «Buenas noches a quienes siguen de turno». Junto a Nilo, una batería. En la baranda hay trece rayas que antes no estaban.";
            default:
                return "El Eco se deshace en la luz y, una a una, las ventanas de Liria se encienden. En la radio, la voz de Maia suena muy lejos: «Apagarse también es parte de la canción». Después, solo silencio. Nilo se queda en la subestación, reparando lo que rompió. Nadie viene a darle las gracias. Nadie viene a culparlo. Sigue de turno.";
        }
    }

    // ------------------------------------------------------------------ decisiones

    /// <summary>Punto de decisión tras un puerto (1 tras el 6, 2 tras el 12), o 0 si no hay.</summary>
    public static int DecisionTrasPuerto(int puerto)
    {
        if (puerto == 6) return 1;
        if (puerto == 12) return 2;
        return 0;
    }

    /// <summary>Cinemática que se juega al dejar un puerto, o null si no hay.</summary>
    public static string CinematicaTrasPuerto(int puerto)
    {
        switch (puerto)
        {
            case 6: return CinematicaAntena.Escena;
            case 10: return CinematicaRenglon.Escena;
            case 12: return CinematicaFundicion.Escena;
            default: return null;
        }
    }

    public static string TituloDecision(int punto) { return punto == 1 ? "La antena" : "El banco de fundición"; }

    public static string TextoDecision(int punto)
    {
        return punto == 1
            ? "La esclusa devuelve corriente a toda la orilla. En la torre de la emisora, la antena de Maia parpadea, casi apagada. «Si me la das a mí, seguiré contigo un poco más», dice la radio. Al otro lado, las ventanas del barrio esperan a oscuras. La energía solo alcanza para una de las dos."
            : "Nilo ya sabe lo que hizo. El banco de fundición sigue templado y el molde tiene la forma exacta de un disco de memoria. Podría fundirlo otra vez y despertar sin culpa, como la primera vez. O podría quedarse con todo y bajar a la subestación sabiendo quién es.";
    }

    public static string[] OpcionesDecision(int punto)
    {
        return punto == 1
            ? new[] { "Dar la energía al barrio", "Dar la energía a la antena de Maia" }
            : new[] { "Recordarlo todo", "Fundir el disco otra vez" };
    }

    public static string ValorDecision(int punto, int opcion)
    {
        if (punto == 1) return opcion == 0 ? "ciudad" : "maia";
        return opcion == 0 ? "recordar" : "borrar";
    }

    // ------------------------------------------------------------------ voz de Maia

    /// <summary>
    /// Nivel de interferencia de la voz de Maia en un puerto: 0 limpia (1-4), 1 cortes (5-6),
    /// 2 ruido (7-9), 3 se rompe (10-12), 4 fragmentos (13). Darle la energía a la antena la
    /// mantiene un escalón más clara.
    /// </summary>
    public static int Interferencia(int puerto)
    {
        int t = puerto <= 4 ? 0 : puerto <= 6 ? 1 : puerto <= 9 ? 2 : puerto <= 12 ? 3 : 4;
        if (puerto > 6 && SaveManager.DecisionEnergia == "maia") t = Mathf.Max(1, t - 1);
        return t;
    }

    /// <summary>Rompe un texto de Maia como una radio que pierde la señal. Determinista por texto.</summary>
    public static string InterferenciaMaia(string texto, int puerto)
    {
        int t = Interferencia(puerto);
        if (t == 0 || string.IsNullOrEmpty(texto)) return texto;

        string[] palabras = texto.Split(' ');
        var r = new System.Random(texto.GetHashCode() ^ puerto);
        var sb = new StringBuilder();
        bool estribillo = false;
        for (int i = 0; i < palabras.Length; i++)
        {
            string w = palabras[i];
            double x = r.NextDouble();
            if (t >= 4 && i % 5 > 1) { if (i % 5 == 2) sb.Append("… "); continue; }
            if (t >= 3 && x < 0.08 && w.Length > 3) w = w.Substring(0, w.Length / 2) + "·" + w.Substring(w.Length / 2 + 1);
            sb.Append(w);
            if (t >= 2 && x > 0.9) sb.Append(" —kssh—");
            else if (t >= 2 && x > 0.86) sb.Append(" " + palabras[i]);   // la radio repite
            else if (x > 0.93) sb.Append("…");
            if (t >= 3 && !estribillo && i == palabras.Length / 2) { sb.Append(" «buenas noches a quienes…»"); estribillo = true; }
            sb.Append(' ');
        }
        return sb.ToString().TrimEnd();
    }
}
