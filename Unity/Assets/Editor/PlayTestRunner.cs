using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Drives one level in play mode for DeadweightPlayTest (editor only).</summary>
public class PlayTestRunner
{
    // Unity won't attach a MonoBehaviour from an Editor folder, so this is a plain class stepped by
    // EditorApplication.update. Test steps are coroutines: yield null = next frame, yield a float = wait that
    // many real seconds, yield another IEnumerator = run it to the end first.
    public int levelIndex;
    public Action<string, int> Done;

    private readonly StringBuilder log = new StringBuilder();
    private readonly List<string> errors = new List<string>();
    private readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    private int fails;
    private Keyboard kb;
    private LevelDirector dir;
    private PlayerController player;
    private Rigidbody2D body;
    private float waitUntil, deadline;
    private bool finished;
    private static readonly FieldInfo CurrentField = typeof(LevelDirector).GetField("current", BindingFlags.NonPublic | BindingFlags.Instance);

    public void Begin()
    {
        Application.logMessageReceived += OnLog;
        log.Append("\n== Level " + (levelIndex + 1) + " (" + SceneManager.GetActiveScene().name + ")\n");
        kb = InputSystem.AddDevice<Keyboard>();
        kb.MakeCurrent();
        deadline = Time.realtimeSinceStartup + 600f;
        stack.Push(Run());
    }

    private void OnLog(string msg, string stack_, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(type + ": " + msg + (string.IsNullOrEmpty(stack_) ? "" : " @ " + stack_.Split('\n')[0]));
    }

    /// <summary>Called every editor update while in play mode.</summary>
    public void Tick()
    {
        if (finished) return;
        if (Time.realtimeSinceStartup > deadline) { Fail("timed out"); Finish(); return; }
        if (Time.realtimeSinceStartup < waitUntil) return;
        while (stack.Count > 0)
        {
            IEnumerator top = stack.Peek();
            bool more;
            try { more = top.MoveNext(); }
            catch (Exception e) { Fail("test crashed: " + e.GetType().Name + " " + e.Message); Finish(); return; }
            if (!more) { stack.Pop(); continue; }
            object cur = top.Current;
            if (cur is IEnumerator sub) { stack.Push(sub); continue; }
            if (cur is float secs) waitUntil = Time.realtimeSinceStartup + secs;
            return;      // null or a wait: resume on a later update
        }
        Finish();
    }

    private void Finish()
    {
        finished = true;
        Application.logMessageReceived -= OnLog;
        if (errors.Count > 0)
        {
            Fail(errors.Count + " error(s) logged during play:");
            for (int i = 0; i < Mathf.Min(errors.Count, 12); i++) log.Append("      " + errors[i] + "\n");
        }
        else Pass("no errors or exceptions logged");
        Time.timeScale = 1f;
        if (kb != null) InputSystem.RemoveDevice(kb);
        Done?.Invoke(log.ToString(), fails);
    }

    private IEnumerator Run()
    {
        // ---- start (a fresh save: level 1 plays its intro, which we skip with Esc like a player would)
        yield return WaitReal(2.5f);
        IntroCutscene intro = UnityEngine.Object.FindFirstObjectByType<IntroCutscene>();
        if (intro != null && intro.gameObject.activeInHierarchy)
        {
            yield return Press(Key.Escape);
            float until = Time.realtimeSinceStartup + 20f;
            while (intro != null && intro.gameObject.activeInHierarchy && Time.realtimeSinceStartup < until) yield return null;
            Check(intro == null || !intro.gameObject.activeInHierarchy, "intro skips with Esc and hands control back");
            yield return WaitReal(1.5f);
        }
        dir = LevelDirector.Instance;
        if (dir == null) { Fail("no LevelDirector"); yield break; }
        player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        body = player != null ? player.GetComponent<Rigidbody2D>() : null;
        if (player == null || body == null) { Fail("no player"); yield break; }
        Check(!dir.Deathless, "dying is real here (not a practice soft-reset)");

        Skill expected = LevelCatalog.LoadoutBefore(levelIndex);
        Check(player.Abilities == null || player.Abilities.skills == expected,
            "starts with exactly the earlier areas' skills (" + expected + ")" + (player.Abilities != null ? ", has " + player.Abilities.skills : ""));

        List<Checkpoint> cps = new List<Checkpoint>(UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None));
        cps.Sort((a, b) => a.area.CompareTo(b.area));
        Checkpoint start = cps.Find(c => c.isStart);
        Check(start != null && Current() == start, "starts at the start marker");

        // ---- dying in a pit
        int d0 = dir.Deaths;
        GameObject kill = GameObject.Find("KillZone");
        if (kill == null) Fail("no KillZone");
        else
        {
            Teleport(kill.transform.position);
            yield return WaitReal(2.2f);
            Check(dir.Deaths == d0 + 1, "falling into the pit kills you (deaths " + d0 + " -> " + dir.Deaths + ")");
            Diag("after pit", kill);
            Check(start == null || Near(start.RespawnPoint, 1.5f), "respawned at the start marker " + Pos());
            Check(PlayerVisible(), "player visible again after respawn");
        }

        // ---- dying on spikes
        Hazard spikes = null;
        foreach (Hazard h in UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None)) if (h.GetComponent<Tilemap>() != null) spikes = h;
        if (spikes == null) log.Append("  -  no spike tiles in this level\n");
        else
        {
            Tilemap tm = spikes.GetComponent<Tilemap>();
            Vector3? spot = null;
            foreach (Vector3Int c in tm.cellBounds.allPositionsWithin) if (tm.HasTile(c)) { spot = tm.GetCellCenterWorld(c); break; }
            int d1 = dir.Deaths;
            Teleport((Vector2)spot.Value);
            yield return WaitReal(2.2f);
            Check(dir.Deaths == d1 + 1, "touching spikes kills you (deaths " + d1 + " -> " + dir.Deaths + ")");
        }

        // ---- checkpoints in order, approached from above (a launch over the marker)
        Checkpoint last = start;
        foreach (Checkpoint cp in cps)
        {
            if (cp.isStart) continue;
            Teleport(cp.RespawnPoint + new Vector2(0f, 6f));
            yield return WaitReal(0.25f);
            Check(Current() == cp, "checkpoint " + (cp.area - 1) + " registers when you pass over it" + (Current() != cp ? " (current: " + Name(Current()) + ")" : ""));
            last = Current() ?? last;
        }
        // backtracking
        if (cps.Count > 2)
        {
            Checkpoint early = cps[1];
            Teleport(early.RespawnPoint);
            yield return WaitReal(0.25f);
            Check(Current() == last, "walking back over checkpoint " + (early.area - 1) + " keeps the later one (" + Name(Current()) + ")");
        }
        // death respawns at the latest checkpoint
        if (kill != null && last != null)
        {
            int d2 = dir.Deaths;
            Teleport(kill.transform.position);
            yield return WaitReal(2.2f);
            Check(dir.Deaths == d2 + 1 && Near(last.RespawnPoint, 1.5f), "dying again respawns at the latest checkpoint " + Pos());
        }

        // ---- relic altar
        RelicAltar altar = UnityEngine.Object.FindFirstObjectByType<RelicAltar>();
        if (altar == null) log.Append("  -  no altar in this level\n");
        else
        {
            Skill reward = altar.skill;
            Teleport((Vector2)altar.transform.position + new Vector2(0f, 1f));
            yield return WaitReal(1.5f);
            Check(Time.timeScale == 0f, "altar shows the skill card");
            yield return Press(Key.Space);
            yield return WaitReal(1f);
            Check(Time.timeScale == 1f, "skill card closes on Space");
            Check(player.Abilities == null || (player.Abilities.skills & reward) == reward, "altar grants " + reward);
            Diag("after altar", altar.gameObject);
        }

        // ---- pause menu opens and closes
        yield return Press(Key.P);
        yield return WaitReal(0.3f);
        Check(Time.timeScale == 0f, "P opens the pause menu");
        yield return Press(Key.P);
        yield return WaitReal(0.3f);
        Check(Time.timeScale == 1f, "P closes the pause menu");

        // ---- exit
        LevelExit exit = UnityEngine.Object.FindFirstObjectByType<LevelExit>();
        if (exit == null) { Fail("no exit"); yield break; }
        string here = SceneManager.GetActiveScene().name;
        Teleport((Vector2)exit.transform.position + new Vector2(0f, 1f));
        yield return WaitReal(2.5f);
        Check(Time.timeScale == 0f, "reaching the exit shows the level-complete card");
        yield return Press(Key.Space);
        yield return WaitReal(2.5f);
        string now = SceneManager.GetActiveScene().name;
        bool lastLevel = levelIndex >= LevelCatalog.Count - 1;
        string want = lastLevel ? GameConfig.EndingScene : GameConfig.LevelSelectScene;
        Check(now == want, "exit loads " + want + " (got " + now + ")");
        Check(GameProgress.IsCompleted(LevelCatalog.All[levelIndex].id), "level marked completed");
        if (!lastLevel) Check(GameProgress.IsUnlocked(levelIndex + 1), "next level unlocked");
        yield return WaitReal(1.5f);     // let the next scene run a moment, so its errors count too
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Dumps the state that decides whether triggers reach the player.</summary>
    private void Diag(string when, GameObject target)
    {
        log.Append("    [diag " + when + "] player go active=" + player.gameObject.activeInHierarchy + " layer=" + LayerMask.LayerToName(player.gameObject.layer)
            + " controller.enabled=" + player.enabled + " body.simulated=" + body.simulated + " bodyType=" + body.bodyType + " pos=" + Pos() + "\n");
        foreach (Collider2D c in player.GetComponentsInChildren<Collider2D>(true))
            log.Append("      player collider " + c.GetType().Name + " on '" + c.name + "' enabled=" + c.enabled + " trigger=" + c.isTrigger
                + " layer=" + LayerMask.LayerToName(c.gameObject.layer) + " bounds=" + c.bounds.center + " size " + c.bounds.size + "\n");
        if (target != null)
        {
            foreach (Collider2D c in target.GetComponentsInChildren<Collider2D>(true))
            {
                bool ignored = Physics2D.GetIgnoreLayerCollision(player.gameObject.layer, c.gameObject.layer);
                log.Append("      target '" + c.name + "' " + c.GetType().Name + " enabled=" + c.enabled + " trigger=" + c.isTrigger
                    + " layer=" + LayerMask.LayerToName(c.gameObject.layer) + " active=" + c.gameObject.activeInHierarchy
                    + " bounds=" + c.bounds.center + " size " + c.bounds.size + " layerCollisionIgnored=" + ignored + "\n");
            }
            foreach (MonoBehaviour m in target.GetComponents<MonoBehaviour>())
                log.Append("      target script " + (m == null ? "MISSING" : m.GetType().Name + " enabled=" + m.enabled) + "\n");
        }
        FieldInfo dying = typeof(LevelDirector).GetField("dying", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo intro = typeof(LevelDirector).GetField("introRunning", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo busy = typeof(LevelDirector).GetField("busy", BindingFlags.NonPublic | BindingFlags.Instance);
        log.Append("      director dying=" + (dying != null ? dying.GetValue(dir) : "?") + " introRunning=" + (intro != null ? intro.GetValue(dir) : "?")
            + " busy=" + (busy != null ? busy.GetValue(dir) : "?") + " Deathless=" + dir.Deathless + " timeScale=" + Time.timeScale
            + " autoSync=" + Physics2D.autoSyncTransforms + " simMode=" + Physics2D.simulationMode + "\n");
        IntroCutscene ic = UnityEngine.Object.FindFirstObjectByType<IntroCutscene>(FindObjectsInactive.Include);
        if (ic != null) log.Append("      intro active=" + ic.gameObject.activeInHierarchy + "\n");
    }

    private Checkpoint Current() { return dir != null ? CurrentField.GetValue(dir) as Checkpoint : null; }
    private static string Name(Checkpoint c) { return c == null ? "none" : c.name; }
    private string Pos() { return "(" + body.position.x.ToString("0.0") + ", " + body.position.y.ToString("0.0") + ")"; }
    private bool Near(Vector2 p, float r) { return Vector2.Distance(body.position, p) <= r; }

    private bool PlayerVisible()
    {
        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>()) if (sr.enabled && sr.sprite != null) return true;
        return false;
    }

    private void Teleport(Vector2 p)
    {
        body.linearVelocity = Vector2.zero;
        body.position = p;
        player.transform.position = p;
        Physics2D.SyncTransforms();
    }

    private IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(kb, new KeyboardState(key));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(kb, new KeyboardState());
        yield return null;
    }

    private static IEnumerator WaitReal(float s) { yield return s; }

    private void Check(bool ok, string what) { if (ok) Pass(what); else Fail(what); }
    private void Pass(string what) { log.Append("  ok " + what + "\n"); }
    private void Fail(string what) { fails++; log.Append("  FAIL " + what + "\n"); }
}
