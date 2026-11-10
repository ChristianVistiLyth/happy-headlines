using Microsoft.AspNetCore.Mvc;
using NewsletterService.Newsletters;

namespace NewsletterService.Controllers;

[ApiController]
[Route("api/newsletters")]
public class NewslettersController(DailyNewsletter dailyNewsletter) : ControllerBase
{
    /// <summary>Sends the daily newsletter now, instead of waiting for the daily timer (handy for the demo).</summary>
    [HttpPost("daily")]
    public async Task<IActionResult> SendDaily(CancellationToken cancellationToken)
    {
        var articleCount = await dailyNewsletter.SendAsync(cancellationToken);
        return Ok(new { articleCount });
    }
}
