using System;

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
            double rollRad = (Math.PI / 180.0) * roll;
            double pitchRad = (Math.PI / 180.0) * pitch;

            // Beregne inclination
            double inclinationRad = Math.Atan(Math.Sqrt(Math.Pow(Math.Tan(rollRad), 2.0) + Math.Pow(Math.Tan(pitchRad), 2.0)));

            // Konvertere tilbake til grader
            return (180.0 / Math.PI) * inclinationRad;
        }
    }
}
