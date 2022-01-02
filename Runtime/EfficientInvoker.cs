namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.Assertions;

    public sealed class EfficientInvoker
    {
        private static readonly Dictionary<MemberInfo, EfficientInvoker> _methodToWrapperMap
            = new Dictionary<MemberInfo, EfficientInvoker>();

        private readonly Action<object, object[]> _func;

        private EfficientInvoker(Action<object, object[]> func)
        {
            _func = func;
        }

        public static EfficientInvoker ForMethod(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            if (_methodToWrapperMap.TryGetValue(methodInfo, out var func))
            {
                return func;
            }

            var wrapper = CreateMethodWrapper(methodInfo, false);
            func = new EfficientInvoker(wrapper);
            _methodToWrapperMap.Add(methodInfo, func);
            return func;
        }

        public static EfficientInvoker ForMethodAot(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            // if (_methodToWrapperMap.TryGetValue(methodInfo, out var func))
            // {
            //     return func;
            // }

            var wrapper = CreateMethodWrapper(methodInfo, true);
            var func = new EfficientInvoker(wrapper);
            // _methodToWrapperMap.Add(methodInfo, func);
            return func;
        }

        public static EfficientInvoker ForProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException(nameof(propertyInfo));

            if (_methodToWrapperMap.TryGetValue(propertyInfo, out var func))
            {
                return func;
            }

            var wrapper = CreatePropertyWrapper(propertyInfo);
            func = new EfficientInvoker(wrapper);
            _methodToWrapperMap.Add(propertyInfo, func);
            return func;
        }

        public void Invoke(object target, params object[] args)
        {
            _func(target, args);
        }

        private static LambdaExpression CreateMethodLambda(MethodInfo method)
        {
            CreateParamsExpressions(method, out ParameterExpression argsExp, out Expression[] paramsExps);

            var targetExp = Expression.Parameter(typeof(object), "target");
            Assert.IsNotNull(method.DeclaringType);
            var castTargetExp = Expression.Convert(targetExp, method.DeclaringType);
            var invokeExp = Expression.Call(castTargetExp, method, paramsExps);
            return Expression.Lambda(invokeExp, targetExp, argsExp);
        }

        private static Action<object, object[]> CreateMethodWrapper(MethodInfo method, bool aot)
        {
            var lambda = CreateMethodLambda(method);

            if (aot)
            {
                return lambda.CompileAot();
            }

            var compiledLambda = lambda.Compile();
            return (Action<object, object[]>)compiledLambda;
        }

        private static void CreateParamsExpressions(MethodBase method, out ParameterExpression argsExp, out Expression[] paramsExps)
        {
            var parameters = method.GetParameters();

            argsExp = Expression.Parameter(typeof(object[]), "args");
            paramsExps = new Expression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var constExp = Expression.Constant(i, typeof(int));
                var argExp = Expression.ArrayIndex(argsExp, constExp);
                paramsExps[i] = Expression.Convert(argExp, parameters[i].ParameterType);
            }
        }

        private static Action<object, object[]> CreatePropertyWrapper(PropertyInfo propertyInfo)
        {
            var targetExp = Expression.Parameter(typeof(object), "target");
            var argsExp = Expression.Parameter(typeof(object[]), "args");
            Assert.IsNotNull(propertyInfo.DeclaringType);
            var castArgExp = Expression.Convert(targetExp, propertyInfo.DeclaringType);
            var propExp = Expression.Property(castArgExp, propertyInfo);
            var castPropExp = Expression.Convert(propExp, typeof(object));
            var lambdaExp = Expression.Lambda(castPropExp, targetExp, argsExp);
            var lambda = lambdaExp.Compile();
            return (Action<object, object[]>) lambda;
        }
    }
}