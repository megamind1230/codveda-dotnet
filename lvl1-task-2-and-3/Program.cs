using Task2.Services;
using Task2.Exceptions;
using System.Text.RegularExpressions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    // .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Console.OutputEncoding = System.Text.Encoding.UTF8; //to even accept non-ascii input

try
{
    static bool IsAlpha(string strToCheck)
    {
        return new Regex(@"^[a-zA-Z\s,]*$").IsMatch(strToCheck);
    }
    Log.Information("Application started");
    var engine = new RecommendationEngine();


    while (true)
    {
        Console.WriteLine("===== WE RECOMMEND YOU APPS =====");

        //name
        Console.Write("Enter your name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name)) continue;
        if (!IsAlpha(name)) continue;

        //age
        Console.Write("Enter your age: ");
        if (!int.TryParse(Console.ReadLine(), out var age)) continue;
        if (age < 0 || age > 100) continue;

        //profession
        Console.Write("Enter your profession (Student/Teacher/Developer/Artist/Designer/Writer/Manager): ");
        var profession = Console.ReadLine()?.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(profession)) continue;
        if (!IsAlpha(profession)) continue;

        Log.Information("User input — Name: {Name}, Age: {Age}, Profession: {Profession}", name, age, profession);

        try {
            var user = UserFactory.Create(name, age, profession);
            Log.Debug("Created {UserType} for {Name}", user.GetType().Name, name);

            Log.Information("Processing user {Name}", name);
            engine.Process(user);
        }
        catch (UnknownProfessionException ex) {
            Log.Warning(ex, "Invalid profession attempted: {Profession}", ex.Profession);
            Console.WriteLine($"Unknown profession '{ex.Profession}'. Please pick from listed.");
        }
        catch (Exception ex) {
            Log.Error(ex, "Error processing user {Name}", name);
            Console.WriteLine("Something went wrong. Please try again.");
        }

        Console.Write("\nRun again? (y/n): ");
        var again = Console.ReadLine()?.Trim().ToLower();
        if (again != "y" && again != "yes") break;

        Console.WriteLine(); 
        Console.WriteLine(); 
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
