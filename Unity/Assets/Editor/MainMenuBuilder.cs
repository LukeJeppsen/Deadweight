using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > Pixel Art > Build Main Menu Scene
/// Imports the menu art (Assets/Art/Menu), builds Assets/Scenes/MainMenu.unity and puts it first in Build Settings.
/// Safe to re-run: the scene is rebuilt from scratch each time.
/// </summary>
public static class MainMenuBuilder
{
    const string ArtDir = "Assets/Art/Menu";
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/Game.unity";

    static readonly Color Dark = new Color32(0x27, 0x27, 0x44, 0xFF);
    static readonly Color Light = new Color32(0xFB, 0xF5, 0xEF, 0xFF);
    static readonly Color Cream = new Color32(0xF2, 0xD3, 0xAB, 0xFF);
    static readonly Color Pink = new Color32(0xC6, 0x9F, 0xA5, 0xFF);

    [MenuItem("Tools/Pixel Art/Build Main Menu Scene")]
    public static void Build()
    {
        if (!Directory.Exists(ArtDir))
        {
            EditorUtility.DisplayDialog("Main Menu", "Couldn't find " + ArtDir + ".\nUnzip the menu art into Assets/Art/Menu first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ImportArt();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera (the canvas is an overlay, but a camera keeps the editor and URP happy)
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Dark;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();

        // Canvas
        GameObject canvasGo = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(320f, 180f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;   // height decides scale; wider screens just see more sky

        Transform root = canvasGo.transform;

        // ---- background layers (back to front)
        var layers = new List<MenuBackground.Layer>();
        RawImage sky = AddRaw(root, "Sky", "bg_sky");
        layers.Add(new MenuBackground.Layer { name = "Sky", raw = sky, scrollSpeed = 0f, parallax = 0.5f, swayAmplitude = 0f });
        RawImage starsA = AddRaw(root, "StarsA", "bg_stars_a");
        RawImage starsB = AddRaw(root, "StarsB", "bg_stars_b");
        layers.Add(new MenuBackground.Layer { name = "StarsA", raw = starsA, parallax = 0.8f, swayAmplitude = 0f });
        layers.Add(new MenuBackground.Layer { name = "StarsB", raw = starsB, parallax = 0.8f, swayAmplitude = 0f });
        layers.Add(new MenuBackground.Layer { name = "Clouds", raw = AddRaw(root, "Clouds", "bg_clouds"), scrollSpeed = 3f, parallax = 1.5f, swayAmplitude = 0f });
        layers.Add(new MenuBackground.Layer { name = "RidgeFar", raw = AddRaw(root, "RidgeFar", "bg_ridge_far"), parallax = 2f, swayAmplitude = 0.6f, swayPeriod = 11f, phase = 0.5f });
        layers.Add(new MenuBackground.Layer { name = "RidgeMid", raw = AddRaw(root, "RidgeMid", "bg_ridge_mid"), parallax = 3f, swayAmplitude = 0.8f, swayPeriod = 9f, phase = 1.7f });
        layers.Add(new MenuBackground.Layer { name = "Spires", raw = AddRaw(root, "Spires", "bg_spires"), parallax = 4f, swayAmplitude = 0.8f, swayPeriod = 8f, phase = 2.9f });
        layers.Add(new MenuBackground.Layer { name = "Mist", raw = AddRaw(root, "Mist", "bg_mist"), scrollSpeed = -2f, parallax = 5f, swayAmplitude = 0f });

        Image fg = AddImage(root, "Foreground", "bg_foreground");
        SetRect(fg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(336f, 180f));
        layers.Add(new MenuBackground.Layer { name = "Foreground", rect = fg.rectTransform, parallax = 6f, swayAmplitude = 1f, swayPeriod = 7f, phase = 0.9f });

        // ---- embers
        GameObject emberRoot = new GameObject("Embers", typeof(RectTransform));
        emberRoot.transform.SetParent(root, false);
        Stretch((RectTransform)emberRoot.transform);

        // ---- vignette
        Image vig = AddImage(root, "Vignette", "vignette");
        Stretch(vig.rectTransform);
        vig.raycastTarget = false;

        // ---- title
        Image title = AddImage(root, "Title", "title");
        Sprite titleSprite = title.sprite;
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -5f), titleSprite.rect.size);
        title.raycastTarget = false;

        // ---- main buttons
        GameObject mainButtons = new GameObject("MainButtons", typeof(RectTransform));
        mainButtons.transform.SetParent(root, false);
        Stretch((RectTransform)mainButtons.transform);
        Button play = AddButton(mainButtons.transform, "PlayButton", "play", -52f);
        Button options = AddButton(mainButtons.transform, "OptionsButton", "options", -79f);
        Button quit = AddButton(mainButtons.transform, "QuitButton", "quit", -106f);
        SetExplicitNav(play, null, options);
        SetExplicitNav(options, play, quit);
        SetExplicitNav(quit, options, null);

        // ---- options panel (placeholder)
        Image panel = AddImage(root, "OptionsPanel", "panel");
        panel.type = Image.Type.Sliced;
        SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(160f, 80f));
        Image lbl = AddImage(panel.transform, "OptionsLabel", "label_options");
        SetRect(lbl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), lbl.sprite.rect.size);
        lbl.raycastTarget = false;
        Image soon = AddImage(panel.transform, "ComingSoonLabel", "label_coming_soon");
        SetRect(soon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), soon.sprite.rect.size);
        soon.raycastTarget = false;
        Button back = AddButton(panel.transform, "BackButton", "back", 0f);
        RectTransform brt = (RectTransform)back.transform;
        SetRect(brt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(112f, 22f));
        panel.gameObject.SetActive(false);

        // ---- fade overlay
        GameObject fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeGo.transform.SetParent(root, false);
        Stretch((RectTransform)fadeGo.transform);
        Image fade = fadeGo.GetComponent<Image>();
        fade.color = new Color(Dark.r, Dark.g, Dark.b, 0f);   // invisible in the editor; the script fades in at runtime
        fade.raycastTarget = false;
        fadeGo.SetActive(false);

        // ---- scripts
        MenuBackground bg = canvasGo.AddComponent<MenuBackground>();
        bg.layers = layers.ToArray();
        bg.starsA = starsA;
        bg.starsB = starsB;
        bg.title = title.rectTransform;
        bg.titleBaseY = -5f;
        bg.emberRoot = (RectTransform)emberRoot.transform;
        bg.emberSprite = LoadSprite("ember");
        bg.emberColors = new[] { Cream, Light, Pink };

        TitleMenuController ctl = canvasGo.AddComponent<TitleMenuController>();
        SerializedObject so = new SerializedObject(ctl);
        so.FindProperty("gameSceneName").stringValue = "Game";
        so.FindProperty("playButton").objectReferenceValue = play;
        so.FindProperty("optionsButton").objectReferenceValue = options;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("mainButtons").objectReferenceValue = mainButtons;
        so.FindProperty("optionsPanel").objectReferenceValue = panel.gameObject;
        so.FindProperty("fade").objectReferenceValue = fade;
        so.FindProperty("fontAtlas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(LevelBuilder.FontPath);
        so.FindProperty("panelSprite").objectReferenceValue = LoadSprite("panel");
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- music (Assets/Audio/Beneath_the_Dead_Sun.mp3)
        const string MusicPath = "Assets/Audio/Beneath_the_Dead_Sun.mp3";
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        if (clip != null)
        {
            AudioImporter ai = AssetImporter.GetAtPath(MusicPath) as AudioImporter;
            if (ai != null)
            {
                AudioImporterSampleSettings ss = ai.defaultSampleSettings;
                ss.loadType = AudioClipLoadType.Streaming;
                ss.preloadAudioData = false;
                ai.defaultSampleSettings = ss;
                ai.SaveAndReimport();
            }
            GameObject musicGo = new GameObject("MenuMusic", typeof(AudioSource));
            musicGo.transform.SetParent(canvasGo.transform, false);
            AudioSource src = musicGo.GetComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 0.7f;
            so = new SerializedObject(ctl);
            so.FindProperty("music").objectReferenceValue = src;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else Debug.LogWarning("[MainMenu] No music found at " + MusicPath + " - the menu will be silent.");

        // ---- event system (new Input System)
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.GetComponent<EventSystem>().firstSelectedGameObject = play.gameObject;
        // Newer Input System versions assign the default UI actions themselves (in the editor and at runtime).
        // Calling AssignDefaultActions() here throws on some versions, so it's tried defensively only.
        InputSystemUIInputModule module = es.GetComponent<InputSystemUIInputModule>();
        if (module.actionsAsset == null)
        {
            try { module.AssignDefaultActions(); }
            catch (System.Exception) { /* the module falls back to its built-in defaults when it starts */ }
        }

        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MainMenu] Built " + ScenePath + " and made it scene 0 in Build Settings.");
        EditorUtility.DisplayDialog("Main Menu", "Built " + ScenePath + ".\nIt is now scene 0 in Build Settings, so the built game opens on it.\nPress Play to try it (Quit stops play mode in the editor).", "OK");
    }

    // ------------------------------------------------------------------ build settings

    static void SetBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        // (the old Game.unity sandbox is no longer part of the build)
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.path == ScenePath || s.path == GameScenePath) continue;
            list.Add(s);
        }
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ------------------------------------------------------------------ import

    static void ImportArt()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16f;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            ti.sRGBTexture = true;

            TextureImporterSettings s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(s);

            string name = Path.GetFileNameWithoutExtension(path);
            if (name == "panel") ti.spriteBorder = new Vector4(8, 8, 8, 8);
            if (name.StartsWith("btn_blank")) ti.spriteBorder = new Vector4(8, 8, 8, 8);

            ti.SaveAndReimport();
        }
    }

    static string Find(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Texture2D", new[] { ArtDir }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == name) return p;
        }
        Debug.LogError("[MainMenu] Missing menu art: " + name + ".png");
        return null;
    }

    static Sprite LoadSprite(string name)
    {
        string p = Find(name);
        return p == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(p);
    }

    static Texture2D LoadTexture(string name)
    {
        string p = Find(name);
        return p == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    // ------------------------------------------------------------------ ui helpers

    static RawImage AddRaw(Transform parent, string name, string texture)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        // full width (so wide screens keep tiling), exactly 180 reference pixels tall
        SetRect(rt, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 180f));
        RawImage raw = go.GetComponent<RawImage>();
        raw.texture = LoadTexture(texture);
        raw.raycastTarget = false;
        return raw;
    }

    static Image AddImage(Transform parent, string name, string sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = LoadSprite(sprite);
        img.raycastTarget = false;
        return img;
    }

    static Button AddButton(Transform parent, string name, string key, float y)
    {
        Image img = AddImage(parent, name, "btn_" + key + "_normal");
        img.raycastTarget = true;
        SetRect(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(112f, 22f));

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.SpriteSwap;
        SpriteState ss = new SpriteState
        {
            highlightedSprite = LoadSprite("btn_" + key + "_hover"),
            selectedSprite = LoadSprite("btn_" + key + "_hover"),
            pressedSprite = LoadSprite("btn_" + key + "_pressed"),
            disabledSprite = LoadSprite("btn_" + key + "_normal")
        };
        b.spriteState = ss;
        return b;
    }

    static void SetExplicitNav(Button b, Selectable up, Selectable down)
    {
        Navigation n = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };
        b.navigation = n;
    }

    static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
