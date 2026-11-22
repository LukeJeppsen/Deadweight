using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// All the on-screen interface in a level, built from code at runtime: skill icons, death count, the area title
/// banner, toasts, the relic pickup card, the level-complete card, the pause menu and the screen fade.
/// Everything uses unscaled time so it keeps working while the game is paused.
/// </summary>
public class LevelHud : MonoBehaviour
{
    public const int PauseResume = 0, PauseRestart = 1, PauseSelect = 2, PauseTitle = 3;

    public Action<int> PauseChosen;

    private LevelAssets art;
    private Canvas canvas;
    private Image fade;
    private RectTransform skillRow;
    private PixelText deathsText;
    private PixelText titleText, subtitleText;
    private PixelText toastText;
    private float toastUntil;
    private RectTransform pausePanel;
    private Color textColor, accentColor, shadowColor;
    private bool built;
    private int pauseOpenFrame = -1;

    public bool PauseOpen { get; private set; }

    public void Build(LevelAssets assets)
    {
        if (built) return;
        built = true;
        art = assets;
        textColor = art != null ? art.Pal(5) : Color.white;
        accentColor = art != null ? art.Pal(4) : Color.yellow;
        shadowColor = art != null ? art.Pal(0) : Color.black;

        canvas = UIKit.MakeCanvas("HudCanvas", transform, 50);

        // top-left: skill icons
        skillRow = UIKit.MakeRect(canvas.transform, "Skills", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -4f), new Vector2(100f, 16f));

        // top-right: deaths
        deathsText = Label(canvas.transform, "Deaths", "", 90, false, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -4f), textColor);

        // title banner
        titleText = Label(canvas.transform, "Title", "", 240, true, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), accentColor);
        subtitleText = Label(canvas.transform, "Subtitle", "", 240, true, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), textColor);
        UIKit.SetAlpha(titleText, 0f);
        UIKit.SetAlpha(subtitleText, 0f);

        // toast
        toastText = Label(canvas.transform, "Toast", "", 260, true, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), textColor);
        UIKit.SetAlpha(toastText, 0f);

        BuildPause();

        // fade on top of everything
        Canvas fc = UIKit.MakeCanvas("FadeCanvas", transform, 100);
        fade = UIKit.MakeFullScreen(fc.transform, "Fade", art != null ? art.Pal(0) : Color.black);
        UIKit.SetAlpha(fade, 1f);
    }

    private PixelText Label(Transform parent, string name, string text, int width, bool centered, Vector2 anchor, Vector2 pivot, Vector2 pos, Color tint)
    {
        Texture2D font = art != null ? art.fontAtlas : null;
        return UIKit.MakeText(parent, name, font, text, width, tint, centered, anchor, pivot, pos, 1f, true, shadowColor);
    }

    // ------------------------------------------------------------------ fade

    public void SetFade(float a) { if (fade != null) UIKit.SetAlpha(fade, UIKit.Step(a, 8)); }
    public float FadeAlpha { get { return fade != null ? fade.color.a : 0f; } }

    public IEnumerator FadeTo(float target, float duration)
    {
        float start = FadeAlpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(start, target, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetFade(target);
    }

    // ------------------------------------------------------------------ corner widgets

    public void SetDeaths(int n)
    {
        if (deathsText == null) return;
        deathsText.gameObject.SetActive(n >= 0);
        if (n >= 0) deathsText.SetText("DEATHS " + n, 90);
    }

    public void RefreshSkills(Skill owned)
    {
        if (skillRow == null) return;
        for (int i = skillRow.childCount - 1; i >= 0; i--) Destroy(skillRow.GetChild(i).gameObject);
        int slot = 0;
        for (int i = 0; i < SkillInfo.Order.Length; i++)
        {
            Skill s = SkillInfo.Order[i];
            if ((owned & s) != s) continue;
            Sprite icon = art != null ? art.SkillIcon(s) : null;
            if (icon == null) continue;
            UIKit.MakeImage(skillRow, "Icon" + i, icon, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(slot * 18f, 0f), new Vector2(16f, 16f));
            slot++;
        }
    }

    public void Toast(string text, float seconds)
    {
        if (toastText == null) return;
        toastText.SetText(text, 260);
        UIKit.SetAlpha(toastText, 1f);
        toastUntil = Time.unscaledTime + seconds;
    }

    private void Update()
    {
        if (toastText != null && toastUntil > 0f)
        {
            float left = toastUntil - Time.unscaledTime;
            if (left <= 0f) { UIKit.SetAlpha(toastText, 0f); toastUntil = 0f; }
            else if (left < 0.4f) UIKit.SetAlpha(toastText, UIKit.Step(left / 0.4f, 4));
        }
        if (PauseOpen) UpdatePause();
    }

    // ------------------------------------------------------------------ title banner

    public IEnumerator ShowTitle(string title, string subtitle)
    {
        if (titleText == null) yield break;
        titleText.SetText(title, 240);
        subtitleText.SetText(subtitle, 240);
        float t = 0f;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; float a = UIKit.Step(t / 0.5f, 5); UIKit.SetAlpha(titleText, a); UIKit.SetAlpha(subtitleText, a * 0.9f); yield return null; }
        yield return new WaitForSecondsRealtime(2.2f);
        t = 0f;
        while (t < 0.6f) { t += Time.unscaledDeltaTime; float a = 1f - UIKit.Step(t / 0.6f, 5); UIKit.SetAlpha(titleText, a); UIKit.SetAlpha(subtitleText, a * 0.9f); yield return null; }
        UIKit.SetAlpha(titleText, 0f);
        UIKit.SetAlpha(subtitleText, 0f);
    }

    // ------------------------------------------------------------------ cards

    private RectTransform MakePanel(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        if (art != null && art.panel != null) { img.sprite = art.panel; UIKit.PixelSlice(img); img.color = Color.white; }
        else img.color = new Color(0f, 0f, 0f, 0.85f);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = rt.anchorMin; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        return rt;
    }

    private Image MakeDim(string name, float alpha)
    {
        Image dim = UIKit.MakeFullScreen(canvas.transform, name, art != null ? art.Pal(0) : Color.black);
        UIKit.SetAlpha(dim, alpha);
        return dim;
    }

    /// <summary>Relic pickup card. Returns when the player confirms.</summary>
    public IEnumerator SkillCard(Skill skill)
    {
        Image dim = MakeDim("SkillDim", 0f);
        RectTransform panel = MakePanel(dim.transform, "SkillPanel", new Vector2(236f, 128f), Vector2.zero);

        UIKit.MakeImage(panel, "Icon", art != null ? art.SkillIcon(skill) : null, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(16f, 16f));
        Label(panel, "Kind", "SKILL GAINED", 200, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), textColor);
        Label(panel, "Name", SkillInfo.Name(skill), 200, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -43f), accentColor);
        Label(panel, "Desc", SkillInfo.Description(skill), 208, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), textColor);
        Label(panel, "Lore", SkillInfo.Lore(skill), 208, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), art != null ? art.Pal(3) : textColor);
        PixelText hint = Label(panel, "Hint", SkillInfo.Hint(skill), 220, true, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), accentColor);
        PixelText cont = Label(panel, "Cont", "PRESS SPACE", 200, true, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), textColor);

        float t = 0f;
        while (t < 0.35f) { t += Time.unscaledDeltaTime; UIKit.SetAlpha(dim, 0.55f * UIKit.Step(t / 0.35f, 5)); panel.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(t / 0.35f)); yield return null; }
        panel.localScale = Vector3.one;

        float wait = 0f;
        while (true)
        {
            wait += Time.unscaledDeltaTime;
            UIKit.SetAlpha(cont, (Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0) ? 1f : 0.35f);
            if (wait > 0.6f && (MenuInput.Confirm() || MenuInput.Clicked())) break;
            yield return null;
        }

        t = 0f;
        while (t < 0.2f) { t += Time.unscaledDeltaTime; UIKit.SetAlpha(dim, 0.55f * (1f - UIKit.Step(t / 0.2f, 4))); panel.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, t / 0.2f); yield return null; }
        Destroy(dim.gameObject);
    }

    /// <summary>Level-complete card. Returns when the player confirms.</summary>
    public IEnumerator CompleteCard(LevelInfo info, int deaths, float seconds, bool isBest, bool last)
    {
        Image dim = MakeDim("DoneDim", 0f);
        RectTransform panel = MakePanel(dim.transform, "DonePanel", new Vector2(200f, 96f), Vector2.zero);
        Label(panel, "Head", last ? "THE SUMMIT" : "AREA CLEARED", 180, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), accentColor);
        Label(panel, "Name", info.title, 180, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), textColor);
        int mins = Mathf.FloorToInt(seconds / 60f);
        float secs = seconds - mins * 60f;
        string time = "TIME " + mins + ":" + secs.ToString("00.0");
        if (isBest) time += "  BEST";
        Label(panel, "Time", time, 180, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), textColor);
        Label(panel, "Deaths", deaths < 0 ? "NO DEATHS HERE" : "DEATHS " + deaths, 180, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), textColor);
        PixelText cont = Label(panel, "Cont", "PRESS SPACE", 180, true, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), accentColor);

        float t = 0f;
        while (t < 0.35f) { t += Time.unscaledDeltaTime; UIKit.SetAlpha(dim, 0.55f * UIKit.Step(t / 0.35f, 5)); panel.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(t / 0.35f)); yield return null; }
        panel.localScale = Vector3.one;

        float wait = 0f;
        while (true)
        {
            wait += Time.unscaledDeltaTime;
            UIKit.SetAlpha(cont, (Mathf.FloorToInt(Time.unscaledTime * 2f) % 2 == 0) ? 1f : 0.35f);
            if (wait > 0.6f && (MenuInput.Confirm() || MenuInput.Clicked())) break;
            yield return null;
        }
        Destroy(dim.gameObject);
    }

    // ------------------------------------------------------------------ pause menu

    private MenuList pauseMenu;
    private GameObject controlsCard;
    private int controlsOpenFrame = -1;

    private void BuildPause()
    {
        Image dim = MakeDim("PauseDim", 0.6f);
        pausePanel = (RectTransform)dim.transform;
        RectTransform panel = MakePanel(pausePanel, "PausePanel", new Vector2(220f, 142f), new Vector2(0f, -2f));
        Label(panel, "Head", "PAUSED", 140, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -9f), accentColor);

        Texture2D font = art != null ? art.fontAtlas : null;
        pauseMenu = new MenuList(panel, font, textColor, accentColor, shadowColor, -23f, 12f, 190);
        pauseMenu.AddChoice("RESUME", () => Choose(PauseResume));
        pauseMenu.AddSlider("SOUND", () => Sfx.SfxLevel, v => Sfx.SfxLevel = v, Sfx.VolumeSteps);
        pauseMenu.AddSlider("MUSIC", () => Sfx.MusicLevel, v => Sfx.MusicLevel = v, Sfx.VolumeSteps);
        pauseMenu.AddToggle("SCREEN SHAKE", () => GameSettings.ScreenShake, v => GameSettings.ScreenShake = v);
        pauseMenu.AddToggle("FULLSCREEN", () => GameSettings.Fullscreen, v => GameSettings.Fullscreen = v);
        pauseMenu.AddChoice("CONTROLS", OpenControls);
        pauseMenu.AddChoice("RESTART LEVEL", () => Choose(PauseRestart));
        pauseMenu.AddChoice("LEVEL SELECT", () => Choose(PauseSelect));
        pauseMenu.AddChoice("TITLE SCREEN", () => Choose(PauseTitle));
        pausePanel.gameObject.SetActive(false);
    }

    public void OpenPause()
    {
        if (PauseOpen) return;
        PauseOpen = true;
        pauseOpenFrame = Time.frameCount;
        pauseMenu.Index = 0;
        pausePanel.gameObject.SetActive(true);
        pauseMenu.Refresh();
    }

    public void ClosePause()
    {
        PauseOpen = false;
        CloseControls();
        if (pausePanel != null) pausePanel.gameObject.SetActive(false);
    }

    private void OpenControls()
    {
        if (controlsCard != null) return;
        controlsCard = ControlsCard.Build(canvas.transform, art != null ? art.fontAtlas : null, art != null ? art.panel : null,
            textColor, accentColor, art != null ? art.Pal(0) : Color.black, shadowColor);
        controlsOpenFrame = Time.frameCount;
    }

    private void CloseControls()
    {
        if (controlsCard != null) Destroy(controlsCard);
        controlsCard = null;
    }

    private void UpdatePause()
    {
        if (Time.frameCount == pauseOpenFrame) return;
        if (controlsCard != null)
        {
            if (Time.frameCount != controlsOpenFrame && ControlsCard.CloseRequested()) { CloseControls(); Sfx.Play("menu_back", 1f, 0f); }
            return;
        }
        if (MenuInput.Back() || MenuInput.Pause()) { Choose(PauseResume); return; }
        pauseMenu.Tick();
    }

    private void Choose(int choice)
    {
        if (PauseChosen != null) PauseChosen(choice);
    }
}
