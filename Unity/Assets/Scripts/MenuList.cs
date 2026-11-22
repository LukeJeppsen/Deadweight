using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A vertical pixel-text menu (used by the pause menu and the title screen's Options).
/// Rows can be plain choices (confirm runs them) or settings (left / right change them, confirm or a click steps them).
/// Driven by keyboard, gamepad or mouse through MenuInput; call Tick() once a frame while it is shown.
/// </summary>
public class MenuList
{
    private class Row
    {
        public string label;
        public Func<string> value;     // null = a plain choice
        public Action<int> change;     // settings: -1 / +1 from left / right, 0 = confirm or click (cycle)
        public Action choose;          // plain choices
        public PixelText text;
        public RectTransform rect;
    }

    private readonly List<Row> rows = new List<Row>();
    private readonly Transform parent;
    private readonly Texture2D font;
    private readonly Color textColor, accentColor, shadowColor;
    private readonly float top, spacing;
    private readonly int width;

    public int Index;

    public MenuList(Transform parent, Texture2D font, Color text, Color accent, Color shadow, float top, float spacing, int width)
    {
        this.parent = parent;
        this.font = font;
        textColor = text; accentColor = accent; shadowColor = shadow;
        this.top = top; this.spacing = spacing; this.width = width;
    }

    public int Count { get { return rows.Count; } }

    public void AddChoice(string label, Action choose)
    {
        AddRow(new Row { label = label, choose = choose });
    }

    public void AddSetting(string label, Func<string> value, Action<int> change)
    {
        AddRow(new Row { label = label, value = value, change = change });
    }

    /// <summary>A 0..max volume-style setting. Left / right step it; confirm steps up and wraps round to 0.</summary>
    public void AddSlider(string label, Func<int> get, Action<int> set, int max)
    {
        AddSetting(label, () => get() + " OF " + max, step =>      // the pixel font has no "/"
        {
            int cur = get();
            int v = step == 0 ? (cur >= max ? 0 : cur + 1) : Mathf.Clamp(cur + step, 0, max);
            set(v);
        });
    }

    /// <summary>An ON / OFF setting. Any of left / right / confirm flips it.</summary>
    public void AddToggle(string label, Func<bool> get, Action<bool> set)
    {
        AddSetting(label, () => get() ? "ON" : "OFF", _ => set(!get()));
    }

    private void AddRow(Row r)
    {
        int i = rows.Count;
        r.text = UIKit.MakeText(parent, "Item" + i, font, r.label, width, textColor, true,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top - i * spacing), 1f, true, shadowColor);
        r.rect = (RectTransform)r.text.transform;
        rows.Add(r);
    }

    public void Refresh()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            Row r = rows[i];
            bool sel = i == Index;
            string label = r.value != null ? r.label + " " + r.value() : r.label;
            r.text.tint = sel ? accentColor : textColor;
            r.text.SetText(sel ? "> " + label : label, width);      // the pixel font has no "<"
        }
    }

    /// <summary>Handles one frame of input. Back is left to the owner.</summary>
    public void Tick()
    {
        if (rows.Count == 0) return;
        int old = Index;
        if (MenuInput.Up()) Index = (Index + rows.Count - 1) % rows.Count;
        if (MenuInput.Down()) Index = (Index + 1) % rows.Count;
        if (MenuInput.PointerMoved())
        {
            for (int i = 0; i < rows.Count; i++) if (MenuInput.PointerOver(rows[i].rect)) Index = i;
        }
        if (old != Index) { Refresh(); Sfx.Play("menu_move", 1f, 0f); }

        Row r = rows[Index];
        bool click = MenuInput.Clicked() && MenuInput.PointerOver(r.rect);
        if (r.value != null)
        {
            int step = MenuInput.Right() ? 1 : MenuInput.Left() ? -1 : 0;
            bool confirm = MenuInput.Confirm() || click;
            if (step != 0 || confirm)
            {
                r.change(step);
                Refresh();
                Sfx.Play("menu_move", 1f, 0f);    // played after the change, so a volume step is heard at the new level
            }
            return;
        }
        if (MenuInput.Confirm() || click)
        {
            Sfx.Play("menu_confirm", 1f, 0f);
            r.choose();
        }
    }

    /// <summary>Changes a row's label (e.g. a "press again to confirm" prompt).</summary>
    public void SetLabel(int index, string label)
    {
        if (index < 0 || index >= rows.Count) return;
        rows[index].label = label;
        Refresh();
    }
}
