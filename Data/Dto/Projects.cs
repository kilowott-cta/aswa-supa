namespace DataBasic.Dto;


public class Project
{
    public int Id { get; set; }
    public string? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Designers { get; set; }
    public string? Analysts { get; set; }
    public string? Architects { get; set; }
    public string? Skillsets { get; set; }
    public string? Tags { get; set; }
    public string? Status { get; set; }
    public string? BallparkHours { get; set; }
    public string? SoldHours { get; set; }
    public string? ClientName { get; set; }
    public string? AccountManager { get; set; }
    public string? PresalesPriority { get; set; }
    public string? Owner { get; set; }
    public string? Stage { get; set; }
}