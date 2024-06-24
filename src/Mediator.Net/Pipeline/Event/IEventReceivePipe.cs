using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline.Event
{
    
    //TContext 泛型逆变参数 
    //TContext 必须实现 IContext<IEvent> 接口
    public interface IEventReceivePipe<in TContext> : IPipe<TContext> 
        where TContext : IContext<IEvent>
    {
        
    }
}
