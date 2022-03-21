using System;

namespace Unordinal.Editor.Utils
{
    public static class ReferenceExtensions
    {
        public static void WithStrongReference(this WeakReference<object> target, Action<object> consumer)
        {
            object strongReference;
            if (target.TryGetTarget(out strongReference))
            {
                consumer(strongReference);
            }
        }
    }
}
