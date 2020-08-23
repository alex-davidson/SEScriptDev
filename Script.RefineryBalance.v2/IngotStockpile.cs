
using System;

namespace IngameScript
{
    /// <summary>
    /// Records current quantities and target quantities for a material.
    /// Long-lived object.
    /// </summary>
    public class IngotStockpile
    {
        public readonly IngotType Ingot;

        public IngotStockpile(IngotType ingot)
        {
            if (ingot.ProductionNormalisationFactor <= 0) throw new Exception(string.Format("ProductionNormalisationFactor is not positive, for ingot type {0}", ingot.ItemType));
            Ingot = ingot;
            UpdateAssemblerSpeed(1); // Default assembler speed to 'realistic'. Provided for tests; should be updated prior to use anyway.
        }

        public void AssignedWork(double seconds, double productionRate)
        {
            EstimatedProduction += seconds * productionRate;
        }

        public void UpdateAssemblerSpeed(double totalAssemblerSpeed)
        {
            TargetQuantity = Ingot.StockpileTargetOverride ?? Ingot.ConsumedPerSecond * Constants.TARGET_INTERVAL_SECONDS * totalAssemblerSpeed;

            shortfallUpperLimit = TargetQuantity / 2;
            shortfallLowerLimit = TargetQuantity / 100;
        }

        public void UpdateQuantity(double currentQuantity)
        {
            UpdateShortfall(CurrentQuantity - currentQuantity);
            CurrentQuantity = currentQuantity;
            EstimatedProduction = 0;
        }

        private void UpdateShortfall(double shortfall)
        {
            if (shortfall <= 0)
            {
                // Production appears to be keeping up. Reduce shortfall until it falls below a threshold, then forget it.
                if (lastShortfall <= 0) return;
                if (lastShortfall < shortfallLowerLimit)
                {
                    lastShortfall = 0;
                    return;
                }
                lastShortfall /= 2;
            }
            else
            {
                // Produced less than was consumed?
                lastShortfall = Math.Min(Math.Max(shortfall, lastShortfall), shortfallUpperLimit);
            }
        }

        private double shortfallUpperLimit;
        private double shortfallLowerLimit;
        // Adjustment factor for ingot consumption rate.
        private double lastShortfall;

        public double CurrentQuantity { get; private set; }
        /// <summary>
        /// Units (kg) of ingots which we believe newly-allocated refinery work will produce
        /// before the next iteration.
        /// </summary>
        public double EstimatedProduction { get; private set; }
        /// <summary>
        /// Maximum number of units (kg) of ingots which may be consumed by assemblers, assuming
        /// most expensive blueprint.
        /// </summary>
        public double TargetQuantity { get; private set; }

        /// <summary>
        /// Based on newly-allocated work, the estimated number of units (kg) of ingots which
        /// will be produced before the next iteration, minus consumption.
        /// </summary>
        public double EstimatedQuantity { get { return Math.Max(0, CurrentQuantity + EstimatedProduction - lastShortfall); } }
        /// <summary>
        /// Estimated fraction of target quantity (kg) of ingots which should be produced before
        /// the next iteration.
        /// </summary>
        /// <remarks>
        /// Used as a priority indicator. The lower this is, the sooner the given ingot type might
        /// run out. If this is less than 1, it means that if all assemblers are manufacturing the
        /// most demanding blueprint then the ingots may run out.
        /// </remarks>
        public double QuotaFraction { get { return EstimatedQuantity / TargetQuantity; } }

        public bool IsSatisfied { get { return Ingot.StockpileLimit.HasValue && Ingot.StockpileLimit.Value <= EstimatedQuantity; } }
    }

}
