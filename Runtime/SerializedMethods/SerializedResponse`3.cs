namespace ExtEvents
{
    using System;

    [Serializable]
    public class SerializedResponse<T1, T2, T3>
    {
        // the arguments are passed here from extevent, but the response decides how to shuffle and prepare them for passing to response
        // it also holds serialized arguments if any
        public void Invoke(T1 arg1, T2 arg2, T3 arg)
        {

        }
    }
}