using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Main menu logic: Play loads the game scene, Options opens the settings menu (sound, music, screen shake,
/// fullscreen, controls, reset progress), Quit exits (hidden in a browser build, where it can't do anything).
/// Built and wired up by Tools > Pixel Art > Build Main Menu Scene.
/// </summary>
public class TitleMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    [Header("Panels")]
    [SerializeField] private GameObject mainButtons;
    [SerializeField] private GameObject optionsPanel;

    [Header("Music")]
    [SerializeField] private AudioSource music;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
    [SerializeField] private float musicFadeIn = 2f;

    [Header("Options menu art")]
    [SerializeField] private Texture2D fontAtlas;
    [SerializeField] private Sprite panelSprite;

    [Header("Fade")]
    [SerializeField] private Image fade;
    [SerializeField] private float fadeTime = 0.5f;

    private bool busy;

    private static readonly Color Cream = new Color32(0xFB, 0xF5, 0xEF, 0xFF);
    private static readonly Color Sand = new Color32(0xF2, 0xD3, 0xAB, 0xFF);
    private static readonly Color Ink = new Color32(0x27, 0x27, 0x44, 0xFF);

    private RectTransform optionsRoot;
    private MenuList optionsMenu;
    private int resetRow = -1;
    private bool resetArmed;
    private int optionsOpenFrame = -1, optionsClosedFrame = -1;
    private GameObject controlsCard;
    private int controlsOpenFrame = -1;

    private void Awake()
    {
        foreach (Button b in new[] { playButton, optionsButton, quitButton, backButton })
            if (b != null) b.onClick.AddListener(() => Sfx.Play("menu_confirm", 1f, 0f));
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (backButton != null) backButton.onClick.AddListener(CloseOptions);

        HoverSelect(playButton);
        HoverSelect(optionsButton);
        HoverSelect(quitButton);
        HoverSelect(backButton);

        if (!GameSettings.CanQuit && quitButton != null)
        {
            quitButton.gameObject.SetActive(false);
            if (optionsButton != null)
            {
                Navigation n = optionsButton.navigation;
                n.selectOnDown = null;
                optionsButton.navigation = n;
            }
        }
        Sfx.MusicVolumeChanged += OnMusicVolumeChanged;
    }

    private void OnDestroy()
    {
        Sfx.MusicVolumeChanged -= OnMusicVolumeChanged;
    }

    private void OnMusicVolumeChanged()
    {
        if (music != null && !busy) music.volume = musicVolume * Sfx.MusicVolume;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainButtons != null) mainButtons.SetActive(true);
        Select(playButton);
        if (music != null)
        {
            music.loop = true;
            music.volume = 0f;
            music.Play();
            StartCoroutine(FadeMusic(0f, musicVolume * Sfx.MusicVolume, musicFadeIn));
        }
        if (fade != null)
        {
            SetFade(1f);   // the fade object is saved invisible; start black and fade in
            StartCoroutine(Fade(1f, 0f));
        }
    }

    private GameObject lastSelected;

    private void Update()
    {
        // a click of sound whenever the highlighted button changes (keyboard, pad or mouse)
        GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (sel != lastSelected)
        {
            if (lastSelected != null && sel != null && !busy) Sfx.Play("menu_move", 1f, 0f);
            lastSelected = sel;
        }
        if (optionsRoot != null && optionsRoot.gameObject.activeSelf) UpdateOptions();
    }

    public void PlayGame()
    {
        if (busy) return;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        busy = true;
        if (music != null) StartCoroutine(FadeMusic(music.volume, 0f, fadeTime));
        if (fade != null) yield return Fade(0f, 1f);
        string target = Application.CanStreamedLevelBeLoaded(GameConfig.LevelSelectScene) ? GameConfig.LevelSelectScene : gameSceneName;
        SceneManager.LoadScene(target);
    }

    public void OpenOptions()
    {
        if (busy || Time.frameCount == optionsClosedFrame) return;
        if (mainButtons != null) mainButtons.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);    // the old placeholder panel stays hidden
        if (optionsRoot == null) BuildOptions();
        optionsRoot.gameObject.SetActive(true);
        optionsMenu.Index = 0;
        DisarmReset();
        optionsMenu.Refresh();
        optionsOpenFrame = Time.frameCount;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        lastSelected = null;
    }

    public void CloseOptions()
    {
        optionsClosedFrame = Time.frameCount;     // the key that closed Options must not also press the Options button
        CloseControls();
        if (optionsRoot != null) optionsRoot.gameObject.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainButtons != null) mainButtons.SetActive(true);
        Select(optionsButton);
        lastSelected = optionsButton != null ? optionsButton.gameObject : null;
    }

    private void BuildOptions()
    {
        optionsRoot = UIKit.MakeRect(transform, "OptionsMenu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        optionsRoot.anchorMin = Vector2.zero; optionsRoot.anchorMax = Vector2.one;
        optionsRoot.offsetMin = Vector2.zero; optionsRoot.offsetMax = Vector2.zero;
        if (fade != null) optionsRoot.SetSiblingIndex(fade.transform.GetSiblingIndex());   // under the fade overlay

        GameObject pgo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        pgo.transform.SetParent(optionsRoot, false);
        Image pimg = pgo.GetComponent<Image>();
        pimg.raycastTarget = false;
        if (panelSprite != null) { pimg.sprite = panelSprite; UIKit.PixelSlice(pimg); }
        else pimg.color = new Color(Ink.r, Ink.g, Ink.b, 0.9f);
        RectTransform panel = pimg.rectTransform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(232f, 116f);
        panel.anchoredPosition = new Vector2(0f, -30f);

        UIKit.MakeText(panel, "Head", fontAtlas, "OPTIONS", 160, Sand, true, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), 1f, true, Ink);
        optionsMenu = new MenuList(panel, fontAtlas, Cream, Sand, Ink, -21f, 12f, 200);
        optionsMenu.AddSlider("SOUND", () => Sfx.SfxLevel, v => Sfx.SfxLevel = v, Sfx.VolumeSteps);
        optionsMenu.AddSlider("MUSIC", () => Sfx.MusicLevel, v => Sfx.MusicLevel = v, Sfx.VolumeSteps);
        optionsMenu.AddToggle("SCREEN SHAKE", () => GameSettings.ScreenShake, v => GameSettings.ScreenShake = v);
        optionsMenu.AddToggle("FULLSCREEN", () => GameSettings.Fullscreen, v => GameSettings.Fullscreen = v);
        optionsMenu.AddChoice("CONTROLS", OpenControls);
        resetRow = optionsMenu.Count;
        optionsMenu.AddChoice("RESET PROGRESS", ResetProgress);
        optionsMenu.AddChoice("BACK", CloseOptions);
    }

    private void UpdateOptions()
    {
        if (Time.frameCount == optionsOpenFrame) return;
        if (controlsCard != null)
        {
            if (Time.frameCount != controlsOpenFrame && ControlsCard.CloseRequested()) { CloseControls(); Sfx.Play("menu_back", 1f, 0f); }
            return;
        }
        if (MenuInput.Back()) { Sfx.Play("menu_back", 1f, 0f); CloseOptions(); return; }
        optionsMenu.Tick();
        if (resetArmed && optionsMenu.Index != resetRow) DisarmReset();
    }

    private void ResetProgress()
    {
        if (!resetArmed)
        {
            resetArmed = true;
            optionsMenu.SetLabel(resetRow, "SURE? PRESS AGAIN");
            return;
        }
        GameProgress.ResetAll();
        resetArmed = false;
        optionsMenu.SetLabel(resetRow, "PROGRESS ERASED");
        Sfx.Play("death", 0.5f, 0f);
    }

    private void DisarmReset()
    {
        resetArmed = false;
        if (optionsMenu != null && resetRow >= 0) optionsMenu.SetLabel(resetRow, "RESET PROGRESS");
    }

    private void OpenControls()
    {
        if (controlsCard != null) return;
        controlsCard = ControlsCard.Build(optionsRoot, fontAtlas, panelSprite, Cream, Sand, Ink, Ink);
        controlsOpenFrame = Time.frameCount;
    }

    private void CloseControls()
    {
        if (controlsCard != null) Destroy(controlsCard);
        controlsCard = null;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeMusic(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            music.volume = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        music.volume = to;
    }

    private IEnumerator Fade(float from, float to)
    {
        fade.gameObject.SetActive(true);
        fade.raycastTarget = to > 0f;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(from, to, t / fadeTime));
            yield return null;
        }
        SetFade(to);
        if (to <= 0f) { fade.raycastTarget = false; fade.gameObject.SetActive(false); }
    }

    private void SetFade(float a)
    {
        // Four steps instead of a smooth ramp keeps the retro feel.
        a = Mathf.Round(a * 4f) / 4f;
        Color c = fade.color;
        c.a = a;
        fade.color = c;
    }

    private static void Select(Button b)
    {
        if (b != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
    }

    // Hovering with the mouse moves the "selected" highlight so only one button ever looks lit.
    private static void HoverSelect(Button b)
    {
        if (b == null) return;
        EventTrigger trigger = b.GetComponent<EventTrigger>();
        if (trigger == null) trigger = b.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => { if (b.interactable) Select(b); });
        trigger.triggers.Add(entry);
    }
}
