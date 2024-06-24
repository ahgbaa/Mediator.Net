using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline.Global
{
    public interface IGlobalReceivePipeConfigurator : IPipeConfigurator<IReceiveContext<IMessage>>
    {
        IGlobalReceivePipe<IReceiveContext<IMessage>> Build();
    }
}
