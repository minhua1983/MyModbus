using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyModbus.Common
{
    public class MyModbusContext : IDisposable
    {
        IMyModbus _myModbus;
        MyLogger _myLogger;

        private bool disposedValue;
        CancellationTokenSource _cancellationTokenSource;
        Task _collectTask;

        Dictionary<byte, MyDeviceConfig> _deviceConfigDictionary;
        bool _needTocollect = false;
        public event EventHandler<MyEventArgs> Collected;

        long _collectedCount = 0;
        public Dictionary<byte, MyDeviceConfig> DeviceConfigDictionary
        {
            get
            {
                return _deviceConfigDictionary;
            }
        }
        public int DefaultTimeout { get; set; } = 2000;

        public bool Connected
        {
            get
            {
                return _myModbus.Connected;
            }
        }

        public MyModbusContext(MyLogger myLogger, IMyModbus myModbus, params MyDeviceConfig[] deviceConfigs)
        {
            _myLogger = myLogger;
            _myModbus = myModbus;
            _myModbus.AutoConnected += _myModbus_AutoConnected;
            _myModbus.AutoDisconnected += _myModbus_AutoDisconnected;

            _deviceConfigDictionary = new Dictionary<byte, MyDeviceConfig>();
            foreach (var item in deviceConfigs)
            {
                _deviceConfigDictionary.Add(item.DeviceId, item);
            }
        }

        protected virtual void OnCollected(MyEventArgs e)
        { 
            Collected?.Invoke(this, e);
        }

        private void _myModbus_AutoDisconnected(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private void _myModbus_AutoConnected(object sender, EventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _collectTask = Task.Run(CollectAsync, _cancellationTokenSource.Token);
        }

        public bool Connect()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            if (_collectTask == null)
            {
                _collectTask = Task.Run(CollectAsync, _cancellationTokenSource.Token);
            }

            return _myModbus.Connect();
        }

        public async Task DisconnectAsync()
        {
            // 取消_cancellationTokenSource
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            if (_collectTask != null)
            {
                //await Task.Delay(1000);
                await Task.WhenAll(_collectTask);
            }

            if (_collectTask != null)
            {
                _collectTask = null;
            }

            await _myModbus.DisconnectAsync();
        }

        public void WhetherToCollect(bool needTocollect)
        {
            _needTocollect = needTocollect;
        }

        async Task CollectAsync()
        {
            _myLogger.AddLog(LogLevel.Warn, $"_collectTask任务开始");

            await Task.Delay(DefaultTimeout);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (!_needTocollect)
                {
                    continue;
                }
                //*
                // 采集逻辑
                foreach (var item in _deviceConfigDictionary.Values)
                {

                    var collectList = item.PointConfigList.Where(pc => (DateTime.Now - pc.LastCollected).TotalMilliseconds > pc.CollectInterval)?.ToList();
                    collectList.ForEach(async pc =>
                    {
                        try
                        {
                            pc.LastCollected = DateTime.Now;
                            await GetValueAsync(item, pc);
                            Interlocked.Increment(ref _collectedCount);
                            OnCollected(new MyEventArgs(_collectedCount));
                        }
                        catch (TimeoutException te)
                        {
                            // timeout异常
                            //throw new TimeoutException(tex.Message);
                            _myLogger.AddLog(LogLevel.Error, $"采集数据捕获TimeoutException异常,{te.Message}");
                            //throw te;
                        }
                        catch (OperationCanceledException oce)
                        {
                            // cancel异常
                            _myLogger.AddLog(LogLevel.Error, $"采集数据捕获OperationCanceledException异常,{oce.Message}");
                            //throw oce;
                        }
                        catch (SocketException se)
                        {
                            // socket异常
                            _myLogger.AddLog(LogLevel.Error, $"采集数据捕获SocketException异常{se.SocketErrorCode.ToString()},{se.Message}");
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

                            //throw se;
                        }
                        catch (Exception e)
                        {
                            _myLogger.AddLog(LogLevel.Error, $"采集数据捕获Exception异常,{e.Message}");
                            //throw e;
                        }
                        finally
                        {

                        }

                    });
                }
                //*/

                await Task.Delay(10);
            }

            _myLogger.AddLog(LogLevel.Warn, $"_collectTask任务结束");
        }

        public async Task GetValueAsync(MyDeviceConfig item, IMyPointConfig pc)
        {
            if (pc.DataRegion == MyPointDataRegion.DiscreteInput && pc.DataType == MyPointDataType.Bool)
            {
                var values = await GetBoolsAsync(item.DeviceId, FunctionCode.FC02, pc.Address, 1);
                var npc = (MyPointConfig<bool>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.Coil && pc.DataType == MyPointDataType.Bool)
            {
                var values = await GetBoolsAsync(item.DeviceId, FunctionCode.FC01, pc.Address, 1);
                var npc = (MyPointConfig<bool>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.InputRegister && pc.DataType == MyPointDataType.UInt16)
            {
                var values = await GetUInt16sAsync(item.DeviceId, FunctionCode.FC04, pc.Address, 1);
                var npc = (MyPointConfig<ushort>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.UInt16)
            {
                var values = await GetUInt16sAsync(item.DeviceId, FunctionCode.FC03, pc.Address, 1);
                var npc = (MyPointConfig<ushort>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.InputRegister && pc.DataType == MyPointDataType.Int16)
            {
                var values = await GetInt16sAsync(item.DeviceId, FunctionCode.FC04, pc.Address, 1);
                var npc = (MyPointConfig<short>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.Int16)
            {
                var values = await GetInt16sAsync(item.DeviceId, FunctionCode.FC03, pc.Address, 1);
                var npc = (MyPointConfig<short>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.InputRegister && pc.DataType == MyPointDataType.UInt32)
            {
                var values = await GetUInt32sAsync(item.DeviceId, FunctionCode.FC04, pc.Address, 1);
                var npc = (MyPointConfig<uint>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.UInt32)
            {
                var values = await GetUInt32sAsync(item.DeviceId, FunctionCode.FC03, pc.Address, 1);
                var npc = (MyPointConfig<uint>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.InputRegister && pc.DataType == MyPointDataType.Int32)
            {
                var values = await GetInt32sAsync(item.DeviceId, FunctionCode.FC04, pc.Address, 1);
                var npc = (MyPointConfig<int>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.Int32)
            {
                var values = await GetInt32sAsync(item.DeviceId, FunctionCode.FC03, pc.Address, 1);
                var npc = (MyPointConfig<int>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.InputRegister && pc.DataType == MyPointDataType.Float32)
            {
                var values = await GetFloat32sAsync(item.DeviceId, FunctionCode.FC04, pc.Address, 1);
                var npc = (MyPointConfig<float>)pc;
                npc.Callback(values[0]);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.Float32)
            {
                var values = await GetFloat32sAsync(item.DeviceId, FunctionCode.FC03, pc.Address, 1);
                var npc = (MyPointConfig<float>)pc;
                npc.Callback(values[0]);
            }
        }

        public async Task SetValueAsync(MyDeviceConfig item, IMyPointConfig pc, object value)
        {

            if (pc.DataRegion == MyPointDataRegion.Coil && pc.DataType == MyPointDataType.Bool)
            {
                bool[] values = new bool[1] { (bool)value };
                await SetBoolsAsync(item.DeviceId, pc.Address, values);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.UInt16)
            {
                ushort[] values = new ushort[1] { (ushort)value };
                await SetUInt16sAsync(item.DeviceId, pc.Address, values);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.Int16)
            {
                short[] values = new short[1] { (short)value };
                await SetInt16sAsync(item.DeviceId, pc.Address, values);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.UInt32)
            {
                uint[] values = new uint[1] { (uint)value };
                await SetUInt32sAsync(item.DeviceId, pc.Address, values);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.Int32)
            {
                int[] values = new int[1] { (int)value };
                await SetInt32sAsync(item.DeviceId, pc.Address, values);
            }
            else if (pc.DataRegion == MyPointDataRegion.HoldingRegister && pc.DataType == MyPointDataType.Float32)
            {
                float[] values = new float[1] { (float)value };
                await SetFloat32sAsync(item.DeviceId, pc.Address, values);
            }
        }

        public void CheckNumberValue<T>(MyPointConfig<T> pc, T v) where T : IComparable<T>
        {
            dynamic value = v;
            dynamic highThreshold = pc.HighThreshold;
            dynamic lowThreshold = pc.LowThreshold;
            dynamic highDeadBand = pc.HighDeadBand;
            dynamic lowDeadBand = pc.LowDeadBand;


            var isInvalid = value >= highThreshold
                || value <= lowThreshold
                || value >= highThreshold - highDeadBand && pc.IsAlarmed
                || value <= lowThreshold + lowDeadBand && pc.IsAlarmed;

            if (isInvalid)
            {
                // 非法值
                if (!pc.IsAlarmed)
                {
                    // 还没报警
                    if (!pc.IsNoticed)
                    {
                        // 还没预警
                        if ((DateTime.Now - pc.LastNoticed).TotalMilliseconds >= pc.OnDelay)
                        {
                            // 还没报警，还没预警，不在OnDelay时间范围内，进行预警
                            pc.IsNoticed = true;
                            pc.LastNoticed = DateTime.Now;
                            _myLogger.AddAlarmLog(LogLevel.Warn, $"{pc.ClassName}.{pc.PropertyName}触发预警，当前值{v}");
                        }
                        else
                        {
                            // 还没报警，还没预警，在OnDelay时间范围内，这种情况不存在，不用处理
                        }
                    }
                    else
                    {
                        // 已经预警
                        if ((DateTime.Now - pc.LastNoticed).TotalMilliseconds >= pc.OnDelay)
                        {
                            // 还没报警，已经预警，不在OnDelay时间范围内，进行报警
                            pc.IsAlarmed = true;
                            pc.LastAlarmed = DateTime.Now;
                            pc.LastNoticed = DateTime.Now;
                            _myLogger.AddAlarmLog(LogLevel.Warn, $"{pc.ClassName}.{pc.PropertyName}触发报警，当前值{v}");
                        }
                        else
                        {
                            // 还没报警，已经预警，在OnDelay时间范围内，这种情况正常
                            //pc.LastNoticed = DateTime.Now;
                        }
                    }
                }
                else
                {
                    // 已经报警
                    pc.LastAlarmed = DateTime.Now;
                    pc.LastNoticed = DateTime.Now;
                }
            }
            else
            {
                if ((DateTime.Now - pc.LastAlarmed).TotalMilliseconds >= pc.OffDelay && pc.IsAlarmed)
                {
                    pc.IsAlarmed = false;
                    pc.LastNoticed = DateTime.Now;
                    _myLogger.AddAlarmLog(LogLevel.Warn, $"{pc.ClassName}.{pc.PropertyName}取消报警，当前值{v}");
                }

                if ((DateTime.Now - pc.LastNoticed).TotalMilliseconds >= pc.OffDelay && pc.IsNoticed)
                {
                    pc.IsNoticed = false;
                    _myLogger.AddAlarmLog(LogLevel.Warn, $"{pc.ClassName}.{pc.PropertyName}取消预警，当前值{v}");
                }

            }
        }

        public async Task<byte[]> GetBytesAsync(byte slaveId, FunctionCode functionCode, ushort start, ushort quantity)
        {
            byte[] sendingBytes = default;
            switch (functionCode)
            {
                case FunctionCode.FC01:
                    sendingBytes = MyModbusProtocol.BuildReadOutputCoils(slaveId, start, quantity);
                    break;
                case FunctionCode.FC02:
                    sendingBytes = MyModbusProtocol.BuildReadInputCoils(slaveId, start, quantity);
                    break;
                case FunctionCode.FC03:
                    sendingBytes = MyModbusProtocol.BuildReadOutputRegisters(slaveId, start, quantity);
                    break;
                case FunctionCode.FC04:
                    sendingBytes = MyModbusProtocol.BuildReadInputRegisters(slaveId, start, quantity);
                    break;
                default:
                    throw new Exception($"not support function code: {functionCode.ToString()}");
            }
            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
            return receivingBytes;
        }

        public async Task<bool[]> GetBoolsAsync(byte slaveId, FunctionCode functionCode, ushort start, ushort quantity)
        {
            byte[] sendingBytes = default;
            switch (functionCode)
            {
                case FunctionCode.FC01:
                    sendingBytes = MyModbusProtocol.BuildReadOutputCoils(slaveId, start, quantity);
                    break;
                case FunctionCode.FC02:
                    sendingBytes = MyModbusProtocol.BuildReadInputCoils(slaveId, start, quantity);
                    break;
                default:
                    throw new Exception($"not support function code: {functionCode.ToString()}");
            }
            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
            var values = MyDataConverter.GetBoolsFromBytes(receivingBytes.Skip(9).ToArray()).Take(quantity).ToArray();
            return values;
        }

        public async Task<ushort[]> GetUInt16sAsync(byte slaveId, FunctionCode functionCode, ushort start, ushort quantity)
        {
            byte[] sendingBytes = default;
            switch (functionCode)
            {
                case FunctionCode.FC03:
                    sendingBytes = MyModbusProtocol.BuildReadOutputRegisters(slaveId, start, quantity);
                    break;
                case FunctionCode.FC04:
                    sendingBytes = MyModbusProtocol.BuildReadInputRegisters(slaveId, start, quantity);
                    break;
                default:
                    throw new Exception($"not support function code: {functionCode.ToString()}");
            }
            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
            var values = MyDataConverter.GetUInt16sFromBytes(receivingBytes.Skip(9).ToArray(), _deviceConfigDictionary[receivingBytes[6]].ByteOrder);
            return values;
        }

        public async Task<short[]> GetInt16sAsync(byte slaveId, FunctionCode functionCode, ushort start, ushort quantity)
        {
            byte[] sendingBytes = default;
            switch (functionCode)
            {
                case FunctionCode.FC03:
                    sendingBytes = MyModbusProtocol.BuildReadOutputRegisters(slaveId, start, quantity);
                    break;
                case FunctionCode.FC04:
                    sendingBytes = MyModbusProtocol.BuildReadInputRegisters(slaveId, start, quantity);
                    break;
                default:
                    throw new Exception($"not support function code: {functionCode.ToString()}");
            }
            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
            var values = MyDataConverter.GetInt16sFromBytes(receivingBytes.Skip(9).ToArray(), _deviceConfigDictionary[slaveId].ByteOrder);
            return values;
        }

        public async Task<uint[]> GetUInt32sAsync(byte slaveId, FunctionCode functionCode, ushort start, ushort quantity)
        {
            byte[] sendingBytes = default;
            switch (functionCode)
            {
                case FunctionCode.FC03:
                    sendingBytes = MyModbusProtocol.BuildReadOutputRegisters(slaveId, start, (ushort)(quantity * 2));
                    break;
                case FunctionCode.FC04:
                    sendingBytes = MyModbusProtocol.BuildReadInputRegisters(slaveId, start, (ushort)(quantity * 2));
                    break;
                default:
                    throw new Exception($"not support function code: {functionCode.ToString()}");
            }
            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
            var values = MyDataConverter.GetUInt32sFromBytes(receivingBytes.Skip(9).ToArray(), _deviceConfigDictionary[slaveId].ByteOrder, _deviceConfigDictionary[slaveId].WordOrder);
            return values;
        }

        public async Task<int[]> GetInt32sAsync(byte slaveId, FunctionCode functionCode, ushort start, ushort quantity)
        {
            byte[] sendingBytes = default;
            switch (functionCode)
            {
                case FunctionCode.FC03:
                    sendingBytes = MyModbusProtocol.BuildReadOutputRegisters(slaveId, start, (ushort)(quantity * 2));
                    break;
                case FunctionCode.FC04:
                    sendingBytes = MyModbusProtocol.BuildReadInputRegisters(slaveId, start, (ushort)(quantity * 2));
                    break;
                default:
                    throw new Exception($"not support function code: {functionCode.ToString()}");
            }
            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
            var values = MyDataConverter.GetInt32sFromBytes(receivingBytes.Skip(9).ToArray(), _deviceConfigDictionary[slaveId].ByteOrder, _deviceConfigDictionary[slaveId].WordOrder);
            return values;
        }

        public async Task<float[]> GetFloat32sAsync(byte slaveId, FunctionCode functionCode, ushort start, ushort quantity)
        {
            byte[] sendingBytes = default;
            switch (functionCode)
            {
                case FunctionCode.FC03:
                    sendingBytes = MyModbusProtocol.BuildReadOutputRegisters(slaveId, start, (ushort)(quantity * 2));
                    break;
                case FunctionCode.FC04:
                    sendingBytes = MyModbusProtocol.BuildReadInputRegisters(slaveId, start, (ushort)(quantity * 2));
                    break;
                default:
                    throw new Exception($"not support function code: {functionCode.ToString()}");
            }
            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
            var values = MyDataConverter.GetFloat32sFromBytes(receivingBytes.Skip(9).ToArray(), _deviceConfigDictionary[slaveId].ByteOrder, _deviceConfigDictionary[slaveId].WordOrder);
            return values;
        }

        public async Task SetBoolsAsync(byte slaveId, ushort start, bool[] values)
        {
            if (values == null || values.Length == 0) { throw new Exception("invalid values"); }

            byte[] sendingBytes = default;

            if (values.Length == 1)
            {
                sendingBytes = MyModbusProtocol.BuildWriteSingleCoil(slaveId, start, values[0]);

            }
            else
            {
                var valuesBytes = MyDataConverter.GetBytesFromBools(values);
                sendingBytes = MyModbusProtocol.BuildWriteMultiCoils(slaveId, start, (ushort)values.Length, (byte)valuesBytes.Length, valuesBytes);
            }

            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
        }

        public async Task SetUInt16sAsync(byte slaveId, ushort start, ushort[] values)
        {
            if (values == null || values.Length == 0) { throw new Exception("invalid values"); }

            byte[] sendingBytes = default;
            var valuesBytes = MyDataConverter.GetBytesFromUInt16s(values, _deviceConfigDictionary[slaveId].ByteOrder);
            if (values.Length == 1)
            {
                sendingBytes = MyModbusProtocol.BuildWriteSingleRegister(slaveId, start, valuesBytes);

            }
            else
            {

                sendingBytes = MyModbusProtocol.BuildWriteMultiRegisters(slaveId, start, (ushort)values.Length, (byte)valuesBytes.Length, valuesBytes);
            }

            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
        }

        public async Task SetInt16sAsync(byte slaveId, ushort start, short[] values)
        {
            if (values == null || values.Length == 0) { throw new Exception("invalid values"); }

            byte[] sendingBytes = default;
            var valuesBytes = MyDataConverter.GetBytesFromInt16s(values, _deviceConfigDictionary[slaveId].ByteOrder);

            if (values.Length == 1)
            {
                sendingBytes = MyModbusProtocol.BuildWriteSingleRegister(slaveId, start, valuesBytes);

            }
            else
            {
                sendingBytes = MyModbusProtocol.BuildWriteMultiRegisters(slaveId, start, (ushort)values.Length, (byte)valuesBytes.Length, valuesBytes);
            }

            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
        }

        public async Task SetUInt32sAsync(byte slaveId, ushort start, uint[] values)
        {
            if (values == null || values.Length == 0) { throw new Exception("invalid values"); }

            byte[] sendingBytes = default;
            var valuesBytes = MyDataConverter.GetBytesFromUInt32s(values, _deviceConfigDictionary[slaveId].ByteOrder, _deviceConfigDictionary[slaveId].WordOrder);

            // a 32 bit data always writes 2 as quantity, so cannot use MyModbusProtocol.BuildWriteSingleRegister
            sendingBytes = MyModbusProtocol.BuildWriteMultiRegisters(slaveId, start, (ushort)(values.Length * 2), (byte)valuesBytes.Length, valuesBytes);

            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
        }

        public async Task SetInt32sAsync(byte slaveId, ushort start, int[] values)
        {
            if (values == null || values.Length == 0) { throw new Exception("invalid values"); }

            byte[] sendingBytes = default;
            var valuesBytes = MyDataConverter.GetBytesFromInt32s(values, _deviceConfigDictionary[slaveId].ByteOrder, _deviceConfigDictionary[slaveId].WordOrder);

            // a 32 bit data always writes 2 as quantity, so cannot use MyModbusProtocol.BuildWriteSingleRegister
            sendingBytes = MyModbusProtocol.BuildWriteMultiRegisters(slaveId, start, (ushort)(values.Length * 2), (byte)valuesBytes.Length, valuesBytes);

            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
        }

        public async Task SetFloat32sAsync(byte slaveId, ushort start, float[] values)
        {
            if (values == null || values.Length == 0) { throw new Exception("invalid values"); }

            byte[] sendingBytes = default;
            var valuesBytes = MyDataConverter.GetBytesFromFloat32s(values, _deviceConfigDictionary[slaveId].ByteOrder, _deviceConfigDictionary[slaveId].WordOrder);

            // a 32 bit data always writes 2 as quantity, so cannot use MyModbusProtocol.BuildWriteSingleRegister
            sendingBytes = MyModbusProtocol.BuildWriteMultiRegisters(slaveId, start, (ushort)(values.Length * 2), (byte)valuesBytes.Length, valuesBytes);

            var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);
        }

        protected virtual async Task Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                _myModbus?.Dispose();

                await Task.WhenAll(_collectTask);

                _collectTask?.Dispose();

                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MyModbusContext()
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

        ~MyModbusContext()
        {
            Dispose(false);
        }
    }
}
