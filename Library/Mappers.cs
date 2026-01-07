namespace DomainBasic.Models;

public static class Mapper
{
    public static Dto.Project ToDto(this Dbo.Project p)
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
            UpdatedAt = p.UpdatedAt,
            CreatedAt = p.CreatedAt
        };
    }
}