namespace BlazorBasic.Entities;

using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table(tableName: "projects")]
public class Project : BaseModel
{
    [PrimaryKey(columnName: "id", shouldInsert: false)]
    public int Id { get; set; }

    [Column(columnName: "project_id")]
    [JsonPropertyName(name: "project_id")]
    public string? ProjectId { get; set; }

    [Column(columnName: "project_name")]
    [JsonPropertyName(name: "project_name")]
    public string? ProjectName { get; set; }

    [Column(columnName: "created_at")]
    [JsonPropertyName(name: "created_at")]
    public DateTime CreatedAt { get; set; }

    [Column(columnName: "updated_at")]
    [JsonPropertyName(name: "updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column(columnName: "designers")]
    [JsonPropertyName(name: "designers")]
    public string? Designers { get; set; }
    [Column(columnName: "analysts")]
    [JsonPropertyName(name: "analysts")]
    public string? Analysts { get; set; }
    [Column(columnName: "architects")]
    [JsonPropertyName(name: "architects")]
    public string? Architects { get; set; }
    [Column(columnName: "skillsets")]
    [JsonPropertyName(name: "skillsets")]
    public string? Skillsets { get; set; }
    [Column(columnName: "tags")]
    [JsonPropertyName(name: "tags")]
    public string? Tags { get; set; }
    [Column(columnName: "status")]
    [JsonPropertyName(name: "status")]
    public string? Status { get; set; }
    [Column(columnName: "ballpark_hours_2")]
    [JsonPropertyName(name: "ballpark_hours_2")]
    public string? BallparkHours { get; set; }
    [Column(columnName: "sold_hours_2")]
    [JsonPropertyName(name: "sold_hours_2")]
    public string? SoldHours { get; set; }

    [Column(columnName: "client_name")]
    [JsonPropertyName(name: "client_name")]
    public string? ClientName { get; set; }

    [Column(columnName: "account_manager")]
    [JsonPropertyName(name: "account_manager")]
    public string? AccountManager { get; set; }

    [Column(columnName: "presales_priority")]
    [JsonPropertyName(name: "presales_priority")]
    public string? PresalesPriority { get; set; }

    [Column(columnName: "owner")]
    [JsonPropertyName(name: "owner")]
    public string? Owner { get; set; }

    [Column(columnName: "stage")]
    [JsonPropertyName(name: "stage")]
    public string? Stage { get; set; }
}