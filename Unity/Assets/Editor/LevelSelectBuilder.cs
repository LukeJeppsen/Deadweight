using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds Assets/Scenes/LevelSelect.unity and Assets/Scenes/Ending.unity.</summary>
public static class LevelSelectBuilder
{
    public const string SelectScenePath = "Assets/Scenes/LevelSelect.unity";
    public const string EndingScenePath = "Assets/Scenes/Ending.unity";

    static Sprite Ui(string name) { return AreaArt.SingleSprite(AreaArt.UiDir + "/" + name + ".png"); }

    static void AddCamera(Color bg)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        camGo.AddComponent<AudioListener>();
    }

    public static void BuildSelect()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SelectScenePath));
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCamera(new Color32(0x27, 0x27, 0x44, 0xFF));

        GameObject go = new GameObject("LevelSelect");
        LevelSelectController c = go.AddComponent<LevelSelectController>();
        c.fontAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(LevelBuilder.FontPath);
        c.background = Ui("levelselect_bg");
        c.panel = AreaArt.SingleSprite(LevelBuilder.PanelPath);
        c.nodeLocked = Ui("node_locked");
        c.nodeOpen = Ui("node_open");
        c.nodeDone = Ui("node_done");
        c.cursor = new[] { Ui("cursor_0"), Ui("cursor_1") };
        c.glyphLock = Ui("glyph_lock");
        c.glyphCheck = Ui("glyph_check");
        c.glyphUp = Ui("glyph_up");
        c.glyphDown = Ui("glyph_down");
        c.skillIcons = new[] { Ui("icon_wallgrip"), Ui("icon_breaker"), Ui("icon_twinedge"), Ui("icon_pulse"), Ui("icon_ricochet") };

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SelectScenePath);
        Debug.Log("[Deadweight] Built " + SelectScenePath);
    }

    public static void BuildEnding()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(EndingScenePath));
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddCamera(Color.black);

        GameObject go = new GameObject("EndingSequence");
        EndingSequence e = go.AddComponent<EndingSequence>();
        e.fontAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(LevelBuilder.FontPath);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, EndingScenePath);
        Debug.Log("[Deadweight] Built " + EndingScenePath);
    }
}
