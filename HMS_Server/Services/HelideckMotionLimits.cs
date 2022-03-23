using System;

namespace HMS_Server
{
    public class HelideckMotionLimits
    {
        public RegulationStandard regulationStandard;
        private UserInputs userInputs;
        private AdminSettingsVM adminSettingsVM;

        public HelideckMotionLimits(UserInputs userInputs, Config config, AdminSettingsVM adminSettingsVM)
        {
            this.userInputs = userInputs;
            this.adminSettingsVM = adminSettingsVM;

            regulationStandard = (RegulationStandard)Enum.Parse(typeof(RegulationStandard), config.ReadWithDefault(ConfigKey.RegulationStandard, RegulationStandard.NOROG.ToString()));
        }

        public double GetLimit(LimitType limitType)
        {
            double limit = 0;

            //////////////////////////////////////////////////////////////////////
            // NOROG
            //////////////////////////////////////////////////////////////////////
            if (regulationStandard == RegulationStandard.NOROG)
            {
                switch (limitType)
                {
                    case LimitType.PitchRoll:

                        switch (adminSettingsVM.helideckCategory)
                        {
                            case HelideckCategory.Category1:
                            case HelideckCategory.Category1_Semisub:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3;
                                                break;
                                            case DayNight.Night:
                                                if (adminSettingsVM.helideckCategory == HelideckCategory.Category1_Semisub)
                                                    limit = 3;
                                                else
                                                    limit = 2;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 4;
                                                break;
                                            case DayNight.Night:
                                                limit = 3;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category2:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 2;
                                                break;
                                            case DayNight.Night:
                                                limit = 2;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3;
                                                break;
                                            case DayNight.Night:
                                                limit = 2;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category3:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 2;
                                                break;
                                            case DayNight.Night:
                                                limit = 1;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3;
                                                break;
                                            case DayNight.Night:
                                                limit = 1.5;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;

                    case LimitType.Inclination:

                        switch (adminSettingsVM.helideckCategory)
                        {
                            case HelideckCategory.Category1:
                            case HelideckCategory.Category1_Semisub:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3.5;
                                                break;
                                            case DayNight.Night:
                                                if (adminSettingsVM.helideckCategory == HelideckCategory.Category1_Semisub)
                                                    limit = 3.5;
                                                else
                                                    limit = 2.5;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 4.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 3.5;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category2:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 2.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 2.5;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 2.5;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category3:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 2.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 1.5;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 2;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;

                    case LimitType.SignificantHeaveRate:

                        switch (adminSettingsVM.helideckCategory)
                        {
                            case HelideckCategory.Category1:
                            case HelideckCategory.Category1_Semisub:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:
                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 1.3;
                                                break;
                                            case DayNight.Night:
                                                limit = 1;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category2:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:
                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 1;
                                                break;
                                            case DayNight.Night:
                                                limit = 0.5;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category3:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:
                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 1;
                                                break;
                                            case DayNight.Night:
                                                limit = 0.5;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;

                    case LimitType.HeaveAmplitude:

                        switch (adminSettingsVM.helideckCategory)
                        {
                            case HelideckCategory.Category1:
                            case HelideckCategory.Category1_Semisub:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:
                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 5;
                                                break;
                                            case DayNight.Night:
                                                limit = 4;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category2:
                            case HelideckCategory.Category3:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:
                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3;
                                                break;
                                            case DayNight.Night:
                                                limit = 1.5;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;
                }
            }
            //////////////////////////////////////////////////////////////////////
            // CAP
            //////////////////////////////////////////////////////////////////////
            else
            if (regulationStandard == RegulationStandard.CAP)
            {
                switch (limitType)
                {
                    case LimitType.PitchRoll:

                        switch (userInputs.helideckCategory)
                        {
                            case HelideckCategory.Category1:
                            case HelideckCategory.Category1_Semisub:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3;
                                                break;
                                            case DayNight.Night:
                                                if (adminSettingsVM.helideckCategory == HelideckCategory.Category1_Semisub)
                                                    limit = 3;
                                                else
                                                    limit = 2;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 4;
                                                break;
                                            case DayNight.Night:
                                                if (adminSettingsVM.helideckCategory == HelideckCategory.Category1_Semisub)
                                                    limit = 4;
                                                else
                                                    limit = 3;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category2:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 2;
                                                break;
                                            case DayNight.Night:
                                                limit = 2;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3;
                                                break;
                                            case DayNight.Night:
                                                limit = 2;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category3:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 2;
                                                break;
                                            case DayNight.Night:
                                                limit = 1;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3;
                                                break;
                                            case DayNight.Night:
                                                limit = 1.5;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;

                    case LimitType.Inclination:

                        switch (userInputs.helideckCategory)
                        {
                            case HelideckCategory.Category1:
                            case HelideckCategory.Category1_Semisub:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3.5;
                                                break;
                                            case DayNight.Night:
                                                if (adminSettingsVM.helideckCategory == HelideckCategory.Category1_Semisub)
                                                    limit = 3.5;
                                                else
                                                    limit = 2.5;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 4.5;
                                                break;
                                            case DayNight.Night:
                                                if (adminSettingsVM.helideckCategory == HelideckCategory.Category1_Semisub)
                                                    limit = 4.5;
                                                else
                                                    limit = 3.5;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category2:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 2.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 2.5;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 2.5;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category3:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 2.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 1.5;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 3.5;
                                                break;
                                            case DayNight.Night:
                                                limit = 2;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;

                    case LimitType.SignificantHeaveRate:

                        switch (userInputs.helideckCategory)
                        {
                            case HelideckCategory.Category1:
                            case HelideckCategory.Category1_Semisub:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 1.3;
                                                break;
                                            case DayNight.Night:
                                                limit = 1;
                                                break;
                                        }
                                        break;

                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 1.3;
                                                break;
                                            case DayNight.Night:
                                                if (adminSettingsVM.helideckCategory == HelideckCategory.Category1_Semisub)
                                                    limit = 1.3;
                                                else
                                                    limit = 1;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category2:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:
                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 1;
                                                break;
                                            case DayNight.Night:
                                                limit = 0.5;
                                                break;
                                        }
                                        break;
                                }
                                break;

                            case HelideckCategory.Category3:

                                switch (userInputs.helicopterCategory)
                                {
                                    case HelicopterCategory.Heavy:
                                    case HelicopterCategory.Medium:

                                        switch (userInputs.dayNight)
                                        {
                                            case DayNight.Day:
                                                limit = 1;
                                                break;
                                            case DayNight.Night:
                                                limit = 0.5;
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;

                    case LimitType.HeaveAmplitude:
                        // Ikke i bruk under CAP
                        break;
                }
            }

            return limit;
        }
    }
}
