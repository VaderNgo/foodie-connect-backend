﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using foodie_connect_backend.Shared.Classes;
using foodie_connect_backend.Shared.Enums;

namespace foodie_connect_backend.Data;

public class Restaurant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required][MaxLength(64)] public string Name { get; set; } = null!;

    [Required] public int OpenTime { get; set; }

    [Required] public int CloseTime { get; set; }

    [Required][MaxLength(256)] public string Address { get; set; } = null!;

    public RestaurantStatus Status { get; set; } = RestaurantStatus.Open;

    public ICollection<SocialLink> SocialLinks { get; set; } = new List<SocialLink>();

    [Required]
    [MinLength(10)]
    [MaxLength(10)]
    public string Phone { get; set; } = null!;

    [MaxLength(64)] public IList<string> Images { get; set; } = new List<string>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? AreaId { get; set; }

    [JsonIgnore]
    public virtual Area? Area { get; set; }

    [JsonIgnore]
    public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    [JsonIgnore]
    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
    [JsonIgnore]
    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public string HeadId { get; set; } = null!;
}