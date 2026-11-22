using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Turns the Player in Game.unity into Assets/Prefabs/Player.prefab so every level scene shares one player.
/// Only runs when the prefab does not exist yet (delete the prefab to re-extract it).
/// </summary>
public static class PlayerPrefabTool
{
    public const string PrefabPath = "Assets/Prefabs/Player.prefab";
    const string GameScene = "Assets/Scenes/Game.unity";

    public static bool EnsurePrefab()
    {
        if (File.Exists(PrefabPath)) return true;

        string scenePath = GameScene;
        if (!File.Exists(scenePath))
        {
            string[] g = AssetDatabase.FindAssets("Game t:Scene");
            scenePath = null;
            for (int i = 0; i < g.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(g[i]);
                if (Path.GetFileNameWithoutExtension(p) == "Game") { scenePath = p; break; }
            }
        }
        if (scenePath == null) { Debug.LogError("[Deadweight] Could not find Game.unity to extract the Player from."); return false; }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Debug.LogError("[Deadweight] No PlayerController in " + scenePath); return false; }
        GameObject player = pc.gameObject;

        // the new components live on the prefab so the inspector shows them
        if (player.GetComponent<PlayerAbilities>() == null) player.AddComponent<PlayerAbilities>();
        if (player.GetComponent<ShardPulse>() == null) player.AddComponent<ShardPulse>();
        if (player.GetComponent<PlayerEffects>() == null) player.AddComponent<PlayerEffects>();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
        if (prefab == null) { Debug.LogError("[Deadweight] Saving the Player prefab failed."); return false; }
        AssetDatabase.SaveAssets();
        Debug.Log("[Deadweight] Created " + PrefabPath + " from " + scenePath);
        return true;
    }
}
