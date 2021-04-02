using System;
using System.Collections.Generic;

namespace Yam
{
    /* Simply a List<> of WorldInfo classes. Exists to allow a single object to be serialized to a file
     * for ease of reading and writing.
     */
     [Serializable]
    public class WorldCollection
    {
        public List<WorldInfo> WorldList { get; } = new();

        public void AddWorld(WorldInfo world)
        {
            WorldList.Add(world);
        }

        public WorldInfo GetWorld(string name)
        {
            return WorldList.Find(world => world.WorldName == name);
        }
    }

}
