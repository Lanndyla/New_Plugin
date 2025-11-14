using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace QuestTracker.Services;

public unsafe class QuestCompletionService
{
    private readonly IPluginLog log;
    private readonly HashSet<int> completedQuests = new();
    
    public QuestCompletionService(IPluginLog pluginLog)
    {
        log = pluginLog;
    }
    
    public bool IsQuestComplete(int questId)
    {
        try
        {
            var questManager = QuestManager.Instance();
            if (questManager == null)
            {
                return false;
            }
            
            // Check if quest is completed
            return QuestManager.IsQuestComplete((ushort)questId);
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Error checking quest completion for quest {questId}");
            return false;
        }
    }
    
    public void RefreshCompletedQuests(IEnumerable<int> questIds)
    {
        completedQuests.Clear();
        
        foreach (var questId in questIds)
        {
            if (IsQuestComplete(questId))
            {
                completedQuests.Add(questId);
            }
        }
        
        log.Info($"Refreshed completion status for {completedQuests.Count} completed quests");
    }
    
    public bool IsQuestCompletedCached(int questId)
    {
        return completedQuests.Contains(questId);
    }
}
