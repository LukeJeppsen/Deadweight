using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The CONTROLS card shown from the title screen's Options and from the pause menu.
/// Keep this table in step with Assets/InputSystem_Actions.inputactions. The pixel font has no / ( ) < + symbols.
/// </summary>
public static class ControlsCard
{
    private static readonly string[,] Rows =
    {
        { "MOVE, AIM",  "WASD OR ARROWS", "STICK OR D-PAD" },
        { "JUMP",       "SPACE",         "A" },
        { "SWING",      "CLICK OR ENTER", "X" },
        { "LAUNCH",     "LEFT SHIFT",    "RB OR L3" },
        { "PULSE",      "E",             "Y" },
        { "PAUSE",      "ESC OR P",       "START" },
    };

    /// <summary>Builds the card (full-screen dim + panel) under `parent`. Destroy the returned object to close it.</summary>
    public static GameObject Build(Transform parent, Texture2D font, Sprite panelSprite, Color text, Color accent, Color dim, Color shadow)
    {
        Image back = UIKit.MakeFullScreen(parent, "ControlsCard", dim);
        UIKit.SetAlpha(back, 0.7f);

        GameObject pgo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        pgo.transform.SetParent(back.transform, false);
        Image pimg = pgo.GetComponent<Image>();
        pimg.raycastTarget = false;
        if (panelSprite != null) { pimg.sprite = panelSprite; UIKit.PixelSlice(pimg); pimg.color = Color.white; }
        else pimg.color = new Color(0f, 0f, 0f, 0.9f);
        RectTransform panel = pimg.rectTransform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        // nearly the whole 320x180 screen: the 5x7 font is large at this scale, and the corner ornaments need room
        panel.sizeDelta = new Vector2(304f, 168f);
        panel.anchoredPosition = Vector2.zero;

        Vector2 tl = new Vector2(0f, 1f);
        Vector2 ct = new Vector2(0.5f, 1f);
        UIKit.MakeText(panel, "Head", font, "CONTROLS", 240, accent, true, ct, ct, new Vector2(0f, -11f), 1f, true, shadow);

        float[] colX = { 30f, 102f, 200f };
        string[] heads = { "", "KEYBOARD", "GAMEPAD" };
        for (int c = 1; c < 3; c++)
            UIKit.MakeText(panel, "Col" + c, font, heads[c], 96, accent, false, tl, tl, new Vector2(colX[c], -30f), 1f, true, shadow);

        for (int r = 0; r < Rows.GetLength(0); r++)
        {
            float y = -44f - r * 12f;
            for (int c = 0; c < 3; c++)
                UIKit.MakeText(panel, "R" + r + "C" + c, font, Rows[r, c], 96, c == 0 ? accent : text, false, tl, tl, new Vector2(colX[c], y), 1f, true, shadow);
        }

        UIKit.MakeText(panel, "Tip", font, "HOLD A DIRECTION AND SWING TO BE THROWN", 240, text, true,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 27f), 1f, true, shadow);
        UIKit.MakeText(panel, "Close", font, "PRESS ANY BUTTON", 200, accent, true,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), 1f, true, shadow);
        return back.gameObject;
    }

    /// <summary>True on the frame the player dismisses the card.</summary>
    public static bool CloseRequested()
    {
        return MenuInput.Back() || MenuInput.Confirm() || MenuInput.Clicked() || MenuInput.Pause();
    }
}
