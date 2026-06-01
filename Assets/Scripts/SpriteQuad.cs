using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteQuad : MonoBehaviour
{
    [SerializeField] Vector2 size = Vector2.one;
    [SerializeField] Color color = Color.white;
    [SerializeField] int sortingOrder;

    void Awake()
    {
        var renderer = GetComponent<SpriteRenderer>();
        GameSprites.ConfigureRenderer(renderer);
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }
}
