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
                // NB! Alle grafpunkt tidsstemples med samme klokke (UtcNow) som x-aksen
                // (alignmentTime) bruker. Kildens timestamp kan stå stille mellom
                // bufferoppdateringer eller ligge bak veggklokka, noe som gir like eller
                // ikke-monotone x-verdier. Da tegner Telerik-grafen en linje fra siste
                // til første datapunkt. Konsekvent UtcNow gir strengt økende x-verdier.
                if (data?.status == DataStatus.OK)
                {
                    // Lagre data i buffer
                    buffer.Add(new HMSData(data) { timestamp = DateTime.UtcNow });
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
            if (dataList == null || dataList.Count == 0)
                return;

            DateTime cutoff = DateTime.UtcNow.AddSeconds(-timeInterval);

            // Finn alle gamle datapunkter som skal fjernes.
            List<HMSData> oldData = new List<HMSData>();
            foreach (HMSData item in dataList)
            {
                if (item?.timestamp < cutoff)
                    oldData.Add(item);
            }

            if (oldData.Count == 0)
                return;

            // Fjerne alle gamle datapunkter i én batch-operasjon.
            // Ved å bruke Suspend/ResumeNotifications sendes kun én collection-changed
            // hendelse til grafen. Dersom vi i stedet fjerner ett og ett punkt vil
            // Telerik-grafen midlertidig tegne en linje fra siste til første datapunkt.
            dataList.SuspendNotifications();
            try
            {
                foreach (HMSData item in oldData)
                    dataList.Remove(item);
            }
            finally
            {
                dataList.ResumeNotifications();
            }
        }
    }
}
