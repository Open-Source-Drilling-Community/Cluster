using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NORCE.Drilling.Cluster.Model
{
    public struct CountPerDay
    {
        public DateTime Date { get; set; }
        public ulong Count { get; set; }
        /// <summary>
        /// default constructor
        /// </summary>
        public CountPerDay() { }
        /// <summary>
        /// initialization constructor
        /// </summary>
        /// <param name="date"></param>
        /// <param name="count"></param>
        public CountPerDay(DateTime date, ulong count)
        {
            Date = date;
            Count = count;
        }
    }

    public class History
    {
        public List<CountPerDay> Data { get; set; } = new List<CountPerDay>();
        /// <summary>
        /// default constructor
        /// </summary>
        public History()
        {
            if (Data == null)
            {
                Data = new List<CountPerDay>();
            }
        }

        public void Increment()
        {
            if (Data.Count == 0)
            {
                Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
            }
            else
            {
                if (Data[Data.Count - 1].Date < DateTime.UtcNow.Date)
                {
                    Data.Add(new CountPerDay(DateTime.UtcNow.Date, 1));
                }
                else
                {
                    Data[Data.Count - 1] = new CountPerDay(Data[Data.Count - 1].Date, Data[Data.Count - 1].Count + 1);
                }
            }
        }
    }
    public class UsageStatisticsCluster
    {
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;

        public DateTime LastSaved { get; set; } = DateTime.MinValue;
        public TimeSpan BackUpInterval { get; set; } = TimeSpan.FromMinutes(5);

        public History GetAllClusterIdPerDay { get; set; } = new History();
        public History GetAllClusterMetaInfoPerDay { get; set; } = new History();
        public History GetClusterByIdPerDay { get; set; } = new History();
        public History GetAllClusterPerDay { get; set; } = new History();
        public History PostClusterPerDay { get; set; } = new History();
        public History PutClusterByIdPerDay { get; set; } = new History();
        public History DeleteClusterByIdPerDay { get; set; } = new History();

        private static object lock_ = new object();

        private static UsageStatisticsCluster? instance_ = null;

        public static UsageStatisticsCluster Instance
        {
            get
            {
                if (instance_ == null)
                {
                    if (File.Exists(HOME_DIRECTORY + "history.json"))
                    {
                        try
                        {
                            string? jsonStr = null;
                            lock (lock_)
                            {
                                using (StreamReader reader = new StreamReader(HOME_DIRECTORY + "history.json"))
                                {
                                    jsonStr = reader.ReadToEnd();
                                }
                                if (!string.IsNullOrEmpty(jsonStr))
                                {
                                    instance_ = JsonSerializer.Deserialize<UsageStatisticsCluster>(jsonStr);
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    if (instance_ == null)
                    {
                        instance_ = new UsageStatisticsCluster();
                    }
                }
                return instance_;
            }
        }

        public void IncrementGetAllClusterIdPerDay()
        {
            lock (lock_)
            {
                if (GetAllClusterIdPerDay == null)
                {
                    GetAllClusterIdPerDay = new History();
                }
                GetAllClusterIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllClusterMetaInfoPerDay()
        {
            lock (lock_)
            {
                if (GetAllClusterMetaInfoPerDay == null)
                {
                    GetAllClusterMetaInfoPerDay = new History();
                }
                GetAllClusterMetaInfoPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetClusterByIdPerDay()
        {
            lock (lock_)
            {
                if (GetClusterByIdPerDay == null)
                {
                    GetClusterByIdPerDay = new History();
                }
                GetClusterByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPostClusterPerDay()
        {
            lock (lock_)
            {
                if (PostClusterPerDay == null)
                {
                    PostClusterPerDay = new History();
                }
                PostClusterPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllClusterPerDay()
        {
            lock (lock_)
            {
                if (GetAllClusterPerDay == null)
                {
                    GetAllClusterPerDay = new History();
                }
                GetAllClusterPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementPutClusterByIdPerDay()
        {
            lock (lock_)
            {
                if (PutClusterByIdPerDay == null)
                {
                    PutClusterByIdPerDay = new History();
                }
                PutClusterByIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementDeleteClusterByIdPerDay()
        {
            lock (lock_)
            {
                if (DeleteClusterByIdPerDay == null)
                {
                    DeleteClusterByIdPerDay = new History();
                }
                DeleteClusterByIdPerDay.Increment();
                ManageBackup();
            }
        }

        private void ManageBackup()
        {
            if (DateTime.UtcNow > LastSaved + BackUpInterval)
            {
                LastSaved = DateTime.UtcNow;
                try
                {
                    string jsonStr = JsonSerializer.Serialize(this);
                    if (!string.IsNullOrEmpty(jsonStr) && Directory.Exists(HOME_DIRECTORY))
                    {
                        using (StreamWriter writer = new StreamWriter(HOME_DIRECTORY + "history.json"))
                        {
                            writer.Write(jsonStr);
                            writer.Flush();
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
