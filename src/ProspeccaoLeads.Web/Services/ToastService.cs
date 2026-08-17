namespace ProspeccaoLeads.Web.Services;

public enum ToastType
{
    Success,
    Error,
    Info,
    Warning
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public int DurationMs { get; set; } = 4000;
}

public class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowSuccess(string message, string title = "Sucesso")
    {
        Show(message, title, ToastType.Success);
    }

    public void ShowError(string message, string title = "Erro")
    {
        Show(message, title, ToastType.Error);
    }

    public void ShowInfo(string message, string title = "Informação")
    {
        Show(message, title, ToastType.Info);
    }

    public void ShowWarning(string message, string title = "Atenção")
    {
        Show(message, title, ToastType.Warning);
    }

    private void Show(string message, string title, ToastType type)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Title = title,
            Message = message,
            Type = type
        });
    }
}
