using System.Collections.Generic;
using UnityEngine;

/// <summary>Place stars inside the camera view without spawning on the player or obstacles.</summary>
public class StarSpawner : MonoBehaviour
{
    [SerializeField] private StarCollectible starPrefab;
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float spawnInterval = 2f;
    [SerializeField, Min(1)] private int maxStars = 5;
    [SerializeField, Min(0f)] private float safeDistance = 1.5f;

    private readonly List<StarCollectible> stars = new List<StarCollectible>();
    private Camera gameplayCamera;
    private float nextSpawnTime;

    private void Start()
    {
        gameplayCamera = Camera.main;
        if (starPrefab == null || player == null || gameplayCamera == null)
        {
            Debug.LogWarning("StarSpawner needs a Star prefab, Player, and MainCamera.", this);
            enabled = false;
            return;
        }
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (player == null || gameplayCamera == null || Time.time < nextSpawnTime) return;
        nextSpawnTime = Time.time + spawnInterval;
        stars.RemoveAll(star => star == null);
        if (stars.Count >= maxStars) return;

        float depth = Vector3.Dot(-gameplayCamera.transform.position, gameplayCamera.transform.forward);
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 position = gameplayCamera.ViewportToWorldPoint(new Vector3(
                Random.Range(0.08f, 0.92f), Random.Range(0.1f, 0.9f), depth));
            position.z = 0f;
            if (Vector2.Distance(position, player.position) < safeDistance) continue;

            bool blocked = false;
            foreach (StarCollectible star in stars)
                if (Vector2.Distance(position, star.transform.position) < 0.8f) blocked = true;
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, 0.5f))
                if (hit.GetComponentInParent<Asteroid>() != null) blocked = true;
            if (blocked) continue;

            stars.Add(Instantiate(starPrefab, position, Quaternion.identity));
            break;
        }
    }
}
