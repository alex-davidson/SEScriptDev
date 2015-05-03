using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SESimulator.Data;

namespace SESimulator.Dev
{
    class Program
    {
        static void Main(string[] args)
        {
            var root = args.FirstOrDefault();
            if (String.IsNullOrEmpty(root)) throw new ArgumentNullException("Must specify game data location as first parameter.");
            var gameDataRoot = Path.GetFullPath(root);
            if (!Directory.Exists(gameDataRoot)) throw new ArgumentNullException(String.Format("Directory does not exist: {0}", gameDataRoot));

            var loader = new GameDataLoader(new GameFileLoader(gameDataRoot));
            var localiser = loader.LoadLocalisations();
            var gameData = loader.LoadGameData();
            
            var producersAndBlueprints = gameData.FindAll<ProductionBlock>()
                .Where(i => i.Public)
                .Select(i => new { Block = i, Blueprints = i.Classes.SelectMany(gameData.FindGroupedItems<Blueprint>).Distinct() })
                .Where(i => i.Blueprints.Any());

            foreach(var entry in producersAndBlueprints)
            {
                Console.WriteLine("{0}: ", localiser.ToString(entry.Block.DisplayName));
                foreach (var blueprint in entry.Blueprints)
                {
                    Console.WriteLine("  {0}", localiser.ToString(blueprint.DisplayName));
                }
            }
        }
    }
}
