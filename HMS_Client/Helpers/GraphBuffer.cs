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

        public static void UpdateWithCull(HMSData data, RadObservableCollectionEx<HMSData> dataList, double cullFrequency)
        {
            // Ny løsning ifht det over: Begrenser (cull) graf data ved å ikke ta inn alt, men f.eks. bare hvert 5. sekund.
            // Dropper bruken av buffer.

            // Har vi to eller flere data punkt?
            if (dataList.Count >= 2)
            {
                // Sjekker om tiden mellom siste og nest siste data punkt er mindre enn cullFrequency
                if (dataList[dataList.Count - 2].timestamp.AddMilliseconds(cullFrequency) > dataList[dataList.Count - 1].timestamp)
                {
                    // I så tilfelle skal vi fjerne/cull siste data punkt før vi legge inn nytt
                    dataList.RemoveAt(dataList.Count - 1);
                }
            }

            // Status ok?
            if (data?.status == DataStatus.OK)
                // Lagre data i listen
                dataList.Add(new HMSData(data));
            else
                // Lagre 0 data
                dataList.Add(new HMSData() { data = 0, timestamp = DateTime.UtcNow });
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

        public static void Transfer(RadObservableCollectionEx<HelideckStatus> buffer, RadObservableCollectionEx<HelideckStatus> dataList)
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
                for (int i = 0; i < dataList.Count && dataList.Count > 0; i++)
                {
                    if (dataList[i]?.timestamp < DateTime.UtcNow.AddSeconds(-timeInterval))
                        dataList.RemoveAt(i--);
                }
            }
        }

        public static void RemoveOldData(RadObservableCollectionEx<HelideckStatus> dataList, double timeInterval)
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

        public static void Clear(RadObservableCollectionEx<HMSData> buffer)
        {
            // Sletter alle data i buffer
            if (buffer != null)
                buffer.Clear();
        }

        public static void Clear(RadObservableCollectionEx<HelideckStatus> buffer)
        {
            // Sletter alle data i buffer
            if (buffer != null)
                buffer.Clear();
        }
    }
}
