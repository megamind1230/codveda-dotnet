namespace Task2.Models;

public class Student : User
{
    public Student(string name, int age, string profession) : base(name, age, profession) { }

    public override void AskFollowUpQuestions()
    {
        Console.WriteLine("\n--- Student Questions ---");
        Console.Write("Do you prefer taking notes in the browser? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "no");
        Console.Write("structured layouts or free-form notes? (structured/free): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "free");
        Console.Write("cloud or local storage? (cloud/local): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "cloud");
    }

    public override List<string> GetRecommendations()
    {
        var browserNotes = Answers[0];
        var structure = Answers[1];
        var storage = Answers[2];

        var tools = new List<string>();

        if (browserNotes == "yes" && structure == "structured")
        {
            tools.Add("Notion — all-in-one workspace with databases, great for structured notes in the browser");
            tools.Add("Coda — docs with spreadsheets and app-like building blocks");
        }
        else if (browserNotes == "yes" && structure == "free")
        {
            tools.Add("Google Keep — quick free-form notes with labels and reminders");
            tools.Add("Roam Research — networked thought, great for free-form linking");
        }
        else if (browserNotes == "no" && structure == "structured")
        {
            tools.Add("Obsidian — local-first markdown with graph view, very structured");
            tools.Add("Notion — also works offline with desktop app");
        }
        else
        {
            tools.Add("Obsidian — perfect for free-form local markdown notes");
            tools.Add("Logseq — open-source, free-form outliner with graph");
        }

        if (storage == "cloud")
            tools.Add("Google Drive + Docs — always have your notes accessible anywhere");

        return tools;
    }
}
