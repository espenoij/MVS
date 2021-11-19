using System.Collections.Generic;

namespace HMS_Server
{
    public class FileReaderData
    {
        // Katalog lokasjon
        public string fileFolder { get; set; }
        // Filnavn
        public string fileName { get; set; }
        // Path
        public string filePath
        {
            get
            {
                return string.Format($"{0}/{1}", fileFolder, fileName);
            }
        }

        // Data Calculations Setup
        public List<CalculationSetup> calculationSetup { get; set; }

        // Konstruktør
        public FileReaderData()
        {
            fileFolder = string.Empty;
            fileName = string.Empty;

            calculationSetup = new List<CalculationSetup>();
            for (int i = 0; i < Constants.DataCalculationSteps; i++)
                calculationSetup.Add(new CalculationSetup());
        }

        // Konstruktør
        public FileReaderData(FileReaderData frs)
        {
            fileFolder = frs.fileFolder;
            fileName = frs.fileName;

            calculationSetup = new List<CalculationSetup>();
            foreach (var item in frs.calculationSetup)
                calculationSetup.Add(new CalculationSetup(item));
        }
    }
}
