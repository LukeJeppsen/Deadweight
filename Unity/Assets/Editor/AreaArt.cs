using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Imports and slices the art for the six areas (tilesets, props, backgrounds) and the level-select UI, and builds
/// each area's tile assets (copies of the original tiles, pointed at that area's recoloured tileset).
/// </summary>
public static class AreaArt
{
    public const string AreasDir = "Assets/Art/Areas";
    public const string UiDir = "Assets/Art/UI/Levels";
    public const string TilesRoot = "Assets/Tiles";
    public const string OriginalTileset = "Assets/Art/Tileset/tileset.png";
    public const string OriginalTilesDir = "Assets/Art/Tileset/Tiles";
    public const string OriginalBgDir = "Assets/Art/Background";
    public const float PPU = 16f;

    [Serializable] public class PropEntry { public string name; public int x, y, w, h; public float px, py; }
    [Serializable] public class PropLayout { public int width, height; public PropEntry[] sprites; }

    // tiles copied for every area (everything that is just a sprite from the tileset)
    public static readonly string[] TileNames =
    {
        "Terrain_RuleTile", "backwall_0", "backwall_1", "backwall_2", "backwall_3", "backwall_4", "backwall_5",
        "platform_left", "platform_mid_0", "platform_mid_1", "platform_right", "platform_single", "platform_cracked_0",
        "spike_up", "spike_down", "spike_left", "spike_right",
        "hang_stalactite_a", "hang_stalactite_b",
        "decor_pebbles", "decor_rock", "decor_mushroom", "decor_flower", "decor_grass_a", "decor_grass_b", "decor_grass_c", "decor_tallgrass"
    };

    public static string TileDir(string areaId) { return TilesRoot + "/" + areaId; }

    // ------------------------------------------------------------------ public

    public static void ImportAll()
    {
        AssetDatabase.Refresh();
        EnsureFolder("Assets/Tiles");
        EnsureFolder("Assets/Prefabs");
        ImportUi();
        PropLayout layout = LoadPropLayout();
        for (int i = 0; i < LevelCatalog.Count; i++)
        {
            string id = LevelCatalog.All[i].id;
            ImportArea(id, layout);
            MakeAreaTiles(id);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Deadweight] Area art imported.");
    }

    public static Dictionary<string, Sprite> SpritesAt(string path)
    {
        Dictionary<string, Sprite> d = new Dictionary<string, Sprite>();
        UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < all.Length; i++)
        {
            Sprite s = all[i] as Sprite;
            if (s != null && !d.ContainsKey(s.name)) d[s.name] = s;
        }
        return d;
    }

    public static Sprite SingleSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        Dictionary<string, Sprite> d = SpritesAt(path);
        foreach (KeyValuePair<string, Sprite> kv in d) return kv.Value;
        return null;
    }

    // ------------------------------------------------------------------ area

    static PropLayout LoadPropLayout()
    {
        TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(AreasDir + "/props_layout.json");
        if (ta == null) { Debug.LogError("[Deadweight] Missing " + AreasDir + "/props_layout.json"); return null; }
        return JsonUtility.FromJson<PropLayout>(ta.text);
    }

    static void ImportArea(string id, PropLayout layout)
    {
        string dir = AreasDir + "/" + id;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // the first area keeps the original backgrounds you already have
        if (id == LevelCatalog.All[0].id) CopyOriginalBackgrounds(dir);

        // tileset: same slices as the original tileset
        string tsPath = dir + "/tileset.png";
        if (File.Exists(tsPath))
        {
            Configure(tsPath, SpriteImportMode.Multiple, false);
            List<SpriteRect> rects = new List<SpriteRect>();
            Dictionary<string, Sprite> orig = SpritesAt(OriginalTileset);
            foreach (KeyValuePair<string, Sprite> kv in orig)
            {
                Sprite s = kv.Value;
                rects.Add(MakeRect(kv.Key, s.rect, new Vector2(0.5f, 0.5f)));
            }
            if (rects.Count == 0) Debug.LogWarning("[Deadweight] Could not read the original tileset slices at " + OriginalTileset);
            else ApplySlices(tsPath, rects);
        }
        else Debug.LogWarning("[Deadweight] Missing " + tsPath);

        // props atlas
        string pPath = dir + "/props.png";
        if (File.Exists(pPath) && layout != null)
        {
            Configure(pPath, SpriteImportMode.Multiple, false);
            List<SpriteRect> rects = new List<SpriteRect>();
            for (int i = 0; i < layout.sprites.Length; i++)
            {
                PropEntry e = layout.sprites[i];
                float yUnity = layout.height - (e.y + e.h);
                rects.Add(MakeRect(e.name, new Rect(e.x, yUnity, e.w, e.h), new Vector2(e.px, e.py)));
            }
            ApplySlices(pPath, rects);
        }

        // backgrounds
        string[] layers = { "sky", "aux", "far", "mid", "near" };
        for (int i = 0; i < layers.Length; i++)
        {
            string p = dir + "/bg_" + layers[i] + ".png";
            if (File.Exists(p)) Configure(p, SpriteImportMode.Single, true);
        }
    }

    static void CopyOriginalBackgrounds(string dir)
    {
        string[,] map = { { "sky", "sky" }, { "aux", "clouds" }, { "far", "mountains_far" }, { "mid", "mountains_mid" }, { "near", "forest" } };
        for (int i = 0; i < map.GetLength(0); i++)
        {
            string src = OriginalBgDir + "/" + map[i, 1] + ".png";
            string dst = dir + "/bg_" + map[i, 0] + ".png";
            if (!File.Exists(src) || File.Exists(dst)) continue;
            AssetDatabase.CopyAsset(src, dst);
        }
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------ tiles

    static void MakeAreaTiles(string id)
    {
        string dst = TileDir(id);
        EnsureFolder(dst);
        string ts = AreasDir + "/" + id + "/tileset.png";
        Dictionary<string, Sprite> byName = SpritesAt(ts);
        if (byName.Count == 0) { Debug.LogWarning("[Deadweight] No sprites in " + ts + " yet."); return; }

        for (int i = 0; i < TileNames.Length; i++)
        {
            string name = TileNames[i];
            string src = OriginalTilesDir + "/" + name + ".asset";
            if (!File.Exists(src)) { Debug.LogWarning("[Deadweight] Original tile missing: " + src); continue; }
            string dstPath = dst + "/" + name + ".asset";
            if (File.Exists(dstPath)) AssetDatabase.DeleteAsset(dstPath);
            if (!AssetDatabase.CopyAsset(src, dstPath)) { Debug.LogWarning("[Deadweight] Could not copy " + src); continue; }
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dstPath);
            if (obj == null) continue;
            RemapSprites(obj, byName);
        }

        // the cracked / ghost / breakable tiles come from the props atlas
        string props = AreasDir + "/" + id + "/props.png";
        Dictionary<string, Sprite> pr = SpritesAt(props);
        MakeTile(dst + "/brk_0.asset", GetOrNull(pr, "brk_0"), Tile.ColliderType.Grid);
        MakeTile(dst + "/brk_1.asset", GetOrNull(pr, "brk_1"), Tile.ColliderType.Grid);
        MakeTile(dst + "/brk_2.asset", GetOrNull(pr, "brk_2"), Tile.ColliderType.Grid);
        MakeTile(dst + "/ghost_0.asset", GetOrNull(pr, "ghost_0"), Tile.ColliderType.Grid);
        MakeTile(dst + "/ghost_1.asset", GetOrNull(pr, "ghost_1"), Tile.ColliderType.Grid);
    }

    static Sprite GetOrNull(Dictionary<string, Sprite> d, string key)
    {
        Sprite s;
        return d.TryGetValue(key, out s) ? s : null;
    }

    static void RemapSprites(UnityEngine.Object asset, Dictionary<string, Sprite> byName)
    {
        SerializedObject so = new SerializedObject(asset);
        SerializedProperty it = so.GetIterator();
        bool enter = true;
        int changed = 0;
        while (it.Next(enter))
        {
            enter = true;
            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
            Sprite sp = it.objectReferenceValue as Sprite;
            if (sp == null) continue;
            Sprite repl;
            if (byName.TryGetValue(sp.name, out repl) && repl != sp) { it.objectReferenceValue = repl; changed++; }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    static void MakeTile(string path, Sprite sprite, Tile.ColliderType collider)
    {
        if (sprite == null) { Debug.LogWarning("[Deadweight] Sprite missing for " + path); return; }
        Tile t = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(t, path);
        }
        t.sprite = sprite;
        t.colliderType = collider;
        EditorUtility.SetDirty(t);
    }

    public static TileBase LoadTile(string areaId, string name)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>(TileDir(areaId) + "/" + name + ".asset");
    }

    // ------------------------------------------------------------------ ui

    static void ImportUi()
    {
        if (!Directory.Exists(UiDir)) { Debug.LogWarning("[Deadweight] Missing " + UiDir); return; }
        string[] files = Directory.GetFiles(UiDir, "*.png");
        for (int i = 0; i < files.Length; i++)
        {
            string p = files[i].Replace('\\', '/');
            Configure(p, SpriteImportMode.Single, false);
        }
        // the shared panel must be a 9-slice
        string panel = "Assets/Art/Menu/panel.png";
        TextureImporter pi = AssetImporter.GetAtPath(panel) as TextureImporter;
        if (pi != null && pi.spriteBorder == Vector4.zero)
        {
            pi.textureType = TextureImporterType.Sprite;
            pi.spriteBorder = new Vector4(8, 8, 8, 8);
            pi.filterMode = FilterMode.Point;
            pi.textureCompression = TextureImporterCompression.Uncompressed;
            pi.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------ helpers

    static void EnsureFolder(string path)
    {
        if (Directory.Exists(path)) return;
        Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
    }

    static void Configure(string path, SpriteImportMode mode, bool fullRect)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogWarning("[Deadweight] Not imported yet: " + path); return; }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = mode;
        imp.spritePixelsPerUnit = PPU;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.crunchedCompression = false;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.isReadable = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.npotScale = TextureImporterNPOTScale.None;

        TextureImporterSettings ts = new TextureImporterSettings();
        imp.ReadTextureSettings(ts);
        ts.spriteMeshType = SpriteMeshType.FullRect;
        ts.spriteGenerateFallbackPhysicsShape = false;
        if (mode == SpriteImportMode.Single)
        {
            ts.spriteAlignment = (int)SpriteAlignment.Center;
            ts.spritePivot = new Vector2(0.5f, 0.5f);
        }
        imp.SetTextureSettings(ts);
        imp.SaveAndReimport();
    }

    static SpriteRect MakeRect(string name, Rect rect, Vector2 pivot)
    {
        SpriteRect r = new SpriteRect();
        r.name = name;
        r.rect = rect;
        r.alignment = SpriteAlignment.Custom;
        r.pivot = pivot;
        r.spriteID = GUID.Generate();
        return r;
    }

    static void ApplySlices(string path, List<SpriteRect> rects)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();

        // keep sprite ids stable across re-runs
        Dictionary<string, GUID> existing = new Dictionary<string, GUID>();
        SpriteRect[] old = dp.GetSpriteRects();
        for (int i = 0; i < old.Length; i++) existing[old[i].name] = old[i].spriteID;
        for (int i = 0; i < rects.Count; i++)
        {
            GUID id;
            if (existing.TryGetValue(rects[i].name, out id)) rects[i].spriteID = id;
        }

        dp.SetSpriteRects(rects.ToArray());
        ISpriteNameFileIdDataProvider nameProvider = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        List<SpriteNameFileIdPair> pairs = new List<SpriteNameFileIdPair>();
        for (int i = 0; i < rects.Count; i++) pairs.Add(new SpriteNameFileIdPair(rects[i].name, rects[i].spriteID));
        nameProvider.SetNameFileIdPairs(pairs);
        dp.Apply();
        imp.SaveAndReimport();
    }
}
