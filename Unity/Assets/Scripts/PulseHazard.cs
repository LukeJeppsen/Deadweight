using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A fire jet / sunbeam that cycles: dormant, a short warning, then deadly. The warning is the cue to move.
/// </summary>
public class PulseHazard : MonoBehaviour
{
    public SpriteRenderer flame;
    public Sprite[] activeFrames;
    public Sprite[] warnFrames;
    public float onTime = 1.1f;
    public float offTime = 1.9f;
    public float warnTime = 0.6f;
    public float phase;
    public float fps = 12f;
    public BoxCollider2D hitbox;

    private enum State { Off, Warn, On }
    private State state = State.Off;

    public bool Deadly { get { return state == State.On; } }

    private void Update()
    {
        float cycle = onTime + offTime;
        float t = (Time.time + phase) % cycle;
        State s = t < offTime - warnTime ? State.Off : (t < offTime ? State.Warn : State.On);
        if (s != state && s != State.Off)
        {
            // beams are drawn tiled, jets are not
            bool beam = flame != null && flame.drawMode == SpriteDrawMode.Tiled;
            string snd = (beam ? "beam_" : "jet_") + (s == State.Warn ? "warn" : "fire");
            Sfx.PlayAt(snd, transform.position, s == State.Warn ? 0.6f : 0.75f, 12f);
        }
        state = s;
        if (hitbox != null) hitbox.enabled = s == State.On;
        if (flame == null) return;
        if (s == State.Off) { flame.enabled = false; return; }
        flame.enabled = true;
        Sprite[] set = s == State.On ? activeFrames : warnFrames;
        if (set == null || set.Length == 0) { flame.enabled = false; return; }
        flame.sprite = set[(int)(Time.time * fps) % set.Length];
    }

    private void OnTriggerEnter2D(Collider2D other) { Hit(other); }
    private void OnTriggerStay2D(Collider2D other) { Hit(other); }

    private void Hit(Collider2D other)
    {
        if (!Deadly || LevelDirector.Instance == null) return;
        if (other.GetComponentInParent<PlayerController>() != null) LevelDirector.Instance.KillPlayer();
    }
}
