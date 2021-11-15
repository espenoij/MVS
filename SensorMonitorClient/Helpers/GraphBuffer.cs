using System;

namespace HMS_Client
{
    // NB! Det finnes en egen GraphBuffer i serveren. Dette fordi HMSData har forskjellig definisjon i server/klient.
    // Ellers identisk.
    static class GraphBuffer
    {
        public static void Update(HMSData data, RadObservableCollectionEx<HMSData> buffer)
        {
            // NB! Når vi har data tilgjengelig fores dette inn i grafene.
            // Når vi ikke har data tilgjengelig legges 0 data inn i grafene for å holde de gående.

            // Grunnen til at vi buffrer data først er pga ytelsesproblemer dersom vi kjører data rett ut i grafene på skjerm.
            // Det takler ikke grafene fra Telerik. Buffrer data først og så oppdaterer vi grafene med jevne passende mellomrom.

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

        public static void Transfer(RadObservableCollectionEx<HMSData> buffer, RadObservableCollectionEx<HMSData> dataList)
        {
            // Overfører alle data fra buffer til dataList
            if (buffer != null &&
                dataList != null)
            {
                dataList.AddRange(buffer);
                buffer.Clear();
            }
        }

        public static void RemoveOldData(RadObservableCollectionEx<HMSData> dataList, double timeInterval)
        {
            if (dataList != null)
            {
                bool doneRemovingOldData = false;

                while (!doneRemovingOldData && dataList.Count > 0)
                {
                    if (dataList[0].timestamp < DateTime.UtcNow.AddSeconds(-timeInterval))
                        dataList.RemoveAt(0);
                    else
                        doneRemovingOldData = true;
                }
            }
        }

        public static void Clear(RadObservableCollectionEx<HMSData> buffer)
        {
            // Sletter alle data i buffer
            if (buffer != null)
                buffer.Clear();
        }
    }
}
