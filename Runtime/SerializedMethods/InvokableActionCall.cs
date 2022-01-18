namespace ExtEvents
{
    using System;
    using System.Reflection;
    using UnityEngine.Scripting;

    // We use [Preserve] a lot here because link.xml cannot be used in a package.
    
    [Preserve]
    public abstract class BaseInvokableCall
    {
        protected BaseInvokableCall(object target, MethodInfo method) { }

        [Preserve]
        public abstract void Invoke(object[] args);
    }

    [Preserve]
    public class InvokableActionCall : BaseInvokableCall
    {
        private readonly Action _delegate;

        [Preserve]
        public InvokableActionCall(object target, MethodInfo method) : base(target, method)
        {
            _delegate = (Action) Delegate.CreateDelegate(typeof(Action), target, method);
        }

        [Preserve]
        public static BaseInvokableCall Create(object target, MethodInfo method)
        {
            return new InvokableActionCall(target, method);
        }
        
        public override void Invoke(object[] args)
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

        [Preserve]
        public static BaseInvokableCall Create(object target, MethodInfo method)
        {
            return new InvokableActionCall<T>(target, method);
        }

        public override void Invoke(object[] args)
        {
            _delegate((T) args[0]);
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

        [Preserve]
        public static BaseInvokableCall Create(object target, MethodInfo method)
        {
            return new InvokableActionCall<T1, T2>(target, method);
        }

        public override void Invoke(object[] args)
        {
            _delegate((T1) args[0], (T2) args[1]); 
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

        [Preserve]
        public static BaseInvokableCall Create(object target, MethodInfo method)
        {
            return new InvokableActionCall<T1, T2, T3>(target, method);
        }

        public override void Invoke(object[] args)
        {
            _delegate((T1) args[0], (T2) args[1], (T3) args[2]);
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

        [Preserve]
        public static BaseInvokableCall Create(object target, MethodInfo method)
        {
            return new InvokableFuncCall<TReturn>(target, method);
        }

        public override void Invoke(object[] args)
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

        [Preserve]
        public static BaseInvokableCall Create(object target, MethodInfo method)
        {
            return new InvokableFuncCall<T, TReturn>(target, method);
        }

        public override void Invoke(object[] args)
        {
            _delegate((T) args[0]);
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

        [Preserve]
        public static BaseInvokableCall Create(object target, MethodInfo method)
        {
            return new InvokableFuncCall<T1, T2, TReturn>(target, method);
        }

        public override void Invoke(object[] args)
        {
            _delegate((T1) args[0], (T2) args[1]);
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

        [Preserve]
        public static BaseInvokableCall Create(object target, MethodInfo method)
        {
            return new InvokableFuncCall<T1, T2, T3, TReturn>(target, method);
        }

        public override void Invoke(object[] args)
        {
            _delegate((T1) args[0], (T2) args[1], (T3) args[2]);
        }
    }
}