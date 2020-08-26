using System;
using System.Collections.Generic;

namespace dbcnet
{
    public class Message
    {
        public string Name { get; }

        public uint Identifier { get; }

        public IList<Signal> Signals { get; }

        public string Comment { get; }

        public bool CANExtID { get; }

        public CANTimingType CANTimingType { get; }

        public double CANTxTime { get; }

        public CANIoMode CANIoMode { get; }
    }

    public enum CANIoMode : uint
    {
        CAN,
        CANFD,
        CANFDBRS
    }

    public enum  CANTimingType : uint
    {
        CyclicData,
        CyclicEvent,
        CyclicRemote,
        EventData,
        EventRemote
    };
}
