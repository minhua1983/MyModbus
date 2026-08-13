using MyModbus.Common;
using MyModbus.UI.Models;
using MyModbus.UI.Services;
using MyModbus.UI.Repositories;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Data.SQLite;
using System.Collections.Concurrent;

namespace MyModbus.UI
{
    public partial class Form1 : Form
    {
        //IMyModbus _myModbus;
        MyModbusContext _myModbusContext;
        MyLogger _myLogger;
        MyDevice _myDevice;
        CollectDataService _collectDataService;
        ConcurrentQueue<CollectData> _collectDataQueue;
        Task _persistTask;
        CancellationTokenSource _cancellationTokenSource;
        bool _isAllDoneBeforeFormClosing = false;
        long _insertedCount = 0;

        public Form1()
        {
            InitializeComponent();
            Init();
        }

        void Init()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _persistTask = Task.Run(PersistAsync, _cancellationTokenSource.Token);
            _collectDataQueue = new ConcurrentQueue<CollectData>();

            var conn = new SQLiteConnection("Data Source=mymodbus.db;Journal Mode=WAL;Cache Size=-10000");
            var collectDataRepository = new CollectDataRepository(conn);
            _collectDataService = new CollectDataService(collectDataRepository);


            _myDevice = new MyDevice(SynchronizationContext.Current);
            _myDevice.PropertyChanged += _myDevice_PropertyChanged;

            //var temperatureBinding = new Binding("Text", _myDevice, nameof(_myDevice.Temperature));
            //temperatureBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
            //lbl_temperature.DataBindings.Add(temperatureBinding);

            //var humidityBinding = new Binding("Text", _myDevice, nameof(_myDevice.Humidity));
            //humidityBinding.DataSourceUpdateMode = DataSourceUpdateMode.Never;
            //lbl_humidity.DataBindings.Add(humidityBinding);

            _myLogger = new MyLogger();
            var myModbus = new MyModbusTCP(_myLogger, "127.0.0.1", 502, false);

            myModbus.AutoDisconnected += _myModbus_AutoDisconnected;
            myModbus.AutoConnected += _myModbus_AutoConnected;

            var termperatureConfig = new MyPointConfig<int>
            {
                ClassName = nameof(MyDevice),
                PropertyName = $"Temperature",
                Description = $"温度",
                SlaveId = 1,
                DataRegion = MyPointDataRegion.HoldingRegister,
                Address = 4,
                Scale = 0.1m,
                DataType = MyPointDataType.Int32,
                CollectInterval = 50,
                HighThreshold = 800,
                LowThreshold = 200,
                HighDeadBand = 50,
                LowDeadBand = 50,
                OnDelay = 2000,
                OffDelay = 2000
            };

            termperatureConfig.Callback = value =>
            {
                _myDevice.SetSilent<int>("_temperature", value);
                _myModbusContext.CheckNumberValue<int>(termperatureConfig, _myDevice.Temperature);
                this.Invoke(new Action(() =>
                {
                    lbl_temperature.Text = (_myDevice.Temperature * termperatureConfig.Scale).ToString();
                    SetForeColor<int>(lbl_temperature, termperatureConfig);
                    AddChartSeriesPoint<int>(_myDevice.Temperature);
                }));
                /*
                _collectDataService.Insert(new CollectData()
                {
                    Context = "127000000001_1",
                    Name = $"{nameof(termperatureConfig.ClassName)}.{termperatureConfig.PropertyName}",
                    Value = value,
                    CollectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                });
                //*/
                _collectDataQueue.Enqueue(new CollectData()
                {
                    Context = "127000000001_1",
                    Name = $"{termperatureConfig.ClassName}.{termperatureConfig.PropertyName}",
                    Value = value,
                    CollectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                });
            };

            var humidityConfig = new MyPointConfig<int>
            {
                ClassName = nameof(MyDevice),
                PropertyName = $"Humidity",
                Description = $"湿度",
                SlaveId = 1,
                DataRegion = MyPointDataRegion.HoldingRegister,
                Address = 6,
                Scale = 0.1m,
                DataType = MyPointDataType.Int32,
                CollectInterval = 50,
                HighThreshold = 800,
                LowThreshold = 200,
                HighDeadBand = 50,
                LowDeadBand = 50,
                OnDelay = 2000,
                OffDelay = 2000
            };

            humidityConfig.Callback = value =>
            {
                _myDevice.SetSilent<int>("_humidity", value);
                _myModbusContext.CheckNumberValue<int>(humidityConfig, _myDevice.Humidity);
                this.Invoke(new Action(() =>
                {
                    lbl_humidity.Text = (_myDevice.Humidity * humidityConfig.Scale).ToString();
                    SetForeColor<int>(lbl_humidity, humidityConfig);
                }));
                _collectDataQueue.Enqueue(new CollectData()
                {
                    Context = "127000000001_1",
                    Name = $"{humidityConfig.ClassName}.{humidityConfig.PropertyName}",
                    Value = value,
                    CollectedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                });
            };

            _myModbusContext = new MyModbusContext(_myLogger, myModbus, new MyDeviceConfig
            {
                DeviceId = 1,
                WordOrder = WordOrder.HighWordFirst,
                ByteOrder = ByteOrder.HighByteFirst,
                PointConfigList = new List<IMyPointConfig>
                {
                    termperatureConfig,
                    humidityConfig
                }
            });
            _myModbusContext.Collected += _myModbusContext_Collected;

            _myLogger.Log += _myLogger_Log;
            WhetherToShowButtons(false);

            //* chart
            var series = new Series("Temperature");
            series.ChartType = SeriesChartType.FastLine;
            series.BorderWidth = 2;
            series.Color = Color.Blue;
            series.LegendText = "实时曲线图";
            series.XValueType = ChartValueType.DateTime;
            series.ToolTip = "时间:#VALX{HH:mm:ss}\n温度:#VALY{F2}";
            chart1.Series[0] = series;

            var chartArea = new ChartArea();
            chartArea.AxisX.LabelStyle.Format = "HH:mm:ss";

            chart1.ChartAreas[0] = chartArea;
            //*/
        }

        private void _myModbusContext_Collected(object sender, MyEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                lbl_collected_count.Text = e.Body.ToString();
            }));
        }

        async Task PersistAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (_collectDataQueue.TryDequeue(out CollectData collectData))
                {
                    _collectDataService.Insert(collectData);
                    Interlocked.Increment(ref _insertedCount);
                    Form1.ActiveForm?.Invoke(new Action(() => lbl_inserted_count.Text = _insertedCount.ToString()));
                }

                await Task.Delay(10);
            }

            while (true)
            {
                if (_collectDataQueue.TryDequeue(out CollectData collectData))
                {
                    _collectDataService.Insert(collectData);
                    Interlocked.Increment(ref _insertedCount);
                    Form1.ActiveForm?.Invoke(new Action(() => lbl_inserted_count.Text = _insertedCount.ToString()));
                }
                else
                {
                    break;
                }

                await Task.Delay(10);
            }

            await Task.Delay(500);
        }

        void AddChartSeriesPoint<T>(T value)
        {
            var series = chart1.Series[0];
            var maxPointCount = 100;
            var xValue = DateTime.Now;

            series.Points.AddXY(DateTime.Now, value);
            if (series.Points.Count > maxPointCount)
            {
                series.Points.RemoveAt(0);
            }

            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            // 每次更新都向左移动一单位（例如1秒）
            chart1.ChartAreas[0].AxisX.ScaleView.Zoom(chart1.ChartAreas[0].AxisX.ScaleView.Position - 1, chart1.ChartAreas[0].AxisX.ScaleView.Size);
            chart1.ChartAreas[0].RecalculateAxesScale(); // 重新计算轴的比例尺
        }

        void SetForeColor<T>(Control control, MyPointConfig<T> pc)
        {
            if (!pc.IsNoticed && !pc.IsAlarmed)
            {
                control.ForeColor = Color.Green;
            }

            if (pc.IsNoticed && !pc.IsAlarmed)
            {
                control.ForeColor = Color.Orange;
            }

            if (pc.IsAlarmed)
            {
                control.ForeColor = Color.Red;
            }
        }

        private void _myLogger_Log(object sender, MyEventArgs e)
        {
            var log = ((LogLevel, string))e.Body;
            AddLog(log.Item1, log.Item2);
        }

        public void AddLog(LogLevel logType, string message)
        {
            this.Invoke(new Action(() =>
            {
                ListViewItem item = new ListViewItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(logType.ToString());
                item.SubItems.Add(message);
                lv_message.Items.Add(item);

                //*
                if (lv_message.Items.Count > 10)
                {
                    // bug
                    lv_message.Items.RemoveAt(0);
                }
                //*/

                lv_message.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
            }));
        }

        //private void _myModbus_Received(object sender, ReceivedFrameEventArgs e)
        //{
        //    var receivedFrameBytes = e.ReceivedFrameBytes;
        //    if (InvokeRequired)
        //    {
        //        this.BeginInvoke(new Action(() =>
        //        {
        //            rtb_receive.Text = string.Join(" ", receivedFrameBytes.Select(b => b.ToString("X2")));
        //        }));
        //    }
        //}

        private void _myModbus_Disconnected(object sender, EventArgs e)
        {
            this.Invoke((Action)(() =>
            {
                btn_connect.Text = "开启设备";
                btn_connect.Enabled = true;
                WhetherToShowButtons(false);
            }));
        }

        private void _myModbus_AutoConnected(object sender, EventArgs e)
        {
            this.Invoke((Action)(() =>
            {
                btn_connect.Text = "关闭设备";
                btn_connect.Enabled = true;
                WhetherToShowButtons(true);
            }));
        }

        private void _myModbus_AutoDisconnected(object sender, EventArgs e)
        {
            this.Invoke((Action)(() =>
            {
                btn_connect.Text = "开启设备";
                btn_connect.Enabled = false;
                WhetherToShowButtons(false);
            }));
        }

        private async void btn_connect_Click(object sender, EventArgs e)
        {
            btn_connect.Enabled = false;
            WhetherToShowButtons(false);

            if (btn_connect.Text == "开启设备")
            {
                if (_myModbusContext.Connect())
                {
                    btn_connect.Text = "关闭设备";
                    btn_connect.Enabled = true;
                    WhetherToShowButtons(true);

                }
                else
                {
                    btn_connect.Enabled = true;
                    WhetherToShowButtons(true);
                }
            }
            else
            {
                await _myModbusContext.DisconnectAsync().ContinueWith(callbackTask =>
                {
                    this.Invoke((Action)(() =>
                    {
                        btn_connect.Text = "开启设备";
                        btn_connect.Enabled = true;
                        WhetherToShowButtons(false);
                    }));
                }); ;
            }


        }

        void WhetherToShowButtons(bool whetherToShowButton)
        {
            btn_send.Enabled = whetherToShowButton;
            btn_temperature_update.Enabled = whetherToShowButton;
            btn_humidity_update.Enabled = whetherToShowButton;
            btn_whether_to_collect.Enabled = whetherToShowButton;
        }

        private async void btn_send_Click(object sender, EventArgs e)
        {
            //var receivingBytes = _myModbus.SendAndReceive(sendingBytes);
            //_myModbus.Send(sendingBytes);

            // function code 0x02
            //var sendingBytes = MyModbusProtocol.BuildReadInputCoils(1, 0, 10);

            // function code 0x01
            //var sendingBytes = MyModbusProtocol.BuildReadOutputCoils(1, 0, 10);

            // function code 0x04
            //var sendingBytes = MyModbusProtocol.BuildReadInputRegisters(1, 0, 10);

            // function code 0x03
            //var sendingBytes = MyModbusProtocol.BuildReadOutputRegisters(1, 0, 10);

            // function code 0x05
            //var sendingBytes = MyModbusProtocol.BuildWriteSingleCoil(1, 0, false);

            // function code 0x06
            //var sendingBytes = MyModbusProtocol.BuildWriteSingleRegister(1, 0, 300);

            // function code 0x0F
            //var sendingBytes = MyModbusProtocol.BuildWriteMultiCoils(1, 0, 2, 1, new byte[] { 255 });

            // function code 0x10
            //var sendingBytes = MyModbusProtocol.BuildWriteMultiRegisters(1, 0, 2, 4, new byte[] { 1, 1, 0, 255 });

            //var receivingBytes = await _myModbus.SendAndReceiveAsync(sendingBytes);

            //var receivingBytes = await _myModbusContext.GetBytesAsync(1, FunctionCode.FC01, 0, 10);

            //var receivingBytes = await _myModbusContext.GetBoolsAsync(1, FunctionCode.FC01, 0, 10);

            //var receivingBytes = await _myModbusContext.GetUInt16sAsync(1, FunctionCode.FC03, 0, 5);

            //var receivingBytes = await _myModbusContext.GetInt16sAsync(1, FunctionCode.FC03, 0, 5);

            //var receivingBytes = await _myModbusContext.GetUInt32sAsync(1, FunctionCode.FC03, 0, 5);

            //var receivingBytes = await _myModbusContext.GetInt32sAsync(1, FunctionCode.FC03, 0, 5);

            //var receivingBytes = await _myModbusContext.GetFloat32sAsync(1, FunctionCode.FC03, 0, 3);

            /*
            this.BeginInvoke(new Action(() =>
            {
                rtb_receive.Text = string.Join(" ", receivingBytes.Select(b => b.ToString()));
            }));
            //*/

            //await _myModbusContext.SetBoolsAsync(1, 0, new bool[2] { true,false });

            //await _myModbusContext.SetUInt16sAsync(1, 0, new ushort[2] { 65535, 2 });

            //await _myModbusContext.SetInt16sAsync(1, 0, new short[2] { 10, 2 });

            //await _myModbusContext.SetUInt32sAsync(1, 0, new uint[3] { 1000000,12,1});

            await _myModbusContext.SetInt32sAsync(1, 4, new int[2] { -10, 2 });

            //await _myModbusContext.SetFloat32sAsync(1, 0, new float[5] { -11.1f, 2f, -3.0f, 4f, 5.555f });
        }

        private void btn_temperature_update_Click(object sender, EventArgs e)
        {
            var deviceConfig = _myModbusContext.DeviceConfigDictionary[1];
            var pointConfig = deviceConfig.PointConfigList.FirstOrDefault(pc => pc.ClassName == _myDevice.GetType().Name && pc.PropertyName == "Temperature");
            if (pointConfig != null)
            {
                if (decimal.TryParse(tb_temperature.Text, out decimal result))
                {
                    _myDevice.Temperature = (int)(result / pointConfig.Scale);
                }
            }
        }

        private void btn_humidity_update_Click(object sender, EventArgs e)
        {
            var deviceConfig = _myModbusContext.DeviceConfigDictionary[1];
            var pointConfig = deviceConfig.PointConfigList.FirstOrDefault(pc => pc.ClassName == _myDevice.GetType().Name && pc.PropertyName == "Humidity");
            if (pointConfig != null)
            {
                if (decimal.TryParse(tb_humidity.Text, out decimal result))
                {
                    _myDevice.Humidity = (int)(result / pointConfig.Scale);
                }
            }
        }

        private async void _myDevice_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var myDevice = (MyDevice)sender;
            var propertyValue = myDevice.GetType().GetProperty(e.PropertyName).GetValue(myDevice);
            var deviceConfig = _myModbusContext.DeviceConfigDictionary[1];
            var pointConfig = deviceConfig.PointConfigList.FirstOrDefault(pc => pc.ClassName == nameof(MyDevice) && pc.PropertyName == e.PropertyName);
            if (pointConfig != null)
            {
                await _myModbusContext.SetValueAsync(deviceConfig, pointConfig, propertyValue);
            }
        }

        private void btn_whether_to_collect_Click(object sender, EventArgs e)
        {
            if (btn_whether_to_collect.Text == "开启采集")
            {
                _myModbusContext.WhetherToCollect(true);
                btn_whether_to_collect.Text = "暂停采集";
                //btn_whether_to_collect.Enabled = true;
            }
            else
            {
                _myModbusContext.WhetherToCollect(false);
                btn_whether_to_collect.Text = "开启采集";
                //btn_whether_to_collect.Enabled = true;
            }
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (!_isAllDoneBeforeFormClosing)
            {
                try
                {
                    btn_connect.Enabled = false;
                    WhetherToShowButtons(false);
                    // 取消关闭
                    e.Cancel = true;

                    //必须注销写日志事件的回调,否则_collectTask,_sendTask,_receiveTask,_heartbeatTask就算都变成RanToCompletion状态,但是他们在RanToCompletion状态之前调用Log事件,会驻留在底层事件队列中,就算Form1进行了Dispose,当事件从底层事件队列中取出去调用对应的回调方法时,在回调方法内部实际已经无法访问Form1对象了.
                    _myLogger.Log -= _myLogger_Log;

                    // _myModbusContext执行DisconnectAsync
                    await _myModbusContext.DisconnectAsync();

                    // 取消_cancellationTokenSource
                    if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();
                    }

                    await Task.WhenAll(_persistTask);

                    // 标记为所有任务都处理完
                    _isAllDoneBeforeFormClosing = true;

                    //这个方法会再次触发Form1_FormClosing
                    this.Close();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {

                }
            }
            else
            {
                Console.WriteLine("正式关闭form");
            }
        }
    }
}
