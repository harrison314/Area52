using BlazorStrap;
using Microsoft.JSInterop;

namespace Area52.Shared.Services;

public class FunctionalToaster
{
    private readonly IJSRuntime jsRuntime;
    private readonly IBlazorStrap blazorStrap;
    private readonly ILogger<FunctionalToaster> logger;

    public FunctionalToaster(IJSRuntime jsRuntime, IBlazorStrap blazorStrap, ILogger<FunctionalToaster> logger)
    {
        this.jsRuntime = jsRuntime;
        this.blazorStrap = blazorStrap;
        this.logger = logger;
    }

    public async Task CopyToClipboard(string content, string copyData)
    {
        try
        {
            await this.jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", copyData);
            this.blazorStrap.Toaster.Add("Copy", content, o =>
            {
                o.CloseAfter = 1000;
                o.Color = BSColor.Info;
                o.Toast = Toast.Default;
            });
        }
        catch (JSException ex) when (ex.Message.Contains("navigator.clipboard.writeText"))
        {
            this.logger.LogWarning(ex, "Copy to clipboard failed in browser.");
            this.blazorStrap.Toaster.Add("Copy", "Copy to clipboard failed in browser.", o =>
            {
                o.CloseAfter = 1000;
                o.Color = BSColor.Danger;
                o.Toast = Toast.Default;
            });
        }
    }
}
