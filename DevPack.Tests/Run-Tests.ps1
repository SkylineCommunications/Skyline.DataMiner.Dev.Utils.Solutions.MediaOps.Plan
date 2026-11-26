#requires -Version 7.0

<#
.SYNOPSIS
Builds and repeatedly runs the MediaOps Plan tests with timing reports.
.DESCRIPTION
Requires Windows and Visual Studio MSBuild/VSTest. Uses simulation unless
-UseRealDma is supplied; credentials come from the existing user secrets or
environment variables. QAOps Bridge credential detection still takes precedence.

Builds once unless -NoBuild is supplied. -Hours and -Until stop new iterations;
an iteration already running is allowed to finish. Test failures are recorded
and repetitions continue. The final exit code is nonzero if any iteration fails.

-TestFilter accepts a native VSTest expression. -SettingsPath can select another
runsettings file; otherwise the repository settings and MSTest workers are used.
-Diagnostics adds VSTest runner diagnostics, not DataMiner API-call tracing.

Each session contains build.log, run.json, iterations.csv, test-results.csv,
summary.csv, class-summary.csv, and per-iteration console logs/TRX files.
Summary durations are summed/averaged test durations, not parallel wall time.
.EXAMPLE
pwsh -File .\DevPack.Tests\Run-Tests.ps1 -UseRealDma -Executions 3 -TestFilter "TestCategory=IntegrationTest" -Name baseline
#>
[CmdletBinding()]
param(
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Release',
	[string]$ResultsRoot = (Join-Path $PSScriptRoot '..\TestResults'),
	[Alias('MaxIterations')]
	[ValidateRange(1, [int]::MaxValue)]
	[int]$Executions = 1,
	[double]$Hours,
	[DateTime]$Until,
	[string]$TestFilter,
	[string]$Name,
	[string]$SettingsPath = (Join-Path $PSScriptRoot '..\DevPack.Tests.runsettings'),
	[switch]$NoBuild,
	[switch]$UseRealDma,
	[switch]$Diagnostics
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

if (-not $IsWindows) { throw 'This .NET Framework test suite requires Windows.' }
if ($PSBoundParameters.ContainsKey('Hours') -and $PSBoundParameters.ContainsKey('Until')) {
	throw 'Use either -Hours or -Until, not both.'
}

$startedAt = [DateTimeOffset]::Now
$sessionTimer = [Diagnostics.Stopwatch]::StartNew()
$stopAt = [DateTime]::MaxValue
if ($PSBoundParameters.ContainsKey('Hours')) {
	if ($Hours -le 0 -or [double]::IsNaN($Hours) -or [double]::IsInfinity($Hours)) {
		throw '-Hours must be a positive, finite number.'
	}
	$stopAt = (Get-Date).AddHours($Hours)
} elseif ($PSBoundParameters.ContainsKey('Until')) {
	$stopAt = $Until
}
if ($stopAt -le (Get-Date)) { throw '-Until must be in the future.' }
$hasExecutionLimit = $PSBoundParameters.ContainsKey('Executions') -or $stopAt -eq [DateTime]::MaxValue

function Find-VisualStudioTool {
	param([string]$Command, [string]$RelativePath)

	$tool = Get-Command $Command -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
	if ($tool) { return $tool.Source }

	$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
	if (Test-Path -LiteralPath $vswhere) {
		$path = & $vswhere -latest -products '*' -find $RelativePath | Select-Object -First 1
		if ($path) { return $path }
	}
	throw "Cannot find $Command. Install the Visual Studio build/test tools or use a Developer PowerShell window."
}

function Read-TestResults {
	param([string]$Path, [int]$Iteration)

	[xml]$trx = Get-Content -LiteralPath $Path -Raw
	$ns = [Xml.XmlNamespaceManager]::new($trx.NameTable)
	$ns.AddNamespace('t', $trx.DocumentElement.NamespaceURI)
	$definitions = @{}
	foreach ($definition in $trx.SelectNodes('/t:TestRun/t:TestDefinitions/t:UnitTest', $ns)) {
		$definitions[$definition.GetAttribute('id')] = $definition.SelectSingleNode('t:TestMethod', $ns)
	}

	foreach ($result in $trx.SelectNodes('//t:UnitTestResult[not(t:InnerResults/t:UnitTestResult)]', $ns)) {
		$method = $definitions[$result.GetAttribute('testId')]
		if (-not $method) { throw "Missing TRX definition for '$($result.GetAttribute('testName'))'." }
		$className = ($method.GetAttribute('className') -split ',')[0]
		$duration = [TimeSpan]::Zero
		$hasDuration = [TimeSpan]::TryParse($result.GetAttribute('duration'), [Globalization.CultureInfo]::InvariantCulture, [ref]$duration)
		[pscustomobject]@{
			Iteration = $Iteration
			ClassName = $className
			FullyQualifiedName = "$className.$($method.GetAttribute('name'))"
			DisplayName = $result.GetAttribute('testName')
			Outcome = $result.GetAttribute('outcome')
			DurationSeconds = if ($hasDuration) { $duration.TotalSeconds } else { $null }
			StartedAt = $result.GetAttribute('startTime')
			FinishedAt = $result.GetAttribute('endTime')
		}
	}
}

function Write-Reports {
	$run | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $sessionDir 'run.json')
	if ($iterations.Count) {
		$iterations | Export-Csv -LiteralPath (Join-Path $sessionDir 'iterations.csv') -NoTypeInformation
	}
	if (-not $results.Count) { return }

	$summary = @($results | Group-Object FullyQualifiedName, DisplayName | ForEach-Object {
		$group = $_.Group
		$timing = $group | Where-Object { $null -ne $_.DurationSeconds } | Measure-Object DurationSeconds -Minimum -Maximum -Average -Sum
		[pscustomobject]@{
			FullyQualifiedName = $group[0].FullyQualifiedName
			DisplayName = $group[0].DisplayName
			Executions = $group.Count
			Passed = @($group | Where-Object Outcome -eq 'Passed').Count
			Failed = @($group | Where-Object Outcome -NotIn @('Passed', 'NotExecuted', 'Inconclusive')).Count
			Skipped = @($group | Where-Object Outcome -In @('NotExecuted', 'Inconclusive')).Count
			MinSeconds = $timing.Minimum
			AverageSeconds = $timing.Average
			MaxSeconds = $timing.Maximum
			TotalTestSeconds = $timing.Sum
		}
	} | Sort-Object TotalTestSeconds -Descending)
	$summary | Export-Csv -LiteralPath (Join-Path $sessionDir 'summary.csv') -NoTypeInformation
	$results | Group-Object ClassName | ForEach-Object {
		[pscustomobject]@{
			ClassName = $_.Name
			ResultCount = $_.Count
			TotalTestSeconds = ($_.Group | Measure-Object DurationSeconds -Sum).Sum
		}
	} | Sort-Object TotalTestSeconds -Descending | Export-Csv -LiteralPath (Join-Path $sessionDir 'class-summary.csv') -NoTypeInformation
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$settings = (Resolve-Path -LiteralPath $SettingsPath).Path
$vstest = Find-VisualStudioTool 'vstest.console.exe' 'Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe'
$msbuild = if (-not $NoBuild) { Find-VisualStudioTool 'MSBuild.exe' 'MSBuild\**\Bin\MSBuild.exe' } else { $null }
$sessionName = "$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')-$PID"
if (-not [string]::IsNullOrWhiteSpace($Name)) { $sessionName += '-' + ($Name.Trim() -replace '[^A-Za-z0-9_.-]', '_') }
$sessionDir = Join-Path ([IO.Path]::GetFullPath($ResultsRoot)) $sessionName
New-Item -ItemType Directory -Path $sessionDir | Out-Null
$settingsCopy = Join-Path $sessionDir 'test.runsettings'
Copy-Item -LiteralPath $settings -Destination $settingsCopy

$results = [Collections.Generic.List[object]]::new()
$iterations = [Collections.Generic.List[object]]::new()
$run = [ordered]@{
	Name = $Name
	Status = 'Starting'
	StartedAt = $startedAt.ToString('o')
	FinishedAt = $null
	RequestedMode = if ($UseRealDma) { 'Real DataMiner Agent' } else { 'Simulation' }
	Configuration = $Configuration
	TestFilter = $TestFilter
	ExecutionLimit = if ($hasExecutionLimit) { $Executions } else { $null }
	StopAt = if ($stopAt -ne [DateTime]::MaxValue) { $stopAt.ToString('o') } else { $null }
	BuildSeconds = $null
	TestWallSeconds = 0
	SessionWallSeconds = 0
	TestAssembly = $null
	AssemblySHA256 = $null
	VSTest = $vstest
	MSBuild = $msbuild
	Diagnostics = $Diagnostics.IsPresent
	ExitCode = 0
}
$previousMode = [Environment]::GetEnvironmentVariable('MEDIAOPS_PLAN_USE_REAL_DMA', 'Process')
$harvestPath = $null
Push-Location $repoRoot
try {
	$env:MEDIAOPS_PLAN_USE_REAL_DMA = $UseRealDma.IsPresent.ToString().ToLowerInvariant()
	Write-Host "Session: $sessionDir"
	Write-Host "Requested mode: $($run.RequestedMode); filter: $TestFilter"
	Write-Reports
	if (-not $NoBuild) {
		$run.Status = 'Building'
		Write-Reports
		$tempRoot = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
		$harvestPath = Join-Path $tempRoot "PlanHarvest-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
		$buildArguments = @(
			(Join-Path $repoRoot 'MediaOps Plan.UnitTests.slnf'), '/restore', "/p:Configuration=$Configuration",
			'/verbosity:quiet', '/clp:Summary;ErrorsOnly;ForceConsoleColor', '/p:WarningLevel=0',
			'/p:RunCodeAnalysis=false', '/maxcpucount', "/p:QAOpsHarvestingOutput=$harvestPath/"
		)
		$timer = [Diagnostics.Stopwatch]::StartNew()
		& $msbuild @buildArguments 2>&1 | Tee-Object -FilePath (Join-Path $sessionDir 'build.log') | Out-Host
		$buildExitCode = $LASTEXITCODE
		$run.BuildSeconds = $timer.Elapsed.TotalSeconds
		if ($buildExitCode -ne 0) { throw "Build failed with exit code $buildExitCode. See build.log." }
	}

	$dlls = @(Get-ChildItem (Join-Path $PSScriptRoot "bin/$Configuration") -Recurse -File -Filter 'Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Tests.dll')
	if ($dlls.Count -ne 1) { throw "Expected one test assembly; found $($dlls.Count). Build the selected configuration first." }
	$run.TestAssembly = $dlls[0].FullName
	$run.AssemblySHA256 = (Get-FileHash -LiteralPath $run.TestAssembly -Algorithm SHA256).Hash

	while ((-not $hasExecutionLimit -or $iterations.Count -lt $Executions) -and (Get-Date) -lt $stopAt) {
		$iteration = $iterations.Count + 1
		$iterationDir = Join-Path $sessionDir ('iteration-{0:D4}' -f $iteration)
		New-Item -ItemType Directory -Path $iterationDir | Out-Null
		$run.Status = "Running iteration $iteration"
		Write-Reports
		Write-Host "Iteration $iteration started at $((Get-Date).ToString('o'))" -ForegroundColor Cyan
		$testArguments = @(
			$run.TestAssembly, '/Parallel', "/Settings:$settingsCopy", "/ResultsDirectory:$iterationDir",
			'/Logger:trx;LogFileName=results.trx', '/Logger:console;verbosity=detailed'
		)
		if (-not [string]::IsNullOrWhiteSpace($TestFilter)) { $testArguments += "/TestCaseFilter:$TestFilter" }
		if ($Diagnostics) { $testArguments += "/Diag:$(Join-Path $iterationDir 'vstest.log')" }
		$iterationStartedAt = [DateTimeOffset]::Now
		$timer = [Diagnostics.Stopwatch]::StartNew()
		& $vstest @testArguments 2>&1 | Tee-Object -FilePath (Join-Path $iterationDir 'console.log') | Out-Host
		$testExitCode = $LASTEXITCODE
		$timer.Stop()
		$run.TestWallSeconds += $timer.Elapsed.TotalSeconds
		$trxPath = Join-Path $iterationDir 'results.trx'
		if (-not (Test-Path -LiteralPath $trxPath)) { throw "Iteration $iteration produced no TRX (exit code $testExitCode). See $iterationDir." }
		$rows = @(Read-TestResults -Path $trxPath -Iteration $iteration)
		if ($rows.Count -eq 0) { throw 'No test results. Check -TestFilter and the iteration console log.' }
		$passed = @($rows | Where-Object Outcome -eq 'Passed').Count
		$skipped = @($rows | Where-Object Outcome -In @('NotExecuted', 'Inconclusive')).Count
		$failed = $rows.Count - $passed - $skipped
		if ($testExitCode -ne 0 -or $failed -gt 0) { $run.ExitCode = 1 }
		$results.AddRange($rows)
		$rows | Export-Csv -LiteralPath (Join-Path $sessionDir 'test-results.csv') -Append -NoTypeInformation
		$iterations.Add([pscustomobject]@{
			Iteration = $iteration
			StartedAt = $iterationStartedAt.ToString('o')
			FinishedAt = $iterationStartedAt.Add($timer.Elapsed).ToString('o')
			WallSeconds = $timer.Elapsed.TotalSeconds
			Passed = $passed
			Failed = $failed
			Skipped = $skipped
			ExitCode = $testExitCode
			TrxPath = $trxPath
		})
		Write-Reports
		Write-Host "Iteration $iteration finished in $([Math]::Round($timer.Elapsed.TotalSeconds, 3))s: passed=$passed failed=$failed skipped=$skipped."
	}
	if ($iterations.Count -eq 0) { throw 'The time limit expired before a test iteration could start.' }
	$run.Status = if ($run.ExitCode) { 'Failed' } else { 'Completed' }
} catch {
	$run.ExitCode = 1
	$run.Status = "Failed: $($_.Exception.Message)"
	Write-Host $run.Status -ForegroundColor Red
} finally {
	[Environment]::SetEnvironmentVariable('MEDIAOPS_PLAN_USE_REAL_DMA', $previousMode, 'Process')
	Pop-Location
	$run.FinishedAt = [DateTimeOffset]::Now.ToString('o')
	$run.SessionWallSeconds = $sessionTimer.Elapsed.TotalSeconds
	if ($run.Status -like 'Running*' -or $run.Status -eq 'Building') { $run.Status = 'Interrupted'; $run.ExitCode = 1 }
	Write-Reports
	if ($harvestPath -and (Test-Path -LiteralPath $harvestPath)) { Remove-Item -LiteralPath $harvestPath -Recurse -Force }
	Write-Host "Reports: $sessionDir"
}

exit $run.ExitCode
