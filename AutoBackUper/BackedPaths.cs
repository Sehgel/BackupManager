using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager
{
    [Serializable]
    public struct BackedPath : ISerializable
    {
        public string Name { get; private set; }
        public string[] Paths { get; private set; }
        public string[] Targets { get; private set; }

        public BackedPath(string name, string[] paths, string[] targets)
        {
            this.Name = name;
            this.Paths = paths;
            this.Targets = targets;
        }
        public BackedPath(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            Name = (string)info.GetValue("Name", typeof(string));
            Paths = (string[])info.GetValue("Paths", typeof(string[]));
            Targets = (string[])info.GetValue("Targets", typeof(string[]));
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name,typeof(string));
            info.AddValue("Paths", Paths, typeof(string[]));
            info.AddValue("Targets", Targets, typeof(string[]));
        }

        public override string ToString()
        {
            return Name + " " + Paths.ToString() + " " + Targets;
        }
    }
}
