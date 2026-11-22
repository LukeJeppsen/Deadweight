using UnityEngine;

/// <summary>
/// Player options that aren't volume (volume lives in Sfx): screen shake and fullscreen.
/// Saved in PlayerPrefs, so they carry between sessions (in the browser too).
/// </summary>
public static class GameSettings
{
    private const string ShakeKey = "deadweight_screen_shake";

    public static bool ScreenShake
    {
        get { return PlayerPrefs.GetInt(ShakeKey, 1) == 1; }
        set { PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>
    /// Fullscreen on / off. In a browser the switch happens on the next click or key press
    /// (browsers only allow fullscreen from a user action), which a menu confirm always is.
    /// </summary>
    public static bool Fullscreen
    {
        get { return Screen.fullScreen; }
        set
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Screen.fullScreen = value;
#else
            if (value)
            {
                Resolution r = Screen.currentResolution;
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            }
#endif
        }
    }

    /// <summary>Quit only means something in a desktop build; in a browser the button is hidden.</summary>
    public static bool CanQuit
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
    }
}
