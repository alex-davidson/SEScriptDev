using System;

namespace SESimulator.Data
{
    public abstract class ProductionBlock : CubeBlock
    {
        public Id[] Classes { get; set; }
    }
}