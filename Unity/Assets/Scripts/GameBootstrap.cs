using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs once at startup (no scene setup needed): turns on vsync so motion doesn't tear or judder,
/// and hides the mouse cursor everywhere except the main menu.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool menu = scene.name == "MainMenu";
        Cursor.visible = menu;
        Cursor.lockState = CursorLockMode.None;
    }
}
