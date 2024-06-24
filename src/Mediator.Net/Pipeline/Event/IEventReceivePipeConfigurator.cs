using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline.Event
{
    public interface IEventReceivePipeConfigurator : IPipeConfigurator<IReceiveContext<IEvent>>
    {
        IEventReceivePipe<IReceiveContext<IEvent>> Build();
    }
}
