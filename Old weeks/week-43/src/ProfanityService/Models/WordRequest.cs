using System.ComponentModel.DataAnnotations;

namespace ProfanityService.Models;

public record WordRequest([Required, MaxLength(50)] string Text);
