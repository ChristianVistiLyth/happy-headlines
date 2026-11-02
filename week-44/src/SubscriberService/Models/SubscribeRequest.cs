using System.ComponentModel.DataAnnotations;

namespace SubscriberService.Models;

/// <summary>What a reader sends to subscribe. Id and timestamp are set by the service.</summary>
public record SubscribeRequest([Required, EmailAddress, MaxLength(254)] string Email);
