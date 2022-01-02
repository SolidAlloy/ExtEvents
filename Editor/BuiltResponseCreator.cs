namespace ExtEvents.Editor
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using GenericUnityObjects.Editor;
    using UnityEngine.Assertions;

    public static class BuiltResponseCreator
    {
        public static string CreateBuiltResponseAssembly(string assemblyName, MethodInfo method)
        {
            using var concreteClassAssembly = AssemblyCreatorHelper.CreateConcreteClassAssembly(assemblyName, "BuiltResponseInheritor", typeof(BuiltResponse));
            CreateInvokeMethod(concreteClassAssembly.TypeBuilder, method);
            return concreteClassAssembly.Path;
        }

        private static void CreateInvokeMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            MethodBuilder pInvoke = typeBuilder.DefineMethod(
                "Invoke",
                MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.HideBySig,
                null,
                new Type[] { typeof(object), typeof(object[]) });

            ILGenerator pILInvoke = pInvoke.GetILGenerator();

            // IL_0001: ldarg.1      // obj
            pILInvoke.Emit(OpCodes.Ldarg_1);
            // IL_0002: castclass    ExtEvents.TestTarget
            Assert.IsNotNull(method.DeclaringType);
            pILInvoke.Emit(OpCodes.Castclass, method.DeclaringType);

            var paramInfos = method.GetParameters();

            for (int i = 0; i < paramInfos.Length; i++)
            {
                // IL_0008: ldarg.2      // args
                pILInvoke.Emit(OpCodes.Ldarg_2);

                // IL_0009: ldc.i4.0
                pILInvoke.Emit(OpCodes.Ldc_I4, i);

                // IL_000a: ldelem.ref
                pILInvoke.Emit(OpCodes.Ldelem_Ref);

                // IL_000b: unbox.any    [mscorlib]System.Int32
                var paramType = paramInfos[i].ParameterType;
                pILInvoke.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
            }

            // IL_001d: callvirt     instance void ExtEvents.TestTarget::Test(int32, bool)
            pILInvoke.EmitCall(OpCodes.Callvirt, method, null);

            // IL_0023: ret
            pILInvoke.Emit(OpCodes.Ret);

            var invokeDeclaration = typeof(BuiltResponse).GetMethod(
                nameof(BuiltResponse.Invoke),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null,
                new[] { typeof(object), typeof(object[]) }, null);

            Assert.IsNotNull(invokeDeclaration);

            typeBuilder.DefineMethodOverride(pInvoke, invokeDeclaration);
        }
    }
}