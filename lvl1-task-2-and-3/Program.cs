using Task2.Services;
using System.Text.RegularExpressions;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var engine = new RecommendationEngine();

//i found this useful func on the internet
static Boolean isAlpha(string strToCheck)
{
    Regex rg = new Regex(@"^[a-zA-Z\s,]*$");
    return rg.IsMatch(strToCheck);
}

while (true)
{
    Console.WriteLine("===== Tool Recommender =====");
// name
    Console.Write("Enter your name: ");
    var name = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(name)) continue;
    if (!isAlpha(name)) continue;

//age
    Console.Write("Enter your age: ");
    if (!int.TryParse(Console.ReadLine(), out var age)) continue;
    if (!(0<=age && age<=100)) continue;


//profession
    Console.Write("Enter your profession (Student/Teacher/Developer/Artist/Designer/Writer/Manager): ");
    var profession = Console.ReadLine()?.Trim().ToLower();
    if (string.IsNullOrWhiteSpace(profession)) continue;
    if (!isAlpha(profession)) continue;

//pick the correct identity
    var user = UserFactory.Create(name, age, profession);

//start the engine
    engine.Process(user);

    Console.Write("\nRun again? (y/n): ");
    var again = Console.ReadLine()?.Trim().ToLower();
    if (again != "y" && again != "yes") break;

    Console.WriteLine();
}
