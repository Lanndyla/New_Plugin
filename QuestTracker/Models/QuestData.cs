using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuestTracker.Models;

public class QuestData
{
    [JsonPropertyName("ID")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Icon")]
    public string Icon { get; set; } = string.Empty;
    
    [JsonPropertyName("Expansion")]
    public ExpansionData? Expansion { get; set; }
    
    [JsonPropertyName("PreviousQuest0")]
    public int PreviousQuest0 { get; set; }
    
    [JsonPropertyName("PreviousQuest1")]
    public int PreviousQuest1 { get; set; }
    
    [JsonPropertyName("PreviousQuest2")]
    public int PreviousQuest2 { get; set; }
    
    [JsonPropertyName("ClassJobLevel0")]
    public int Level { get; set; }
    
    [JsonPropertyName("JournalGenre")]
    public JournalGenreData? JournalGenre { get; set; }
    
    // For building the quest tree
    public List<QuestData> NextQuests { get; set; } = new();
    public List<QuestData> PreviousQuests { get; set; } = new();
}

public class ExpansionData
{
    [JsonPropertyName("ID")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
}

public class JournalGenreData
{
    [JsonPropertyName("ID")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
}

public class QuestListResponse
{
    [JsonPropertyName("Pagination")]
    public PaginationData? Pagination { get; set; }
    
    [JsonPropertyName("Results")]
    public List<QuestSummary> Results { get; set; } = new();
}

public class QuestSummary
{
    [JsonPropertyName("ID")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Icon")]
    public string Icon { get; set; } = string.Empty;
}

public class PaginationData
{
    [JsonPropertyName("Page")]
    public int Page { get; set; }
    
    [JsonPropertyName("PageTotal")]
    public int PageTotal { get; set; }
    
    [JsonPropertyName("Results")]
    public int Results { get; set; }
    
    [JsonPropertyName("ResultsTotal")]
    public int ResultsTotal { get; set; }
}
