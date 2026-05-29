namespace Task2.Models;

public class Manager : User
{
    public Manager(string name, int age, string profession) : base(name, age, profession) { }

    public override void AskFollowUpQuestions()
    {
        Console.WriteLine("\n--- Manager Questions ---");
        Console.Write("Do you need project management tools? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "yes");
        Console.Write("How large is your team? (small/medium/large): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "small");
        Console.Write("Do you prefer Kanban boards or list views? (kanban/lists): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "kanban");
    }

    public override List<string> GetRecommendations()
    {
        var needPM = Answers[0];
        var teamSize = Answers[1];
        var viewType = Answers[2];

        var tools = new List<string>();

        if (needPM == "yes")
        {
            if (viewType == "kanban")
            {
                tools.Add("Trello — simple Kanban boards, best for small teams");
                tools.Add("Linear — modern Kanban-style project tracking for product teams");
                if (teamSize is "medium" or "large")
                    tools.Add("Jira — enterprise Kanban and Scrum with advanced reporting");
            }
            else
            {
                tools.Add("Asana — list-based project management with multiple views");
                tools.Add("Monday.com — visual project tracking with list and timeline views");
                tools.Add("Notion — flexible databases that work as lists, kanban, or calendars");
            }

            if (teamSize is "medium" or "large")
            {
                tools.Add("Confluence — documentation and knowledge base for larger orgs");
                tools.Add("Slack — team communication with integrations to PM tools");
            }
        }
        else
        {
            tools.Add("Google Calendar — simple time-blocking and scheduling");
            tools.Add("Todoist — lightweight task management for personal organization");
        }

        if (teamSize == "small")
        {
            tools.Add("Notion — all-in-one workspace that scales with your team");
            tools.Add("Basecamp — simple project management with message boards and schedules");
        }

        return tools;
    }
}
