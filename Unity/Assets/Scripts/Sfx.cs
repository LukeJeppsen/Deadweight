using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every sound effect in the game. The sounds are synthesised in code when the game starts (retro square / noise
/// waves, no files needed). To replace one with your own recording, drop an audio file with the same name into
/// Assets/Audio/Resources/SFX/ (for example "jump.wav") - it is used automatically instead of the generated one.
///
///   Sfx.Play("jump");                       // plain 2D sound
///   Sfx.PlayAt("shatter", worldPos);        // quieter / panned with distance from the camera, silent off-screen
///   Sfx.Loop("wallslide", true);            // start / stop a looping sound
///
/// Lives on its own object that survives scene loads; created automatically, nothing to set up in a scene.
/// </summary>
public class Sfx : MonoBehaviour
{
    // ------------------------------------------------------------------ names (also the override file names)

    public static readonly string[] Names =
    {
        "jump", "walljump", "land", "swing", "launch", "ricochet", "shatter", "crumble_crack", "crumble_fall",
        "death", "respawn", "checkpoint", "relic", "skill", "complete", "pulse", "jet_warn", "jet_fire",
        "beam_warn", "beam_fire", "wallslide", "menu_move", "menu_confirm", "menu_back", "menu_error", "pause",
        "hint", "text"
    };

    // ------------------------------------------------------------------ volume settings (saved)

    private const string SfxKey = "deadweight_sfx_volume", MusicKey = "deadweight_music_volume";
    public const int VolumeSteps = 10;

    /// <summary>Sound-effect volume, 0..10.</summary>
    public static int SfxLevel
    {
        get { return PlayerPrefs.GetInt(SfxKey, 8); }
        set { PlayerPrefs.SetInt(SfxKey, Mathf.Clamp(value, 0, VolumeSteps)); PlayerPrefs.Save(); }
    }

    /// <summary>Music volume, 0..10. Anything playing music listens to MusicVolumeChanged.</summary>
    public static int MusicLevel
    {
        get { return PlayerPrefs.GetInt(MusicKey, 7); }
        set
        {
            PlayerPrefs.SetInt(MusicKey, Mathf.Clamp(value, 0, VolumeSteps)); PlayerPrefs.Save();
            if (MusicVolumeChanged != null) MusicVolumeChanged();
        }
    }

    public static float SfxVolume { get { return SfxLevel / (float)VolumeSteps; } }
    public static float MusicVolume { get { return MusicLevel / (float)VolumeSteps; } }
    public static event Action MusicVolumeChanged;

    // ------------------------------------------------------------------ public API

    public static void Play(string name, float volume = 1f, float pitchJitter = 0.06f)
    {
        Sfx s = Ensure();
        if (s != null) s.PlayInternal(name, volume, pitchJitter, 0f);
    }

    /// <summary>A sound that comes from somewhere in the world: fades with distance from the camera, panned left/right.</summary>
    public static void PlayAt(string name, Vector2 worldPos, float volume = 1f, float range = 15f)
    {
        Sfx s = Ensure();
        if (s == null) return;
        Camera cam = Camera.main;
        float pan = 0f;
        if (cam != null)
        {
            Vector2 c = cam.transform.position;
            float d = Vector2.Distance(c, worldPos);
            if (d > range) return;
            float k = d / range;
            volume *= 1f - k * k;
            pan = Mathf.Clamp((worldPos.x - c.x) / 12f, -1f, 1f) * 0.6f;
        }
        s.PlayInternal(name, volume, 0.06f, pan);
    }

    public static void Loop(string name, bool on, float volume = 1f)
    {
        if (!on && inst == null) return;
        Sfx s = Ensure();
        if (s != null) s.LoopInternal(name, on, volume);
    }

    public static void StopLoops()
    {
        if (inst != null && inst.loopSource != null) { inst.loopSource.Stop(); inst.loopName = null; }
    }

    // ------------------------------------------------------------------ internals

    private static Sfx inst;
    private static bool quitting;
    private const int Rate = 22050;
    private const int Voices = 14;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private AudioSource[] pool;
    private int next;
    private AudioSource loopSource;
    private string loopName;
    private float loopVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { inst = null; quitting = false; MusicVolumeChanged = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot() { Ensure(); }

    private static Sfx Ensure()
    {
        if (inst != null || quitting || !Application.isPlaying) return inst;   // editor tools stay silent
        GameObject go = new GameObject("Sfx");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<Sfx>();
        return inst;
    }

    private void Awake()
    {
        if (inst != null && inst != this) { Destroy(gameObject); return; }
        inst = this;
        pool = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++) pool[i] = NewSource();
        loopSource = NewSource();
        loopSource.loop = true;
        BuildClips();
    }

    private void OnApplicationQuit() { quitting = true; }

    private AudioSource NewSource()
    {
        AudioSource a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.spatialBlend = 0f;
        a.ignoreListenerPause = true;
        return a;
    }

    private void Update()
    {
        if (loopSource == null || loopName == null) return;
        // a loop never keeps playing under the pause menu or a pickup card
        bool frozen = Time.timeScale <= 0f;
        if (frozen && loopSource.isPlaying) loopSource.Pause();
        else if (!frozen && !loopSource.isPlaying) loopSource.UnPause();
        loopSource.volume = loopVolume * SfxVolume;
    }

    private void PlayInternal(string name, float volume, float jitter, float pan)
    {
        AudioClip clip;
        if (!clips.TryGetValue(name, out clip) || clip == null) { Debug.LogWarning("[Sfx] Unknown sound '" + name + "'"); return; }
        float v = volume * SfxVolume;
        if (v <= 0.001f) return;
        // the same sound fired many times in one moment (a patch of stone breaking) only plays once
        float last;
        if (lastPlayed.TryGetValue(name, out last) && Time.unscaledTime - last < 0.045f) return;
        lastPlayed[name] = Time.unscaledTime;

        AudioSource src = pool[next];
        for (int i = 0; i < pool.Length; i++)       // prefer a free voice
        {
            AudioSource cand = pool[(next + i) % pool.Length];
            if (!cand.isPlaying) { src = cand; next = (next + i) % pool.Length; break; }
        }
        next = (next + 1) % pool.Length;
        src.clip = clip;
        src.volume = Mathf.Clamp01(v);
        src.pitch = 1f + UnityEngine.Random.Range(-jitter, jitter);
        src.panStereo = pan;
        src.Play();
    }

    private void LoopInternal(string name, bool on, float volume)
    {
        if (!on)
        {
            if (loopName == name) { loopSource.Stop(); loopName = null; }
            return;
        }
        loopVolume = volume;
        if (loopName == name && loopSource.isPlaying) return;
        AudioClip clip;
        if (!clips.TryGetValue(name, out clip) || clip == null) return;
        loopName = name;
        loopSource.clip = clip;
        loopSource.volume = volume * SfxVolume;
        loopSource.pitch = 1f;
        loopSource.Play();
    }

    // ------------------------------------------------------------------ synthesis

    private enum W { Square, Triangle, Saw, Sine, Noise }

    private class Tone
    {
        public W wave;
        public float f0, f1;            // start / end frequency (Hz), slides exponentially
        public float dur;               // seconds
        public float delay;             // seconds before it starts
        public float vol = 1f;
        public float attack = 0.004f;
        public float decay = 1f;        // envelope shape after the attack: 0 = flat, 1 = linear, >1 = snappier
        public float duty = 0.5f;       // square wave duty
        public float vib, vibRate;      // vibrato depth (fraction) and speed (Hz)
        public float lp = 1f;           // one-pole low-pass amount (1 = off, small = darker)
    }

    private static Tone T(W w, float f0, float f1, float dur, float vol, float delay = 0f)
    {
        return new Tone { wave = w, f0 = f0, f1 = f1, dur = dur, vol = vol, delay = delay };
    }

    private void BuildClips()
    {
        Dictionary<string, Tone[]> r = Recipes();
        foreach (string name in Names)
        {
            AudioClip custom = Resources.Load<AudioClip>("SFX/" + name);
            if (custom != null) { clips[name] = custom; continue; }
            Tone[] tones;
            if (!r.TryGetValue(name, out tones)) continue;
            float[] data = Render(name, tones, name == "wallslide");
            AudioClip clip = AudioClip.Create("sfx_" + name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            clips[name] = clip;
        }
    }

    public const int SampleRate = Rate;

    /// <summary>The generated (not overridden) sounds as raw mono samples - used by the editor's WAV export.</summary>
    public static Dictionary<string, float[]> RenderAll()
    {
        Dictionary<string, float[]> o = new Dictionary<string, float[]>();
        Dictionary<string, Tone[]> r = Recipes();
        foreach (string name in Names)
        {
            Tone[] tones;
            if (r.TryGetValue(name, out tones)) o[name] = Render(name, tones, name == "wallslide");
        }
        return o;
    }

    private static float[] Render(string name, Tone[] tones, bool loop)
    {
        float len = 0f;
        foreach (Tone t in tones) len = Mathf.Max(len, t.delay + t.dur);
        int n = Mathf.Max(1, Mathf.CeilToInt(len * Rate));
        float[] mix = new float[n];
        System.Random rng = new System.Random(name.GetHashCode());

        foreach (Tone t in tones)
        {
            int start = Mathf.RoundToInt(t.delay * Rate);
            int count = Mathf.RoundToInt(t.dur * Rate);
            double phase = 0;
            float noise = 0f, lpState = 0f;
            for (int i = 0; i < count && start + i < n; i++)
            {
                float time = i / (float)Rate;
                float u = t.dur > 0f ? time / t.dur : 1f;
                float f = t.f0 * Mathf.Pow(t.f1 / t.f0, u);
                if (t.vib > 0f) f *= 1f + t.vib * Mathf.Sin(2f * Mathf.PI * t.vibRate * time);
                phase += f / Rate;
                if (phase >= 1.0)
                {
                    phase -= Math.Floor(phase);
                    if (t.wave == W.Noise) noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                }
                float p = (float)phase, s;
                switch (t.wave)
                {
                    case W.Square: s = p < t.duty ? 1f : -1f; break;
                    case W.Triangle: s = 1f - 4f * Mathf.Abs(p - 0.5f); break;
                    case W.Saw: s = 2f * p - 1f; break;
                    case W.Sine: s = Mathf.Sin(2f * Mathf.PI * p); break;
                    default: s = noise; break;
                }
                lpState += t.lp * (s - lpState);
                float env;
                if (loop) env = 1f;
                else if (time < t.attack) env = time / t.attack;
                else
                {
                    float k = Mathf.Clamp01((time - t.attack) / Mathf.Max(0.0001f, t.dur - t.attack));
                    env = t.decay <= 0f ? 1f : Mathf.Pow(1f - k, t.decay);
                }
                mix[start + i] += lpState * env * t.vol;
            }
        }

        // soft clip, and a tiny fade at the edges so nothing clicks
        int edge = Mathf.Min(64, n / 4);
        for (int i = 0; i < n; i++)
        {
            float v = (float)Math.Tanh(mix[i] * 0.9f);
            if (!loop)
            {
                if (i < edge) v *= i / (float)edge;
                if (i > n - edge) v *= (n - i) / (float)edge;
            }
            mix[i] = v;
        }
        return mix;
    }

    private static Dictionary<string, Tone[]> Recipes()
    {
        Dictionary<string, Tone[]> r = new Dictionary<string, Tone[]>();

        Tone jump = T(W.Square, 280f, 640f, 0.13f, 0.45f); jump.duty = 0.25f; jump.decay = 1.4f;
        r["jump"] = new[] { jump };

        Tone wj = T(W.Square, 340f, 780f, 0.12f, 0.4f); wj.duty = 0.25f; wj.decay = 1.4f;
        Tone wjn = T(W.Noise, 3000f, 1500f, 0.08f, 0.25f); wjn.lp = 0.5f;
        r["walljump"] = new[] { wj, wjn };

        Tone land = T(W.Noise, 900f, 300f, 0.09f, 0.7f); land.lp = 0.25f; land.decay = 2f;
        Tone thump = T(W.Sine, 130f, 60f, 0.09f, 0.55f); thump.decay = 1.5f;
        r["land"] = new[] { land, thump };

        Tone swing = T(W.Noise, 7000f, 1400f, 0.15f, 0.55f); swing.lp = 0.55f; swing.attack = 0.025f; swing.decay = 1.6f;
        r["swing"] = new[] { swing };

        Tone launch = T(W.Noise, 4500f, 700f, 0.24f, 0.5f); launch.lp = 0.45f; launch.attack = 0.01f;
        Tone launchSaw = T(W.Saw, 170f, 520f, 0.18f, 0.18f); launchSaw.lp = 0.35f;
        r["launch"] = new[] { launch, launchSaw };

        Tone ricA = T(W.Square, 1500f, 2300f, 0.05f, 0.25f);
        Tone ricB = T(W.Sine, 1760f, 1740f, 0.32f, 0.35f); ricB.decay = 2.5f;
        Tone ricC = T(W.Sine, 4860f, 4800f, 0.2f, 0.14f); ricC.decay = 3f;
        r["ricochet"] = new[] { ricA, ricB, ricC };

        Tone shA = T(W.Noise, 1800f, 200f, 0.45f, 0.9f); shA.lp = 0.5f; shA.decay = 1.8f;
        Tone shB = T(W.Noise, 500f, 100f, 0.32f, 0.6f); shB.lp = 0.2f;
        Tone shC = T(W.Square, 90f, 40f, 0.2f, 0.25f); shC.lp = 0.3f;
        Tone shD = T(W.Noise, 2600f, 1200f, 0.06f, 0.35f, 0.09f); shD.lp = 0.6f; shD.decay = 2f;
        r["shatter"] = new[] { shA, shB, shC, shD };

        Tone crA = T(W.Noise, 2600f, 900f, 0.1f, 0.35f); crA.lp = 0.6f; crA.decay = 3f;
        Tone crB = T(W.Noise, 2200f, 800f, 0.08f, 0.3f, 0.09f); crB.lp = 0.6f; crB.decay = 3f;
        r["crumble_crack"] = new[] { crA, crB };

        Tone cf = T(W.Noise, 900f, 150f, 0.38f, 0.55f); cf.lp = 0.3f; cf.decay = 1.5f;
        r["crumble_fall"] = new[] { cf };

        Tone dA = T(W.Square, 520f, 55f, 0.5f, 0.42f); dA.vib = 0.08f; dA.vibRate = 30f; dA.decay = 1.2f;
        Tone dB = T(W.Noise, 2000f, 200f, 0.35f, 0.45f); dB.lp = 0.4f;
        r["death"] = new[] { dA, dB };

        r["respawn"] = Arp(W.Triangle, new[] { 392f, 523f, 659f }, 0.06f, 0.13f, 0.38f);
        List<Tone> cp = new List<Tone>(Arp(W.Triangle, new[] { 523f, 659f, 784f, 1047f }, 0.07f, 0.16f, 0.38f));
        Tone cpTop = T(W.Square, 1047f, 1047f, 0.28f, 0.1f, 0.21f); cpTop.duty = 0.125f;
        cp.Add(cpTop);
        r["checkpoint"] = cp.ToArray();

        List<Tone> relic = new List<Tone>(Arp(W.Sine, new[] { 784f, 988f, 1175f, 1568f, 1976f }, 0.06f, 0.28f, 0.3f));
        Tone relicPad = T(W.Triangle, 196f, 392f, 0.7f, 0.3f); relicPad.attack = 0.1f;
        relic.Add(relicPad);
        r["relic"] = relic.ToArray();

        List<Tone> skill = new List<Tone>();
        foreach (float f in new[] { 523f, 659f, 784f }) { Tone c = T(W.Triangle, f, f, 1.0f, 0.24f); c.attack = 0.02f; c.decay = 1.2f; skill.Add(c); }
        Tone skTop = T(W.Square, 1047f, 1047f, 0.55f, 0.1f, 0.1f); skTop.duty = 0.125f; skTop.vib = 0.01f; skTop.vibRate = 6f;
        skill.Add(skTop);
        r["skill"] = skill.ToArray();

        List<Tone> done = new List<Tone>();
        float[] fan = { 523f, 659f, 784f, 1047f };
        float[] at = { 0f, 0.12f, 0.24f, 0.36f };
        float[] len = { 0.12f, 0.12f, 0.12f, 0.6f };
        for (int i = 0; i < fan.Length; i++) { Tone c = T(W.Square, fan[i], fan[i], len[i], 0.26f, at[i]); c.duty = 0.25f; c.decay = 0.7f; done.Add(c); }
        Tone bass = T(W.Triangle, 262f, 262f, 0.7f, 0.35f, 0.36f); bass.decay = 0.8f;
        done.Add(bass);
        r["complete"] = done.ToArray();

        Tone puA = T(W.Sine, 220f, 70f, 0.35f, 0.7f);
        Tone puB = T(W.Sine, 1320f, 2640f, 0.5f, 0.15f); puB.attack = 0.05f; puB.vib = 0.02f; puB.vibRate = 12f;
        Tone puC = T(W.Noise, 8000f, 6000f, 0.4f, 0.08f);
        r["pulse"] = new[] { puA, puB, puC };

        Tone jw = T(W.Noise, 1100f, 1700f, 0.35f, 0.3f); jw.lp = 0.3f; jw.attack = 0.2f; jw.decay = 0.5f;
        r["jet_warn"] = new[] { jw };
        Tone jf = T(W.Noise, 700f, 400f, 0.5f, 0.55f); jf.lp = 0.35f; jf.attack = 0.02f;
        Tone jfLow = T(W.Saw, 70f, 60f, 0.45f, 0.2f); jfLow.lp = 0.2f;
        r["jet_fire"] = new[] { jf, jfLow };

        Tone bw = T(W.Sine, 880f, 1320f, 0.35f, 0.22f); bw.attack = 0.2f; bw.vib = 0.03f; bw.vibRate = 20f;
        r["beam_warn"] = new[] { bw };
        Tone bfA = T(W.Square, 220f, 220f, 0.45f, 0.14f); bfA.lp = 0.3f; bfA.vib = 0.02f; bfA.vibRate = 40f;
        Tone bfB = T(W.Sine, 1760f, 1600f, 0.45f, 0.2f);
        r["beam_fire"] = new[] { bfA, bfB };

        Tone slide = T(W.Noise, 2200f, 2200f, 0.5f, 0.3f); slide.lp = 0.18f;
        r["wallslide"] = new[] { slide };

        r["menu_move"] = new[] { T(W.Square, 880f, 880f, 0.04f, 0.22f) };
        r["menu_confirm"] = new[] { T(W.Square, 660f, 660f, 0.05f, 0.22f), T(W.Square, 990f, 990f, 0.09f, 0.22f, 0.05f) };
        r["menu_back"] = new[] { T(W.Square, 660f, 660f, 0.05f, 0.22f), T(W.Square, 440f, 440f, 0.09f, 0.22f, 0.05f) };
        r["menu_error"] = new[] { T(W.Square, 200f, 150f, 0.16f, 0.28f), T(W.Square, 206f, 154f, 0.16f, 0.2f) };
        r["pause"] = new[] { T(W.Triangle, 784f, 784f, 0.06f, 0.35f), T(W.Triangle, 523f, 523f, 0.1f, 0.35f, 0.06f) };
        r["hint"] = new[] { T(W.Triangle, 1175f, 1175f, 0.08f, 0.25f), T(W.Triangle, 1568f, 1568f, 0.12f, 0.25f, 0.06f) };
        Tone text = T(W.Square, 1100f, 1100f, 0.025f, 0.1f); text.duty = 0.5f;
        r["text"] = new[] { text };
        return r;
    }

    private static Tone[] Arp(W wave, float[] notes, float step, float dur, float vol)
    {
        Tone[] t = new Tone[notes.Length];
        for (int i = 0; i < notes.Length; i++) { t[i] = T(wave, notes[i], notes[i], dur, vol, i * step); t[i].decay = 1.3f; }
        return t;
    }
}
