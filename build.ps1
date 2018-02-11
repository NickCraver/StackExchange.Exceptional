[CmdletBinding(PositionalBinding=$false)]
param(
    [bool] $CreatePackages,
    [bool] $RunTests = $true,
    [string] $PullRequestNumber
)

Write-Host "Run Parameters:" -ForegroundColor Cyan
Write-Host "  CreatePackages: $CreatePackages"
Write-Host "  RunTests: $RunTests"

$packageOutputFolder = "$PSScriptRoot\.nupkgs"
$projectsToBuild =
    'StackExchange.Exceptional.Shared',
    'StackExchange.Exceptional',
    'StackExchange.Exceptional.AspNetCore',
    'StackExchange.Exceptional.MySQL',
    'StackExchange.Exceptional.PostgreSql'

$testsToRun =
	'StackExchange.Exceptional.Tests',
	'StackExchange.Exceptional.Tests.AspNetCore'

if ($PullRequestNumber) {
    Write-Host "Building for a pull request (#$PullRequestNumber), skipping packaging." -ForegroundColor Yellow
    $CreatePackages = $false
}

if ($RunTests) {   
    dotnet restore /ConsoleLoggerParameters:Verbosity=Quiet
    foreach ($project in $testsToRun) {
        Write-Host "Running tests: $project (all frameworks)" -ForegroundColor "Magenta"
        Push-Location ".\tests\$project"

        dotnet xunit
        if ($LastExitCode -ne 0) { 
            Write-Host "Error with tests, aborting build." -Foreground "Red"
            Pop-Location
            Exit 1
        }

        Write-Host "Tests passed!" -ForegroundColor "Green"
	    Pop-Location
    }
}

if ($CreatePackages) {
    mkdir -Force $packageOutputFolder | Out-Null
    Write-Host "Clearing existing $packageOutputFolder..." -NoNewline
    Get-ChildItem $packageOutputFolder | Remove-Item
    Write-Host "done." -ForegroundColor "Green"

    Write-Host "Building all packages" -ForegroundColor "Green"
}

foreach ($project in $projectsToBuild) {
    Write-Host "Working on $project`:" -ForegroundColor "Magenta"
	
	Push-Location ".\src\$project"

    Write-Host "  Restoring and packing $project..." -ForegroundColor "Magenta"
    
    $targets = "Restore"
    if ($CreatePackages) {
        $targets += ";Pack"
    }

	dotnet msbuild "/t:$targets" "/p:Configuration=Release" "/p:PackageOutputPath=$packageOutputFolder" "/p:CI=true"

	Pop-Location

    Write-Host "Done."
    Write-Host ""
}