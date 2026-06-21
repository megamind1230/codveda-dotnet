namespace FaceRank.Core.Services;

public static class EloService
{
    private const int K = 32;

    public static (int newWinner, int newLoser) Calculate(int winnerRating, int loserRating)
    {
        double expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loserRating - winnerRating) / 400.0));
        double expectedLoser = 1.0 / (1.0 + Math.Pow(10, (winnerRating - loserRating) / 400.0));

        int newWinner = winnerRating + (int)(K * (1.0 - expectedWinner));
        int newLoser = loserRating + (int)(K * (0.0 - expectedLoser));

        return (newWinner, newLoser);
    }
}
