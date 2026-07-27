using MyModbus.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyModbus.UI.Models
{
    public class MyModel : INotifyPropertyChanged
    {
        SynchronizationContext _uiContext;
        public event PropertyChangedEventHandler PropertyChanged;

        public MyModel(SynchronizationContext uiContext)
        {
            _uiContext = uiContext;
        }

        protected virtual void OnPropertyChanged(string name, object value)
        {
            var propertyChangedEventArgs = new PropertyChangedEventArgs(name);

            if (SynchronizationContext.Current == _uiContext)
            {
                // ui线程中使用set方法
                PropertyChanged?.Invoke(this, propertyChangedEventArgs);
            }
            else
            {
                // 非ui线程,即其他线程中无法操作model的set方法，必须SynchronizationCOntext.Post或Send消息
                _uiContext.Post(state =>
                {
                    PropertyChanged?.Invoke(this, propertyChangedEventArgs);
                }, null);
            }
        }

        public void SetSilent<T>(string fieldName, T value)
        {
            var fieldInfo = this.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo?.SetValue(this, value);
        }
    }
}
