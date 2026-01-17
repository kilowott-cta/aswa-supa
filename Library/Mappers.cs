namespace DomainBasic.Models;

public static class Mapper
{
    public static Dbo.Project ToDboFromDto(this Dto.Project p)
    {
        return new Dbo.Project
        {
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectName,
            Stage = p.Stage,
            Status = p.Status,
            ClientName = p.ClientName,
            AccountManager = p.AccountManager,
            Designers= p.Designers,
            Architects = p.Architects,
            Analysts = p.Analysts,
            Tags = p.Tags,
            SoldHours = p.SoldHours,
            BallparkHours = p.BallparkHours,
            Owner = p.Owner,
            PresalesPriority = p.PresalesPriority,
            Skillsets = p.Skillsets,
            IsLatest= true,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static Dto.Project ToDtoFromDbo(this Dbo.Project p)
    {
        return new Dto.Project
        {
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectName,
            Stage = p.Stage,
            Status = p.Status,
            ClientName = p.ClientName,
            AccountManager = p.AccountManager,
            Designers= p.Designers,
            Architects = p.Architects,
            Analysts = p.Analysts,
            Tags = p.Tags,
            SoldHours = p.SoldHours,
            BallparkHours = p.BallparkHours,
            Owner = p.Owner,
            PresalesPriority = p.PresalesPriority,
            Skillsets = p.Skillsets,
            IsLatest= p.IsLatest,
            UpdatedAt = p.UpdatedAt,
            CreatedAt = p.CreatedAt
        };
    }

    public static Dto.Project ToDtoFromDict(this IDictionary<string, object?> p)
    {
        return new Dto.Project
        {
            ProjectId= p["PROJECT ID"]?.ToString(),
            ProjectName = p["PROJECT NAME"]?.ToString(),
            Stage = p["STAGE"]?.ToString(),
            Status = p["STATUS"]?.ToString(),
            ClientName = p["CLIENT NAME"]?.ToString(),
            AccountManager = p["ACCOUNT MANAGER"]?.ToString(),
            Designers= p["DESIGNERS"]?.ToString(),
            Architects = p["ARCHITECTS"]?.ToString(),
            Analysts = p["ANALYSTS"]?.ToString(),
            Tags = p["TAGS"]?.ToString(),
            SoldHours = p["SOLD HOURS 2.0"]?.ToString(),
            BallparkHours = p["BALLPARK HOURS 2.0"]?.ToString(),
            Owner = p["OWNER"]?.ToString(),
            PresalesPriority = p["PRESALES PRIORITY"]?.ToString(),
            Skillsets = p["SKILLSETS"]?.ToString()
        };
    }
}