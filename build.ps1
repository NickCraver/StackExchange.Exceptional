[CmdletBinding(PositionalBinding=$false)]
param(
    [bool] $CreatePackages,
    [bool] $RunTests = $true,
    [string] $PullRequestNumber
)

Write-Host "Run Parameters:" -ForegroundColor Cyan
Write-Host "  CreatePackages: $CreatePackages"
Write-Host "  RunTests: $RunTests"
Write-Host "  dotnet --version:" (dotnet --version)

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

Write-Host "Doube restoring StackExchange.Exceptional.sln due to NuGet/Home #4337"
dotnet restore ".\StackExchange.Exceptional.sln"
dotnet restore ".\StackExchange.Exceptional.sln"

foreach ($project in $projectsToBuild) {
    Write-Host "Working on $project`:" -ForegroundColor "Magenta"
	
	Push-Location ".\src\$project"

    if ($CreatePackages) {
        Write-Host "  Packing (dotnet pack)..." -ForegroundColor "Magenta"
        dotnet pack -c Release /p:PackageOutputPath=$packageOutputFolder /p:NoPackageAnalysis=true /p:CI=true
    } else {
        Write-Host "  Building (dotnet build)..." -ForegroundColor "Magenta"
        dotnet build /p:CI=true
    }

	Pop-Location

    Write-Host "Done."
    Write-Host ""
}