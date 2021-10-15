param(
	[string]$ConfigurationName, 
	[string]$OutDir
)


$PlaynitePath = "G:\Playnite_dev"
$ToolboxPath = (Join-Path $PlaynitePath "toolbox.exe")
$OutDirPath = (Join-Path $OutDir "..")


if ($ConfigurationName -eq "release") 
{
	if (Test-Path $ToolboxPath)
	{
		& $ToolboxPath "pack" $OutDir $OutDirPath
	}
	else 
	{
		Write-Error "toolbox.exe not find."
	}	
}
