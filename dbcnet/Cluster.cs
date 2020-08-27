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

        public IList<Signal> Signals { get; set; } = new List<Signal>();

        public IList<Message> Messages { get; set; } = new List<Message>();

        public IList<Node> Nodes { get; set; } = new List<Node>();

        public string Comment { get; set; }

        public override string ToString() => Name;
    }
}
