using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>An updraft (or gust) the player can ride. Drifting streaks show where it blows.</summary>
public class WindZone : MonoBehaviour
{
    public Vector2 targetVelocity = new Vector2(0f, 6f);
    public float accel = 90f;
    public Sprite[] streakSprites;
    public int streakCount = 10;

    private BoxCollider2D box;
    private readonly List<Transform> streaks = new List<Transform>();

    private void Start()
    {
        box = GetComponent<BoxCollider2D>();
        if (box == null || streakSprites == null || streakSprites.Length == 0) return;
        Bounds b = box.bounds;
        for (int i = 0; i < streakCount; i++)
        {
            GameObject g = new GameObject("Streak");
            g.transform.SetParent(transform, true);
            g.transform.position = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
            SpriteRenderer r = g.AddComponent<SpriteRenderer>();
            r.sprite = streakSprites[i % streakSprites.Length];
            r.sortingOrder = 3;
            Color c = Color.white;
            c.a = 0.55f;
            r.color = c;
            streaks.Add(g.transform);
        }
    }

    private void Update()
    {
        if (box == null || streaks.Count == 0) return;
        Bounds b = box.bounds;
        Vector2 dir = targetVelocity.sqrMagnitude > 0.01f ? targetVelocity.normalized : Vector2.up;
        for (int i = 0; i < streaks.Count; i++)
        {
            Transform t = streaks[i];
            t.position += (Vector3)(dir * (5f + (i % 3)) * Time.deltaTime);
            Vector3 p = t.position;
            if (p.y > b.max.y) p.y = b.min.y;
            if (p.y < b.min.y) p.y = b.max.y;
            if (p.x > b.max.x) p.x = b.min.x;
            if (p.x < b.min.x) p.x = b.max.x;
            t.position = p;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerMovement m = other.GetComponentInParent<PlayerMovement>();
        if (m != null) m.ApplyWind(targetVelocity, accel);
    }
}
