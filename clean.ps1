$projects = @("Abstractions", "CMS", "CodelistProvider", "CodelistProviderClient", "DocumentStorageApi", "DocumentStorageClient", "IAM", "IAM.Base", "IAMClient", "ImportRegistrations", "MonitoringCheck", "MonitoringSettings", "MonitoringSettingsClient", "NotificationService",
"RdfFileStorage", "RdfFulltextIndex", "ValidationCore", "WebApi")

$root = Join-Path $PSScriptRoot "src"

foreach ($proj in $projects) {
    $projDir = Join-Path $root $proj

    if (-not (Test-Path $projDir)) {
        Write-Warning "Project folder not found: $projDir"
        continue
    }

    foreach ($folder in 'bin', 'obj') {
        $target = Join-Path $projDir $folder
        if (Test-Path $target) {
            Write-Host "Removing $target" -ForegroundColor Yellow
            Remove-Item $target -Recurse -Force -ErrorAction Stop
        }
    }
}