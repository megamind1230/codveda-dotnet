namespace Task2.Models;

public class Writer : User
{
    public Writer(string name, int age, string profession) : base(name, age, profession) { }

    public override void AskFollowUpQuestions()
    {
        Console.WriteLine("\n--- Writer Questions ---");
        Console.Write("Do you write long-form or short-form content? (long/short/both): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "long");
        Console.Write("Do you prefer distraction-free writing? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "yes");
        Console.Write("Do you prefer Markdown or rich text? (markdown/rich): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "markdown");
    }

    public override List<string> GetRecommendations()
    {
        var contentLength = Answers[0];
        var distractionFree = Answers[1];
        var textFormat = Answers[2];

        var tools = new List<string>();

        if (distractionFree == "yes")
        {
            if (textFormat == "markdown")
            {
                tools.Add("Obsidian — local markdown with focused writing mode and graph view");
                tools.Add("iA Writer — minimal markdown editor with live preview and focus mode");
                tools.Add("Ulysses — subscription-based, clean writing environment with library management");
            }
            else
            {
                tools.Add("Scrivener — power-writing tool for books and long manuscripts");
                tools.Add("FocusWriter — full-screen distraction-free rich text writing");
                tools.Add("Emacs org-mode/org-roam — you don't know it until you try it");
            }
        }
        else
        {
            if (textFormat == "markdown")
            {
                tools.Add("VS Code + Markdown extensions — customizable writing environment");
                tools.Add("Ghost — modern markdown editor for blogging with preview");
            }
            else
            {
                tools.Add("Google Docs — collaborative rich text with version history");
                tools.Add("Notion — databases and rich text in one place");
            }
        }

        if (contentLength == "long" || contentLength == "both")
        {
            tools.Add("Scrivener — best for organizing chapters and research");
            tools.Add("Google Docs — great for long-form collaboration with editors");
        }

        if (contentLength == "short" || contentLength == "both")
        {
            tools.Add("Substack — write and publish newsletters directly");
            tools.Add("Twitter/X Threads — for micro-writing and building audience");
        }

        return tools;
    }
}
