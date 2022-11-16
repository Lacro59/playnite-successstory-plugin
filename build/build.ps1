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


if ($ConfigurationName -eq "release") 
{
	$Version = ""
	foreach($Line in Get-Content (Join-Path $SolutionDir  "extension.yaml")) 
	{
		if($Line -imatch "Version:")
		{
			$Version = $Line
		}
	}

	$Manifest = (Join-Path $SolutionDir  "..\manifest\")
	$YmlFile = Get-ChildItem -Path $Manifest -Filter *.yaml | Select-Object -First 1
	$Manifest = (Join-Path $Manifest $YmlFile.Name)

	$Result = Get-Content $Manifest
	if($Result -imatch $Version)
	{
		if (Test-Path $ToolboxPath)
		{
			& $ToolboxPath "pack" $OutDir $OutDirPath
		
			$Result = & $ToolboxPath "verify" "installer" $Manifest
			if($Result -imatch "Installer manifest passed verification")
			{		
				
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
}
