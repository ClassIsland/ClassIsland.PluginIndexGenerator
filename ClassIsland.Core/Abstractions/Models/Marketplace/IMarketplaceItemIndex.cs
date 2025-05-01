namespace ClassIsland.Core.Abstractions.Models.Marketplace;

public interface IMarketplaceItemIndex
{
    public ICollection<IMarketplaceItemInfo> Index { get; set; }
}