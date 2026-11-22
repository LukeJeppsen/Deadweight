using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gives the main menu its life: slow cloud/mist drift, mouse parallax, a gentle sway, twinkling stars,
/// rising embers and a bobbing title. Everything is snapped to whole art pixels so it stays crisp.
/// Built and wired up by Tools > Pixel Art > Build Main Menu Scene.
/// </summary>
public class MenuBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name;
        [Tooltip("Tileable layers use a RawImage and scroll through its uvRect.")]
        public RawImage raw;
        [Tooltip("Non-tileable layers (the foreground) just move their RectTransform.")]
        public RectTransform rect;
        [Tooltip("Pixels of drift per second (negative = left).")]
        public float scrollSpeed;
        [Tooltip("Pixels the layer shifts at the screen edge when the mouse moves. Bigger = closer to the viewer.")]
        public float parallax = 1f;
        public float swayAmplitude = 0.75f;
        public float swayPeriod = 9f;
        public float phase;
    }

    private const float ArtWidth = 320f;

    [Header("Layers (back to front)")]
    public Layer[] layers;

    [Header("Stars")]
    public RawImage starsA;
    public RawImage starsB;
    public float twinkleSpeed = 1.4f;

    [Header("Title")]
    public RectTransform title;
    public float titleBaseY = -5f;

    [Header("Embers")]
    public RectTransform emberRoot;
    public Sprite emberSprite;
    public Color[] emberColors;
    public int emberCount = 16;

    [Header("Mouse parallax")]
    public float mouseSmoothing = 3f;

    private Vector2 mouse;
    private Image[] embers;
    private float[] emberAge, emberLife, emberX, emberY, emberSpeed, emberSway, emberPhase;
    private Color[] emberCol;

    private void Start()
    {
        BuildEmbers();
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;

        // Smoothed mouse position, -1..1 around the screen centre
        Vector2 target = Vector2.zero;
        if (TryGetMouse(out Vector2 m) && Screen.width > 0 && Screen.height > 0)
        {
            target = new Vector2(
                Mathf.Clamp((m.x / Screen.width) * 2f - 1f, -1f, 1f),
                Mathf.Clamp((m.y / Screen.height) * 2f - 1f, -1f, 1f));
        }
        mouse = Vector2.Lerp(mouse, target, 1f - Mathf.Exp(-mouseSmoothing * dt));

        if (layers != null)
        {
            for (int i = 0; i < layers.Length; i++) UpdateLayer(layers[i], t);
        }

        if (starsA != null) SetAlpha(starsA, 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * twinkleSpeed)));
        if (starsB != null) SetAlpha(starsB, 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * twinkleSpeed + Mathf.PI)));

        if (title != null)
        {
            title.anchoredPosition = new Vector2(0f, titleBaseY + Mathf.Round(Mathf.Sin(t * 1.6f) * 1.0f));
        }

        UpdateEmbers(dt);
    }

    private void UpdateLayer(Layer l, float t)
    {
        if (l == null) return;
        float sway = l.swayPeriod > 0.01f ? Mathf.Sin(t * Mathf.PI * 2f / l.swayPeriod + l.phase) * l.swayAmplitude : 0f;
        float shift = Mathf.Round(l.scrollSpeed * t + sway - mouse.x * l.parallax);

        if (l.raw != null)
        {
            // Texture repeats; stretch the uv window to the real width so ultrawide screens still tile.
            float w = ((RectTransform)l.raw.transform).rect.width;
            if (w < 1f) w = ArtWidth;
            Rect uv = l.raw.uvRect;
            uv.width = w / ArtWidth;
            uv.x = (-shift - (w - ArtWidth) * 0.5f) / ArtWidth;
            uv.y = 0f;
            uv.height = 1f;
            l.raw.uvRect = uv;
        }
        else if (l.rect != null)
        {
            Vector2 p = l.rect.anchoredPosition;
            p.x = shift;
            l.rect.anchoredPosition = p;
        }
    }

    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    private static bool TryGetMouse(out Vector2 pos)
    {
        pos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }
        return false;
#else
        pos = Input.mousePosition;
        return true;
#endif
    }

    // ---------------------------------------------------------------- embers

    private void BuildEmbers()
    {
        if (emberRoot == null || emberSprite == null || emberCount <= 0) return;
        embers = new Image[emberCount];
        emberAge = new float[emberCount];
        emberLife = new float[emberCount];
        emberX = new float[emberCount];
        emberY = new float[emberCount];
        emberSpeed = new float[emberCount];
        emberSway = new float[emberCount];
        emberPhase = new float[emberCount];
        emberCol = new Color[emberCount];

        for (int i = 0; i < emberCount; i++)
        {
            GameObject go = new GameObject("Ember" + i, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(emberRoot, false);
            Image img = go.GetComponent<Image>();
            img.sprite = emberSprite;
            img.raycastTarget = false;
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(2f, 2f);
            embers[i] = img;
            Respawn(i, true);
        }
    }

    private void Respawn(int i, bool scatter)
    {
        float halfW = Mathf.Max(emberRoot.rect.width, ArtWidth) * 0.5f;
        emberX[i] = Random.Range(-halfW, halfW);
        emberSpeed[i] = Random.Range(5f, 12f);
        emberLife[i] = Random.Range(9f, 16f);
        emberSway[i] = Random.Range(2f, 6f);
        emberPhase[i] = Random.Range(0f, 6.28f);
        emberY[i] = -92f;
        emberAge[i] = scatter ? Random.Range(0f, emberLife[i]) : 0f;
        emberY[i] += emberSpeed[i] * emberAge[i];
        emberCol[i] = (emberColors != null && emberColors.Length > 0)
            ? emberColors[Random.Range(0, emberColors.Length)] : Color.white;
    }

    private void UpdateEmbers(float dt)
    {
        if (embers == null) return;
        for (int i = 0; i < embers.Length; i++)
        {
            emberAge[i] += dt;
            if (emberAge[i] >= emberLife[i]) Respawn(i, false);
            emberY[i] += emberSpeed[i] * dt;

            float k = emberAge[i] / emberLife[i];
            float fade = Mathf.Sin(k * Mathf.PI);              // fade in, fade out
            float x = emberX[i] + Mathf.Sin(emberAge[i] * 0.8f + emberPhase[i]) * emberSway[i];

            embers[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(emberY[i]));
            Color c = emberCol[i];
            c.a = fade > 0.5f ? 1f : (fade > 0.2f ? 0.6f : 0.25f);   // stepped fade keeps the pixel feel
            embers[i].color = c;
        }
    }
}
