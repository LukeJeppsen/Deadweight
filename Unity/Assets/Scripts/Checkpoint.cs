using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>The start of an area. Walk into the area and this becomes your respawn point.</summary>
public class Checkpoint : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Sprite offSprite;
    public Sprite[] onSprites;
    public float fps = 8f;
    public bool isStart;
    [Tooltip("1-based number of the area this marker starts. Entering the area sets the respawn point here.")]
    public int area;

    public bool Lit { get; private set; }

    public Vector2 RespawnPoint { get { return (Vector2)transform.position + new Vector2(0f, 0.8f); } }

    private void Awake()
    {
        // A tall catch zone: launching high over the marker (or passing just under it) still counts.
        // The levels run left to right, so a tall, narrow column is safe.
        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        if (bc != null && bc.isTrigger) { bc.size = new Vector2(4f, 26f); bc.offset = new Vector2(0f, 9f); }
    }

    private void Update()
    {
        if (sprite == null) return;
        if (Lit && onSprites != null && onSprites.Length > 0) sprite.sprite = onSprites[(int)(Time.time * fps) % onSprites.Length];
        else if (!Lit && offSprite != null) sprite.sprite = offSprite;
    }

    public void SetLit(bool lit) { Lit = lit; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isStart || Lit) return;     // the director also refuses earlier areas, so backtracking never moves you back
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelDirector.Instance != null) LevelDirector.Instance.SetCheckpoint(this, true);
    }
}
