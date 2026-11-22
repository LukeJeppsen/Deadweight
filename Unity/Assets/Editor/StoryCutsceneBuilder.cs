using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > Pixel Art > Add Intro Cutscene To Open Scene
/// Imports Assets/Art/Story and adds a "CutsceneCanvas" (letterbox, narration, dialogue box) plus the
/// IntroCutscene component to the open scene. Re-running replaces the old one.
/// </summary>
public static class StoryCutsceneBuilder
{
    const string ArtDir = "Assets/Art/Story";
    const string MenuArtDir = "Assets/Art/Menu";

    static readonly Color Dark = new Color32(0x27, 0x27, 0x44, 0xFF);
    static readonly Color Light = new Color32(0xFB, 0xF5, 0xEF, 0xFF);

    [MenuItem("Tools/Pixel Art/Add Intro Cutscene To Open Scene")]
    public static void Build()
    {
        if (!Directory.Exists(ArtDir))
        {
            EditorUtility.DisplayDialog("Intro Cutscene", "Couldn't find " + ArtDir + ".\nUnzip the story art into Assets/Art/Story first.", "OK");
            return;
        }
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Intro Cutscene", "Open your Game scene first (no PlayerController found in the open scene).", "OK");
            return;
        }

        IntroCutscene made = AddToScene(player);
        GameObject canvasGo = made.gameObject;
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        EditorSceneManager.SaveScene(canvasGo.scene);
        Debug.Log("[Cutscene] Added CutsceneCanvas to " + canvasGo.scene.name + ". Press Play to watch it (Esc skips).");
        EditorUtility.DisplayDialog("Intro Cutscene", "Added CutsceneCanvas to the scene and saved it.\nPress Play to watch the intro (Esc skips).\n\nTip: if the camera is confined or the fall looks wrong, adjust 'Drop Height' on the CutsceneCanvas > Intro Cutscene component.", "OK");
    }

    /// <summary>Adds the CutsceneCanvas (letterbox, narration, dialogue) + IntroCutscene to the open scene. No dialogs, no saving.</summary>
    public static IntroCutscene AddToScene(PlayerController player)
    {
        ImportArt();

        // remove a previous build
        GameObject old = GameObject.Find("CutsceneCanvas");
        if (old != null) Object.DestroyImmediate(old);

        GameObject canvasGo = new GameObject("CutsceneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(320f, 180f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        Transform root = canvasGo.transform;
        Texture2D font = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtDir + "/font_5x7.png");

        // letterbox bars
        RectTransform top = AddSolid(root, "BarTop", Dark, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), 14f);
        RectTransform bottom = AddSolid(root, "BarBottom", Dark, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), 14f);

        // narration (centred on screen)
        PixelText narration = AddPixelText(root, "Narration", font, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        narration.centered = true;
        narration.shadow = false;
        narration.tint = new Color32(0xF2, 0xD3, 0xAB, 0xFF);
        narration.lineHeight = 12;

        // dialogue box
        GameObject box = new GameObject("DialogueBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(root, false);
        Image panel = box.GetComponent<Image>();
        panel.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MenuArtDir + "/panel.png");
        UIKit.PixelSlice(panel);
        panel.raycastTarget = false;
        RectTransform prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 18f);
        prt.sizeDelta = new Vector2(304f, 52f);
        if (panel.sprite == null) panel.color = Dark;

        Image frame = AddImage(box.transform, "PortraitFrame", ArtDir + "/portrait_frame.png", new Vector2(8f, -8f), new Vector2(36f, 36f));
        Image portrait = AddImage(box.transform, "Portrait", ArtDir + "/portrait_wren.png", new Vector2(10f, -10f), new Vector2(32f, 32f));
        PixelText nameText = AddPixelText(box.transform, "Name", font, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(52f, -8f));
        PixelText body = AddPixelText(box.transform, "Body", font, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(52f, -20f));
        PixelText arrow = AddPixelText(box.transform, "Arrow", font, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(286f, -40f));
        arrow.tint = new Color32(0xF2, 0xD3, 0xAB, 0xFF);
        frame.raycastTarget = false;

        DialogueBox dlg = canvasGo.AddComponent<DialogueBox>();
        dlg.root = box;
        dlg.portraitImage = portrait;
        dlg.nameText = nameText;
        dlg.bodyText = body;
        dlg.arrow = arrow;
        box.SetActive(false);

        // flash + fade (full screen)
        Image flash = AddFullScreen(root, "Flash", new Color32(0xFB, 0xF5, 0xEF, 0x00));
        Image fade = AddFullScreen(root, "Fade", Dark);
        fade.transform.SetAsFirstSibling();   // behind the narration, bars and dialogue

        IntroCutscene cut = canvasGo.AddComponent<IntroCutscene>();
        cut.player = player;
        cut.animDriver = player.GetComponent<PlayerAnimationDriver>();
        cut.body = player.GetComponent<Rigidbody2D>();
        cut.fade = fade;
        cut.flash = flash;
        cut.barTop = top;
        cut.barBottom = bottom;
        cut.narration = narration;
        cut.dialogue = dlg;
        cut.wrenPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/portrait_wren.png");
        cut.marrowPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/portrait_marrow.png");

        if (cut.animDriver == null)
            Debug.LogWarning("[Cutscene] No PlayerAnimationDriver on the player - run Tools > Pixel Art > Import Everything first for the fall/land animations.");

        return cut;
    }

    static void ImportArt()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            bool isFont = Path.GetFileNameWithoutExtension(path) == "font_5x7";
            ti.textureType = isFont ? TextureImporterType.Default : TextureImporterType.Sprite;
            if (!isFont)
            {
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 16f;
            }
            ti.isReadable = isFont;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.SaveAndReimport();
        }
        // the dialogue box reuses the menu panel; make sure it is imported as a 9-slice sprite
        string panelPath = MenuArtDir + "/panel.png";
        TextureImporter pi = AssetImporter.GetAtPath(panelPath) as TextureImporter;
        if (pi != null && pi.spriteBorder == Vector4.zero)
        {
            pi.textureType = TextureImporterType.Sprite;
            pi.spriteBorder = new Vector4(8, 8, 8, 8);
            pi.filterMode = FilterMode.Point;
            pi.textureCompression = TextureImporterCompression.Uncompressed;
            pi.SaveAndReimport();
        }
    }

    static RectTransform AddSolid(Transform parent, string name, Color color, Vector2 aMin, Vector2 aMax, Vector2 pivot, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
        return rt;
    }

    static Image AddFullScreen(Transform parent, string name, Color color)
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

    static Image AddImage(Transform parent, string name, string spritePath, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    static PixelText AddPixelText(Transform parent, string name, Texture2D font, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RawImage raw = go.GetComponent<RawImage>();
        raw.raycastTarget = false;
        RectTransform rt = raw.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(8f, 8f);
        PixelText pt = go.AddComponent<PixelText>();
        pt.atlas = font;
        pt.tint = Light;
        return pt;
    }
}
