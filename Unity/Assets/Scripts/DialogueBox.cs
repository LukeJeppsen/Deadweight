using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Input used by cutscenes and dialogue (works with the new Input System).</summary>
public static class CutsceneInput
{
    public static bool AdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame)) return true;
        Mouse m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame) return true;
        Gamepad g = Gamepad.current;
        if (g != null && g.buttonSouth.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
#endif
    }

    public static bool SkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame) return true;
        Gamepad g = Gamepad.current;
        return g != null && g.startButton.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}

/// <summary>A pixel-art dialogue box with portrait, speaker name, typewriter text and a "continue" arrow.</summary>
public class DialogueBox : MonoBehaviour
{
    [System.Serializable]
    public struct Line
    {
        public string speaker;
        public string text;
        public Sprite portrait;
        public Color nameColor;
    }

    public GameObject root;
    public Image portraitImage;
    public PixelText nameText;
    public PixelText bodyText;
    public PixelText arrow;
    public int bodyWidth = 242;
    public float secondsPerChar = 0.028f;
    public float punctuationPause = 0.16f;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        // the box art is 16 px per unit: draw its frame at true pixel size, not 6x too big
        if (root != null) UIKit.PixelSlice(root.GetComponent<Image>());
    }

    public void Hide() { if (root != null) root.SetActive(false); }

    /// <summary>Plays every line in order. `skip` lets the caller abort (return true to stop).</summary>
    public IEnumerator Play(Line[] lines, System.Func<bool> skip = null)
    {
        root.SetActive(true);
        foreach (Line line in lines)
        {
            if (skip != null && skip()) break;

            portraitImage.sprite = line.portrait;
            portraitImage.enabled = line.portrait != null;
            nameText.tint = line.nameColor.a > 0f ? line.nameColor : (Color)new Color32(0xF2, 0xD3, 0xAB, 0xFF);
            nameText.SetText(line.speaker, 120);
            arrow.SetText("`", 8);
            arrow.gameObject.SetActive(false);
            bodyText.SetText(line.text, bodyWidth, 0);

            int total = bodyText.GlyphCount, shown = 0;
            float timer = 0f;
            bool aborted = false;
            yield return null;                                 // don't let the key that opened this line also skip it
            while (shown < total)
            {
                if (skip != null && skip()) { aborted = true; break; }
                if (CutsceneInput.AdvancePressed()) { shown = total; break; }
                timer += Time.unscaledDeltaTime;
                float per = secondsPerChar;
                while (timer >= per && shown < total)
                {
                    timer -= per;
                    shown++;
                    if (shown % 2 == 1 && shown <= line.text.Length && line.text[shown - 1] != ' ') Sfx.Play("text", 0.8f, 0.08f);
                    char last = line.text.Length > 0 ? line.text[Mathf.Min(shown - 1, line.text.Length - 1)] : ' ';
                    if (last == '.' || last == ',' || last == '!' || last == '?') timer -= punctuationPause;
                }
                bodyText.Reveal(shown);
                yield return null;
            }
            if (aborted) break;
            bodyText.Reveal(total);

            // wait for the player to continue, with a blinking arrow
            arrow.gameObject.SetActive(true);
            yield return null;
            float t = 0f;
            while (true)
            {
                if (skip != null && skip()) { aborted = true; break; }
                if (CutsceneInput.AdvancePressed()) { Sfx.Play("menu_move", 0.8f, 0f); break; }
                t += Time.unscaledDeltaTime;
                arrow.gameObject.SetActive(Mathf.Repeat(t, 0.9f) < 0.6f);
                yield return null;
            }
            if (aborted) break;
        }
        root.SetActive(false);
    }
}
