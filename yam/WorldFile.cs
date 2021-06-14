using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Yam
{
    public static class WorldFile
    {
        public static WorldCollection Read(string filePath)
        {
            WorldCollection data = null;
         
            using Stream stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            IFormatter formatter = new BinaryFormatter();
            if (stream.Length != 0)
            {
                data = (WorldCollection)formatter.Deserialize(stream);
            }
                   
            return data;
        }

        public static void Write(WorldInfo data, string filePath)
        {
            WorldCollection tempwc = new();
            ///If there's already saved worlds, load them
            if (File.Exists(filePath))
            {
                tempwc = WorldFile.Read(MainWindow.ConfigFilePath);
            }

            IFormatter formatter = new BinaryFormatter();
            try
            {
                tempwc.AddWorld(data); //Add world to list (not overwriting)
                Stream stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, tempwc);
            }
            catch 
            {
                throw;
            }
        }
    }
}
