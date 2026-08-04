using System;
using System.Collections.Generic;
using Telerik.Windows.Data;

namespace MVS
{
    // NB! Det finnes en egen GraphBuffer i serveren. Dette fordi HMSData har forskjellig definisjon i server/klient.
    // Ellers identisk.
    static class GraphBuffer
    {
        public static void Update(HMSData data, RadObservableCollection<HMSData> buffer)
        {
            // NB! Når vi har data tilgjengelig fores dette inn i grafene.
            // Når vi ikke har data tilgjengelig legges 0 data inn i grafene for å holde de gående.

            // Grunnen til at vi buffrer data først er pga ytelsesproblemer dersom vi kjører data rett ut i grafene på skjerm.
            // Det takler ikke grafene fra Telerik. Buffrer data først og så oppdaterer vi grafene med jevne passende mellomrom.

            // Lock ensures Add is mutually exclusive with Transfer's snapshot+clear.
            lock (buffer)
            {
                if (data?.status == DataStatus.OK)
                {
                    // Lagre data i buffer
                    buffer.Add(new HMSData(data));
                }
                else
                {
                    // Lagre 0 data
                    buffer.Add(new HMSData() { data = 0, timestamp = DateTime.UtcNow });
                }
            }
        }

        public static void Transfer(RadObservableCollection<HMSData> buffer, RadObservableCollection<HMSData> dataList)
        {
            // Overfører alle data fra buffer til dataList
            if (buffer != null &&
                dataList != null)
            {
                // Snapshot the buffer under a lock so a concurrent background Add cannot
                // modify it while we enumerate, then add the plain List to dataList on
                // the UI thread where no further race is possible.
                List<HMSData> snapshot;
                lock (buffer)
                {
                    snapshot = new List<HMSData>(buffer);
                    buffer.Clear();
                }
                dataList.AddRange(snapshot);
            }
        }

        public static void RemoveOldData(RadObservableCollection<HMSData> dataList, double timeInterval)
        {
            if (dataList != null)
            {
                for (int i = 0; i < dataList.Count && dataList.Count > 0; i++)
                {
                    if (dataList[i]?.timestamp < DateTime.UtcNow.AddSeconds(-timeInterval))
                        dataList.RemoveAt(i--);
                }
            }
        }
    }
}
