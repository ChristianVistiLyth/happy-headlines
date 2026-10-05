using System.Text.Json.Serialization;

namespace PublisherService.Models;

/// <summary>PublisherService's own copy of the eight regions; an unknown region gives 400 Bad Request.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Region>))]
public enum Region
{
    Africa,
    Antarctica,
    Asia,
    Europe,
    NorthAmerica,
    Oceania,
    SouthAmerica,
    Global
}
