using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Automated play-through check of every level, in real play mode:
/// dying on a hazard and in a pit, every checkpoint (and that backtracking never moves you back),
/// the relic altar, and the exit loading the next scene. Any error or exception logged during play fails the run.
///
///   Tools > Deadweight > Play-Test All Levels
///   Headless: Unity.exe -batchmode -projectPath ... -executeMethod DeadweightPlayTest.Run -logFile log.txt
///             (no -quit: the test exits Unity itself; exit code 0 = all passed)
/// Results: Builds/playtest_results.txt. Your saved progress is backed up first and restored afterwards.
/// </summary>
[InitializeOnLoad]
public static class DeadweightPlayTest
{
    const string KeyIndex = "dw_playtest_index";
    const string KeyFails = "dw_playtest_fails";
    const string KeySave = "dw_playtest_save_backup";
    const string SaveKey = "deadweight_save_v1";
    public const string ResultsPath = "Builds/playtest_results.txt";

    static DeadweightPlayTest()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static PlayTestRunner runner;

    static void TickRunner()
    {
        if (runner != null && EditorApplication.isPlaying) runner.Tick();
    }

    static bool Active { get { return SessionState.GetInt(KeyIndex, -1) >= 0; } }

    /// <summary>Headless: only the first level.</summary>
    public static void RunFirst()
    {
        SessionState.SetInt("dw_playtest_count", 1);
        Run();
    }

    [MenuItem("Tools/Deadweight/Play-Test All Levels")]
    public static void RunAll()
    {
        SessionState.SetInt("dw_playtest_count", LevelCatalog.Count);
        Run();
    }

    static void Run()
    {
        File.WriteAllText("Builds/playtest_save_backup.txt", PlayerPrefs.GetString(SaveKey, ""));   // survives a crash
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Builds");
        File.WriteAllText(ResultsPath, "Deadweight play-test " + System.DateTime.Now + "\n");
        SessionState.SetString(KeySave, PlayerPrefs.GetString(SaveKey, ""));
        SessionState.SetInt(KeyFails, 0);
        SessionState.SetInt(KeyIndex, 0);
        StartLevel(0);
    }

    static void StartLevel(int i)
    {
        // a clean save where this level is open and the intro has been seen
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene(LevelBuilder.ScenePath(i), OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange s)
    {
        if (!Active) return;
        int i = SessionState.GetInt(KeyIndex, 0);
        if (s == PlayModeStateChange.EnteredPlayMode)
        {
            GameProgress.CurrentLevelIndex = i;
            runner = new PlayTestRunner { levelIndex = i, Done = OnLevelDone };
            runner.Begin();
            EditorApplication.update -= TickRunner;
            EditorApplication.update += TickRunner;
        }
        else if (s == PlayModeStateChange.EnteredEditMode)
        {
            i++;
            if (i < SessionState.GetInt("dw_playtest_count", LevelCatalog.Count))
            {
                SessionState.SetInt(KeyIndex, i);
                StartLevel(i);
            }
            else Finish();
        }
    }

    static void OnLevelDone(string report, int fails)
    {
        runner = null;
        EditorApplication.update -= TickRunner;
        File.AppendAllText(ResultsPath, report);
        SessionState.SetInt(KeyFails, SessionState.GetInt(KeyFails, 0) + fails);
        EditorApplication.ExitPlaymode();
    }

    static void Finish()
    {
        int fails = SessionState.GetInt(KeyFails, 0);
        string backup = SessionState.GetString(KeySave, "");
        if (string.IsNullOrEmpty(backup)) PlayerPrefs.DeleteKey(SaveKey); else PlayerPrefs.SetString(SaveKey, backup);
        PlayerPrefs.Save();
        SessionState.EraseInt(KeyIndex);
        string summary = fails == 0 ? "ALL LEVELS PASSED" : fails + " CHECK(S) FAILED";
        File.AppendAllText(ResultsPath, "\n" + summary + "\n");
        Debug.Log("[PlayTest] " + summary + " - see " + ResultsPath);
        if (File.Exists("Builds/playtest_save_backup.txt")) File.Delete("Builds/playtest_save_backup.txt");
        if (Application.isBatchMode) EditorApplication.Exit(fails == 0 ? 0 : 1);
        else EditorUtility.DisplayDialog("Deadweight", summary + "\nDetails: " + ResultsPath, "OK");
    }
}
