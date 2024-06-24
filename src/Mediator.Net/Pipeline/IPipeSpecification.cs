using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator.Net.Context;
using Mediator.Net.Contracts;

namespace Mediator.Net.Pipeline
{
    //IPipeSpecification的 泛型参数TContext，要求TContext必须实现IContext接口，并且IContext接口的类型参数必须是IMessage类型。
    public interface IPipeSpecification<TContext>
        where TContext : IContext<IMessage>
    {
        bool ShouldExecute(TContext context, CancellationToken cancellationToken);
        Task BeforeExecute(TContext context, CancellationToken cancellationToken);
        Task Execute(TContext context, CancellationToken cancellationToken);
        Task AfterExecute(TContext context, CancellationToken cancellationToken);
        Task OnException(Exception ex, TContext context);
    }
}