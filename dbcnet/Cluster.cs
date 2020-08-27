using System;
using System.Collections.Generic;
using System.Text;

namespace dbcnet
{
    public class Cluster
    {
        public string Name { get; set; }

        public ulong BaudRate { get; set; }

        public ulong FDBaudRate { get; set; }

        public IList<Signal> Signals { get; set; }

        public IList<Message> Messages { get; set; }

        public IList<Node> Nodes { get; set; }

        public string Comment { get; set; }

        public override string ToString() => Name;
    }
}
