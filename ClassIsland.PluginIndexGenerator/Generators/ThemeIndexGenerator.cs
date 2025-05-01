using System.Text.Json;
using ClassIsland.Core.Models.XamlTheme;
using ClassIsland.PluginIndexGenerator.Abstractions.Generators;
using Octokit;

namespace ClassIsland.PluginIndexGenerator.Generators;

public class ThemeIndexGenerator(GitHubClient client, string indexBasePath, string indexOutputFilePath) 
    : MarketplaceIndexGeneratorBase<ThemeRepoManifest, ThemeIndexItem>(client, indexBasePath, indexOutputFilePath)
{
    public override async Task GenerateIndexAsync()
    {
        var index = new ThemeIndex();
        foreach (var (mfPath, manifest) in Manifests)
        {
            try
            {
                var repo = await GetRepository(manifest);
                var (downloadUrl, md5, downloadCount, latest) = await GetArtifactDownloadInfoAsync(manifest, repo,  x => string.IsNullOrWhiteSpace(manifest.ArtifactName) ? x.Name.EndsWith(".zip") : x.Name == manifest.ArtifactName);
                manifest.Version = latest.TagName;
                index.Themes.Add(new ThemeIndexItem()
                {
                    Manifest = manifest,
                    DownloadMd5 = md5,
                    DownloadUrl = downloadUrl,
                    RealBannerPath = 
                        $"{{root}}/{manifest.RepoOwner}/{manifest.RepoName}/raw/{manifest.AssetsRoot}/{manifest.Banner}",
                    StarsCount = repo.StargazersCount,
                    DownloadCount = downloadCount
                });
                Console.WriteLine($"成功添加主题 {manifest.Id}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("在添加主题 {0} 时发生错误 {1}", mfPath, e);
                Console.Error.WriteLine($"::error file={mfPath},line=1::{e.Message}");
                if (ApplicationCommand.Instance.Validate)
                {
                    throw new InvalidOperationException($"无法处理主题 {mfPath} 的元数据。", e);;
                }
            }
        }
        
        await File.WriteAllTextAsync(IndexOutputFilePath, JsonSerializer.Serialize(index));
    }
}