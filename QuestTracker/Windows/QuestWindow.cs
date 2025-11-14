using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using QuestTracker.Models;
using QuestTracker.Services;

namespace QuestTracker.Windows;

public class QuestWindow : Window, IDisposable
{
    private readonly QuestDataService questDataService;
    private int selectedExpansion = 0;
    
    private readonly string[] expansionNames = new[]
    {
        "A Realm Reborn",
        "Heavensward",
        "Stormblood",
        "Shadowbringers",
        "Endwalker",
        "Dawntrail"
    };
    
    public QuestWindow(QuestDataService questService) : base("Quest Tracker###QuestTrackerWindow")
    {
        questDataService = questService;
        
        Size = new Vector2(800, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
        
        Flags = ImGuiWindowFlags.None;
    }
    
    public override void Draw()
    {
        if (questDataService.IsLoading)
        {
            ImGui.Text("Loading quest data from XIVAPI...");
            ImGui.Text("This may take a minute on first load.");
            return;
        }
        
        if (!questDataService.IsLoaded)
        {
            ImGui.Text("Quest data not loaded yet.");
            if (ImGui.Button("Load Quest Data"))
            {
                _ = questDataService.LoadAllQuestsAsync();
            }
            return;
        }
        
        if (ImGui.BeginTabBar("ExpansionTabs"))
        {
            for (int i = 0; i < expansionNames.Length; i++)
            {
                if (ImGui.BeginTabItem(expansionNames[i]))
                {
                    selectedExpansion = i;
                    DrawQuestList(i);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }
    
    private void DrawQuestList(int expansionId)
    {
        var rootQuests = questDataService.GetRootQuests(expansionId);
        
        if (rootQuests.Count == 0)
        {
            ImGui.Text($"No quests found for {expansionNames[expansionId]}");
            return;
        }
        
        ImGui.Text($"Found {rootQuests.Count} root quests");
        ImGui.Separator();
        
        if (ImGui.BeginChild("QuestTreeView", new Vector2(0, 0), true))
        {
            foreach (var quest in rootQuests)
            {
                DrawQuestNode(quest, 0);
            }
            ImGui.EndChild();
        }
    }
    
    private void DrawQuestNode(QuestData quest, int depth)
    {
        var indent = depth * 20f;
        ImGui.Indent(indent);
        
        var hasChildren = quest.NextQuests.Count > 0;
        var flags = hasChildren ? ImGuiTreeNodeFlags.None : ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        
        var nodeOpen = ImGui.TreeNodeEx($"{quest.Name}###{quest.Id}", flags);
        
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), $"(Lv. {quest.Level})");
        
        if (nodeOpen && hasChildren)
        {
            foreach (var nextQuest in quest.NextQuests)
            {
                DrawQuestNode(nextQuest, depth + 1);
            }
            
            ImGui.TreePop();
        }
        
        ImGui.Unindent(indent);
    }
    
    public void Dispose()
    {
    }
}

