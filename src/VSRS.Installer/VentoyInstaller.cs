using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace VSRS.Installer
{
    internal sealed class VentoyInstaller
    {
        private static readonly string[] RequiredFolders =
        {
            "apps", "data", "image", "pe", "script", "ventoy", "ventoyHDD"
        };

        private readonly DiskService _diskService;
        private readonly Action<string> _log;

        public VentoyInstaller(DiskService diskService, Action<string> log)
        {
            _diskService = diskService;
            _log = log;
        }

        public void Install(DiskInfo selected, bool useGpt)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string ventoyDirectory = Path.Combine(baseDirectory, "tools", "ventoy");
            string ventoyExe = Path.Combine(ventoyDirectory, "Ventoy2Disk_X64.exe");
            string payloadDirectory = Path.Combine(baseDirectory, "payload");

            if (!File.Exists(ventoyExe))
                throw new FileNotFoundException("找不到內附的 Ventoy2Disk_X64.exe。請將 Ventoy2Disk_X64.exe 與完整配套檔案放在程式旁的 tools/ventoy 資料夾。", ventoyExe);

            DiskInfo verified = _diskService.GetDisk(selected.Number);
            if (verified == null || !verified.IsEligible || verified.SafetyIdentity != selected.SafetyIdentity)
                throw new InvalidOperationException("磁碟狀態或身分已改變，為避免誤寫入，作業已中止。請重新整理並再次選擇外接 SSD。");

            _log($"再次驗證完成：磁碟 {verified.Number}，USB 外接 SSD，非系統／開機磁碟。");
            _log("開始安裝 Ventoy；此步驟將清除目標 SSD 的全部資料。");

            string arguments = $"VTOYCLI /I /PhyDrive:{verified.Number}" + (useGpt ? " /GPT" : string.Empty);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ventoyExe,
                Arguments = arguments,
                WorkingDirectory = ventoyDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("無法啟動 Ventoy 安裝程序。");

                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("Ventoy 安裝失敗，結束碼：" + process.ExitCode + "。請查看程式安裝目錄中的 Ventoy2Disk.log。");
            }

            _log("Ventoy 安裝成功，等待 Windows 掛載資料分割區……");
            string targetRoot = _diskService.WaitForLargestVolume(verified.Number, TimeSpan.FromSeconds(60));
            _log("資料分割區：" + targetRoot);

            foreach (string folder in RequiredFolders)
            {
                string destination = Path.Combine(targetRoot, folder);
                Directory.CreateDirectory(destination);
                string source = Path.Combine(payloadDirectory, folder);
                if (Directory.Exists(source))
                    CopyDirectory(source, destination);
                _log("已建立／填入：" + folder);
            }

            File.WriteAllText(Path.Combine(targetRoot, "VSRS-INSTALL-COMPLETE.txt"),
                "VSRS Installer completed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
            _log("全部作業完成。");
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                if (string.Equals(Path.GetFileName(file), ".gitkeep", StringComparison.OrdinalIgnoreCase))
                    continue;
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }

            foreach (string directory in Directory.GetDirectories(source))
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
