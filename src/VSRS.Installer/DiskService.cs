using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;

namespace VSRS.Installer
{
    internal sealed class DiskService
    {
        private const string StorageNamespace = @"root\Microsoft\Windows\Storage";

        public IReadOnlyList<DiskInfo> GetAllDisks()
        {
            Dictionary<uint, ushort> mediaTypes = GetMediaTypes();
            List<DiskInfo> result = new List<DiskInfo>();
            ManagementScope scope = new ManagementScope(StorageNamespace);
            scope.Connect();

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT Number,FriendlyName,SerialNumber,UniqueId,Size,BusType,IsBoot,IsSystem,IsReadOnly,IsOffline FROM MSFT_Disk")))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                foreach (ManagementObject item in objects)
                {
                    uint number = Convert.ToUInt32(item["Number"]);
                    result.Add(new DiskInfo
                    {
                        Number = number,
                        FriendlyName = Convert.ToString(item["FriendlyName"]) ?? "未知裝置",
                        SerialNumber = Convert.ToString(item["SerialNumber"]),
                        UniqueId = Convert.ToString(item["UniqueId"]),
                        Size = Convert.ToUInt64(item["Size"]),
                        BusType = Convert.ToUInt16(item["BusType"]),
                        MediaType = mediaTypes.TryGetValue(number, out ushort mediaType) ? mediaType : (ushort)0,
                        IsBoot = ToBool(item["IsBoot"]),
                        IsSystem = ToBool(item["IsSystem"]),
                        IsReadOnly = ToBool(item["IsReadOnly"]),
                        IsOffline = ToBool(item["IsOffline"])
                    });
                }
            }

            return result.OrderBy(d => d.Number).ToList();
        }

        public DiskInfo GetDisk(uint number)
        {
            return GetAllDisks().SingleOrDefault(d => d.Number == number);
        }

        public string WaitForLargestVolume(uint diskNumber, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                string root = FindLargestVolume(diskNumber);
                if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
                    return root;

                Thread.Sleep(1000);
            }

            throw new InvalidOperationException("Ventoy 已完成，但 Windows 尚未掛載外接 SSD 的資料分割區。請重新插拔 SSD 後再試。 ");
        }

        private static Dictionary<uint, ushort> GetMediaTypes()
        {
            Dictionary<uint, ushort> result = new Dictionary<uint, ushort>();
            ManagementScope scope = new ManagementScope(StorageNamespace);
            scope.Connect();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT DeviceId,MediaType FROM MSFT_PhysicalDisk")))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                foreach (ManagementObject item in objects)
                {
                    if (uint.TryParse(Convert.ToString(item["DeviceId"]), out uint deviceId))
                        result[deviceId] = Convert.ToUInt16(item["MediaType"]);
                }
            }
            return result;
        }

        private static string FindLargestVolume(uint diskNumber)
        {
            List<Tuple<string, long>> candidates = new List<Tuple<string, long>>();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_DiskDrive WHERE Index=" + diskNumber))
            using (ManagementObjectCollection disks = searcher.Get())
            {
                foreach (ManagementObject disk in disks)
                using (ManagementObjectCollection partitions = disk.GetRelated("Win32_DiskPartition"))
                {
                    foreach (ManagementObject partition in partitions)
                    using (ManagementObjectCollection logicalDisks = partition.GetRelated("Win32_LogicalDisk"))
                    {
                        foreach (ManagementObject logicalDisk in logicalDisks)
                        {
                            string deviceId = Convert.ToString(logicalDisk["DeviceID"]);
                            long size = logicalDisk["Size"] == null ? 0 : Convert.ToInt64(logicalDisk["Size"]);
                            if (!string.IsNullOrWhiteSpace(deviceId))
                                candidates.Add(Tuple.Create(deviceId + @"\", size));
                        }
                    }
                }
            }

            return candidates.OrderByDescending(x => x.Item2).Select(x => x.Item1).FirstOrDefault();
        }

        private static bool ToBool(object value)
        {
            return value != null && Convert.ToBoolean(value);
        }
    }
}
