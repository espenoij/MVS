using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    public class VerificationData : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        public VerificationData()
        {
        }

        public VerificationData(VerificationData verificationData)
        {
            Set(verificationData);
        }

        public void Set(VerificationData verificationData)
        {
            id = verificationData.id;
            name = verificationData.name;
            testData = verificationData.testData;
            refData = verificationData.refData;
            differenceAbs = verificationData.differenceAbs;
            differenceTimePctCounterTotal = verificationData.differenceTimePctCounterTotal;
            differenceTimePctCounterWrong = verificationData.differenceTimePctCounterWrong;
        }

        private VerificationType _id { get; set; }
        public VerificationType id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        private string _name { get; set; }
        public string name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private double _testData { get; set; }
        public double testData
        {
            get
            {
                return _testData;
            }
            set
            {
                if (!double.IsNaN(value))
                    _testData = value;
                else
                    _testData = 0;
                OnPropertyChanged();
            }
        }

        private double _refData { get; set; }
        public double refData
        {
            get
            {
                return _refData;
            }
            set
            {
                if (!double.IsNaN(value))
                    _refData = value;
                else
                    _refData = 0;
                OnPropertyChanged();
            }
        }

        private double _differenceAbs { get; set; }
        public double differenceAbs
        {
            get
            {
                return _differenceAbs;
            }
            set
            {
                _differenceAbs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(differenceAbsString));
            }
        }
        public string differenceAbsString
        {
            get
            {
                return Math.Round(differenceAbs, 1, MidpointRounding.AwayFromZero).ToString();
            }
        }

        private double differenceTimePctCounterTotal = 0;
        private double differenceTimePctCounterWrong = 0;
        public string differenceTimePctString
        {
            get
            {
                if (differenceTimePctCounterTotal > 0)
                    return Math.Round(((differenceTimePctCounterWrong / differenceTimePctCounterTotal) * 100.0), 2).ToString("0.00");
                else
                    return "0";
            }
        }

        public void Compare()
        {
            if (!double.IsNaN(refData) && !double.IsNaN(testData))
            {
                differenceAbs = Math.Abs(refData - testData);

                differenceTimePctCounterTotal++;
                if (differenceAbs != 0)
                    differenceTimePctCounterWrong++;
            }
            else
            {
                differenceAbs = 0;
            }
        }

        public void Reset()
        {
            differenceAbs = 0;
            differenceTimePctCounterTotal = 0;
            differenceTimePctCounterWrong = 0;
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Må matche med liste i SensorData.config (TestData og ReferenceData)
    public enum VerificationType
    {
        TimeID = 1,
        RollMaxRight20m = 2,
        RollMaxLeft20m = 3,
        PitchMaxUp20m = 4,
        PitchMaxDown20m = 5,
        InclinationMax20m = 6,
        SHR = 7,
        MSI = 8,
        WSI = 9,
        VesselHeading = 10,
        HelideckHeading = 11,
        HelicopterHeading = 12,
        DeltaVesselHeading = 13,
        DeltaWindDirection = 14,
        RelativeWindDirection = 15,
        HelideckStatus = 16,
        WindSpeed2m = 17,
        WindDir2m = 18,
        WindGust2m = 19,
        WindSpeed10m = 20,
        WindDir10m = 21,
        WindGust10m = 22,
        SensorMRU = 23,
        SensorGyro = 24,
        SensorWind = 25
    }
}