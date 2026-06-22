param (
    [Parameter(Mandatory = $true)]
    [string]$PathToTestPackageContent
)

$ErrorActionPreference = 'Stop'

Install-Module Skyline.DataMiner.QAOps.PipelineLibrary -Repository PSGallery -Force -Scope CurrentUser -MinimumVersion 1.3.0
Import-Module Skyline.DataMiner.QAOps.PipelineLibrary -Force

$pathToTestHarvesting = Join-Path $PathToTestPackageContent 'TestHarvesting'
$pathToGeneratedTests = Join-Path $pathToTestHarvesting 'tests.generated'
$testAssemblyPath = Join-Path $pathToGeneratedTests 'Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests.dll'
$testCaseName = 'pipeline_ExecuteMediaOpsPlanIntegrationTests'

# Track script start time
$scriptStart = Get-Date

try {
    Write-Host "Running MediaOps Plan integration tests..." -ForegroundColor Cyan

    if (!(Test-Path $testAssemblyPath)) {
        throw "Could not find integration test assembly at '$testAssemblyPath'."
    }

    Invoke-DotNetTestAndPublishResults `
        -PathToTestPackageContent $PathToTestPackageContent `
        -TestDllPath $testAssemblyPath `
        -ResultsFileName 'mediaops-plan-integration-tests.trx' `
        -TestFilter 'TestCategory=IntegrationTest' `
        -PublishNotExecuted $false

    # Send OK result indicating that test package execution has finished successfully
    Push-TestCaseResult -Outcome 'OK' -Name $testCaseName -Duration ((Get-Date) - $scriptStart) -Message "MediaOps Plan integration test execution finished." -TestAspect Execution
} catch {
    Push-TestCaseResult -Outcome 'Fail' -Name $testCaseName -Duration ((Get-Date) - $scriptStart) -Message "Exception during MediaOps Plan integration test execution: $($_.Exception.Message)" -TestAspect Execution
    exit 1
}