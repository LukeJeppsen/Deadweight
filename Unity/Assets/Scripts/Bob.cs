using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Gentle vertical bob (the floating relic).</summary>
public class Bob : MonoBehaviour
{
    public float amplitude = 0.12f;
    public float speed = 2f;
    private Vector3 start;

    private void Start() { start = transform.localPosition; }

    private void Update()
    {
        // snapped to the 1/16 pixel grid so it stays crisp
        float y = Mathf.Round(Mathf.Sin(Time.time * speed) * amplitude * 16f) / 16f;
        transform.localPosition = start + new Vector3(0f, y, 0f);
    }
}
