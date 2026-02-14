$isccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)

$isccPath = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $isccPath) {
    Write-Error "Inno Setup compiler (ISCC.exe) not found."
    exit 1
}

Write-Host "Ensure you have run dotnet publish (SingleFile=False)!" 

Write-Host "Found ISCC at: $isccPath"
& $isccPath "setup.iss"
