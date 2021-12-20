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
                _testData = value;
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
                _refData = value;
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

        public void Compare()
        {
            differenceAbs = Math.Abs(refData - testData);
        }

        public void Reset()
        {
            differenceAbs = 0;
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
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
        HelideckStatus = 10,
        VesselDir = 11,
        WindSpeed2m = 12,
        WindDir2m = 13,
        WindGust2m = 14,
        WindSpeed10m = 15,
        WindDir10m = 16,
        WindGust10m = 17,
        SensorMRU = 18,
        SensorGyro = 19,
        SensorWind = 20
    }
}