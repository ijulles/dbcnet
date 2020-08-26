using System;

namespace dbcnet
{
    public class Signal
    {
        public string Name { get; }

        public string UniqueName { get; }

        public ByteOrder ByteOrder { get; }

        public DataType DataType { get; }

        public double DefaultValue { get; }

        public double MaximumValue { get; }

        public double MinimumValue { get; }

        public uint StartBit { get; }

        public uint NumberOfBits { get; }

        public double ScalingFactor { get; }

        public double ScalingOffset { get; }

        public string Unit { get; }

        public string Comment { get; }

    }

    public enum ByteOrder : uint
    {
        LittleEndian,
        BigEndian
    }

    public enum DataType : uint
    {
        Signed,
        Unsigned,
        IEEEFloat
    }
}
