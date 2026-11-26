using Microsoft.Playwright;

namespace TripWise.E2ETests;

public static class DevToolsOverlay
{
    private const string HideScript = """
        document.addEventListener('DOMContentLoaded', () => {
          const style = document.createElement('style');
          style.textContent = '#__vue-devtools-container__ { display: none !important; }';
          document.head.appendChild(style);
        });
        """;

    public static Task HideAsync(IBrowserContext context) => context.AddInitScriptAsync(HideScript);
}
