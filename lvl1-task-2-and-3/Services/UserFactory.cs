namespace Task2.Services;
using Task2.Models;


public static class UserFactory
{
    public static User Create(string name, int age, string profession)
    {
        return profession switch
        {
            "student" => new Student(name, age, profession),
            "teacher" => new Teacher(name, age, profession),
            "developer" or "dev" => new Developer(name, age, profession),
            "artist" => new Artist(name, age, profession),
            "designer" => new Designer(name, age, profession),
            "writer" or "author" => new Writer(name, age, profession),
            "manager" => new Manager(name, age, profession),
            _ => new User(name, age, profession)
        };
    }
}
