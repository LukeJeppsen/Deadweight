using System.Collections;
using UnityEngine;

/// <summary>An expanding, fading ring (used by the Shard Pulse and checkpoint lighting).</summary>
public class RingFx : MonoBehaviour
{
    public static void Spawn(Sprite sprite, Vector2 pos, float startScale, float endScale, float life, Color color)
    {
        if (sprite == null) return;
        GameObject go = new GameObject("Ring");
        go.transform.position = pos;
        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = 15;
        RingFx fx = go.AddComponent<RingFx>();
        fx.StartCoroutine(fx.Run(r, startScale, endScale, life, color));
    }

    private IEnumerator Run(SpriteRenderer r, float a, float b, float life, Color color)
    {
        float t = 0f;
        while (t < life)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            transform.localScale = Vector3.one * Mathf.Lerp(a, b, 1f - (1f - k) * (1f - k));
            Color c = color;
            c.a = color.a * Mathf.Ceil((1f - k) * 4f) / 4f;
            r.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}
