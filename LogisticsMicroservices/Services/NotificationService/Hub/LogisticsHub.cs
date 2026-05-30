using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

// Frontend (Angular) bu sınıfa bağlanacak
public class LogisticsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"⚡ Bir istemci haritaya bağlandı! Bağlantı ID: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }
}