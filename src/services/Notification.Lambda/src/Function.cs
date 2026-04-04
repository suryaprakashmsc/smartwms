namespace Notification.Lambda;

public sealed class Function
{
    public string Handle(string input)
    {
        return $"Notification placeholder received: {input}";
    }
}
