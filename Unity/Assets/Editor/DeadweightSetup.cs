using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Deadweight: one click builds the whole progression: area art, the Player prefab, every level scene,
/// the level-select and ending scenes, and the Build Settings scene list.
/// </summary>
public static class DeadweightSetup
{
    [MenuItem("Tools/Deadweight/Build Everything")]
    public static void BuildEverything()
    {
        if (!Directory.Exists(AreaArt.AreasDir) || !Directory.Exists(AreaArt.UiDir))
        {
            EditorUtility.DisplayDialog("Deadweight", "Missing " + AreaArt.AreasDir + " or " + AreaArt.UiDir + ". Copy the area art into the project first.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Deadweight", "This builds the area art, the Player prefab and all level scenes. The current scene will be closed (you will be asked to save changes first).\n\nTip: check in or shelve in Plastic SCM before running.", "Build", "Cancel")) return;

        try
        {
            EditorUtility.DisplayProgressBar("Deadweight", "Importing area art...", 0.05f);
            AreaArt.ImportAll();
            AssetDatabase.Refresh();

            EditorUtility.DisplayProgressBar("Deadweight", "Extracting the Player prefab...", 0.2f);
            if (!PlayerPrefabTool.EnsurePrefab()) return;
            AssetDatabase.Refresh();

            for (int i = 0; i < LevelCatalog.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Deadweight", "Building " + LevelCatalog.All[i].title + "...", 0.25f + 0.1f * i);
                LevelBuilder.BuildLevel(i);
            }

            EditorUtility.DisplayProgressBar("Deadweight", "Building level select + ending...", 0.9f);
            LevelSelectBuilder.BuildSelect();
            LevelSelectBuilder.BuildEnding();

            SetBuildSettings();
            AssetDatabase.SaveAssets();
        }
        finally { EditorUtility.ClearProgressBar(); }

        EditorUtility.DisplayDialog("Deadweight", "Done. Open the MainMenu scene and press Play: PLAY now goes to the level-select screen.\n\nGameConfig.UnlockAllLevels (in GameProgress.cs) is ON for development - set it to false for the real game.", "OK");
    }

    [MenuItem("Tools/Deadweight/Rebuild Level Scenes Only")]
    public static void RebuildLevels()
    {
        if (!PlayerPrefabTool.EnsurePrefab()) return;
        for (int i = 0; i < LevelCatalog.Count; i++) LevelBuilder.BuildLevel(i);
        SetBuildSettings();
    }

    [MenuItem("Tools/Deadweight/Reset Progress")]
    public static void ResetProgress()
    {
        GameProgress.ResetAll();
        Debug.Log("[Deadweight] Progress reset.");
    }

    static string FindScene(string name)
    {
        string[] g = AssetDatabase.FindAssets(name + " t:Scene");
        for (int i = 0; i < g.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(g[i]);
            if (Path.GetFileNameWithoutExtension(p) == name) return p;
        }
        return null;
    }

    public static void SetBuildSettings()
    {
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
        string[] fixedFirst = { "MainMenu" };
        for (int i = 0; i < fixedFirst.Length; i++) { string p = FindScene(fixedFirst[i]); if (p != null) list.Add(new EditorBuildSettingsScene(p, true)); }
        list.Add(new EditorBuildSettingsScene(LevelSelectBuilder.SelectScenePath, true));
        for (int i = 0; i < LevelCatalog.Count; i++)
        {
            string p = LevelBuilder.ScenePath(i);
            if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
        }
        list.Add(new EditorBuildSettingsScene(LevelSelectBuilder.EndingScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[Deadweight] Build Settings updated (" + list.Count + " scenes).");
    }
}
