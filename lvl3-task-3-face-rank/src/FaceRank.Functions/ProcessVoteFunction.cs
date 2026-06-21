using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FaceRank.Core.Services;

namespace FaceRank.Functions;

public static class ProcessVoteFunction
{
    public class VoteRequest
    {
        public int WinnerRating { get; set; }
        public int LoserRating { get; set; }
    }

    [FunctionName("ProcessVote")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Processing vote request");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<VoteRequest>(requestBody);

        if (data == null)
        {
            return new BadRequestObjectResult("Pass winnerRating and loserRating in the request body");
        }

        var (newWinner, newLoser) = EloService.Calculate(data.WinnerRating, data.LoserRating);

        return new OkObjectResult(new
        {
            newWinnerRating = newWinner,
            newLoserRating = newLoser,
            winnerDelta = newWinner - data.WinnerRating,
            loserDelta = newLoser - data.LoserRating
        });
    }
}
