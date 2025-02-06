param(
    [string]$ConfigurationName, 
    [string]$OutDir,
    [string]$SolutionDir 
)

$PlaynitePaths = @(
    "C:\Playnite_dev", "C:\Projects\Playnite_dev",
    "D:\Playnite_dev", "D:\Projects\Playnite_dev",
    "G:\Playnite_dev", "G:\Projects\Playnite_dev",
    "F:\Playnite_dev", "F:\Projects\Playnite_dev"
)

$PlaynitePath = $null

foreach ($path in $PlaynitePaths) {
    if (Test-Path -Path $path) {
        $PlaynitePath = $path
        break
    }
}

if ($null -eq $PlaynitePath) {
    Write-Host "No Playnite path valid found"
} 
else {
    $ToolboxPath = (Join-Path $PlaynitePath "toolbox.exe")
    $OutDirPath = (Join-Path $OutDir "..")

    if ($ConfigurationName -eq "debug-release") {
		if (Test-Path $ToolboxPath) {
			$string = & $ToolboxPath "pack" $OutDir $OutDirPath
            Write-Host $string

            if ($string -match '"([^"]+)"') {
                $fullPath = $matches[1]
                $fileName = Split-Path -Path $fullPath -Leaf
                $fileNameWithoutExt = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
                
                $zipPath = Join-Path $OutDirPath ($fileNameWithoutExt + ".zip")
                if (Test-Path $zipPath) {
                    Remove-Item $zipPath -Force
                }
                Compress-Archive -Path $fullPath -DestinationPath $zipPath
                Write-Host "Compressed as ""$zipPath"""
            }
		} 
		else {
			Write-Host "toolbox.exe not found."
		}		
	}

    if ($ConfigurationName -eq "release") {
        $Version = ""

        foreach ($Line in Get-Content (Join-Path $SolutionDir "extension.yaml")) {
            if ($Line -imatch "Version:") {
                $Version = $Line
            }
        }

        $Manifest = (Join-Path $SolutionDir "..\manifest\")
        $YmlFile = Get-ChildItem -Path $Manifest -Filter *.yaml | Select-Object -First 1
        $Manifest = (Join-Path $Manifest $YmlFile.Name)

        $Result = Get-Content $Manifest

        if ($Result -imatch $Version) {
            if (Test-Path $ToolboxPath) {
                & $ToolboxPath "pack" $OutDir $OutDirPath

                $Result = & $ToolboxPath "verify" "installer" $Manifest
                if ($Result -imatch "Installer manifest passed verification") {
                    # Si nécessaire, ajouter des actions ici en cas de réussite
                } else {
                    Write-Host $Result
                }
            } 
			else {
                Write-Host "toolbox.exe not found."
            }
        } 
		else {
            Write-Host "Manifest does not contain the actual version"
        }
    }
}
