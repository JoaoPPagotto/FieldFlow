using FieldFlow.Domain.Catalog;
using FieldFlow.Domain.WorkOrders;

namespace FieldFlow.Domain.UnitTests.Catalog;

public class CatalogTests
{
    [Theory]
    [InlineData(Priority.Low, 48)]
    [InlineData(Priority.Medium, 24)]
    [InlineData(Priority.High, 8)]
    [InlineData(Priority.Critical, 4)]
    public void SlaPolicy_returns_hours_by_priority(Priority priority, int expected)
    {
        SlaPolicy.Create(48, 24, 8, 4).Value.GetHours(priority).Should().Be(expected);
    }

    [Fact]
    public void SlaPolicy_rejects_higher_priority_with_longer_deadline()
    {
        SlaPolicy.Create(8, 24, 48, 4).Error.Code.Should().Be("SERVICE_TYPE_SLA_ORDER_INVALID");
    }

    [Fact]
    public void SlaPolicy_rejects_non_positive_hours()
    {
        SlaPolicy.Create(48, 24, 8, 0).Error.Code.Should().Be("SERVICE_TYPE_SLA_HOURS_INVALID");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1441)]
    public void ServiceType_validates_default_duration(int minutes)
    {
        var sla = SlaPolicy.Create(48, 24, 8, 4).Value;

        ServiceType.Create("Preventiva", minutes, sla).Error.Code.Should().Be("SERVICE_TYPE_DURATION_INVALID");
    }

    [Fact]
    public void Part_stock_moves_and_rejects_invalid_quantities()
    {
        var part = Part.Create("flt-01", "Filtro", 10m, 3).Value;

        part.Code.Should().Be("FLT-01");
        part.DecreaseStock(2).IsSuccess.Should().BeTrue();
        part.IncreaseStock(4).IsSuccess.Should().BeTrue();
        part.StockQuantity.Should().Be(5);
        part.DecreaseStock(0).Error.Code.Should().Be("PART_QUANTITY_INVALID");
        part.DecreaseStock(6).Error.Code.Should().Be("PART_INSUFFICIENT_STOCK");
    }

    [Fact]
    public void Part_rejects_negative_price_and_stock()
    {
        Part.Create("A", "B", -1m, 0).Error.Code.Should().Be("PART_UNIT_PRICE_INVALID");
        Part.Create("A", "B", 1m, -1).Error.Code.Should().Be("PART_STOCK_INVALID");
    }
}
