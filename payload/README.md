# 預置資料區

把要隨安裝程式部署到外接 SSD 的檔案，放進下列同名資料夾即可：

- `apps`
- `data`
- `image`
- `pe`
- `script`
- `ventoy`
- `ventoyHDD`

提交並推送到 `main` 後，GitHub Actions 會把這些內容連同程式、Ventoy 及 .NET Framework 4.8 離線安裝程式一起封裝。`.gitkeep` 只用來保留空資料夾，執行時不會複製到 SSD。
