# VSRS Installer

這是一個以 .NET Framework 4.8 WinForms 製作的 Windows 11 工具，用來把 Ventoy 安裝到外接 SSD，接著在資料分割區根目錄建立：

`apps`、`data`、`image`、`pe`、`script`、`ventoy`、`ventoyHDD`

## 安全機制

- 只允許 Windows Storage API 同時回報為 **USB 匯流排**、**SSD 媒體類型**且具有穩定唯一識別碼的實體磁碟。
- 系統碟、開機碟、內接碟、唯讀碟、離線碟不會出現在可選清單。
- 執行前必須輸入 `ERASE 磁碟編號`，並再次比對磁碟編號、容量及唯一識別碼。
- 不提供略過 SSD 或 USB 檢查的參數；呼叫 Ventoy 時也保留 Ventoy 本身的 USB 檢查。

> 安裝 Ventoy 會清除目標 SSD 的全部分割區與資料。USB 轉接盒若未正確回報 SSD 媒體類型或唯一識別碼，程式會基於安全理由拒絕顯示該裝置。

## 放入自訂資料

將檔案加入 [`payload`](payload) 下對應的七個資料夾後提交。建置時會把內容封裝進離線 Setup；安裝 Ventoy 成功後，程式會將內容複製到外接 SSD 的同名資料夾。

## 產生完整離線安裝包

1. 開啟 GitHub 倉庫的 **Actions**。
2. 選擇 **Build offline installer**。
3. 按 **Run workflow**。
4. 完成後下載 `VSRS-Installer-Offline` artifact。

Setup 內含：

- VSRS Installer 應用程式（目標 .NET Framework 4.8）
- Microsoft .NET Framework 4.8 離線安裝程式（僅在未安裝時執行）
- 官方 Ventoy 1.1.17 Windows 套件（建置時以固定 SHA-256 驗證）
- `payload` 預置資料

Windows 11 一般已內建 .NET Framework 4.8 或更新版本；離線 Setup 仍會攜帶 4.8 安裝程式，以符合離線部署需求。請注意，.NET Framework 不支援像現代 .NET 那樣的 app-local runtime；因此這裡採用原廠離線 runtime 隨 Setup 攜帶並在必要時安裝。

## 本機開發

使用 Visual Studio 2022/2026 開啟 `VSRS.Installer.sln`。若要直接執行偵錯版本，需先把完整 Ventoy Windows 套件放在輸出目錄的 `tools\ventoy` 下，並把 `payload` 複製到輸出目錄。

## 第三方元件

Ventoy 由其原作者提供，版本、授權與原始碼請參閱 [ventoy/Ventoy](https://github.com/ventoy/Ventoy)。本專案沒有修改 Ventoy 二進位檔。

