using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI promptLabel;
    public TMP_InputField guessInput;
    public Button submitButton;
    public Button endGameButton;
    public BuildZone buildZone;
    public GalleryReviewManager reviewManager;

    private enum Phase { Sculpt, Guess }
    private Phase phase;

    private readonly List<string> chain = new List<string>();
    private string currentPrompt;

    void Start()
    {
        submitButton.onClick.AddListener(OnSubmit);
        endGameButton.onClick.AddListener(OnEndGame);

        currentPrompt = PromptGenerator.GeneratePrompt();
        chain.Add(currentPrompt);
        EnterSculptPhase();
    }

    private void EnterSculptPhase()
    {
        phase = Phase.Sculpt;
        SetPromptText($"Sculpt: {currentPrompt}");
        guessInput.gameObject.SetActive(false);
        submitButton.gameObject.SetActive(true);
    }

    private void EnterGuessPhase()
    {
        phase = Phase.Guess;
        SetPromptText("Guess the sculpture:");
        guessInput.text = "";
        guessInput.gameObject.SetActive(true);
        guessInput.ActivateInputField();
        submitButton.gameObject.SetActive(true);
    }

    private void OnSubmit()
    {
        switch (phase)
        {
            case Phase.Sculpt:
                EnterGuessPhase();
                break;

            case Phase.Guess:
                var guess = guessInput.text.Trim();
                if (string.IsNullOrEmpty(guess))
                    return;

                if (buildZone != null)
                {
                    var saved = buildZone.SaveAndClear(currentPrompt, guess);
                }

                chain.Add(guess);
                currentPrompt = guess;

                EnterSculptPhase();
                break;
        }
    }


    private void OnEndGame()
    {
        submitButton.interactable = false;
        endGameButton.interactable = false;
        guessInput.interactable = false;

        if (reviewManager != null)
        {
            reviewManager.StartReview();
        }
        else
        {
            Debug.LogWarning("GalleryReviewManager not assigned in GameController.");
        }
    }

    private void SetPromptText(string text)
    {
        if (promptLabel != null)
            promptLabel.text = text;
    }

}
