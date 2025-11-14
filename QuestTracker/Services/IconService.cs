using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace QuestTracker.Services;

public class IconService : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly IPluginLog log;
    private readonly ITextureProvider textureProvider;
    private readonly Dictionary<string, IDalamudTextureWrap?> iconCache = new();
    private readonly string cacheDirectory;
    
    private const string XIVAPI_BASE = "https://xivapi.com";
    
    public IconService(IPluginLog pluginLog, ITextureProvider texProvider, string pluginDirectory)
    {
        log = pluginLog;
        textureProvider = texProvider;
        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        
        // Create cache directory for icons
        cacheDirectory = Path.Combine(pluginDirectory, "IconCache");
        Directory.CreateDirectory(cacheDirectory);
    }
    
    public async Task<IDalamudTextureWrap?> GetQuestIconAsync(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
            return null;
        
        // Check if already cached in memory
        if (iconCache.TryGetValue(iconPath, out var cachedTexture))
        {
            return cachedTexture;
        }
        
        try
        {
            // Check if icon exists on disk
            var fileName = iconPath.Replace("/", "_").Replace("\\", "_");
            var localPath = Path.Combine(cacheDirectory, fileName);
            
            byte[] imageData;
            
            if (File.Exists(localPath))
            {
                // Load from disk cache
                imageData = await File.ReadAllBytesAsync(localPath);
            }
            else
            {
                // Download from XIVAPI
                var url = $"{XIVAPI_BASE}{iconPath}";
                imageData = await httpClient.GetByteArrayAsync(url);
                
                // Save to disk cache
                await File.WriteAllBytesAsync(localPath, imageData);
            }
            
            // Load texture using Dalamud's texture provider
            var texture = textureProvider.CreateFromImageAsync(imageData).Result;
            
            // Cache in memory
            iconCache[iconPath] = texture;
            
            return texture;
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to load icon: {iconPath}");
            iconCache[iconPath] = null; // Cache the failure
            return null;
        }
    }
    
    public IDalamudTextureWrap? GetQuestIconSync(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
            return null;
        
        // Check if already cached
        if (iconCache.TryGetValue(iconPath, out var cachedTexture))
        {
            return cachedTexture;
        }
        
        // Start async load if not cached
        _ = GetQuestIconAsync(iconPath);
        
        return null; // Will be available on next frame
    }
    
    public void ClearCache()
    {
        foreach (var texture in iconCache.Values)
        {
            texture?.Dispose();
        }
        iconCache.Clear();
    }
    
    public void Dispose()
    {
        ClearCache();
        httpClient?.Dispose();
        
        // Optionally clean up disk cache
        // Directory.Delete(cacheDirectory, true);
    }
}
