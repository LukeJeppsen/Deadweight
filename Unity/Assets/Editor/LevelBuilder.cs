using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds a playable scene for each area from its level file (Assets/Levels/&lt;id&gt;.json).
/// Everything is generated, so re-running replaces the scenes cleanly.
/// </summary>
public static class LevelBuilder
{
    public const string LevelsDir = "Assets/Levels";
    public const string ScenesDir = "Assets/Scenes/Levels";
    public const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    public const string FontPath = "Assets/Art/Story/font_5x7.png";
    public const string PanelPath = "Assets/Art/Menu/panel.png";
    public const string SlipperyGuid = "3ac386740c6a7804896b71a4b75dd627";
    const float Cell = 0.5f;

    // ---------------------------------------------------------------- style per area

    class PropRule
    {
        public string[] sprites;
        public float chance;
        public string mode;          // floor, ceiling, air
        public int order = -2;
        public bool animate;
        public float fps = 6f;
        public int minGap = 0;       // cells between two of these
        public PropRule(string mode, float chance, params string[] sprites) { this.mode = mode; this.chance = chance; this.sprites = sprites; }
    }

    class Style
    {
        public string[] floorDecor = new string[0];
        public float floorDecorChance = 0.08f;
        public string[] ceilingDecor = new string[0];
        public float ceilingDecorChance = 0.05f;
        public List<PropRule> props = new List<PropRule>();
    }

    static Style StyleFor(int index)
    {
        Style s = new Style();
        switch (index)
        {
            case 0:
                s.floorDecor = new[] { "decor_pebbles", "decor_rock", "decor_mushroom" }; s.floorDecorChance = 0.07f;
                s.ceilingDecor = new[] { "hang_stalactite_a", "hang_stalactite_b" }; s.ceilingDecorChance = 0.05f;
                s.props.Add(new PropRule("floor", 0.012f, "bones_a", "bones_b") { minGap = 8 });
                s.props.Add(new PropRule("floor", 0.008f, "pillar_b") { minGap = 12 });
                s.props.Add(new PropRule("ceiling", 0.012f, "banner_0", "banner_1") { animate = true, fps = 3f, minGap = 10 });
                break;
            case 1:
                s.floorDecor = new[] { "decor_pebbles", "decor_rock", "decor_mushroom" }; s.floorDecorChance = 0.08f;
                s.ceilingDecor = new[] { "hang_stalactite_a", "hang_stalactite_b" }; s.ceilingDecorChance = 0.07f;
                s.props.Add(new PropRule("ceiling", 0.05f, "drop_0") { animate = true, fps = 5f, minGap = 4 });
                s.props.Add(new PropRule("ceiling", 0.03f, "pipe_0") { minGap = 6 });
                s.props.Add(new PropRule("ceiling", 0.02f, "vine_a") { minGap = 6 });
                s.props.Add(new PropRule("floor", 0.012f, "bones_a") { minGap = 10 });
                s.props.Add(new PropRule("floor", 0.01f, "pillar_b") { minGap = 12 });
                break;
            case 2:
                s.floorDecor = new[] { "decor_grass_a", "decor_grass_b", "decor_grass_c", "decor_tallgrass", "decor_flower", "decor_mushroom" }; s.floorDecorChance = 0.22f;
                s.ceilingDecor = new string[0];
                s.props.Add(new PropRule("ceiling", 0.10f, "vine_a", "vine_b", "vine_c") { minGap = 2 });
                s.props.Add(new PropRule("floor", 0.012f, "bones_b") { minGap = 10 });
                break;
            case 3:
                s.floorDecor = new[] { "decor_pebbles", "decor_rock" }; s.floorDecorChance = 0.05f;
                s.ceilingDecor = new[] { "hang_stalactite_a", "hang_stalactite_b" }; s.ceilingDecorChance = 0.10f;
                s.props.Add(new PropRule("floor", 0.14f, "snow_pile") { minGap = 3 });
                break;
            case 4:
                s.floorDecor = new[] { "decor_rock", "decor_pebbles" }; s.floorDecorChance = 0.08f;
                s.ceilingDecor = new[] { "hang_stalactite_a", "hang_stalactite_b" }; s.ceilingDecorChance = 0.04f;
                s.props.Add(new PropRule("ceiling", 0.05f, "chain_a", "chain_b") { minGap = 4 });
                s.props.Add(new PropRule("ceiling", 0.03f, "pipe_0") { minGap = 6 });
                s.props.Add(new PropRule("air", 0.0008f, "gear_s_0") { animate = true, fps = 6f, order = -3, minGap = 16 });
                s.props.Add(new PropRule("air", 0.0005f, "gear_l_0") { animate = true, fps = 4f, order = -3, minGap = 24 });
                s.props.Add(new PropRule("floor", 0.01f, "bones_a") { minGap = 12 });
                break;
            default:
                s.floorDecor = new[] { "decor_pebbles", "decor_rock" }; s.floorDecorChance = 0.04f;
                s.props.Add(new PropRule("ceiling", 0.03f, "banner_0", "banner_1") { animate = true, fps = 3f, minGap = 8 });
                s.props.Add(new PropRule("floor", 0.025f, "pillar_a", "pillar_b") { minGap = 8 });
                break;
        }
        return s;
    }

    // ---------------------------------------------------------------- entry points

    public static void BuildAll()
    {
        for (int i = 0; i < LevelCatalog.Count; i++) BuildLevel(i);
    }

    public static string ScenePath(int index) { return ScenesDir + "/" + LevelCatalog.All[index].SceneName + ".unity"; }

    public static LevelData LoadLevel(string id)
    {
        TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelsDir + "/" + id + ".json");
        if (ta == null) return null;
        return JsonUtility.FromJson<LevelData>(ta.text);
    }

    // ---------------------------------------------------------------- build one level

    class Ctx
    {
        public int index;
        public LevelInfo info;
        public LevelData data;
        public string areaDir;
        public Dictionary<string, Sprite> props;
        public System.Random rng;
        public bool[,] reserved;        // [col,row-from-top]
        public Transform root;
        public Transform grid;
        public int groundLayer;
        public PhysicsMaterial2D slippery;
    }

    public static void BuildLevel(int index)
    {
        LevelInfo info = LevelCatalog.All[index];
        LevelData data = LoadLevel(info.id);
        if (data == null) { Debug.LogError("[Deadweight] Level file missing: " + LevelsDir + "/" + info.id + ".json"); return; }
        if (!File.Exists(PlayerPrefabPath)) { Debug.LogError("[Deadweight] Player prefab missing - run Tools > Deadweight > Build Everything."); return; }

        Directory.CreateDirectory(ScenesDir);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Ctx c = new Ctx();
        c.index = index; c.info = info; c.data = data;
        c.areaDir = AreaArt.AreasDir + "/" + info.id;
        c.props = AreaArt.SpritesAt(c.areaDir + "/props.png");
        c.rng = new System.Random(index * 7919 + 13);
        c.groundLayer = LayerMask.NameToLayer("Ground");
        if (c.groundLayer < 0) { c.groundLayer = 6; Debug.LogWarning("[Deadweight] No 'Ground' layer found; using layer 6."); }
        string slipPath = AssetDatabase.GUIDToAssetPath(SlipperyGuid);
        c.slippery = string.IsNullOrEmpty(slipPath) ? null : AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(slipPath);
        c.reserved = new bool[data.cols, data.Rows];

        GameObject rootGo = new GameObject("Level");
        c.root = rootGo.transform;

        // ---- find the special glyphs first (spawn, checkpoints, exit, altar)
        Vector2 spawnWorld = Vector2.zero;
        for (int r = 0; r < data.Rows; r++)
            for (int col = 0; col < data.cols; col++)
            {
                char ch = data.At(col, r);
                if (ch == 'P') spawnWorld = BottomCenter(data, col, r);
            }

        GameObject player = AddPlayer(c, spawnWorld);
        AddLighting(c);
        Transform camTarget = AddCamera(c, player.transform, spawnWorld);
        AddBackground(c);
        AddTerrain(c);
        AddGlyphObjects(c);
        AddObjects(c);
        AddScenery(c);
        AddKillZone(c);
        AddAssetsAndDirector(c, player.GetComponent<PlayerController>());

        if (index == 0)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            IntroCutscene intro = StoryCutsceneBuilder.AddToScene(pc);
            if (intro != null) { intro.playEveryTime = false; intro.dropHeight = 14f; }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath(index));
        Debug.Log("[Deadweight] Built " + ScenePath(index));
    }

    // ---------------------------------------------------------------- helpers

    static Vector2 BottomCenter(LevelData d, int col, int rowFromTop)
    {
        return new Vector2((col + 0.5f) * Cell, (d.Rows - 1 - rowFromTop) * Cell);
    }

    static Vector2 CellCenter(LevelData d, int col, int rowFromTop)
    {
        return new Vector2((col + 0.5f) * Cell, (d.Rows - 1 - rowFromTop + 0.5f) * Cell);
    }

    static Sprite Spr(Ctx c, string name)
    {
        Sprite s;
        if (c.props != null && c.props.TryGetValue(name, out s)) return s;
        Debug.LogWarning("[Deadweight] Missing prop sprite '" + name + "' in " + c.areaDir + "/props.png");
        return null;
    }

    static Sprite[] SprList(Ctx c, string prefix, int count)
    {
        List<Sprite> l = new List<Sprite>();
        for (int i = 0; i < count; i++) { Sprite s; if (c.props.TryGetValue(prefix + "_" + i, out s)) l.Add(s); }
        return l.ToArray();
    }

    static GameObject NewChild(Transform parent, string name, Vector3 pos)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.position = pos;
        return g;
    }

    static SpriteRenderer AddSprite(GameObject g, Sprite s, int order)
    {
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        return sr;
    }

    static void Reserve(Ctx c, int col, int row, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                int x = col + dx, y = row + dy;
                if (x >= 0 && x < c.data.cols && y >= 0 && y < c.data.Rows) c.reserved[x, y] = true;
            }
    }

    // ---------------------------------------------------------------- player, light, camera, background

    static GameObject AddPlayer(Ctx c, Vector2 spawn)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject p = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        p.name = "Player";
        p.transform.position = new Vector3(spawn.x, spawn.y + 0.8f, 0f);
        return p;
    }

    static void AddLighting(Ctx c)
    {
        GameObject go = new GameObject("Global Light 2D");
        Light2D l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Global;
        l.intensity = 1f;
        l.color = Color.white;
    }

    static Component AddByName(GameObject go, string typeName)
    {
        Type t = Type.GetType(typeName);
        if (t == null) { Debug.LogWarning("[Deadweight] Type not found: " + typeName); return null; }
        return go.AddComponent(t);
    }

    static void SetProp(Component comp, string path, Action<SerializedProperty> setter)
    {
        if (comp == null) return;
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(path);
        if (p == null) { Debug.LogWarning("[Deadweight] Property not found: " + comp.GetType().Name + "." + path); return; }
        setter(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform AddCamera(Ctx c, Transform player, Vector2 spawn)
    {
        LevelData d = c.data;
        Rect bounds = new Rect(0f, 0f, d.cols * Cell, d.Rows * Cell);
        Vector2 half = new Vector2(10f, 5.625f);
        Vector2 start = new Vector2(spawn.x, spawn.y + 0.8f + 1.4f);
        start.x = bounds.width < half.x * 2f ? bounds.center.x : Mathf.Clamp(start.x, bounds.xMin + half.x, bounds.xMax - half.x);
        start.y = bounds.height < half.y * 2f ? bounds.center.y : Mathf.Clamp(start.y, bounds.yMin + half.y, bounds.yMax - half.y);

        // follow target
        GameObject ctGo = new GameObject("CameraTarget");
        ctGo.transform.SetParent(c.root, false);
        ctGo.transform.position = new Vector3(start.x, start.y, 0f);
        CameraTarget ct = ctGo.AddComponent<CameraTarget>();
        ct.follow = player;
        ct.bounds = bounds;
        ct.viewHalfSize = half;

        // main camera
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(start.x, start.y, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = LevelCatalog.ParseHex(c.info.palette[0]);
        camGo.AddComponent<AudioListener>();
        PixelPerfectCamera ppc = camGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = 16;
        ppc.refResolutionX = 320;
        ppc.refResolutionY = 180;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.None;
        AddByName(camGo, "Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");

        // cinemachine camera that follows the target
        GameObject cmGo = new GameObject("CinemachineCamera");
        cmGo.transform.position = new Vector3(start.x, start.y, -10f);
        Component cm = AddByName(cmGo, "Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        Component follow = AddByName(cmGo, "Unity.Cinemachine.CinemachineFollow, Unity.Cinemachine");
        AddByName(cmGo, "Unity.Cinemachine.CinemachinePixelPerfect, Unity.Cinemachine");
        SetProp(cm, "Target.TrackingTarget", delegate (SerializedProperty p) { p.objectReferenceValue = ctGo.transform; });
        SetProp(cm, "Lens.OrthographicSize", delegate (SerializedProperty p) { p.floatValue = 5.625f; });
        SetProp(follow, "FollowOffset", delegate (SerializedProperty p) { p.vector3Value = new Vector3(0f, 0f, -10f); });
        SetProp(follow, "TrackerSettings.PositionDamping", delegate (SerializedProperty p) { p.vector3Value = new Vector3(0.25f, 0.35f, 0f); });
        return ctGo.transform;
    }

    static void AddBackground(Ctx c)
    {
        GameObject bg = new GameObject("Background");
        string[] layer = { "sky", "aux", "far", "mid", "near" };
        float[] fx = { 1f, 0.92f, 0.8f, 0.62f, 0.42f };
        int[] order = { -100, -90, -80, -70, -60 };
        for (int i = 0; i < layer.Length; i++)
        {
            Sprite s = AreaArt.SingleSprite(c.areaDir + "/bg_" + layer[i] + ".png");
            if (s == null) { Debug.LogWarning("[Deadweight] Missing background " + layer[i] + " for " + c.info.id); continue; }
            GameObject g = new GameObject(layer[i]);
            g.transform.SetParent(bg.transform, false);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(60f, 11.25f);
            sr.sortingOrder = order[i];
            ParallaxLayer pl = g.AddComponent<ParallaxLayer>();
            pl.factorX = fx[i];
            pl.factorY = 1f;
            pl.tileWidth = 20f;
        }
    }

    // ---------------------------------------------------------------- terrain

    static Tilemap NewTilemap(Ctx c, string name, int order, int layer)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(c.grid, false);
        go.layer = layer;
        Tilemap tm = go.AddComponent<Tilemap>();
        TilemapRenderer tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = order;
        return tm;
    }

    static TilemapCollider2D AddCollider(Tilemap tm, bool trigger, PhysicsMaterial2D mat)
    {
        TilemapCollider2D col = tm.gameObject.AddComponent<TilemapCollider2D>();
        col.isTrigger = trigger;
        if (mat != null) col.sharedMaterial = mat;
        return col;
    }

    static void MergeColliders(GameObject go, PhysicsMaterial2D mat)
    {
        TilemapCollider2D tc = go.GetComponent<TilemapCollider2D>();
        if (tc == null) return;
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D cc = go.GetComponent<CompositeCollider2D>();
        if (cc == null) cc = go.AddComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
        cc.generationType = CompositeCollider2D.GenerationType.Synchronous;
        if (mat != null) cc.sharedMaterial = mat;
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        cc.GenerateGeometry();
    }

    static void AddTerrain(Ctx c)
    {
        LevelData d = c.data;
        string id = c.info.id;
        GameObject gridGo = new GameObject("Grid");
        gridGo.transform.SetParent(c.root, false);
        Grid grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(Cell, Cell, 0f);
        c.grid = gridGo.transform;

        TileBase terrain = AreaArt.LoadTile(id, "Terrain_RuleTile");
        Tilemap tMass = NewTilemap(c, "Mass", -2, 0);
        Tilemap tRock = NewTilemap(c, "Rock", -1, 0);                       // rock art (rule tile), no collider
        Tilemap tCol = NewTilemap(c, "Collision", 0, c.groundLayer);        // invisible collider, only where the player can reach
        tCol.GetComponent<TilemapRenderer>().enabled = false;
        string colPath = AreaArt.TileDir(id) + "/collision_tile.asset";
        Tile colTile = AssetDatabase.LoadAssetAtPath<Tile>(colPath);
        if (colTile == null)
        {
            colTile = ScriptableObject.CreateInstance<Tile>();
            colTile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(colTile, colPath);
        }
        List<Vector3Int> pR = new List<Vector3Int>(); List<TileBase> tR = new List<TileBase>();
        List<Vector3Int> pC = new List<Vector3Int>(); List<TileBase> tC = new List<TileBase>();
        Tilemap tTerrain = NewTilemap(c, "Terrain", 0, c.groundLayer);
        Tilemap tBreak = NewTilemap(c, "Breakables", 0, c.groundLayer);
        Tilemap tGhost = NewTilemap(c, "Ghosts", 0, c.groundLayer);
        Tilemap tHaz = NewTilemap(c, "Hazards", 1, 0);
        Tilemap tDecor = NewTilemap(c, "Decor", -1, 0);

        List<Vector3Int> pM = new List<Vector3Int>(); List<TileBase> tM = new List<TileBase>();
        bool[,] near = ComputeNear(d, 3);
        bool[,] nearView = ComputeNear(d, 26);   // the camera never sees deeper than this, so deep rock stays empty (cheap)
        TileBase[] backwalls = new TileBase[6];
        for (int i = 0; i < 6; i++) backwalls[i] = AreaArt.LoadTile(id, "backwall_" + i);
        List<Vector3Int> pT = new List<Vector3Int>(); List<TileBase> tT = new List<TileBase>();
        List<Vector3Int> pB = new List<Vector3Int>(); List<TileBase> tB = new List<TileBase>();
        List<Vector3Int> pG = new List<Vector3Int>(); List<TileBase> tG = new List<TileBase>();
        List<Vector3Int> pH = new List<Vector3Int>(); List<TileBase> tH = new List<TileBase>();

        bool hasBreak = false, hasGhost = false, hasSpike = false;
        for (int r = 0; r < d.Rows; r++)
        {
            for (int col = 0; col < d.cols; col++)
            {
                char ch = d.At(col, r);
                Vector3Int cell = new Vector3Int(col, d.Rows - 1 - r, 0);
                switch (ch)
                {
                    case '#':
                    {
                        // every rock cell gets the dark back wall; only the rim next to open space is real terrain with a collider
                        int h = (col * 73856093) ^ (r * 19349663);
                        int pick = (h & 0xFF) < 40 ? 1 + ((h >> 8) & 0xFF) % 5 : 0;
                        if (nearView[col, r] && backwalls[pick] != null) { pM.Add(cell); tM.Add(backwalls[pick]); }
                        if (nearView[col, r]) { pR.Add(cell); tR.Add(terrain); }
                        if (near[col, r]) { pC.Add(cell); tC.Add(colTile); }
                        break;
                    }
                    case '=':
                    {
                        bool l = d.At(col - 1, r) == '=', rr = d.At(col + 1, r) == '=';
                        string n = (!l && !rr) ? "platform_single" : (!l ? "platform_left" : (!rr ? "platform_right" : ((col % 2 == 0) ? "platform_mid_0" : "platform_mid_1")));
                        pT.Add(cell); tT.Add(AreaArt.LoadTile(id, n));
                        break;
                    }
                    case 'b': pB.Add(cell); tB.Add(AreaArt.LoadTile(id, "brk_" + ((col * 7 + r * 13) % 3))); hasBreak = true; break;
                    case 'g': pG.Add(cell); tG.Add(AreaArt.LoadTile(id, "ghost_" + ((col + r) % 2))); hasGhost = true; break;
                    case '^': pH.Add(cell); tH.Add(AreaArt.LoadTile(id, "spike_up")); hasSpike = true; break;
                    case 'v': pH.Add(cell); tH.Add(AreaArt.LoadTile(id, "spike_down")); hasSpike = true; break;
                    case '<': pH.Add(cell); tH.Add(AreaArt.LoadTile(id, "spike_left")); hasSpike = true; break;
                    case '>': pH.Add(cell); tH.Add(AreaArt.LoadTile(id, "spike_right")); hasSpike = true; break;
                }
            }
        }
        tMass.SetTiles(pM.ToArray(), tM.ToArray());
        tRock.SetTiles(pR.ToArray(), tR.ToArray());
        tCol.SetTiles(pC.ToArray(), tC.ToArray());
        tTerrain.SetTiles(pT.ToArray(), tT.ToArray());
        tBreak.SetTiles(pB.ToArray(), tB.ToArray());
        tGhost.SetTiles(pG.ToArray(), tG.ToArray());
        tHaz.SetTiles(pH.ToArray(), tH.ToArray());

        AddCollider(tTerrain, false, c.slippery);
        AddCollider(tCol, false, c.slippery);
        // merge the per-tile boxes into one smooth outline, so the player never catches on the seams between tiles
        MergeColliders(tTerrain.gameObject, c.slippery);
        MergeColliders(tCol.gameObject, c.slippery);

        if (hasBreak)
        {
            AddCollider(tBreak, false, c.slippery);
            tBreak.gameObject.AddComponent<BreakableTilemap>();
        }
        else UnityEngine.Object.DestroyImmediate(tBreak.gameObject);

        if (hasGhost)
        {
            AddCollider(tGhost, false, c.slippery);
            tGhost.gameObject.AddComponent<GhostTilemap>();
        }
        else UnityEngine.Object.DestroyImmediate(tGhost.gameObject);

        if (hasSpike)
        {
            AddCollider(tHaz, true, null);
            tHaz.gameObject.AddComponent<Hazard>();
        }
        else UnityEngine.Object.DestroyImmediate(tHaz.gameObject);

        tTerrain.CompressBounds();

        // small decor tiles on tops and hanging from ceilings
        Style st = StyleFor(c.index);
        c.grid.name = "Grid";
        PlaceDecor(c, tDecor, st);
    }

    static bool IsSolidGlyph(char ch) { return ch == '#'; }

    /// <summary>true for rock cells within `reach` cells (either axis) of anything that is not plain rock</summary>
    static bool[,] ComputeNear(LevelData d, int reach)
    {
        int cols = d.cols, rows = d.Rows;
        bool[,] open = new bool[cols, rows];
        for (int r = 0; r < rows; r++) for (int col = 0; col < cols; col++) open[col, r] = d.At(col, r) != '#';
        bool[,] hz = new bool[cols, rows];
        for (int r = 0; r < rows; r++)
        {
            int[] pref = new int[cols + 1];
            for (int col = 0; col < cols; col++) pref[col + 1] = pref[col] + (open[col, r] ? 1 : 0);
            for (int col = 0; col < cols; col++)
            {
                int a = Mathf.Max(0, col - reach), b = Mathf.Min(cols - 1, col + reach);
                hz[col, r] = pref[b + 1] - pref[a] > 0;
            }
        }
        bool[,] res = new bool[cols, rows];
        for (int col = 0; col < cols; col++)
        {
            int[] pref = new int[rows + 1];
            for (int r = 0; r < rows; r++) pref[r + 1] = pref[r] + (hz[col, r] ? 1 : 0);
            for (int r = 0; r < rows; r++)
            {
                int a = Mathf.Max(0, r - reach), b = Mathf.Min(rows - 1, r + reach);
                res[col, r] = pref[b + 1] - pref[a] > 0;
            }
        }
        return res;
    }

    static void PlaceDecor(Ctx c, Tilemap decor, Style st)
    {
        LevelData d = c.data;
        string id = c.info.id;
        List<Vector3Int> pos = new List<Vector3Int>(); List<TileBase> tiles = new List<TileBase>();
        // reserve space around the glyph objects first so decor never sits on a checkpoint or an altar
        for (int r = 0; r < d.Rows; r++)
            for (int col = 0; col < d.cols; col++)
            {
                char ch = d.At(col, r);
                if (ch == 'P' || ch == 'E' || ch == 'A' || (ch >= '1' && ch <= '9')) Reserve(c, col, r, 3);
            }

        for (int r = 1; r < d.Rows - 1; r++)
        {
            for (int col = 2; col < d.cols - 2; col++)
            {
                if (d.At(col, r) != '#') continue;
                if (c.reserved[col, r - 1] && d.At(col, r - 1) == ' ') { /* reserved above */ }
                // floor decor: solid with empty above
                if (d.At(col, r - 1) == ' ' && !c.reserved[col, r - 1] && st.floorDecor.Length > 0 && c.rng.NextDouble() < st.floorDecorChance)
                {
                    string n = st.floorDecor[c.rng.Next(st.floorDecor.Length)];
                    TileBase t = AreaArt.LoadTile(id, n);
                    if (t != null) { pos.Add(new Vector3Int(col, d.Rows - 1 - (r - 1), 0)); tiles.Add(t); }
                }
                // ceiling decor: solid with empty below
                if (r + 1 < d.Rows && d.At(col, r + 1) == ' ' && !c.reserved[col, r + 1] && st.ceilingDecor.Length > 0 && c.rng.NextDouble() < st.ceilingDecorChance)
                {
                    string n = st.ceilingDecor[c.rng.Next(st.ceilingDecor.Length)];
                    TileBase t = AreaArt.LoadTile(id, n);
                    if (t != null) { pos.Add(new Vector3Int(col, d.Rows - 1 - (r + 1), 0)); tiles.Add(t); }
                }
            }
        }
        decor.SetTiles(pos.ToArray(), tiles.ToArray());
    }

    // ---------------------------------------------------------------- scenery props

    static bool CellFree(LevelData d, int col, int row)
    {
        char ch = d.At(col, row);
        return ch == ' ' || ch == '.';
    }

    static void AddScenery(Ctx c)
    {
        LevelData d = c.data;
        Style st = StyleFor(c.index);
        GameObject root = new GameObject("Scenery");
        root.transform.SetParent(c.root, false);

        for (int pi = 0; pi < st.props.Count; pi++)
        {
            PropRule rule = st.props[pi];
            int lastCol = -9999;
            if (rule.mode == "air")
            {
                for (int r = 4; r < d.Rows - 4; r += 2)
                    for (int col = 6; col < d.cols - 6; col += 3)
                    {
                        if (c.rng.NextDouble() > rule.chance * 6f) continue;
                        if (!AirClear(d, col, r, 4) || c.reserved[col, r]) continue;
                        if (Math.Abs(col - lastCol) < rule.minGap && lastCol >= 0) continue;
                        lastCol = col;
                        SpawnProp(c, root.transform, rule, CellCenter(d, col, r), false);
                    }
                continue;
            }
            for (int r = 1; r < d.Rows - 1; r++)
            {
                lastCol = -9999;
                for (int col = 3; col < d.cols - 3; col++)
                {
                    if (d.At(col, r) != '#') continue;
                    if (c.rng.NextDouble() > rule.chance) continue;
                    if (Math.Abs(col - lastCol) < rule.minGap) continue;
                    if (rule.mode == "floor")
                    {
                        if (!CellFree(d, col, r - 1) || !CellFree(d, col, r - 2) || !CellFree(d, col, r - 3) || c.reserved[col, r - 1]) continue;
                        lastCol = col;
                        SpawnProp(c, root.transform, rule, BottomCenter(d, col, r - 1), false);
                    }
                    else // ceiling
                    {
                        if (!CellFree(d, col, r + 1) || !CellFree(d, col, r + 2) || c.reserved[col, r + 1]) continue;
                        lastCol = col;
                        Vector2 top = BottomCenter(d, col, r + 1);
                        top.y += Cell;      // top of the empty cell below the ceiling
                        SpawnProp(c, root.transform, rule, top, true);
                    }
                }
            }
        }
    }

    static bool AirClear(LevelData d, int col, int row, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                if (!CellFree(d, col + dx, row + dy)) return false;
        return true;
    }

    static void SpawnProp(Ctx c, Transform parent, PropRule rule, Vector2 at, bool hanging)
    {
        string name = rule.sprites[c.rng.Next(rule.sprites.Length)];
        Sprite s = Spr(c, name);
        if (s == null) return;
        GameObject g = NewChild(parent, name, new Vector3(at.x, at.y, 0f));
        SpriteRenderer sr = AddSprite(g, s, rule.order);
        if (rule.animate)
        {
            // find a numbered series (gear_s_0 -> gear_s_N, drop_0..., banner_0/1)
            int us = name.LastIndexOf('_');
            string prefix = us > 0 ? name.Substring(0, us) : name;
            Sprite[] frames = SprList(c, prefix, 6);
            if (frames.Length > 1)
            {
                SpriteFlipbook fb = g.AddComponent<SpriteFlipbook>();
                fb.frames = frames;
                fb.fps = rule.fps;
            }
        }
    }

    // ---------------------------------------------------------------- glyph objects

    static void AddGlyphObjects(Ctx c)
    {
        LevelData d = c.data;
        GameObject root = new GameObject("Objects");
        root.transform.SetParent(c.root, false);
        Sprite cpOff = Spr(c, "cp_off");
        Sprite[] cpOn = SprList(c, "cp_on", 4);
        Sprite[] exitFrames = SprList(c, "exit", 4);
        Sprite[] orb = SprList(c, "altar_orb", 4);

        // crumble platforms: scan runs of 'c'
        for (int r = 0; r < d.Rows; r++)
        {
            int col = 0;
            while (col < d.cols)
            {
                if (d.At(col, r) != 'c') { col++; continue; }
                int start = col;
                while (col < d.cols && d.At(col, r) == 'c') col++;
                int len = col - start;
                for (int k = 0; k + 1 < len + 1; k += 2)
                {
                    int w = (k + 1 < len) ? 2 : 1;
                    float cx = (start + k + w * 0.5f) * Cell;
                    float top = (d.Rows - 1 - r + 1) * Cell;
                    MakeCrumble(c, root.transform, new Vector2(cx, top), w);
                }
            }
        }

        for (int r = 0; r < d.Rows; r++)
        {
            for (int col = 0; col < d.cols; col++)
            {
                char ch = d.At(col, r);
                Vector2 b = BottomCenter(d, col, r);
                if (ch == 'P' || (ch >= '1' && ch <= '9'))
                {
                    bool start = ch == 'P';
                    GameObject g = NewChild(root.transform, start ? "Start" : "Checkpoint_" + ch, new Vector3(b.x, b.y, 0f));
                    SpriteRenderer sr = AddSprite(g, start && cpOn.Length > 0 ? cpOn[0] : cpOff, -1);
                    Checkpoint cp = g.AddComponent<Checkpoint>();
                    cp.sprite = sr; cp.offSprite = cpOff; cp.onSprites = cpOn; cp.isStart = start;
                    cp.area = start ? 1 : (ch - '0') + 1;
                    BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
                    bc.isTrigger = true; bc.size = new Vector2(4f, 8f); bc.offset = new Vector2(0f, 3.4f);   // walking into the area sets the checkpoint
                }
                else if (ch == 'E')
                {
                    GameObject g = NewChild(root.transform, "Exit", new Vector3(b.x, b.y, 0f));
                    SpriteRenderer sr = AddSprite(g, exitFrames.Length > 0 ? exitFrames[0] : null, -1);
                    if (exitFrames.Length > 1) { SpriteFlipbook fb = g.AddComponent<SpriteFlipbook>(); fb.frames = exitFrames; fb.fps = 6f; }
                    g.AddComponent<LevelExit>();
                    BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
                    bc.isTrigger = true; bc.size = new Vector2(1.2f, 2.2f); bc.offset = new Vector2(0f, 1.2f);
                }
                else if (ch == 'A')
                {
                    GameObject g = NewChild(root.transform, "Altar", new Vector3(b.x, b.y, 0f));
                    AddSprite(g, Spr(c, "altar_base"), -1);
                    GameObject haloGo = NewChild(g.transform, "Halo", new Vector3(b.x, b.y + 2.3f, 0f));
                    SpriteRenderer hs = AddSprite(haloGo, Spr(c, "halo"), 0);
                    Color hc = LevelCatalog.ParseHex(c.info.palette[4]); hc.a = 0.6f; hs.color = hc;
                    GameObject orbGo = NewChild(g.transform, "Orb", new Vector3(b.x, b.y + 2.0f, 0f));
                    SpriteRenderer os = AddSprite(orbGo, orb.Length > 0 ? orb[0] : null, 1);
                    orbGo.AddComponent<Bob>();
                    RelicAltar ra = g.AddComponent<RelicAltar>();
                    ra.skill = c.info.reward;
                    ra.orb = os; ra.halo = hs; ra.orbFrames = orb;
                    BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
                    bc.isTrigger = true; bc.size = new Vector2(2.6f, 3.4f); bc.offset = new Vector2(0f, 1.7f);
                }
            }
        }
    }

    static void MakeCrumble(Ctx c, Transform parent, Vector2 topCenter, int widthCells)
    {
        GameObject g = NewChild(parent, "Crumble", new Vector3(topCenter.x, topCenter.y, 0f));
        g.layer = c.groundLayer;
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(widthCells * Cell, Cell);
        bc.offset = new Vector2(0f, -Cell * 0.5f);
        if (c.slippery != null) bc.sharedMaterial = c.slippery;
        GameObject vis = NewChild(g.transform, "Visual", new Vector3(topCenter.x, topCenter.y, 0f));
        Sprite[] stages = SprList(c, "crumble", 3);
        SpriteRenderer sr = AddSprite(vis, stages.Length > 0 ? stages[0] : null, 0);
        if (widthCells == 1) vis.transform.localScale = new Vector3(0.5f, 1f, 1f);
        CrumblePlatform cp = g.AddComponent<CrumblePlatform>();
        cp.sprite = sr;
        cp.stages = stages;
    }

    // ---------------------------------------------------------------- level-file objects

    static void AddObjects(Ctx c)
    {
        LevelData d = c.data;
        if (d.objects == null) return;
        GameObject root = new GameObject("Zones");
        root.transform.SetParent(c.root, false);
        Sprite[] windH = SprList(c, "wind_h", 2);
        Sprite[] windV = SprList(c, "wind_v", 2);

        for (int i = 0; i < d.objects.Length; i++)
        {
            LevelObjectData o = d.objects[i];
            if (o == null || string.IsNullOrEmpty(o.t)) continue;
            if (o.t == "hint")
            {
                GameObject g = NewChild(root.transform, "Hint", BoxCenter(d, o));
                BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
                bc.isTrigger = true; bc.size = new Vector2(o.w * Cell, o.h * Cell);
                HintZone hz = g.AddComponent<HintZone>();
                hz.text = o.text;
            }
            else if (o.t == "wind")
            {
                GameObject g = NewChild(root.transform, "Wind", BoxCenter(d, o));
                BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
                bc.isTrigger = true; bc.size = new Vector2(o.w * Cell, o.h * Cell);
                WindZone wz = g.AddComponent<WindZone>();
                wz.targetVelocity = new Vector2(o.vx, o.vy);
                wz.accel = o.accel;
                wz.streakSprites = Mathf.Abs(o.vy) >= Mathf.Abs(o.vx) ? windV : windH;
                wz.streakCount = Mathf.Clamp(o.w * o.h / 4, 6, 40);
            }
            else if (o.t == "jet" || o.t == "beam")
            {
                MakeCycler(c, root.transform, o);
            }
        }
    }

    static Vector3 BoxCenter(LevelData d, LevelObjectData o)
    {
        // o.x, o.y = top-left cell (row from the top)
        return new Vector3((o.x + o.w * 0.5f) * Cell, (d.Rows - o.y - o.h * 0.5f) * Cell, 0f);
    }

    static void MakeCycler(Ctx c, Transform parent, LevelObjectData o)
    {
        LevelData d = c.data;
        bool beam = o.t == "beam";
        Vector3 center = new Vector3((o.x + 0.5f) * Cell, (d.Rows - o.y - 0.5f) * Cell, 0f);
        GameObject g = NewChild(parent, beam ? "Sunbeam" : "FireJet", center);
        float rot = 0f;
        if (!beam)
        {
            switch (o.dir) { case "l": rot = 90f; break; case "d": rot = 180f; break; case "r": rot = -90f; break; default: rot = 0f; break; }
        }
        else rot = 0f;      // beams always shine downward from their lens
        g.transform.rotation = Quaternion.Euler(0f, 0f, rot);
        float len = Mathf.Max(1, o.len) * Cell;

        PulseHazard ph = g.AddComponent<PulseHazard>();
        ph.onTime = o.on; ph.offTime = o.off; ph.warnTime = o.warn; ph.phase = o.phase;

        GameObject flameGo = new GameObject("Flame");
        flameGo.transform.SetParent(g.transform, false);
        SpriteRenderer fl = flameGo.AddComponent<SpriteRenderer>();
        fl.sortingOrder = 3;

        if (!beam)
        {
            // base block at the cell, flame grows out of its top
            GameObject b = new GameObject("Base");
            b.transform.SetParent(g.transform, false);
            b.transform.localPosition = new Vector3(0f, -Cell * 0.5f, 0f);
            AddSprite(b, Spr(c, "jet_base"), 2);
            flameGo.transform.localPosition = new Vector3(0f, Cell * 0.5f, 0f);
            Sprite[] fire = SprList(c, "jet_fire", 4);
            float spriteH = fire.Length > 0 ? fire[0].rect.height / 16f : 2.25f;
            flameGo.transform.localScale = new Vector3(1f, len / spriteH, 1f);
            ph.activeFrames = fire;
            ph.warnFrames = SprList(c, "jet_warn", 2);
            BoxCollider2D hb = g.AddComponent<BoxCollider2D>();
            hb.isTrigger = true;
            hb.size = new Vector2(0.7f, len);
            hb.offset = new Vector2(0f, Cell * 0.5f + len * 0.5f);
            ph.hitbox = hb;
        }
        else
        {
            // lens at the top of the cell, beam hangs below it
            GameObject lens = new GameObject("Lens");
            lens.transform.SetParent(g.transform, false);
            lens.transform.localPosition = new Vector3(0f, Cell * 0.5f, 0f);
            AddSprite(lens, Spr(c, "beam_lens"), 4);
            Sprite[] frames = SprList(c, "beam", 4);
            flameGo.transform.localPosition = new Vector3(0f, -Cell * 0.5f - len, 0f);
            fl.drawMode = SpriteDrawMode.Tiled;
            fl.size = new Vector2(0.5f, len);
            ph.activeFrames = frames;
            Sprite warn = Spr(c, "beam_warn");
            ph.warnFrames = warn != null ? new[] { warn } : new Sprite[0];
            BoxCollider2D hb = g.AddComponent<BoxCollider2D>();
            hb.isTrigger = true;
            hb.size = new Vector2(0.4f, len);
            hb.offset = new Vector2(0f, -Cell * 0.5f - len * 0.5f);
            ph.hitbox = hb;
        }
        ph.flame = fl;
        // the hazards of one area share a position-based phase so neighbouring ones are not in lock-step
    }

    // ---------------------------------------------------------------- kill zone, assets, director

    static void AddKillZone(Ctx c)
    {
        LevelData d = c.data;
        GameObject g = new GameObject("KillZone");
        g.transform.SetParent(c.root, false);
        g.transform.position = new Vector3(d.cols * Cell * 0.5f, -4f, 0f);
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(d.cols * Cell + 40f, 4f);
        g.AddComponent<Hazard>();
    }

    static void AddAssetsAndDirector(Ctx c, PlayerController player)
    {
        GameObject ag = new GameObject("LevelAssets");
        ag.transform.SetParent(c.root, false);
        LevelAssets a = ag.AddComponent<LevelAssets>();
        a.fontAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(FontPath);
        a.panel = AreaArt.SingleSprite(PanelPath);
        Sprite[] icons = new Sprite[5];
        string[] iconNames = { "icon_wallgrip", "icon_breaker", "icon_twinedge", "icon_pulse", "icon_ricochet" };
        for (int i = 0; i < 5; i++) icons[i] = AreaArt.SingleSprite(AreaArt.UiDir + "/" + iconNames[i] + ".png");
        a.skillIcons = icons;
        a.halo = Spr(c, "halo");
        string[] px = { "px_5", "px_4", "px_3", "px_2", "px_0" };
        List<Sprite> pxl = new List<Sprite>();
        for (int i = 0; i < px.Length; i++) { Sprite s; if (c.props.TryGetValue(px[i], out s)) pxl.Add(s); }
        a.pixels = pxl.ToArray();
        a.shards = SprList(c, "shard", 3);
        Color[] pal = new Color[6];
        for (int i = 0; i < 6; i++) pal[i] = LevelCatalog.ParseHex(c.info.palette[i]);
        a.palette = pal;

        GameObject dg = new GameObject("LevelDirector");
        dg.transform.SetParent(c.root, false);
        LevelDirector dir = dg.AddComponent<LevelDirector>();
        dir.levelIndex = c.index;
        dir.player = player;
    }
}
