using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mediator.Net.Binding;
using Mediator.Net.Context;
using Mediator.Net.Contracts;
using Mediator.Net.Pipeline.Exceptions;

namespace Mediator.Net.Pipeline
{
    public static class PipeHelper
    {
        public static List<MessageBinding> GetHandlerBindings<TContext>(TContext context, bool messageTypeExactMatch,
            MessageHandlerRegistry messageHandlerRegistry) where TContext : IContext<IMessage>
        {
            // true 为准确类型匹配，false为非准确类型匹配
            var handlerBindings = messageTypeExactMatch
                ? messageHandlerRegistry.MessageBindings
                    .Where(x =>
                        x.MessageType == context.Message.GetType())
                    .ToList()
                : messageHandlerRegistry.MessageBindings
                    .Where(x =>
                        x.MessageType.GetTypeInfo().IsAssignableFrom(context.Message.GetType().GetTypeInfo()))
                    .ToList();
            if (!handlerBindings.Any())
                throw new NoHandlerFoundException(context.Message.GetType());
            return handlerBindings;
        }

        public static bool IsHandleMethod(MethodInfo m, Type messageType, bool isForEvent)
        {
            //检查Handle方法的第一个参数的泛型参数中是否包含messageType对应的类型
            //（说明：messageType可不在第一个泛型参数上）
            //检查Handle方法的第一个参数的第一个泛型参数是否是messageType对应的类型
            var exactMatch = m.Name == "Handle" && m.IsPublic && m.GetParameters().Any()
                             && (m.GetParameters()[0].ParameterType.GenericTypeArguments.Contains(messageType) ||
                                 m.GetParameters()[0].ParameterType.GenericTypeArguments.First().GetTypeInfo()
                                     .Equals(messageType.GetTypeInfo()));
            //是否为event接收，如果不是直接返回
            if (!isForEvent) return exactMatch;

            //event准确查询是否成功
            if (exactMatch)
                return true;
            
            //检查Handle方法的第一个参数的泛型参数中是否包含messageType对应的类型
            //检查Handle方法的第一个参数的第一个泛型参数是否是messageType的父类
            //注意：只有event才可以进行该次判断
            return m.Name == "Handle" && m.IsPublic && m.GetParameters().Any()
                   && (m.GetParameters()[0].ParameterType.GenericTypeArguments.Contains(messageType) ||
                       m.GetParameters()[0].ParameterType.GenericTypeArguments.First().GetTypeInfo()
                           .IsAssignableFrom(messageType.GetTypeInfo()));
        }

        //检查task是否具有result属性
        //检查task是否为泛型
        //  为什么要检查task是否为泛型？
        //     非泛型的Task表示一个异步操作，但没有指定操作的返回类型。
        //      Task task = SomeAsyncMethod();
        //     泛型的 Task<TResult> 表示异步操作，并且指定了操作的返回类型 TResult 
        //       Task<int> task = GetNumberAsync();
        //获取result属性的get方法
        //调用get方法胡傲气结构
        public static object GetResultFromTask(Task task)
        {
            if (task.GetType().GetRuntimeProperty("Result") == null)
                return null;

            if (!task.GetType().GetTypeInfo().IsGenericType)
            {
                throw new Exception("A task without a result is returned");
            }

            var result = task.GetType().GetRuntimeProperty("Result").GetMethod;
            return result.Invoke(task, new object[] { });
        }
    }
}