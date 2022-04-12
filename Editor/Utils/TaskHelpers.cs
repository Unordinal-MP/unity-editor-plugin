using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unordinal.Editor.Utils
{
    public static class TaskHelpers
    {
        public static async Task<T> RetryWithCondition<T>(Func<Task<T>> action, Func<T, bool> condition, CancellationToken cancellationToken, int maxRetries = 100, int delay = 500)
        {
            return await Retry<T>(async () => {
                var result = await action().ConfigureAwait(false);
                if (!condition(result)) {
                    throw new ConditionNotSatisfiedException($"Condition {condition} not satisfied");
                }
                return result;
            }, cancellationToken, maxRetries, delay);
        }
        
        public static async Task<T> RetryWithConditionAndBreakCondition<T>(Func<Task<T>> action, Func<T, bool> condition, Func<T, bool> breakCondition, CancellationToken cancellationToken, int maxRetries = 100, int delay = 500)
        {
            var retryResult =  await Retry<T>(async () => {
                var result = await action().ConfigureAwait(false);
                if (breakCondition(result))
                {
                    return result;
                }
                if (!condition(result)) {
                    throw new ConditionNotSatisfiedException($"Condition {condition} not satisfied");
                }
                return result;
            }, cancellationToken, maxRetries, delay);

            if (breakCondition(retryResult))
            {
                throw new BreakConditionSatisfiedException($"Condition {breakCondition} was satisfied");
            }
            return retryResult;
        }

        public static async Task<T> RetryWithCondition<T>(Func<Task<T>> action, Func<T, bool> condition, int maxRetries = 100, int delay = 500)
        {
            return await RetryWithCondition(action, condition, CancellationToken.None, maxRetries, delay);
        }

        public static async Task<T> Retry<T>(Func<Task<T>> action, CancellationToken cancellationToken, int maxRetries = 100, int delay = 500)
        {
            var errors = new List<Exception>();
            for (int i = 0; i < maxRetries; ++i)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    errors.Add(new OperationCanceledException(cancellationToken));
                    break;
                }
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    errors.Add(e);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
            throw new AggregateException("Retry limit reached", errors);
        }

        public static async Task<T> Retry<T>(Func<Task<T>> action, int maxRetries = 100, int delay = 500)
        {
            return await Retry(action, CancellationToken.None, maxRetries, delay);
        }

        public static async Task RunAsync(Action work)
        {
            //Unity engine silences async exceptions otherwise...
            await Task.Run(() =>
            {
                try
                {
                    work();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            });
        }
    }

    public class ConditionNotSatisfiedException : Exception
    {
        public ConditionNotSatisfiedException(string message) : base(message)
        {
        }
    }
    
    public class BreakConditionSatisfiedException : Exception
    {
        public BreakConditionSatisfiedException(string message) : base(message)
        {
        }
    }
}
