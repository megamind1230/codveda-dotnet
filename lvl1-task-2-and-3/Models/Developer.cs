namespace Task2.Models;

public class Developer : User
{
    public Developer(string name, int age, string profession) : base(name, age, profession) { }

    public override void AskFollowUpQuestions()
    {
        Console.WriteLine("\n--- Developer Questions ---");
        Console.Write("What OS do you use? (windows/mac/linux): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "windows");
        Console.Write("Do you prefer full IDEs or lightweight editors? (ide/editor): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "editor");
        Console.Write("Do you work solo or in a team? (solo/team): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "solo");
    }

    public override List<string> GetRecommendations()
    {
        var os = Answers[0];
        var idkORtext = Answers[1];
        var teamSize = Answers[2];

        var tools = new List<string>();

        if (idkORtext == "ide")
        {
            if (os == "windows")
                tools.Add("Visual Studio 2022 — full IDE with .NET debugging, great for enterprise");
            else if (os == "mac")
                tools.Add("JetBrains Rider — cross-platform .NET IDE with ReSharper smarts");
            else
                tools.Add("JetBrains Rider or VS Code + C# Dev Kit — best options on Linux");

        }
        else
        {
            tools.Add("VS Code — lightweight, extensible, works everywhere");
            tools.Add("Sublime Text — blazing fast editor for quick edits");
            tools.Add("Emacs - if you like it the long-term rewarding, tough way");
            if (os == "mac" || os == "linux")
                tools.Add("Zed — Rust-based, modern, GPU-accelerated editor");
        }

        if (teamSize == "team")
        {
            tools.Add("GitHub — essential for version control and PR reviews");
            tools.Add("Linear or Jira — project tracking for dev teams");
        }
        else
        {
            tools.Add("Git + GitHub — still useful even when solo");
        }

        if (os == "linux")
            tools.Add("Neovim + tmux — terminal-based power user setup");

        return tools;
    }
}
