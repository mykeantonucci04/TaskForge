using FluentAssertions;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using Xunit;

namespace TaskForge.Domain.UnitTests;

public class DomainUnitTests
{
    [Fact]
    public void TaskItem_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Implement Clean Architecture",
            Priority = TaskPriority.High,
            ColumnId = Guid.NewGuid()
        };

        // Assert
        task.Title.Should().Be("Implement Clean Architecture");
        task.Priority.Should().Be(TaskPriority.High);
        task.Status.Should().Be(TaskItemStatus.Todo);
        task.Position.Should().Be(1000.0);
    }

    [Fact]
    public void Tenant_Should_Generate_Unique_Id()
    {
        // Arrange
        var tenant1 = new Tenant { Id = Guid.NewGuid(), Name = "Tenant One", SchemaName = "tenant_1" };
        var tenant2 = new Tenant { Id = Guid.NewGuid(), Name = "Tenant Two", SchemaName = "tenant_2" };

        // Assert
        tenant1.Id.Should().NotBe(tenant2.Id);
        tenant1.SchemaName.Should().NotBe(tenant2.SchemaName);
    }
}
