using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class ItemTypeDescriptor
    {
        public double Accuracy { get; set; } = 1;
        public Rectangle Location { get; set; } = Rectangle.Empty;
        public string Name { get; set; } = string.Empty;
        
    }
}
