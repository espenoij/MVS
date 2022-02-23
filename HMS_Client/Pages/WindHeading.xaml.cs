﻿using System;
using System.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for WindHeading.xaml
    /// </summary>
    public partial class WindHeading : UserControl
    {
        private WindHeadingVM windHeadingVM;
        private UserInputsVM userInputsVM;

        public WindHeading()
        {
            InitializeComponent();
        }

        public void Init(WindHeadingVM windHeadingVM, UserInputsVM userInputsVM, AdminSettingsVM adminSettingsVM)
        {
            this.windHeadingVM = windHeadingVM;
            this.userInputsVM = userInputsVM;
            DataContext = windHeadingVM;

            // Compass init
            ucCompass.Init(windHeadingVM);

            // Wind & Heading readouts init
            ucReadouts_CAP.Init(windHeadingVM);
            ucReadouts_NOROG.Init(windHeadingVM);

            // Wind Measurement - NOROG
            if (adminSettingsVM.regulationStandard == RegulationStandard.NOROG)
            {
                foreach (WindMeasurement value in Enum.GetValues(typeof(WindMeasurement)))
                    cboWindMeasurement.Items.Add(value.GetDescription());
            }
            // Wind Measurement - CAP
            else
            {
                cboWindMeasurement.Items.Add(WindMeasurement.TwoMinuteMean.GetDescription());
                cboWindMeasurement.Items.Add(WindMeasurement.TenMinuteMean.GetDescription());
            }

            cboWindMeasurement.SelectedIndex = (int)windHeadingVM.windMeasurement;
        }

        private void cboWindMeasurement_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            windHeadingVM.windMeasurement = (WindMeasurement)cboWindMeasurement.SelectedIndex;
        }

        public void SetDefaultWindMeasurement()
        {
            // Sette Wind & Heading til å vise: 
            if (userInputsVM.displayMode == DisplayMode.OnDeck)
            {
                // 2 - min mean vind ved on-deck
                windHeadingVM.windMeasurement = WindMeasurement.TwoMinuteMean;
            }
            else
            {
                // 10 - min mean vind ved pre-landing
                windHeadingVM.windMeasurement = WindMeasurement.TenMinuteMean;
                cboWindMeasurement.SelectedIndex = (int)windHeadingVM.windMeasurement;
            }
        }
    }
}
