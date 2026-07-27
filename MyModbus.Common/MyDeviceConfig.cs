using System.Collections.Generic;

namespace MyModbus.Common
{
    public class MyDeviceConfig
    {
        public byte DeviceId { get; set; } = 0x01;
        //public string Address { get; set; } = string.Empty;
        //public int port { get; set; } = 0;
        public WordOrder WordOrder { get; set; } = WordOrder.HighWordFirst;
        public ByteOrder ByteOrder { get; set; } = ByteOrder.HighByteFirst;
        public List<IMyPointConfig> PointConfigList { get; set; }
    }

    public enum WordOrder
    {
        HighWordFirst,
        LowWordFirst
    }

    public enum ByteOrder
    {
        HighByteFirst, 
        LowByteFirst
    }
}
