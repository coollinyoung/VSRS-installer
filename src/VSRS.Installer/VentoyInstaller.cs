using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;

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
            _log("NTFS 修正版 1.0.1；程式：" + System.Reflection.Assembly.GetExecutingAssembly().Location);
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string ventoyDirectory = Path.Combine(baseDirectory, "tools", "ventoy");
            string ventoyExe = Path.Combine(ventoyDirectory, "Ventoy2Disk_X64.exe");
            string payloadDirectory = Path.Combine(baseDirectory, "payload");

            if (!File.Exists(ventoyExe))
                throw new FileNotFoundException("找不到內附的 Ventoy2Disk_X64.exe。請將 Ventoy2Disk_X64.exe 與完整配套檔案放在程式旁的 tools/ventoy 資料夾。", ventoyExe);

            DiskInfo verified = _diskService.GetDisk(selected.Number);
            if (verified == null || !verified.IsEligible || verified.SafetyIdentity != selected.SafetyIdentity)
                throw new InvalidOperationException("磁碟狀態或身分已改變，為避免誤寫入，作業已中止。請重新整理並再次選擇外接 SSD。");

            _log($"再次驗證完成：磁碟 {verified.Number}，{verified.DetectionText}，非系統／開機磁碟。");
            _log("已解除 USB 與 SSD 類型限制；使用者須自行確認目標磁碟並承擔資料清除風險。");
            _log("開始安裝 Ventoy，資料分割區指定為 NTFS；此步驟將清除目標 SSD 的全部資料。");

            string arguments = $"VTOYCLI /I /PhyDrive:{verified.Number} /NOUSBCheck /FS:NTFS" + (useGpt ? " /GPT" : string.Empty);
            _log("Ventoy 執行檔：" + ventoyExe);
            _log("Ventoy 參數：" + arguments);
            // Remove stale status so a previous run cannot be mistaken for success.
            string doneFile = Path.Combine(ventoyDirectory, "cli_done.txt");
            if (File.Exists(doneFile))
                File.Delete(doneFile);
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
                    throw new InvalidOperationException("Ventoy 安裝失敗，結束碼：" + process.ExitCode + "。請查看tools/ventoy/cli_log.txt。");
            }

            if (!File.Exists(doneFile) || File.ReadAllText(doneFile).Trim() != "0")
                throw new InvalidOperationException("Ventoy 未回報本次安裝成功，已停止。請查看 tools/ventoy/cli_log.txt。");

            _log("Ventoy 安裝成功，等待 Windows 掛載資料分割區……");
            string targetRoot = _diskService.WaitForLargestVolume(verified.Number, TimeSpan.FromSeconds(60));
            _log("資料分割區：" + targetRoot);

            EnsureNtfsDataPartition(verified, targetRoot);
            if (!string.Equals(new DriveInfo(targetRoot).DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("資料分割區未通過 NTFS 驗證，已停止複製資料。");
            _log("NTFS 驗證通過，開始複製預置資料。");

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


        private void EnsureNtfsDataPartition(DiskInfo selected, string targetRoot)
        {
            DiskInfo current = _diskService.GetDisk(selected.Number);
            if (current == null || !current.IsEligible || current.SafetyIdentity != selected.SafetyIdentity)
                throw new InvalidOperationException("格式檢查前磁碟身分或狀態已改變，已停止。");
            if (targetRoot.Length != 3 || targetRoot[1] != ':' || !char.IsLetter(targetRoot[0]))
                throw new InvalidOperationException("資料分割區掛載路徑無效。");

            // Bind formatting to a validated partition object, never a bare drive letter.
            // Only the fresh Ventoy data partition is eligible; VTOYEFI is never formatted.
            string property = !string.IsNullOrWhiteSpace(selected.UniqueId) ? "UniqueId" : "SerialNumber";
            string identity = property == "UniqueId" ? selected.UniqueId : selected.SerialNumber;
            string script = "$ErrorActionPreference = 'Stop'\r\ntry {\r\n"
                + "$disk = Get-Disk -Number " + selected.Number + "\r\n"
                + "if ($disk.IsBoot -or $disk.IsSystem -or $disk.IsReadOnly -or $disk.IsOffline"
                + " -or $disk.Size -ne " + selected.Size
                + " -or ([string]$disk." + property + ").Trim() -cne " + PsLiteral(identity.Trim())
                + ") { throw 'Disk identity or state changed.' }\r\n"
                + "$parts = @(Get-Partition -DiskNumber $disk.Number)\r\n"
                + "$data = @($parts | Where-Object PartitionNumber -eq 1)\r\n"
                + "$efi = @($parts | Where-Object PartitionNumber -eq 2)\r\n"
                + "if ($parts.Count -ne 2 -or $data.Count -ne 1 -or $efi.Count -ne 1) { throw 'Unexpected Ventoy partition layout.' }\r\n"
                + "$p = $data[0]\r\n"
                + "if ($p.Offset -ne 1MB -or $p.Size -le 32MB -or $efi[0].Size -ne 32MB"
                + " -or ($p.Offset + $p.Size) -gt $efi[0].Offset"
                + " -or $p.DriveLetter -ne " + PsLiteral(targetRoot.Substring(0, 1))
                + ") { throw 'Data partition validation failed.' }\r\n"
                + "$bootVolume = @($efi[0] | Get-Volume)\r\n"
                + "if ($bootVolume.Count -ne 1 -or $bootVolume[0].FileSystemLabel -ne 'VTOYEFI') { throw 'VTOYEFI partition not verified.' }\r\n"
                + "$volume = @($p | Get-Volume)\r\n"
                + "if ($volume.Count -ne 1) { throw 'Data volume is ambiguous.' }\r\n"
                + "if ($volume[0].FileSystem -eq 'exFAT') {\r\n"
                + "  Write-Output 'exFAT detected: formatting verified data partition to NTFS.'\r\n"
                + "  Format-Volume -Partition $p -FileSystem NTFS -NewFileSystemLabel 'Ventoy' -Force -Confirm:$false | Out-Null\r\n"
                + "} elseif ($volume[0].FileSystem -ne 'NTFS') { throw 'Unexpected filesystem; no automatic format performed.' }\r\n"
                + "$check = Get-Partition -DiskNumber $disk.Number -PartitionNumber 1 | Get-Volume\r\n"
                + "if ($check.FileSystem -ne 'NTFS') { throw 'NTFS verification failed.' }\r\n"
                + "Write-Output 'NTFS verified.'\r\n"
                + "} catch { Write-Output ($_ | Out-String); exit 1 }\r\n";
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string system = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? "Sysnative" : "System32";
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = Path.Combine(windows, system, @"WindowsPowerShell\v1.0\powershell.exe"),
                Arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand "
                    + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            _log("檢查 Ventoy 資料分割區；若仍為 exFAT，將補做 NTFS 快速格式化。");
            using (Process process = Process.Start(info))
            {
                if (process == null)
                    throw new InvalidOperationException("無法啟動 NTFS 格式檢查程序。");
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                string details = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
                _log(details.Trim());
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("NTFS 格式化或驗證失敗，尚未複製預置資料。\r\n" + details);
            }
        }

        private static string PsLiteral(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
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
