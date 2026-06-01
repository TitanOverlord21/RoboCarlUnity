using System.Collections.Generic;
using UnityEngine;

public class CarlSparkEffect : MonoBehaviour
{
    readonly List<SpriteRenderer> _sparks = new();
    float _timer;
    bool _active;

    void Awake()
    {
        var sprite = GameSprites.White;
        for (var i = 0; i < 5; i++)
        {
            var spark = new GameObject($"Spark{i}");
            spark.transform.SetParent(transform, false);
            spark.transform.localPosition = new Vector3(Random.Range(-0.28f, 0.28f), Random.Range(-0.1f, 0.55f), 0f);
            spark.transform.localScale = new Vector3(0.06f, 0.06f, 1f);

            var renderer = spark.AddComponent<SpriteRenderer>();
            GameSprites.ConfigureRenderer(renderer);
            renderer.color = new Color(1f, 0.85f, 0.2f, 0f);
            renderer.sortingOrder = 5;
            _sparks.Add(renderer);
        }

        SetActive(false);
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (!active)
        {
            foreach (var spark in _sparks)
                spark.color = new Color(1f, 0.85f, 0.2f, 0f);
        }
    }

    void Update()
    {
        if (!_active)
            return;

        _timer += Time.deltaTime;
        if (_timer < 0.08f)
            return;

        _timer = 0f;
        foreach (var spark in _sparks)
        {
            bool on = Random.value > 0.45f;
            spark.color = on ? new Color(1f, 0.9f, 0.25f, 1f) : new Color(1f, 0.85f, 0.2f, 0f);
            if (on)
            {
                spark.transform.localPosition = new Vector3(
                    Random.Range(-0.32f, 0.32f),
                    Random.Range(-0.05f, 0.6f),
                    0f);
            }
        }
    }
}
