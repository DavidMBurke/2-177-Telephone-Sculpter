using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI promptLabel;
    public TMP_InputField guessInput;
    public Button submitButton;
    public Button endGameButton;
    public Button startNewButton;   // Show only after End Game

    [Header("Game Objects")]
    public BuildZone buildZone;
    public GalleryReviewManager reviewManager;

    private enum Phase { Sculpt, Guess }
    private Phase phase;

    private readonly List<string> chain = new List<string>();
    private string currentPrompt;

    void Start()
    {
        // Safety: null checks before wiring
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
        if (endGameButton != null) endGameButton.onClick.AddListener(OnEndGame);
        if (startNewButton != null)
        {
            startNewButton.onClick.AddListener(OnStartNew);
            startNewButton.gameObject.SetActive(false);   // hidden during play
        }

        // End Game available during play
        if (endGameButton != null)
        {
            endGameButton.gameObject.SetActive(true);
            endGameButton.interactable = true;
        }

        StartFreshRound();
    }

    // -------------------------
    // Round / Phase management
    // -------------------------
    private void StartFreshRound()
    {
        // Generate first prompt
        currentPrompt = PromptGenerator.GeneratePrompt();
        chain.Clear();
        chain.Add(currentPrompt);

        // Reset UI state for a new round
        if (startNewButton != null) startNewButton.gameObject.SetActive(false);
        if (endGameButton != null) { endGameButton.gameObject.SetActive(true); endGameButton.interactable = true; }
        if (submitButton != null) { submitButton.gameObject.SetActive(true); submitButton.interactable = true; }
        if (guessInput != null)
        {
            guessInput.text = "";
            guessInput.gameObject.SetActive(false);
            guessInput.interactable = true;
        }

        // Make sure any leftover parts are gone (e.g., after a previous run)
        ClearBuildZoneChildren();

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

    // -------------------------
    // Button handlers
    // -------------------------
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

                if (buildZone != null)
                {
                    // BuildZone should internally call: reviewManager.Register(sculpture, prompt, guess)
                    buildZone.SaveAndClear(currentPrompt, guess);
                }

                chain.Add(guess);
                currentPrompt = guess;

                EnterSculptPhase();
                break;
        }
    }

    private void OnEndGame()
    {
        // Capture any sculpture still in the build zone (no guess yet)
        CaptureFinalSculptIfAny();

        // Lock interactive inputs during review
        if (submitButton != null) submitButton.interactable = false;
        if (guessInput != null) guessInput.interactable = false;
        if (endGameButton != null) endGameButton.interactable = false;

        // Reveal "Start New" so the player can begin a fresh round after reviewing
        if (startNewButton != null) startNewButton.gameObject.SetActive(true);

        // Kick off the review slideshow
        if (reviewManager != null)
        {
            reviewManager.StartReview();
        }
        else
        {
            Debug.LogWarning("GameController: GalleryReviewManager is not assigned.");
        }
    }

    private void OnStartNew()
    {
        // Stop and clear the previous review items/UI
        if (reviewManager != null)
        {
            reviewManager.StopReview();
            reviewManager.ClearAll();
        }

        // Ensure the build zone is cleared (in case End Game was pressed mid-sculpt)
        ClearBuildZoneChildren();

        // Re-enable all controls for a new round
        if (submitButton != null) { submitButton.interactable = true; submitButton.gameObject.SetActive(true); }
        if (guessInput != null) { guessInput.interactable = true; guessInput.text = ""; guessInput.gameObject.SetActive(false); }
        if (endGameButton != null) { endGameButton.interactable = true; endGameButton.gameObject.SetActive(true); }
        if (startNewButton != null) startNewButton.gameObject.SetActive(false);

        StartFreshRound();
    }

    // -------------------------
    // Helpers
    // -------------------------
    private void CaptureFinalSculptIfAny()
    {
        if (buildZone == null) return;
        var t = buildZone.transform;
        if (t.childCount == 0) return;

        // No guess provided; Register will just show Prompt in the review
        buildZone.SaveAndClear(currentPrompt, "");
    }

    private void ClearBuildZoneChildren()
    {
        if (buildZone == null) return;
        var t = buildZone.transform;
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
