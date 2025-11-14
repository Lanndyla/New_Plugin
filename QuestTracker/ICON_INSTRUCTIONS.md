# Plugin Icon Setup

To add the Quest Map 2.0 icon to the plugin:

1. Save the icon image as: `QuestTracker/images/icon.png`
   - The image should be 512x512 pixels (or at least square)
   - PNG format with transparency

2. The plugin manifest has been updated to reference this icon

3. After adding the icon, rebuild and redeploy:
   ```bash
   dotnet build QuestTracker/QuestTracker.csproj
   cp QuestTracker/bin/Debug/QuestTracker.dll "$HOME/Library/Application Support/XIV on Mac/wineprefix/drive_c/users/crossover/AppData/Roaming/XIVLauncher/devPlugins/QuestTracker/"
   cp QuestTracker/images/icon.png "$HOME/Library/Application Support/XIV on Mac/wineprefix/drive_c/users/crossover/AppData/Roaming/XIVLauncher/devPlugins/QuestTracker/"
   ```
