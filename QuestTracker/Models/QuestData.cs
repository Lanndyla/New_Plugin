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
    
    [JsonPropertyName("ClassJobCategory0")]
    public ClassJobCategoryData? ClassJobCategory { get; set; }
    
    // For building the quest tree
    public List<QuestData> NextQuests { get; set; } = new();
    public List<QuestData> PreviousQuests { get; set; } = new();
    
    // Helper property to determine quest category
    public QuestCategory Category
    {
        get
        {
            if (JournalGenre == null) return QuestCategory.Other;
            
            // Main Scenario Quests (IDs 1-13 are MSQ categories)
            if (JournalGenre.Id >= 1 && JournalGenre.Id <= 13)
                return QuestCategory.MainScenario;
            
            // Raid Quests (14-30 range includes raids)
            if ((JournalGenre.Id >= 14 && JournalGenre.Id <= 30) || 
                JournalGenre.Name.Contains("Raid") || 
                JournalGenre.Name.Contains("Bahamut") ||
                JournalGenre.Name.Contains("Alexander") ||
                JournalGenre.Name.Contains("Omega") ||
                JournalGenre.Name.Contains("Eden") ||
                JournalGenre.Name.Contains("Pandaemonium") ||
                JournalGenre.Name.Contains("Arcadion"))
                return QuestCategory.Raids;
            
            // Job/Class Quests
            if (JournalGenre.Name.Contains("Quests") && 
                (JournalGenre.Name.Contains("Gladiator") || JournalGenre.Name.Contains("Paladin") ||
                 JournalGenre.Name.Contains("Pugilist") || JournalGenre.Name.Contains("Monk") ||
                 JournalGenre.Name.Contains("Marauder") || JournalGenre.Name.Contains("Warrior") ||
                 JournalGenre.Name.Contains("Lancer") || JournalGenre.Name.Contains("Dragoon") ||
                 JournalGenre.Name.Contains("Archer") || JournalGenre.Name.Contains("Bard") ||
                 JournalGenre.Name.Contains("Rogue") || JournalGenre.Name.Contains("Ninja") ||
                 JournalGenre.Name.Contains("Conjurer") || JournalGenre.Name.Contains("White Mage") ||
                 JournalGenre.Name.Contains("Thaumaturge") || JournalGenre.Name.Contains("Black Mage") ||
                 JournalGenre.Name.Contains("Arcanist") || JournalGenre.Name.Contains("Summoner") || JournalGenre.Name.Contains("Scholar") ||
                 JournalGenre.Name.Contains("Dark Knight") || JournalGenre.Name.Contains("Machinist") || JournalGenre.Name.Contains("Astrologian") ||
                 JournalGenre.Name.Contains("Samurai") || JournalGenre.Name.Contains("Red Mage") ||
                 JournalGenre.Name.Contains("Gunbreaker") || JournalGenre.Name.Contains("Dancer") ||
                 JournalGenre.Name.Contains("Reaper") || JournalGenre.Name.Contains("Sage") ||
                 JournalGenre.Name.Contains("Viper") || JournalGenre.Name.Contains("Pictomancer") ||
                 JournalGenre.Name.Contains("Carpenter") || JournalGenre.Name.Contains("Blacksmith") ||
                 JournalGenre.Name.Contains("Armorer") || JournalGenre.Name.Contains("Goldsmith") ||
                 JournalGenre.Name.Contains("Leatherworker") || JournalGenre.Name.Contains("Weaver") ||
                 JournalGenre.Name.Contains("Alchemist") || JournalGenre.Name.Contains("Culinarian") ||
                 JournalGenre.Name.Contains("Miner") || JournalGenre.Name.Contains("Botanist") || JournalGenre.Name.Contains("Fisher")))
                return QuestCategory.JobClass;
            
            // Tribal/Beast Tribe Quests
            if (JournalGenre.Name.Contains("Tribal") || JournalGenre.Name.Contains("Beast Tribe"))
                return QuestCategory.Tribal;
            
            // Feature Quests (Hildibrand, Postmoogle, etc.)
            if (JournalGenre.Name.Contains("Hildibrand") || 
                JournalGenre.Name.Contains("Postmoogle") ||
                JournalGenre.Name.Contains("Scholasticate") ||
                JournalGenre.Name.Contains("Four Lords") ||
                JournalGenre.Name.Contains("Weapon") ||
                JournalGenre.Name.Contains("Bozja") ||
                JournalGenre.Name.Contains("Eureka"))
                return QuestCategory.Feature;
            
            // Side Quests (anything with "Sidequests" in the name)
            if (JournalGenre.Name.Contains("Sidequests") || JournalGenre.Name.Contains("Side Quests"))
                return QuestCategory.Side;
            
            return QuestCategory.Other;
        }
    }
}

public enum QuestCategory
{
    MainScenario,
    Side,
    JobClass,
    Raids,
    Tribal,
    Feature,
    Other
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

public class ClassJobCategoryData
{
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
