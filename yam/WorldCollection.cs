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
        public WorldCollection()
        {
            Worlds = new List<WorldInfo>();
        }

        public List<WorldInfo> Worlds { get; set; }

        public void AddWorld(WorldInfo world)
        {
            Worlds.Add(world);
        }
    }

}
