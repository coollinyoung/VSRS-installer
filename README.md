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

將檔案加入與 `VSRS.Installer.sln` 同一層的 [`payload`](payload) 資料夾，放進對應的七個子資料夾。支援任意深度的子資料夾。

Visual Studio 專案會以連結顯示這份 `payload`，按「建置」後自動複製到 EXE 旁的 `payload`，不必手動設定每個檔案的屬性，也不必上傳 GitHub。安裝 Ventoy 成功後，程式會將內容複製到外接 SSD 的同名資料夾。

例如：`payload/pe/rescue.iso` 會放到 SSD 的 `pe/rescue.iso`；`payload/apps/MyTool/tool.exe` 會放到 SSD 的 `apps/MyTool/tool.exe`。

檔案放在 EXE 旁，不是嵌入 EXE。搬到另一台電腦時請攜帶整個輸出資料夾。新增檔案後若方案總管未更新，請重新載入專案再建置；移除或更名預置檔案後，請清除舊輸出的 `payload` 再建置，以免舊檔殘留。不要只改輸出目錄，下一次建置會以來源資料夾覆蓋同名檔案。

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

使用 Visual Studio 2022/2026 開啟 `VSRS.Installer.sln`，選擇 Release 後建置。預設輸出位置為 `src\VSRS.Installer\bin\Release\net48\`；`payload` 會自動複製到此處。若要直接執行，仍需先把完整 Ventoy Windows 套件放在輸出目錄的 `tools\ventoy` 下（不是 SSD 資料用的 `payload\ventoy`）。一般建置不會自動產生 Setup 或把 .NET Framework 安裝程式包進 EXE；完整離線 Setup 請使用上述打包流程。

### Ventoy 安裝入口

程式固定呼叫 EXE 旁的 `tools\ventoy\Ventoy2Disk_X64.exe`，保留原有 VTOYCLI 安裝參數與 SSD 防呆機制。找不到此檔案時會停止，不會改用 `Ventoy2Disk.exe`。請提供真正的 x64 執行檔及其完整配套資料，不要僅將其他版本重新命名。自動打包也會檢查下載套件內是否包含指定檔案，缺少時停止建置。

## 第三方元件

Ventoy 由其原作者提供，版本、授權與原始碼請參閱 [ventoy/Ventoy](https://github.com/ventoy/Ventoy)。本專案沒有修改 Ventoy 二進位檔。
