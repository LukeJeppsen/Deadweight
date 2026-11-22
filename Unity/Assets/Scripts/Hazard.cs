using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Anything that kills the player on touch (spike tilemap, kill zone, ...).</summary>
public class Hazard : MonoBehaviour
{
    private void Awake()
    {
        Tilemap tm = GetComponent<Tilemap>();
        if (tm != null) BuildSpikeHitboxes(tm);
    }

    /// <summary>
    /// The spike tiles use "Sprite" collision, but the tileset is imported without physics outlines, so the
    /// tilemap collider had no shape at all and spikes never killed anyone. Build one hitbox per spike instead,
    /// covering the drawn teeth (a little inside the tips so near-misses stay fair).
    /// </summary>
    private void BuildSpikeHitboxes(Tilemap tm)
    {
        TilemapCollider2D old = GetComponent<TilemapCollider2D>();
        if (old != null) old.enabled = false;

        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
        if (poly == null) poly = gameObject.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;

        Vector3 cs = tm.layoutGrid != null ? tm.layoutGrid.cellSize : Vector3.one * 0.5f;
        float px = cs.x / 8f;     // the spike art is 8x8 pixels per cell
        List<Vector2[]> paths = new List<Vector2[]>();
        BoundsInt b = tm.cellBounds;
        foreach (Vector3Int c in b.allPositionsWithin)
        {
            TileBase t = tm.GetTile(c);
            if (t == null) continue;
            // pixel rectangle (x0, y0, x1, y1) of the teeth inside the 8x8 cell, by direction
            Vector4 r;
            string n = t.name;
            if (n.Contains("down")) r = new Vector4(1, 3, 7, 8);
            else if (n.Contains("left")) r = new Vector4(3, 1, 8, 7);
            else if (n.Contains("right")) r = new Vector4(0, 1, 5, 7);
            else r = new Vector4(1, 0, 7, 5);      // up
            Vector2 o = tm.CellToLocal(c);
            paths.Add(new[]
            {
                o + new Vector2(r.x * px, r.y * px), o + new Vector2(r.z * px, r.y * px),
                o + new Vector2(r.z * px, r.w * px), o + new Vector2(r.x * px, r.w * px)
            });
        }
        poly.pathCount = paths.Count;
        for (int i = 0; i < paths.Count; i++) poly.SetPath(i, paths[i]);
    }

    private void OnTriggerEnter2D(Collider2D other) { Hit(other); }
    private void OnTriggerStay2D(Collider2D other) { Hit(other); }

    private void Hit(Collider2D other)
    {
        if (LevelDirector.Instance == null) return;
        if (other.GetComponentInParent<PlayerController>() != null) LevelDirector.Instance.KillPlayer();
    }
}
