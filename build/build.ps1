param(
	[string]$ConfigurationName, 
	[string]$OutDir
)


$PlaynitePathTEMP = "C:\Playnite_dev"
if (Test-Path -Path $PlaynitePathTEMP) 
{
	$PlaynitePath = $PlaynitePathTEMP
}

$PlaynitePathTEMP = "D:\Playnite_dev"
if (Test-Path -Path $PlaynitePathTEMP) 
{
	$PlaynitePath = $PlaynitePathTEMP
}

$PlaynitePathTEMP = "G:\Playnite_dev"
if (Test-Path -Path $PlaynitePathTEMP) 
{
	$PlaynitePath = $PlaynitePathTEMP
}

$PlaynitePathTEMP = "F:\Playnite_dev"
if (Test-Path -Path $PlaynitePathTEMP) 
{
	$PlaynitePath = $PlaynitePathTEMP
}


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
