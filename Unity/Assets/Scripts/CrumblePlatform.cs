using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>A platform that crumbles shortly after you stand on it, then comes back.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CrumblePlatform : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Sprite[] stages;             // intact, cracked, nearly gone
    public float delay = 0.5f;
    public float respawnTime = 2.6f;

    private BoxCollider2D col;
    private bool crumbling;

    private void Awake() { col = GetComponent<BoxCollider2D>(); }

    private void OnCollisionStay2D(Collision2D c)
    {
        if (crumbling) return;
        if (c.collider.GetComponentInParent<PlayerController>() == null) return;
        // only when standing on top (the player's feet are at the platform's surface)
        if (c.collider.bounds.min.y < col.bounds.max.y - 0.2f) return;
        StartCoroutine(Crumble());
    }

    private IEnumerator Crumble()
    {
        crumbling = true;
        Sfx.PlayAt("crumble_crack", col.bounds.center, 0.9f);
        Vector3 home = sprite != null ? sprite.transform.localPosition : Vector3.zero;
        float t = 0f;
        while (t < delay)
        {
            t += Time.deltaTime;
            float k = t / delay;
            if (sprite != null && stages != null && stages.Length > 0)
                sprite.sprite = stages[Mathf.Min(stages.Length - 1, (int)(k * stages.Length))];
            if (sprite != null)
                sprite.transform.localPosition = home + new Vector3((Mathf.Round(Random.Range(-1f, 1f)) / 16f) * k, 0f, 0f);
            yield return null;
        }
        if (sprite != null) sprite.transform.localPosition = home;
        SimpleParticle.Chips(col.bounds.center, 6, 4f, 0.8f);
        Sfx.PlayAt("crumble_fall", col.bounds.center, 0.9f);
        Bounds area = col.bounds;          // a disabled collider reports empty bounds, so keep them now
        col.enabled = false;
        if (sprite != null) sprite.enabled = false;
        yield return new WaitForSeconds(respawnTime);
        // wait until the spot is clear of the player before coming back
        while (Physics2D.OverlapBox(area.center, area.size, 0f, 1 << 7) != null) yield return null;
        col.enabled = true;
        if (sprite != null)
        {
            sprite.enabled = true;
            if (stages != null && stages.Length > 0) sprite.sprite = stages[0];
        }
        crumbling = false;
    }
}
