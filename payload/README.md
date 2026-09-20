# 預置資料區

把要隨安裝程式部署到外接 SSD 的檔案，放進下列同名資料夾即可：

- `apps`
- `data`
- `image`
- `pe`
- `script`
- `ventoy`
- `ventoyHDD`

直接將檔案放入本機的這七個資料夾，再以 Visual Studio 建置即可；不需要提交或上傳 GitHub。專案會把整份內容（含子資料夾）自動複製到 EXE 旁的 `payload`，供安裝 SSD 時使用。

例如：`pe/rescue.iso` → SSD 根目錄的 `pe/rescue.iso`。

`.gitkeep` 只用來保留空資料夾，執行時不會複製到 SSD。本說明也不會複製到 SSD。請勿在這七個資料夾以外放置要部署的檔案，程式只部署這七個資料夾。移除或更名來源檔案後，請清除舊輸出的 `payload` 再重新建置，避免殘留舊檔。

本倉庫為公開倉庫：個人資料、授權軟體、Windows/WinPE 映像等可只放在本機打包，請勿在未確認授權與隱私前提交到 GitHub。
