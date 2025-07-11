using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epic7SecretShopAutoBuyer.Models
{
    public class BestMatchLocation
    {
        public double MinVal { get; set; }
        public double MaxVal { get; set; }
        public OpenCvSharp.Point MinLoc { get; set; }
        public OpenCvSharp.Point MaxLoc { get; set; }
    }
}
