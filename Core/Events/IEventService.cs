using Microsoft.Azure.SignalR.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visavi.Quantis.Events
{
    public interface IEventService
    {
        public void FireAndForget(EventNames eventName, string message);

        public void FireAndForget(string eventName, string message);

        public Task Publish(EventNames eventName, string message);
        public Task Publish(string eventName, string message);
    }

    public enum EventNames
    {
        modelUpdated = 1
    }
}
