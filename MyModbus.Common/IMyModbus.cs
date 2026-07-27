using System;
using System.Threading.Tasks;

namespace MyModbus.Common
{
    public interface IMyModbus : IDisposable
    {
        bool Connected { get; }
        event EventHandler<EventArgs> AutoConnected;
        event EventHandler<EventArgs> AutoDisconnected;
        bool Connect();
        Task DisconnectAsync();
        Task<byte[]> SendAndReceiveAsync(byte[] sendingBytes);

    }
}
