using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyModbus.Common
{
    public class MyModbusTCP : IMyModbus
    {
        // 同步锁
        protected readonly object _locker = new object();
        // 异步锁,用于await的代码块
        protected readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        protected readonly SemaphoreSlim _semaphoreSlimForDisconnect = new SemaphoreSlim(1);

        protected bool _needToReconnect = false;
        protected bool _supportPipelineMode = false;
        protected int _transactionId = 0;
        protected Socket _socket;
        protected CancellationTokenSource _cancellationTokenSource;
        protected ConcurrentQueue<byte[]> _sendQueue = new ConcurrentQueue<byte[]>();
        protected ConcurrentDictionary<ushort, (TaskCompletionSource<byte[]>, CancellationTokenSource)> _tcsDictionary = new ConcurrentDictionary<ushort, (TaskCompletionSource<byte[]>, CancellationTokenSource)>();

        protected List<byte> _receiviedBytes = new List<byte>();

        protected Task _sendTask;
        protected Task _receiveTask;
        protected Task _heartbeatTask;

        protected int _heartBeatFailedCount = 0;
        protected int _heartBeatFailedThreshold = 3;
        protected int _MaxTransactionId = 10000;
        protected bool disposedValue;
        protected MyLogger _myLogger;

        public event EventHandler<EventArgs> AutoConnected;
        public event EventHandler<EventArgs> AutoDisconnected;

        public string Address { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public bool Connected { get; set; } = false;
        public int DefaultTimeout { get; set; } = 2000;
        public int SendReceiveTimeout { get; set; } = 2000;

        public MyModbusTCP(MyLogger myLogger, string address, int port, bool supportPipelineMode = false)
        {
            _myLogger = myLogger;
            Address = address;
            Port = port;
            _supportPipelineMode = supportPipelineMode;
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
        /// 连接TCP server端,如plc或终端设备
        /// </summary>
        public bool Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SendTimeout = DefaultTimeout;
            _socket.ReceiveTimeout = DefaultTimeout;
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

                _myLogger.AddLog(LogLevel.Info, "打开socket成功");

                RunTaskAfterConnect();
            }
            catch (Exception e)
            {
                _socket.Close();
                _myLogger.AddLog(LogLevel.Error, "打开socket失败");
                //throw new Exception("connect failed");
                return false;
            }
            Connected = true;
            return true;
        }

        protected virtual void RunTaskAfterConnect()
        {
            if (_sendTask == null)
            {
                _sendTask = Task.Run(SendAsync, _cancellationTokenSource.Token);
            }

            if (_receiveTask == null)
            {
                _receiveTask = Task.Run(ReceiveAsync, _cancellationTokenSource.Token);
            }

            if (_heartbeatTask == null)
            {
                _heartbeatTask = Task.Run(CheckHeartbeatAsnyc, _cancellationTokenSource.Token);
            }

            //主动调用DisconnectAsync(主动点击Disconnect按钮)和被动主动调用DisconnectAsync(发现下位机plc离线)和被动都会触发下面DisconnectAsync
            //当_sendTask,_receiveTask,_heartbeatTask都完成后,调用DisconnectAsync
            Task.WhenAll(_sendTask, _receiveTask, _heartbeatTask).ContinueWith(async t =>
            {
                await DisconnectAsync();
            });
        }

        public async Task DisconnectAsync()
        {
            if (Connected)
            {
                await _semaphoreSlimForDisconnect.WaitAsync();

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

                        _myLogger.AddLog(LogLevel.Info, "关闭socket成功");

                        Connected = false;

                        if (_needToReconnect)
                        {
                            _myLogger.AddLog(LogLevel.Info, $"触发重连");
                            await this.TryReconnect();
                        }
                    }

                }
                finally
                {
                    _semaphoreSlimForDisconnect.Release();
                }
            }
        }

        protected virtual async Task TryReconnect()
        {

            while (_needToReconnect)
            {
                // 尝试重连
                if (Connect())
                {
                    _myLogger.AddLog(LogLevel.Info, $"重连成功");
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
                    _myLogger.AddLog(LogLevel.Error, $"重连失败,尝试关闭socket");
                    await DisconnectAsync();
                }

                await Task.Delay(DefaultTimeout);
            }
        }

        protected virtual async Task CheckHeartbeatAsnyc()
        {
            _myLogger.AddLog(LogLevel.Warn, $"_heartbeatTask任务开始");

            await Task.Delay(DefaultTimeout);

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
                    _myLogger.AddLog(LogLevel.Error, $"心跳检测捕获TimeoutException异常,{te.Message}");
                }
                catch (OperationCanceledException oce)
                {
                    // cancel异常
                    _heartBeatFailedCount++;
                    _myLogger.AddLog(LogLevel.Error, $"心跳检测捕获OperationCanceledException异常,{oce.Message}");
                }
                catch (SocketException se)
                {
                    // socket异常
                    _myLogger.AddLog(LogLevel.Error, $"心跳检测捕获SocketException异常{se.SocketErrorCode.ToString()},{se.Message}");
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
                    _myLogger.AddLog(LogLevel.Error, $"心跳检测捕获Exception异常,{e.Message}");
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

            _myLogger.AddLog(LogLevel.Warn, $"_heartbeatTask任务结束");
        }


        /// <summary>
        /// _sendTask循环调用_socket.Send方法
        /// </summary>
        /// <returns></returns>
        protected virtual async Task SendAsync()
        {
            _myLogger.AddLog(LogLevel.Warn, $"_sendTask任务开始");

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                byte[] sendingBytes = null;

                if (_sendQueue.TryDequeue(out sendingBytes))
                {
                    var tempTransactionId = MyDataConverter.GetUInt16FromBytes(new byte[] { sendingBytes[0], sendingBytes[1] });

                    if (_tcsDictionary.TryGetValue(tempTransactionId, out var item))
                    {
                        try
                        {
                            _socket.Send(sendingBytes);
                        }
                        catch (TimeoutException te)
                        {
                            // timeout异常
                            //throw new TimeoutException(tex.Message);
                            _myLogger.AddLog(LogLevel.Error, $"发送数据捕获TimeoutException异常,{te.Message}");
                            //throw te;
                            item.Item1.SetException(te);
                        }
                        catch (OperationCanceledException oce)
                        {
                            // cancel异常
                            _myLogger.AddLog(LogLevel.Error, $"发送数据捕获OperationCanceledException异常,{oce.Message}");
                            //throw oce;
                            item.Item1.SetException(oce);
                        }
                        catch (SocketException se)
                        {
                            // socket异常
                            _myLogger.AddLog(LogLevel.Error, $"发送数据捕获SocketException异常{se.SocketErrorCode.ToString()},{se.Message}");
                            //*
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
                                    break;
                            }
                            //*/

                            //throw se;
                            item.Item1.SetException(se);
                        }
                        catch (Exception e)
                        {
                            _myLogger.AddLog(LogLevel.Error, $"发送数据捕获Exception异常,{e.Message}");
                            //throw e;
                            item.Item1.SetException(e);
                        }
                        finally
                        {
                            //if (_tcsDictionary.TryRemove(tempTransactionId, out var item))
                            //{
                            //    item.Item2.Cancel();
                            //    item.Item2.Dispose();
                            //    cancelTokenRegistration.Dispose();
                            //}
                        }
                    }
                }
                await Task.Delay(10);
            }

            _myLogger.AddLog(LogLevel.Warn, $"_sendTask任务结束");
        }

        /// <summary>
        /// CAS自旋机制实现TransactionId自增
        /// </summary>
        /// <returns></returns>
        ushort GetTransactionId()
        {
            int oldValue;
            int newValue;
            do
            {
                oldValue = Volatile.Read(ref _transactionId);

                if (oldValue > _MaxTransactionId)
                {
                    newValue = 1;
                }
                else
                {
                    newValue = oldValue + 1;
                }
            } while (Interlocked.CompareExchange(ref _transactionId, newValue, oldValue) != oldValue);
            return (ushort)newValue;
        }

        #region 发送和响应
        /// <summary>
        /// 发送数据然后获取到响应数据
        /// </summary>
        /// <param name="sendingBytes"></param>
        /// <returns></returns>
        public async Task<byte[]> SendAndReceiveAsync(byte[] sendingBytes)
        {
            ushort tempTransactionId = 0;

            //tempTransactionId = GetTransactionId();
            //*
            lock (_locker)
            {
                if (_transactionId >= _MaxTransactionId)
                {
                    _transactionId = 1;
                }
                else
                {
                    _transactionId++;
                }

                tempTransactionId = (ushort)_transactionId;
            }
            //*/

            sendingBytes[0] = (byte)(tempTransactionId >> 8);
            sendingBytes[1] = (byte)(tempTransactionId & 0xFF);

            _myLogger.AddLog(LogLevel.Info, $"发送数据：{MyDataConverter.GetStringFromBytes(sendingBytes)}");

            var tcs = new TaskCompletionSource<byte[]>();

            // 创建带有超时的cts
            var cts = new CancellationTokenSource(SendReceiveTimeout);
            // 注册超时后的callback
            var cancelTokenRegistration = cts.Token.Register(() =>
            {
                // 超时后删除tcs
                if (_tcsDictionary.TryRemove(tempTransactionId, out var item))
                {
                    item.Item1.TrySetException(new TimeoutException("Send and receive timeout! please check whether socket is connected."));
                    item.Item2.Dispose();
                }
            });


            byte[] receivingBytes = default;

            try
            {
                _tcsDictionary.TryAdd(tempTransactionId, (tcs, cts));
                if (_supportPipelineMode)
                {
                    // 流水线模式,即高级plc能并发接收处理请求(普通plc在接收到一个请求后,在这个请求没完成前,后续请求会排队,即他们不支持流水线模式,只支持串行模式).

                    // 发送数据入栈由_sendTask发送,响应数据由_receiveTask通过tcs返回,由于不强求一发一收的顺序,多条请求可以随便哪种顺序.因此不能加临界区
                    _sendQueue.Enqueue(sendingBytes);
                    receivingBytes = await tcs.Task;
                }
                else
                {
                    // 非流水线模式（串行模式）,即支持顺序一发一收,一发一收,一发一收这种强制的请求响应模式.
                    await _semaphoreSlim.WaitAsync();
                    try
                    {
                        // 发送数据入栈由_sendTask发送,响应数据由_receiveTask通过tcs返回,由于必须顺序一发一收.因此必须加上临界区
                        _sendQueue.Enqueue(sendingBytes);
                        receivingBytes = await tcs.Task;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }
                }
            }
            catch (TimeoutException te)
            {
                // timeout异常
                //throw new TimeoutException(tex.Message);
                _myLogger.AddLog(LogLevel.Error, $"发送数据捕获TimeoutException异常,{te.Message}");
                throw te;
            }
            catch (OperationCanceledException oce)
            {
                // cancel异常
                _myLogger.AddLog(LogLevel.Error, $"发送数据捕获OperationCanceledException异常,{oce.Message}");
                throw oce;
            }
            catch (SocketException se)
            {
                // socket异常
                _myLogger.AddLog(LogLevel.Error, $"发送数据捕获SocketException异常{se.SocketErrorCode.ToString()},{se.Message}");
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
                _myLogger.AddLog(LogLevel.Error, $"发送数据捕获Exception异常,{e.Message}");
                throw e;
            }
            finally
            {
                // 尝试移除tcs
                if (_tcsDictionary.TryRemove(tempTransactionId, out var item))
                {
                    item.Item2.Cancel();
                    item.Item2.Dispose();
                    cancelTokenRegistration.Dispose();
                }
            }

            _myLogger.AddLog(LogLevel.Info, $"接收数据：{MyDataConverter.GetStringFromBytes(receivingBytes)}");

            return receivingBytes;
        }
        #endregion

        /// <summary>
        /// _receiveTask循环调用_socket.Receive方法,然后循环取出完整响应帧(即一次响应的完整数据),这样可以解决分片和粘包问题
        /// </summary>
        /// <returns></returns>
        protected virtual async Task ReceiveAsync()
        {
            _myLogger.AddLog(LogLevel.Warn, $"_receiveTask任务开始");

            byte[] receivingBytes = new byte[1024];

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                //await Task.Delay(100);
                try
                {
                    int receivingBytesLength = await _socket.ReceiveAsync(new ArraySegment<byte>(receivingBytes), SocketFlags.None);
                    //lock (_locker)
                    //{
                    _receiviedBytes.AddRange(receivingBytes.Take(receivingBytesLength));
                    ParseReceivedFrame();
                    //}
                }
                catch (TimeoutException te)
                {
                    // timeout异常
                    _myLogger.AddLog(LogLevel.Error, $"接收数据捕获TimeoutException异常,{te.Message}");
                }
                catch (OperationCanceledException oce)
                {
                    // cancel异常
                    _myLogger.AddLog(LogLevel.Error, $"接收数据捕获OperationCanceledException异常,{oce.Message}");
                }
                catch (SocketException se)
                {
                    // socket异常
                    _myLogger.AddLog(LogLevel.Error, $"接收数据捕获SocketException异常{se.SocketErrorCode.ToString()},{se.Message}");
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
                    _myLogger.AddLog(LogLevel.Error, $"接收数据捕获Exception异常,{e.Message}");
                }
            }

            _myLogger.AddLog(LogLevel.Warn, $"_receiveTask任务结束");
        }

        protected virtual void ParseReceivedFrame()
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
                        //    _myLogger.AddLog(LogLevel.Error, $"ParseReceivedFrame执行TrySetResult失败,{MyDataConverter.GetStringFromBytes(receivedFrameBytes)}");
                        //}
                    }
                    #endregion
                }
            }
        }

        protected virtual async Task WaitForTaskDone()
        {
            var releaseTaskList = new List<Task>();

            if (_sendTask != null)
            {
                releaseTaskList.Add(_sendTask);
            }

            if (_receiveTask != null)
            {
                releaseTaskList.Add(_receiveTask);
            }

            if (_heartbeatTask != null)
            {
                releaseTaskList.Add(_heartbeatTask);
            }

            await Task.WhenAll(releaseTaskList);

            if (_sendTask != null)
            {
                _sendTask = null;
            }

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

                _sendTask?.Dispose();
                _receiveTask?.Dispose();
                _heartbeatTask?.Dispose();
                _semaphoreSlim?.Dispose();
                _semaphoreSlimForDisconnect?.Dispose();

                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~MyModbusTCP()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
