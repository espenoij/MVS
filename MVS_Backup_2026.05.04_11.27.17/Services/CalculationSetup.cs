namespace MVS
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
