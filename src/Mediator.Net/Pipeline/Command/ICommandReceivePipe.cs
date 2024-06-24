using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline.Command
{
    public interface ICommandReceivePipe<in TContext> : IPipe<TContext> 
        where TContext : IContext<ICommand>
    {
        
    }
}
