using System;
using System.Collections.Generic;
using System.Text;

namespace dbcnet
{
    public class Node
    {
        public string Name { get; set; }

        public List<Message> MessagesReceived { get; set; } = new List<Message>();

        public List<Message> MessagesTransmitted { get; set; } = new List<Message>();

        public string Comment { get; set; }

        public override string ToString() => Name;
    }
}
