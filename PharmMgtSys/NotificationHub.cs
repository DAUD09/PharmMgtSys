using Microsoft.AspNet.SignalR;
using PharmMgtSys.Models;

namespace PharmMgtSys.Hubs
{
    public class NotificationHub : Hub
    {
        public void SendNotification(string userId, string message)
        {
            Clients.User(userId).addNotification(message);
        }
    }
}