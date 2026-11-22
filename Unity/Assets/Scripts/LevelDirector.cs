using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs one level: gives the player the right skills, tracks deaths and checkpoints, respawns, handles relic
/// pickups, the level-complete flow, the pause menu and scene changes. One per level scene (added by the builder).
/// </summary>
public class LevelDirector : MonoBehaviour
{
    public static LevelDirector Instance;

    [Tooltip("Index in LevelCatalog. -1 = work it out from the scene name.")]
    public int levelIndex = -1;
    public PlayerController player;
    public Checkpoint startPoint;
    public float respawnDelay = 0.7f;
    [Tooltip("Optional practice mode for the first area: hazards only fade you back to the checkpoint, with no death or death count. Off by default: you die normally everywhere.")]
    public bool practiceFirstLevel = false;     // (renamed from deathlessFirstLevel so the old saved "true" in the scenes no longer applies)

    private LevelInfo info;
    private LevelHud hud;
    private Rigidbody2D body;
    private PlayerEffects effects;
    private SpriteRenderer[] playerRenderers;
    private bool[] rendererShown;   // which player sprites were visible when the level started
    private readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
    private Checkpoint current;
    private int deaths;
    private float startTime;
    private bool dying;
    private bool finished;
    private bool busy;           // pickup card / completion in progress
    private bool introRunning;
    private int pauseClosedFrame = -1;   // the Esc that closes the menu must not reopen it in the same frame

    public int Deaths { get { return deaths; } }
    public bool Deathless { get { return practiceFirstLevel && levelIndex == 0; } }
    public LevelInfo Info { get { return info; } }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (levelIndex < 0) levelIndex = LevelCatalog.IndexOfScene(SceneManager.GetActiveScene().name);
        if (levelIndex < 0) levelIndex = 0;
        info = LevelCatalog.All[levelIndex];
        GameProgress.CurrentLevelIndex = levelIndex;

        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player == null) { Debug.LogError("[LevelDirector] No player in the scene."); return; }
        body = player.GetComponent<Rigidbody2D>();
        effects = player.GetComponent<PlayerEffects>();
        if (effects == null) effects = player.gameObject.AddComponent<PlayerEffects>();
        playerRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        // Remember which sprites are meant to be visible. The prefab carries a few old / debug sprites that
        // are switched off (root sprite, attack point, ground check...); respawning must never turn those on.
        rendererShown = new bool[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++) rendererShown[i] = playerRenderers[i] != null && playerRenderers[i].enabled;

        // skills follow the progression
        if (player.Abilities != null) player.Abilities.SetSkills(GameProgress.SkillsForLevel(levelIndex));

        checkpoints.AddRange(FindObjectsByType<Checkpoint>(FindObjectsSortMode.None));
        if (startPoint == null)
        {
            for (int i = 0; i < checkpoints.Count; i++) if (checkpoints[i].isStart) { startPoint = checkpoints[i]; break; }
        }
        current = startPoint;
        if (current != null) current.SetLit(true);

        hud = GetComponentInChildren<LevelHud>();
        if (hud == null)
        {
            GameObject h = new GameObject("LevelHud");
            h.transform.SetParent(transform, false);
            hud = h.AddComponent<LevelHud>();
        }
        hud.Build(LevelAssets.Instance);
        hud.PauseChosen = OnPauseChosen;
        hud.SetDeaths(Deathless ? -1 : 0);
        if (player.Abilities != null) hud.RefreshSkills(player.Abilities.skills);

        if (current != null) PlaceAt(current.RespawnPoint);

        startTime = Time.time;
        StartCoroutine(Begin());
    }

    private IEnumerator Begin()
    {
        // an intro cutscene (level 1) handles its own fade and input; just wait for it
        IntroCutscene intro = FindFirstObjectByType<IntroCutscene>();
        introRunning = intro != null && intro.gameObject.activeInHierarchy;
        if (introRunning)
        {
            hud.SetFade(0f);
            while (intro != null && intro.gameObject.activeInHierarchy) yield return null;
            introRunning = false;
            startTime = Time.time;
        }
        else
        {
            player.SetInputLocked(true);
            yield return hud.FadeTo(0f, 0.5f);
            player.SetInputLocked(false);
        }
        yield return hud.ShowTitle(info.title, info.subtitle);
    }

    private void Update()
    {
        if (finished || dying || introRunning || player == null) return;
        if (busy && !hud.PauseOpen) return;
        if ((MenuInput.Back() || MenuInput.Pause()) && !hud.PauseOpen && Time.frameCount != pauseClosedFrame) OpenPause();
    }

    // ------------------------------------------------------------------ pause

    private void OpenPause()
    {
        Time.timeScale = 0f;
        player.SetInputLocked(true);
        Sfx.Play("pause", 1f, 0f);
        hud.OpenPause();
    }

    private void ClosePause()
    {
        hud.ClosePause();
        Sfx.Play("menu_back", 1f, 0f);
        pauseClosedFrame = Time.frameCount;
        Time.timeScale = 1f;
        // stay locked if a pickup card is still up underneath
        if (!busy) player.SetInputLocked(false);
    }

    private void OnPauseChosen(int choice)
    {
        switch (choice)
        {
            case LevelHud.PauseResume:
                ClosePause();
                break;
            case LevelHud.PauseRestart:
                hud.ClosePause();
                StartCoroutine(LoadScene(SceneManager.GetActiveScene().name));
                break;
            case LevelHud.PauseSelect:
                hud.ClosePause();
                StartCoroutine(LoadScene(GameConfig.LevelSelectScene));
                break;
            case LevelHud.PauseTitle:
                hud.ClosePause();
                StartCoroutine(LoadScene(GameConfig.TitleScene));
                break;
        }
    }

    private IEnumerator LoadScene(string sceneName)
    {
        finished = true;
        Time.timeScale = 1f;
        yield return hud.FadeTo(1f, 0.35f);
        if (Application.CanStreamedLevelBeLoaded(sceneName)) SceneManager.LoadScene(sceneName);
        else
        {
            Debug.LogWarning("[LevelDirector] Scene '" + sceneName + "' is not in Build Settings. Run Tools > Deadweight > Build Everything.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // ------------------------------------------------------------------ death + checkpoints

    private void PlaceAt(Vector2 pos)
    {
        player.transform.position = new Vector3(pos.x, pos.y, player.transform.position.z);
        if (body != null)
        {
            // move the body too: with interpolation on, only moving the transform can leave the body behind
            body.position = pos;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        Physics2D.SyncTransforms();
        if (CameraTarget.Instance != null) CameraTarget.Instance.Snap();
    }

    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderers == null) return;
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null || !rendererShown[i]) continue;
            playerRenderers[i].enabled = visible;
        }
    }

    public void KillPlayer()
    {
        if (dying || finished || player == null) return;
        if (Deathless) { StartCoroutine(SoftResetRoutine()); return; }
        StartCoroutine(DieRoutine());
    }

    /// <summary>Level 1 only: no death. A quick fade and you are back at the start of the area.</summary>
    private IEnumerator SoftResetRoutine()
    {
        dying = true;
        player.SetInputLocked(true);
        Sfx.Play("death", 0.5f);
        if (body != null) { body.linearVelocity = Vector2.zero; body.simulated = false; }
        yield return hud.FadeTo(1f, 0.12f);

        Vector2 at = current != null ? current.RespawnPoint : (Vector2)player.transform.position;
        player.ResetForRespawn();
        if (body != null) body.simulated = true;
        PlaceAt(at);
        ResetRoom();

        yield return hud.FadeTo(0f, 0.18f);
        player.SetInputLocked(false);
        dying = false;
    }

    private IEnumerator DieRoutine()
    {
        dying = true;
        deaths++;
        hud.SetDeaths(deaths);
        player.SetInputLocked(true);
        if (effects != null) effects.DeathBurst();
        CameraTarget.Shake(0.35f);
        SetPlayerVisible(false);
        if (body != null) { body.linearVelocity = Vector2.zero; body.simulated = false; }

        yield return new WaitForSeconds(respawnDelay);
        yield return hud.FadeTo(1f, 0.15f);

        Vector2 at = current != null ? current.RespawnPoint : (Vector2)player.transform.position;
        player.ResetForRespawn();
        if (body != null) body.simulated = true;
        PlaceAt(at);
        SetPlayerVisible(true);
        ResetRoom();
        Sfx.Play("respawn", 0.8f, 0f);

        yield return hud.FadeTo(0f, 0.2f);
        player.SetInputLocked(false);
        dying = false;
    }

    /// <summary>Hook for putting timed / destroyed things back after a death (crumble platforms recover on their own).</summary>
    private void ResetRoom()
    {
    }

    public void SetCheckpoint(Checkpoint cp, bool announce)
    {
        if (cp == null || cp == current) return;
        if (current != null && cp.area <= current.area) return;   // walking back over an earlier checkpoint keeps the later one
        if (current != null && !current.isStart) current.SetLit(false);
        current = cp;
        cp.SetLit(true);
        if (announce) Sfx.Play("checkpoint", 1f, 0f);
        LevelAssets a = LevelAssets.Instance;
        if (a != null)
        {
            Vector2 p = cp.transform.position;
            RingFx.Spawn(a.halo, p + new Vector2(0f, 0.6f), 0.4f, 5f, 0.5f, a.Pal(4));
            SimpleParticle.Burst(p + new Vector2(0f, 0.6f), 12, 1.5f, 5f, 4f, 0.7f, 5, 4);
        }
        if (announce && hud != null) hud.Toast(cp.area > 0 ? "AREA " + cp.area + " OF " + checkpoints.Count : "CHECKPOINT", 1.6f);
    }

    public void Toast(string text, float seconds)
    {
        if (hud != null) hud.Toast(text, seconds);
    }

    // ------------------------------------------------------------------ relic

    public IEnumerator GrantSkillSequence(Skill skill, PlayerController who)
    {
        busy = true;
        who.SetInputLocked(true);
        if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y * 0.2f);
        yield return new WaitForSeconds(0.35f);

        if (who.Abilities != null) who.Abilities.Grant(skill);
        else GameProgress.GrantSkill(skill);
        if (hud != null && who.Abilities != null) hud.RefreshSkills(who.Abilities.skills);

        Time.timeScale = 0f;
        Sfx.Play("skill", 1f, 0f);
        if (hud != null) yield return hud.SkillCard(skill);
        Time.timeScale = 1f;

        who.SetInputLocked(false);
        busy = false;
    }

    // ------------------------------------------------------------------ finishing

    public void CompleteLevel()
    {
        if (finished || dying) return;
        StartCoroutine(CompleteRoutine());
    }

    private IEnumerator CompleteRoutine()
    {
        finished = true;
        busy = true;
        player.SetInputLocked(true);
        float seconds = Time.time - startTime;

        GameProgress.LevelStat before = GameProgress.GetStat(info.id, false);
        bool best = before == null || before.bestTime <= 0f || seconds < before.bestTime;
        GameProgress.MarkCompleted(info.id, deaths, seconds);

        LevelAssets a = LevelAssets.Instance;
        if (a != null)
        {
            Vector2 p = player.transform.position;
            RingFx.Spawn(a.halo, p, 0.5f, 14f, 0.8f, a.Pal(5));
            SimpleParticle.Burst(p, 30, 2f, 9f, -2f, 1.1f, 5, 4, 3);
        }
        CameraTarget.Shake(0.2f);
        Sfx.Loop("wallslide", false);
        Sfx.Play("complete", 1f, 0f);
        yield return new WaitForSeconds(0.8f);

        bool last = levelIndex >= LevelCatalog.Count - 1;
        Time.timeScale = 0f;
        yield return hud.CompleteCard(info, Deathless ? -1 : deaths, seconds, best, last);
        Time.timeScale = 1f;

        yield return hud.FadeTo(1f, 0.5f);
        string next = last ? GameConfig.EndingScene : GameConfig.LevelSelectScene;
        if (Application.CanStreamedLevelBeLoaded(next)) SceneManager.LoadScene(next);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
