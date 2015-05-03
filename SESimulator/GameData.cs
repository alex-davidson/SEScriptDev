using System;
using System.Collections.Generic;
using System.Linq;
using SESimulator.Data;

namespace SESimulator
{
    public class GameData : Dictionary<Id, Thing>
    {
        private readonly IDictionary<Id, Thing> everything;
        private readonly IList<IGrouping<Type, Thing>> everythingByType;
        private readonly ILookup<Id, GroupEntry> groupsByGroupId;
        private readonly ILookup<Id, GroupEntry> groupsByItemId;

        public GameData(IEnumerable<Thing> things, IList<GroupEntry> groups)
        {
            everything = things.ToDictionary(t => t.Id);
            everythingByType = everything.Values.GroupBy(t => t.GetType()).ToList();
            groupsByGroupId = groups.ToLookup(g => g.Group);
            groupsByItemId = groups.ToLookup(g => g.Entry);
        }

        public void Add(IEnumerable<Thing> things)
        {
            foreach (var thing in things) everything.Add(thing.Id, thing);
        }

        public IEnumerable<T> FindGroups<T>(Id itemId) where T : Thing
        {
            return groupsByItemId[itemId]
                .Select(i => everything[i.Group])
                .OfType<T>();
        }

        public IEnumerable<T> FindGroupedItems<T>(Id groupId) where T : Thing
        {
            return groupsByGroupId[groupId]
                .SelectMany(i => {
                    Thing t;
                    return everything.TryGetValue(i.Entry, out t) ? new[]{ t } : Enumerable.Empty<Thing>();
                })
                .OfType<T>();
        }

        public T Find<T>(Id id) where T : Thing
        {
            return (T)everything[id];
        }

        public IEnumerable<T> FindAll<T>() where T : Thing
        {
            return everythingByType
                .Where(g => typeof(T).IsAssignableFrom(g.Key))
                .SelectMany(g => g.Cast<T>());
        }
    }
}