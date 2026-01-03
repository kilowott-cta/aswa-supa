using System.ComponentModel.DataAnnotations;
using BlazorBasic.Entities;

namespace BlazorBasic.Services;

public static class AggregationService
{
    public static Dictionary<string, int> GetOccurenceCountsFromZohoProjectsEntities(IEnumerable<Project> rows, string field)
    {
        Dictionary<string, int> items = new();
        rows.ToList().ForEach(r =>
        {
            var values = field switch
            {
                "Skills" => r.Skillsets,
                "Tags" => r.Tags,
                "Designers" => r.Designers,
                "Analysts" => r.Analysts,
                "Architects" => r.Architects,
                "ProjectStatus" => r.Status,
                "ProjectStage" => r.Stage,
                "BallparkHours" => r.BallparkHours, // this is not correct, needs to change
                _ => string.Empty
            };
            if (values is null) return;
            var splitValues = values.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            foreach (var value in splitValues)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (value.Length == 1) continue;  // ignore values with just '-'
                var cleanValue = value.Replace("\"", String.Empty); // remove '"' from values
                if (!items.Keys.Contains(cleanValue))
                {
                    items.Add(cleanValue, 1);  // add first item
                }
                else 
                {
                    items[cleanValue]++;
                }
            }
        });
        return items;
    }

    public static int CountNonEmptyField(List<Entities.Project> projects, Func<Entities.Project, string?> fieldSelector)
    {
        return projects.Count(p => !string.IsNullOrWhiteSpace(fieldSelector(p)));
    }
}