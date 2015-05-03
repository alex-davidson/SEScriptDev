using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Xml;
using System.Xml.Linq;
using SESimulator.Data;

namespace SESimulator
{
    /// <summary>
    /// Reads and parses game data.
    /// </summary>
    public class GameDataLoader
    {
        private readonly GameFileLoader loader;

        public GameDataLoader(GameFileLoader loader)
        {
            this.loader = loader;
        }

        public Localiser LoadLocalisations(CultureInfo culture = null)
        {
            using (var stream = loader.OpenLocalisationFile(culture ?? CultureInfo.CurrentUICulture))
            {
                using (var set = new ResXResourceReader(stream))
                {
                    return new Localiser(set.Cast<DictionaryEntry>()
                        .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString()));
                }
            }
        }

        public GameData LoadGameData()
        {
            var things = Enumerable.Empty<Thing>()
                .Concat(LoadBlocks())
                .Concat(LoadItems())
                .Concat(LoadBlueprintClasses())
                .Concat(LoadBlueprints());

            var mappings = Enumerable.Empty<GroupEntry>()
                .Concat(LoadBlueprintClassEntries())
                .ToList();

            return new GameData(things, mappings);
        }

        public IEnumerable<CubeBlock> LoadBlocks()
        {
            var typeAttribute = XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance");
            var definitions = Load(GameFile.CubeBlocks).Element("CubeBlocks").Elements("Definition");
            

            var refineries = definitions
                .Where(d => (string)d.Attribute(typeAttribute) == "MyObjectBuilder_RefineryDefinition")
                .Select(x =>
                {
                    var refinery = ReadThing<RefineryBlock>(x);
                    refinery.Classes = x.Element("BlueprintClasses").Elements("Class").Select(c => new Id("BlueprintClassDefinition", c.Value)).ToArray();
                    refinery.MaterialEfficiency = (decimal)x.Element("MaterialEfficiency");
                    refinery.RefineSpeed = (decimal)x.Element("RefineSpeed");
                    return refinery;
                });
            var assemblers = definitions
                .Where(d => (string)d.Attribute(typeAttribute) == "MyObjectBuilder_AssemblerDefinition")
                .Select(x =>
                {
                    var assembler = ReadThing<AssemblerBlock>(x);
                    assembler.Classes = x.Element("BlueprintClasses").Elements("Class").Select(c => new Id("BlueprintClassDefinition", c.Value)).ToArray();
                    assembler.AssemblySpeed = (decimal?)x.Element("AssemblySpeed") ?? 1;
                    return assembler;
                });

            return refineries.Cast<CubeBlock>().Concat(assemblers);
        }

        public IEnumerable<BlueprintClass> LoadBlueprintClasses()
        {
            return Load(GameFile.BlueprintClasses)
                .Element("BlueprintClasses")
                .Elements("Class")
                .Select(ReadThing<BlueprintClass>);
        }

        public IEnumerable<Blueprint> LoadBlueprints()
        {
            return Load(GameFile.Blueprints)
                .Element("Blueprints")
                .Elements("Blueprint")
                .Select(x =>
                {
                    var blueprint = ReadThing<Blueprint>(x);
                    blueprint.Inputs = x.Element("Prerequisites").Elements("Item").Select(ReadItemStack).ToArray();
                    blueprint.Output = ReadItemStack(x.Element("Result"));
                    blueprint.BaseProductionTimeInSeconds = (decimal)x.Element("BaseProductionTimeInSeconds");
                    return blueprint;
                });
        }

        public IEnumerable<GroupEntry> LoadBlueprintClassEntries()
        {
            return Load(GameFile.BlueprintClasses)
                .Element("BlueprintClassEntries")
                .Elements("Entry")
                .Select(x => new GroupEntry {
                    Group = IdFromSubTypeAttribute("BlueprintClassDefinition", x, "Class"),
                    Entry = IdFromSubTypeAttribute("BlueprintDefinition", x, "BlueprintSubtypeId")
                });
        }

        public IEnumerable<ItemType> LoadItems()
        {
            return Load(GameFile.PhysicalItems)
                    .Element("PhysicalItems")
                    .Elements("PhysicalItem")
                    .Select(ReadItemType<PhysicalItem>)
                .Concat(Load(GameFile.Components)
                    .Element("Components")
                    .Elements("Component")
                    .Select(ReadItemType<Component>));
        }


        private XElement Load(GameFile file)
        {
            using (var stream = loader.OpenFile(file))
            {
                return XDocument.Load(stream).Element("Definitions");
            }
        }

        private static Id ReadId(XElement el)
        {
            if (el == null) throw new ArgumentNullException("el");
            return new Id((string)el.Element("TypeId"),(string)el.Element("SubtypeId"));
        }

        private static ItemStack ReadItemStack(XElement el)
        {
            if (el == null) throw new ArgumentNullException("el");
            return new ItemStack {
                ItemId = new Id((string)el.Attribute("TypeId"),(string)el.Attribute("SubtypeId")),
                Amount = (decimal)el.Attribute("Amount")
            };
        }

        private static Id IdFromSubTypeAttribute(string typeId, XElement x, string attributeName)
        {
            return new Id(typeId, (string)x.Attribute(attributeName));
        }

        private static T ReadThing<T>(XElement x) where T : Thing, new()
        {
            return new T
            {
                Id = ReadId(x.Element("Id")),
                DisplayName = new LocalisableString((string)x.Element("DisplayName")),
                Public = (bool?)x.Element("Public") ?? true
            };
        }

        private static ItemType ReadItemType<T>(XElement x) where T : ItemType, new()
        {
            var itemType = ReadThing<T>(x);
            itemType.Mass = (decimal)x.Element("Mass");
            itemType.Volume = (decimal)x.Element("Volume");
            return itemType;
        }
    }
}