using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tools > Deadweight > Verify Levels: opens every level scene (without saving it) and checks that
/// every patch of cracked stone can be shattered by a hit at its edge, that the terrain colliders are merged,
/// and that nothing points at a missing script. Results go to the Console.
/// </summary>
public static class DeadweightVerify
{
    [MenuItem("Tools/Deadweight/Verify Levels")]
    public static void VerifyAll()
    {
        int problems = 0;
        for (int i = 0; i < LevelCatalog.Count; i++)
        {
            string path = LevelBuilder.ScenePath(i);
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            problems += VerifyOpenScene(LevelCatalog.All[i].id);
        }
        foreach (string other in new[] { "Assets/Scenes/MainMenu.unity", LevelSelectBuilder.SelectScenePath, LevelSelectBuilder.EndingScenePath })
        {
            if (!System.IO.File.Exists(other)) continue;
            EditorSceneManager.OpenScene(other, OpenSceneMode.Single);
            problems += VerifyOpenScene(System.IO.Path.GetFileNameWithoutExtension(other));
        }
        Debug.Log(problems == 0 ? "[Verify] ALL LEVELS OK" : "[Verify] " + problems + " PROBLEM(S) FOUND");
    }

    /// <summary>Batch-mode entry point: rebuild every level scene, then verify everything.</summary>
    public static void RebuildAndVerify()
    {
        DeadweightSetup.RebuildLevels();
        VerifyAll();
    }

    static int VerifyOpenScene(string id)
    {
        int problems = 0;
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (Component c in go.GetComponents<Component>())
                if (c == null) { Debug.LogError("[Verify] " + id + ": missing script on " + go.name); problems++; }
        }

        foreach (string n in new[] { "Collision", "Terrain" })
        {
            if (GameObject.Find("Level") == null) break;      // not a level scene
            GameObject t = GameObject.Find("Level/Grid/" + n);
            CompositeCollider2D cc = t != null ? t.GetComponent<CompositeCollider2D>() : null;
            if (cc == null || cc.pathCount == 0) { Debug.LogError("[Verify] " + id + ": " + n + " colliders are not merged"); problems++; }
        }

        BreakableTilemap brk = Object.FindFirstObjectByType<BreakableTilemap>();
        if (brk != null)
        {
            Tilemap map = brk.GetComponent<Tilemap>();
            map.CompressBounds();
            List<Vector3Int> cells = new List<Vector3Int>();
            foreach (Vector3Int c in map.cellBounds.allPositionsWithin) if (map.HasTile(c)) cells.Add(c);
            int before = cells.Count, patches = 0;
            // hit each patch once, from the left edge, the way a player swinging at it would
            foreach (Vector3Int c in cells)
            {
                if (!map.HasTile(c)) continue;
                patches++;
                Vector2 edge = (Vector2)map.GetCellCenterWorld(c) + new Vector2(-0.6f, 0f);
                if (!brk.BreakInBox(edge + new Vector2(0.35f, 0f), new Vector2(1.3f, 1.3f)))
                { Debug.LogError("[Verify] " + id + ": swing next to cracked stone at " + c + " did not break it"); problems++; }
            }
            int left = 0;
            foreach (Vector3Int c in cells) if (map.HasTile(c)) left++;
            Debug.Log("[Verify] " + id + ": cracked stone " + before + " tiles in " + patches + " patches, " + left + " left after one hit each");
            if (left > 0) problems++;
        }
        Debug.Log("[Verify] " + id + ": checked");
        return problems;
    }
}
