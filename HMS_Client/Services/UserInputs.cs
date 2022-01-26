using System;

public class UserInputs
{
    public HelicopterType helicopterType { get; set; }
    public HelideckCategory helideckCategory { get; set; }
    public DayNight dayNight { get; set; }
    public DisplayMode displayMode { get; set; }

    // On-Deck variabler
    public DateTime onDeckTime { get; set; }
    public double onDeckHelicopterHeading { get; set; }
    public bool onDeckHelicopterHeadingIsCorrected { get; set; }
    public double onDeckVesselHeading { get; set; }
    public double onDeckWindDirection { get; set; }

    public UserInputs()
    {
    }

    public void Set(UserInputs userInputs)
    {
        if (userInputs != null)
        {
            helicopterType = userInputs.helicopterType;
            helideckCategory = userInputs.helideckCategory;
            dayNight = userInputs.dayNight;
            displayMode = userInputs.displayMode;

            onDeckTime = userInputs.onDeckTime;
            onDeckHelicopterHeading = userInputs.onDeckHelicopterHeading;
            onDeckHelicopterHeadingIsCorrected = userInputs.onDeckHelicopterHeadingIsCorrected;
            onDeckVesselHeading = userInputs.onDeckVesselHeading;
            onDeckWindDirection = userInputs.onDeckWindDirection;
        }
    }

    // Helicopter Category
    public HelicopterCategory helicopterCategory
    {
        get
        {
            switch (helicopterType)
            {
                // Heavy / A
                case HelicopterType.AS332:
                case HelicopterType.EC225:
                case HelicopterType.AW189:
                case HelicopterType.S61:
                case HelicopterType.S92:
                    return HelicopterCategory.Heavy;

                // Medium / B
                case HelicopterType.AS365:
                case HelicopterType.EC135:
                case HelicopterType.EC155:
                case HelicopterType.EC175:
                case HelicopterType.AW139:
                case HelicopterType.AW169:
                case HelicopterType.S76:
                case HelicopterType.B212:
                case HelicopterType.B412:
                case HelicopterType.B525:
                case HelicopterType.H145:
                case HelicopterType.H175:
                    return HelicopterCategory.Medium;

                default:
                    return HelicopterCategory.Heavy;
            }
        }
    }
}

