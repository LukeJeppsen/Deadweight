using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The ending: the Deadweight comes free and the sun rises. Built from code; the scene only needs this
/// component with the font atlas assigned. Space speeds lines up, Esc skips to the credits card.
/// </summary>
public class EndingSequence : MonoBehaviour
{
    public Texture2D fontAtlas;

    private static readonly Color Night = new Color32(0x10, 0x0C, 0x22, 0xFF);
    private static readonly Color Dusk = new Color32(0x7A, 0x3F, 0x78, 0xFF);
    private static readonly Color Dawn = new Color32(0xD9, 0x77, 0x4F, 0xFF);
    private static readonly Color Gold = new Color32(0xF6, 0xD3, 0x6A, 0xFF);
    private static readonly Color Cream = new Color32(0xFF, 0xFB, 0xE6, 0xFF);

    private Canvas canvas;
    private Image sky;
    private Image sun;
    private Image sunCore;
    private Image flash;
    private PixelText line;
    private PixelText credits;
    private bool skip;

    private void Start()
    {
        Time.timeScale = 1f;
        canvas = UIKit.MakeCanvas("EndingCanvas", transform, 0);
        sky = UIKit.MakeFullScreen(canvas.transform, "Sky", Night);

        Sprite disc = MakeDisc(24, false);
        Sprite core = MakeDisc(16, true);
        Vector2 mid = new Vector2(0.5f, 0.5f);
        sun = UIKit.MakeImage(canvas.transform, "Sun", disc, Color.white, mid, mid, new Vector2(0f, -140f), new Vector2(96f, 96f));
        sunCore = UIKit.MakeImage(canvas.transform, "SunCore", core, Color.white, mid, mid, new Vector2(0f, -140f), new Vector2(64f, 64f));
        sun.color = Gold; sunCore.color = Cream;

        line = UIKit.MakeText(canvas.transform, "Line", fontAtlas, "", 260, Cream, true, mid, mid, new Vector2(0f, 30f), 1f, true, Night);
        credits = UIKit.MakeText(canvas.transform, "Credits", fontAtlas, "", 260, Cream, true, mid, mid, new Vector2(0f, 0f), 1f, true, Night);
        flash = UIKit.MakeFullScreen(canvas.transform, "Flash", Cream);
        UIKit.SetAlpha(flash, 0f);
        UIKit.SetAlpha(line, 0f);
        UIKit.SetAlpha(credits, 0f);

        StartCoroutine(Run());
    }

    private void Update()
    {
        if (MenuInput.Back()) skip = true;
    }

    private IEnumerator Run()
    {
        yield return Wait(1.0f);
        yield return Say("THE BLADE CAME FREE\nWITHOUT A SOUND.", 3.2f);
        yield return Say("FOR A MOMENT,\nNOTHING MOVED.", 2.8f);

        // the sun comes up behind the text
        yield return Wait(0.4f);
        StartCoroutine(Rise());
        yield return Say("THEN THE LIGHT CAME BACK.", 3.2f);
        yield return Say("MARROW WAS RIGHT.\nTHE SUN COULD NOT GET UP ON ITS OWN.", 4.2f);
        yield return Say("A HUNDRED YEARS OF NIGHT\nENDED WITH ONE PULL.", 3.6f);

        yield return Credits();
        string next = GameConfig.TitleScene;
        if (Application.CanStreamedLevelBeLoaded(next)) SceneManager.LoadScene(next);
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds && !skip) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private IEnumerator Say(string text, float hold)
    {
        if (skip) yield break;
        line.SetText(text, 260);
        float t = 0f;
        while (t < 0.6f && !skip) { t += Time.unscaledDeltaTime; UIKit.SetAlpha(line, UIKit.Step(t / 0.6f, 5)); yield return null; }
        UIKit.SetAlpha(line, 1f);
        float h = 0f;
        while (h < hold && !skip && !MenuInput.Confirm()) { h += Time.unscaledDeltaTime; yield return null; }
        t = 0f;
        while (t < 0.5f && !skip) { t += Time.unscaledDeltaTime; UIKit.SetAlpha(line, 1f - UIKit.Step(t / 0.5f, 5)); yield return null; }
        UIKit.SetAlpha(line, 0f);
    }

    private IEnumerator Rise()
    {
        float t = 0f;
        float dur = 14f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = k * k * (3f - 2f * k);
            Color c = e < 0.5f ? Color.Lerp(Night, Dusk, e * 2f) : Color.Lerp(Dusk, Dawn, (e - 0.5f) * 2f);
            sky.color = new Color(Mathf.Round(c.r * 16f) / 16f, Mathf.Round(c.g * 16f) / 16f, Mathf.Round(c.b * 16f) / 16f, 1f);
            float y = Mathf.Lerp(-140f, -10f, e);
            sun.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Round(y));
            sunCore.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Round(y));
            if (t < 0.5f) UIKit.SetAlpha(flash, 0.7f * (1f - t / 0.5f));
            else UIKit.SetAlpha(flash, 0f);
            yield return null;
        }
    }

    private IEnumerator Credits()
    {
        int deaths = 0;
        float time = 0f;
        for (int i = 0; i < LevelCatalog.Count; i++)
        {
            GameProgress.LevelStat st = GameProgress.GetStat(LevelCatalog.All[i].id, false);
            if (st == null) continue;
            deaths += st.deaths;
            time += st.bestTime;
        }
        int mins = Mathf.FloorToInt(time / 60f);
        string stats = "BEST TIMES " + mins + ":" + (time - mins * 60f).ToString("00") + "   DEATHS " + deaths;
        credits.SetText("DEADWEIGHT\n\nTHANK YOU FOR CLIMBING\n\n" + stats + "\n\nPRESS SPACE", 260);

        float t = 0f;
        while (t < 0.8f) { t += Time.unscaledDeltaTime; UIKit.SetAlpha(credits, UIKit.Step(t / 0.8f, 5)); yield return null; }
        skip = false;
        yield return null;
        while (!MenuInput.Confirm() && !MenuInput.Clicked()) yield return null;
        t = 0f;
        while (t < 0.6f) { t += Time.unscaledDeltaTime; UIKit.SetAlpha(credits, 1f - UIKit.Step(t / 0.6f, 5)); sky.color = Color.Lerp(sky.color, Night, 0.1f); yield return null; }
    }

    private static Sprite MakeDisc(int size, bool solidOnly)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                bool inside = dx * dx + dy * dy <= r * r;
                px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
    }
}
