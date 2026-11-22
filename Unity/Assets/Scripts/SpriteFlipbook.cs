using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Plays a short sprite animation on a SpriteRenderer.</summary>
public class SpriteFlipbook : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 8f;
    public bool randomStart = true;

    private SpriteRenderer sr;
    private float offset;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (randomStart) offset = Random.value * 10f;
    }

    private void Update()
    {
        if (sr == null || frames == null || frames.Length == 0) return;
        int i = (int)((Time.time + offset) * fps) % frames.Length;
        sr.sprite = frames[i];
    }
}
