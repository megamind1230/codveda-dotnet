namespace Task2.Exceptions;

public class UnknownProfessionException(string profession)
    : Exception($"Unknown profession: '{profession}'")
{
    public string Profession { get; } = profession;
}
