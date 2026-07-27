using System;

namespace MyModbus.Common
{
    public interface IMyPointConfig
    {
        string ClassName { get; set; }
        string PropertyName { get; set; }
        string Description { get; set; }
        byte SlaveId { get; set; }
        ushort Address { get; set; }
        decimal Scale { get; set; }
        MyPointDataRegion DataRegion { get; set; }
        MyPointDataType DataType { get; set; }
        int CollectInterval { get; set; }
        DateTime LastCollected { get; set; }
    }
}
