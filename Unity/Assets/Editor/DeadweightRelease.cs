using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Release tooling for itch.io.
///   Tools > Deadweight > Apply Release Settings   - name, icon, WebGL player settings, scene list, menu wiring
///   Tools > Deadweight > Build WebGL for itch.io  - all of the above, then a WebGL build in Builds/WebGL and
///                                                   Builds/Deadweight_web.zip ready to upload
/// Headless: Unity.exe -batchmode -quit -projectPath ... -buildTarget WebGL -executeMethod DeadweightRelease.BuildWebGL
/// </summary>
public static class DeadweightRelease
{
    public const string ProductName = "Deadweight";
    public const string CompanyName = "Deadweight";          // change to your name / studio
    public const string Version = "1.0.0";

    const string IconPath = "Assets/Art/Icon/deadweight_icon.png";
    const string TemplateName = "Deadweight";
    const string TemplateDir = "Assets/WebGLTemplates/" + TemplateName;
    public const string OutDir = "Builds/WebGL";
    public const string ZipPath = "Builds/Deadweight_web.zip";

    static readonly Color32 Ink = new Color32(0x27, 0x27, 0x44, 0xFF);
    static readonly Color32 Sand = new Color32(0xF2, 0xD3, 0xAB, 0xFF);
    static readonly Color32 Rose = new Color32(0xC6, 0x9F, 0xA5, 0xFF);

    [MenuItem("Tools/Deadweight/Apply Release Settings")]
    public static void ApplySettingsMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        ApplySettings();
        EditorUtility.DisplayDialog("Deadweight", "Release settings applied (name, icon, WebGL template and compression, scene list).", "OK");
    }

    public static void ApplySettings()
    {
        PlayerSettings.productName = ProductName;
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.bundleVersion = Version;

        // icon (also the browser tab icon via the WebGL template)
        Texture2D icon = MakeIcon();
        if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        PlayerSettings.SplashScreen.backgroundColor = (Color)Ink;
        PlayerSettings.runInBackground = false;            // pause when the tab / window is in the background

        // desktop build (if you ever make one): fullscreen window, but resizable when windowed
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;

        // WebGL for itch.io
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        if (Directory.Exists(TemplateDir)) PlayerSettings.WebGL.template = "PROJECT:" + TemplateName;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;  // itch doesn't send Content-Encoding headers
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.showDiagnostics = false;

        // quieter, smaller logs in builds
        PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

        DeadweightSetup.SetBuildSettings();                 // menus + levels + ending, no sandbox scenes
        WireMainMenu();
        AssetDatabase.SaveAssets();
        Debug.Log("[Release] Settings applied: " + ProductName + " " + Version + ", " + EditorBuildSettings.scenes.Length + " scenes.");
    }

    // ------------------------------------------------------------------ build

    [MenuItem("Tools/Deadweight/Build WebGL for itch.io")]
    public static void BuildWebGLMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        bool ok = BuildWebGLInternal();
        EditorUtility.DisplayDialog("Deadweight", ok
            ? "Built " + OutDir + " and zipped it to " + ZipPath + ".\nUpload the zip to itch.io as an HTML game."
            : "The build failed - see the Console.", "OK");
    }

    /// <summary>Headless entry point (exits with code 1 on failure).</summary>
    public static void BuildWebGL()
    {
        bool ok = BuildWebGLInternal();
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool BuildWebGLInternal()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Debug.LogError("[Release] WebGL Build Support isn't installed. Add it in Unity Hub > Installs > 6000.3.10f1 > Add modules.");
            return false;
        }
        ApplySettings();

        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);

        if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
        Directory.CreateDirectory(OutDir);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = OutDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[Release] WebGL build " + report.summary.result + " with " + report.summary.totalErrors + " errors.");
            return false;
        }

        Zip(OutDir, ZipPath);
        long zipBytes = new FileInfo(ZipPath).Length;
        Debug.Log("[Release] WebGL build OK (" + (report.summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB). Zip: " + ZipPath
                  + " (" + (zipBytes / (1024f * 1024f)).ToString("0.0") + " MB)");
        return true;
    }

    /// <summary>Zips a folder with forward-slash entry names (itch.io needs index.html at the zip root).</summary>
    static void Zip(string dir, string zipPath)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        string root = Path.GetFullPath(dir).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        using (FileStream fs = File.Create(zipPath))
        using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetFullPath(file).Substring(root.Length).Replace('\\', '/');
                ZipArchiveEntry e = zip.CreateEntry(rel, System.IO.Compression.CompressionLevel.Optimal);
                using (Stream es = e.Open())
                using (FileStream src = File.OpenRead(file)) src.CopyTo(es);
            }
        }
    }

    // ------------------------------------------------------------------ main menu wiring

    /// <summary>Gives the title screen the font and panel it needs for the Options and Controls screens.</summary>
    static void WireMainMenu()
    {
        string path = null;
        foreach (string g in AssetDatabase.FindAssets("MainMenu t:Scene"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) == "MainMenu") path = p;
        }
        if (path == null) { Debug.LogWarning("[Release] MainMenu scene not found."); return; }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        TitleMenuController ctl = Object.FindFirstObjectByType<TitleMenuController>();
        if (ctl == null) { Debug.LogWarning("[Release] No TitleMenuController in " + path); return; }
        SerializedObject so = new SerializedObject(ctl);
        so.FindProperty("fontAtlas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(LevelBuilder.FontPath);
        so.FindProperty("panelSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(LevelBuilder.PanelPath);
        if (so.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    // ------------------------------------------------------------------ icon

    /// <summary>
    /// Draws a 32x32 pixel icon (the hero's first idle frame on the menu's ink colour) and saves it 8x upscaled.
    /// Also writes favicon.png into the WebGL template. Replace Assets/Art/Icon/deadweight_icon.png with your own art
    /// any time - it is only regenerated when missing.
    /// </summary>
    static Texture2D MakeIcon()
    {
        if (!File.Exists(IconPath))
        {
            const string src = "Assets/Art/Player/idle_player.png";
            if (!File.Exists(src)) { Debug.LogWarning("[Release] No player art for the icon."); return null; }
            Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sheet.LoadImage(File.ReadAllBytes(src));
            int fh = sheet.height, fw = fh;                  // frames are square, laid out in a row

            // bounding box of the first frame
            int minX = fw, minY = fh, maxX = -1, maxY = -1;
            for (int y = 0; y < fh; y++)
                for (int x = 0; x < fw; x++)
                    if (sheet.GetPixel(x, y).a > 0.5f) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            if (maxX < 0) { minX = 0; minY = 0; maxX = fw - 1; maxY = fh - 1; }
            int bw = maxX - minX + 1, bh = maxY - minY + 1;

            const int N = 32;
            Color32[] px = new Color32[N * N];
            for (int i = 0; i < px.Length; i++) px[i] = Ink;
            // frame and ground line
            for (int i = 0; i < N; i++) { px[i] = Rose; px[(N - 1) * N + i] = Rose; px[i * N] = Rose; px[i * N + N - 1] = Rose; }
            for (int x = 3; x < N - 3; x++) px[4 * N + x] = Sand;

            int scale = Mathf.Max(1, Mathf.Min(24 / bw, 24 / bh));
            int ox = (N - bw * scale) / 2, oy = 5;
            for (int y = 0; y < bh; y++)
                for (int x = 0; x < bw; x++)
                {
                    Color c = sheet.GetPixel(minX + x, minY + y);
                    if (c.a < 0.5f) continue;
                    for (int sy = 0; sy < scale; sy++)
                        for (int sx = 0; sx < scale; sx++)
                        {
                            int X = ox + x * scale + sx, Y = oy + y * scale + sy;
                            if (X >= 0 && X < N && Y >= 0 && Y < N) px[Y * N + X] = c;
                        }
                }

            Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
            File.WriteAllBytes(IconPath, Upscale(px, N, 8).EncodeToPNG());
            if (Directory.Exists(TemplateDir + "/TemplateData"))
                File.WriteAllBytes(TemplateDir + "/TemplateData/favicon.png", Upscale(px, N, 2).EncodeToPNG());
            AssetDatabase.ImportAsset(IconPath);
        }

        TextureImporter ti = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (ti != null && (ti.filterMode != FilterMode.Point || ti.textureCompression != TextureImporterCompression.Uncompressed))
        {
            ti.textureType = TextureImporterType.Default;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
    }

    static Texture2D Upscale(Color32[] px, int n, int k)
    {
        Texture2D t = new Texture2D(n * k, n * k, TextureFormat.RGBA32, false);
        Color32[] o = new Color32[n * k * n * k];
        for (int y = 0; y < n * k; y++)
            for (int x = 0; x < n * k; x++)
                o[y * n * k + x] = px[(y / k) * n + x / k];
        t.SetPixels32(o);
        t.Apply();
        return t;
    }
}
