using System;

namespace SESimulator.Runtime
{
    public interface ISimulated
    {
        /// <summary>
        /// Attempt to pass time.
        /// </summary>
        /// <remarks>
        /// Returns a new snapshot structure representing the object's current idea of 'now'.
        /// </remarks>
        /// <param name="lastSnapshot"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        Snapshot TryAdvance(Snapshot lastSnapshot, TimeSpan target);
    }
}