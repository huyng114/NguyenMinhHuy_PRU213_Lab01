using System.Collections.Generic;
using UnityEngine;

/// <summary>Spawns a limited number of asteroids in the upper part of the camera view.</summary>
public class AsteroidSpawner : MonoBehaviour
{
    [SerializeField] private Asteroid asteroidPrefab;
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.5f;
    [SerializeField, Min(1)] private int maxAsteroids = 8;
    [SerializeField, Min(0f)] private float safeDistance = 2f;

    private readonly List<Asteroid> spawnedAsteroids = new List<Asteroid>();
    private Camera gameplayCamera;
    private float nextSpawnTime;

    private void Start()
    {
        gameplayCamera = Camera.main;
        if (asteroidPrefab == null || player == null || gameplayCamera == null)
        {
            Debug.LogWarning("AsteroidSpawner needs an Asteroid prefab, Player, and MainCamera.", this);
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

        // Destroyed asteroids no longer count toward the limit.
        spawnedAsteroids.RemoveAll(asteroid => asteroid == null);
        if (spawnedAsteroids.Count >= maxAsteroids) return;

        float depth = Vector3.Dot(new Vector3(0f, 0f, 0f) - gameplayCamera.transform.position,
            gameplayCamera.transform.forward);

        // Try several positions; skip this spawn if no safe position is available.
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 position = gameplayCamera.ViewportToWorldPoint(new Vector3(
                Random.Range(0.1f, 0.9f), Random.Range(0.55f, 0.9f), depth));
            position.z = 0f;

            if (Vector2.Distance(position, player.position) < safeDistance) continue;

            bool overlapsAsteroid = false;
            foreach (Asteroid asteroid in spawnedAsteroids)
            {
                if (Vector2.Distance(position, asteroid.transform.position) < 1.2f)
                {
                    overlapsAsteroid = true;
                    break;
                }
            }
            if (overlapsAsteroid) continue;

            spawnedAsteroids.Add(Instantiate(asteroidPrefab, position, Quaternion.identity));
            break;
        }
    }
}
