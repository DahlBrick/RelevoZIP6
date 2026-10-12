using System.Collections.Generic;
using UnityEngine;

/// <summary>Clase de casilla del mapa de texto.</summary>
public enum TipoTile
{
    Agua,
    Vacio,
    Piedra,
    Madera,
    Cobre,
    Vegetacion,
    PuenteTemporal,
    PasarelaPermanente
}

/// <summary>
/// Nivel descrito como un mapa de texto: una fila por línea, de norte (arriba) a sur.
/// Leyenda de caracteres:
///   w agua        . vacío (bloquea sin dibujar)   # piedra crema   m madera oscura
///   c cobre       v piedra con vegetación y farol
///   B puente eléctrico de la base A   C puente eléctrico de la base D
///   P pasarela manual de la palanca L  Q pasarela manual de la palanca K
///   A base A      D base D      L palanca 1   K palanca 2
///   E salida      X recuerdo    N inicio de Nilo   Y batería   M Maia
///   k caja empujable   o placa de la compuerta g   g compuerta
///   u placa del puente R   R puente de placa (se despliega mientras la placa u está pisada)
/// Las casillas marcadas (A, D, L, K, E, X, N, Y, M, k, o, u, g) se apoyan sobre piedra transitable.
/// </summary>
public sealed class LevelLayout
{
    public readonly string Nombre;
    public readonly float TamanoTile;
    public readonly string[] Filas;

    public LevelLayout(string nombre, float tamanoTile, params string[] filas)
    {
        Nombre = nombre;
        TamanoTile = tamanoTile;
        Filas = filas;
    }

    public int Columnas
    {
        get { return Filas.Length > 0 ? Filas[0].Length : 0; }
    }

    public int FilasCuenta
    {
        get { return Filas.Length; }
    }

    public char CharEn(int columna, int fila)
    {
        if (fila < 0 || fila >= Filas.Length) return 'w';
        string linea = Filas[fila];
        if (columna < 0 || columna >= linea.Length) return 'w';
        return linea[columna];
    }

    public Vector3 CentroTile(int columna, int fila)
    {
        return new Vector3((columna + 0.5f) * TamanoTile, 0f, -(fila + 0.5f) * TamanoTile);
    }

    /// <summary>Centro de una corrida horizontal: columna admite medias casillas.</summary>
    public Vector3 CentroTile(float columna, int fila)
    {
        return new Vector3((columna + 0.5f) * TamanoTile, 0f, -(fila + 0.5f) * TamanoTile);
    }

    /// <summary>Casilla que contiene un punto del mundo (inversa de CentroTile).</summary>
    public Vector2Int Casilla(Vector3 posicion)
    {
        return new Vector2Int(Mathf.FloorToInt(posicion.x / TamanoTile), Mathf.FloorToInt(-posicion.z / TamanoTile));
    }

    public static TipoTile Mapear(char c)
    {
        switch (c)
        {
            case 'R': return TipoTile.PuenteTemporal;
            case 'm': return TipoTile.Madera;
            case 'c': return TipoTile.Cobre;
            case 'v': return TipoTile.Vegetacion;
            case 'B': return TipoTile.PuenteTemporal;
            case 'C': return TipoTile.PuenteTemporal;
            case 'P': return TipoTile.PasarelaPermanente;
            case 'Q': return TipoTile.PasarelaPermanente;
            case '.': return TipoTile.Vacio;
            case 'w': return TipoTile.Agua;
            default: return TipoTile.Piedra;
        }
    }

    public static bool EsTerrenoTransitable(TipoTile tipo)
    {
        return tipo == TipoTile.Piedra || tipo == TipoTile.Madera || tipo == TipoTile.Cobre || tipo == TipoTile.Vegetacion;
    }

    public List<Vector2Int> Casillas(char caracter)
    {
        var lista = new List<Vector2Int>();
        for (int fila = 0; fila < FilasCuenta; fila++)
        {
            for (int columna = 0; columna < Columnas; columna++)
            {
                if (CharEn(columna, fila) == caracter) lista.Add(new Vector2Int(columna, fila));
            }
        }
        return lista;
    }

    public List<Vector3> CentrosDeCasillas(char caracter)
    {
        var centros = new List<Vector3>();
        List<Vector2Int> casillas = Casillas(caracter);
        for (int i = 0; i < casillas.Count; i++)
        {
            centros.Add(CentroTile(casillas[i].x, casillas[i].y));
        }
        return centros;
    }

    public bool TieneCasilla(char caracter)
    {
        return Casillas(caracter).Count > 0;
    }

    /// <summary>Posición del primer marcador encontrado, o 'porDefecto' si no existe.</summary>
    public Vector3 PrimerMarcador(char caracter, Vector3 porDefecto)
    {
        List<Vector2Int> casillas = Casillas(caracter);
        if (casillas.Count == 0) return porDefecto;
        return CentroTile(casillas[0].x, casillas[0].y);
    }

    /// <summary>Límites de la maqueta; los usa la cámara ortográfica fija.</summary>
    public Bounds Limites()
    {
        float ancho = Columnas * TamanoTile;
        float largo = FilasCuenta * TamanoTile;
        Vector3 centro = new Vector3(ancho * 0.5f, 0f, -largo * 0.5f);
        Vector3 tamano = new Vector3(ancho, 3.4f, largo);
        return new Bounds(centro, tamano);
    }
}
