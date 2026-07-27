using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using ClassIsland.PluginIndexGenerator;
using ClassIsland.PluginIndexGenerator.Abstractions.Generators;
using ClassIsland.PluginIndexGenerator.Generators;
using ClassIsland.PluginIndexGenerator.Models.Configurations;
using Octokit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var rootCmd = new RootCommand
{
    new Argument<string>("inputDir", "清单输入目录。"),
    new Argument<string>("output", "索引输出文件。"),
    new Option<string>(["--gitHubToken", "-t"], "要使用的 GitHub Token。"),
    new Option<string>(["--baseFile", "-b"], "构建索引时基于的索引文件。"),
    new Option<string>(["--validate", "-v"], "是否使用验证模式。"),
};

var command = new ApplicationCommand();
rootCmd.Handler = CommandHandler.Create((ApplicationCommand c) => { command = c; });
await rootCmd.InvokeAsync(args);

ApplicationCommand.Instance = command;
var input = command.InputDir;
var output = command.Output;
var configPath = Path.Combine(input, "./config.yml");
var indexBase = command.BaseFile;
var token = command.GitHubToken;
var deserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    .WithTypeConverter(new OSPlatformTypeConverter_Yaml())
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
var config = deserializer.Deserialize<Configuration>(File.ReadAllText(configPath));


var github = new GitHubClient(new ProductHeaderValue("ClassIsland.PluginIndexGenerator"));
if (!string.IsNullOrEmpty(token))
{
    github.Credentials = new Credentials(token);
}

var generatorsFactory = new Dictionary<string, Func<string, string, MarketplaceIndexGeneratorBase>>()
{
    { "plugin", (i, o) => new PluginIndexGenerator(github, Path.Combine(input, i), Path.Combine(output, o), indexBase) },
    { "theme", (i, o) => new ThemeIndexGenerator(github, Path.Combine(input, i), Path.Combine(output, o)) }
};

foreach (var info in config.Generators)
{
    var gen = generatorsFactory[info.Id](info.Input, info.Output);
    Console.WriteLine($"正在生成 {info.Id} 索引: {info.Input} -> {info.Output}");
    await gen.GenerateIndexAsync();
}

Console.WriteLine("OK!");