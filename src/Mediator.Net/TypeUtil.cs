using System;
using System.Linq;
using System.Reflection;

namespace Mediator.Net;

public class TypeUtil
{
    
    //判断是否有与之适配的泛型类型
    public static bool IsAssignableToGenericType(Type givenType, Type genericType)
    {
        //获取该类所实现的所有接口 type
        var interfaceTypes = givenType.GetTypeInfo().ImplementedInterfaces;
        //判断该类所实现的接口 上是否有对应匹配的类型
        if (interfaceTypes.Any(it => it.GetTypeInfo().IsGenericType && it.GetGenericTypeDefinition() == genericType))
            return true;
        //判断本类上是否有实现 对应匹配的类型
        if (givenType.GetTypeInfo().IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            return true;
        
        //获取父类 type
        Type baseType = givenType.GetTypeInfo().BaseType;
        //对起父类进行扫描
        return baseType != null && IsAssignableToGenericType(baseType, genericType);
    }
}