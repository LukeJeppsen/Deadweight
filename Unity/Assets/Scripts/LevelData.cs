using System;
using UnityEngine;

/// <summary>One placed object in a level file (anything that is not a plain tile glyph).</summary>
[Serializable]
public class LevelObjectData
{
    public string t;            // hint, wind, jet, beam
    public int x, y;            // cell column / row of its top-left cell (row 0 = top of the level)
    public int w = 1, h = 1;    // size in cells
    public int len = 1;         // jets / beams: length in cells
    public string dir = "u";    // jets / beams: u, d, l, r
    public string text;         // hints
    public float vx, vy, accel = 90f;     // wind
    public float on = 1.1f, off = 1.9f, warn = 0.6f, phase;   // jets / beams
}

/// <summary>A level file (Assets/Levels/*.json). Each row is a string, row 0 is the top; one character = one 8x8 tile.</summary>
[Serializable]
public class LevelData
{
    public string id;
    public string name;
    public int cols;
    public string[] rows;
    public LevelObjectData[] objects;

    public const float Cell = 0.5f;

    public int Rows { get { return rows != null ? rows.Length : 0; } }

    public char At(int col, int row)
    {
        if (rows == null || row < 0 || row >= rows.Length) return ' ';
        string r = rows[row];
        if (col < 0 || col >= r.Length) return ' ';
        return r[col];
    }

    /// <summary>World position of the CENTRE of a cell.</summary>
    public Vector2 CellCenter(int col, int row)
    {
        return new Vector2((col + 0.5f) * Cell, (Rows - 1 - row + 0.5f) * Cell);
    }

    public Rect WorldBounds
    {
        get { return new Rect(0f, 0f, cols * Cell, Rows * Cell); }
    }
}
