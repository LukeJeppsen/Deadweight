using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Compares each built level scene's spike tiles with its level JSON (Assets/Levels).
/// Headless: -executeMethod DeadweightProbe.CheckSpikes ; writes Builds/spike_probe.txt
/// </summary>
public static class DeadweightProbe
{
    [MenuItem("Tools/Deadweight/Check Spikes Match Level Files")]
    public static void CheckSpikes()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < LevelCatalog.Count; i++)
        {
            LevelInfo info = LevelCatalog.All[i];
            string json = File.ReadAllText("Assets/Levels/" + info.id + ".json");
            LevelData d = JsonUtility.FromJson<LevelData>(json);
            EditorSceneManager.OpenScene(LevelBuilder.ScenePath(i), OpenSceneMode.Single);

            Tilemap haz = null;
            foreach (Tilemap t in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None)) if (t.name == "Hazards") haz = t;
            int want = 0, have = 0, missing = 0, extra = 0;
            StringBuilder miss = new StringBuilder();
            for (int r = 0; r < d.Rows; r++)
                for (int c = 0; c < d.cols; c++)
                {
                    char ch = d.At(c, r);
                    bool spike = ch == '^' || ch == 'v' || ch == '<' || ch == '>';
                    bool tile = haz != null && haz.HasTile(new Vector3Int(c, d.Rows - 1 - r, 0));
                    if (spike) want++;
                    if (tile) have++;
                    if (spike && !tile) { missing++; if (missing <= 8) miss.Append(" (" + c + "," + r + ")"); }
                    if (!spike && tile) extra++;
                }
            string colInfo = "";
            if (haz != null)
            {
                TilemapCollider2D tc = haz.GetComponent<TilemapCollider2D>();
                colInfo = " collider=" + (tc != null ? "trigger=" + tc.isTrigger + " enabled=" + tc.enabled : "NONE")
                        + " origin=" + haz.origin + " size=" + haz.size + " pos=" + haz.transform.position;
            }
            sb.AppendLine(info.id + ": json spikes=" + want + " scene spike tiles=" + have + " missing=" + missing + " extra=" + extra + colInfo + (missing > 0 ? " first missing (col,row):" + miss : ""));
        }
        Directory.CreateDirectory("Builds");
        File.WriteAllText("Builds/spike_probe.txt", sb.ToString());
        Debug.Log("[Probe]\n" + sb);
    }
}
