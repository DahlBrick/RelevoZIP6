using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Datos persistidos en disco. Se guardan al terminar cada nivel.</summary>
[System.Serializable]
public class SaveData
{
    public int version = 1;
    public DestinoCorriente ramalLonja;
    public List<string> nivelesCompletados = new List<string>();
    public List<string> recuerdos = new List<string>();

    // Recuerdos ya leídos en el cuaderno. Se separa de 'recuerdos' (descubiertos) para poder
    // distinguir los tres estados: bloqueado, descubierto y leído. Campo nuevo y aditivo: los
    // archivos antiguos se leen igual porque Cargar() lo deja en lista vacía si falta.
    public List<string> recuerdosLeidos = new List<string>();
}

/// <summary>Progreso local en JSON (Application.persistentDataPath/relevo_save.json).</summary>
public static class SaveManager
{
    const string Archivo = "relevo_save.json";
    static SaveData datos;

    public static SaveData Datos
    {
        get
        {
            if (datos == null) Cargar();
            return datos;
        }
    }

    public static string RutaArchivo
    {
        get { return Path.Combine(Application.persistentDataPath, Archivo); }
    }

    public static void Cargar()
    {
        datos = LeerProgreso(RutaArchivo);
    }

    static bool IntentarLeer(string ruta, out SaveData progreso)
    {
        progreso = null;
        if (!File.Exists(ruta)) return false;
        try
        {
            string json = File.ReadAllText(ruta);
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{") ||
                (!json.Contains("\"nivelesCompletados\"") && !json.Contains("\"recuerdos\""))) return false;
            progreso = JsonUtility.FromJson<SaveData>(json);
            if (progreso == null || (progreso.nivelesCompletados == null && progreso.recuerdos == null)) return false;
            if (progreso.nivelesCompletados == null) progreso.nivelesCompletados = new List<string>();
            if (progreso.recuerdos == null) progreso.recuerdos = new List<string>();
            if (progreso.recuerdosLeidos == null) progreso.recuerdosLeidos = new List<string>();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("RELEVO: no se pudo leer " + Path.GetFileName(ruta) + " (" + e.Message + ").");
            return false;
        }
    }

    static SaveData LeerProgreso(string ruta)
    {
        SaveData progreso;
        if (IntentarLeer(ruta, out progreso) || IntentarLeer(ruta + ".bak", out progreso)) return progreso;
        return new SaveData();
    }

    public static void Guardar()
    {
        try { EscribirProgreso(RutaArchivo, Datos); }
        catch (System.Exception e) { Debug.LogWarning("RELEVO: no se pudo guardar el progreso (" + e.Message + ")."); }
    }

    static void EscribirProgreso(string ruta, SaveData progreso)
    {
        string temporal = ruta + "." + System.Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporal, JsonUtility.ToJson(progreso, true));
            if (!File.Exists(ruta))
            {
                File.Move(temporal, ruta);
                if (!File.Exists(ruta + ".bak")) File.Copy(ruta, ruta + ".bak");
            }
            else
            {
                // Si recuperamos la partida desde .bak, conservamos esa copia válida.
                SaveData anterior;
                string respaldo = IntentarLeer(ruta, out anterior) ? ruta + ".bak" : null;
                File.Replace(temporal, ruta, respaldo);
            }
        }
        finally { if (File.Exists(temporal)) File.Delete(temporal); }
    }

    static string ClaveNivel(int indice)
    {
        return "nivel" + indice;
    }

    public static bool NivelCompletado(int indice)
    {
        return Datos.nivelesCompletados.Contains(ClaveNivel(indice));
    }

    public static void MarcarNivelCompletado(int indice, DestinoCorriente ramal = DestinoCorriente.Ninguno)
    {
        bool completado = NivelCompletado(indice);
        bool nuevaEleccion = indice == 7 && ramal != DestinoCorriente.Ninguno && Datos.ramalLonja != ramal;
        if (completado && !nuevaEleccion) return;
        if (!completado) Datos.nivelesCompletados.Add(ClaveNivel(indice));
        if (nuevaEleccion) Datos.ramalLonja = ramal;
        Guardar();
    }

    public static bool NivelDesbloqueado(int indice)
    {
        return indice >= 1 && indice <= LevelLayouts.Cantidad &&
            (indice == 1 || NivelCompletado(indice) || NivelCompletado(indice - 1));
    }

    public static int PuertoParaContinuar()
    {
        for (int i = LevelLayouts.Cantidad; i > 1; i--)
            if (NivelDesbloqueado(i)) return i;
        return 1;
    }

    public static void RegistrarRecuerdo(string identificador)
    {
        if (string.IsNullOrEmpty(identificador)) return;
        if (Datos.recuerdos.Contains(identificador)) return;
        Datos.recuerdos.Add(identificador);
    }

    public static bool TieneRecuerdo(string identificador)
    {
        return !string.IsNullOrEmpty(identificador) && Datos.recuerdos.Contains(identificador);
    }

    /// <summary>
    /// El recuerdo ya se ha abierto en el cuaderno. Solo tiene sentido si antes se descubrió, así
    /// que la comprobación de descubierto se deja en manos de quien pregunta (TieneRecuerdo).
    /// </summary>
    public static bool RecuerdoLeido(string identificador)
    {
        return !string.IsNullOrEmpty(identificador) && Datos.recuerdosLeidos.Contains(identificador);
    }

    /// <summary>Marca el recuerdo como leído. No guarda: el guardado lo decide quien llama.</summary>
    public static void MarcarRecuerdoLeido(string identificador)
    {
        if (string.IsNullOrEmpty(identificador)) return;
        if (Datos.recuerdosLeidos.Contains(identificador)) return;
        Datos.recuerdosLeidos.Add(identificador);
    }

    public static int RecuerdosDelNivel(int indice)
    {
        string prefijo = ClaveNivel(indice) + "_recuerdo";
        int total = 0;
        for (int i = 0; i < Datos.recuerdos.Count; i++)
        {
            if (Datos.recuerdos[i].StartsWith(prefijo)) total++;
        }
        return total;
    }

    public static void ReiniciarProgreso()
    {
        datos = new SaveData();
        Guardar();
    }
}
