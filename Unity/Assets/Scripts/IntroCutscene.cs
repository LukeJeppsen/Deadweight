using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opening cutscene: a few narration cards on black, then the player (Wren) falls from the peak,
/// lands, gets up, and talks with Marrow in a dialogue box before control is handed over.
/// Edit the words in Narration and Lines() below. Press Esc / Start to skip.
/// Added to the scene by Tools > Pixel Art > Add Intro Cutscene To Open Scene.
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    [Header("Player")]
    public PlayerController player;
    public PlayerAnimationDriver animDriver;
    public Rigidbody2D body;

    [Header("UI")]
    public Image fade;
    public Image flash;
    public RectTransform barTop;
    public RectTransform barBottom;
    public PixelText narration;
    public DialogueBox dialogue;
    public Sprite wrenPortrait;
    public Sprite marrowPortrait;

    [Header("Fall")]
    [Tooltip("How far above the starting point the player drops from (world units).")]
    public float dropHeight = 18f;
    public float maxFallSpeed = 24f;
    [Tooltip("Play every time the scene loads. Off = only the first time the game is launched.")]
    public bool playEveryTime = true;

    static readonly string[] Narration =
    {
        "A HUNDRED YEARS AGO,\nTHE SUN STOPPED RISING.",
        "THEY SAY A BLADE OLDER THAN THE MOUNTAIN\nPINS IT TO THE PEAK.",
        "THEY CALL IT THE DEADWEIGHT."
    };

    static bool played;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { played = false; }

    private bool skip, ticking;
    private const float BarHeight = 14f;

    private DialogueBox.Line[] Lines()
    {
        Color wren = new Color32(0xF2, 0xD3, 0xAB, 0xFF);
        Color marrow = new Color32(0xC6, 0x9F, 0xA5, 0xFF);
        return new[]
        {
            new DialogueBox.Line { speaker = "WREN",   portrait = wrenPortrait,   nameColor = wren,   text = "...Again." },
            new DialogueBox.Line { speaker = "MARROW", portrait = marrowPortrait, nameColor = marrow, text = "Seventh fall, by my count. The mountain doesn't care how many." },
            new DialogueBox.Line { speaker = "WREN",   portrait = wrenPortrait,   nameColor = wren,   text = "Then I'll make it care." },
            new DialogueBox.Line { speaker = "MARROW", portrait = marrowPortrait, nameColor = marrow, text = "Heh. Remember what the shard does. Swing it where you want to go, and it drags you after it." },
            new DialogueBox.Line { speaker = "WREN",   portrait = wrenPortrait,   nameColor = wren,   text = "Swing to rise. I remember." },
            new DialogueBox.Line { speaker = "MARROW", portrait = marrowPortrait, nameColor = marrow, text = "Then climb, little blade. The sun is waiting. It can't get up on its own." },
        };
    }

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            if (animDriver == null) animDriver = player.GetComponent<PlayerAnimationDriver>();
            if (body == null) body = player.GetComponent<Rigidbody2D>();
        }
        if ((played || GameProgress.IntroSeen) && !playEveryTime) { gameObject.SetActive(false); return; }
        if (fade != null) fade.transform.SetAsFirstSibling();   // black sits BEHIND the narration, bars and dialogue
        // Belt and braces: give the text and dialogue their own sorting so nothing can ever cover them.
        Canvas root = GetComponentInParent<Canvas>();
        int order = root != null ? root.sortingOrder + 1 : 201;
        RaiseAboveFade(narration != null ? narration.gameObject : null, order);
        RaiseAboveFade(dialogue != null ? dialogue.root : null, order);
        RaiseAboveFade(barTop != null ? barTop.gameObject : null, order);
        RaiseAboveFade(barBottom != null ? barBottom.gameObject : null, order);
        SetAlpha(fade, 1f);                    // black from the very first frame
        if (fade != null) fade.gameObject.SetActive(true);
        if (flash != null) SetAlpha(flash, 0f);
        if (dialogue != null) dialogue.Hide();
        if (narration != null) narration.gameObject.SetActive(false);
        SetBars(0f);
    }

    private static void RaiseAboveFade(GameObject go, int order)
    {
        if (go == null) return;
        Canvas c = go.GetComponent<Canvas>();
        if (c == null) c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = order;
    }

    private IEnumerator Start()
    {
        Debug.Log("[Cutscene] Intro starting (narration cards: " + Narration.Length + ")");
        if (!gameObject.activeSelf || player == null || body == null) yield break;
        played = true;
        GameProgress.IntroSeen = true;

        // --- hold the player above the level, frozen, while the narration plays
        player.enabled = false;
        Vector2 home = body.position;
        Vector2 start = home + Vector2.up * dropHeight;
        body.simulated = false;
        body.linearVelocity = Vector2.zero;
        body.position = start;
        player.transform.position = start;
        WarpCamera(start - home);

        // --- narration cards
        yield return Wait(0.35f);
        narration.gameObject.SetActive(true);
        foreach (string card in Narration)
        {
            if (skip) break;
            narration.SetText(card, 260, 0);
            Debug.Log("[Cutscene] Narration card: " + narration.GlyphCount + " glyphs");
            SetAlpha(narration.GetComponent<RawImage>(), 1f);
            yield return TypeNarration();
            yield return WaitOrAdvance(1.3f);
            yield return FadeGraphic(narration.GetComponent<RawImage>(), 1f, 0f, 0.35f);
            yield return Wait(0.15f);
        }
        narration.gameObject.SetActive(false);

        // --- the fall
        body.simulated = true;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = player.movement != null ? player.movement.fallMultiplier : 3f;
        ticking = false;
        StartCoroutine(SlideBars(0f, 1f, 0.6f));
        StartCoroutine(FadeGraphic(fade, 1f, 0f, 0.9f));

        float fallTime = 0f;
        while (!skip)
        {
            yield return null;
            fallTime += Time.deltaTime;
            Vector2 v = body.linearVelocity;
            if (v.y < -maxFallSpeed) body.linearVelocity = new Vector2(0f, -maxFallSpeed);
            bool grounded = IsGrounded();
            if (animDriver != null) animDriver.Tick(0f, grounded, body.linearVelocity.y);
            if (grounded && fallTime > 0.4f) break;
            if (fallTime > 15f) break;      // safety net
        }
        if (skip)
        {
            body.linearVelocity = Vector2.zero;
            body.position = home;
            player.transform.position = home;
            WarpCamera(home - start);
            SetAlpha(fade, 0f);
            SetBars(1f);
        }
        else
        {
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 1f;
            Sfx.Play("land", 1f, 0f);
            yield return Impact();
        }

        // --- get up, then talk
        ticking = true;
        if (!skip)
        {
            yield return Wait(1.1f);
            yield return dialogue.Play(Lines(), () => skip);
        }

        // --- hand control back
        dialogue.Hide();
        ticking = false;
        yield return SlideBars(1f, 0f, 0.5f);
        body.gravityScale = 1f;
        body.simulated = true;
        player.enabled = true;
        if (player.inputHandler != null)
        {
            player.inputHandler.ResetAttackTrigger();
            player.inputHandler.ResetDashTrigger();
        }
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (CutsceneInput.SkipPressed()) skip = true;
        if (ticking && animDriver != null && body != null)
            animDriver.Tick(0f, IsGrounded(), body.linearVelocity.y);
    }

    // -------------------------------------------------------------- pieces

    private IEnumerator Impact()
    {
        // two quick flash steps on landing
        if (flash != null)
        {
            SetAlpha(flash, 0.5f); yield return null; yield return null;
            SetAlpha(flash, 0.25f); yield return null; yield return null;
            SetAlpha(flash, 0f);
        }
    }

    private IEnumerator TypeNarration()
    {
        int total = narration.GlyphCount;
        for (int i = 1; i <= total && !skip; i++)
        {
            narration.Reveal(i);
            if (CutsceneInput.AdvancePressed()) { narration.Reveal(total); break; }
            yield return new WaitForSeconds(0.03f);
        }
        narration.Reveal(total);
    }

    private bool IsGrounded()
    {
        if (player.groundCheck == null) return false;
        return Physics2D.OverlapCircle(player.groundCheck.position, player.checkRadius, player.groundLayer);
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds && !skip) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private IEnumerator WaitOrAdvance(float seconds)
    {
        float t = 0f;
        yield return null;
        while (t < seconds && !skip && !CutsceneInput.AdvancePressed()) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private IEnumerator FadeGraphic(Graphic g, float from, float to, float time)
    {
        if (g == null) yield break;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            // stepped (5 levels) so fades keep a pixel-art feel
            SetAlpha(g, Mathf.Round(Mathf.Lerp(from, to, t / time) * 5f) / 5f);
            yield return null;
        }
        SetAlpha(g, to);
        if (g == fade && to <= 0f) fade.gameObject.SetActive(false);
    }

    private IEnumerator SlideBars(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            SetBars(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time)));
            yield return null;
        }
        SetBars(to);
    }

    private void SetBars(float k)
    {
        float off = Mathf.Round(Mathf.Lerp(-BarHeight, 0f, k));
        if (barTop != null) barTop.anchoredPosition = new Vector2(0f, -off);
        if (barBottom != null) barBottom.anchoredPosition = new Vector2(0f, off);
    }

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    // Tell Cinemachine the target teleported so the camera doesn't slowly swoop across the level.
    private void WarpCamera(Vector2 delta)
    {
        try
        {
            System.Type t = System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
            if (t == null) return;
            Object cam = FindFirstObjectByType(t);
            if (cam == null) return;
            var m = t.GetMethod("OnTargetObjectWarped");
            if (m != null) m.Invoke(cam, new object[] { player.transform, (Vector3)delta });
        }
        catch (System.Exception) { /* purely cosmetic */ }
    }
}
