param(
    [Parameter(Mandatory = $true)][string]$CacheDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$runtimeName = 'ndp48-x86-x64-allos-enu.exe'
$runtimeUrl = 'https://go.microsoft.com/fwlink/?linkid=2088631'

function Assert-MicrosoftRuntime([string]$Path) {
    $signature = Get-AuthenticodeSignature -LiteralPath $Path
    if ($signature.Status -ne 'Valid' -or
        $null -eq $signature.SignerCertificate -or
        $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation(?:,|$)') {
        throw "Invalid Microsoft signature on .NET Framework installer: $Path"
    }
    $version = (Get-Item -LiteralPath $Path).VersionInfo
    if ($version.ProductName -notmatch '\.NET Framework' -or
        $version.ProductVersion -notmatch '^4\.8(?:\.|\s|$)') {
        throw "Expected Microsoft .NET Framework 4.8 offline runtime: $Path"
    }
}

try {
    New-Item -ItemType Directory -Force -Path $CacheDirectory | Out-Null
    $cachedRuntime = Join-Path $CacheDirectory $runtimeName
    if (-not (Test-Path -LiteralPath $cachedRuntime -PathType Leaf)) {
        Write-Host 'Downloading Microsoft .NET Framework 4.8 offline runtime (first build only)...'
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $downloadFile = Join-Path $CacheDirectory ([Guid]::NewGuid().ToString() + '.exe')
        try {
            Invoke-WebRequest -UseBasicParsing -Uri $runtimeUrl -OutFile $downloadFile -TimeoutSec 600
            Assert-MicrosoftRuntime $downloadFile
            Move-Item -LiteralPath $downloadFile -Destination $cachedRuntime -Force
        }
        finally {
            if (Test-Path -LiteralPath $downloadFile) {
                Remove-Item -LiteralPath $downloadFile -Force
            }
        }
    }
    Assert-MicrosoftRuntime $cachedRuntime
    $redistDirectory = Join-Path $OutputDirectory 'redist'
    New-Item -ItemType Directory -Force -Path $redistDirectory | Out-Null
    Copy-Item -LiteralPath $cachedRuntime -Destination (Join-Path $redistDirectory $runtimeName) -Force
    Write-Host "Bundled .NET Framework 4.8: $redistDirectory"
}
catch {
    [Console]::Error.WriteLine('Cannot bundle .NET Framework 4.8: ' + $_.Exception.Message)
    [Console]::Error.WriteLine('Check Internet access, or place the official offline runtime in ' + $CacheDirectory)
    exit 1
}
