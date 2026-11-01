using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Menu actions and fades shared by the three game scenes.</summary>
[RequireComponent(typeof(AudioSource))]
public class SceneUI : MonoBehaviour
{
    [SerializeField] private GameObject instructionsPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private AudioClip clickSound;
    private bool transitioning;

    private void Start()
    {
        Time.timeScale = 1f;
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (resultText != null) resultText.text = "FINAL SCORE  " + GameManager.FinalScore;
        if (fadeOverlay != null) StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        fadeOverlay.alpha = 1f;
        fadeOverlay.blocksRaycasts = true;
        while (fadeOverlay.alpha > 0f)
        {
            fadeOverlay.alpha -= Time.unscaledDeltaTime / 0.3f;
            yield return null;
        }
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
    }

    public void PlayGame() { BeginTransition("Gameplay"); }
    public void ReturnToMenu() { BeginTransition("MainMenu"); }
    public void OpenEndGame() { BeginTransition("EndGame"); }
    public void ShowInstructions() { Click(); if (instructionsPanel != null) instructionsPanel.SetActive(true); }
    public void HideInstructions() { Click(); if (instructionsPanel != null) instructionsPanel.SetActive(false); }
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void Click()
    {
        if (clickSound != null) GetComponent<AudioSource>().PlayOneShot(clickSound, 0.35f);
    }

    private void BeginTransition(string scene)
    {
        if (transitioning) return;
        transitioning = true;
        Click();
        StartCoroutine(LoadWithFade(scene));
    }

    private IEnumerator LoadWithFade(string scene)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            while (fadeOverlay.alpha < 1f)
            {
                fadeOverlay.alpha += Time.unscaledDeltaTime / 0.3f;
                yield return null;
            }
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }
}
