using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace Visavi.Quantis.Events
{
    internal class EventService : IEventService
    {
        private readonly ILogger _logger;
        private readonly Connections _connections;
        private readonly ServiceHubContext _eventHub;

        public EventService(Connections connections, ILogger logger)
        {
            _logger = logger;
            _connections = connections;
            _eventHub = connections.EventHub().Result;
        }

        public void FireAndForget(EventNames eventName, string message)
        {
            FireAndForget(eventName.ToString(), message);
        }

        public void FireAndForget(string eventName, string message)
        {
            Task task = Publish(eventName, message);
            if (task.IsFaulted)
            {
                _logger.LogError(task.Exception, $"Exception occured after firing and forgetting event: {eventName}");
            }
        }

        public async Task Publish(EventNames eventName, string message)
        {
            await Publish(eventName.ToString(), message);
        }

        public async Task Publish(string eventName, string message)
        {
            try
            {
                await _eventHub.Clients.All.SendAsync(eventName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occured publishing event: {eventName}");
                throw;
            }
        }
    }
}
