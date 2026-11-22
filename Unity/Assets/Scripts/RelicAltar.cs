using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>A fallen climber's altar. Touch the relic to gain its skill.</summary>
public class RelicAltar : MonoBehaviour
{
    public Skill skill;
    public SpriteRenderer orb;
    public SpriteRenderer halo;
    public Sprite[] orbFrames;
    public float orbFps = 6f;

    private bool collected;
    private bool busy;

    private IEnumerator Start()
    {
        // Wait a frame: the player prefab starts with every skill and the LevelDirector trims that to this level's
        // loadout in its own Start. Checking before then made the altar think its skill was already collected.
        yield return null;
        PlayerAbilities ab = FindFirstObjectByType<PlayerAbilities>();
        collected = ab != null && ab.Has(skill);
        RefreshVisuals();
    }

    private void Update()
    {
        if (collected || orb == null || orbFrames == null || orbFrames.Length == 0) return;
        orb.sprite = orbFrames[(int)(Time.time * orbFps) % orbFrames.Length];
        if (halo != null)
        {
            Color c = halo.color;
            c.a = 0.55f + 0.2f * Mathf.Sin(Time.time * 3f);
            halo.color = c;
        }
    }

    private void RefreshVisuals()
    {
        if (orb != null) orb.enabled = !collected;
        if (halo != null) halo.enabled = !collected;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || busy) return;
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p == null) return;
        StartCoroutine(Collect(p));
    }

    private IEnumerator Collect(PlayerController p)
    {
        busy = true;
        LevelAssets a = LevelAssets.Instance;
        Vector2 at = orb != null ? (Vector2)orb.transform.position : (Vector2)transform.position;
        if (a != null)
        {
            RingFx.Spawn(a.halo, at, 0.5f, 10f, 0.6f, a.Pal(5));
            SimpleParticle.Burst(at, 26, 2f, 9f, 6f, 0.9f, 5, 4, 3);
        }
        CameraTarget.Shake(0.3f);
        Sfx.Play("relic");
        collected = true;
        RefreshVisuals();

        if (LevelDirector.Instance != null) yield return LevelDirector.Instance.GrantSkillSequence(skill, p);
        else if (p.Abilities != null) p.Abilities.Grant(skill);
        busy = false;
    }
}
