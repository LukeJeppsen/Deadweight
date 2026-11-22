using System;

/// <summary>
/// The skills Wren can earn on the climb. Each area's relic altar grants exactly one, and the rest of that
/// area (and the areas after it) is built around it. Flags, so a save is just one integer.
/// </summary>
[Flags]
public enum Skill
{
    None = 0,
    WallGrip = 1,
    Breaker = 2,
    TwinEdge = 4,
    ShardPulse = 8,
    Ricochet = 16,
    All = 31
}

public static class SkillInfo
{
    /// <summary>Order the skills are earned in (also the order of the HUD icons).</summary>
    public static readonly Skill[] Order = { Skill.WallGrip, Skill.Breaker, Skill.TwinEdge, Skill.ShardPulse, Skill.Ricochet };

    public static int IconIndex(Skill s)
    {
        for (int i = 0; i < Order.Length; i++) if (Order[i] == s) return i;
        return -1;
    }

    public static string Name(Skill s)
    {
        switch (s)
        {
            case Skill.WallGrip: return "WALL GRIP";
            case Skill.Breaker: return "BREAKER";
            case Skill.TwinEdge: return "TWIN EDGE";
            case Skill.ShardPulse: return "SHARD PULSE";
            case Skill.Ricochet: return "RICOCHET";
            default: return "NOTHING YET";
        }
    }

    public static string Description(Skill s)
    {
        switch (s)
        {
            case Skill.WallGrip: return "Press toward a wall in the air to cling to it. Jump to leap away.";
            case Skill.Breaker: return "Your swings shatter cracked stone.";
            case Skill.TwinEdge: return "Swing twice before you touch the ground.";
            case Skill.ShardPulse: return "Pulse the Shard. Ghost stone turns solid for a few seconds.";
            case Skill.Ricochet: return "Swing into a surface to bounce off it, with the blade ready again.";
            default: return "";
        }
    }

    /// <summary>The controls hint shown with the pickup.</summary>
    public static string Hint(Skill s)
    {
        switch (s)
        {
            case Skill.WallGrip: return "HOLD TOWARD THE WALL, THEN JUMP";
            case Skill.Breaker: return "SWING AT CRACKED STONE";
            case Skill.TwinEdge: return "SWING, THEN SWING AGAIN";
            case Skill.ShardPulse: return "PRESS E OR Y TO PULSE";
            case Skill.Ricochet: return "SWING INTO A WALL OR THE FLOOR";
            default: return "";
        }
    }

    /// <summary>A line from the climber who left the relic behind.</summary>
    public static string Lore(Skill s)
    {
        switch (s)
        {
            case Skill.WallGrip: return "ANSELM THE MASON CUT THE FIRST STAIR. WHEN IT ENDED, HE CLUNG TO THE WALL AND KEPT GOING.";
            case Skill.Breaker: return "ORRA SOUNDED THE DEEP. SHE LEARNED THE CISTERN KEEPS ITS DOORS SHUT WITH STONE, NOT LOCKS.";
            case Skill.TwinEdge: return "HALE AND HOLLOW SHARED ONE BLADE. WHAT ONE SWUNG, THE OTHER ANSWERED.";
            case Skill.ShardPulse: return "MIRA CARRIED A LANTERN INTO THE COLD. THE STONE ANSWERED HER LIGHT.";
            case Skill.Ricochet: return "VAREK FORGED THE DEADWEIGHT. HE NEVER SAID WHY IT RINGS BACK WHEN YOU STRIKE.";
            default: return "";
        }
    }
}
