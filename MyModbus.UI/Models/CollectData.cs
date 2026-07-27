using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus.UI.Models
{
    public class CollectData
    {
        public int Id { get; set; }
        public string Context { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public long CollectedAt { get; set; }
    }
}
