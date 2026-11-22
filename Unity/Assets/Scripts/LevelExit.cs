using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>The way out of the level.</summary>
public class LevelExit : MonoBehaviour
{
    private bool used;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelDirector.Instance == null) return;
        used = true;
        LevelDirector.Instance.CompleteLevel();
    }
}
