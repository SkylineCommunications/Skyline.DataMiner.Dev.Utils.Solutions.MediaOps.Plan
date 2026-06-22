$ErrorActionPreference = 'Stop'

$pathToGeneratedTests = Join-Path $PSScriptRoot 'tests.generated'

if (Test-Path $pathToGeneratedTests) {
    Remove-Item -Recurse -Force $pathToGeneratedTests
}

New-Item -ItemType Directory -Force -Path $pathToGeneratedTests  | Out-Null

$pathToPostBuildTests = Join-Path $PSScriptRoot 'postbuild.generated'

if (!(Test-Path $pathToPostBuildTests)) {
    throw "Could not find harvested integration test output at '$pathToPostBuildTests'. Build the integration test project before building the Test Package."
}

Copy-Item -Path (Join-Path $pathToPostBuildTests '*') -Destination $pathToGeneratedTests -Recurse -Force

# Warning, do not cleanup the collected files here. Next step in the SDK will use these.
Write-Information "Script completed successfully."
exit 0
