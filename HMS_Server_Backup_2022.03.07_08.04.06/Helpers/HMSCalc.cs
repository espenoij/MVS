using System;

public static class HMSCalc
{
    public const double KnotsToMSFactor = 0.51444444;

    public static double ToRadians(this double val)
    {
        return (Math.PI / 180) * val;
    }

    public static double ToDegrees(this double val)
    {
        return (180 / Math.PI) * val;
    }

    public static double Inclination(double pitch, double roll)
    {
        // https://math.stackexchange.com/questions/2563622/vertical-inclination-from-pitch-and-roll
        // Formel: inclination = atan(sqrt(tan^2(roll)+tan^2(pitch)))
        // Beregner først tangenten til pitch og roll.
        // Deretter bruker vi pythagoras teorem til å finne tangenten til inclination.
        // Til slutt arctan for å finne vinkelen til inclination.

        // Konvertere input til radianer
        double rollRad = ToRadians(roll);
        double pitchRad = ToRadians(pitch);

        // Beregne inclination
        double inclinationRad = Math.Atan(Math.Sqrt(Math.Pow(Math.Tan(rollRad), 2.0) + Math.Pow(Math.Tan(pitchRad), 2.0)));

        // Konvertere tilbake til grader
        return ToDegrees(inclinationRad);
    }

    public static double KnotsToMS(double val)
    {
        return val * KnotsToMSFactor;
    }

    public static double MStoKnots(double val)
    {
        return val / KnotsToMSFactor;
    }
}
