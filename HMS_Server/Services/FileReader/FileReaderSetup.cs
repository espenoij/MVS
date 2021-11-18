using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMS_Server
{
    public class FileReaderSetup
    {
        // Katalog lokasjon
        public string filePath { get; set; }
        // Filnavn
        public string fileName { get; set; }

        // Konstruktør
        public FileReaderSetup(FileReaderSetup frs)
        {
            filePath = frs.filePath;
            fileName = frs.fileName;
        }
    }
}
