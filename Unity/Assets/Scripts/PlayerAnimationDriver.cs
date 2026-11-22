using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the two layered player animators (body + sword layer). Both animators contain the same
/// state names, so every state change is applied to both at once and they stay in sync.
/// Built by Tools > Pixel Art > Import Everything.
/// </summary>
public class PlayerAnimationDriver : MonoBehaviour
{
    public enum SlashDir { Forward, ForwardUp, Up, ForwardDown, Down }

    [Header("Animators (body and sword layer share state names)")]
    [SerializeField] private Animator bodyAnim;
    [SerializeField] private Animator swordAnim;

    [Header("Locomotion")]
    [Tooltip("Horizontal input magnitude below this counts as standing still.")]
    [SerializeField] private float idleThreshold = 0.1f;
    [Tooltip("Input up to this plays Walk (a light analog tilt); above it plays Run.")]
    [SerializeField] private float walkMaxInput = 0.6f;
    [Tooltip("Vertical speed below this (up or down) shows the jump apex pose.")]
    [SerializeField] private float apexVelocity = 1.5f;

    [Header("Rare idles")]
    [Range(0f, 1f)]
    [SerializeField] private float rareIdleChance = 0.2f;
    [Tooltip("Idle loops that must play before a rare idle can be rolled.")]
    [SerializeField] private int minIdleLoopsBeforeRare = 2;

    private static readonly string[] RareIdles = { "Idle_Shake", "Idle_Stretch", "Idle_Flourish", "Idle_ShoulderTap" };
    private static readonly string[] SlashStates = { "Slash_Forward", "Slash_ForwardUp", "Slash_Up", "Slash_ForwardDown", "Slash_Down" };

    private readonly Dictionary<string, float> clipLengths = new Dictionary<string, float>();
    private string current;
    private bool wasGrounded = true;
    private float slashUntil;
    private float landUntil;
    private float rareUntil;
    private float idleTimer;
    private int idleLoops;

    public bool IsSlashing => Time.time < slashUntil;

    private void Awake()
    {
        CacheLengths();
    }

    private void Start()
    {
        Play("Idle", true);
    }

    /// <summary>Call once per frame with |horizontal input|, grounded state and vertical velocity.</summary>
    public void Tick(float moveInput, bool grounded, float velocityY)
    {
        if (IsSlashing)
        {
            wasGrounded = grounded;
            return;
        }

        if (!grounded)
        {
            wasGrounded = false;
            string air = velocityY > apexVelocity ? "Jump_Rise"
                       : velocityY < -apexVelocity ? "Jump_Fall"
                       : "Jump_Apex";
            Play(air);
            return;
        }

        if (!wasGrounded)
        {
            wasGrounded = true;
            if (moveInput < idleThreshold)
            {
                Play("Land", true);
                landUntil = Time.time + Len("Land");
            }
        }

        if (Time.time < landUntil && moveInput < idleThreshold) return;

        if (moveInput >= idleThreshold)
        {
            Play(moveInput <= walkMaxInput ? "Walk" : "Run");
            idleTimer = 0f;
            idleLoops = 0;
            return;
        }

        // Standing still
        if (IsRareIdle(current))
        {
            if (Time.time >= rareUntil)
            {
                Play("Idle", true);
                idleTimer = 0f;
                idleLoops = 0;
            }
            return;
        }

        if (current != "Idle")
        {
            Play("Idle", true);
            idleTimer = 0f;
            idleLoops = 0;
            return;
        }

        idleTimer += Time.deltaTime;
        float len = Len("Idle");
        if (idleTimer >= len)
        {
            idleTimer -= len;
            idleLoops++;
            if (idleLoops >= minIdleLoopsBeforeRare && Random.value < rareIdleChance)
            {
                string rare = RareIdles[Random.Range(0, RareIdles.Length)];
                Play(rare, true);
                rareUntil = Time.time + Len(rare);
                idleLoops = 0;
            }
        }
    }

    /// <summary>
    /// Plays the slash clip that matches the aim. Frames always face right, so only |x| matters;
    /// the caller is responsible for flipping the visuals to face aim.x.
    /// </summary>
    public void PlaySlash(Vector2 aim)
    {
        float angle = Mathf.Atan2(aim.y, Mathf.Abs(aim.x)) * Mathf.Rad2Deg;
        SlashDir dir;
        if (angle > 67.5f) dir = SlashDir.Up;
        else if (angle > 22.5f) dir = SlashDir.ForwardUp;
        else if (angle > -22.5f) dir = SlashDir.Forward;
        else if (angle > -67.5f) dir = SlashDir.ForwardDown;
        else dir = SlashDir.Down;

        string state = SlashStates[(int)dir];
        Play(state, true);
        slashUntil = Time.time + Len(state);
        idleTimer = 0f;
        idleLoops = 0;
    }

    private static bool IsRareIdle(string state)
    {
        if (string.IsNullOrEmpty(state)) return false;
        for (int i = 0; i < RareIdles.Length; i++)
            if (RareIdles[i] == state) return true;
        return false;
    }

    private void Play(string state, bool force = false)
    {
        if (!force && state == current) return;
        current = state;
        int hash = Animator.StringToHash(state);
        if (bodyAnim != null) bodyAnim.Play(hash, 0, 0f);
        if (swordAnim != null) swordAnim.Play(hash, 0, 0f);
    }

    private float Len(string state)
    {
        return clipLengths.TryGetValue(state, out float l) ? l : 0.4f;
    }

    private void CacheLengths()
    {
        clipLengths.Clear();
        if (bodyAnim == null || bodyAnim.runtimeAnimatorController == null) return;
        foreach (AnimationClip clip in bodyAnim.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name.StartsWith("Body_"))
                clipLengths[clip.name.Substring(5)] = clip.length;
        }
    }
}
