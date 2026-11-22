using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > Pixel Art > Fix Game Camera (open Game scene first)
/// - Removes the stray extra Camera (and its Pixel Perfect Camera) that was sitting on the CinemachineCamera object.
///   It rendered on top of the real camera, which is what produced the blue frame and the mis-sized background.
/// - Puts a Pixel Perfect Camera (16 PPU, 320x180, window-boxed) on the Main Camera instead, so the view is always
///   exactly the 320x180 the background art was made for.
/// - Adds Cinemachine's Pixel Perfect extension so Cinemachine and the Pixel Perfect Camera don't fight.
/// </summary>
public static class FixGameCamera
{
    [MenuItem("Tools/Pixel Art/Fix Game Camera")]
    public static void Fix()
    {
        Camera main = Camera.main;
        if (main == null)
        {
            EditorUtility.DisplayDialog("Fix Game Camera", "No camera tagged MainCamera in the open scene. Open Game.unity first.", "OK");
            return;
        }

        int removed = 0;

        // 1. Stray cameras: any Camera that is not the main camera AND lives on a CinemachineCamera object
        Type cmType = Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        foreach (Camera cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cam == main) continue;
            bool onCinemachine = cmType != null && cam.GetComponent(cmType) != null;
            if (!onCinemachine) continue;

            PixelPerfectCamera strayPpc = cam.GetComponent<PixelPerfectCamera>();
            if (strayPpc != null) { UnityEngine.Object.DestroyImmediate(strayPpc); removed++; }
            UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data != null) UnityEngine.Object.DestroyImmediate(data);
            UnityEngine.Object.DestroyImmediate(cam);
            removed++;
        }

        // 2. Main camera: solid dark background + Pixel Perfect Camera
        main.clearFlags = CameraClearFlags.SolidColor;
        main.backgroundColor = new Color32(0x27, 0x27, 0x44, 0xFF);
        main.orthographic = true;
        main.orthographicSize = 5.625f;

        PixelPerfectCamera ppc = main.GetComponent<PixelPerfectCamera>();
        if (ppc == null) ppc = main.gameObject.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = 16;
        ppc.refResolutionX = 320;
        ppc.refResolutionY = 180;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.None;

        // 3. Cinemachine <-> Pixel Perfect Camera extension on the CinemachineCamera
        string extNote = "";
        Type ext = Type.GetType("Unity.Cinemachine.CinemachinePixelPerfect, Unity.Cinemachine");
        if (cmType != null && ext != null)
        {
            UnityEngine.Object cm = UnityEngine.Object.FindFirstObjectByType(cmType);
            if (cm != null)
            {
                GameObject go = ((Component)cm).gameObject;
                if (go.GetComponent(ext) == null) { go.AddComponent(ext); extNote = " Added the Cinemachine Pixel Perfect extension."; }
            }
        }

        EditorSceneManager.MarkSceneDirty(main.gameObject.scene);
        EditorSceneManager.SaveScene(main.gameObject.scene);
        Debug.Log("[Camera] Removed " + removed + " stray component(s); Main Camera now has a Pixel Perfect Camera (320x180, PPU 16, windowbox)." + extNote);
        EditorUtility.DisplayDialog("Fix Game Camera", "Done and saved.\nRemoved " + removed + " stray camera component(s) and set up the Pixel Perfect Camera on the Main Camera." + extNote + "\n\nPress Play to check it.", "OK");
    }
}
