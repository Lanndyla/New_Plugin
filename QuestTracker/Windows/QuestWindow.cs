using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using QuestTracker.Models;
using QuestTracker.Services;

namespace QuestTracker.Windows;

public class QuestWindow : Window, IDisposable
{
    private readonly QuestDataService questDataService;
    private readonly IconService iconService;
    private readonly QuestCompletionService completionService;
    private int selectedExpansion = 0;
    private QuestCategory selectedCategory = QuestCategory.MainScenario;
    private bool completionDataLoaded = false;
    
    private readonly string[] expansionNames = new[]
    {
        "A Realm Reborn",
        "Heavensward",
        "Stormblood",
        "Shadowbringers",
        "Endwalker",
        "Dawntrail"
    };
    
    private readonly Dictionary<QuestCategory, string> categoryNames = new()
    {
        { QuestCategory.MainScenario, "Main Scenario" },
        { QuestCategory.Side, "Side Quests" },
        { QuestCategory.JobClass, "Job/Class Quests" },
        { QuestCategory.Raids, "Raids & Trials" },
        { QuestCategory.Tribal, "Tribal Quests" },
        { QuestCategory.Feature, "Feature Quests" },
        { QuestCategory.Other, "Other" }
    };
    
    public QuestWindow(QuestDataService service, IconService icons, QuestCompletionService completion) : base(
        "Quest Tracker", ImGuiWindowFlags.None)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(900, 700),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        questDataService = service;
        iconService = icons;
        completionService = completion;
        
        // Start loading quest data
        _ = questDataService.LoadAllQuestsAsync();
    }
    
    public override void Draw()
    {
        if (questDataService.IsLoading)
        {
            ImGui.Text($"Loading quest data from XIVAPI... ({questDataService.TotalQuestsLoaded} quests loaded)");
            ImGui.Text("This may take a few minutes on first load...");
            return;
        }
        
        if (!questDataService.IsLoaded)
        {
            ImGui.Text("Failed to load quest data. Please try again later.");
            if (ImGui.Button("Retry"))
            {
                _ = questDataService.LoadAllQuestsAsync();
            }
            return;
        }
        
        // Load completion data once quests are loaded
        if (!completionDataLoaded)
        {
            var questIds = questDataService.GetAllQuestIds();
            completionService.RefreshCompletedQuests(questIds);
            completionDataLoaded = true;
        }
        
        ImGui.Text($"Total quests loaded: {questDataService.TotalQuestsLoaded}");
        
        if (ImGui.Button("Refresh Completion Status"))
        {
            var questIds = questDataService.GetAllQuestIds();
            completionService.RefreshCompletedQuests(questIds);
        }
        
        ImGui.Separator();
        
        // Draw expansion tabs
        if (ImGui.BeginTabBar("ExpansionTabs"))
        {
            for (int i = 0; i < expansionNames.Length; i++)
            {
                if (ImGui.BeginTabItem(expansionNames[i]))
                {
                    selectedExpansion = i;
                    DrawExpansionContent(i);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }
    
    private void DrawExpansionContent(int expansionId)
    {
        // Get quest counts for this expansion
        var counts = questDataService.GetQuestCountsByCategory(expansionId);
        
        // Draw category selection buttons
        ImGui.Text("Quest Categories:");
        ImGui.Separator();
        
        foreach (var category in categoryNames.Keys)
        {
            var count = counts.ContainsKey(category) ? counts[category] : 0;
            if (count == 0) continue; // Skip empty categories
            
            var buttonLabel = $"{categoryNames[category]} ({count})";
            
            if (selectedCategory == category)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.8f, 1.0f));
            }
            
            if (ImGui.Button(buttonLabel, new Vector2(200, 30)))
            {
                selectedCategory = category;
            }
            
            if (selectedCategory == category)
            {
                ImGui.PopStyleColor();
            }
            
            ImGui.SameLine();
        }
        
        ImGui.NewLine();
        ImGui.Separator();
        
        // Draw quests for selected category
        DrawCategoryQuests(expansionId, selectedCategory);
    }
    
    private void DrawCategoryQuests(int expansionId, QuestCategory category)
    {
        var rootQuests = questDataService.GetRootQuestsSortedByLevel(expansionId, category);
        
        if (rootQuests.Count == 0)
        {
            ImGui.Text($"No {categoryNames[category]} quests found for {expansionNames[expansionId]}");
            return;
        }
        
        ImGui.Text($"Showing {rootQuests.Count} root quest chains (sorted by level)");
        ImGui.Separator();
        
        if (ImGui.BeginChild("QuestList"))
        {
            foreach (var quest in rootQuests)
            {
                DrawQuestNode(quest, 0);
            }
            ImGui.EndChild();
        }
    }
    
    private void DrawQuestNode(QuestData quest, int indent)
    {
        ImGui.Indent(indent * 20);
        
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth;
        
        bool hasChildren = quest.NextQuests.Count > 0;
        if (!hasChildren)
        {
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        }
        
        // Check if quest is completed
        bool isCompleted = completionService.IsQuestCompletedCached(quest.Id);
        
        // Get quest icon
        var icon = iconService.GetQuestIconSync(quest.Icon);
        
        // Draw icon if available
        if (icon != null)
        {
            ImGui.Image(icon.Handle, new Vector2(24, 24));
            ImGui.SameLine();
        }
        
        // Draw completion checkbox
        if (isCompleted)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 1.0f, 0.0f, 1.0f)); // Green for completed
            ImGui.Text("[✓]");
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1.0f)); // Gray for incomplete
            ImGui.Text("[ ]");
            ImGui.PopStyleColor();
        }
        ImGui.SameLine();
        
        var label = $"{quest.Name} (Lv.{quest.Level})";
        if (quest.JournalGenre != null && !string.IsNullOrEmpty(quest.JournalGenre.Name))
        {
            label += $" [{quest.JournalGenre.Name}]";
        }
        
        // Dim completed quests
        if (isCompleted)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
        }
        
        bool nodeOpen = ImGui.TreeNodeEx(label, flags);
        
        if (isCompleted)
        {
            ImGui.PopStyleColor();
        }
        
        if (hasChildren && nodeOpen)
        {
            // Sort child quests by level
            var sortedChildren = quest.NextQuests.OrderBy(q => q.Level).ToList();
            foreach (var nextQuest in sortedChildren)
            {
                DrawQuestNode(nextQuest, indent + 1);
            }
            ImGui.TreePop();
        }
        
        ImGui.Unindent(indent * 20);
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}
