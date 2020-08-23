
using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public class IngotWorklist
    {
        public IngotWorklist(IEnumerable<IngotStockpile> ingotStockpiles)
        {
            stockpilesByIngotType = ingotStockpiles.ToDictionary(s => s.Ingot.ItemType);
            candidateStockpiles = new List<IngotStockpile>(stockpilesByIngotType.Count);
        }

        public void Initialise()
        {
            candidateStockpiles.AddRange(stockpilesByIngotType.Values);
            UpdatePreferred();
        }

        private readonly List<IngotStockpile> candidateStockpiles;
        private readonly Dictionary<ItemType, IngotStockpile> stockpilesByIngotType;

        private double sortThreshold;
        private int? preferredIndex;

        private void UpdatePreferred()
        {
            preferredIndex = null;
            var lowest = double.MaxValue;
            for (var i = 0; i < candidateStockpiles.Count; i++)
            {
                var qf = candidateStockpiles[i].QuotaFraction;
                if (qf < lowest)
                {
                    sortThreshold = lowest;
                    preferredIndex = i;
                    lowest = qf;
                }
                else if (qf < sortThreshold)
                {
                    sortThreshold = qf;
                }
            }
        }

        public bool TryGetPreferred(out IngotStockpile preferred)
        {
            if (!preferredIndex.HasValue) UpdatePreferred();

            if (preferredIndex == null)
            {
                preferred = null;
                return false;
            }

            preferred = candidateStockpiles[preferredIndex.Value];
            return true;
        }

        // ASSUMPTION: TryGetPreferred is always called between calls to this.
        public void Skip()
        {
            candidateStockpiles.RemoveAtFast(preferredIndex.Value);
            preferredIndex = null;
        }

        public void UpdateStockpileEstimates(Refinery refinery, Blueprint blueprint, double workTowardsDeadline)
        {
            for (var i = 0; i < blueprint.Outputs.Length; i++)
            {
                var output = blueprint.Outputs[i];

                IngotStockpile stockpile;
                if (!stockpilesByIngotType.TryGetValue(output.ItemType, out stockpile)) continue;

                stockpile.AssignedWork(workTowardsDeadline, refinery.GetActualIngotProductionRate(output, blueprint.Duration));
            }
            if (preferredIndex.HasValue)
            {
                var s = candidateStockpiles[preferredIndex.Value];
                if (s.IsSatisfied)
                {
                    candidateStockpiles.RemoveAtFast(preferredIndex.Value);
                    preferredIndex = null;
                }
                else if (s.QuotaFraction > sortThreshold)
                {
                    preferredIndex = null;
                }
            }
        }

        public double ScoreBlueprint(Blueprint blueprint)
        {
            double score = 0;
            for (var i = 0; i < blueprint.Outputs.Length; i++)
            {
                var output = blueprint.Outputs[i];

                IngotStockpile stockpile;
                if (!stockpilesByIngotType.TryGetValue(output.ItemType, out stockpile)) continue;
                var quantityPerSecond = output.Quantity / blueprint.Duration;
                score += quantityPerSecond / stockpile.Ingot.ProductionNormalisationFactor / stockpile.QuotaFraction;
            }
            return score;
        }
    }

}
