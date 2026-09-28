using FieldFlow.Domain.Assets;

namespace FieldFlow.Domain.UnitTests.Assets;

public class AssetTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public void Create_normalizes_tag_and_starts_active()
    {
        var asset = Asset.Create(CustomerId, " chl-01 ", "Chiller", "Carrier", "SN1", null, null).Value;

        asset.Tag.Should().Be("CHL-01");
        asset.Status.Should().Be(AssetStatus.Active);
    }

    [Fact]
    public void Create_rejects_warranty_ending_before_installation()
    {
        var result = Asset.Create(CustomerId, "T1", "M", "F", "S", new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1));

        result.Error.Code.Should().Be("ASSET_WARRANTY_INVALID");
    }

    [Fact]
    public void Decommissioned_asset_cannot_change_status()
    {
        var asset = Asset.Create(CustomerId, "T1", "M", "F", "S", null, null).Value;
        asset.Decommission();

        asset.Reactivate().Error.Code.Should().Be("ASSET_DECOMMISSIONED");
        asset.Status.Should().Be(AssetStatus.Decommissioned);
    }

    [Fact]
    public void IsUnderWarranty_compares_dates_inclusively()
    {
        var asset = Asset.Create(CustomerId, "T1", "M", "F", "S", null, new DateOnly(2026, 6, 30)).Value;

        asset.IsUnderWarranty(new DateOnly(2026, 6, 30)).Should().BeTrue();
        asset.IsUnderWarranty(new DateOnly(2026, 7, 1)).Should().BeFalse();
    }
}
