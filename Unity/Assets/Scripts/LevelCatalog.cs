using UnityEngine;

/// <summary>Static description of one area / level. Scenes are named Level_&lt;id&gt;.</summary>
public class LevelInfo
{
    public string id;
    public string title;
    public string subtitle;
    public string blurb;
    public Skill reward;          // granted by this area's relic altar (None = no altar)
    public string[] palette;      // 6 hex colours, dark to light (used by the level-select swatches)

    public string SceneName { get { return "Level_" + id; } }
}

public static class LevelCatalog
{
    public static readonly LevelInfo[] All =
    {
        new LevelInfo
        {
            id = "cinder_foot", title = "THE CINDER FOOT", subtitle = "WHERE EVERY CLIMB BEGINS",
            blurb = "Ash-choked stairs at the Spire's root. Learn to swing.",
            reward = Skill.WallGrip,
            palette = new[] { "272744", "494d7e", "8b6d9c", "c69fa5", "f2d3ab", "fbf5ef" }
        },
        new LevelInfo
        {
            id = "drowned_cistern", title = "THE DROWNED CISTERN", subtitle = "SEALED BEHIND CRACKED STONE",
            blurb = "Flooded halls that remember every climber. Some doors are only stone.",
            reward = Skill.Breaker,
            palette = new[] { "0c1f2b", "1b4152", "2f6b73", "5ea39b", "b5dfc2", "eef6d8" }
        },
        new LevelInfo
        {
            id = "hollow_garden", title = "THE HOLLOW GARDEN", subtitle = "WHERE THE LAST TREES DIED STANDING",
            blurb = "A dead forest grips the Spire. The ground will not wait for you.",
            reward = Skill.TwinEdge,
            palette = new[] { "18211a", "2b4533", "55703f", "94a255", "d6cf88", "f4eec6" }
        },
        new LevelInfo
        {
            id = "pale_ledges", title = "THE PALE LEDGES", subtitle = "COLD LIGHT, NO WARMTH",
            blurb = "Frozen shelves in the wind. Stone appears when the Shard calls.",
            reward = Skill.ShardPulse,
            palette = new[] { "1c2238", "38497a", "5f7fb3", "9cbbdb", "d6e8ef", "f8fcff" }
        },
        new LevelInfo
        {
            id = "rust_foundry", title = "THE RUST FOUNDRY", subtitle = "THE FORGE THAT CAST THE BLADE",
            blurb = "The furnaces still burn. The blade rings off every wall.",
            reward = Skill.Ricochet,
            palette = new[] { "2a1416", "56262b", "94452f", "d4803f", "f2bf6f", "fff1c9" }
        },
        new LevelInfo
        {
            id = "zenith", title = "THE ZENITH", subtitle = "THE SUN IS WAITING",
            blurb = "The summit. Light that never moves. Pull the Deadweight.",
            reward = Skill.None,
            palette = new[] { "100c22", "2d2353", "7a3f78", "d9774f", "f6d36a", "fffbe6" }
        }
    };

    public static int Count { get { return All.Length; } }

    public static int IndexOfScene(string sceneName)
    {
        for (int i = 0; i < All.Length; i++) if (All[i].SceneName == sceneName) return i;
        return -1;
    }

    public static int IndexOfId(string id)
    {
        for (int i = 0; i < All.Length; i++) if (All[i].id == id) return i;
        return -1;
    }

    /// <summary>Skills granted by all levels before this one.</summary>
    public static Skill LoadoutBefore(int index)
    {
        Skill s = Skill.None;
        for (int i = 0; i < index && i < All.Length; i++) s |= All[i].reward;
        return s;
    }

    public static Color ParseHex(string hex)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString("#" + hex, out c)) return c;
        return Color.magenta;
    }
}
