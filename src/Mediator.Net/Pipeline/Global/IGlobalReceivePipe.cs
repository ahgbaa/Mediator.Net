using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline.Global
{
    public interface IGlobalReceivePipe<TContext> : IPipe<TContext>
        where TContext : IContext<IMessage>
    {
       
    }
}
