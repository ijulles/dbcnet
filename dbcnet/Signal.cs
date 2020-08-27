using System;

namespace dbcnet
{
    public class Signal
    {
        public string Name { get; set; }

        public string UniqueName { get; set; }

        public ByteOrder ByteOrder { get; set; }

        public DataType DataType { get; set; }

        public double DefaultValue { get; set; }

        public double MaximumValue { get; set; }

        public double MinimumValue { get; set; }

        public uint StartBit { get; set; }

        public uint NumberOfBits { get; set; }

        public double ScalingFactor { get; set; }

        public double ScalingOffset { get; set; }

        public string Unit { get; set; }

        public string Comment { get; set; }

        public override string ToString() => Name;
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
