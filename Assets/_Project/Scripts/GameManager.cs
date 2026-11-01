using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns the current run's score, HUD, sound effects, and game-over state.</summary>
[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameOver { get; private set; }
    public static int FinalScore { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [Header("Scoring")]
    [SerializeField, Min(0)] private int crashPenalty = 5;
    [Header("Sounds")]
    [SerializeField] private AudioClip laserSound;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip gameOverSound;

    private int score;
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        RefreshScore();
    }

    public void CollectStar(int points)
    {
        if (IsGameOver) return;
        score += points;
        RefreshScore();
        PlaySound(collectSound);
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        score -= crashPenalty;
        RefreshScore();
        if (finalScoreText != null) finalScoreText.text = "FINAL SCORE: " + score;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        PlaySound(gameOverSound);
        Time.timeScale = 0f;
        FinalScore = score;
        StartCoroutine(FinishRun());
    }

    private IEnumerator FinishRun()
    {
        yield return new WaitForSecondsRealtime(0.7f);
        SceneUI ui = FindAnyObjectByType<SceneUI>();
        if (ui != null) ui.OpenEndGame();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PlayLaserSound() { PlaySound(laserSound); }
    public void PlayExplosionSound() { PlaySound(explosionSound); }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    private void RefreshScore()
    {
        if (scoreText != null) scoreText.text = "SCORE: " + score;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        Time.timeScale = 1f;
    }
}
