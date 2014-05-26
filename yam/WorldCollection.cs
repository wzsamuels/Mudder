using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam
{
    public class WorldCollection
    {
        private List<WorldInfo> _worlds;

        public WorldCollection()
        {
            _worlds = new List<WorldInfo>();
        }       

        public List<WorldInfo> Worlds
        {
            get
            {
                return _worlds;
            }
            set
            {
                _worlds = value;
            }
        }

        public void AddWorld(WorldInfo world)
        {
            _worlds.Add(world);
        }
    }

}
