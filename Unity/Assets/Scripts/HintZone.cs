using UnityEngine;

/// <summary>Shows a short tip the first time the player walks into this trigger.</summary>
public class HintZone : MonoBehaviour
{
    [TextArea] public string text;
    public float seconds = 3.5f;
    public bool once = true;

    private bool shown;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (shown && once) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelDirector.Instance == null) return;
        shown = true;
        LevelDirector.Instance.Toast(text, seconds);
        Sfx.Play("hint", 0.7f, 0f);
    }
}
