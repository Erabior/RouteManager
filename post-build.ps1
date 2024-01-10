param
(   
    [string]$SolnDir,
    [string]$ProjDir,
    [string]$Config,
    [string]$Output,
	[string]$Type,
	[string]$Ver
)

Write-Host $SolnDir
Write-Host $ProjDir
Write-Host $Ver
Write-Host $Type
Write-Host $Config

$compress

if($Type -eq "UMM"){
	#If we are building for UMM we should update the JSON
	
	$json = Get-Content ($ProjDir + 'info.json') -raw | ConvertFrom-Json
	$json.Version = $Ver
	$json | ConvertTo-Json -depth 32| set-content ($ProjDir + '\info.json')
	
	#Files to be compressed if we make a UMM zip
	$compress = @{
		Path = ($ProjDir + "bin\Release\RouteManager.UMM.dll"), ($ProjDir + '\info.json')
		CompressionLevel = "Fastest"
		DestinationPath = ($SolnDir + "\Release\RouteManager.UMM " + $Ver + ".zip")
	}
	
	#Check the game mod folder exists
	if (!(Test-Path ($Output))) {
		New-Item -ItemType Directory -Path $Output
	}
	
	#copy files to game mod folder for UMM mod
	Copy-Item -Force -Path ($ProjDir + "bin\" + $Config + "\RouteManager.UMM.dll") -Destination $Output
	Copy-Item -Force -Path ($ProjDir + '\info.json') -Destination $Output
	
}elseif($Type -eq "BepInEx"){
	#Files to be compressed if we make a BepInEx zip (only applies to Release config)
	$compress = @{
		Path = ($ProjDir + "bin\Release\RouteManager.BepInEx.dll"),($ProjDir + "configs\RouteManager.ini")
		CompressionLevel = "Fastest"
		DestinationPath = ($SolnDir + "\Release\RouteManager.BepInEx " + $Ver + ".zip")
	}
	
	#Check the game mod folder exists
	if (!(Test-Path ($Output))) {
		New-Item -ItemType Directory -Path $Output
	}
	
	#copy files to game plugin folder for BepInEx mod
	Copy-Item -Force -Path ($ProjDir + "bin\" + $Config + "\RouteManager.BepInEx.dll") -Destination $Output
	Copy-Item -Force -Path ($ProjDir + "configs\RouteManager.ini") -Destination $Output
}

#Are we building a release or debug?
if ($Config -eq "Release"){
	if (!(Test-Path ($SolnDir + "\Release"))) {
		New-Item -ItemType Directory -Path ($SolnDir + "\Release")
	}
	
    Compress-Archive @compress -Force
}