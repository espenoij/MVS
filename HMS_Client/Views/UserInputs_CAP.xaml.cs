﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for UserInputs_CAP.xaml
    /// </summary>
    public partial class UserInputs_CAP : UserControl
    {
        // Configuration settings
        private Config config;

        // User Inputs View Model
        private UserInputsVM userInputsVM;
        private OnDeckStabilityLimitsVM helideckStabilityLimitsVM;
        private LandingStatusTrendVM landingStatusTrendVM;

        public UserInputs_CAP()
        {
            InitializeComponent();
        }

        public void Init(UserInputsVM userInputsVM, Config config, AdminSettingsVM adminSettingsVM, OnDeckStabilityLimitsVM helideckStabilityLimitsVM, LandingStatusTrendVM landingStatusTrendVM)
        {
            DataContext = userInputsVM;

            this.config = config;
            this.userInputsVM = userInputsVM;
            this.helideckStabilityLimitsVM = helideckStabilityLimitsVM;
            this.landingStatusTrendVM = landingStatusTrendVM;

            // Helicopter Type
            foreach (HelicopterType value in Enum.GetValues(typeof(HelicopterType)))
                cboHelicopterType.Items.Add(value.GetDescription());
            cboHelicopterType.SelectedIndex = (int)userInputsVM.helicopterType;

            // Helideck Category
            switch (adminSettingsVM.helideckCategory)
            {
                case HelideckCategory.Category1:
                    cboHelideckCategory.Items.Add(HelideckCategory.Category1.GetDescription());
                    break;
                case HelideckCategory.Category1_Semisub:
                    cboHelideckCategory.Items.Add(HelideckCategory.Category1_Semisub.GetDescription());
                    break;
                case HelideckCategory.Category2:
                    cboHelideckCategory.Items.Add(HelideckCategory.Category2.GetDescription());
                    break;
                case HelideckCategory.Category2_or_3:
                    cboHelideckCategory.Items.Add(HelideckCategory.Category2.GetDescription());
                    cboHelideckCategory.Items.Add(HelideckCategory.Category3.GetDescription());
                    break;
                default:
                    cboHelideckCategory.Items.Add(HelideckCategory.Category1.GetDescription());
                    break;
            }

            switch ((HelideckCategory)Enum.Parse(typeof(HelideckCategory), config.ReadWithDefault(ConfigKey.HelideckCategory, HelideckCategory.Category1.ToString())))
            {
                case HelideckCategory.Category1:
                    cboHelideckCategory.SelectedIndex = 0;
                    break;
                case HelideckCategory.Category1_Semisub:
                    cboHelideckCategory.SelectedIndex = 1;
                    break;
                case HelideckCategory.Category2:
                    cboHelideckCategory.SelectedIndex = 2;
                    break;
                case HelideckCategory.Category3:
                    cboHelideckCategory.SelectedIndex = 3;
                    break;
                default:
                    cboHelideckCategory.SelectedIndex = 0;
                    break;
            }

            // Day / Night
            foreach (DayNight value in Enum.GetValues(typeof(DayNight)))
                cboDayNight.Items.Add(value.ToString());
            cboDayNight.SelectedIndex = (int)userInputsVM.dayNight;

            if (userInputsVM.dayNight.ToString().CompareTo("Day") == 0)
            {
                imgDayNightIcon.Source = new BitmapImage(new Uri("../Icons/outline_wb_sunny_black_48dp.png", UriKind.Relative));
            }
            else
            {
                imgDayNightIcon.Source = new BitmapImage(new Uri("../Icons/outline_nightlight_black_48dp.png", UriKind.Relative));
            }

            //// Is Semi Submersible
            //if (config.Read(ConfigKey.VesselSemiSubmersible).CompareTo("1") == 0)
            //    viewModel.isSemiSubmersible = true;
            //else
            //    viewModel.isSemiSubmersible = false;

            // Input fra bruker tillates ikke i Observer mode
            if (!adminSettingsVM.clientIsMaster)
            {
                // Pre-landing
                cboHelicopterType.IsEnabled = false;
                cboHelideckCategory.IsEnabled = false;
                cboDayNight.IsEnabled = false;
                tbHelicopterHeading.IsEnabled = false;
                btnEnterHeading.IsEnabled = false;

                // On-deck
                tbCorrectedHelicopterHeading.IsEnabled = false;
                btnEnterCorrectedHeading.IsEnabled = false;
                btnHelicopterDeparted.IsEnabled = false;
            }
        }

        private void cboHelicopterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            userInputsVM.helicopterType = (HelicopterType)cboHelicopterType.SelectedIndex;
        }

        private void cboHelideckCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            userInputsVM.helideckCategory = EnumExtension.GetEnumValueFromDescription<HelideckCategory>(cboHelideckCategory.Text);
        }

        private void cboDayNight_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            userInputsVM.dayNight = (DayNight)cboDayNight.SelectedIndex;

            switch (userInputsVM.dayNight)
            {
                case DayNight.Day:
                    imgDayNightIcon.Source = new BitmapImage(new Uri("../Icons/outline_wb_sunny_black_48dp.png", UriKind.Relative));
                    break;

                case DayNight.Night:
                    imgDayNightIcon.Source = new BitmapImage(new Uri("../Icons/outline_nightlight_black_48dp.png", UriKind.Relative));
                    break;
            }
        }

        private void tbHelicopterHeading_LostFocus(object sender, RoutedEventArgs e)
        {
            tbHelicopterHeading_Update(sender);
        }

        private void tbHelicopterHeading_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbHelicopterHeading_Update(sender);
                Keyboard.ClearFocus();
                EnterHeading();
            }
        }

        private void tbHelicopterHeading_Update(object sender)
        {
            // Sjekk av input
            DataValidation.String(
                (sender as TextBox).Text,
                Constants.HeadingMin,
                Constants.HeadingMax,
                out string validatedInput);

            tbHelicopterHeading.Text = validatedInput;
        }

        private void btnEnterHeading_Click(object sender, RoutedEventArgs e)
        {
            EnterHeading();
        }

        private void EnterHeading()
        {
            if (!string.IsNullOrEmpty(tbHelicopterHeading.Text))
            {
                // Åpne dialog vindu
                DialogHelicopterLanded dialog = new DialogHelicopterLanded(userInputsVM, helideckStabilityLimitsVM, landingStatusTrendVM);
                dialog.Owner = App.Current.MainWindow;
                dialog.Heading(Convert.ToDouble(tbHelicopterHeading.Text)); // Data er allerede validert i metoden ovenfor
                dialog.ShowDialog();
            }
        }

        private void tbCorrectedHelicopterHeading_LostFocus(object sender, RoutedEventArgs e)
        {
            tbCorrectedHelicopterHeading_Update(sender);
        }

        private void tbCorrectedHelicopterHeading_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                tbCorrectedHelicopterHeading_Update(sender);
                Keyboard.ClearFocus();
                EnterCorrectedHeading();
            }
        }

        private void tbCorrectedHelicopterHeading_Update(object sender)
        {
            // Sjekk av input
            DataValidation.String(
                (sender as TextBox).Text,
                Constants.HeadingMin,
                Constants.HeadingMax,
                out string validatedInput);

            tbCorrectedHelicopterHeading.Text = validatedInput;
        }

        private void btn_EnterCorrectedHeading_Click(object sender, RoutedEventArgs e)
        {
            EnterCorrectedHeading();
        }

        private void EnterCorrectedHeading()
        {
            if (!string.IsNullOrEmpty(tbCorrectedHelicopterHeading.Text))
            {
                try
                {
                    // Overføre heading verdi
                    userInputsVM.onDeckHelicopterHeading = Convert.ToDouble(tbCorrectedHelicopterHeading.Text); // Data er allerede validert i metoden ovenfor

                    // Fjerne data fra input feltet
                    tbCorrectedHelicopterHeading.Text = string.Empty;
                }
                catch (Exception)
                {
                    DialogHandler.Warning("Input Error", string.Format("Valid heading range: {0} - {1}",
                        Constants.HeadingMin,
                        Constants.HeadingMax));
                }
            }
        }

        private void btn_HelicopterDeparted_Click(object sender, RoutedEventArgs e)
        {
            // Åpne dialog vindu
            DialogHelicopterDeparted dialog = new DialogHelicopterDeparted(userInputsVM, tbHelicopterHeading);
            dialog.Owner = App.Current.MainWindow;
            dialog.ShowDialog();
        }
    }
}