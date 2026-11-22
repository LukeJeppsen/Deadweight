using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws text with the project's 5x7 pixel font (font_5x7.png) into a small Texture2D shown on a RawImage.
/// No font asset needed, always crisp, and supports a typewriter reveal. Text is shown in capitals.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class PixelText : MonoBehaviour
{
    public Texture2D atlas;                       // font_5x7.png, Read/Write enabled
    public Color tint = new Color32(0xFB, 0xF5, 0xEF, 0xFF);
    public Color shadowColor = new Color32(0x27, 0x27, 0x44, 0xFF);
    public bool shadow = true;
    public bool centered = false;
    public int lineHeight = 10;

    private const int Cols = 16, CellW = 8, CellH = 8, FirstChar = 32;
    private static Color32[] atlasPx;
    private static int atlasW, atlasH;
    private static Texture2D atlasCached;

    private struct Glyph { public int cell; public int x, y, w; }

    private RawImage raw;
    private Texture2D tex;
    private Color32[] buf;
    private readonly List<Glyph> glyphs = new List<Glyph>();
    private int tw, th, shown = -1;

    public int GlyphCount => glyphs.Count;

    private void Awake() { raw = GetComponent<RawImage>(); }

    /// <summary>Lays the text out within maxWidth pixels (word wrapped) and draws the first `reveal` glyphs.</summary>
    public void SetText(string text, int maxWidth, int reveal = int.MaxValue)
    {
        if (raw == null) raw = GetComponent<RawImage>();
        CacheAtlas();
        glyphs.Clear();
        if (string.IsNullOrEmpty(text)) text = "";

        int x = 0, line = 0;
        string[] paragraphs = text.Split('\n');
        for (int p = 0; p < paragraphs.Length; p++)
        {
            if (p > 0) { line++; x = 0; }
            string[] words = paragraphs[p].Split(' ');
            for (int w = 0; w < words.Length; w++)
            {
                string word = words[w];
                int ww = WordWidth(word);
                if (x > 0 && x + ww > maxWidth) { line++; x = 0; }
                foreach (char c in word)
                {
                    int cell = CellFor(c);
                    int gw = GlyphWidth(cell);
                    glyphs.Add(new Glyph { cell = cell, x = x, y = line * lineHeight, w = gw });
                    x += gw + 1;
                }
                x += 3;   // space
                if (w < words.Length - 1 && x > maxWidth) { /* trailing space at line end is harmless */ }
            }
        }

        int lines = line + 1;
        if (centered)
        {
            for (int ln = 0; ln < lines; ln++)
            {
                int minX = int.MaxValue, maxX = 0;
                foreach (Glyph g in glyphs)
                    if (g.y == ln * lineHeight) { minX = Mathf.Min(minX, g.x); maxX = Mathf.Max(maxX, g.x + g.w); }
                if (minX == int.MaxValue) continue;
                int shift = (maxWidth - (maxX - minX)) / 2 - minX;
                for (int i = 0; i < glyphs.Count; i++)
                {
                    Glyph g = glyphs[i];
                    if (g.y != ln * lineHeight) continue;
                    g.x += shift;
                    glyphs[i] = g;
                }
            }
        }
        int newW = Mathf.Max(1, maxWidth + 1), newH = Mathf.Max(1, lines * lineHeight + 1);
        if (tex == null || newW != tw || newH != th)
        {
            if (tex != null) Destroy(tex);
            tw = newW; th = newH;
            tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.name = "PixelText";
            buf = new Color32[tw * th];
            raw.texture = tex;
            ((RectTransform)raw.transform).sizeDelta = new Vector2(tw, th);
        }
        shown = -1;
        Reveal(reveal);
    }

    /// <summary>Show only the first n glyphs (typewriter effect).</summary>
    public void Reveal(int n)
    {
        n = Mathf.Clamp(n, 0, glyphs.Count);
        if (n == shown || tex == null) return;
        shown = n;
        System.Array.Clear(buf, 0, buf.Length);
        Color32 fg = tint, sh = shadowColor;
        if (shadow) for (int i = 0; i < n; i++) Draw(glyphs[i], 1, -1, sh);
        for (int i = 0; i < n; i++) Draw(glyphs[i], 0, 0, fg);
        tex.SetPixels32(buf);
        tex.Apply(false);
    }

    private void Draw(Glyph g, int dx, int dy, Color32 col)
    {
        int cx = (g.cell % Cols) * CellW;
        for (int gy = 0; gy < CellH; gy++)
        {
            int ay = (atlasH / CellH - 1 - g.cell / Cols) * CellH + (CellH - 1 - gy);
            for (int gx = 0; gx < g.w; gx++)
            {
                if (atlasPx[ay * atlasW + cx + gx].a == 0) continue;
                int px = g.x + gx + dx;
                int py = th - 1 - (g.y + gy) + dy;
                if (px < 0 || px >= tw || py < 0 || py >= th) continue;
                buf[py * tw + px] = col;
            }
        }
    }

    private int WordWidth(string word)
    {
        int w = 0;
        foreach (char c in word) w += GlyphWidth(CellFor(c)) + 1;
        return w;
    }

    private static int CellFor(char c)
    {
        if (c != '`') c = char.ToUpperInvariant(c);
        int code = c;
        if (code < FirstChar || code > 127) code = '?';
        return code - FirstChar;
    }

    private int GlyphWidth(int cell)
    {
        int cx = (cell % Cols) * CellW;
        int rowFromTop = cell / Cols;
        int widest = 0;
        for (int gy = 0; gy < CellH; gy++)
        {
            int ay = (atlasH / CellH - 1 - rowFromTop) * CellH + (CellH - 1 - gy);
            for (int gx = CellW - 1; gx >= widest; gx--)
            {
                if (atlasPx[ay * atlasW + cx + gx].a != 0) { widest = gx + 1; break; }
            }
        }
        return widest == 0 ? 3 : widest;
    }

    private void CacheAtlas()
    {
        if (atlas == null) { Debug.LogError("PixelText: assign the font atlas."); atlasPx = new Color32[Cols * CellW * 6 * CellH]; atlasW = Cols * CellW; atlasH = 6 * CellH; return; }
        if (atlasCached == atlas && atlasPx != null) return;
        atlasCached = atlas;
        atlasW = atlas.width;
        atlasH = atlas.height;
        atlasPx = atlas.GetPixels32();
    }

    private void OnDestroy() { if (tex != null) Destroy(tex); }
}
