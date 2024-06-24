using System;
using System.Collections.Generic;
using Mediator.Net.Contracts;

namespace Mediator.Net.Context
{
 
    // TMessage是一个协变类型参数
    //规定了TMessage必须实现IMessage
    public interface IContext<out TMessage> where TMessage : IMessage
    {
        TMessage Message { get; }
        void RegisterService<T>(T service);
        bool TryGetService<T>(out T service);
        Dictionary<string, object> MetaData { get; }
        object Result { get; set; }
        
        Type ResultDataType { get; set; }
        
    }
}
