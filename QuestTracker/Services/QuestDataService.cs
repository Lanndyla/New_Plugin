using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using QuestTracker.Models;

namespace QuestTracker.Services;

public class QuestDataService : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly IPluginLog log;
    private readonly Dictionary<int, QuestData> questCache = new();
    private readonly Dictionary<int, Dictionary<QuestCategory, List<QuestData>>> questsByExpansionAndCategory = new();
    private bool isLoading = false;
    private bool isLoaded = false;
    private int totalQuestsLoaded = 0;
    
    private const string XIVAPI_BASE = "https://xivapi.com";
    
    public bool IsLoading => isLoading;
    public bool IsLoaded => isLoaded;
    public int TotalQuestsLoaded => totalQuestsLoaded;
    
    public QuestDataService(IPluginLog pluginLog)
    {
        log = pluginLog;
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
    
    public async Task LoadAllQuestsAsync()
    {
        if (isLoading || isLoaded) return;
        
        isLoading = true;
        log.Info("Starting to load quest data from XIVAPI...");
        
        try
        {
            // First, get the total number of quests
            var firstPage = await FetchQuestPageAsync(1);
            if (firstPage?.Pagination == null)
            {
                log.Error("Failed to fetch first page of quests");
                return;
            }
            
            var totalPages = firstPage.Pagination.PageTotal;
            log.Info($"Total pages to fetch: {totalPages}");
            
            // Fetch all pages
            var allQuests = new List<QuestSummary>(firstPage.Results);
            
            // Fetch all pages (not just 100)
            for (int page = 2; page <= totalPages; page++)
            {
                var pageData = await FetchQuestPageAsync(page);
                if (pageData?.Results != null)
                {
                    allQuests.AddRange(pageData.Results);
                }
                
                // Small delay to avoid rate limiting
                await Task.Delay(50);
                
                if (page % 10 == 0)
                {
                    log.Info($"Fetched {page}/{totalPages} pages...");
                }
            }
            
            log.Info($"Fetched {allQuests.Count} quest summaries, now loading details...");
            
            // Now fetch detailed data for each quest (in batches)
            var batchSize = 50;
            for (int i = 0; i < allQuests.Count; i += batchSize)
            {
                var batch = allQuests.Skip(i).Take(batchSize);
                var tasks = batch.Select(q => FetchQuestDetailAsync(q.Id));
                var results = await Task.WhenAll(tasks);
                
                foreach (var quest in results.Where(q => q != null))
                {
                    if (quest != null && !string.IsNullOrEmpty(quest.Name))
                    {
                        questCache[quest.Id] = quest;
                        
                        var expansionId = quest.Expansion?.Id ?? 0;
                        var category = quest.Category;
                        
                        if (!questsByExpansionAndCategory.ContainsKey(expansionId))
                        {
                            questsByExpansionAndCategory[expansionId] = new Dictionary<QuestCategory, List<QuestData>>();
                        }
                        
                        if (!questsByExpansionAndCategory[expansionId].ContainsKey(category))
                        {
                            questsByExpansionAndCategory[expansionId][category] = new List<QuestData>();
                        }
                        
                        questsByExpansionAndCategory[expansionId][category].Add(quest);
                        totalQuestsLoaded++;
                    }
                }
                
                if ((i / batchSize) % 10 == 0)
                {
                    log.Info($"Loaded {totalQuestsLoaded} quests so far...");
                }
                
                await Task.Delay(200); // Delay between batches
            }
            
            // Build quest relationships
            BuildQuestRelationships();
            
            isLoaded = true;
            log.Info($"Finished loading {totalQuestsLoaded} quests");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Error loading quest data");
        }
        finally
        {
            isLoading = false;
        }
    }
    
    private async Task<QuestListResponse?> FetchQuestPageAsync(int page)
    {
        try
        {
            var url = $"{XIVAPI_BASE}/Quest?page={page}&limit=100";
            var response = await httpClient.GetStringAsync(url);
            return JsonSerializer.Deserialize<QuestListResponse>(response);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Error fetching quest page {page}");
            return null;
        }
    }
    
    private async Task<QuestData?> FetchQuestDetailAsync(int questId)
    {
        try
        {
            var url = $"{XIVAPI_BASE}/Quest/{questId}?columns=ID,Name,Icon,Expansion.ID,Expansion.Name,PreviousQuest0,PreviousQuest1,PreviousQuest2,ClassJobLevel0,JournalGenre.ID,JournalGenre.Name,ClassJobCategory0.Name";
            var response = await httpClient.GetStringAsync(url);
            return JsonSerializer.Deserialize<QuestData>(response);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Error fetching quest {questId}");
            return null;
        }
    }
    
    private void BuildQuestRelationships()
    {
        foreach (var quest in questCache.Values)
        {
            // Link previous quests
            if (quest.PreviousQuest0 > 0 && questCache.TryGetValue(quest.PreviousQuest0, out var prev0))
            {
                quest.PreviousQuests.Add(prev0);
                prev0.NextQuests.Add(quest);
            }
            if (quest.PreviousQuest1 > 0 && questCache.TryGetValue(quest.PreviousQuest1, out var prev1))
            {
                quest.PreviousQuests.Add(prev1);
                prev1.NextQuests.Add(quest);
            }
            if (quest.PreviousQuest2 > 0 && questCache.TryGetValue(quest.PreviousQuest2, out var prev2))
            {
                quest.PreviousQuests.Add(prev2);
                prev2.NextQuests.Add(quest);
            }
        }
    }
    
    public List<QuestData> GetQuestsByExpansionAndCategory(int expansionId, QuestCategory category)
    {
        if (questsByExpansionAndCategory.TryGetValue(expansionId, out var categories))
        {
            if (categories.TryGetValue(category, out var quests))
            {
                return quests;
            }
        }
        return new List<QuestData>();
    }
    
    public List<QuestData> GetRootQuests(int expansionId, QuestCategory category)
    {
        var quests = GetQuestsByExpansionAndCategory(expansionId, category);
        return quests.Where(q => q.PreviousQuests.Count == 0).ToList();
    }
    
    public Dictionary<QuestCategory, int> GetQuestCountsByCategory(int expansionId)
    {
        var counts = new Dictionary<QuestCategory, int>();
        
        if (questsByExpansionAndCategory.TryGetValue(expansionId, out var categories))
        {
            foreach (var kvp in categories)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
        }
        return counts;
    }
        
    public List<QuestData> GetRootQuestsSortedByLevel(int expansionId, QuestCategory category)
    {
        var quests = GetRootQuests(expansionId, category);
        return quests.OrderBy(q => q.Level).ToList();
    }
    
    public List<int> GetAllQuestIds()
    {
        return questCache.Keys.ToList();
}
    
    public void Dispose()
    {
        httpClient?.Dispose();
    }
}
