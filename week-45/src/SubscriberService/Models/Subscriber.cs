using System.ComponentModel.DataAnnotations;

namespace SubscriberService.Models;

/// <summary>A reader who receives the newsletter.</summary>
public class Subscriber
{
    public Guid Id { get; set; }

    [MaxLength(254)]
    public required string Email { get; set; }

    public DateTime SubscribedAt { get; set; }
}
