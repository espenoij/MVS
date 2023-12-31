﻿using System;
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

        public static void UpdateWithCull(HMSData data, RadObservableCollection<HMSData> dataList, double cullFrequency)
        {
            // Ny løsning ifht det over: Begrenser (cull) graf data ved å ikke ta inn alt, men f.eks. bare hvert 5. sekund.
            // Dropper bruken av buffer.

            // Har vi to eller flere data punkt?
            if (dataList?.Count >= 2)
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

        public static void UpdateWithCull(RWDData data, RadObservableCollection<RWDData> dataList, double cullFrequency)
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
            {
                // Lagre data i listen
                dataList.Add(new RWDData()
                {
                    rwd = data.rwd,
                    wind = data.wind,
                    status = data.status,
                    timestamp = data.timestamp
                });
            }
            else
            {
                // Lagre null-data i listen
                dataList.Add(new RWDData()
                {
                    rwd = double.NaN,
                    wind = double.NaN,
                    status = DataStatus.NONE,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        public static void Transfer(RadObservableCollection<HMSData> buffer, RadObservableCollection<HMSData> dataList)
        {
            // Overfører alle data fra buffer til dataList
            if (buffer != null &&
                dataList != null)
            {
                dataList.AddRange(buffer);
                buffer.Clear();
            }
        }

        public static void Transfer(RadObservableCollection<HelideckStatus> buffer, RadObservableCollection<HelideckStatus> dataList)
        {
            // Overfører alle data fra buffer til dataList
            if (buffer != null &&
                dataList != null)
            {
                dataList.AddRange(buffer);
                buffer.Clear();
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

        public static void RemoveOldData(RadObservableCollection<RWDData> dataList, double timeInterval)
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

        public static void RemoveOldData(RadObservableCollection<HelideckStatus> dataList, double timeInterval)
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

        public static void Clear(RadObservableCollection<HMSData> buffer)
        {
            // Sletter alle data i buffer
            if (buffer != null)
                buffer.Clear();
        }

        public static void Clear(RadObservableCollection<RWDData> buffer)
        {
            // Sletter alle data i buffer
            if (buffer != null)
                buffer.Clear();
        }

        public static void Clear(RadObservableCollection<HelideckStatus> buffer)
        {
            // Sletter alle data i buffer
            if (buffer != null)
                buffer.Clear();
        }

        public static void Clear(List<HelideckStatusType> buffer)
        {
            // Sletter alle data i buffer
            if (buffer != null)
            {
                buffer.Clear();
                buffer.TrimExcess();
            }
        }

        public static void TransferDisplayData(RadObservableCollection<HelideckStatus> list, List<HelideckStatusType> dispList)
        {
            // Denne funksjonen mapper et visst antall statuser til en status indikator posisjon på tidslinjen på skjermen.
            // Viser høyeste nivå fra sub-settet med statuser.

            if (list.Count > 0 && dispList.Count > 0)
            {
                DateTime subsetStartTime = list[0].timestamp;
                double subsetTime = (list[list.Count - 1].timestamp - list[0].timestamp).TotalMilliseconds / dispList.Count;

                HelideckStatusType status = HelideckStatusType.NO_DATA;

                int dataCounter = 0;
                int subsetCounter = 0;

                for (; subsetCounter < dispList.Count; subsetCounter++)
                {
                    bool nextSubset = false;

                    for (; !nextSubset && dataCounter < list.Count; dataCounter++)
                    {
                        // Har vi kommet til nytt subSet?
                        // Eller siste data i status listen
                        if (list[dataCounter].timestamp >= subsetStartTime.AddMilliseconds(subsetTime) ||
                            dataCounter == list.Count - 1)
                        {
                            // Sette status i display listen
                            dispList[subsetCounter] = status;

                            // Gå til neste subset
                            subsetStartTime = subsetStartTime.AddMilliseconds(subsetTime);

                            // Resette høyeste status nivå
                            status = HelideckStatusType.NO_DATA;

                            // Exit denne loopen
                            nextSubset = true;

                            // Trekke fra en så vi ikke går videre til neste
                            dataCounter--;
                        }

                        // Finne høyeste status nivå
                        switch (list[dataCounter].status)
                        {
                            case HelideckStatusType.RED:
                                status = HelideckStatusType.RED;
                                break;

                            case HelideckStatusType.AMBER:
                                if (status != HelideckStatusType.RED)
                                    status = HelideckStatusType.AMBER;
                                break;

                            case HelideckStatusType.BLUE:
                                if (status != HelideckStatusType.RED &&
                                    status != HelideckStatusType.AMBER)
                                    status = HelideckStatusType.BLUE;
                                break;

                            case HelideckStatusType.OFF:
                                if (status != HelideckStatusType.RED &&
                                    status != HelideckStatusType.AMBER &&
                                    status != HelideckStatusType.BLUE)
                                    status = HelideckStatusType.OFF;
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
}
