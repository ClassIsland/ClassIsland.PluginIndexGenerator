using System.Collections.ObjectModel;
using System.Text.Json;
using ClassIsland.Core.Abstractions.Models.Marketplace;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Plugin;
using Microsoft.Extensions.FileSystemGlobbing;
using Octokit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ClassIsland.PluginIndexGenerator.Abstractions.Generators;

public abstract class MarketplaceIndexGeneratorBase
{
    public abstract Task GenerateIndexAsync();
}

public abstract class MarketplaceIndexGeneratorBase<TRepoManifest, TIndexInfo> : MarketplaceIndexGeneratorBase
    where TRepoManifest : class, IMarketplaceItemRepoManifest
    where TIndexInfo : class, IMarketplaceItemInfo
{
    protected MarketplaceIndexGeneratorBase(GitHubClient client, string indexBasePath, string indexOutputFilePath)
    {
        Client = client;
        IndexBasePath = indexBasePath;
        IndexOutputFilePath = indexOutputFilePath;
        
        var manifests = Directory.EnumerateFiles(IndexBasePath)
            .Where(x => Path.GetExtension(x) == ".yml")
            .ToList();
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new OSPlatformTypeConverter_Yaml())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        foreach (var mfFile in manifests)
        {
            try
            {
                var mfText = File.ReadAllText(mfFile);
                var manifest = deserializer.Deserialize<TRepoManifest>(mfText);
                Manifests.Add(mfFile, manifest);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("在读取市场元数据 {0} 时发生错误 {1}", mfFile, e);
                Console.Error.WriteLine($"::error file={mfFile},line=1::{e.Message}");
            }
        }
    }

    private const string DownloadRoot = "https://github.com";
    private const string DownloadRootTemplate = "{root}";

    protected GitHubClient Client { get; }
    protected string IndexBasePath { get; }
    protected string IndexOutputFilePath { get; }

    protected Dictionary<string, TRepoManifest> Manifests { get; } = [];

    protected async Task<Repository> GetRepository(TRepoManifest manifest)
    {
        var repo = await Client.Repository.Get(manifest.RepoOwner, manifest.RepoName);
        if (repo == null)
        {
            throw new InvalidOperationException($"对象 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 无效。");
        }

        return repo;
    }
    
    protected async Task<(string, string, long, Release)> GetArtifactDownloadInfoAsync(TRepoManifest manifest, Repository repo, Func<ReleaseAsset, bool> matchAsset)
    {
        var matcher = new Matcher();
        var usePattern = manifest.TagPattern != null;
        if (usePattern)
        {
            matcher.AddInclude(manifest.TagPattern);
        }
        var latest = (await Client.Repository.Release.GetAll(repo.Id))
            .Where(x => !usePattern || matcher.Match(x.TagName).HasMatches)
            .Where(x => Version.TryParse(x.TagName, out _))
            .MaxBy(x => Version.Parse(x.TagName));
        if (latest == null)
        {
            throw new InvalidOperationException(
                $"对象 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 中没有标记为最新的发行版。");
        }

        var asset = latest.Assets.FirstOrDefault(matchAsset);
        if (asset == null)
        {
            throw new InvalidOperationException(
                $"对象 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 中最新的发行版中没有有效的插件包资产。");
        }
        var releases = await Client.Repository.Release.GetAll(repo.Id);
        var totalDownloads = releases.SelectMany(release => release.Assets).Aggregate<ReleaseAsset, long>(0, (current, i) => current + i.DownloadCount);
        
        var md5 = ChecksumHelper.ExtractHashInfo(latest.Body, asset.Name);

        return (asset.BrowserDownloadUrl.Replace(DownloadRoot, DownloadRootTemplate), md5, totalDownloads, latest);
    }
    
}