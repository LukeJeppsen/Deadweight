using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Ghost stone: faint and non-solid until the Shard Pulses, then solid for a few seconds
/// (it blinks when the pulse is about to run out).
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class GhostTilemap : MonoBehaviour
{
    public float dimAlpha = 0.3f;
    public float warnSeconds = 0.9f;

    private Tilemap map;
    private TilemapCollider2D col;

    private void Awake()
    {
        map = GetComponent<Tilemap>();
        col = GetComponent<TilemapCollider2D>();
        Apply(false, 1f);
    }

    private void Update()
    {
        bool on = ShardPulse.Active;
        float a = on ? 1f : dimAlpha;
        if (on && ShardPulse.Remaining < warnSeconds)
            a = ((int)(Time.time * 10f) % 2 == 0) ? 1f : 0.55f;     // about to fade: blink
        Apply(on, a);
    }

    private void Apply(bool solid, float alpha)
    {
        if (col != null && col.enabled != solid) col.enabled = solid;
        if (map != null) map.color = new Color(1f, 1f, 1f, alpha);
    }
}
