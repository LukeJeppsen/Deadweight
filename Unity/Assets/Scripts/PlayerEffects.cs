using UnityEngine;

/// <summary>
/// Juice for the player: an afterimage trail while launching, sparks on a ricochet, dust on a wall jump,
/// a ring when the Shard pulses, and the burst when you die. Auto-added by the LevelDirector.
/// </summary>
public class PlayerEffects : MonoBehaviour
{
    private PlayerMovement movement;
    private SpriteRenderer[] renderers;
    private float trailTimer;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnEnable()
    {
        if (movement != null)
        {
            movement.Ricocheted += OnRicochet;
            movement.WallJumped += OnWallJump;
        }
        ShardPulse.Triggered += OnPulse;
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.Ricocheted -= OnRicochet;
            movement.WallJumped -= OnWallJump;
        }
        ShardPulse.Triggered -= OnPulse;
    }

    private void Update()
    {
        if (movement == null || !movement.IsDashing) { trailTimer = 0f; return; }
        trailTimer -= Time.deltaTime;
        if (trailTimer > 0f) return;
        trailTimer = 0.03f;

        LevelAssets a = LevelAssets.Instance;
        Color tint = a != null ? a.Pal(4) : new Color(0.95f, 0.83f, 0.67f);
        tint.a = 0.55f;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer r = renderers[i];
            if (r == null || !r.enabled || r.sprite == null || !r.gameObject.activeInHierarchy) continue;
            if (r.gameObject.name != "PlayerBody" && r.gameObject.name != "SwordLayer") continue;
            GameObject g = new GameObject("Afterimage");
            g.transform.position = r.transform.position;
            g.transform.rotation = r.transform.rotation;
            g.transform.localScale = r.transform.lossyScale;
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = r.sprite;
            sr.flipX = r.flipX;
            sr.flipY = r.flipY;
            sr.color = tint;
            sr.sortingOrder = -2;
            FadeAndDie f = g.AddComponent<FadeAndDie>();
            f.life = 0.22f;
        }
    }

    private void OnRicochet(Vector2 normal)
    {
        Sfx.Play("ricochet");
        Vector2 p = (Vector2)transform.position - normal * 0.45f;
        SimpleParticle.Burst(p, 9, 3f, 8f, 10f, 0.4f, 5, 4, 3);
    }

    private void OnWallJump(float awayDir)
    {
        Vector2 p = (Vector2)transform.position - new Vector2(awayDir * 0.4f, -0.2f);
        SimpleParticle.Burst(p, 5, 1f, 3f, 6f, 0.3f, 3, 4);
    }

    private void OnPulse()
    {
        Sfx.Play("pulse");
        LevelAssets a = LevelAssets.Instance;
        if (a == null) return;
        Color c = a.Pal(4);
        c.a = 0.8f;
        RingFx.Spawn(a.halo, transform.position, 0.6f, 12f, 0.5f, c);
    }

    /// <summary>The burst when the player dies.</summary>
    public void DeathBurst()
    {
        Sfx.Play("death");
        SimpleParticle.Burst(transform.position, 22, 3f, 10f, 14f, 0.7f, 5, 4, 3, 2);
    }
}
