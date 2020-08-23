using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SESimulator.Data;
using SESimulator.Runtime;

namespace SESimulator.Dev
{
    class Program
    {
        private readonly GameData gameData;
        private readonly Localiser localiser;

        static void Main(string[] args)
        {
            var root = args.FirstOrDefault();
            if (String.IsNullOrEmpty(root)) throw new ArgumentNullException("Must specify game data location as first parameter.");
            var gameDataRoot = Path.GetFullPath(root);
            if (!Directory.Exists(gameDataRoot)) throw new ArgumentNullException(String.Format("Directory does not exist: {0}", gameDataRoot));

            var loader = new GameDataLoader(new GameFileLoader(gameDataRoot));

            var localiser = loader.LoadLocalisations();
            var gameData = loader.LoadGameData();
            new Program(gameData, localiser).Run_BlueprintGeneration();
            //new Program(gameData, localiser).Run_BlueprintSpeeds();
        }

        private Program(GameData gameData, Localiser localiser)
        {
            this.gameData = gameData;
            this.localiser = localiser;
        }

        private void Run_BlueprintGeneration()
        {
            Console.Out.WriteLine(@"
public static Blueprint[] BLUEPRINTS = {
    // Default blueprints for refining ore to ingots:");

            var blueprints = gameData.FindAll<Blueprint>()
                .Where(b => b.Public)
                .Where(b => b.Outputs.Any(i => i.ItemId.TypeId == "Ingot"));

            foreach (var bp in blueprints)
            {
                Console.Out.Write("new Blueprint(\"{0}\", {1:0.###}f", bp.Id.SubTypeId, bp.BaseProductionTimeInSeconds);
                var input = bp.Inputs.Single();
                FormatStack(", new ItemAndQuantity(\"{0}/{1}\", {2:0.###}f)", input);
                if (bp.Outputs.Any())
                {
                    var firstOutput = bp.Outputs.First();
                    FormatStack(",\n    new ItemAndQuantity(\"{0}/{1}\", {2:0.###}f)", firstOutput);
                    foreach (var output in bp.Outputs.Skip(1))
                    {
                        FormatStack(", new ItemAndQuantity(\"{0}/{1}\", {2:0.###}f)", output);
                    }
                }
                Console.Out.WriteLine("),");
            }

            Console.Out.WriteLine(@"};");

            var refineries = gameData.FindAll<ProductionBlock>()
                .Where(i => i.Public)
                .OfType<RefineryBlock>()
                .Select(i => new { Refinery = i, Blueprints = i.Classes.SelectMany(gameData.FindGroupedItems<Blueprint>).Distinct() })
                .Where(i => i.Blueprints.Any());

            Console.Out.WriteLine(@"
public static RefineryType[] REFINERY_TYPES = {");

            foreach (var entry in refineries)
            {
                Console.Out.Write("new RefineryType(\"{0}\")", entry.Refinery.Id.SubTypeId);
                Console.Out.WriteLine(" {");
                Console.Out.Write("    SupportedBlueprints = { ");
                Console.Out.Write(String.Join(", ", entry.Blueprints.Select(b => String.Concat('"', b.Id.SubTypeId, '"'))));
                Console.Out.WriteLine(" },");
                Console.Out.WriteLine("    Efficiency = {0}, Speed = {1}", entry.Refinery.MaterialEfficiency, entry.Refinery.RefineSpeed);
                Console.Out.WriteLine("},");
            }
            Console.Out.WriteLine(@"};");
        }

        private static void FormatStack(string format, ItemStack stack)
        {
            Console.Out.Write(format, stack.ItemId.TypeId, stack.ItemId.SubTypeId, stack.Amount);
        }

        private void Run_CSV()
        {
            var producersAndBlueprints = gameData.FindAll<ProductionBlock>()
                .Where(i => i.Public)
                .Select(i => new { Block = i, Blueprints = i.Classes.SelectMany(gameData.FindGroupedItems<Blueprint>).Distinct() })
                .Where(i => i.Blueprints.Any());

            foreach (var entry in producersAndBlueprints)
            {
                if (entry.Block is RefineryBlock)
                {
                    var refinery = (RefineryBlock)entry.Block;
                    var produced = DescribeIngotProduction(entry.Blueprints).ToArray();
                    var refineryIngotSpeed = refinery.RefineSpeed * refinery.MaterialEfficiency;
                    var refineryOreSpeed = refinery.RefineSpeed;
                    Console.WriteLine("{0}: {1} ingot factor, {2} ore factor", localiser.ToString(entry.Block.DisplayName), refineryIngotSpeed, refineryOreSpeed);
                    Console.WriteLine("Type,Ore per hour (kg),Seconds per kg of ore,Ingots per hour (kg),Seconds per kg of ingots");
                    foreach (var type in produced)
                    {
                        var item = gameData.Find<ItemType>(type.OreType);

                        Console.WriteLine("{0},{1:0},{2:0.##},{3:0},{4:0.##}",
                            localiser.ToString(item.DisplayName),
                            type.MeanOrePerSecond * refineryOreSpeed * 60 *60,
                            1/ (type.MeanOrePerSecond * refineryOreSpeed),
                            type.MeanIngotsPerSecond * refineryIngotSpeed * 60 * 60,
                            1 / (type.MeanIngotsPerSecond * refineryIngotSpeed));
                    }
                }
            }
        }

        private void Run_BlueprintSpeeds()
        {
            var producersAndBlueprints = gameData.FindAll<ProductionBlock>()
                .Where(i => i.Public)
                .Select(i => new { Block = i, Blueprints = i.Classes.SelectMany(gameData.FindGroupedItems<Blueprint>).Distinct() })
                .Where(i => i.Blueprints.Any());

            foreach(var entry in producersAndBlueprints)
            {
                if (entry.Block is AssemblerBlock)
                {
                    Console.WriteLine("{0}: ", localiser.ToString(entry.Block.DisplayName));
                    var consumed = DescribeIngotConsumption((AssemblerBlock)entry.Block, entry.Blueprints).ToArray();
                    var maximums = consumed.GroupBy(i => i.ItemId).Select(g => new Consumed { ItemId = g.Key, Spike = g.Max(i => i.Spike), MeanPerSecond = g.Max(i => i.MeanPerSecond) }).ToArray();
                    Console.WriteLine("  MAXIMUMS");
                    foreach (var type in maximums)
                    {
                        var item = gameData.Find<ItemType>(type.ItemId);
                        Console.WriteLine("   * {0} : {1}, {2}/sec", localiser.ToString(item.DisplayName), type.Spike, type.MeanPerSecond);
                    }
                }
                if (entry.Block is RefineryBlock)
                {
                    var refinery = (RefineryBlock)entry.Block;
                    var produced = DescribeIngotProduction(entry.Blueprints).ToArray();
                    var refineryIngotSpeed = refinery.RefineSpeed * refinery.MaterialEfficiency;
                    var refineryOreSpeed = refinery.RefineSpeed;
                    Console.WriteLine("{0}: {1} ingot factor, {2} ore factor", localiser.ToString(entry.Block.DisplayName), refineryIngotSpeed, refineryOreSpeed);
                    foreach (var type in produced)
                    {
                        var item = gameData.Find<ItemType>(type.IngotType);
                        Console.WriteLine("   * {0} : {1:0.####} ingots/sec ({2:0.####} adjusted), {3:0.####} ore/sec ({4:0.####} adjusted)",
                            localiser.ToString(item.DisplayName),
                            type.MeanIngotsPerSecond,
                            type.MeanIngotsPerSecond * refineryIngotSpeed,
                            type.MeanOrePerSecond,
                            type.MeanOrePerSecond * refineryOreSpeed);
                    }
                }
            }

        }

        private IEnumerable<Consumed> DescribeIngotConsumption(AssemblerBlock producer, IEnumerable<Blueprint> blueprints)
        {
            foreach (var blueprint in blueprints)
            {
                Console.WriteLine("  {0}", localiser.ToString(blueprint.DisplayName));
                
                var processingTimeSeconds = blueprint.BaseProductionTimeInSeconds / producer.AssemblySpeed;
                foreach (var input in blueprint.Inputs)
                {
                    var consumed = new Consumed { ItemId = input.ItemId, Spike = input.Amount, MeanPerSecond = input.Amount / processingTimeSeconds };
                    var item = gameData.Find<ItemType>(consumed.ItemId);
                    Console.WriteLine("   * {0} : {1}, {2}/sec", localiser.ToString(item.DisplayName), consumed.Spike, consumed.MeanPerSecond);
                    yield return consumed;
                }
            }
        }

        private IEnumerable<Produced> DescribeIngotProduction(IEnumerable<Blueprint> blueprints)
        {
            foreach (var blueprint in blueprints)
            {
                var produced = new Produced {
                    IngotType = blueprint.Outputs.Single().ItemId,
                    OreType = blueprint.Inputs.Single().ItemId,
                    MeanIngotsPerSecond = blueprint.Outputs.Single().Amount / blueprint.BaseProductionTimeInSeconds,
                    MeanOrePerSecond = blueprint.Inputs.Single().Amount / blueprint.BaseProductionTimeInSeconds
                };
                yield return produced;
            }
        }
        struct Produced
        {
            public Id OreType { get; set; }
            public Id IngotType { get; set; }
            public decimal MeanIngotsPerSecond { get; set; }
            public decimal MeanOrePerSecond { get; set; }
        }
        struct Consumed {
            public Id ItemId { get; set; }
            public decimal Spike { get; set; }
            public decimal MeanPerSecond { get; set; }
        }
    }
}
