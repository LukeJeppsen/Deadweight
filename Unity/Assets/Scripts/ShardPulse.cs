using System;
using UnityEngine;

/// <summary>
/// The Shard Pulse skill: for a few seconds, ghost stone (GhostTilemap) becomes solid.
/// Lives on the player; the controller calls Trigger() when the Interact button is pressed.
/// </summary>
public class ShardPulse : MonoBehaviour
{
    public float duration = 3f;
    public float cooldown = 1.2f;

    public static bool Active { get; private set; }
    /// <summary>Seconds of pulse left (0 when not pulsing). Ghost stone blinks near the end.</summary>
    public static float Remaining { get; private set; }
    public static event Action Triggered;

    private float endTime;
    private float readyTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Active = false; Remaining = 0f; Triggered = null; }

    public bool CanPulse { get { return !Active && Time.time >= readyTime; } }

    /// <summary>1 = just pulsed, 0 = spent.</summary>
    public float Fraction { get { return Active ? Mathf.Clamp01((endTime - Time.time) / duration) : 0f; } }

    public void Trigger()
    {
        if (!CanPulse) return;
        Active = true;
        endTime = Time.time + duration;
        Remaining = duration;
        if (Triggered != null) Triggered();
    }

    private void Update()
    {
        if (Active)
        {
            Remaining = Mathf.Max(0f, endTime - Time.time);
            if (Time.time >= endTime)
            {
                Active = false;
                Remaining = 0f;
                readyTime = Time.time + cooldown;
            }
        }
    }

    public void ResetPulse()
    {
        Active = false;
        Remaining = 0f;
        readyTime = 0f;
    }

    private void OnDestroy()
    {
        Active = false;
        Remaining = 0f;
    }
}
