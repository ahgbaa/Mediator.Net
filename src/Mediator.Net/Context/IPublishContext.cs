using Mediator.Net.Contracts;

namespace Mediator.Net.Context
{

    // TMessage必须实现IEvent
    public interface IPublishContext<TMessage> : IContext<TMessage>
        where TMessage : IEvent 
    {
        
    }
}
