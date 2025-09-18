using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI promptLabel;
    public TMP_InputField guessInput;
    public Button submitButton;
    public Button endGameButton;
    public Button startNewButton;   // NEW
    public BuildZone buildZone;
    public GalleryReviewManager reviewManager;

    private enum Phase { Sculpt, Guess }
    private Phase phase;

    private readonly List<string> chain = new List<string>();
    private string currentPrompt;

    void Start()
    {
        // Wire up buttons
        submitButton.onClick.AddListener(OnSubmit);
        endGameButton.onClick.AddListener(OnEndGame);

        if (startNewButton != null)
        {
            startNewButton.onClick.AddListener(OnStartNew);
            startNewButton.gameObject.SetActive(false); // hidden until you end the game
        }

        // Make sure End Game is visible & usable from the start
        endGameButton.gameObject.SetActive(true);
        endGameButton.interactable = true;

        // First prompt
        currentPrompt = PromptGenerator.GeneratePrompt();
        chain.Clear();
        chain.Add(currentPrompt);
        EnterSculptPhase();
    }

    private void EnterSculptPhase()
    {
        phase = Phase.Sculpt;
        SetPromptText($"Sculpt: {currentPrompt}");
        if (guessInput != null)
        {
            guessInput.text = "";
            guessInput.gameObject.SetActive(false);
            guessInput.interactable = true;
        }
        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(true);
            submitButton.interactable = true;
        }
    }

    private void EnterGuessPhase()
    {
        phase = Phase.Guess;
        SetPromptText("Guess the sculpture:");
        if (guessInput != null)
        {
            guessInput.text = "";
            guessInput.gameObject.SetActive(true);
            guessInput.interactable = true;
        }
        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(true);
            submitButton.interactable = true;
        }
    }

    private void OnSubmit()
    {
        switch (phase)
        {
            case Phase.Sculpt:
                EnterGuessPhase();
                break;

            case Phase.Guess:
                var guess = guessInput != null ? guessInput.text.Trim() : "";
                if (string.IsNullOrEmpty(guess))
                    return;

                // Save & clear the build zone and register for review
                if (buildZone != null)
                {
                    var saved = buildZone.SaveAndClear(currentPrompt, guess); // :contentReference[oaicite:1]{index=1}
                }

                chain.Add(guess);
                currentPrompt = guess;

                EnterSculptPhase();
                break;
        }
    }

    private void OnEndGame()
    {
        // Lock inputs
        if (submitButton != null) submitButton.interactable = false;
        if (guessInput != null) guessInput.interactable = false;

        // Keep End Game visible but disable the click
        if (endGameButton != null) endGameButton.interactable = false;

        // Show Start New
        if (startNewButton != null) startNewButton.gameObject.SetActive(true);

        // Start the review slideshow
        if (reviewManager != null)
        {
            reviewManager.StartReview(); // :contentReference[oaicite:2]{index=2}
        }
        else
        {
            Debug.LogWarning("GalleryReviewManager not assigned in GameController.");
        }
    }

    private void OnStartNew()
    {
        // Stop and clear the existing review list/items
        if (reviewManager != null)
        {
            reviewManager.StopReview(); // :contentReference[oaicite:3]{index=3}
            reviewManager.ClearAll();   // NEW method below
        }

        // Clear any leftover pieces in the build zone (if user ended during sculpt)
        ClearBuildZoneChildren();

        // Reset the prompt chain and UI
        chain.Clear();
        currentPrompt = PromptGenerator.GeneratePrompt();
        chain.Add(currentPrompt);

        // Re-enable inputs
        if (submitButton != null) { submitButton.interactable = true; submitButton.gameObject.SetActive(true); }
        if (guessInput != null) { guessInput.interactable = true; guessInput.text = ""; guessInput.gameObject.SetActive(false); }
        if (endGameButton != null) { endGameButton.interactable = true; endGameButton.gameObject.SetActive(true); }
        if (startNewButton != null) startNewButton.gameObject.SetActive(false);

        EnterSculptPhase();
    }

    private void ClearBuildZoneChildren()
    {
        if (buildZone == null) return;
        var t = buildZone.transform;
        // destroy any loose parts left in the zone
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var child = t.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }
    }

    private void SetPromptText(string text)
    {
        if (promptLabel != null)
            promptLabel.text = text;
    }
}
