using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.Entities
{
    public class ItemAspectDescriptor
    {
        public double Similarity { get; set; } = 1;
        public Rectangle Location { get; set; } = Rectangle.Empty;
    }
}
