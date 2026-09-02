using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace TotalMonitor.Server.Hubs;
[Authorize] public sealed class MonitoringHub : Hub { }
