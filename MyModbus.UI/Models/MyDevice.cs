using MyModbus.Common;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace MyModbus.UI.Models
{
    public class MyDevice : MyModel
    {
        int _temperature = 0;
        int _humidity = 0;

        public MyDevice(SynchronizationContext uiContext) : base(uiContext)
        {

        }

        public int Temperature
        {
            get
            {
                return _temperature;
            }
            set
            {
                if (_temperature == value)
                {
                    return;
                }

                _temperature = value;

                OnPropertyChanged(nameof(Temperature), value);
            }
        }

        public int Humidity
        {
            get
            {
                return _humidity;
            }
            set
            {
                if (_humidity == value)
                {
                    return;
                }

                _humidity = value;

                OnPropertyChanged(nameof(Humidity), value);
            }
        }
    }
}
