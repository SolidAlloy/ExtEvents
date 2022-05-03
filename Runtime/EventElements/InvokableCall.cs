namespace ExtEvents
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEngine.Scripting;

    // We use [Preserve] a lot here because link.xml cannot be used in a package.

    /// <summary>
    /// An invokable call constructs a delegate from a method info so that it is invoked efficiently.
    /// The generic types derived from BaseInvokableCall are constructed at runtime by <see cref="PersistentListener"/>.
    /// </summary>
    [Preserve]
    public abstract class BaseInvokableCall
    {
        public readonly MethodInfo Method;

        protected BaseInvokableCall(object target, MethodInfo method)
        {
            Method = method;
        }

        [Preserve]
        public static BaseInvokableCall CreateAction<T>(object target, MethodInfo method)
        {
            return new InvokableActionCall<T>(target, method);
        }

        [Preserve]
        public static BaseInvokableCall CreateAction<T1, T2>(object target, MethodInfo method)
        {
            return new InvokableActionCall<T1, T2>(target, method);
        }

        [Preserve]
        public static BaseInvokableCall CreateAction<T1, T2, T3>(object target, MethodInfo method)
        {
            return new InvokableActionCall<T1, T2, T3>(target, method);
        }

        [Preserve]
        public static BaseInvokableCall CreateAction<T1, T2, T3, T4>(object target, MethodInfo method)
        {
            return new InvokableActionCall<T1, T2, T3, T4>(target, method);
        }

        [Preserve]
        public static BaseInvokableCall CreateFunc<TResult>(object target, MethodInfo method)
        {
            return new InvokableFuncCall<TResult>(target, method);
        }
        [Preserve]
        public static BaseInvokableCall CreateFunc<T1, TResult>(object target, MethodInfo method)
        {
            return new InvokableFuncCall<T1, TResult>(target, method);
        }

        [Preserve]
        public static BaseInvokableCall CreateFunc<T1, T2, TResult>(object target, MethodInfo method)
        {
            return new InvokableFuncCall<T1, T2, TResult>(target, method);
        }

        [Preserve]
        public static BaseInvokableCall CreateFunc<T1, T2, T3, TResult>(object target, MethodInfo method)
        {
            return new InvokableFuncCall<T1, T2, T3, TResult>(target, method);
        }

        [Preserve]
        public static BaseInvokableCall CreateFunc<T1, T2, T3, T4, TResult>(object target, MethodInfo method)
        {
            return new InvokableFuncCall<T1, T2, T3, T4, TResult>(target, method);
        }

        [Preserve]
        public abstract unsafe void Invoke(void*[] args);
    }

    [Preserve]
    public class InvokableActionCall : BaseInvokableCall
    {
        private readonly Action _delegate;

        [Preserve]
        public InvokableActionCall(object target, MethodInfo method) : base(target, method)
        {
            if (method.IsStatic)
            {
                _delegate = (Action) Delegate.CreateDelegate(typeof(Action), method);
            }
            else
            {
                _delegate = (Action) Delegate.CreateDelegate(typeof(Action), target, method);
            }
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate();
        }
    }

    [Preserve]
    public class InvokableActionCall<T> : BaseInvokableCall
    {
        private readonly Action<T> _delegate;

        [Preserve]
        public InvokableActionCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Action<T>) Delegate.CreateDelegate(typeof(Action<T>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate(Unsafe.Read<T>(args[0]));
        }
    }

    [Preserve]
    public class InvokableActionCall<T1, T2> : BaseInvokableCall
    {
        private readonly Action<T1, T2> _delegate;

        [Preserve]
        public InvokableActionCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Action<T1, T2>) Delegate.CreateDelegate(typeof(Action<T1, T2>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate(Unsafe.Read<T1>(args[0]), Unsafe.Read<T2>(args[1]));
        }
    }

    [Preserve]
    public class InvokableActionCall<T1, T2, T3> : BaseInvokableCall
    {
        private readonly Action<T1, T2, T3> _delegate;

        [Preserve]
        public InvokableActionCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Action<T1, T2, T3>) Delegate.CreateDelegate(typeof(Action<T1, T2, T3>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate(Unsafe.Read<T1>(args[0]), Unsafe.Read<T2>(args[1]), Unsafe.Read<T3>(args[2]));
        }
    }

    [Preserve]
    public class InvokableActionCall<T1, T2, T3, T4> : BaseInvokableCall
    {
        private readonly Action<T1, T2, T3, T4> _delegate;

        [Preserve]
        public InvokableActionCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Action<T1, T2, T3, T4>) Delegate.CreateDelegate(typeof(Action<T1, T2, T3, T4>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate(Unsafe.Read<T1>(args[0]), Unsafe.Read<T2>(args[1]), Unsafe.Read<T3>(args[2]), Unsafe.Read<T4>(args[3]));
        }
    }

    [Preserve]
    public class InvokableFuncCall<TReturn> : BaseInvokableCall
    {
        private readonly Func<TReturn> _delegate;

        [Preserve]
        public InvokableFuncCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Func<TReturn>) Delegate.CreateDelegate(typeof(Func<TReturn>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate();
        }
    }

    [Preserve]
    public class InvokableFuncCall<T, TReturn> : BaseInvokableCall
    {
        private readonly Func<T, TReturn> _delegate;

        [Preserve]
        public InvokableFuncCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Func<T, TReturn>) Delegate.CreateDelegate(typeof(Func<T, TReturn>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate(Unsafe.Read<T>(args[0]));
        }
    }

    [Preserve]
    public class InvokableFuncCall<T1, T2, TReturn> : BaseInvokableCall
    {
        private readonly Func<T1, T2, TReturn> _delegate;

        [Preserve]
        public InvokableFuncCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Func<T1, T2, TReturn>) Delegate.CreateDelegate(typeof(Func<T1, T2, TReturn>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate(Unsafe.Read<T1>(args[0]), Unsafe.Read<T2>(args[1]));
        }
    }

    [Preserve]
    public class InvokableFuncCall<T1, T2, T3, TReturn> : BaseInvokableCall
    {
        private readonly Func<T1, T2, T3, TReturn> _delegate;

        [Preserve]
        public InvokableFuncCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Func<T1, T2, T3, TReturn>) Delegate.CreateDelegate(typeof(Func<T1, T2, T3, TReturn>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate(Unsafe.Read<T1>(args[0]), Unsafe.Read<T2>(args[1]), Unsafe.Read<T3>(args[2]));
        }
    }

    [Preserve]
    public class InvokableFuncCall<T1, T2, T3, T4, TReturn> : BaseInvokableCall
    {
        private readonly Func<T1, T2, T3, T4, TReturn> _delegate;

        [Preserve]
        public InvokableFuncCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Func<T1, T2, T3, T4, TReturn>) Delegate.CreateDelegate(typeof(Func<T1, T2, T3, T4, TReturn>), target, method);
        }

        public override unsafe void Invoke(void*[] args)
        {
            _delegate(Unsafe.Read<T1>(args[0]), Unsafe.Read<T2>(args[1]), Unsafe.Read<T3>(args[2]), Unsafe.Read<T4>(args[3]));
        }
    }
}