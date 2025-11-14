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
    private readonly Dictionary<int, List<QuestData>> questsByExpansion = new();
    private bool isLoading = false;
    private bool isLoaded = false;
    
    private const string XIVAPI_BASE = "https://xivapi.com";
    
    public bool IsLoading => isLoading;
    public bool IsLoaded => isLoaded;
    
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
            
            for (int page = 2; page <= Math.Min(totalPages, 100); page++) // Limit to 100 pages for now
            {
                var pageData = await FetchQuestPageAsync(page);
                if (pageData?.Results != null)
                {
                    allQuests.AddRange(pageData.Results);
                }
                
                // Small delay to avoid rate limiting
                await Task.Delay(100);
            }
            
            log.Info($"Fetched {allQuests.Count} quest summaries");
            
            // Now fetch detailed data for each quest (in batches)
            var batchSize = 50;
            for (int i = 0; i < Math.Min(allQuests.Count, 500); i += batchSize) // Limit to 500 quests for testing
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
                        if (!questsByExpansion.ContainsKey(expansionId))
                        {
                            questsByExpansion[expansionId] = new List<QuestData>();
                        }
                        questsByExpansion[expansionId].Add(quest);
                    }
                }
                
                log.Info($"Loaded {questCache.Count} quests so far...");
                await Task.Delay(500); // Delay between batches
            }
            
            // Build quest relationships
            BuildQuestRelationships();
            
            isLoaded = true;
            log.Info($"Finished loading {questCache.Count} quests");
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
            var url = $"{XIVAPI_BASE}/Quest/{questId}";
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
    
    public List<QuestData> GetQuestsByExpansion(int expansionId)
    {
        return questsByExpansion.TryGetValue(expansionId, out var quests) 
            ? quests 
            : new List<QuestData>();
    }
    
    public List<QuestData> GetRootQuests(int expansionId)
    {
        var quests = GetQuestsByExpansion(expansionId);
        return quests.Where(q => q.PreviousQuests.Count == 0).ToList();
    }
    
    public void Dispose()
    {
        httpClient?.Dispose();
    }
}
