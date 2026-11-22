using System;
using UnityEngine;

/// <summary>
/// Which skills the player has right now. A LevelDirector sets this when a level starts; a scene with no
/// director (like the old sandbox Game scene) keeps everything unlocked so it still plays as before.
/// </summary>
public class PlayerAbilities : MonoBehaviour
{
    public Skill skills = Skill.All;

    public event Action<Skill> SkillGained;

    public bool Has(Skill s)
    {
        return s != Skill.None && (skills & s) == s;
    }

    public void SetSkills(Skill s)
    {
        skills = s;
    }

    /// <summary>Gives a skill and saves it.</summary>
    public void Grant(Skill s)
    {
        skills |= s;
        GameProgress.GrantSkill(s);
        if (SkillGained != null) SkillGained(s);
    }
}
