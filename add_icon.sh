#!/bin/bash

# Script to add the plugin icon

ICON_SOURCE="$1"
ICON_DEST="QuestTracker/images/icon.png"
PLUGIN_DIR="$HOME/Library/Application Support/XIV on Mac/wineprefix/drive_c/users/crossover/AppData/Roaming/XIVLauncher/devPlugins/QuestTracker"

if [ -z "$ICON_SOURCE" ]; then
    echo "Usage: ./add_icon.sh <path_to_icon_image>"
    echo ""
    echo "Example: ./add_icon.sh ~/Downloads/quest_map_icon.png"
    echo ""
    echo "The icon should be a square PNG image (recommended 512x512 pixels)"
    exit 1
fi

if [ ! -f "$ICON_SOURCE" ]; then
    echo "Error: Icon file not found at $ICON_SOURCE"
    exit 1
fi

# Copy icon to project
mkdir -p QuestTracker/images
cp "$ICON_SOURCE" "$ICON_DEST"
echo "✅ Icon copied to project: $ICON_DEST"

# Rebuild plugin
export DALAMUD_HOME="$HOME/Library/Application Support/XIV on Mac/dalamud/Hooks/dev"
dotnet build QuestTracker/QuestTracker.csproj

if [ $? -eq 0 ]; then
    echo "✅ Plugin rebuilt successfully"
    
    # Copy to plugin directory
    cp QuestTracker/bin/Debug/QuestTracker.dll "$PLUGIN_DIR/"
    cp QuestTracker/bin/Debug/QuestTracker.json "$PLUGIN_DIR/"
    cp "$ICON_DEST" "$PLUGIN_DIR/"
    
    echo "✅ Plugin and icon deployed!"
    echo ""
    echo "Reload the plugin in-game to see the new icon:"
    echo "  /xlplugins → Dev Tools → Toggle QuestTracker OFF/ON"
else
    echo "❌ Build failed"
    exit 1
fi
