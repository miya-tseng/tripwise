using System.Text;

namespace TripWise.Application.Suggestions;

public static class SuggestionPrompt
{
    public const int IdeaCount = 8;

    public static string Build(SuggestionContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("你是熟悉當地的旅遊規劃助手。");
        builder.AppendLine($"旅客剛安排了「{context.PlaceName}」（地址：{context.PlaceAddress}），預計 {context.EndsAt:HH\\:mm} 結束。");

        if (context.PlannedThatDay.Count > 0)
        {
            builder.AppendLine($"當天已經安排：{string.Join("、", context.PlannedThatDay)}。");
        }

        builder.AppendLine($"請列出 {IdeaCount} 個旅客通常會和「{context.PlaceName}」排在同一天、步行或短程交通可到的真實地點（景點、餐廳、咖啡廳或商店皆可）。");
        builder.AppendLine("規則：");
        builder.AppendLine("- name：Google Maps 上可以搜尋到的正式名稱，只寫一種語言，不要加括號註解");
        builder.AppendLine("- area：所在城市與區域，例如「東京都台東區」");
        builder.AppendLine("- reason：用繁體中文一句話說明為什麼適合一起安排，不超過 40 字");
        builder.AppendLine("- suggestedMinutes：建議停留分鐘數，介於 15 到 240");
        builder.AppendLine("- 不要重複當天已經安排的地點，也不要包含「" + context.PlaceName + "」本身");
        builder.Append("- 不確定是否存在的地點不要列出，不要編造");

        return builder.ToString();
    }
}
