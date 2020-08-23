
using System.Collections.Generic;

namespace IngameScript
{
    public struct Blueprints
    {
        public readonly IEnumerable<Blueprint> All;
        private readonly IDictionary<ItemType, List<Blueprint>> blueprintsByOutputType;

        public Blueprints(IList<Blueprint> blueprints) : this()
        {
            All = blueprints;
            blueprintsByOutputType = new Dictionary<ItemType, List<Blueprint>>();
            foreach (var blueprint in blueprints)
            {
                foreach (var output in blueprint.Outputs)
                {
                    if (output.Quantity <= 0) continue;

                    List<Blueprint> existing;
                    if (!blueprintsByOutputType.TryGetValue(output.ItemType, out existing))
                    {
                        existing = new List<Blueprint>();
                        blueprintsByOutputType.Add(output.ItemType, existing);
                    }
                    existing.Add(blueprint);
                }
            }
        }

        public double GetOutputPerSecondForDefaultBlueprint(ItemType singleOutput)
        {
            List<Blueprint> blueprints;
            if (!blueprintsByOutputType.TryGetValue(singleOutput, out blueprints)) return 0;

            double perSecond = 0;
            var ignoreMultiOutput = false;
            foreach (var blueprint in blueprints)
            {
                var hasMultiOutput = blueprint.Outputs.Length > 1;
                // Once we have a single-output blueprint, ignore all subsequent multi-output blueprints.
                if (ignoreMultiOutput && hasMultiOutput) continue;

                var quantity = QuantityProduced(blueprint, singleOutput);
                if (quantity <= 0) continue; // Blueprint doesn't produce this item type.
                var thisPerSecond = quantity / blueprint.Duration;

                var isBetterMatch = (!ignoreMultiOutput && blueprint.Outputs.Length == 1)
                    || (thisPerSecond > perSecond);

                if (!isBetterMatch) continue;

                perSecond = thisPerSecond;
                ignoreMultiOutput = blueprint.Outputs.Length == 1;
            }
            return perSecond;
        }

        private static double QuantityProduced(Blueprint blueprint, ItemType itemType)
        {
            for (var i = 0; i < blueprint.Outputs.Length; i++)
            {
                if (Equals(blueprint.Outputs[i].ItemType, itemType))
                {
                    return blueprint.Outputs[i].Quantity;
                }
            }
            return 0;
        }

        public IngotType CalculateNormalisationFactor(IngotType type)
        {
            if (type.ProductionNormalisationFactor > 0) return type;
            type.ProductionNormalisationFactor = GetOutputPerSecondForDefaultBlueprint(type.ItemType);
            return type;
        }

        public IList<Blueprint> GetBlueprintsProducing(ItemType singleOutput)
        {
            List<Blueprint> blueprints;
            if (!blueprintsByOutputType.TryGetValue(singleOutput, out blueprints)) return new List<Blueprint>();
            return blueprints;
        }
    }
}
