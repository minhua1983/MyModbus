using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MyModbus.Common
{
    /// <summary>
    /// 流水线模式 仅仅高端PLC支持,即PLC支持同时接收多个请求.普通PLC不支持,只能一收一发模式,当在处理接收的请求时,如果新来一条请求将被丢弃.
    /// </summary>
    public class MyModbusTCP2 : IMyModbus
    {
        // 同步锁
        protected readonly object _locker = new object();
        // 异步锁,用于await的代码块
        protected readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        protected bool _needToReconnect = false;
        protected ushort _transactionId = 0;
        protected Socket _socket;
        protected CancellationTokenSource _cancellationTokenSource;
        protected ConcurrentDictionary<ushort, (TaskCompletionSource<byte[]>, CancellationTokenSource)> _tcsDictionary = new ConcurrentDictionary<ushort, (TaskCompletionSource<byte[]>, CancellationTokenSource)>();

        protected List<byte> _receiviedBytes = new List<byte>();
        protected Task _receiveTask;
        protected Task _heartbeatTask;

        protected int _heartBeatFailedCount = 0;
        protected int _heartBeatFailedThreshold = 3;
        protected bool disposedValue;
        protected MyLogger _myLogger;

        public event EventHandler<EventArgs> AutoConnected;
        public event EventHandler<EventArgs> AutoDisconnected;

        public string Address { get; set; } = string.Empty;
        public int Port { get; set; } = 0;

        public bool Connected { get; set; } = false;
        public int SendTimeout { get; set; } = 2000;
        public int ReceiveTimeout { get; set; } = 2000;

        public int DefaultTimeout { get; set; } = 2000;
        public int SendReceiveTimeout { get; set; } = 2000;
        //public int SleepingPeriod { get; set; } = 50;
        public int MaxWaitingTimes { get; set; } = 10;


        public MyModbusTCP2(MyLogger myLogger, string address, int port)
        {
            _myLogger = myLogger;
            Address = address;
            Port = port;
        }

        protected virtual void OnAutoConnected(EventArgs eventArgs)
        {
            AutoConnected?.Invoke(this, eventArgs);
        }

        protected virtual void OnAutoDisconnected(EventArgs eventArgs)
        {
            AutoDisconnected?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// This function is to use socket (TCP client side) to connect TCP server side (PLC or other terminal)
        /// </summary>
        public bool Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SendTimeout = SendTimeout;
            _socket.ReceiveTimeout = ReceiveTimeout;

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                if (IPAddress.TryParse(Address, out IPAddress iPAddress))
                {
                    // ip address
                    _socket.Connect(iPAddress, Port);
                }
                else
                {
                    // host address
                    _socket.Connect(Address, Port);
                }

                _myLogger.AddLog(LogType.Info, "打开socket成功");

                RunTaskAfterConnect();
            }
            catch (Exception e)
            {
                _socket.Close();
                _myLogger.AddLog(LogType.Alert, "打开socket失败");
                //throw new Exception("connect failed");
                return false;
            }
            Connected = true;
            return true;
        }

        protected virtual void RunTaskAfterConnect()
        {
            if (_heartbeatTask == null)
            {
                _heartbeatTask = Task.Run(CheckHeartbeatAsnyc, _cancellationTokenSource.Token);
            }

            if (_receiveTask == null)
            {
                _receiveTask = Task.Run(ReceiveAsync, _cancellationTokenSource.Token);
            }

            Task.WhenAll(_receiveTask, _heartbeatTask).ContinueWith(async t =>
            {
                await DisconnectAsync();
            });
        }

        public async Task DisconnectAsync()
        {
            if (Connected)
            {
                await _semaphoreSlim.WaitAsync(_cancellationTokenSource.Token);

                try
                {
                    if (Connected)
                    {
                        // 取消缓存的tcs
                        _tcsDictionary.Keys.ToList().ForEach(tranactionId =>
                        {
                            if (_tcsDictionary.TryRemove(tranactionId, out (TaskCompletionSource<byte[]>, CancellationTokenSource) value))
                            {
                                value.Item1.TrySetCanceled();
                            }
                        });

                        // 取消_cancellationTokenSource
                        if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource.Cancel();
                        }

                        // 关闭_socket双向通信channel
                        //_socket?.Shutdown(SocketShutdown.Both);

                        // 关闭_socket
                        _socket?.Close();

                        // 释放_socket
                        _socket?.Dispose();

                        _socket = null;

                        await WaitForTaskDone();

                        // 释放_cancellationTokenSource
                        _cancellationTokenSource?.Dispose();

                        _heartBeatFailedCount = 0;

                        _myLogger.AddLog(LogType.Info, "关闭socket成功");

                        Connected = false;

                        if (_needToReconnect)
                        {
                            _myLogger.AddLog(LogType.Info, $"触发重连");
                            await this.TryReconnect();
                        }
                    }

                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        async Task TryReconnect()
        {

            while (_needToReconnect)
            {
                // 尝试重连
                if (Connect())
                {
                    _myLogger.AddLog(LogType.Info, $"重连成功");
                    lock (_locker)
                    {
                        if (_needToReconnect)
                        {
                            _needToReconnect = false;
                            OnAutoConnected(new EventArgs());
                        }
                    }
                }
                else
                {
                    _myLogger.AddLog(LogType.Alert, $"重连失败,尝试关闭socket");
                    await DisconnectAsync();
                }

                await Task.Delay(DefaultTimeout);
            }
        }

        protected async Task CheckHeartbeatAsnyc()
        {
            _myLogger.AddLog(LogType.Warning, $"_heartbeatTask任务开始");

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var sendingBytes = MyModbusProtocol.BuildReadOutputRegisters(1, 0, 1);
                try
                {
                    var receivingBytes = await SendAndReceiveAsync(sendingBytes);
                    _heartBeatFailedCount = 0;
                }
                catch (TimeoutException te)
                {
                    // timeout异常
                    _heartBeatFailedCount++;
                    _myLogger.AddLog(LogType.Alert, $"心跳检测捕获TimeoutException异常,{te.Message}");
                }
                catch (OperationCanceledException oce)
                {
                    // cancel异常
                    _heartBeatFailedCount++;
                    _myLogger.AddLog(LogType.Alert, $"心跳检测捕获OperationCanceledException异常,{oce.Message}");
                }
                catch (SocketException se)
                {
                    // socket异常
                    _myLogger.AddLog(LogType.Alert, $"心跳检测捕获SocketException异常{se.SocketErrorCode.ToString()},{se.Message}");
                    switch (se.SocketErrorCode)
                    {
                        case SocketError.ConnectionReset:
                        case SocketError.ConnectionAborted:
                        case SocketError.Shutdown:
                        case SocketError.NotConnected:
                            _heartBeatFailedCount = 0;
                            _cancellationTokenSource?.Cancel();
                            lock (_locker)
                            {
                                _needToReconnect = true;
                                OnAutoDisconnected(new EventArgs());
                            }
                            break;
                        default:
                            _heartBeatFailedCount++;
                            break;
                    }
                }
                catch (Exception e)
                {
                    _heartBeatFailedCount++;
                    _myLogger.AddLog(LogType.Alert, $"心跳检测捕获Exception异常,{e.Message}");
                }

                if (_heartBeatFailedCount >= _heartBeatFailedThreshold)
                {
                    _heartBeatFailedCount = 0;
                    _cancellationTokenSource?.Cancel();
                    lock (_locker)
                    {
                        _needToReconnect = true;
                        OnAutoDisconnected(new EventArgs());
                    }
                }

                await Task.Delay(DefaultTimeout);
            }

            _myLogger.AddLog(LogType.Warning, $"_heartbeatTask任务结束");
        }

        public void Send(byte[] sendingBytes)
        {
            throw new NotImplementedException();
        }

        #region send and receive by asynchronized blocked mode (this mode can return whole response frame bytes)
        public virtual async Task<byte[]> SendAndReceiveAsync(byte[] sendingBytes)
        {
            if (!_socket.Connected)
            {
                throw new Exception("socket is not connected!");
            }

            ushort tempTransactionId = 0;

            lock (_locker)
            {
                if (_transactionId >= 10)
                {
                    _transactionId = 1;
                }
                else
                {
                    _transactionId++;
                }

                tempTransactionId = _transactionId;
            }

            sendingBytes[0] = (byte)(tempTransactionId >> 8);
            sendingBytes[1] = (byte)(tempTransactionId & 0xFF);

            _myLogger.AddLog(LogType.Info, $"发送数据：{MyDataConverter.GetStringFromBytes(sendingBytes)}");

            var tcs = new TaskCompletionSource<byte[]>();

            // timeout (send + receive)
            var cts = new CancellationTokenSource(SendReceiveTimeout);
            var cancelTokenRegistration = cts.Token.Register(() =>
            {
                if (_tcsDictionary.TryRemove(tempTransactionId, out var item))
                {
                    item.Item1.TrySetException(new TimeoutException("Send and receive timeout! please check whether socket is connected."));
                    item.Item2.Dispose();
                }
            });

            _tcsDictionary.TryAdd(tempTransactionId, (tcs, cts));
            byte[] receivingBytes = default;

            try
            {
                lock (_locker)
                {
                    _socket.Send(sendingBytes);
                }
                receivingBytes = await tcs.Task;
                //await Task.WhenAny(tcs.Task, Task.Delay(2000));
            }
            catch (TimeoutException te)
            {
                // timeout异常
                //throw new TimeoutException(tex.Message);
                _myLogger.AddLog(LogType.Alert, $"发送数据捕获TimeoutException异常,{te.Message}");
                throw te;
            }
            catch (OperationCanceledException oce)
            {
                // cancel异常
                _myLogger.AddLog(LogType.Alert, $"发送数据捕获OperationCanceledException异常,{oce.Message}");
                throw oce;
            }
            catch (SocketException se)
            {
                // socket异常
                _myLogger.AddLog(LogType.Alert, $"发送数据捕获SocketException异常{se.SocketErrorCode.ToString()},{se.Message}");
                /*
                switch (se.SocketErrorCode)
                {
                    case SocketError.ConnectionReset:
                    case SocketError.ConnectionAborted:
                    case SocketError.Shutdown:
                    case SocketError.NotConnected:
                        break;
                    default:
                        break;
                }
                //*/

                throw se;
            }
            catch (Exception e)
            {
                _myLogger.AddLog(LogType.Alert, $"发送数据捕获Exception异常,{e.Message}");
                throw e;
            }
            finally
            {
                if (_tcsDictionary.TryRemove(tempTransactionId, out var item))
                {
                    item.Item2.Cancel();
                    item.Item2.Dispose();
                    cancelTokenRegistration.Dispose();
                }
            }

            _myLogger.AddLog(LogType.Info, $"接收数据：{MyDataConverter.GetStringFromBytes(receivingBytes)}");

            return receivingBytes;
        }
        #endregion

        protected async Task ReceiveAsync()
        {
            _myLogger.AddLog(LogType.Warning, $"_receiveTask任务开始");

            byte[] receivingBytes = new byte[1024];

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                //await Task.Delay(100);
                try
                {
                    int receivingBytesLength = await _socket.ReceiveAsync(new ArraySegment<byte>(receivingBytes), SocketFlags.None);
                    lock (_locker)
                    {
                        _receiviedBytes.AddRange(receivingBytes.Take(receivingBytesLength));
                        ParseReceivedFrame();
                    }
                }
                catch (TimeoutException te)
                {
                    // timeout异常
                    _myLogger.AddLog(LogType.Alert, $"接收数据捕获TimeoutException异常,{te.Message}");
                }
                catch (OperationCanceledException oce)
                {
                    // cancel异常
                    _myLogger.AddLog(LogType.Alert, $"接收数据捕获OperationCanceledException异常,{oce.Message}");
                }
                catch (SocketException se)
                {
                    // socket异常
                    _myLogger.AddLog(LogType.Alert, $"接收数据捕获SocketException异常{se.SocketErrorCode.ToString()},{se.Message}");
                    //*
                    switch (se.SocketErrorCode)
                    {
                        case SocketError.ConnectionReset:
                        case SocketError.ConnectionAborted:
                        case SocketError.Shutdown:
                        case SocketError.NotConnected:
                            _cancellationTokenSource?.Cancel();
                            lock (_locker)
                            {
                                _needToReconnect = true;
                                OnAutoDisconnected(new EventArgs());
                            }
                            break;
                        default:
                            break;
                    }
                    //*/

                }
                catch (Exception e)
                {
                    // 异常
                    _myLogger.AddLog(LogType.Alert, $"接收数据捕获Exception异常,{e.Message}");
                }
            }

            _myLogger.AddLog(LogType.Warning, $"_receiveTask任务结束");
        }

        void ParseReceivedFrame()
        {
            while (_receiviedBytes.Count >= 6)
            {
                var tempLength = MyDataConverter.GetUInt16FromBytes(new byte[] {
                    _receiviedBytes[4],
                    _receiviedBytes[5]
                });

                var receivedFrameLength = 6 + tempLength;
                if (_receiviedBytes.Count >= receivedFrameLength)
                {
                    var receivedFrameBytes = _receiviedBytes.GetRange(0, receivedFrameLength).ToArray();
                    _receiviedBytes.RemoveRange(0, receivedFrameLength);

                    //Console.WriteLine($"剩余缓存{MyDataConverter.GetStringFromBytes(_receiviedBytes.ToArray())}");

                    #region TaskCompletionSource async/await mode
                    var transactionId = MyDataConverter.GetUInt16FromBytes(new byte[] {
                        receivedFrameBytes[0],
                        receivedFrameBytes[1]
                    });

                    if (_tcsDictionary.TryGetValue(transactionId, out var item))
                    {
                        item.Item1.TrySetResult(receivedFrameBytes);
                        //var successful = item.Item1.TrySetResult(receivedFrameBytes);
                        //if (!successful)
                        //{
                        //    Console.WriteLine(MyDataConverter.GetStringFromBytes(receivedFrameBytes));
                        //    _myLogger.AddLog(LogType.Alert, $"ParseReceivedFrame执行TrySetResult失败,{MyDataConverter.GetStringFromBytes(receivedFrameBytes)}");
                        //}
                    }
                    #endregion
                }
            }
        }

        protected virtual async Task WaitForTaskDone()
        {
            var releaseTaskList = new List<Task>();

            if (_receiveTask != null)
            {
                releaseTaskList.Add(_receiveTask);
            }

            if (_heartbeatTask != null)
            {
                releaseTaskList.Add(_heartbeatTask);
            }

            await Task.WhenAll(releaseTaskList);

            if (_receiveTask != null)
            {
                _receiveTask = null;
            }

            if (_heartbeatTask != null)
            {
                _heartbeatTask = null;
            }
        }

        protected virtual async void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _tcsDictionary = null;
                    _receiviedBytes = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                _socket?.Dispose();
                _cancellationTokenSource?.Dispose();

                await WaitForTaskDone();

                _receiveTask?.Dispose();
                _heartbeatTask?.Dispose();
                _semaphoreSlim?.Dispose();

                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MyModbusTCP()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~MyModbusTCP2()
        {
            Dispose(disposing: false);
        }
    }
}
