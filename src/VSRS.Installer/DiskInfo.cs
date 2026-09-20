using System;

namespace VSRS.Installer
{
    internal sealed class DiskInfo
    {
        public uint Number { get; set; }
        public string FriendlyName { get; set; }
        public string SerialNumber { get; set; }
        public string UniqueId { get; set; }
        public ulong Size { get; set; }
        public ushort BusType { get; set; }
        public ushort MediaType { get; set; }
        public bool IsBoot { get; set; }
        public bool IsSystem { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsOffline { get; set; }

        public bool IsUsb => BusType == 7;
        public bool IsSsd => MediaType == 4;
        public bool HasStableIdentifier => !string.IsNullOrWhiteSpace(UniqueId) || !string.IsNullOrWhiteSpace(SerialNumber);
        public bool IsEligible => IsUsb && IsSsd && HasStableIdentifier && !IsBoot && !IsSystem && !IsReadOnly && !IsOffline;

        public string SafetyIdentity
        {
            get
            {
                string stableId = !string.IsNullOrWhiteSpace(UniqueId) ? UniqueId : SerialNumber;
                return Number + "|" + Size + "|" + (stableId ?? string.Empty).Trim();
            }
        }

        public string SizeText => (Size / 1024d / 1024d / 1024d).ToString("N1") + " GB";

        public override string ToString()
        {
            string serial = string.IsNullOrWhiteSpace(SerialNumber) ? "無序號" : SerialNumber.Trim();
            return $"磁碟 {Number}｜{FriendlyName}｜{SizeText}｜S/N {serial}";
        }
    }
}
