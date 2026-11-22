using System.Collections;
using UnityEngine;

/// <summary>Fades a sprite out (stepped) and destroys the object.</summary>
public class FadeAndDie : MonoBehaviour
{
    public float life = 0.25f;
    private float age;
    private SpriteRenderer sr;
    private Color baseColor;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= life) { Destroy(gameObject); return; }
        if (sr != null)
        {
            Color c = baseColor;
            c.a = baseColor.a * Mathf.Ceil((1f - age / life) * 4f) / 4f;
            sr.color = c;
        }
    }
}
