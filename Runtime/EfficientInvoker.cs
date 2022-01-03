namespace ExtEvents
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using UnityEngine.Assertions;

    public sealed class EfficientInvoker
    {
        private static readonly Dictionary<MemberInfo, EfficientInvoker> _memberToWrapperMap
            = new Dictionary<MemberInfo, EfficientInvoker>();

        private readonly Action<object, object[]> _func;

        private EfficientInvoker(Action<object, object[]> func)
        {
            _func = func;
        }

        public static EfficientInvoker Create(MemberInfo memberInfo)
        {
            if (memberInfo == null)
                throw new ArgumentNullException(nameof(memberInfo));

            if (_memberToWrapperMap.TryGetValue(memberInfo, out var func))
            {
                return func;
            }

            var wrapper = CreateMemberWrapper(memberInfo);
            func = new EfficientInvoker(wrapper);
            _memberToWrapperMap.Add(memberInfo, func);
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

        private static Action<object, object[]> CreateMethodWrapper(MethodInfo method)
        {
            var lambda = CreateMethodLambda(method);
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

        private static Action<object, object[]> CreateMemberWrapper(MemberInfo memberInfo)
        {
            if (memberInfo is MethodInfo methodInfo)
                return CreateMethodWrapper(methodInfo);

            var targetExp = Expression.Parameter(typeof(object), "target");
            var argsExp = Expression.Parameter(typeof(object[]), "args");
            Assert.IsNotNull(memberInfo.DeclaringType);
            var castArgExp = Expression.Convert(targetExp, memberInfo.DeclaringType);
            var propExp = (memberInfo is FieldInfo fieldInfo) ? Expression.Field(castArgExp, fieldInfo) : Expression.Property(castArgExp, (PropertyInfo) memberInfo);
            var castPropExp = Expression.Convert(propExp, typeof(object));
            var lambdaExp = Expression.Lambda(castPropExp, targetExp, argsExp);
            var lambda = lambdaExp.Compile();
            return (Action<object, object[]>) lambda;
        }
    }
}