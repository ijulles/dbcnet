using System;
using System.Collections.Generic;

namespace dbcnet
{
    public class Message
    {
        public string Name { get; set; }

        public uint Identifier { get; set; }

        public IList<Signal> Signals { get; set; }

        public string Comment { get; set; }

        public bool CANExtID { get; set; }

        public uint Length { get; set; }

        public CANTimingType CANTimingType { get; set; }

        public double CANTxTime { get; set; }

        public CANIoMode CANIoMode { get; set; }

        public override string ToString() => Name;
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
