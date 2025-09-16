using System;
using System.Text;

public class PromptGenerator
{
    private static readonly string[] verbs = new string[]
    {
        "Dancing", "Juggling", "Fencing", "Flying", "Running",
        "Swimming", "Climbing", "Singing", "Skipping", "Painting"
    };

    private static readonly string[] nouns = new string[]
    {
        "Unicorn", "Kitten", "Frog", "Dragon", "Penguin",
        "Robot", "Wizard", "Octopus", "Pirate", "Ninja"
    };

    private static Random rng = new Random();

    public static string GeneratePrompt()
    {
        string verb = verbs[rng.Next(verbs.Length)];
        string noun = nouns[rng.Next(nouns.Length)];
        return $"{verb} {noun}";
    }
}
