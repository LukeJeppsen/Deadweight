using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > Pixel Art > Polish Game Scene (open Game.unity first)
/// Smooths the way the game looks and moves:
///  - Pixel Perfect Camera snaps sprites to the pixel grid (stops shimmer while the camera moves)
///  - Cinemachine follows with a short, snappy damping and sits a little above the player (you are climbing)
///  - Player Rigidbody uses interpolation + continuous collision (no stutter, no tunnelling during launches)
/// </summary>
public static class GamePolish
{
    [MenuItem("Tools/Pixel Art/Polish Game Scene")]
    public static void Polish()
    {
        Camera main = Camera.main;
        if (main == null)
        {
            EditorUtility.DisplayDialog("Polish Game Scene", "Open Game.unity first (no MainCamera found).", "OK");
            return;
        }
        string report = "";

        PixelPerfectCamera ppc = main.GetComponent<PixelPerfectCamera>();
        if (ppc != null)
        {
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            report += "- Pixel Perfect Camera: pixel snapping on\n";
        }
        else report += "- No Pixel Perfect Camera on the main camera (run Fix Game Camera first)\n";

        Type cmType = Type.GetType("Unity.Cinemachine.CinemachineFollow, Unity.Cinemachine");
        if (cmType != null)
        {
            UnityEngine.Object follow = UnityEngine.Object.FindFirstObjectByType(cmType);
            if (follow != null)
            {
                SerializedObject so = new SerializedObject(follow);
                SerializedProperty damp = so.FindProperty("TrackerSettings.PositionDamping");
                if (damp != null) damp.vector3Value = new Vector3(0.2f, 0.35f, 0f);
                SerializedProperty off = so.FindProperty("FollowOffset");
                if (off != null) off.vector3Value = new Vector3(0f, 1.5f, -10f);
                so.ApplyModifiedPropertiesWithoutUndo();
                report += "- Cinemachine: damping 0.2 / 0.35, camera 1.5 units above the player\n";
            }
        }

        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                EditorUtility.SetDirty(rb);
                report += "- Player Rigidbody: interpolation + continuous collision\n";
            }
        }

        EditorSceneManager.MarkSceneDirty(main.gameObject.scene);
        EditorSceneManager.SaveScene(main.gameObject.scene);
        Debug.Log("[Polish]\n" + report);
        EditorUtility.DisplayDialog("Polish Game Scene", "Done and saved:\n" + report, "OK");
    }
}
