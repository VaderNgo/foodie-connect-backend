using foodie_connect_backend.Data;
using foodie_connect_backend.Shared.Enums;

namespace foodie_connect_backend.Shared.Classes;

public record SocialLink
{
    public int Id { get; set; }
    
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    
    public SocialMediaPlatform Platform { get; init; }
    public string Url { get; init; } = String.Empty;
}