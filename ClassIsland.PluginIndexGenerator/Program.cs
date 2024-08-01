using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.PluginIndexGenerator;
using Octokit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var rootCmd = new RootCommand
{
    new Argument<string>("inputDir", "清单输入目录。"),
    new Argument<string>("output", "索引输出文件。"),
    new Option<string>(["--gitHubToken", "-t"], "要使用的 GitHub Token。"),
    new Option<string>(["--baseFile", "-b"], "构建索引时基于的索引文件。"),
};

var command = new ApplicationCommand();
rootCmd.Handler = CommandHandler.Create((ApplicationCommand c) => { command = c; });
await rootCmd.InvokeAsync(args);

var input = command.InputDir;
var output = command.Output;
var indexBase = command.BaseFile;
var token = command.GitHubToken;

var manifests = Directory.EnumerateFiles(input).Where(x => Path.GetExtension(x) == ".yml");
var deserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
var index = string.IsNullOrWhiteSpace(indexBase)
    ? new PluginIndex()
    : JsonSerializer.Deserialize<PluginIndex>(await File.ReadAllTextAsync(indexBase))
      ?? new PluginIndex();
var github = new GitHubClient(new ProductHeaderValue("ClassIsland.PluginIndexGenerator"));
if (!string.IsNullOrEmpty(token))
{
    github.Credentials = new Credentials(token);
}
const string root = "https://github.com";
foreach (var mfPath in manifests)
{
    var mfText = await File.ReadAllTextAsync(mfPath);
    var manifest = deserializer.Deserialize<PluginRepoManifest>(mfText);

    var repo = await github.Repository.Get(manifest.RepoOwner, manifest.RepoName);
    if (repo == null)
    {
        await Console.Error.WriteLineAsync(
            $"error: 插件 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 无效，已跳过。");
        continue;
    }

    var latest = (await github.Repository.Release.GetAll(repo.Id))
        .Where(x => Version.TryParse(x.TagName, out _)).MaxBy(x => Version.Parse(x.TagName));
    if (latest == null)
    {
        await Console.Error.WriteLineAsync(
            $"error: 插件 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 中没有标记为最新的发行版，已跳过。");
        continue;
    }

    var asset = latest.Assets.FirstOrDefault(x => x.Name.EndsWith(".cipx"));
    if (asset == null)
    {
        await Console.Error.WriteLineAsync(
            $"error: 插件 {manifest.Id} 的仓库路径 {manifest.RepoOwner}/{manifest.RepoName} 中最新的发行版中没有有效的插件包资产，已跳过。");
        continue;
    }

    var md5 = ChecksumHelper.ExtractHashInfo(latest.Body, asset.Name);
    manifest.Version = latest.TagName;
    manifest.Readme =
        $"{{root}}/{manifest.RepoOwner}/{manifest.RepoName}/raw/{manifest.AssetsRoot}/{manifest.Readme}";
    index.Plugins.Add(new PluginIndexItem()
    {
        Manifest = manifest,
        DownloadMd5 = md5,
        DownloadUrl = asset.BrowserDownloadUrl.Replace(root, "{root}"),
        RealIconPath = $"{{root}}/{manifest.RepoOwner}/{manifest.RepoName}/raw/{manifest.AssetsRoot}/{manifest.Icon}"
    });
    Console.WriteLine($"成功添加插件 {manifest.Id}");
}

await File.WriteAllTextAsync(output, JsonSerializer.Serialize(index));
Console.Write("OK!");