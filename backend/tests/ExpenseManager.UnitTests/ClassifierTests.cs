using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Enums;
using ExpenseManager.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace ExpenseManager.UnitTests;

public class ClassifierTests
{
    private static readonly List<CategoryDto> Categories =
    [
        new() { Id = Guid.NewGuid(), Name = "Ăn uống", Type = TransactionType.Expense },
        new() { Id = Guid.NewGuid(), Name = "Di chuyển", Type = TransactionType.Expense },
        new() { Id = Guid.NewGuid(), Name = "Mua sắm", Type = TransactionType.Expense },
        new() { Id = Guid.NewGuid(), Name = "Hóa đơn & Tiện ích", Type = TransactionType.Expense }
    ];

    [Theory]
    [InlineData("GRABFOOD don hang com trua", "Ăn uống")]
    [InlineData("Thanh toan GRAB chuyen di", "Di chuyển")]
    [InlineData("SHOPEE thanh toan don hang", "Mua sắm")]
    [InlineData("EVN tien dien thang 5", "Hóa đơn & Tiện ích")]
    [InlineData("Cafe Highlands", "Ăn uống")]
    public void Classify_ShouldMatchExpectedCategory(string description, string expectedCategory)
    {
        var result = KeywordClassifier.Classify(description, Categories);

        result.CategoryName.Should().Be(expectedCategory);
        result.Confidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Classify_ShouldReturnEmpty_WhenNoMatch()
    {
        var result = KeywordClassifier.Classify("XYZABC khong ro rang", Categories);

        result.CategoryId.Should().BeNull();
        result.Confidence.Should().Be(0);
    }
}
