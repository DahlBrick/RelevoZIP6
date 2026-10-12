using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Datos persistidos en disco. Se guardan al terminar cada nivel.</summary>
[System.Serializable]
public class SaveData
{
    public int version = 1;
    public List<string> nivelesCompletados = new List<string>();
    public List<string> recuerdos = new List<string>();
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
        try
        {
            if (File.Exists(RutaArchivo))
            {
                string texto = File.ReadAllText(RutaArchivo);
                datos = JsonUtility.FromJson<SaveData>(texto);
                Debug.Log("RELEVO: progreso cargado desde " + RutaArchivo);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("RELEVO: no se pudo leer el progreso (" + e.Message + "). Se empieza de cero.");
        }

        if (datos == null)
        {
            datos = new SaveData();
        }

        if (datos.nivelesCompletados == null) datos.nivelesCompletados = new List<string>();
        if (datos.recuerdos == null) datos.recuerdos = new List<string>();
    }

    public static void Guardar()
    {
        try
        {
            File.WriteAllText(RutaArchivo, JsonUtility.ToJson(Datos, true));
            Debug.Log("RELEVO: progreso guardado en " + RutaArchivo);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("RELEVO: no se pudo guardar el progreso (" + e.Message + ").");
        }
    }

    static string ClaveNivel(int indice)
    {
        return "nivel" + indice;
    }

    public static bool NivelCompletado(int indice)
    {
        return Datos.nivelesCompletados.Contains(ClaveNivel(indice));
    }

    public static void MarcarNivelCompletado(int indice)
    {
        if (NivelCompletado(indice)) return;
        Datos.nivelesCompletados.Add(ClaveNivel(indice));
        Guardar();
    }

    public static bool NivelDesbloqueado(int indice)
    {
        if (indice <= 1) return true;
        return NivelCompletado(indice - 1);
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
