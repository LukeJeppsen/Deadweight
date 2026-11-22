using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Keyboard / gamepad / mouse reads for menus (new Input System, no EventSystem needed).</summary>
public static class MenuInput
{
    public static bool Up()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; Gamepad g = Gamepad.current;
        return (k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && g.dpad.up.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
    }

    public static bool Down()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; Gamepad g = Gamepad.current;
        return (k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && g.dpad.down.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#endif
    }

    public static bool Left()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; Gamepad g = Gamepad.current;
        return (k != null && (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)) || (g != null && g.dpad.left.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
#endif
    }

    public static bool Right()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; Gamepad g = Gamepad.current;
        return (k != null && (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)) || (g != null && g.dpad.right.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
    }

    public static bool Confirm()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; Gamepad g = Gamepad.current;
        return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame))
            || (g != null && g.buttonSouth.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
    }

    public static bool Back()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current; Gamepad g = Gamepad.current;
        return (k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame))
            || (g != null && (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    /// <summary>Dedicated pause key: P (Esc also works, but a browser in fullscreen keeps Esc for itself).</summary>
    public static bool Pause()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && k.pKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.P);
#endif
    }

    public static bool Clicked()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse m = Mouse.current;
        return m != null && m.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    public static Vector2 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse m = Mouse.current;
        return m != null ? m.position.ReadValue() : new Vector2(-9999f, -9999f);
#else
        return Input.mousePosition;
#endif
    }

    public static bool PointerMoved()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse m = Mouse.current;
        return m != null && m.delta.ReadValue().sqrMagnitude > 0.5f;
#else
        return Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f;
#endif
    }

    /// <summary>True when the mouse pointer is over this rect (overlay canvas).</summary>
    public static bool PointerOver(RectTransform rt)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rt, PointerPosition(), null);
    }
}

/// <summary>Small helpers for building pixel-art UI from code.</summary>
public static class UIKit
{
    public static Canvas MakeCanvas(string name, Transform parent, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (parent != null) go.transform.SetParent(parent, false);
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortingOrder;
        CanvasScaler s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(320f, 180f);
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 1f;
        GraphicRaycaster gr = go.GetComponent<GraphicRaycaster>();
        if (gr != null) gr.enabled = false;      // menus here use their own pointer tests
        return c;
    }

    public static RectTransform MakeRect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    public static Image MakeImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return img;
    }

    public static Image MakeFullScreen(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return img;
    }

    public static PixelText MakeText(Transform parent, string name, Texture2D font, string text, int width, Color tint, bool centered,
                                     Vector2 anchor, Vector2 pivot, Vector2 pos, float scale = 1f, bool shadow = true, Color? shadowColor = null, int lineHeight = 9)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RawImage raw = go.GetComponent<RawImage>();
        raw.raycastTarget = false;
        RectTransform rt = raw.rectTransform;
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.localScale = new Vector3(scale, scale, 1f);
        PixelText pt = go.AddComponent<PixelText>();
        pt.atlas = font;
        pt.tint = tint;
        pt.centered = centered;
        pt.shadow = shadow;
        if (shadowColor.HasValue) pt.shadowColor = shadowColor.Value;
        pt.lineHeight = lineHeight;
        pt.SetText(text, width);
        return pt;
    }

    /// <summary>Draws a 9-sliced pixel-art panel at 1 sprite pixel = 1 UI unit (the canvas assumes 100 px per unit, the art is 16).</summary>
    public static void PixelSlice(Image img)
    {
        if (img == null || img.sprite == null) return;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 100f / img.sprite.pixelsPerUnit;
    }

    public static void SetAlpha(PixelText t, float a)
    {
        if (t == null) return;
        RawImage r = t.GetComponent<RawImage>();
        if (r != null) { Color c = r.color; c.a = a; r.color = c; }
    }

    public static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color; c.a = a; g.color = c;
    }

    public static float Step(float a, int levels)
    {
        return Mathf.Round(Mathf.Clamp01(a) * levels) / levels;
    }
}
