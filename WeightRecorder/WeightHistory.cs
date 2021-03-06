﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WeightRecorder
{
    public class WeightHistory
    {
        List<WeightRecord> History = new List<WeightRecord>();
        public WeightHistory() { }
        const int WEIGHT_THRESHOLD = 100; // ignore less than 100g
        const int NO_LOAD_TIME = 100; // 100 samples

        public Action<double> DumpCompleted;

        public void Dump()
        {
            var temp_history = new List<WeightRecord>(History);
            NormalizeTime(temp_history);

            var maxWeight = 0.0;
            foreach (var r in temp_history)
            {
                maxWeight = Math.Max(maxWeight, r.Weight);
            }

            var filename = DateTime.Now.ToString().Replace('/', '-').Replace(':', '-').Replace(' ', '-') + "_Max" + maxWeight + "g.csv";
            int lastTime = 0;
            int upper2Byte = 0;

            using (StreamWriter writer = new StreamWriter(filename, false))
            {
                StringBuilder sb = new StringBuilder();
                foreach (var r in temp_history)
                {
                    if (lastTime - r.TimeInMillis > (65536 * (upper2Byte + 1) - 5000))
                    {
                        // maybe clock reset
                        upper2Byte++;
                    }

                    r.TimeInMillis += upper2Byte * 65536;
                    sb.AppendFormat("{0},{1}{2}", r.TimeInMillis, r.Weight, Environment.NewLine);
                    //Console.Write("time: {0} weight {1}\n", r.TimeInMillis, r.Weight);
                    lastTime = r.TimeInMillis;
                }
                writer.Write(sb.ToString());
            }
            Console.WriteLine(temp_history.Count + " records saved (~" + lastTime + " ms) : " + filename);
            History.Clear();

            DumpCompleted?.Invoke(maxWeight);
        }

        void NormalizeTime(List<WeightRecord> records)
        {
            if (records.Count < 1) { return; }

            var offset = records[0].TimeInMillis;
            foreach(var r in records)
            {
                r.TimeInMillis -= offset;
            }
        }

        bool recording = false;

        public void Add(int time, double weight)
        {
            History.Add(new WeightRecord() { TimeInMillis = time, Weight = weight });


            if (!recording)
            {
                if (NoLoad(History, 0, History.Count))
                {
                    History.RemoveAt(0);
                }
                else if (History.Count > NO_LOAD_TIME)
                {
                    recording = true;
                }
            }
            else
            {
                // recording
                if (NoLoad(History, History.Count - NO_LOAD_TIME, History.Count))
                {
                    Dump();
                    recording = false;
                }
            }

        }

        bool NoLoad(List<WeightRecord> record, int start, int last)
        {
            if (record.Count < NO_LOAD_TIME) { return false; }

            var max = record[0].Weight;
            for (int i = start; i < last; i++)
            {
                max = Math.Max(record[i].Weight, max);
            }

            return max < WEIGHT_THRESHOLD;
        }
    }

    public class WeightRecord
    {
        public int TimeInMillis { get; set; } = 0;
        public double Weight { get; set; } = 0;
    }
}
