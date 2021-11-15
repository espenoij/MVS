using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMS_Server
{
    public class CalculationSetup
    {
        public CalculationType type { get; set; }
        public double parameter { get; set; }

        public CalculationSetup()
        {
            type = CalculationType.None;
            parameter = 0;
        }

        public CalculationSetup(CalculationSetup cs)
        {
            type = cs.type;
            parameter = cs.parameter;
        }
    }
}
