Param(
	  [string]$SonarQubeLogin,
    [string]$SonarQubeProjectKey = "OpenVersion",
    [string]$SonarQubeServer = "http://localhost:9000",
    [string]$SonarQubeReportsPath = "./sonarqubecoverage/SonarQube.xml"
)

# Fallback to .sonarQubeLogin.secret if not explicitly passed
if (-not $SonarQubeLogin) {
	$tokenFile = "secrets\sonarQubeLogin.secret"
	$SonarQubeLogin = Get-Content $tokenFile -ErrorAction Stop | Select-Object -First 1

	if (!(Test-Path $tokenFile)) {
			Write-Host "❌ Missing SonarQube token file: $tokenFile" -ForegroundColor Red
			exit 1
	}
}

function RunTestsAndGenerateCoverageReport {
	try {
		dotnet test --configuration Release --collect:"XPlat Code Coverage"
		reportgenerator "-reports:Tests\**\TestResults\*\coverage.cobertura.xml" "-targetdir:sonarqubecoverage" "-reporttypes:SonarQube"
	}
	catch {
		Write-Host "Error: Failed to run tests and generate coverage report." -ForegroundColor Red
		Write-Host $_.Exception.Message
		exit 1
	}
}

function RunSonarQubeScan {
	try {
		dotnet sonarscanner begin /k:$SonarQubeProjectKey /d:sonar.host.url=$SonarQubeServer /d:sonar.login=$SonarQubeLogin /d:sonar.coverageReportPaths=$SonarQubeReportsPath
		RunTestsAndGenerateCoverageReport
		dotnet sonarscanner end /d:sonar.login=$SonarQubeLogin
	}
	catch {
		Write-Host "Error: Failed to run SonarQube scan." -ForegroundColor Red
		Write-Host $_.Exception.Message
		CleanUpTestResultsDirectory
		exit 1
	}
}

function CleanUpTestResultsDirectory {
	try {
		Remove-Item .\Tests\*\*\TestResults\* -Force -Recurse -ErrorAction Stop
	}
	catch {
		Write-Host "Error: Failed to clean up results directories." -ForegroundColor Red
		Write-Host $_.Exception.Message
		exit 1
	}
}

RunSonarQubeScan
CleanUpTestResultsDirectory