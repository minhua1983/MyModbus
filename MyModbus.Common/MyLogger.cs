using NLog;
using System;
using System.Runtime.Remoting.Messaging;

namespace MyModbus.Common
{
    public class MyLogger
    {
        public static readonly ILogger _log = LogManager.GetLogger("*");
        public static readonly ILogger _alarmLog = LogManager.GetLogger("MyModbusAlarmLogger");

        public event EventHandler<MyEventArgs> Log;

        public void AddLog(LogLevel logType, string message, Exception e = default)
        {
            /*
            Action action = logType switch
            {
                LogLevel.Info => () => _log.Info(message),
                LogLevel.Warn => () => _log.Warn(message),
                LogLevel.Error => () => _log.Error(e, message),
                _ => () => _log.Debug(message)
            };
            action.Invoke();
            //*/

            switch (logType)
            {
                case LogLevel.Info:
                    _log.Info(message);
                    break;
                case LogLevel.Warn:
                    _log.Warn(message);
                    break;
                case LogLevel.Error:
                    _log.Error(e, message);
                    break;
                default:
                    _log.Debug(message);
                    break;
            }

            OnLog(new MyEventArgs((logType, message)));
        }

        public void AddAlarmLog(LogLevel logType, string message, Exception e = default)
        {
            /*
            Action action = logType switch
            {
                LogLevel.Info => () => _log.Info(message),
                LogLevel.Warn => () => _log.Warn(message),
                LogLevel.Error => () => _log.Error(e, message),
                _ => () => _log.Debug(message)
            };
            action.Invoke();
            //*/

            switch (logType)
            {
                case LogLevel.Info:
                    _alarmLog.Info(message);
                    break;
                case LogLevel.Warn:
                    _alarmLog.Warn(message);
                    break;
                case LogLevel.Error:
                    _alarmLog.Error(e, message);
                    break;
                default:
                    _alarmLog.Debug(message);
                    break;
            }

            OnLog(new MyEventArgs((logType, message)));
        }

        protected void OnLog(MyEventArgs myEventArgs)
        {
            Log?.Invoke(this, myEventArgs);
        }
    }

    public enum LogLevel
    {
        Info = 1,
        Warn = 2,
        Error = 3
    }
}
