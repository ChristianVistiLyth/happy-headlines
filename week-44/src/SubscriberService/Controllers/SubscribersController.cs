using System.Diagnostics;
using EasyNetQ;
using Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Npgsql;
using SubscriberService.Data;
using SubscriberService.Models;

namespace SubscriberService.Controllers;

/// <summary>
/// Subscribe, unsubscribe and list subscribers. Logging policy: an email address is personal data,
/// so only subscriber ids are logged - never the email address.
/// </summary>
[ApiController]
[Route("api/subscribers")]
public class SubscribersController(SubscriberDbContext db, IBus bus, ILogger<SubscribersController> logger) : ControllerBase
{
    // NewsletterService asks for this list before it sends a newsletter
    [HttpGet]
    public async Task<List<Subscriber>> GetAll() =>
        await db.Subscribers.AsNoTracking().OrderBy(s => s.SubscribedAt).ToListAsync();

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Subscriber>> Get(Guid id)
    {
        var subscriber = await db.Subscribers.FindAsync(id);
        if (subscriber is null)
        {
            logger.LogWarning("Subscriber {SubscriberId} not found", id);
            return NotFound();
        }

        return subscriber;
    }

    // An invalid email address never gets here: [ApiController] answers 400 because of [EmailAddress]
    [HttpPost]
    public async Task<ActionResult<Subscriber>> Subscribe(SubscribeRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();     // "Ann@Mail.dk" and "ann@mail.dk" are the same reader
        if (await db.Subscribers.AnyAsync(s => s.Email == email))
        {
            logger.LogWarning("Subscribe refused: the email address is already subscribed");
            return Conflict();
        }

        var subscriber = new Subscriber { Id = Guid.NewGuid(), Email = email, SubscribedAt = DateTime.UtcNow };
        db.Subscribers.Add(subscriber);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Safety net: two requests with the same address at the same moment both passed the check above,
            // and the unique index on Email refused the second one
            logger.LogWarning("Subscribe refused: the email address was subscribed at the same moment");
            return Conflict();
        }

        logger.LogInformation("Subscriber {SubscriberId} subscribed", subscriber.Id);

        await PublishSubscribedAsync(subscriber, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = subscriber.Id }, subscriber);
    }

    /// <summary>
    /// Puts the new subscriber on the SubscriberQueue, so NewsletterService sends a welcome mail.
    /// The queue is NOT on the critical path: if RabbitMQ is down, the subscriber is still saved and the reader
    /// still gets 201 - only the welcome mail is missing.
    /// </summary>
    private async Task PublishSubscribedAsync(Subscriber subscriber, CancellationToken cancellationToken)
    {
        var message = new SubscriberSubscribed { SubscriberId = subscriber.Id, Email = subscriber.Email };
        try
        {
            using var activity = Tracing.Source.StartActivity("publish subscriber", ActivityKind.Producer);
            activity?.SetTag("subscriber.id", subscriber.Id);

            // The trace context goes INTO the message, so NewsletterService continues this trace
            Tracing.Inject(activity, message.Headers);
            await bus.PubSub.PublishAsync(message, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Subscriber {SubscriberId} saved, but no welcome mail: the SubscriberQueue is unavailable ({Reason})",
                subscriber.Id, e.GetType().Name);
        }
    }

    // Unsubscribe: the row is deleted, nothing about the reader is kept
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unsubscribe(Guid id)
    {
        var deleted = await db.Subscribers.Where(s => s.Id == id).ExecuteDeleteAsync();
        if (deleted == 0)
        {
            logger.LogWarning("Subscriber {SubscriberId} not found, nothing deleted", id);
            return NotFound();
        }

        logger.LogInformation("Subscriber {SubscriberId} unsubscribed", id);
        return NoContent();
    }
}
