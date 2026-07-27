using System.ComponentModel;

namespace MyModbus.Common
{
    public class MyPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public MyPropertyChangedEventArgs(string propertyName) : base(propertyName)
        {
        }

        public object value { get; set; }
    }
}
