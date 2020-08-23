using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public struct IngotStockpiles : IEnumerable<IngotStockpile>
    {
        private readonly ICollection<IngotStockpile> ingotStockpiles;

        public IngotStockpiles(IEnumerable<IngotStockpile> initialIngotStockpiles)
        {
            ingotStockpiles = initialIngotStockpiles.ToArray();
        }

        public void UpdateAssemblerSpeed(double totalAssemblerSpeed)
        {
            foreach (var stockpile in ingotStockpiles)
            {
                stockpile.UpdateAssemblerSpeed(totalAssemblerSpeed);
            }
        }

        public void UpdateQuantities(IDictionary<ItemType, double> currentQuantities)
        {
            foreach (var stockpile in ingotStockpiles)
            {
                double quantity;
                currentQuantities.TryGetValue(stockpile.Ingot.ItemType, out quantity);
                stockpile.UpdateQuantity(quantity);
            }
        }

        public IEnumerator<IngotStockpile> GetEnumerator()
        {
            return ingotStockpiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
