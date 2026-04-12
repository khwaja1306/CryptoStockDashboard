using CryptoStockDashboard.Models;
using Microsoft.AspNetCore.SignalR;

namespace CryptoStockDashboard.Hubs;

public class DashboardHub : Hub
{
    public async Task RequestUpdate()
    {
        await Clients.Caller.SendAsync("UpdateRequested");
    }

    public async Task JoinGroup(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public async Task LeaveGroup(string group)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
}
