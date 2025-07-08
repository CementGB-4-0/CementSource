using System;
using System.Collections.Concurrent;

namespace CementGB.Mod.Utilities;

internal static class MainThreadDispatcher
{
    private static ConcurrentQueue<Action> toDispatch = new();


    public static void QueueActionForMainThread(Action action) => toDispatch.Enqueue(action);

    public static void DispatchActions()
    {
        if (toDispatch.IsEmpty) return;

        for (int i = 0; i < toDispatch.Count; i++)
        {
            if (toDispatch.TryDequeue(out Action method))
                method.Invoke();
        }
    }
}
