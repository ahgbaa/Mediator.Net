﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mediator.Net.Contracts;

namespace Mediator.Net.Context
{
    public class PublishContext : IPublishContext<IEvent>
    {

        private readonly IList<object> _registeredServices;
        private Dictionary<string, object> _metaData;
        public PublishContext(IEvent message)
        {
            Message = message;
            _registeredServices = new List<object>();
        }
        public IEvent Message { get; }
        public void RegisterService<T>(T service)
        {
            _registeredServices.Add(service);
        }

        public bool TryGetService<T>(out T service)
        {
            //通过检查 _registeredServices 列表中的每个项,: 项的类型与指定类型 T 相同 和 项是指定类型 T 的子类或实现类。
            var result = _registeredServices.LastOrDefault(x => x.GetType() == typeof(T) || x is T);
            if (result != null)
            {
                service = (T)result;
                return true;
            }
            service = default(T);
            return false;

        }

        public Dictionary<string, object> MetaData
        {
            get
            {
                _metaData = _metaData ?? new Dictionary<string, object>();
                return _metaData;
            }
        }

        public object Result { get; set; }
        
        public Type ResultDataType { get; set; }
    }
}