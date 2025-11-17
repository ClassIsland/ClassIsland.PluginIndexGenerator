namespace ClassIsland.Core.Abstractions.Models.Marketplace;

public interface IMarketplaceItemRepoManifest : IMarketplaceItemManifest
{
    /// <summary>
    /// 仓库所有者
    /// </summary>
    /// <example>HelloWRC</example>
    public string RepoOwner { get; set; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    /// <example>MyPlugin</example>
    public string RepoName { get; set; }

    /// <summary>
    /// 资产文件根目录
    /// </summary>
    public string AssetsRoot { get; set; }
    
    /// <summary>
    /// 发布工件名称。
    /// </summary>
    public string? ArtifactName { get; set; }
    
    /// <summary>
    /// 要匹配的 Tag 模式。如果设置，将查找匹配这个模式的 Tag
    /// </summary>
    public string? TagPattern { get; set; }
}