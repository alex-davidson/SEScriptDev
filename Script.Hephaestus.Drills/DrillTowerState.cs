namespace IngameScript
{
    public enum DrillTowerState
    {
        /// <summary>
        /// Drill state is unknown and will be inferred.
        /// </summary>
        None = 0,
        /// <summary>
        /// Drill is stopped, and clamped if possible.
        /// </summary>
        Stopped = 1,
        /// <summary>
        /// Drill is preparing for operation.
        /// </summary>
        Starting = 2,
        /// <summary>
        /// Drill is extending.
        /// </summary>
        Drilling = 3,
        /// <summary>
        /// Drill is retracting.
        /// </summary>
        Retrieving = 4,
        /// <summary>
        /// Drill has retracted, preparing to shut down.
        /// </summary>
        Stopping = 5,
        /// <summary>
        /// Drill has stopped where it is and will not reactivate. Must be made safe.
        /// </summary>
        EmergencyStopped = 6,
    }
}
