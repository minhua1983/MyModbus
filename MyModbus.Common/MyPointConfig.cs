using System;

namespace MyModbus.Common
{
    public class MyPointConfig<T> : IMyPointConfig
    {
        public string ClassName { get; set; }
        public string PropertyName { get; set; }
        public string Description { get; set; }
        public byte SlaveId { get; set; } = 1;
        public ushort Address { get; set; } = 0;
        public decimal Scale { get; set; } = 1;
        public MyPointDataRegion DataRegion { get; set; }
        public MyPointDataType DataType { get; set; }
        public int CollectInterval { get; set; } = 1000;
        public DateTime LastCollected { get; set; } = DateTime.Now;
        public Action<T> Callback { get; set; }
        public T HighThreshold { get; set; }
        public T LowThreshold { get; set; }
        public T HighDeadBand { get; set; }
        public T LowDeadBand { get; set; }
        public bool IsNoticed { get; set; } = false;
        public DateTime LastNoticed { get; set; } = DateTime.Now;
        public int OnDelay { get; set; } = 3000;
        public bool IsAlarmed { get; set; } = false;
        public DateTime LastAlarmed { get; set; } = DateTime.Now;
        public int OffDelay { get; set; } = 3000;
    }

    public enum MyPointDataRegion
    {
        DiscreteInput = 1,
        Coil = 0,
        InputRegister = 3,
        HoldingRegister = 4
    }

    public enum MyPointDataType
    {
        Bool,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Float32
    }
}
