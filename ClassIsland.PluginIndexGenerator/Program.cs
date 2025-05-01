using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using ClassIsland.PluginIndexGenerator;
using ClassIsland.PluginIndexGenerator.Generators;
using Octokit;

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
var indexBase = command.BaseFile;
var token = command.GitHubToken;


var github = new GitHubClient(new ProductHeaderValue("ClassIsland.PluginIndexGenerator"));
if (!string.IsNullOrEmpty(token))
{
    github.Credentials = new Credentials(token);
}

var pluginIndexBasePath = Path.Combine(input, "plugins");
Console.Write($"正在生成插件索引: {pluginIndexBasePath}");
var pluginIndexGenerator = new PluginIndexGenerator(github, pluginIndexBasePath, Path.Combine(output, "index.json"), indexBase);
await pluginIndexGenerator.GenerateIndexAsync();

var themeIndexBasePath = Path.Combine(input, "themes");
Console.Write($"正在生成主题索引: {themeIndexBasePath}");
var themeIndexGenerator = new ThemeIndexGenerator(github, themeIndexBasePath, Path.Combine(output, "themes.json"));
await themeIndexGenerator.GenerateIndexAsync();

Console.Write("OK!");