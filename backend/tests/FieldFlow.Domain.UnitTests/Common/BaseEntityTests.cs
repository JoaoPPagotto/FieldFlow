using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.UnitTests.Common;

public class BaseEntityTests
{
    private sealed class Dummy : BaseEntity;

    private static readonly DateTime Now = new(2026, 3, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MarkDeleted_sets_soft_delete_fields()
    {
        var entity = new Dummy();

        entity.MarkDeleted("ana@fieldflow.dev", Now);

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(Now);
        entity.DeletedBy.Should().Be("ana@fieldflow.dev");
    }

    [Fact]
    public void Restore_clears_soft_delete_fields()
    {
        var entity = new Dummy();
        entity.MarkDeleted("ana", Now);

        entity.Restore();

        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void SetCreated_and_SetUpdated_fill_audit_columns()
    {
        var entity = new Dummy();

        entity.SetCreated("System", Now);
        entity.SetUpdated("bob", Now.AddHours(1));

        entity.CreatedBy.Should().Be("System");
        entity.CreatedAt.Should().Be(Now);
        entity.UpdatedBy.Should().Be("bob");
        entity.UpdatedAt.Should().Be(Now.AddHours(1));
    }
}
