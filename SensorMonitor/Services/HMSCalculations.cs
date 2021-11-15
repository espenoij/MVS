using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMS_Server
{
    public class HMSCalculations
    {
        public static double Inclination(double pitch, double roll)
        {
            // Formel: inclination = atan(sqrt(tan^2(roll)+tan^2(pitch)))
            // Beregner først tangenten til pitch og roll.
            // Deretter bruker vi pythagoras teorem til å finne tangenten til inclination.
            // Til slutt arctan for å finne vinkelen til inclination.

            // Konvertere input til radianer
            double rollRad = (Math.PI / 180) * roll;
            double pitchRad = (Math.PI / 180) * pitch;

            // Beregne inclination
            double inclinationRad = Math.Atan(Math.Sqrt(Math.Pow(Math.Tan(rollRad), 2) + Math.Pow(Math.Tan(pitchRad), 2)));

            // Konvertere tilbake til grader
            return (180 / Math.PI) * inclinationRad;
        }
    }
}
