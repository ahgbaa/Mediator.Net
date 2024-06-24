using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline.PublishPipe
{
    public interface IPublishPipeConfigurator : IPipeConfigurator<IPublishContext<IEvent>>

    {
        IPublishPipe<IPublishContext<IEvent>> Build();
    }
}
