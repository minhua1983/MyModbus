using System;

namespace MyModbus.Common
{
    public class ReceivedFrameEventArgs: EventArgs
    {
        public byte[] ReceivedFrameBytes { get; set; }
    }
}
