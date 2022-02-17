﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Data;

namespace HMS_Server
{
    static class DisplayList
    {
        public static void Transfer(RadObservableCollection<HMSData> dataList, RadObservableCollection<HMSData> displayList)
        {
            // Løpe gjennom listen med data som skal overføres til skjerm
            foreach (var item in dataList.ToList())
            {
                // Finne igjen data i display listen
                var displayItem = displayList.ToList().Where(x => x.id == item.id);

                // Dersom vi fant data
                if (displayItem.Count() > 0)
                {
                    // Oppdater data
                    displayItem.First().Set(item);
                }
                // ...fant ikke data
                else
                {
                    // Legg den inn i listen
                    displayList.Add(new HMSData(item));
                }
            }
        }

        public static void Transfer(RadObservableCollection<SensorData> dataList, RadObservableCollection<SensorData> displayList)
        {
            // Løpe gjennom listen med data som skal overføres til skjerm
            foreach (var item in dataList.ToList())
            {
                // Finne igjen data i display listen
                var displayItem = displayList.ToList().Where(x => x.id == item.id);

                // Dersom vi fant data
                if (displayItem.Count() > 0)
                {
                    // Oppdater data
                    displayItem.First().Set(item);
                }
                // ...fant ikke data
                else
                {
                    // Legg den inn i listen
                    displayList.Add(new SensorData(item));
                }
            }
        }

        public static void Transfer(RadObservableCollection<VerificationData> dataList, RadObservableCollection<VerificationData> displayList)
        {
            // Løpe gjennom listen med data som skal overføres til skjerm
            foreach (var item in dataList.ToList())
            {
                // Finne igjen data i display listen
                var displayItem = displayList.ToList().Where(x => x.id == item.id);

                // Dersom vi fant data
                if (displayItem.Count() > 0)
                {
                    // Oppdater data
                    displayItem.First().Set(item);
                }
                // ...fant ikke data
                else
                {
                    // Legg den inn i listen
                    displayList.Add(new VerificationData(item));
                }
            }
        }

        public static void Transfer(List<SerialPortData> dataList, RadObservableCollection<SerialPortData> displayList)
        {
            // Løpe gjennom listen med data som skal overføres til skjerm
            foreach (var item in dataList.ToList())
            {
                // Finne igjen data i display listen
                var displayItem = displayList.ToList().Where(x => x.portName == item.portName);

                // Dersom vi fant data
                if (displayItem.Count() > 0)
                {
                    // Oppdater data
                    displayItem.First().Set(item);
                }
                // ...fant ikke data
                else
                {
                    // Legg den inn i listen
                    displayList.Add(new SerialPortData(item));
                }
            }
        }

        public static void Transfer(List<FileReaderSetup> dataList, RadObservableCollection<FileReaderSetup> displayList)
        {
            // Løpe gjennom listen med data som skal overføres til skjerm
            foreach (var item in dataList.ToList())
            {
                // Finne igjen data i display listen
                var displayItem = displayList.ToList().Where(x => x.fileFolder == item.fileFolder && x.fileName == item.fileName);

                // Dersom vi fant data
                if (displayItem.Count() > 0)
                {
                    // Oppdatere data hentet fra fil
                    displayItem.First().dataLine = item.dataLine;
                    displayItem.First().timestamp = item.timestamp;

                    // Oppdatere lese status
                    displayItem.First().portStatus = item.portStatus;
                }
                // ...fant ikke data
                else
                {
                    // Legg den inn i listen
                    displayList.Add(new FileReaderSetup(item));
                }
            }
        }

        public static void Transfer(RadObservableCollection<SensorGroup> dataList, RadObservableCollection<SensorGroup> displayList)
        {
            // Løpe gjennom listen med data som skal overføres til skjerm
            foreach (var item in dataList.ToList())
            {
                // Finne igjen data i display listen
                var displayItem = displayList.ToList().Where(x => x.id == item.id);

                // Dersom vi fant data
                if (displayItem.Count() > 0)
                {
                    // Oppdater data
                    displayItem.First().Set(item);
                }
                // ...fant ikke data
                else
                {
                    // Legg den inn i listen
                    displayList.Add(new SensorGroup(item));
                }
            }
        }
    }
}
