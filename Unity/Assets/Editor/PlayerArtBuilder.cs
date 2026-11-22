using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One-click importer for the generated pixel art: slices every sheet, builds the player animation
/// clips + controllers, wires the player in the open scene, and builds the tileset, animated decor
/// tiles and parallax background prefab.  Menu: Tools > Pixel Art.
/// Safe to run repeatedly (existing assets are updated in place).
/// </summary>
public static class PlayerArtBuilder
{
    // Everything is authored at 16 pixels per unit to match your Pixel Perfect Camera (PPU 16, 320x180):
    // one art pixel = one screen pixel, the 16x16 character is 1 unit and an 8x8 tile is 0.5 unit.
    const int PPU = 16;

    const string PlayerArt = "Assets/Art/Player";
    const string BgArt = "Assets/Art/Background";
    const string TileArt = "Assets/Art/Tileset";
    const string DecorArt = "Assets/Art/Tileset/Decor";
    const string TileOut = "Assets/Art/Tileset/Tiles";
    const string AnimDir = "Assets/Sprites/Animations";
    const string ClipDir = "Assets/Sprites/Animations/Player";

    // ------------------------------------------------------------------ definitions
    class StateDef
    {
        public string State, Sheet;
        public int[] Frames;
        public float Fps;
        public bool Loop;
        public StateDef(string state, string sheet, int[] frames, float fps, bool loop)
        { State = state; Sheet = sheet; Frames = frames; Fps = fps; Loop = loop; }
    }

    static int[] Seq(int a, int b)
    {
        var l = new List<int>();
        for (int i = a; i <= b; i++) l.Add(i);
        return l.ToArray();
    }

    // State names are shared by the body and sword controllers; PlayerAnimationDriver relies on them.
    static readonly StateDef[] States =
    {
        new StateDef("Idle",             "idle",                   Seq(0, 7), 6.67f, true),
        new StateDef("Idle_Shake",       "idle_rare_shake",        Seq(0, 7), 9f,    false),
        new StateDef("Idle_Stretch",     "idle_rare_stretch",      Seq(0, 7), 8f,    false),
        new StateDef("Idle_Flourish",    "idle_rare_flourish",     Seq(0, 7), 12f,   false),
        new StateDef("Idle_ShoulderTap", "idle_rare_shoulder_tap", Seq(0, 7), 8f,    false),
        new StateDef("Walk",             "walk",                   Seq(0, 7), 10f,   true),
        new StateDef("Run",              "run",                    Seq(0, 7), 14f,   true),
        new StateDef("Jump_Rise",        "jump",                   new[] { 1, 2, 3 }, 12f, false),
        new StateDef("Jump_Apex",        "jump",                   new[] { 4 },       8f,  true),
        new StateDef("Jump_Fall",        "jump",                   new[] { 5, 6 },    10f, false),
        new StateDef("Land",             "jump",                   new[] { 7, 0 },    16f, false),
        new StateDef("Slash_Forward",     "slash_forward",         Seq(0, 7), 24f,   false),
        new StateDef("Slash_ForwardUp",   "slash_forward_up",      Seq(0, 7), 24f,   false),
        new StateDef("Slash_Up",          "slash_up",              Seq(0, 7), 24f,   false),
        new StateDef("Slash_ForwardDown", "slash_forward_down",    Seq(0, 7), 24f,   false),
        new StateDef("Slash_Down",        "slash_down",            Seq(0, 7), 24f,   false),
    };

    class TileDef
    {
        public string Name;
        public int Col, Row;
        public int[] Rule;   // N, NE, E, SE, S, SW, W, NW  (1 = solid neighbour, 2 = empty, 0 = any); null for non-terrain
        public TileDef(string name, int col, int row, int[] rule) { Name = name; Col = col; Row = row; Rule = rule; }
    }

    static readonly TileDef[] TileDefs =
    {
        new TileDef("terrain_000", 0, 0, new[] { 2, 0, 2, 0, 2, 0, 2, 0 }),
        new TileDef("terrain_001", 1, 0, new[] { 1, 0, 2, 0, 2, 0, 2, 0 }),
        new TileDef("terrain_004", 2, 0, new[] { 2, 0, 1, 0, 2, 0, 2, 0 }),
        new TileDef("terrain_005", 3, 0, new[] { 1, 2, 1, 0, 2, 0, 2, 0 }),
        new TileDef("terrain_007", 4, 0, new[] { 1, 1, 1, 0, 2, 0, 2, 0 }),
        new TileDef("terrain_016", 5, 0, new[] { 2, 0, 2, 0, 1, 0, 2, 0 }),
        new TileDef("terrain_017", 6, 0, new[] { 1, 0, 2, 0, 1, 0, 2, 0 }),
        new TileDef("terrain_020", 7, 0, new[] { 2, 0, 1, 2, 1, 0, 2, 0 }),
        new TileDef("terrain_021", 8, 0, new[] { 1, 2, 1, 2, 1, 0, 2, 0 }),
        new TileDef("terrain_023", 9, 0, new[] { 1, 1, 1, 2, 1, 0, 2, 0 }),
        new TileDef("terrain_028", 10, 0, new[] { 2, 0, 1, 1, 1, 0, 2, 0 }),
        new TileDef("terrain_029", 11, 0, new[] { 1, 2, 1, 1, 1, 0, 2, 0 }),
        new TileDef("terrain_031", 12, 0, new[] { 1, 1, 1, 1, 1, 0, 2, 0 }),
        new TileDef("terrain_064", 13, 0, new[] { 2, 0, 2, 0, 2, 0, 1, 0 }),
        new TileDef("terrain_065", 14, 0, new[] { 1, 0, 2, 0, 2, 0, 1, 2 }),
        new TileDef("terrain_068", 15, 0, new[] { 2, 0, 1, 0, 2, 0, 1, 0 }),
        new TileDef("terrain_069", 0, 1, new[] { 1, 2, 1, 0, 2, 0, 1, 2 }),
        new TileDef("terrain_071", 1, 1, new[] { 1, 1, 1, 0, 2, 0, 1, 2 }),
        new TileDef("terrain_080", 2, 1, new[] { 2, 0, 2, 0, 1, 2, 1, 0 }),
        new TileDef("terrain_081", 3, 1, new[] { 1, 0, 2, 0, 1, 2, 1, 2 }),
        new TileDef("terrain_084", 4, 1, new[] { 2, 0, 1, 2, 1, 2, 1, 0 }),
        new TileDef("terrain_085", 5, 1, new[] { 1, 2, 1, 2, 1, 2, 1, 2 }),
        new TileDef("terrain_087", 6, 1, new[] { 1, 1, 1, 2, 1, 2, 1, 2 }),
        new TileDef("terrain_092", 7, 1, new[] { 2, 0, 1, 1, 1, 2, 1, 0 }),
        new TileDef("terrain_093", 8, 1, new[] { 1, 2, 1, 1, 1, 2, 1, 2 }),
        new TileDef("terrain_095", 9, 1, new[] { 1, 1, 1, 1, 1, 2, 1, 2 }),
        new TileDef("terrain_112", 10, 1, new[] { 2, 0, 2, 0, 1, 1, 1, 0 }),
        new TileDef("terrain_113", 11, 1, new[] { 1, 0, 2, 0, 1, 1, 1, 2 }),
        new TileDef("terrain_116", 12, 1, new[] { 2, 0, 1, 2, 1, 1, 1, 0 }),
        new TileDef("terrain_117", 13, 1, new[] { 1, 2, 1, 2, 1, 1, 1, 2 }),
        new TileDef("terrain_119", 14, 1, new[] { 1, 1, 1, 2, 1, 1, 1, 2 }),
        new TileDef("terrain_124", 15, 1, new[] { 2, 0, 1, 1, 1, 1, 1, 0 }),
        new TileDef("terrain_125", 0, 2, new[] { 1, 2, 1, 1, 1, 1, 1, 2 }),
        new TileDef("terrain_127", 1, 2, new[] { 1, 1, 1, 1, 1, 1, 1, 2 }),
        new TileDef("terrain_193", 2, 2, new[] { 1, 0, 2, 0, 2, 0, 1, 1 }),
        new TileDef("terrain_197", 3, 2, new[] { 1, 2, 1, 0, 2, 0, 1, 1 }),
        new TileDef("terrain_199", 4, 2, new[] { 1, 1, 1, 0, 2, 0, 1, 1 }),
        new TileDef("terrain_209", 5, 2, new[] { 1, 0, 2, 0, 1, 2, 1, 1 }),
        new TileDef("terrain_213", 6, 2, new[] { 1, 2, 1, 2, 1, 2, 1, 1 }),
        new TileDef("terrain_215", 7, 2, new[] { 1, 1, 1, 2, 1, 2, 1, 1 }),
        new TileDef("terrain_221", 8, 2, new[] { 1, 2, 1, 1, 1, 2, 1, 1 }),
        new TileDef("terrain_223", 9, 2, new[] { 1, 1, 1, 1, 1, 2, 1, 1 }),
        new TileDef("terrain_241", 10, 2, new[] { 1, 0, 2, 0, 1, 1, 1, 1 }),
        new TileDef("terrain_245", 11, 2, new[] { 1, 2, 1, 2, 1, 1, 1, 1 }),
        new TileDef("terrain_247", 12, 2, new[] { 1, 1, 1, 2, 1, 1, 1, 1 }),
        new TileDef("terrain_253", 13, 2, new[] { 1, 2, 1, 1, 1, 1, 1, 1 }),
        new TileDef("terrain_255", 14, 2, new[] { 1, 1, 1, 1, 1, 1, 1, 1 }),
        new TileDef("terrain_fill_0", 0, 3, null),
        new TileDef("terrain_fill_1", 1, 3, null),
        new TileDef("terrain_fill_2", 2, 3, null),
        new TileDef("terrain_fill_3", 3, 3, null),
        new TileDef("terrain_fill_4", 4, 3, null),
        new TileDef("terrain_fill_5", 5, 3, null),
        new TileDef("terrain_top_0", 6, 3, null),
        new TileDef("terrain_top_1", 7, 3, null),
        new TileDef("terrain_top_2", 8, 3, null),
        new TileDef("terrain_top_3", 9, 3, null),
        new TileDef("terrain_ceiling_0", 10, 3, null),
        new TileDef("terrain_ceiling_1", 11, 3, null),
        new TileDef("terrain_leftwall_0", 12, 3, null),
        new TileDef("terrain_leftwall_1", 13, 3, null),
        new TileDef("terrain_rightwall_0", 14, 3, null),
        new TileDef("terrain_rightwall_1", 15, 3, null),
        new TileDef("platform_left", 0, 4, null),
        new TileDef("platform_mid_0", 1, 4, null),
        new TileDef("platform_mid_1", 2, 4, null),
        new TileDef("platform_right", 3, 4, null),
        new TileDef("platform_single", 4, 4, null),
        new TileDef("platform_cracked_0", 5, 4, null),
        new TileDef("backwall_0", 6, 4, null),
        new TileDef("backwall_1", 7, 4, null),
        new TileDef("backwall_2", 8, 4, null),
        new TileDef("backwall_3", 9, 4, null),
        new TileDef("backwall_4", 10, 4, null),
        new TileDef("backwall_5", 11, 4, null),
        new TileDef("decor_grass_a", 0, 5, null),
        new TileDef("decor_grass_b", 1, 5, null),
        new TileDef("decor_grass_c", 2, 5, null),
        new TileDef("decor_tallgrass", 3, 5, null),
        new TileDef("decor_pebbles", 4, 5, null),
        new TileDef("decor_rock", 5, 5, null),
        new TileDef("decor_mushroom", 6, 5, null),
        new TileDef("decor_flower", 7, 5, null),
        new TileDef("hang_stalactite_a", 8, 5, null),
        new TileDef("hang_stalactite_b", 9, 5, null),
        new TileDef("spike_up", 0, 6, null),
        new TileDef("spike_right", 1, 6, null),
        new TileDef("spike_down", 2, 6, null),
        new TileDef("spike_left", 3, 6, null),
    };

    // ------------------------------------------------------------------ menu
    [MenuItem("Tools/Pixel Art/Import Everything")]
    public static void ImportEverything()
    {
        BuildPlayerAnimations();
        BuildTileset();
        BuildTilePalette();
        BuildBackground();
        if (UnityEngine.Object.FindFirstObjectByType<PlayerController>() != null)
        {
            RewirePlayerInOpenScene();
            AddBackgroundToOpenScene();
            BuildBasicLevelInOpenScene();
        }
        else
            Debug.Log("[PixelArt] Open your Game scene and run Tools > Pixel Art > Rewire Player In Open Scene, then Add Parallax Background To Open Scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("[PixelArt] Done.");
    }

    [MenuItem("Tools/Pixel Art/Build Player Animations")]
    public static void BuildPlayerAnimations()
    {
        foreach (string sheet in States.Select(s => s.Sheet).Distinct())
        {
            SliceGrid($"{PlayerArt}/{sheet}_player.png", 16, 16, new Vector2(0.5f, 0f), $"{sheet}_player");
            // Sword layer frames are 80x72 with the player's 16x16 box at (32,32): feet pivot = (40, 24 above bottom).
            SliceGrid($"{PlayerArt}/{sheet}_sword_layer.png", 80, 72, new Vector2(0.5f, 24f / 72f), $"{sheet}_sword_layer");
        }
        AssetDatabase.Refresh();

        Directory.CreateDirectory(ClipDir);
        foreach (StateDef d in States)
        {
            SaveClip($"{ClipDir}/Body_{d.State}.anim", MakeClip($"Body_{d.State}", $"{PlayerArt}/{d.Sheet}_player.png", d));
            SaveClip($"{ClipDir}/Sword_{d.State}.anim", MakeClip($"Sword_{d.State}", $"{PlayerArt}/{d.Sheet}_sword_layer.png", d));
        }
        AssetDatabase.SaveAssets();

        RebuildController($"{AnimDir}/BodyController.controller", "Body");
        RebuildController($"{AnimDir}/SwordController.controller", "Sword");

        // Old clips from the previous character art
        foreach (string old in new[] { "Body_Idle", "Body_Run", "Sword_Idle", "Sword_Run", "Sword_Attack" })
        {
            string p = $"{AnimDir}/{old}.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(p) != null) AssetDatabase.DeleteAsset(p);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[PixelArt] Player sheets sliced, clips + controllers rebuilt.");
    }

    [MenuItem("Tools/Pixel Art/Build Tileset And Decor Tiles")]
    public static void BuildTileset()
    {
        string sheetPath = $"{TileArt}/tileset.png";
        if (!File.Exists(sheetPath)) { Debug.LogError("[PixelArt] Missing " + sheetPath); return; }

        // Slice the 8x8 sheet, named after the layout json
        ConfigureTexture(sheetPath, SpriteImportMode.Multiple);
        var imp = (TextureImporter)AssetImporter.GetAtPath(sheetPath);
        imp.GetSourceTextureWidthAndHeight(out int w, out int h);
        var rects = TileDefs.Select(t => (t.Name, new Rect(t.Col * 8, h - (t.Row + 1) * 8, 8, 8))).ToList();
        ApplySlices(sheetPath, rects, new Vector2(0.5f, 0.5f));

        Directory.CreateDirectory(TileOut);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToDictionary(s => s.name, s => s);

        // Terrain: 47-tile blob rule tile
        var rt = ScriptableObject.CreateInstance<RuleTile>();
        rt.m_DefaultSprite = sprites.ContainsKey("terrain_255") ? sprites["terrain_255"] : sprites.Values.First();
        rt.m_DefaultColliderType = Tile.ColliderType.Grid;
        rt.m_TilingRules = new List<RuleTile.TilingRule>();
        foreach (TileDef t in TileDefs.Where(t => t.Rule != null))
        {
            var r = new RuleTile.TilingRule();
            r.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single;
            r.m_Sprites = new[] { sprites[t.Name] };
            r.m_ColliderType = Tile.ColliderType.Grid;
            // Rule array is N, NE, E, SE, S, SW, W, NW. RuleTile's neighbour order is NW, N, NE, W, E, SW, S, SE.
            r.m_NeighborPositions = new List<Vector3Int>
            {
                new Vector3Int(-1, 1, 0), new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0),
                new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0),
                new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0),
            };
            int[] q = t.Rule;
            r.m_Neighbors = new List<int> { q[7], q[0], q[1], q[6], q[2], q[5], q[4], q[3] };
            rt.m_TilingRules.Add(r);
        }
        SaveAsset(rt, $"{TileOut}/Terrain_RuleTile.asset");

        // Everything else: plain tiles
        foreach (TileDef t in TileDefs.Where(t => t.Rule == null))
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprites[t.Name];
            tile.colliderType = t.Name.StartsWith("platform_") ? Tile.ColliderType.Grid
                              : t.Name.StartsWith("spike_") ? Tile.ColliderType.Sprite
                              : Tile.ColliderType.None;
            SaveAsset(tile, $"{TileOut}/{t.Name}.asset");
        }

        // Animated decor (sway strips: 4 frames of 8x8)
        if (Directory.Exists(DecorArt))
        {
            foreach (string f in Directory.GetFiles(DecorArt, "*_sway.png"))
            {
                string path = f.Replace('\\', '/');
                string baseName = Path.GetFileNameWithoutExtension(path);
                SliceGrid(path, 8, 8, new Vector2(0.5f, 0.5f), baseName);
                var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(SpriteIndex).ToArray();
                var at = ScriptableObject.CreateInstance<AnimatedTile>();
                at.m_AnimatedSprites = frames;
                at.m_MinSpeed = 3f;
                at.m_MaxSpeed = 5f;
                at.m_AnimationStartTime = 0f;
                at.m_TileColliderType = Tile.ColliderType.None;
                SaveAsset(at, $"{TileOut}/{baseName.Replace("_sway", "")}_animated.asset");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[PixelArt] Tileset built: " + TileOut);
    }

    [MenuItem("Tools/Pixel Art/Build Parallax Background Prefab")]
    public static void BuildBackground()
    {
        // name, parallax factor, sort order
        var layers = new (string name, float fx, float fy, int order)[]
        {
            ("sky", 1f, 1f, -100),
            ("clouds", 0.9f, 1f, -90),
            ("mountains_far", 0.8f, 1f, -80),
            ("mountains_mid", 0.6f, 1f, -70),
            ("forest", 0.4f, 1f, -60),
        };

        var root = new GameObject("ParallaxBackground");
        try
        {
            foreach (var l in layers)
            {
                string path = $"{BgArt}/{l.name}.png";
                if (!File.Exists(path)) { Debug.LogWarning("[PixelArt] Missing " + path); continue; }
                ConfigureTexture(path, SpriteImportMode.Single);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                float wUnits = sprite.rect.width / PPU, hUnits = sprite.rect.height / PPU;

                var go = new GameObject(l.name);
                go.transform.SetParent(root.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = l.order;
                var pl = go.AddComponent<ParallaxLayer>();
                pl.factorX = l.fx;
                pl.factorY = l.fy;
                if (l.name != "sky")
                {
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    sr.size = new Vector2(wUnits * 3f, hUnits);
                    pl.tileWidth = wUnits;
                }
            }
            string prefabPath = $"{BgArt}/ParallaxBackground.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[PixelArt] Parallax prefab saved: " + prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }


    [MenuItem("Tools/Pixel Art/Build Tile Palette")]
    public static void BuildTilePalette()
    {
        string[][] rows =
        {
            new[] { "Terrain_RuleTile", "platform_left", "platform_mid_0", "platform_mid_1", "platform_right", "platform_single", "platform_cracked_0" },
            new[] { "backwall_0", "backwall_1", "backwall_2", "backwall_3", "backwall_4", "backwall_5" },
            new[] { "decor_grass_a_animated", "decor_grass_b_animated", "decor_grass_c_animated", "decor_tallgrass_animated", "decor_flower_animated", "decor_pebbles", "decor_rock", "decor_mushroom" },
            new[] { "hang_stalactite_a", "hang_stalactite_b", "spike_up", "spike_right", "spike_down", "spike_left" },
        };

        string palName = "Level Tiles";
        string path = $"{TileArt}/{palName}.prefab";
        float cell = 8f / PPU;   // an 8x8 tile in world units
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        // Preferred: Unity's own palette creator (found by reflection so this compiles on any editor version)
        bool created = false;
        try
        {
            Type util = FindEditorType("GridPaletteUtility");
            MethodInfo m = util == null ? null : util
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "CreateNewPalette" && x.GetParameters().Length == 6);
            if (m != null)
            {
                ParameterInfo[] ps = m.GetParameters();
                m.Invoke(null, new object[]
                {
                    TileArt, palName, GridLayout.CellLayout.Rectangle,
                    Enum.ToObject(ps[3].ParameterType, 0),   // CellSizing.Automatic
                    new Vector3(cell, cell, 0f), GridLayout.CellSwizzle.XYZ,
                });
                AssetDatabase.Refresh();
                created = File.Exists(path);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PixelArt] Palette utility failed, using fallback: " + e.Message);
        }

        if (!created)
        {
            var root = new GameObject(palName);
            var g = root.AddComponent<Grid>();
            g.cellSize = new Vector3(cell, cell, 0f);
            var layer = new GameObject("Layer1");
            layer.transform.SetParent(root.transform, false);
            layer.AddComponent<Tilemap>();
            layer.AddComponent<TilemapRenderer>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        // Fill the palette
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var tm = contents.GetComponentInChildren<Tilemap>();
            if (tm == null)
            {
                var layer = new GameObject("Layer1");
                layer.transform.SetParent(contents.transform, false);
                tm = layer.AddComponent<Tilemap>();
                layer.AddComponent<TilemapRenderer>();
            }
            tm.ClearAllTiles();
            int placed = 0;
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileOut}/{rows[r][c]}.asset");
                    if (tile == null) { Debug.LogWarning("[PixelArt] Palette: missing tile " + rows[r][c]); continue; }
                    tm.SetTile(new Vector3Int(c, -r, 0), tile);
                    placed++;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            Debug.Log($"[PixelArt] Tile palette saved with {placed} tiles: {path}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // Make sure Unity treats the prefab as a palette (needs a GridPalette sub-asset)
        Type gpType = FindEditorType("GridPalette");
        if (gpType != null && typeof(ScriptableObject).IsAssignableFrom(gpType)
            && !AssetDatabase.LoadAllAssetsAtPath(path).Any(o => o != null && o.GetType() == gpType))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var gp = ScriptableObject.CreateInstance(gpType);
            gp.name = "Palette Settings";
            AssetDatabase.AddObjectToAsset(gp, prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
        }
    }

    [MenuItem("Tools/Pixel Art/Add Parallax Background To Open Scene")]
    public static void AddBackgroundToOpenScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BgArt}/ParallaxBackground.prefab");
        if (prefab == null)
        {
            BuildBackground();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BgArt}/ParallaxBackground.prefab");
        }
        if (prefab == null) { Debug.LogError("[PixelArt] ParallaxBackground prefab missing."); return; }
        if (UnityEngine.Object.FindFirstObjectByType<ParallaxLayer>() != null)
        {
            Debug.Log("[PixelArt] Scene already has a parallax background; skipping.");
            return;
        }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(inst, "Add parallax background");
        Camera cam = Camera.main;
        if (cam != null) inst.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        EditorSceneManager.MarkSceneDirty(inst.scene);
        EditorSceneManager.SaveScene(inst.scene);
        Debug.Log("[PixelArt] Parallax background added to the scene.");
    }

    static Type FindEditorType(string name)
    {
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = a.GetTypes(); } catch { continue; }
            foreach (Type t in types)
                if (t.Name == name && t.Namespace != null && t.Namespace.StartsWith("UnityEditor")) return t;
        }
        return null;
    }


    [MenuItem("Tools/Pixel Art/Build Basic Level In Open Scene")]
    public static void BuildBasicLevelInOpenScene()
    {
        TileBase T(string n) => AssetDatabase.LoadAssetAtPath<TileBase>($"{TileOut}/{n}.asset");
        TileBase terrain = T("Terrain_RuleTile");
        if (terrain == null) { Debug.LogError("[PixelArt] Build the tileset first (Tools > Pixel Art > Build Tileset And Decor Tiles)."); return; }

        // The main level: a Grid at the scene root with a Tilemap child. (Grids parented under moving platforms are left alone.)
        Tilemap ground = null;
        foreach (Tilemap tm in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            Grid g = tm.transform.parent != null ? tm.transform.parent.GetComponent<Grid>() : null;
            if (g != null && g.transform.parent == null) { ground = tm; break; }
        }
        if (ground == null)
        {
            var gridGo = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridGo, "Create level grid");
            gridGo.AddComponent<Grid>();
            var tmGo = new GameObject("Tilemap");
            tmGo.transform.SetParent(gridGo.transform, false);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) tmGo.layer = groundLayer;
            ground = tmGo.AddComponent<Tilemap>();
            tmGo.AddComponent<TilemapRenderer>();
            tmGo.AddComponent<TilemapCollider2D>();
        }

        Grid grid = ground.transform.parent.GetComponent<Grid>();
        Undo.RecordObject(grid, "Build basic level");
        float cell = 8f / PPU;
        grid.cellSize = new Vector3(cell, cell, 0f);
        Undo.RecordObject(ground, "Build basic level");
        ground.ClearAllTiles();

        void Fill(TileBase tile, int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    ground.SetTile(new Vector3Int(x, y, 0), tile);
        }

        // Cell (x, y) covers world [x*0.5, (x+1)*0.5]. The floor surface is at world y = -1, just under the player start.
        Fill(terrain, -44, 83, -8, -3);                 // floor
        Fill(terrain, -44, -41, -2, 12);                // left wall
        Fill(terrain, 80, 83, -2, 12);                  // right wall

        // Spike pit (3 units wide)
        for (int x = 36; x <= 41; x++)
            for (int y = -5; y <= -3; y++)
                ground.SetTile(new Vector3Int(x, y, 0), null);
        TileBase spike = T("spike_up");
        if (spike != null)
            for (int x = 36; x <= 41; x++) ground.SetTile(new Vector3Int(x, -5, 0), spike);

        // Stairs up to a block
        Fill(terrain, 18, 25, -2, -1);
        Fill(terrain, 22, 25, 0, 1);

        // Floating island and one-way style platform row
        Fill(terrain, 4, 11, 2, 3);
        TileBase pl = T("platform_left"), pm0 = T("platform_mid_0"), pm1 = T("platform_mid_1"), pr = T("platform_right");
        if (pl != null && pm0 != null && pm1 != null && pr != null)
        {
            ground.SetTile(new Vector3Int(14, 6, 0), pl);
            for (int x = 15; x <= 18; x++) ground.SetTile(new Vector3Int(x, 6, 0), (x % 2 == 0) ? pm0 : pm1);
            ground.SetTile(new Vector3Int(19, 6, 0), pr);
        }

        // Far side: step and tower
        Fill(terrain, 52, 55, -2, -1);
        Fill(terrain, 56, 61, -2, 3);

        // Decor (no collider): animated grass, flowers and a few static props on every exposed floor top
        Transform decorT = grid.transform.Find("Decor");
        if (decorT == null)
        {
            var d = new GameObject("Decor");
            Undo.RegisterCreatedObjectUndo(d, "Create decor tilemap");
            d.transform.SetParent(grid.transform, false);
            d.AddComponent<Tilemap>();
            d.AddComponent<TilemapRenderer>().sortingOrder = -1;
            decorT = d.transform;
        }
        Tilemap decor = decorT.GetComponent<Tilemap>();
        decor.ClearAllTiles();
        TileBase[] grass = { T("decor_grass_a_animated"), T("decor_grass_b_animated"), T("decor_grass_c_animated") };
        TileBase tall = T("decor_tallgrass_animated"), flower = T("decor_flower_animated");
        TileBase[] props = { T("decor_pebbles"), T("decor_rock"), T("decor_mushroom") };
        var rng = new System.Random(7);
        for (int x = -40; x <= 79; x++)
        {
            int top = int.MinValue;
            for (int y = 12; y >= -8; y--)
                if (ground.HasTile(new Vector3Int(x, y, 0))) { top = y; break; }
            if (top == int.MinValue) continue;
            if (ground.GetTile(new Vector3Int(x, top, 0)) != terrain) continue;   // skip spikes and platforms
            if (rng.NextDouble() > 0.6) continue;
            double roll = rng.NextDouble();
            TileBase pick = roll < 0.55 ? grass[rng.Next(grass.Length)]
                          : roll < 0.70 ? tall
                          : roll < 0.82 ? flower
                          : props[rng.Next(props.Length)];
            if (pick != null) decor.SetTile(new Vector3Int(x, top + 1, 0), pick);
        }

        EditorUtility.SetDirty(ground);
        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        EditorSceneManager.SaveScene(grid.gameObject.scene);
        Debug.Log("[PixelArt] Basic level built and scene saved.");
    }

    [MenuItem("Tools/Pixel Art/Rewire Player In Open Scene")]
    public static void RewirePlayerInOpenScene()
    {
        var pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (pc == null)
        {
            EditorUtility.DisplayDialog("Pixel Art", "No PlayerController found in the open scene. Open your Game scene first.", "OK");
            return;
        }

        var so = new SerializedObject(pc);
        var bodyAnim = so.FindProperty("bodyAnim").objectReferenceValue as Animator;
        var oldSwordAnim = so.FindProperty("swordAnim").objectReferenceValue as Animator;
        var visuals = so.FindProperty("visualsGroup").objectReferenceValue as Transform;
        if (bodyAnim == null || visuals == null)
        {
            EditorUtility.DisplayDialog("Pixel Art", "PlayerController needs bodyAnim and visualsGroup assigned.", "OK");
            return;
        }

        var bodyCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimDir}/BodyController.controller");
        var swordCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimDir}/SwordController.controller");
        var idleBody = AssetDatabase.LoadAllAssetsAtPath($"{PlayerArt}/idle_player.png").OfType<Sprite>().OrderBy(SpriteIndex).FirstOrDefault();
        var idleSword = AssetDatabase.LoadAllAssetsAtPath($"{PlayerArt}/idle_sword_layer.png").OfType<Sprite>().OrderBy(SpriteIndex).FirstOrDefault();
        if (bodyCtrl == null || swordCtrl == null || idleBody == null)
        {
            EditorUtility.DisplayDialog("Pixel Art", "Run Tools > Pixel Art > Build Player Animations first.", "OK");
            return;
        }

        // Feet sit on the bottom of the player's collider
        float feetY = -0.71f;
        var col = pc.GetComponent<BoxCollider2D>();
        if (col != null) feetY = col.offset.y - col.size.y * 0.5f;

        // Body
        var bodySR = bodyAnim.GetComponent<SpriteRenderer>();
        Undo.RecordObject(bodyAnim.transform, "Rewire player");
        Undo.RecordObject(bodySR, "Rewire player");
        bodyAnim.transform.localPosition = new Vector3(0f, feetY, 0f);
        bodyAnim.transform.localRotation = Quaternion.identity;
        bodyAnim.transform.localScale = Vector3.one;
        bodySR.sprite = idleBody;
        bodyAnim.runtimeAnimatorController = bodyCtrl;
        bodyAnim.applyRootMotion = false;

        // Sword layer: sits in the same Visuals group so it flips with the body and never rotates
        Transform swordT = visuals.Find("SwordLayer");
        if (swordT == null)
        {
            var go = new GameObject("SwordLayer");
            Undo.RegisterCreatedObjectUndo(go, "Create SwordLayer");
            go.transform.SetParent(visuals, false);
            swordT = go.transform;
        }
        var swordSR = swordT.GetComponent<SpriteRenderer>();
        if (swordSR == null) swordSR = swordT.gameObject.AddComponent<SpriteRenderer>();
        var swordAnimNew = swordT.GetComponent<Animator>();
        if (swordAnimNew == null) swordAnimNew = swordT.gameObject.AddComponent<Animator>();
        swordT.localPosition = new Vector3(0f, feetY, 0f);
        swordT.localRotation = Quaternion.identity;
        swordT.localScale = Vector3.one;
        swordSR.sharedMaterial = bodySR.sharedMaterial;
        swordSR.sortingLayerID = bodySR.sortingLayerID;
        swordSR.sortingOrder = bodySR.sortingOrder;       // sword layer is drawn behind the body
        bodySR.sortingOrder = bodySR.sortingOrder + 1;
        swordSR.sprite = idleSword;
        swordAnimNew.runtimeAnimatorController = swordCtrl;
        swordAnimNew.applyRootMotion = false;

        // Retire the old rotating sword arm and its particle visuals
        if (oldSwordAnim != null && oldSwordAnim != swordAnimNew) oldSwordAnim.gameObject.SetActive(false);
        if (pc.attackPoint != null)
        {
            foreach (Transform t in pc.attackPoint.GetComponentsInChildren<Transform>(true))
                if (t.name == "SlashParticle" || t.name == "SlashParticleVisual") t.gameObject.SetActive(false);
        }

        // Animation driver
        var driver = pc.GetComponent<PlayerAnimationDriver>();
        if (driver == null) driver = Undo.AddComponent<PlayerAnimationDriver>(pc.gameObject);
        var dso = new SerializedObject(driver);
        dso.FindProperty("bodyAnim").objectReferenceValue = bodyAnim;
        dso.FindProperty("swordAnim").objectReferenceValue = swordAnimNew;
        dso.ApplyModifiedProperties();

        so.FindProperty("swordAnim").objectReferenceValue = swordAnimNew;
        var driverProp = so.FindProperty("animDriver");
        if (driverProp != null) driverProp.objectReferenceValue = driver;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(bodySR);
        EditorUtility.SetDirty(swordSR);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        EditorSceneManager.SaveScene(pc.gameObject.scene);
        Debug.Log("[PixelArt] Player rewired and scene saved.");
    }

    // ------------------------------------------------------------------ helpers
    static int SpriteIndex(Sprite s)
    {
        int i = s.name.LastIndexOf('_');
        return int.TryParse(s.name.Substring(i + 1), out int n) ? n : 0;
    }

    static void ConfigureTexture(string path, SpriteImportMode mode)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp == null) { Debug.LogError("[PixelArt] Not imported yet: " + path); return; }
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
        var ts = new TextureImporterSettings();
        imp.ReadTextureSettings(ts);
        ts.textureType = TextureImporterType.Sprite;
        ts.spriteMode = (int)mode;
        ts.spritePixelsPerUnit = PPU;
        ts.filterMode = FilterMode.Point;
        ts.wrapMode = TextureWrapMode.Clamp;
        ts.mipmapEnabled = false;
        ts.alphaIsTransparency = true;
        ts.readable = false;
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

    static void SliceGrid(string path, int fw, int fh, Vector2 pivot, string namePrefix)
    {
        if (!File.Exists(path)) { Debug.LogWarning("[PixelArt] Missing " + path); return; }
        ConfigureTexture(path, SpriteImportMode.Multiple);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.GetSourceTextureWidthAndHeight(out int w, out int h);
        var rects = new List<(string, Rect)>();
        int cols = w / fw, rows = h / fh;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                rects.Add(($"{namePrefix}_{r * cols + c}", new Rect(c * fw, h - (r + 1) * fh, fw, fh)));
        ApplySlices(path, rects, pivot);
    }

    static void ApplySlices(string path, List<(string name, Rect rect)> rects, Vector2 pivot)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();

        // Reuse existing sprite IDs so animation clips keep their references when re-sliced
        var existing = dp.GetSpriteRects().ToDictionary(s => s.name, s => s.spriteID);
        var list = new List<SpriteRect>();
        foreach (var (name, rect) in rects)
        {
            list.Add(new SpriteRect
            {
                name = name,
                rect = rect,
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = existing.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
            });
        }
        dp.SetSpriteRects(list.ToArray());
        var nameProvider = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameProvider.SetNameFileIdPairs(list.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)).ToList());
        dp.Apply();
        imp.SaveAndReimport();
    }

    static AnimationClip MakeClip(string name, string sheetPath, StateDef d)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().OrderBy(SpriteIndex).ToArray();
        var frames = d.Frames.Select(i => sprites[i]).ToArray();
        var clip = new AnimationClip { name = name, frameRate = d.Fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / d.Fps, value = frames[i] };
        // Closing key so the last frame is shown for a full frame and the clip length is exactly n / fps
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / d.Fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = d.Loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    static void SaveClip(string path, AnimationClip clip) { SaveAsset(clip, path); }

    static void SaveAsset<T>(T obj, string path) where T : UnityEngine.Object
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        obj.name = Path.GetFileNameWithoutExtension(path);
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(obj, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(obj);
        }
        else
        {
            AssetDatabase.CreateAsset(obj, path);
        }
    }

    static void RebuildController(string path, string prefix)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var cs in sm.states.ToArray()) sm.RemoveState(cs.state);
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
        foreach (var t in sm.entryTransitions.ToArray()) sm.RemoveEntryTransition(t);

        AnimatorState idle = null;
        for (int i = 0; i < States.Length; i++)
        {
            StateDef d = States[i];
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipDir}/{prefix}_{d.State}.anim");
            var st = sm.AddState(d.State, new Vector3(250f + 250f * (i % 4), 60f + 60f * (i / 4), 0f));
            st.motion = clip;
            st.writeDefaultValues = false;
            if (d.State == "Idle") idle = st;
        }
        sm.defaultState = idle;
        EditorUtility.SetDirty(ctrl);
    }
}
