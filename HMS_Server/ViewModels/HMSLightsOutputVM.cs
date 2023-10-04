﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace HMS_Server
{
    public class HMSLightsOutputVM : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer statusUpdateTimer = new DispatcherTimer();

        private UserInputs userInputs;
        private Config config;
        private AdminSettingsVM adminSettingsVM;

        // Test Mode Auto
        private int testModeAutoStep = 0;
        private DispatcherTimer testModeAutoTimer = new DispatcherTimer();

        public void Init(HMSDataCollection hmsOutputDataList, Config config, UserInputs userInputs, AdminSettingsVM adminSettingsVM)
        {
            this.userInputs = userInputs;
            this.config = config;
            this.adminSettingsVM = adminSettingsVM;

            testModeManual = false;
            testModeStatus = HelideckStatusType.OFF;
            testModeDisplayMode = DisplayMode.PreLanding;
            testModeStepDuration = config.ReadWithDefault(ConfigKey.TestModeStepDuration, Constants.TestModeStepDurationDefault);

            // Lese output adresser fra fil
            for (int i = 0; i < 3; i++)
                _outputAddressList.Add(new UInt16());

            outputAddress1 = (UInt16)config.ReadWithDefault(ConfigKey.LightsOutputAddress1, Constants.ModbusCoilMin);
            outputAddress2 = (UInt16)config.ReadWithDefault(ConfigKey.LightsOutputAddress2, Constants.ModbusCoilMin);
            outputAddress3 = (UInt16)config.ReadWithDefault(ConfigKey.LightsOutputAddress3, Constants.ModbusCoilMin);

            // Oppdatere UI
            statusUpdateTimer.Interval = TimeSpan.FromMilliseconds(config.ReadWithDefault(ConfigKey.ServerUIUpdateFrequency, Constants.ServerUIUpdateFrequencyDefault));
            statusUpdateTimer.Tick += UIUpdate;
            statusUpdateTimer.Start();

            void UIUpdate(object sender, EventArgs e)
            {
                helideckLightData = hmsOutputDataList?.GetData(ValueType.HelideckLight);

                OnPropertyChanged(nameof(helideckStatus));
                OnPropertyChanged(nameof(HMSLightsOutput));
            }

            // Oppdatere lys i display
            DispatcherTimer lightDisplayTimer = new DispatcherTimer();
            lightDisplayTimer.Interval = TimeSpan.FromMilliseconds(200);
            lightDisplayTimer.Tick += LightUpdate;
            lightDisplayTimer.Start();

            void LightUpdate(object sender, EventArgs e)
            {
                OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
            }

            // Test Mode Auto
            testModeAutoTimer.Tick += TestModeAutoUpdate;

            void TestModeAutoUpdate(object sender, EventArgs e)
            {
                testModeAutoStep++;

                // CAP
                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                {
                    if (testModeAutoStep > 6)
                        testModeAutoStep = 0;
                }
                // NOROG
                else
                {
                    if (testModeAutoStep > 2)
                        testModeAutoStep = 0;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Test Mode Auto
        /////////////////////////////////////////////////////////////////////////////
        public void ToggleTestModeAuto()
        {
            if (testModeAuto)
            {
                if (!testModeAutoTimer.IsEnabled)
                {
                    testModeAutoStep = 0;

                    testModeAutoTimer.Interval = TimeSpan.FromSeconds(testModeStepDuration);
                    testModeAutoTimer.Start();
                }
            }
            else
            {
                if (testModeAutoTimer.IsEnabled)
                    testModeAutoTimer.Stop();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Light Status
        /////////////////////////////////////////////////////////////////////////////
        private HMSData _helideckLightData { get; set; } = new HMSData();
        public HMSData helideckLightData
        {
            get
            {
                return _helideckLightData;
            }
            set
            {
                if (value != null)
                {
                    _helideckLightData.Set(value);

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(helideckStatus));
                    OnPropertyChanged(nameof(HMSLightsOutput));
                    OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
                }
            }
        }

        public HelideckStatusType helideckStatus
        {
            get
            {
                HelideckStatusType status;

                if (_helideckLightData.status == DataStatus.OK)
                    status = (HelideckStatusType)_helideckLightData.data;
                else
                    // Dersom vi har data timeout skal lyset slåes av
                    status = HelideckStatusType.OFF;

                return status;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Lights Output
        /////////////////////////////////////////////////////////////////////////////
        public LightsOutputType HMSLightsOutput
        {
            get
            {
                LightsOutputType lightsOutput = LightsOutputType.Off;

                // Test mode: Manual
                if (testModeManual)
                {
                    if (testModeDisplayMode == DisplayMode.PreLanding)
                    {
                        switch (testModeStatus)
                        {
                            case HelideckStatusType.OFF:
                                lightsOutput = LightsOutputType.Off;
                                break;
                            case HelideckStatusType.BLUE:
                                lightsOutput = LightsOutputType.Blue;
                                break;
                            case HelideckStatusType.AMBER:
                                lightsOutput = LightsOutputType.Amber;
                                break;
                            case HelideckStatusType.RED:
                                lightsOutput = LightsOutputType.Red;
                                break;
                            case HelideckStatusType.GREEN:
                                lightsOutput = LightsOutputType.Green;
                                break;
                            default:
                                lightsOutput = LightsOutputType.Off;
                                break;
                        }
                    }
                    else
                    {
                        switch (testModeStatus)
                        {
                            case HelideckStatusType.OFF:
                                lightsOutput = LightsOutputType.Off;
                                break;
                            case HelideckStatusType.BLUE:
                                lightsOutput = LightsOutputType.BlueFlash;
                                break;
                            case HelideckStatusType.AMBER:
                                lightsOutput = LightsOutputType.AmberFlash;
                                break;
                            case HelideckStatusType.RED:
                                lightsOutput = LightsOutputType.RedFlash;
                                break;
                            case HelideckStatusType.GREEN:
                                lightsOutput = LightsOutputType.GreenFlash;
                                break;
                            default:
                                lightsOutput = LightsOutputType.Off;
                                break;
                        }
                    }
                }
                else
                // Test Mode: Automatic
                if (testModeAuto)
                {
                    // CAP
                    if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                    {
                        switch (testModeAutoStep)
                        {
                            case 0:
                                lightsOutput = LightsOutputType.Red;
                                break;
                            case 1:
                                lightsOutput = LightsOutputType.Amber;
                                break;
                            case 2:
                                lightsOutput = LightsOutputType.Blue;
                                break;
                            case 3:
                                lightsOutput = LightsOutputType.RedFlash;
                                break;
                            case 4:
                                lightsOutput = LightsOutputType.AmberFlash;
                                break;
                            case 5:
                                lightsOutput = LightsOutputType.BlueFlash;
                                break;
                            case 6:
                                lightsOutput = LightsOutputType.Off;
                                break;
                            default:
                                lightsOutput = LightsOutputType.Off;
                                break;
                        }
                    }
                    // NOROG
                    else
                    {
                        switch (testModeAutoStep)
                        {
                            case 0:
                                lightsOutput = LightsOutputType.Red;
                                break;
                            case 1:
                                lightsOutput = LightsOutputType.Green;
                                break;
                            case 2:
                                lightsOutput = LightsOutputType.Off;
                                break;
                            default:
                                lightsOutput = LightsOutputType.Off;
                                break;
                        }
                    }
                }
                // Normal operations
                else
                {
                    if (userInputs.displayMode == DisplayMode.PreLanding)
                    {
                        switch (helideckStatus)
                        {
                            case HelideckStatusType.OFF:
                                lightsOutput = LightsOutputType.Off;
                                break;
                            case HelideckStatusType.BLUE:
                                lightsOutput = LightsOutputType.Blue;
                                break;
                            case HelideckStatusType.AMBER:
                                lightsOutput = LightsOutputType.Amber;
                                break;
                            case HelideckStatusType.RED:
                                lightsOutput = LightsOutputType.Red;
                                break;
                            case HelideckStatusType.GREEN:
                                lightsOutput = LightsOutputType.Green;
                                break;
                            default:
                                lightsOutput = LightsOutputType.Off;
                                break;
                        }
                    }
                    else
                    {
                        switch (helideckStatus)
                        {
                            case HelideckStatusType.OFF:
                                lightsOutput = LightsOutputType.Off;
                                break;
                            case HelideckStatusType.BLUE:
                                lightsOutput = LightsOutputType.BlueFlash;
                                break;
                            case HelideckStatusType.AMBER:
                                lightsOutput = LightsOutputType.AmberFlash;
                                break;
                            case HelideckStatusType.RED:
                                lightsOutput = LightsOutputType.RedFlash;
                                break;
                            case HelideckStatusType.GREEN:
                                lightsOutput = LightsOutputType.GreenFlash;
                                break;
                            default:
                                lightsOutput = LightsOutputType.Off;
                                break;
                        }
                    }
                }

                return lightsOutput;
            }
        }

        public HelideckStatusType HMSLightsOutputToDisplay
        {
            // Her legges blinking på

            get
            {
                HelideckStatusType status = HelideckStatusType.OFF;

                if (adminSettingsVM.regulationStandard == RegulationStandard.CAP)
                {
                    switch (HMSLightsOutput)
                    {
                        case LightsOutputType.Off:
                            status = HelideckStatusType.OFF;
                            break;

                        case LightsOutputType.Blue:
                            status = HelideckStatusType.BLUE;
                            break;

                        case LightsOutputType.BlueFlash:
                            if ((int)DateTime.Now.Second % 2 == 0) // 30 fpm
                                status = HelideckStatusType.OFF;
                            else
                                status = HelideckStatusType.BLUE;
                            break;

                        case LightsOutputType.Amber:
                            status = HelideckStatusType.AMBER;
                            break;

                        case LightsOutputType.AmberFlash:
                            if (DateTime.Now.Millisecond < 500) // 60 fpm
                                status = HelideckStatusType.OFF;
                            else
                                status = HelideckStatusType.AMBER;
                            break;

                        case LightsOutputType.Red:
                            status = HelideckStatusType.RED;
                            break;

                        case LightsOutputType.RedFlash:
                            if (DateTime.Now.Millisecond < 500) // 60 fpm
                                status = HelideckStatusType.OFF;
                            else
                                status = HelideckStatusType.RED;
                            break;

                        case LightsOutputType.Green:
                            status = HelideckStatusType.GREEN;
                            break;

                        case LightsOutputType.GreenFlash:
                            if ((int)DateTime.Now.Second % 2 == 0) // 30 fpm
                                status = HelideckStatusType.OFF;
                            else
                                status = HelideckStatusType.GREEN;
                            break;
                    }
                }

                return status;
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Helideck Lights Output Test Mode
        /////////////////////////////////////////////////////////////////////////////
        private bool _testModeManual { get; set; }
        public bool testModeManual
        {
            get
            {
                return _testModeManual;
            }
            set
            {
                _testModeManual = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HMSLightsOutput));
                OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
            }
        }

        private HelideckStatusType _testModeStatus { get; set; }
        public HelideckStatusType testModeStatus
        {
            get
            {
                return _testModeStatus;
            }
            set
            {
                _testModeStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HMSLightsOutput));
                OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
            }
        }

        private DisplayMode _testModeDisplayMode { get; set; }
        public DisplayMode testModeDisplayMode
        {
            get
            {
                return _testModeDisplayMode;
            }
            set
            {
                _testModeDisplayMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HMSLightsOutput));
                OnPropertyChanged(nameof(HMSLightsOutputToDisplay));
            }
        }

        private bool _testModeAuto { get; set; }
        public bool testModeAuto
        {
            get
            {
                return _testModeAuto;
            }
            set
            {
                _testModeAuto = value;
                OnPropertyChanged();
            }
        }

        private double _testModeStepDuration { get; set; }
        public double testModeStepDuration
        {
            get
            {
                return _testModeStepDuration;
            }
            set
            {
                _testModeStepDuration = value;
                config.Write(ConfigKey.TestModeStepDuration, value.ToString());
                OnPropertyChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // MODBUS Addresses
        /////////////////////////////////////////////////////////////////////////////
        private List<UInt16> _outputAddressList { get; set; } = new List<ushort>();
        public List<UInt16> outputAddressList
        {
            get
            {
                return _outputAddressList;
            }
        }

        public UInt16 outputAddress1
        {
            get
            {
                return _outputAddressList[0];
            }
            set
            {
                _outputAddressList[0] = value;
                config.Write(ConfigKey.LightsOutputAddress1, value.ToString());
                OnPropertyChanged();
            }
        }

        public UInt16 outputAddress2
        {
            get
            {
                return _outputAddressList[1];
            }
            set
            {
                _outputAddressList[1] = value;
                config.Write(ConfigKey.LightsOutputAddress2, value.ToString());
                OnPropertyChanged();
            }
        }

        public UInt16 outputAddress3
        {
            get
            {
                return _outputAddressList[2];
            }
            set
            {
                _outputAddressList[2] = value;
                config.Write(ConfigKey.LightsOutputAddress3, value.ToString());
                OnPropertyChanged();
            }
        }


        // Variabel oppdatert
        // Dersom navn ikke er satt brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
