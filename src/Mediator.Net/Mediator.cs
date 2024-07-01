using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mediator.Net.Context;
using Mediator.Net.Contracts;
using Mediator.Net.Pipeline.Command;
using Mediator.Net.Pipeline.Event;
using Mediator.Net.Pipeline.Global;
using Mediator.Net.Pipeline.PublishPipe;
using Mediator.Net.Pipeline.Request;

//小结一下：IEvent，IRequest,ICommand其实是不会影响到 I(Event,Handle,Request)Handle的注册,只不过是对SendAsync,PushAsync
//方法的调用起到限制作用
namespace Mediator.Net
{
    public class Mediator : IMediator
    {
        private readonly ICommandReceivePipe<IReceiveContext<ICommand>> _commandReceivePipe;
        private readonly IEventReceivePipe<IReceiveContext<IEvent>> _eventReceivePipe;
        private readonly IRequestReceivePipe<IReceiveContext<IRequest>> _requestPipe;
        private readonly IPublishPipe<IPublishContext<IEvent>> _publishPipe;
        private readonly IGlobalReceivePipe<IReceiveContext<IMessage>> _globalPipe;
        private readonly IDependencyScope _scope;

        public Mediator(
            ICommandReceivePipe<IReceiveContext<ICommand>> commandReceivePipe,
            IEventReceivePipe<IReceiveContext<IEvent>> eventReceivePipe,
            IRequestReceivePipe<IReceiveContext<IRequest>> requestPipe,
            IPublishPipe<IPublishContext<IEvent>> publishPipe,
            IGlobalReceivePipe<IReceiveContext<IMessage>> globalPipe,
            IDependencyScope scope = null)
        {
            _commandReceivePipe = commandReceivePipe;
            _eventReceivePipe = eventReceivePipe;
            _requestPipe = requestPipe;
            _publishPipe = publishPipe;
            _globalPipe = globalPipe;
            _scope = scope;
        }


        public async Task SendAsync<TMessage>(TMessage cmd,
            CancellationToken cancellationToken = default(CancellationToken))
            where TMessage : ICommand
        {
            await SendMessage(cmd, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResponse> SendAsync<TMessage, TResponse>(TMessage cmd,
            CancellationToken cancellationToken = default(CancellationToken))
            where TMessage : ICommand
            where TResponse : IResponse
        {
            return await SendMessage<TMessage, TResponse>(cmd, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync<TMessage>(IReceiveContext<TMessage> receiveContext,
            CancellationToken cancellationToken = default(CancellationToken))
            where TMessage : ICommand
        {
            await SendMessage(receiveContext, cancellationToken).ConfigureAwait(false);
        }

        public async Task PublishAsync<TMessage>(TMessage evt,
            CancellationToken cancellationToken = default(CancellationToken))
            where TMessage : IEvent
        {
            await SendMessage(evt, cancellationToken).ConfigureAwait(false);
        }

        public async Task PublishAsync<TMessage>(IReceiveContext<TMessage> receiveContext,
            CancellationToken cancellationToken = default(CancellationToken))
            where TMessage : IEvent
        {
            await SendMessage(receiveContext, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
            where TRequest : IRequest
            where TResponse : IResponse
        {
            return await SendMessage<TRequest, TResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResponse> RequestAsync<TRequest, TResponse>(IReceiveContext<TRequest> receiveContext,
            CancellationToken cancellationToken = default(CancellationToken))
            where TRequest : IRequest
            where TResponse : IResponse
        {
            var result = await SendMessage(receiveContext, cancellationToken).ConfigureAwait(false);
            return (TResponse)result;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TRequest, TResponse>(
            IReceiveContext<TRequest> receiveContext,
            CancellationToken cancellationToken = default(CancellationToken))
            where TRequest : IMessage where TResponse : IResponse
        {
            return CreateStreamInternal<TRequest, TResponse>(receiveContext, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TRequest, TResponse>(TRequest request,
            CancellationToken cancellationToken = default) where TRequest : IMessage where TResponse : IResponse
        {
            return CreateStreamInternal<TRequest, TResponse>(request, cancellationToken);
        }

        private IAsyncEnumerable<TResponse> CreateStreamInternal<TMessage, TResponse>(
            IReceiveContext<TMessage> customReceiveContext,
            [EnumeratorCancellation] CancellationToken cancellationToken)
            where TMessage : IMessage
        {
            RegisterServiceIfRequired(customReceiveContext);

            return _globalPipe.ConnectStream<TResponse>((IReceiveContext<IMessage>)customReceiveContext,
                cancellationToken);
        }

        private IAsyncEnumerable<TResponse> CreateStreamInternal<TMessage, TResponse>(TMessage msg,
            [EnumeratorCancellation] CancellationToken cancellationToken)
            where TMessage : IMessage
        {
            if (msg is IEvent)
                throw new NotSupportedException("IEvent is not supported for CreateStream");

            var receiveContext =
                (IReceiveContext<TMessage>)Activator.CreateInstance(
                    typeof(ReceiveContext<>).MakeGenericType(msg.GetType()), msg);
            RegisterServiceIfRequired(receiveContext);

            return _globalPipe.ConnectStream<TResponse>((IReceiveContext<IMessage>)receiveContext, cancellationToken);
        }

        // ICommand，IRequest可调用到此处
        //规定了传入和传出值类型，并且传出值类型会进行强转
        //此处可以看到到调用 SendMessage<TMessage, TResponse> 的 ICommand，IRequest 是有返回值，且返回值类型是不可变的
        private async Task<TResponse> SendMessage<TMessage, TResponse>(TMessage msg,
            CancellationToken cancellationToken)
            where TMessage : IMessage
        {
            // 指定 ReceiveContext<> 中的泛型类型，并进行 ReceiveContext<> 的有参构造
            var receiveContext =
                (IReceiveContext<TMessage>)Activator.CreateInstance(
                    typeof(ReceiveContext<>).MakeGenericType(msg.GetType()), msg);
            RegisterServiceIfRequired(receiveContext);

            receiveContext.ResultDataType = typeof(TResponse);

            var task = _globalPipe.Connect((IReceiveContext<IMessage>)receiveContext, cancellationToken);

            var result = await task.ConfigureAwait(false);

            return (TResponse)(receiveContext.Result ?? result);
        }


        // ICommand，IEvent 可调到此处
        //此处可以看到到调用 SendMessage<TMessage>的 ICommand，IEvent 是无返回值，
        private async Task<object> SendMessage<TMessage>(TMessage msg, CancellationToken cancellationToken)
            where TMessage : IMessage
        {
            // MakeGenericType方法反射指定 ReceiveContext<> 中泛型参数类型为 msg类型，通过CreateInstance进行有参构造
            //创建一个 receiveContext 接收上下文
            var receiveContext =
                (IReceiveContext<TMessage>)Activator.CreateInstance(
                    typeof(ReceiveContext<>).MakeGenericType(msg.GetType()), msg);

            // 向receiveContext中加入mediator中所拥有的pipeline
            RegisterServiceIfRequired(receiveContext);

            //获取表示 object 类型的 System.Type 对象
            receiveContext.ResultDataType = typeof(object);

            var task = _globalPipe.Connect((IReceiveContext<IMessage>)receiveContext, cancellationToken);

            var result = await task.ConfigureAwait(false);

            return receiveContext.Result ?? result;
        }


        //看此处可能时，可能会有个小疑问就是，
        //SendMessage<TMessage>(IReceiveContext<TMessage> customReceiveContext， CancellationToken cancellationToken)
        //只有IEvent是可以使用到publishAsync,那为啥 IRequest和ICommand都用到了而IReceiveContext，因为 而IReceiveContext 是所有消息
        //的上下文，而只有IEvent是可以进行消息重推的，而这个方法的正确使用：
        //大概可能应该是：
        //          （1）IEvent需要消息重推的时候，使用该方法
        //          （2）自定义 IReceiveContext 上下文的时候使用到

        //ICommand,IEvent,IRequest 可调到此处
        //为什么此处会的SendMessage<TMessage> 会允许存在返回值呢？
        //首先他的入参是 IReceiveContext<TMessage>，而IReceiveContext<TMessage>是允许消息重推，并且会先执行一遍 publishPipeLine
        //然后再调用 PublishAsync 进行重推

        // 但是有个疑问的是 该方法允许 ICommand，IRequest进入进行消息处理，但是  ICommand，IRequest似乎无法使用到
        // IReceiveContext中的PublishAsync中的重推功能

        //ICommand 和 IEvent  如果使用这个的话是可以设置到返回值了，
        //而不是 SendMessage<TMessage>(TMessage msg, CancellationToken cancellationToken)中无法获取到返回值
        private async Task<object> SendMessage<TMessage>(IReceiveContext<TMessage> customReceiveContext,
            CancellationToken cancellationToken)
            where TMessage : IMessage
        {
            RegisterServiceIfRequired(customReceiveContext);

            var task = _globalPipe.Connect((IReceiveContext<IMessage>)customReceiveContext, cancellationToken);
            return await task.ConfigureAwait(false);
        }

        private void RegisterServiceIfRequired<TMessage>(IReceiveContext<TMessage> receiveContext)
            where TMessage : IMessage
        {
            //从这块代码可以直接看出，IPublishContext类 和 IReceiveContext类 这两个上下文类是服务与 哪些 IMessage
            // IPublishContext上下文是IEvent的message的 上下文
            // IReceiveContext上下文是ICommand，IEvent，IRequest的message的 上下文


            // IPublishContext 和 IReceiveContext 的区别在于：在 IReceiveContext 中可以进行消息重推，IPublishContext无法进行消
            // 息重新推
            receiveContext.RegisterService(this);
            if (!receiveContext.TryGetService(out IPublishPipe<IPublishContext<IEvent>> _))
            {
                receiveContext.RegisterService(_publishPipe);
            }

            if (!receiveContext.TryGetService(out ICommandReceivePipe<IReceiveContext<ICommand>> _))
            {
                receiveContext.RegisterService(_commandReceivePipe);
            }

            if (!receiveContext.TryGetService(out IEventReceivePipe<IReceiveContext<IEvent>> _))
            {
                receiveContext.RegisterService(_eventReceivePipe);
            }

            if (!receiveContext.TryGetService(out IRequestReceivePipe<IReceiveContext<IRequest>> _))
            {
                receiveContext.RegisterService(_requestPipe);
            }
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}