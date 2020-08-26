using System;
using System.Collections.Generic;
using System.Text;

namespace dbcnet
{
    public class Node
    {
        public string Name { get; }

        public IList<Message> MessagesReceived { get; }

        public IList<Message> MessagesTansmitted { get; }

        public string Comment { get; }
    }
}
