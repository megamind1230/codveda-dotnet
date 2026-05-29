namespace Task2.Models;

public class Designer : User
{
    public Designer(string name, int age, string profession) : base(name, age, profession) { }

    public override void AskFollowUpQuestions()
    {
        Console.WriteLine("\n--- Designer Questions ---");
        Console.Write("Do you focus on UI/UX or graphic design? (uiux/graphic/both): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "uiux");
        Console.Write("Do you need prototyping capabilities? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "yes");
        Console.Write("Do you prefer browser-based or desktop tools? (browser/desktop): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "browser");
    }

    public override List<string> GetRecommendations()
    {
        var specialty = Answers[0];
        var needPrototyping = Answers[1];
        var platform = Answers[2];

        var tools = new List<string>();

        if (specialty == "uiux" || specialty == "both")
        {
            if (platform == "browser")
            {
                tools.Add("Figma — browser-first UI/UX design with real-time collaboration");
                tools.Add("Penpot — free, open-source Figma alternative");
            }
            else
            {
                tools.Add("Sketch — macOS native UI design tool with robust plugin ecosystem");
                tools.Add("Adobe XD — vector-based UI/UX with prototyping built in");
            }

            if (needPrototyping == "yes")
            {
                tools.Add("Framer — interactive prototypes with code-like precision");
                tools.Add("ProtoPie — advanced prototyping without code");
            }
        }

        if (specialty == "graphic" || specialty == "both")
        {
            tools.Add("Adobe Illustrator — vector graphics for logos, icons, branding");
            tools.Add("Affinity Designer — one-time purchase alternative to Illustrator");
            tools.Add("Canva — quick social media graphics and templates");
        }

        if (platform == "browser")
            tools.Add("Photopea — browser-based Photoshop alternative for quick edits");

        return tools;
    }
}
