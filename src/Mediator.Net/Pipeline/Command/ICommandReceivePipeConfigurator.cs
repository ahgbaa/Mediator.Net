using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline.Command
{
    public interface ICommandReceivePipeConfigurator : IPipeConfigurator<IReceiveContext<ICommand>>
    {
        ICommandReceivePipe<IReceiveContext<ICommand>> Build();
    }
}
