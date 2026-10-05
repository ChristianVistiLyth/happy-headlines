using System.ComponentModel.DataAnnotations;

namespace ProfanityService.Models;

public record FilterRequest([Required] string Text);
