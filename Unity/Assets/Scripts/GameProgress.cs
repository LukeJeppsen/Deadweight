using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Project-wide switches.</summary>
public static class GameConfig
{
    /// <summary>
    /// While the game is in development every level can be picked from the level-select screen.
    /// Set to FALSE for the real game: a level then only unlocks once the one before it has been beaten.
    /// (Skills always follow the progression either way - see GameProgress.SkillsForLevel.)
    /// </summary>
    public static bool UnlockAllLevels = false;

    public const string TitleScene = "MainMenu";
    public const string LevelSelectScene = "LevelSelect";
    public const string EndingScene = "Ending";
}

/// <summary>
/// Save data + progression rules. Stored in PlayerPrefs as JSON.
/// Rules:
///  - level 0 is always open; level N opens when level N-1 is completed (unless GameConfig.UnlockAllLevels).
///  - a level starts with the skills already earned PLUS every skill granted by the levels before it, so jumping
///    straight to a later level (while developing) plays exactly as it would after a normal run.
/// </summary>
public static class GameProgress
{
    [Serializable]
    public class LevelStat
    {
        public string id;
        public int deaths;
        public float bestTime;
    }

    [Serializable]
    private class SaveData
    {
        public int skills;
        public bool introSeen;
        public string lastLevel;
        public List<string> completed = new List<string>();
        public List<LevelStat> stats = new List<LevelStat>();
    }

    private const string Key = "deadweight_save_v1";
    private static SaveData data;

    /// <summary>The level index chosen on the level-select screen (read by the level scene).</summary>
    public static int CurrentLevelIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { data = null; }

    private static SaveData Data
    {
        get
        {
            if (data != null) return data;
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception) { data = null; }
            if (data == null) data = new SaveData();
            if (data.completed == null) data.completed = new List<string>();
            if (data.stats == null) data.stats = new List<LevelStat>();
            return data;
        }
    }

    public static void Save()
    {
        try
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
        catch (Exception e) { Debug.LogWarning("[Progress] Could not save: " + e.Message); }
    }

    public static void ResetAll()
    {
        data = new SaveData();
        Save();
    }

    // ------------------------------------------------------------------ skills

    /// <summary>Skills saved from altars the player has already touched.</summary>
    public static Skill SavedSkills { get { return (Skill)Data.skills; } }

    public static void GrantSkill(Skill s)
    {
        Data.skills |= (int)s;
        Save();
    }

    /// <summary>
    /// Everything the player should have when entering this level: exactly the skills from the areas before it.
    /// (Not every skill ever earned - replaying an early area after finishing the game must play the same way.)
    /// </summary>
    public static Skill SkillsForLevel(int index)
    {
        return LevelCatalog.LoadoutBefore(index);
    }

    // ------------------------------------------------------------------ levels

    public static bool IsCompleted(string id) { return Data.completed.Contains(id); }

    public static bool IsUnlocked(int index)
    {
        if (index < 0 || index >= LevelCatalog.Count) return false;
        if (GameConfig.UnlockAllLevels || index == 0) return true;
        return IsCompleted(LevelCatalog.All[index - 1].id);
    }

    public static int HighestUnlocked()
    {
        int best = 0;
        for (int i = 0; i < LevelCatalog.Count; i++) if (IsUnlocked(i)) best = i;
        return best;
    }

    /// <summary>First level that is open but not yet beaten (where "continue" should land).</summary>
    public static int NextToPlay()
    {
        for (int i = 0; i < LevelCatalog.Count; i++)
            if (IsUnlocked(i) && !IsCompleted(LevelCatalog.All[i].id)) return i;
        return LevelCatalog.Count - 1;
    }

    public static void MarkCompleted(string id, int deaths, float seconds)
    {
        if (!Data.completed.Contains(id)) Data.completed.Add(id);
        LevelStat st = GetStat(id, true);
        if (st.bestTime <= 0f || seconds < st.bestTime) st.bestTime = seconds;
        if (st.deaths <= 0 || deaths < st.deaths) st.deaths = deaths;
        Data.lastLevel = id;
        Save();
    }

    public static LevelStat GetStat(string id, bool create)
    {
        foreach (LevelStat s in Data.stats) if (s.id == id) return s;
        if (!create) return null;
        LevelStat n = new LevelStat { id = id };
        Data.stats.Add(n);
        return n;
    }

    public static bool IntroSeen
    {
        get { return Data.introSeen; }
        set { Data.introSeen = value; Save(); }
    }

    public static int CompletedCount { get { return Data.completed.Count; } }
}
