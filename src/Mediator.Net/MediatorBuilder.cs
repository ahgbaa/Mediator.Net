using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mediator.Net.Binding;
using Mediator.Net.Context;
using Mediator.Net.Contracts;
using Mediator.Net.Pipeline.Command;
using Mediator.Net.Pipeline.Event;
using Mediator.Net.Pipeline.Global;
using Mediator.Net.Pipeline.PublishPipe;
using Mediator.Net.Pipeline.Request;

namespace Mediator.Net
{
    public class MediatorBuilder
    {
        //全局接受管道配置操作
        private Action<IGlobalReceivePipeConfigurator> _globalReceivePipeConfiguratorAction;

        //指令接受管道配置操作
        private Action<ICommandReceivePipeConfigurator> _commandReceivePipeConfiguratorAction;

        //事件接受管道配置操作
        private Action<IEventReceivePipeConfigurator> _eventReceivePipeConfiguratorAction;

        //请求接受管道配置操作
        private Action<IRequestPipeConfigurator<IReceiveContext<IRequest>>> _requestPipeConfiguratorAction;

        //订阅接受管道配置操作
        private Action<IPublishPipeConfigurator> _publishPipeConfiguratorAction;
        public MessageHandlerRegistry MessageHandlerRegistry { get; }

        public MediatorBuilder()
        {
            MessageHandlerRegistry = new MessageHandlerRegistry();
        }

        public MediatorBuilder RegisterHandlers(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                ScanRegistration(assembly.DefinedTypes);
            }

            return this;
        }

        public MediatorBuilder RegisterHandlers(Func<Assembly, IEnumerable<TypeInfo>> filter,
            params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                ScanRegistration(filter(assembly));
            }

            return this;
        }

        public MediatorBuilder RegisterHandlers(IList<MessageBinding> messageHandlerPairs)
        {
            return RegisterHandlers(new HashSet<MessageBinding>(messageHandlerPairs));
        }

        public MediatorBuilder RegisterHandlers(HashSet<MessageBinding> messageBindings)
        {
            MessageHandlerRegistry.MessageBindings = messageBindings;
            return this;
        }

        public MediatorBuilder RegisterHandlers(Func<IList<MessageBinding>> setupBindings)
        {
            return RegisterHandlers(setupBindings());
        }

        public MediatorBuilder RegisterHandlers(Func<HashSet<MessageBinding>> setupBindings)
        {
            MessageHandlerRegistry.MessageBindings = setupBindings();
            return this;
        }

        public MediatorBuilder ConfigureGlobalReceivePipe(Action<IGlobalReceivePipeConfigurator> configurator)
        {
            _globalReceivePipeConfiguratorAction = configurator;
            return this;
        }

        public MediatorBuilder ConfigureCommandReceivePipe(Action<ICommandReceivePipeConfigurator> configurator)
        {
            _commandReceivePipeConfiguratorAction = configurator;
            return this;
        }

        public MediatorBuilder ConfigureEventReceivePipe(Action<IEventReceivePipeConfigurator> configurator)
        {
            _eventReceivePipeConfiguratorAction = configurator;
            return this;
        }

        public MediatorBuilder ConfigureRequestPipe(
            Action<IRequestPipeConfigurator<IReceiveContext<IRequest>>> configurator)
        {
            _requestPipeConfiguratorAction = configurator;
            return this;
        }

        public MediatorBuilder ConfigurePublishPipe(Action<IPublishPipeConfigurator> configurator)
        {
            _publishPipeConfiguratorAction = configurator;
            return this;
        }


        public IMediator Build()
        {
            return BuildMediator();
        }

        public IMediator Build(IDependencyScope scope)
        {
            return BuildMediator(scope);
        }

        private IMediator BuildMediator(IDependencyScope scope = null)
        {
            var commandReceivePipeConfigurator = new CommandReceivePipeConfigurator(MessageHandlerRegistry, scope);
            _commandReceivePipeConfiguratorAction?.Invoke(commandReceivePipeConfigurator);
            var commandReceivePipe = commandReceivePipeConfigurator.Build();

            var eventReceivePipeConfigurator = new EventReceivePipeConfigurator(MessageHandlerRegistry, scope);
            _eventReceivePipeConfiguratorAction?.Invoke(eventReceivePipeConfigurator);
            var eventReceivePipe = eventReceivePipeConfigurator.Build();


            var requestPipeConfigurator = new RequestPipeConfigurator(MessageHandlerRegistry, scope);
            _requestPipeConfiguratorAction?.Invoke(requestPipeConfigurator);
            var requestPipe = requestPipeConfigurator.Build();

            var publishPipeConfigurator = new PublishPipeConfigurator(scope);
            _publishPipeConfiguratorAction?.Invoke(publishPipeConfigurator);
            var publishPipe = publishPipeConfigurator.Build();

            var globalPipeConfigurator = new GlobalRececivePipeConfigurator(scope);
            _globalReceivePipeConfiguratorAction?.Invoke(globalPipeConfigurator);
            var globalReceivePipe = globalPipeConfigurator.Build();

            return new Mediator(commandReceivePipe, eventReceivePipe, requestPipe, publishPipe, globalReceivePipe,
                scope);
        }


        private void ScanRegistration(IEnumerable<TypeInfo> typeInfos)
        {
            //先把本身类或实现类以及父类中未实现ICommandHandler，IEventHandler，IRequestHandler，IStreamRequestHandler
            //的类进行一次筛选，全部排除出去
            var handlers = typeInfos.Where(x => !x.IsAbstract &&
                                                (TypeUtil.IsAssignableToGenericType(x.AsType(),
                                                     typeof(ICommandHandler<>)) ||
                                                 TypeUtil.IsAssignableToGenericType(x.AsType(),
                                                     typeof(ICommandHandler<,>)) ||
                                                 TypeUtil.IsAssignableToGenericType(x.AsType(),
                                                     typeof(IEventHandler<>)) ||
                                                 TypeUtil.IsAssignableToGenericType(x.AsType(),
                                                     typeof(IRequestHandler<,>)) ||
                                                 TypeUtil.IsAssignableToGenericType(x.AsType(),
                                                     typeof(IStreamRequestHandler<,>))
                                                )).ToList();
            foreach (var handler in handlers)
            {
                foreach (var implementedInterface in handler.ImplementedInterfaces)
                {
                    if (TypeUtil.IsAssignableToGenericType(implementedInterface, typeof(ICommandHandler<>)) ||
                        TypeUtil.IsAssignableToGenericType(implementedInterface, typeof(ICommandHandler<,>)) ||
                        TypeUtil.IsAssignableToGenericType(implementedInterface, typeof(IEventHandler<>)) ||
                        TypeUtil.IsAssignableToGenericType(implementedInterface, typeof(IRequestHandler<,>)) ||
                        TypeUtil.IsAssignableToGenericType(implementedInterface, typeof(IStreamRequestHandler<,>))
                       )
                    {
                        //进行message绑定注册：泛型接口中的第一个泛型参数作为key，handler作为value进行message绑定
                        MessageHandlerRegistry.MessageBindings.Add(
                            new MessageBinding(implementedInterface.GenericTypeArguments[0], handler.AsType()));
                    }
                }
            }
        }
    }
}