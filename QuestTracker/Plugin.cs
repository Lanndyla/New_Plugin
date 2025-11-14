using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using QuestTracker.Windows;
using QuestTracker.Services;

namespace QuestTracker;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    
    private const string CommandName = "/questtracker";
    
    public WindowSystem WindowSystem = new("QuestTracker");
    private QuestDataService questDataService;
    private QuestWindow questWindow;
    
    public Plugin()
    {
        questDataService = new QuestDataService(PluginLog);
        questWindow = new QuestWindow(questDataService);
        
        WindowSystem.AddWindow(questWindow);
        
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Quest Tracker window"
        });
        
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleQuestWindow;
    }
    
    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        questWindow.Dispose();
        questDataService.Dispose();
        CommandManager.RemoveHandler(CommandName);
    }
    
    private void OnCommand(string command, string args)
    {
        ToggleQuestWindow();
    }
    
    private void DrawUI() => WindowSystem.Draw();
    
    private void ToggleQuestWindow() => questWindow.Toggle();
}
