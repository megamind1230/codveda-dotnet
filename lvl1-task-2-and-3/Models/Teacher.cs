namespace Task2.Models;

public class Teacher : User
{
    public Teacher(string name, int age, string profession) : base(name, age, profession) { }

    public override void AskFollowUpQuestions()
    {
        Console.WriteLine("\n--- Teacher Questions ---");
        Console.Write("Do you use the browser for note-taking? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "no");
        Console.Write("Do you enjoy writing in Markdown? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "yes");
        Console.Write("Do you need collaboration features? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "yes");
    }

    public override List<string> GetRecommendations()
    {
        var browserNotes = Answers[0];
        var likesMarkdown = Answers[1];
        var needCollab = Answers[2];

        var tools = new List<string>();

        if (browserNotes == "no" && likesMarkdown == "yes")
        {
            tools.Add("Obsidian — local markdown editor with graph view, great for lesson planning");
            tools.Add("tldraw.com — visual whiteboard for explaining concepts online");
        }
        else if (browserNotes == "yes" && likesMarkdown == "yes")
        {
            tools.Add("HackMD / HedgeDoc — collaborative markdown in the browser");
            tools.Add("Obsidian — sync via Obsidian Sync for cross-device access");
        }
        else if (browserNotes == "yes" && likesMarkdown == "no")
        {
            tools.Add("Google Docs — simple, familiar, great for sharing with students");
            tools.Add("Notion — structured databases for curriculum planning");
        }
        else
        {
            tools.Add("Microsoft OneNote — digital notebook with pen support for teaching");
            tools.Add("Notability — great for annotating slides and PDFs");
        }

        if (needCollab == "yes")
        {
            tools.Add("Google Classroom — manage assignments and communicate with students");
            tools.Add("Miro — collaborative whiteboard for brainstorming in class");
        }

        return tools;
    }
}
