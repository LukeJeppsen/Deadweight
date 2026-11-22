using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The level-select screen: the Spire seen from the outside, one node per area from the root up to the summit.
/// Up/down (or the mouse) picks an area, Space/Enter plays it, Esc goes back to the title.
/// Locked areas follow GameProgress.IsUnlocked, so flipping GameConfig.UnlockAllLevels to false turns on real progression.
/// Everything is built from code at runtime; the scene only needs this component with the sprites assigned.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [Header("Art")]
    public Texture2D fontAtlas;
    public Sprite background;
    public Sprite panel;
    public Sprite nodeLocked, nodeOpen, nodeDone;
    public Sprite[] cursor;
    public Sprite glyphLock, glyphCheck, glyphUp, glyphDown;
    [Tooltip("Wall Grip, Breaker, Twin Edge, Shard Pulse, Ricochet")]
    public Sprite[] skillIcons;

    [Header("Layout (pixels in a 320x180 screen, y down)")]
    public Vector2[] nodePositions =
    {
        new Vector2(110f, 155f), new Vector2(138f, 130f), new Vector2(110f, 105f),
        new Vector2(138f, 80f), new Vector2(110f, 55f), new Vector2(138f, 30f)
    };

    private static readonly Color Cream = new Color32(0xFB, 0xF5, 0xEF, 0xFF);
    private static readonly Color Sand = new Color32(0xF2, 0xD3, 0xAB, 0xFF);
    private static readonly Color Rose = new Color32(0xC6, 0x9F, 0xA5, 0xFF);
    private static readonly Color Ink = new Color32(0x27, 0x27, 0x44, 0xFF);

    private Canvas canvas;
    private Image fade;
    private Image[] nodes;
    private Image[] nodeGlyphs;
    private Image cursorImg;
    private RectTransform panelRect;
    private PixelText areaText, titleText, subText, blurbText, skillLabel, skillName, statusText, hintText, devText, messageText;
    private Image rewardIcon;
    private Image[] swatches;
    private Image[] ladder;
    private int selected;
    private bool leaving;
    private float messageUntil;
    private float shakeUntil;

    private void Start()
    {
        Time.timeScale = 1f;
        BuildUi();

        int start = GameProgress.CurrentLevelIndex;
        if (start < 0 || start >= LevelCatalog.Count || !GameProgress.IsUnlocked(start)) start = GameProgress.NextToPlay();
        selected = Mathf.Clamp(start, 0, LevelCatalog.Count - 1);
        Refresh();
        StartCoroutine(FadeTo(0f, 0.5f));
    }

    // ------------------------------------------------------------------ build

    private PixelText Label(Transform parent, string text, int width, bool centered, Vector2 anchor, Vector2 pivot, Vector2 pos, Color tint)
    {
        return UIKit.MakeText(parent, "Text", fontAtlas, text, width, tint, centered, anchor, pivot, pos, 1f, true, Ink);
    }

    private void BuildUi()
    {
        canvas = UIKit.MakeCanvas("LevelSelectCanvas", transform, 0);
        Vector2 tl = new Vector2(0f, 1f);
        Vector2 mid = new Vector2(0.5f, 0.5f);

        UIKit.MakeImage(canvas.transform, "Background", background, Color.white, mid, mid, Vector2.zero, new Vector2(320f, 180f));

        int n = LevelCatalog.Count;
        nodes = new Image[n];
        nodeGlyphs = new Image[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 p = nodePositions[Mathf.Min(i, nodePositions.Length - 1)];
            Vector2 pos = new Vector2(p.x - 160f, 90f - p.y);
            nodes[i] = UIKit.MakeImage(canvas.transform, "Node" + i, nodeOpen, Color.white, mid, mid, pos, new Vector2(20f, 20f));
            nodeGlyphs[i] = UIKit.MakeImage(canvas.transform, "NodeGlyph" + i, null, Color.white, mid, mid, pos, new Vector2(8f, 8f));
        }
        cursorImg = UIKit.MakeImage(canvas.transform, "Cursor", cursor != null && cursor.Length > 0 ? cursor[0] : null, Color.white, mid, mid, Vector2.zero, new Vector2(28f, 28f));

        // info panel on the right
        GameObject pg = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image));
        pg.transform.SetParent(canvas.transform, false);
        Image pimg = pg.GetComponent<Image>();
        pimg.raycastTarget = false;
        if (panel != null) { pimg.sprite = panel; UIKit.PixelSlice(pimg); }
        else pimg.color = new Color(0f, 0f, 0f, 0.8f);
        panelRect = pimg.rectTransform;
        panelRect.anchorMin = tl; panelRect.anchorMax = tl; panelRect.pivot = tl;
        panelRect.anchoredPosition = new Vector2(190f, -8f);
        panelRect.sizeDelta = new Vector2(124f, 164f);

        areaText = Label(panelRect, "", 108, false, tl, tl, new Vector2(8f, -8f), Rose);
        titleText = Label(panelRect, "", 108, false, tl, tl, new Vector2(8f, -19f), Sand);
        subText = Label(panelRect, "", 108, false, tl, tl, new Vector2(8f, -40f), Rose);
        blurbText = Label(panelRect, "", 108, false, tl, tl, new Vector2(8f, -68f), Cream);

        skillLabel = Label(panelRect, "NEW SKILL", 108, false, tl, tl, new Vector2(8f, -108f), Rose);
        rewardIcon = UIKit.MakeImage(panelRect, "RewardIcon", null, Color.white, tl, tl, new Vector2(8f, -118f), new Vector2(16f, 16f));
        skillName = Label(panelRect, "", 84, false, tl, tl, new Vector2(28f, -122f), Sand);

        statusText = Label(panelRect, "", 108, false, tl, tl, new Vector2(8f, -138f), Cream);

        swatches = new Image[6];
        for (int i = 0; i < 6; i++)
            swatches[i] = UIKit.MakeImage(panelRect, "Sw" + i, null, Color.white, tl, tl, new Vector2(8f + i * 9f, -150f), new Vector2(8f, 6f));

        ladder = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            Sprite icon = skillIcons != null && i < skillIcons.Length ? skillIcons[i] : null;
            ladder[i] = UIKit.MakeImage(canvas.transform, "Ladder" + i, icon, Color.white, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f - (4 - i) * 18f, 8f), new Vector2(16f, 16f));
        }
        // the ladder sits under the panel's bottom edge; shrink the panel a touch to make room
        panelRect.sizeDelta = new Vector2(124f, 148f);
        for (int i = 0; i < 6; i++)
            ((RectTransform)swatches[i].transform).anchoredPosition = new Vector2(8f + i * 9f, -134f);
        ((RectTransform)statusText.transform).anchoredPosition = new Vector2(8f, -122f);
        ((RectTransform)skillLabel.transform).anchoredPosition = new Vector2(8f, -94f);
        ((RectTransform)rewardIcon.transform).anchoredPosition = new Vector2(8f, -104f);
        ((RectTransform)skillName.transform).anchoredPosition = new Vector2(28f, -108f);

        hintText = Label(canvas.transform, "ARROWS PICK  SPACE PLAY  ESC BACK", 200, false, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(6f, 6f), Sand);
        devText = Label(canvas.transform, "", 120, false, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(6f, 17f), Rose);
        messageText = Label(canvas.transform, "", 150, true, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(124f, 28f), Cream);
        UIKit.SetAlpha(messageText, 0f);
        if (GameConfig.UnlockAllLevels) devText.SetText("DEV: ALL AREAS UNLOCKED", 120);

        Canvas fc = UIKit.MakeCanvas("FadeCanvas", transform, 100);
        fade = UIKit.MakeFullScreen(fc.transform, "Fade", Ink);
        UIKit.SetAlpha(fade, 1f);
    }

    // ------------------------------------------------------------------ state

    private void Refresh()
    {
        int n = LevelCatalog.Count;
        for (int i = 0; i < n; i++)
        {
            bool unlocked = GameProgress.IsUnlocked(i);
            bool done = GameProgress.IsCompleted(LevelCatalog.All[i].id);
            nodes[i].sprite = done ? nodeDone : (unlocked ? nodeOpen : nodeLocked);
            Sprite g = !unlocked ? glyphLock : (done ? glyphCheck : null);
            nodeGlyphs[i].sprite = g;
            nodeGlyphs[i].enabled = g != null;
        }

        LevelInfo info = LevelCatalog.All[selected];
        bool open = GameProgress.IsUnlocked(selected);
        bool cleared = GameProgress.IsCompleted(info.id);
        Color accent = LevelCatalog.ParseHex(info.palette[4]);
        Color dim = LevelCatalog.ParseHex(info.palette[3]);

        areaText.tint = dim; areaText.SetText("AREA " + (selected + 1) + " OF " + n, 108);
        titleText.tint = accent; titleText.SetText(info.title, 108);
        subText.tint = dim; subText.SetText(info.subtitle, 108);
        blurbText.SetText(info.blurb, 108);

        if (info.reward != Skill.None)
        {
            skillLabel.SetText("NEW SKILL", 108);
            rewardIcon.enabled = true;
            int ic = SkillInfo.IconIndex(info.reward);
            rewardIcon.sprite = skillIcons != null && ic >= 0 && ic < skillIcons.Length ? skillIcons[ic] : null;
            skillName.tint = accent; skillName.SetText(open ? SkillInfo.Name(info.reward) : "???", 84);
        }
        else
        {
            skillLabel.SetText("THE SUMMIT", 108);
            rewardIcon.enabled = false;
            skillName.tint = accent; skillName.SetText("PULL THE DEADWEIGHT", 100);
        }

        string status;
        if (!open) status = "LOCKED  CLEAR " + LevelCatalog.All[Mathf.Max(0, selected - 1)].title.Replace("THE ", "");
        else if (cleared)
        {
            GameProgress.LevelStat st = GameProgress.GetStat(info.id, false);
            status = "CLEARED";
            if (st != null && st.bestTime > 0f)
            {
                int m = Mathf.FloorToInt(st.bestTime / 60f);
                status += "  " + m + ":" + (st.bestTime - m * 60f).ToString("00.0") + "  DEATHS " + st.deaths;
            }
        }
        else status = "READY";
        statusText.tint = open ? (cleared ? accent : Cream) : Rose;
        statusText.SetText(status, 108);

        for (int i = 0; i < 6; i++)
            swatches[i].color = LevelCatalog.ParseHex(info.palette[Mathf.Min(i, info.palette.Length - 1)]);

        Skill owned = GameProgress.SkillsForLevel(selected);
        for (int i = 0; i < 5; i++)
        {
            bool has = (owned & SkillInfo.Order[i]) == SkillInfo.Order[i];
            Color c = Color.white;
            c.a = has ? 1f : 0.25f;
            ladder[i].color = c;
        }
    }

    private void SetMessage(string text, float seconds)
    {
        messageText.SetText(text, 150);
        UIKit.SetAlpha(messageText, 1f);
        messageUntil = Time.unscaledTime + seconds;
    }

    // ------------------------------------------------------------------ update

    private void Update()
    {
        if (leaving) return;

        // cursor + selection pulse
        Vector2 p = nodePositions[Mathf.Min(selected, nodePositions.Length - 1)];
        Vector2 pos = new Vector2(p.x - 160f, 90f - p.y);
        if (Time.unscaledTime < shakeUntil) pos.x += Mathf.Round(Mathf.Sin(Time.unscaledTime * 60f) * 2f);
        cursorImg.rectTransform.anchoredPosition = pos;
        if (cursor != null && cursor.Length > 0) cursorImg.sprite = cursor[(int)(Time.unscaledTime * 3f) % cursor.Length];

        if (messageUntil > 0f && Time.unscaledTime > messageUntil) { UIKit.SetAlpha(messageText, 0f); messageUntil = 0f; }

        int old = selected;
        int n = LevelCatalog.Count;
        if (MenuInput.Up() || MenuInput.Right()) selected = Mathf.Min(n - 1, selected + 1);
        if (MenuInput.Down() || MenuInput.Left()) selected = Mathf.Max(0, selected - 1);
        if (MenuInput.PointerMoved())
        {
            for (int i = 0; i < n; i++) if (MenuInput.PointerOver(nodes[i].rectTransform)) selected = i;
        }
        if (old != selected) { Refresh(); Sfx.Play("menu_move", 1f, 0f); }

        if (MenuInput.Back()) { Sfx.Play("menu_back", 1f, 0f); StartCoroutine(Leave(GameConfig.TitleScene, -1)); return; }

        bool click = MenuInput.Clicked() && MenuInput.PointerOver(nodes[selected].rectTransform);
        if (MenuInput.Confirm() || click)
        {
            if (GameProgress.IsUnlocked(selected)) { Sfx.Play("menu_confirm", 1f, 0f); StartCoroutine(Leave(LevelCatalog.All[selected].SceneName, selected)); }
            else
            {
                Sfx.Play("menu_error", 1f, 0f);
                shakeUntil = Time.unscaledTime + 0.25f;
                SetMessage("CLEAR THE AREA BELOW FIRST", 1.8f);
            }
        }
    }

    private IEnumerator Leave(string scene, int levelIndex)
    {
        leaving = true;
        if (levelIndex >= 0) GameProgress.CurrentLevelIndex = levelIndex;
        yield return FadeTo(1f, 0.4f);
        if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
        else
        {
            Debug.LogWarning("[LevelSelect] Scene '" + scene + "' is not in Build Settings. Run Tools > Deadweight > Build Everything.");
            leaving = false;
            SetMessage("SCENE MISSING - BUILD EVERYTHING", 2.5f);
            yield return FadeTo(0f, 0.3f);
        }
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = fade.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            UIKit.SetAlpha(fade, UIKit.Step(Mathf.Lerp(start, target, Mathf.Clamp01(t / duration)), 8));
            yield return null;
        }
        UIKit.SetAlpha(fade, target);
    }
}
