namespace Task2.Models;

public class User
{
    //private fields {encapsulation}
    private string _name = string.Empty;
    private int _age;
    private string _profession = string.Empty;

    //with public properties to set them up
    //init to only set them once
    public string Name
    {
        get => _name;
        init { _name = value; }
    }
    public int Age
    {
        get => _age;
        init { _age = value; }
    }
    public string Profession
    {
        get => _profession;
        init { _profession = value; }
    }

    protected readonly List<string> Answers = [];

    public User(string name, int age, string profession)
    {
        Name = name;
        Age = age;
        Profession = profession;
    }

    public virtual void AskFollowUpQuestions()
    {
        Console.WriteLine("\n--- General Questions ---");
        Console.Write("Do you prefer working with others? (yes/no): ");
        Answers.Add(Console.ReadLine()?.Trim().ToLower() ?? "no");
    }

    public virtual List<string> GetRecommendations()
    {
        return ["Try exploring different tools based on your interests!"];
    }
}
