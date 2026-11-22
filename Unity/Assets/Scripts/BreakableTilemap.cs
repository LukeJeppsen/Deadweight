using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Cracked stone. Any contiguous patch of tiles on this tilemap shatters at once when the Breaker skill
/// hits it (swing at it, or launch into it).
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class BreakableTilemap : MonoBehaviour
{
    public int maxGroup = 600;
    private Tilemap map;

    private void Awake() { map = GetComponent<Tilemap>(); }

    /// <summary>Shatter every patch that has a tile inside this world-space box.</summary>
    public bool BreakInBox(Vector2 center, Vector2 size)
    {
        if (map == null) map = GetComponent<Tilemap>();
        Vector3Int a = map.WorldToCell(center - size * 0.5f);
        Vector3Int b = map.WorldToCell(center + size * 0.5f);
        bool any = false;
        for (int x = a.x; x <= b.x; x++)
            for (int y = a.y; y <= b.y; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (map.HasTile(cell)) { BreakAt(map.GetCellCenterWorld(cell)); any = true; }
            }
        return any;
    }

    public void BreakAt(Vector2 world)
    {
        if (map == null) map = GetComponent<Tilemap>();
        Vector3Int c = map.WorldToCell(world);
        if (!map.HasTile(c))
        {
            bool found = false;
            for (int dx = -1; dx <= 1 && !found; dx++)
            {
                for (int dy = -1; dy <= 1 && !found; dy++)
                {
                    Vector3Int n = new Vector3Int(c.x + dx, c.y + dy, 0);
                    if (map.HasTile(n)) { c = n; found = true; }
                }
            }
            if (!found) return;
        }

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> seen = new HashSet<Vector3Int>();
        queue.Enqueue(c);
        seen.Add(c);
        int count = 0;
        Vector3Int[] dirs = { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0) };
        while (queue.Count > 0 && count < maxGroup)
        {
            Vector3Int cell = queue.Dequeue();
            Vector3 wc = map.GetCellCenterWorld(cell);
            map.SetTile(cell, null);
            count++;
            if (count % 2 == 0) SimpleParticle.Chips(wc, 1, 6f, 0.8f);
            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3Int nb = cell + dirs[i];
                if (!seen.Contains(nb) && map.HasTile(nb))
                {
                    seen.Add(nb);
                    queue.Enqueue(nb);
                }
            }
        }
        CameraTarget.Shake(0.35f);
        Sfx.PlayAt("shatter", world, 1f, 20f);
    }
}
