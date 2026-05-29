namespace Task2.Models;

public class Artist : User
{
    public Artist(string name, int age, string profession) : base(name, age, profession) { }

    public override void AskFollowUpQuestions()
    {
        Console.WriteLine("\n--- Artist Questions ---");
        Console.Write("Do you work digitally or traditionally? (digital/traditional/both): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "digital");
        Console.Write("Do you prefer vector or raster graphics? (vector/raster/both): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "raster");
        Console.Write("Do you need collaboration features? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "no");
    }

    public override List<string> GetRecommendations()
    {
        var workWay = Answers[0];
        var graphicsStyle = Answers[1];
        var needCollab = Answers[2];

        var tools = new List<string>();

        if (workWay == "digital" || workWay == "both")
        {
            if (graphicsStyle == "raster" || graphicsStyle == "both")
            {
                tools.Add("Procreate — industry-standard for digital painting on iPad");
                tools.Add("Photoshop — raster editing powerhouse for photo manipulation");
                tools.Add("Krita — free, open-source alternative for painting");
            }
            if (graphicsStyle == "vector" || graphicsStyle == "both")
            {
                tools.Add("Figma — vector design with browser-based collaboration");
                tools.Add("Adobe Illustrator — the gold standard for vector illustration");
                tools.Add("Inkscape — free vector editor for SVG work");
            }
        }

        if (workWay == "traditional" || workWay == "both")
        {
            tools.Add("Adobe Fresco — blends digital and traditional painting techniques");
            tools.Add("Rebelle 7 — simulates real watercolor and oil paints");
        }

        if (needCollab == "yes")
        {
            tools.Add("Figma — real-time collaborative design");
            tools.Add("Miro — mood boards and visual brainstorming");
        }

        return tools;
    }
}
