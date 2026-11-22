using System.Collections;
using UnityEngine;

/// <summary>A tiny sprite particle: flies, falls, fades, disappears. No particle system needed.</summary>
public class SimpleParticle : MonoBehaviour
{
    public Vector2 velocity;
    public float gravity;
    public float life = 0.5f;
    public float drag;

    private float age;
    private SpriteRenderer sr;
    private Color baseColor;

    public static SimpleParticle Spawn(Sprite sprite, Vector2 pos, Vector2 vel, float life, float gravity, Color color, int order = 20)
    {
        if (sprite == null) return null;
        GameObject go = new GameObject("Particle");
        go.transform.position = pos;
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = order;
        SimpleParticle p = go.AddComponent<SimpleParticle>();
        p.velocity = vel;
        p.life = life;
        p.gravity = gravity;
        p.sr = r;
        p.baseColor = color;
        return p;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        if (age >= life) { Destroy(gameObject); return; }
        velocity.y -= gravity * dt;
        if (drag > 0f) velocity *= Mathf.Max(0f, 1f - drag * dt);
        transform.position += (Vector3)(velocity * dt);
        if (sr != null)
        {
            Color c = baseColor;
            // fade in four steps so it keeps a pixel-art feel
            c.a = baseColor.a * Mathf.Ceil((1f - age / life) * 4f) / 4f;
            sr.color = c;
        }
    }

    // ------------------------------------------------------------------ ready-made bursts

    /// <summary>Radial burst of coloured pixels.</summary>
    public static void Burst(Vector2 pos, int count, float minSpeed, float maxSpeed, float gravity, float life, params int[] paletteIndices)
    {
        LevelAssets a = LevelAssets.Instance;
        if (a == null) return;
        for (int i = 0; i < count; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float sp = Random.Range(minSpeed, maxSpeed);
            int pi = paletteIndices.Length > 0 ? paletteIndices[Random.Range(0, paletteIndices.Length)] : 4;
            // pixels[] is ordered 5,4,3,2,0 - map palette index to that array
            Sprite s = a.Pixel(PixelIndexFor(pi));
            Spawn(s, pos, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp, life * Random.Range(0.7f, 1.2f), gravity, Color.white);
        }
    }

    public static void Chips(Vector2 pos, int count, float speed, float life)
    {
        LevelAssets a = LevelAssets.Instance;
        if (a == null) return;
        for (int i = 0; i < count; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Spawn(a.Shard(i), pos + Random.insideUnitCircle * 0.2f,
                new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) + 0.6f) * Random.Range(speed * 0.4f, speed), life * Random.Range(0.7f, 1.2f), 16f, Color.white);
        }
    }

    private static int PixelIndexFor(int paletteIndex)
    {
        switch (paletteIndex)
        {
            case 5: return 0;
            case 4: return 1;
            case 3: return 2;
            case 2: return 3;
            default: return 4;
        }
    }
}
