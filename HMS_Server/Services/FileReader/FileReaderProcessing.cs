using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMS_Server
{
    class FileReaderProcessing
    {
        public string delimiter { get; set; }
        public bool fixedPosData { get; set; }
        public int fixedPosStart { get; set; }
        public int fixedPosTotal { get; set; }

        public FileDataFields SplitDataLine(string dataLine)
        {
            FileDataFields fileDataFields = new FileDataFields();
            int i = 0;

            // Splitte data linjen etter delimiter
            if (!fixedPosData)
            {
                if (!string.IsNullOrEmpty(delimiter))
                {
                    foreach (var dataField in dataLine.Split(delimiter[0]))
                    {
                        if (i < fileDataFields.dataField.Length)
                        {
                            // Legge inn nye data felt
                            fileDataFields.dataField[i++] = dataField;
                        }
                    }
                }
            }
            // Fixed pos data
            else
            {
                string str = TextHelper.EscapeControlChars(dataLine);
                if (fixedPosStart + fixedPosTotal < str.Length)
                    fileDataFields.dataField[0] = str.Substring(fixedPosStart, fixedPosTotal);
            }

            return fileDataFields;
        }
    }
}
