using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMS_Server
{
    public class FileReaderData : INotifyPropertyChanged
    {
        // Change notification
        public event PropertyChangedEventHandler PropertyChanged;

        // Katalog lokasjon
        public string fileFolder { get; set; }
        // Filnavn
        public string fileName { get; set; }
        // Path
        public string filePath
        {
            get
            {
                return string.Format("{0}/{1}", fileFolder, fileName);
            }
        }
        // Lese frekvens
        public double readFrequency { get; set; }

        // Data Line delimiter
        public string delimiter { get; set; }

        // Fixed position data
        public bool fixedPosData { get; set; }
        public int fixedPosStart { get; set; }
        public int fixedPosTotal { get; set; }

        // Selected Data field
        public string dataField { get; set; }
        public DecimalSeparator decimalSeparator { get; set; }
        public bool autoExtractValue { get; set; }

        // Data Calculations Setup
        public List<CalculationSetup> calculationSetup { get; set; }

        // Konstruktør
        public FileReaderData()
        {
            fileFolder = string.Empty;
            fileName = string.Empty;
            readFrequency = Constants.FileReadFreqDefault;
            delimiter = Constants.FileReaderDelimiterDefault;
            fixedPosData = false;
            fixedPosStart = 0;
            fixedPosTotal = 0;

            calculationSetup = new List<CalculationSetup>();
            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                calculationSetup.Add(new CalculationSetup());
        }

        // Konstruktør
        public FileReaderData(FileReaderData frs)
        {
            fileFolder = frs.fileFolder;
            fileName = frs.fileName;
            readFrequency = frs.readFrequency;
            delimiter = frs.delimiter;
            fixedPosData = frs.fixedPosData;
            fixedPosStart = frs.fixedPosStart;
            fixedPosTotal = frs.fixedPosTotal;

            calculationSetup = new List<CalculationSetup>();
            foreach (var item in frs.calculationSetup)
                calculationSetup.Add(new CalculationSetup(item));
        }

        // Variabel oppdatert
        // Dersom navn ikke settes brukes kallende medlem sitt navn
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
