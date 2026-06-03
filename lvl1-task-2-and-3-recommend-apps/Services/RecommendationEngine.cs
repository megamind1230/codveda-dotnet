using Task2.Models;

namespace Task2.Services;

public class RecommendationEngine
{
    public void Process(User user)
    {
        Console.WriteLine($"\n=== Welcome, {user.Name}! ===");
        Console.WriteLine($"Age: {user.Age} | Profession: {user.Profession}");

        user.AskFollowUpQuestions();

        Console.WriteLine($"\n--- Recommendations for {user.Name} ---");
        var tools = user.GetRecommendations();

        for (int i = 0; i < tools.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {tools[i]}");
        }

        Console.WriteLine("\nTip: Try 2-3 tools for a week before deciding!");
    }
}
