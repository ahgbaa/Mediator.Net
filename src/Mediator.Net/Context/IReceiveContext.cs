using System.Threading;
using System.Threading.Tasks;
using Mediator.Net.Contracts;

namespace Mediator.Net.Context
{
    
    // TMessage为协变参数
    // TMessage必须实现IMessage
    public interface IReceiveContext<out TMessage> : 
        IContext<TMessage> 
        where TMessage : IMessage
    {
        Task PublishAsync(IEvent message, CancellationToken cancellationToken = default(CancellationToken));
    }
}
