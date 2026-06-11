using System.Net.Http.Json;
using System.Text.Json;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseManager.Infrastructure.Services;

/// <summary>
/// AI phân loại giao dịch: nếu có ANTHROPIC_API_KEY thì dùng Claude API,
/// ngược lại dùng bộ luật từ khóa tiếng Việt (hoạt động offline, không tốn chi phí).
/// </summary>
public class TransactionClassifierService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TransactionClassifierService> logger) : ITransactionClassifier
{
    public async Task<ClassificationResultDto> ClassifyAsync(
        string description, IReadOnlyList<CategoryDto> expenseCategories, CancellationToken ct = default)
    {
        if (expenseCategories.Count == 0)
            return new ClassificationResultDto();

        var apiKey = configuration["Anthropic:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                return await ClassifyWithClaudeAsync(apiKey, description, expenseCategories, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Claude API phân loại thất bại, chuyển sang bộ luật từ khóa.");
            }
        }

        return KeywordClassifier.Classify(description, expenseCategories);
    }

    private async Task<ClassificationResultDto> ClassifyWithClaudeAsync(
        string apiKey, string description, IReadOnlyList<CategoryDto> categories, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("anthropic");
        var categoryList = string.Join("\n", categories.Select(c => $"- {c.Name}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(new
            {
                model = configuration["Anthropic:Model"] ?? "claude-opus-4-8",
                max_tokens = 256,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Phân loại giao dịch chi tiêu sau vào đúng một danh mục trong danh sách.\n" +
                                  $"Mô tả giao dịch: \"{description}\"\n\nDanh mục:\n{categoryList}\n\n" +
                                  "Chỉ trả lời đúng tên danh mục, không thêm gì khác."
                    }
                }
            })
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var stopReason = json.RootElement.GetProperty("stop_reason").GetString();
        if (stopReason == "refusal")
            return KeywordClassifier.Classify(description, categories);

        var text = json.RootElement.GetProperty("content").EnumerateArray()
            .Where(b => b.GetProperty("type").GetString() == "text")
            .Select(b => b.GetProperty("text").GetString())
            .FirstOrDefault()?.Trim();

        var match = categories.FirstOrDefault(c =>
            string.Equals(c.Name, text, StringComparison.OrdinalIgnoreCase));
        return match is not null
            ? new ClassificationResultDto { CategoryId = match.Id, CategoryName = match.Name, Confidence = 0.95 }
            : KeywordClassifier.Classify(description, categories);
    }
}

/// <summary>Bộ phân loại dựa trên từ khóa tiếng Việt — fallback khi không cấu hình Claude API.</summary>
public static class KeywordClassifier
{
    private static readonly Dictionary<string, string[]> Keywords = new()
    {
        ["ăn uống"] = ["an uong", "ăn", "com", "cơm", "pho", "phở", "bun", "bún", "cafe", "coffee", "caphe", "cà phê", "trà sữa", "tra sua", "food", "grabfood", "shopeefood", "befood", "nha hang", "nhà hàng", "quan", "quán", "restaurant", "kfc", "lotteria", "pizza", "highlands", "starbucks", "an sang", "an trua", "an toi", "đồ ăn", "do an", "banh", "bánh"],
        ["di chuyển"] = ["grab", "be ", "gojek", "xanh sm", "taxi", "xang", "xăng", "petrolimex", "gui xe", "gửi xe", "ve xe", "vé xe", "bus", "xe buyt", "xe khach", "may bay", "máy bay", "vietjet", "vietnam airlines", "bamboo", "tau", "tàu", "toll", "phi cau duong", "parking", "fuel"],
        ["mua sắm"] = ["shopee", "lazada", "tiki", "sendo", "tiktok shop", "mua", "sieu thi", "siêu thị", "vinmart", "winmart", "coopmart", "bach hoa", "bách hóa", "circle k", "quan ao", "quần áo", "giay", "giày", "uniqlo", "zara", "h&m", "order", "shopping", "store", "mall", "aeon"],
        ["hóa đơn & tiện ích"] = ["dien", "điện", "evn", "nuoc", "nước", "internet", "wifi", "fpt", "viettel", "vnpt", "mobifone", "vinaphone", "cuoc", "cước", "hoa don", "hóa đơn", "tien nha", "tiền nhà", "thue nha", "thuê nhà", "chung cu", "phi quan ly", "gas", "truyen hinh", "netflix", "spotify", "youtube premium", "icloud", "bill"],
        ["giải trí"] = ["phim", "cgv", "lotte cinema", "galaxy", "game", "steam", "karaoke", "bar", "bia", "beer", "du lich", "du lịch", "travel", "khach san", "khách sạn", "hotel", "resort", "ve so", "concert", "show", "ticketbox"],
        ["sức khỏe"] = ["thuoc", "thuốc", "pharmacity", "long chau", "long châu", "an khang", "benh vien", "bệnh viện", "phong kham", "phòng khám", "bac si", "bác sĩ", "kham", "khám", "gym", "yoga", "fitness", "bao hiem", "bảo hiểm", "hospital", "clinic", "vitamin"],
        ["giáo dục"] = ["hoc phi", "học phí", "khoa hoc", "khóa học", "course", "udemy", "coursera", "sach", "sách", "fahasa", "tiki book", "truong", "trường", "lop", "lớp", "tieng anh", "tiếng anh", "ielts", "toeic", "tuition"]
    };

    public static ClassificationResultDto Classify(string description, IReadOnlyList<CategoryDto> categories)
    {
        var text = " " + description.ToLowerInvariant() + " ";
        var best = (Category: (CategoryDto?)null, Score: 0);

        foreach (var category in categories)
        {
            var key = Keywords.Keys.FirstOrDefault(k =>
                category.Name.ToLowerInvariant().Contains(k) || k.Contains(category.Name.ToLowerInvariant()));
            if (key is null) continue;

            var score = Keywords[key].Count(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase));
            if (score > best.Score)
                best = (category, score);
        }

        return best.Category is not null
            ? new ClassificationResultDto
            {
                CategoryId = best.Category.Id,
                CategoryName = best.Category.Name,
                Confidence = Math.Min(0.9, 0.5 + best.Score * 0.1)
            }
            : new ClassificationResultDto { Confidence = 0 };
    }
}
