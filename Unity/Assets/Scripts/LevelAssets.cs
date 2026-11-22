using UnityEngine;

/// <summary>
/// One place in each level scene that holds the shared art the runtime scripts need (font, UI sprites, skill icons,
/// particle sprites, the area's six colours). Filled in by the level builder; scripts read LevelAssets.Instance.
/// </summary>
public class LevelAssets : MonoBehaviour
{
    public static LevelAssets Instance;

    [Header("UI")]
    public Texture2D fontAtlas;
    public Sprite panel;                  // 9-sliced
    public Sprite buttonNormal;
    public Sprite buttonHover;
    public Sprite buttonPressed;
    [Tooltip("Same order as SkillInfo.Order: Wall Grip, Breaker, Twin Edge, Shard Pulse, Ricochet")]
    public Sprite[] skillIcons;

    [Header("Effects")]
    public Sprite halo;
    public Sprite[] pixels;               // 2x2 squares in palette colours 5,4,3,2,0
    public Sprite[] shards;               // little rock chips

    [Header("Area colours (dark to light)")]
    public Color[] palette = new Color[6];

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Color Pal(int i)
    {
        if (palette == null || palette.Length == 0) return Color.white;
        return palette[Mathf.Clamp(i, 0, palette.Length - 1)];
    }

    public Sprite Pixel(int i)
    {
        if (pixels == null || pixels.Length == 0) return null;
        return pixels[Mathf.Clamp(i, 0, pixels.Length - 1)];
    }

    public Sprite Shard(int i)
    {
        if (shards == null || shards.Length == 0) return null;
        return shards[((i % shards.Length) + shards.Length) % shards.Length];
    }

    public Sprite SkillIcon(Skill s)
    {
        int i = SkillInfo.IconIndex(s);
        if (skillIcons == null || i < 0 || i >= skillIcons.Length) return null;
        return skillIcons[i];
    }
}
