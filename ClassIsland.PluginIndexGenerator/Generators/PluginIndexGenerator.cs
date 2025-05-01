using System.Collections.ObjectModel;
using System.Text.Json;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.PluginIndexGenerator.Abstractions.Generators;
using Octokit;

namespace ClassIsland.PluginIndexGenerator.Generators;

public class PluginIndexGenerator(GitHubClient client, string indexBasePath, string indexOutputFilePath, string? indexBaseFilePath) 
    : MarketplaceIndexGeneratorBase<PluginRepoManifest, PluginIndexItem>(client, indexBasePath, indexOutputFilePath)
{
    public override async Task GenerateIndexAsync()
    {
        var index = string.IsNullOrWhiteSpace(indexBaseFilePath)
            ? new PluginIndex()
            : JsonSerializer.Deserialize<PluginIndex>(await File.ReadAllTextAsync(indexBaseFilePath))
              ?? new PluginIndex();
        foreach (var (mfPath, manifest) in Manifests)
        {
            try
            {
                var repo = await GetRepository(manifest);
                var (downloadUrl, md5, downloadCount, latest) = await GetArtifactDownloadInfoAsync(manifest, repo,  x => string.IsNullOrWhiteSpace(manifest.ArtifactName) ? x.Name.EndsWith(".cipx") : x.Name == manifest.ArtifactName);
                manifest.Version = latest.TagName;
                manifest.Readme =
                    $"{{root}}/{manifest.RepoOwner}/{manifest.RepoName}/raw/{manifest.AssetsRoot}/{manifest.Readme}";
                index.Plugins.Add(new PluginIndexItem()
                {
                    Manifest = manifest,
                    DownloadMd5 = md5,
                    DownloadUrl = downloadUrl,
                    RealIconPath =
                        $"{{root}}/{manifest.RepoOwner}/{manifest.RepoName}/raw/{manifest.AssetsRoot}/{manifest.Icon}",
                    StarsCount = repo.StargazersCount,
                    DownloadCount = downloadCount
                });
                Console.WriteLine($"成功添加插件 {manifest.Id}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("在添加插件 {0} 时发生错误 {1}", mfPath, e);
                Console.Error.WriteLine($"::error file={mfPath},line=1::{e.Message}");
                if (ApplicationCommand.Instance.Validate)
                {
                    throw new InvalidOperationException($"无法处理插件 {mfPath} 的元数据。", e);;
                }
            }
        }
        
        await File.WriteAllTextAsync(IndexOutputFilePath, JsonSerializer.Serialize(index));
    }
}