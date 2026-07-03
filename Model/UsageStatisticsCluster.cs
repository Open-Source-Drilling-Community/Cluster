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
        public History GetAllClusterLightPerDay { get; set; } = new History();
        public History GetAllClusterByFieldIdPerDay { get; set; } = new History();
        public History GetAllClusterByRigIdPerDay { get; set; } = new History();
        public History GetAllSingleWellClusterPerDay { get; set; } = new History();
        public History GetAllFixedPlatformClusterPerDay { get; set; } = new History();
        
        public History PostClusterPerDay { get; set; } = new History();
        public History PutClusterByIdPerDay { get; set; } = new History();
        public History DeleteClusterByIdPerDay { get; set; } = new History();

        public History GetAllClusterIdentityIdPerDay { get; set; } = new History();
        public History GetAllClusterIdentityMetaInfoPerDay { get; set; } = new History();
        public History GetClusterIdentityByIdPerDay { get; set; } = new History();
        public History GetAllClusterIdentityPerDay { get; set; } = new History();
        public History PostClusterIdentityPerDay { get; set; } = new History();
        public History PutClusterIdentityByIdPerDay { get; set; } = new History();
        public History DeleteClusterIdentityByIdPerDay { get; set; } = new History();

        public History GetAllClusterFeatureCategoryIdPerDay { get; set; } = new History();
        public History GetAllClusterFeatureCategoryMetaInfoPerDay { get; set; } = new History();
        public History GetClusterFeatureCategoryByIdPerDay { get; set; } = new History();
        public History GetAllClusterFeatureCategoryPerDay { get; set; } = new History();
        public History PostClusterFeatureCategoryPerDay { get; set; } = new History();
        public History PutClusterFeatureCategoryByIdPerDay { get; set; } = new History();
        public History DeleteClusterFeatureCategoryByIdPerDay { get; set; } = new History();

        public History GetAllSlotFeatureCategoryIdPerDay { get; set; } = new History();
        public History GetAllSlotFeatureCategoryMetaInfoPerDay { get; set; } = new History();
        public History GetSlotFeatureCategoryByIdPerDay { get; set; } = new History();
        public History GetAllSlotFeatureCategoryPerDay { get; set; } = new History();
        public History PostSlotFeatureCategoryPerDay { get; set; } = new History();
        public History PutSlotFeatureCategoryByIdPerDay { get; set; } = new History();
        public History DeleteSlotFeatureCategoryByIdPerDay { get; set; } = new History();

        public History GetClusterUsageStatisticsPerDay { get; set; } = new History();

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
                        catch
                        {

                        }
                    }
                    if (instance_ == null)
                    {
                        instance_ = new UsageStatisticsCluster();
                    }
                    instance_.EnsureInitialized();
                }
                return instance_;
            }
        }

        public void EnsureInitialized()
        {
            GetAllClusterIdPerDay ??= new History();
            GetAllClusterMetaInfoPerDay ??= new History();
            GetClusterByIdPerDay ??= new History();
            GetAllClusterPerDay ??= new History();
            GetAllClusterLightPerDay ??= new History();
            GetAllClusterByFieldIdPerDay ??= new History();
            GetAllClusterByRigIdPerDay ??= new History();
            GetAllSingleWellClusterPerDay ??= new History();
            GetAllFixedPlatformClusterPerDay ??= new History();
            PostClusterPerDay ??= new History();
            PutClusterByIdPerDay ??= new History();
            DeleteClusterByIdPerDay ??= new History();

            GetAllClusterIdentityIdPerDay ??= new History();
            GetAllClusterIdentityMetaInfoPerDay ??= new History();
            GetClusterIdentityByIdPerDay ??= new History();
            GetAllClusterIdentityPerDay ??= new History();
            PostClusterIdentityPerDay ??= new History();
            PutClusterIdentityByIdPerDay ??= new History();
            DeleteClusterIdentityByIdPerDay ??= new History();

            GetAllClusterFeatureCategoryIdPerDay ??= new History();
            GetAllClusterFeatureCategoryMetaInfoPerDay ??= new History();
            GetClusterFeatureCategoryByIdPerDay ??= new History();
            GetAllClusterFeatureCategoryPerDay ??= new History();
            PostClusterFeatureCategoryPerDay ??= new History();
            PutClusterFeatureCategoryByIdPerDay ??= new History();
            DeleteClusterFeatureCategoryByIdPerDay ??= new History();

            GetAllSlotFeatureCategoryIdPerDay ??= new History();
            GetAllSlotFeatureCategoryMetaInfoPerDay ??= new History();
            GetSlotFeatureCategoryByIdPerDay ??= new History();
            GetAllSlotFeatureCategoryPerDay ??= new History();
            PostSlotFeatureCategoryPerDay ??= new History();
            PutSlotFeatureCategoryByIdPerDay ??= new History();
            DeleteSlotFeatureCategoryByIdPerDay ??= new History();

            GetClusterUsageStatisticsPerDay ??= new History();
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
        public void IncrementGetAllClusterLightPerDay()
        {
            lock (lock_)
            {
                if (GetAllClusterLightPerDay == null)
                {
                    GetAllClusterLightPerDay = new History();
                }
                GetAllClusterLightPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllClusterByFieldIdPerDay()
        {
            lock (lock_)
            {
                if (GetAllClusterByFieldIdPerDay == null)
                {
                    GetAllClusterByFieldIdPerDay = new History();
                }
                GetAllClusterByFieldIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllClusterByRigIdPerDay()
        {
            lock (lock_)
            {
                if (GetAllClusterByRigIdPerDay == null)
                {
                    GetAllClusterByRigIdPerDay = new History();
                }
                GetAllClusterByRigIdPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllSingleWellClusterPerDay()
        {
            lock (lock_)
            {
                if (GetAllSingleWellClusterPerDay == null)
                {
                    GetAllSingleWellClusterPerDay = new History();
                }
                GetAllSingleWellClusterPerDay.Increment();
                ManageBackup();
            }
        }
        public void IncrementGetAllFixedPlatformClusterPerDay()
        {
            lock (lock_)
            {
                if (GetAllFixedPlatformClusterPerDay == null)
                {
                    GetAllFixedPlatformClusterPerDay = new History();
                }
                GetAllFixedPlatformClusterPerDay.Increment();
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

        public void IncrementGetAllClusterIdentityIdPerDay() => IncrementHistory(() => GetAllClusterIdentityIdPerDay, value => GetAllClusterIdentityIdPerDay = value);
        public void IncrementGetAllClusterIdentityMetaInfoPerDay() => IncrementHistory(() => GetAllClusterIdentityMetaInfoPerDay, value => GetAllClusterIdentityMetaInfoPerDay = value);
        public void IncrementGetClusterIdentityByIdPerDay() => IncrementHistory(() => GetClusterIdentityByIdPerDay, value => GetClusterIdentityByIdPerDay = value);
        public void IncrementGetAllClusterIdentityPerDay() => IncrementHistory(() => GetAllClusterIdentityPerDay, value => GetAllClusterIdentityPerDay = value);
        public void IncrementPostClusterIdentityPerDay() => IncrementHistory(() => PostClusterIdentityPerDay, value => PostClusterIdentityPerDay = value);
        public void IncrementPutClusterIdentityByIdPerDay() => IncrementHistory(() => PutClusterIdentityByIdPerDay, value => PutClusterIdentityByIdPerDay = value);
        public void IncrementDeleteClusterIdentityByIdPerDay() => IncrementHistory(() => DeleteClusterIdentityByIdPerDay, value => DeleteClusterIdentityByIdPerDay = value);

        public void IncrementGetAllClusterFeatureCategoryIdPerDay() => IncrementHistory(() => GetAllClusterFeatureCategoryIdPerDay, value => GetAllClusterFeatureCategoryIdPerDay = value);
        public void IncrementGetAllClusterFeatureCategoryMetaInfoPerDay() => IncrementHistory(() => GetAllClusterFeatureCategoryMetaInfoPerDay, value => GetAllClusterFeatureCategoryMetaInfoPerDay = value);
        public void IncrementGetClusterFeatureCategoryByIdPerDay() => IncrementHistory(() => GetClusterFeatureCategoryByIdPerDay, value => GetClusterFeatureCategoryByIdPerDay = value);
        public void IncrementGetAllClusterFeatureCategoryPerDay() => IncrementHistory(() => GetAllClusterFeatureCategoryPerDay, value => GetAllClusterFeatureCategoryPerDay = value);
        public void IncrementPostClusterFeatureCategoryPerDay() => IncrementHistory(() => PostClusterFeatureCategoryPerDay, value => PostClusterFeatureCategoryPerDay = value);
        public void IncrementPutClusterFeatureCategoryByIdPerDay() => IncrementHistory(() => PutClusterFeatureCategoryByIdPerDay, value => PutClusterFeatureCategoryByIdPerDay = value);
        public void IncrementDeleteClusterFeatureCategoryByIdPerDay() => IncrementHistory(() => DeleteClusterFeatureCategoryByIdPerDay, value => DeleteClusterFeatureCategoryByIdPerDay = value);

        public void IncrementGetAllSlotFeatureCategoryIdPerDay() => IncrementHistory(() => GetAllSlotFeatureCategoryIdPerDay, value => GetAllSlotFeatureCategoryIdPerDay = value);
        public void IncrementGetAllSlotFeatureCategoryMetaInfoPerDay() => IncrementHistory(() => GetAllSlotFeatureCategoryMetaInfoPerDay, value => GetAllSlotFeatureCategoryMetaInfoPerDay = value);
        public void IncrementGetSlotFeatureCategoryByIdPerDay() => IncrementHistory(() => GetSlotFeatureCategoryByIdPerDay, value => GetSlotFeatureCategoryByIdPerDay = value);
        public void IncrementGetAllSlotFeatureCategoryPerDay() => IncrementHistory(() => GetAllSlotFeatureCategoryPerDay, value => GetAllSlotFeatureCategoryPerDay = value);
        public void IncrementPostSlotFeatureCategoryPerDay() => IncrementHistory(() => PostSlotFeatureCategoryPerDay, value => PostSlotFeatureCategoryPerDay = value);
        public void IncrementPutSlotFeatureCategoryByIdPerDay() => IncrementHistory(() => PutSlotFeatureCategoryByIdPerDay, value => PutSlotFeatureCategoryByIdPerDay = value);
        public void IncrementDeleteSlotFeatureCategoryByIdPerDay() => IncrementHistory(() => DeleteSlotFeatureCategoryByIdPerDay, value => DeleteSlotFeatureCategoryByIdPerDay = value);

        public void IncrementGetClusterUsageStatisticsPerDay() => IncrementHistory(() => GetClusterUsageStatisticsPerDay, value => GetClusterUsageStatisticsPerDay = value);

        private void IncrementHistory(Func<History?> getHistory, Action<History> setHistory)
        {
            lock (lock_)
            {
                History? history = getHistory();
                if (history == null)
                {
                    history = new History();
                    setHistory(history);
                }

                history.Increment();
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
                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        Directory.CreateDirectory(HOME_DIRECTORY);
                        using (StreamWriter writer = new StreamWriter(HOME_DIRECTORY + "history.json"))
                        {
                            writer.Write(jsonStr);
                            writer.Flush();
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}
