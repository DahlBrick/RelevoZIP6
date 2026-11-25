using UnityEditor;
using UnityEngine;

/// <summary>
/// Copia la narrativa escrita en DefinicionesPorDefecto (código) a los guiones de
/// Resources/Bitacora/Guiones/PuertoNN.asset, que son los que el juego carga.
/// Solo toca textos de historia y el recuerdo; reservas y tutoriales se quedan como estén.
/// </summary>
public static class SincronizarGuiones
{
    [MenuItem("Relevo/Sincronizar guiones con el código")]
    public static void Sincronizar()
    {
        int n = 0;
        for (int i = 1; i <= LevelLayouts.Cantidad; i++)
        {
            string ruta = "Assets/Resources/Bitacora/Guiones/Puerto" + i.ToString("00") + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelDefinition>(ruta);
            if (asset == null) continue;
            LevelDefinition d = DefinicionesPorDefecto.Obtener(i);
            asset.textoIntro = d.textoIntro;
            asset.textoCierre = d.textoCierre;
            asset.recuerdoIdentificador = d.recuerdoIdentificador;
            asset.recuerdoTitulo = d.recuerdoTitulo;
            asset.recuerdoTexto = d.recuerdoTexto;
            EditorUtility.SetDirty(asset);
            Object.DestroyImmediate(d);
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Guiones sincronizados: " + n);
    }
}
