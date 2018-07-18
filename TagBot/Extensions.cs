using System;
using System.Threading.Tasks;

namespace TagBot
{
    public static class Extensions
    {
        public static async Task<T> TimeoutAndFallback<T>(this Task<T> task, TimeSpan timeout, Task<T> fallback)
        {
            var delay = Task.Delay(timeout);
            var result = await Task.WhenAny(task, delay).ConfigureAwait(false);
            if (result == delay)
                return await fallback;
            return await task.ConfigureAwait(false);
        }
    }
}
