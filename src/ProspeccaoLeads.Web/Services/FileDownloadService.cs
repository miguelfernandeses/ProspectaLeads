using Microsoft.JSInterop;

namespace ProspeccaoLeads.Web.Services;

public class FileDownloadService
{
    private readonly IJSRuntime _jsRuntime;

    public FileDownloadService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task DownloadFileFromBytesAsync(string fileName, byte[] bytes, string contentType = "application/octet-stream")
    {
        var base64 = Convert.ToBase64String(bytes);
        await _jsRuntime.InvokeVoidAsync("prospeccaoJs.downloadFromBase64", fileName, contentType, base64);
    }

    public async Task CopyToClipboardAsync(string text)
    {
        await _jsRuntime.InvokeVoidAsync("prospeccaoJs.copyToClipboard", text);
    }
}
