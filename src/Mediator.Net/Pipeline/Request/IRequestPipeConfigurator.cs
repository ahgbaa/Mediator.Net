using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline.Request
{
    public interface IRequestPipeConfigurator<TContext> : IPipeConfigurator<TContext>
        where TContext : IReceiveContext<IRequest>
    {
        IRequestReceivePipe<TContext> Build();
    }
}
