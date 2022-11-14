param(
	[string]$ConfigurationName, 
	[string]$OutDir,
	[string]$SolutionDir 
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


$Version = ""
foreach($Line in Get-Content (Join-Path $SolutionDir  "extension.yaml")) 
{
    if($Line -imatch "Version:")
	{
        $Version = $Line
    }
}


$Manifest = (Join-Path $SolutionDir  "..\manifest\Lacro59_ScreenshotsVisualizer.yaml")
$Result = Get-Content $Manifest
if($Result -imatch $Version)
{
	if (Test-Path $ToolboxPath)
	{
		$Result = & $ToolboxPath "verify" "installer" $Manifest
		if($Result -imatch "Installer manifest passed verification")
		{		
			& $ToolboxPath "pack" $OutDir $OutDirPath	
		}
		else 
		{
			echo $Result
		}		
	}
	else 
	{
		echo "toolbox.exe not find."
	}	
}
else
{
    echo "Manifest not contains actual version"
}
