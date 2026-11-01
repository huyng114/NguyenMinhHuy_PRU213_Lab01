using UnityEngine;

/// <summary>Award points once when the player touches a star.</summary>
public class StarCollectible : MonoBehaviour
{
    [SerializeField, Min(1)] private int points = 10;
    [SerializeField, Min(0.1f)] private float lifetime = 20f;
    private bool collected;

    private void Start() { Destroy(gameObject, lifetime); }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponentInParent<PlayerController>() == null) return;
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        collected = true;
        GameManager.Instance.CollectStar(points);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
