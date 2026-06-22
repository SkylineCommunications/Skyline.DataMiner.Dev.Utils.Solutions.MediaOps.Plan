# QAOps Test Package Template - File Structure Guide

This README serves as a guide for creating your QAOps test package. Follow the structure and guidelines below to set up your test automation.

## Setting Up Your TestPackageContent

Create your test package following this directory structure. The `TestPackageContent` directory is the root where you'll organize all your test assets:

```txt
TestPackageContent/
├── TestHarvesting/           # (Required) Auto-generated test discovery
├── TestPackagePipeline/      # (Required) Test execution scripts
├── Tests/                    # (Optional) Fixed tests in repository
├── Dependencies/             # (Optional) Fixed dependencies in repository
└── qaops.config.xml          # (Optional) Configuration file
```

**Mandatory components:**

* `TestHarvesting` directory with a `TestDiscovery.ps1` script
* `TestPackagePipeline` directory with at least one numbered PowerShell script

**You may optionally add:**

* `Tests` and `Dependencies` directories for repository-committed test assets
* `qaops.config.xml` for custom configuration

### TestHarvesting

The `TestDiscovery.ps1` script will automatically harvest tests from anywhere within your Git repository during the build process.

The harvesting process should create the following directories if used:

* **`dependencies.generated`** – Can contain any type of dependency files.
* **`tests.generated`** – Can contain any type of test files.
* **`xmlautomationtests.generated`** – Must follow a specific structure:

  * A directory named after the test.
  * Inside that directory:

    * A `script.xml` file.
    * *(Optional)* a `dlls` folder for additional assemblies.

The scripts inside `xmlautomationtests.generated` will be added to the package itself so that it will be installed on DataMiner on execution of the test run. As such, this directory will not be present on the test system.

#### Important: Do Not Create Generated Directories

The following directories will be automatically created during build - **do not create or commit these manually**:

* `dependencies.generated/`
* `tests.generated/`
* `xmlautomationtests.generated/`

Your project template includes a `.gitignore` file that excludes these generated directories. This ensures:

* Your repository stays clean without build artifacts
* Consistent test discovery across different environments
* Fresh test harvesting on each build
* No conflicts between team members' local generated content

Your `TestDiscovery.ps1` script will regenerate these directories every time you build the project.

#### Example directory structure

```txt
TestHarvesting/
├── dependencies.generated/
│   ├── shared-dependency.dll
│   └── helper-config.json
│
├── tests.generated/
│   ├── example-login.spec.ts
│   └── example-navigation.spec.ts
│
└── xmlautomationtests.generated/
    ├── MyFirstTest/
    │   ├── script.xml
    │   └── dlls/
    │       └── helper-library.dll
    │
    └── MySecondTest/
        └── script.xml
```

### TestPackagePipeline

Create PowerShell scripts that define your test execution workflow. The QAOps Bridge will execute these scripts sequentially during test runs.

#### Script Naming Requirements

Name your scripts following this pattern: `{number}.{description}.ps1`

* The QAOps Bridge sorts scripts numerically by the leading number
* Scripts execute in ascending order (e.g., `1.setup.ps1`, `2.execute-tests.ps1`, `3.finalize.ps1`)

#### Script Template Structure

Each PowerShell script you create must accept this mandatory parameter:

```powershell
param (
    [Parameter(Mandatory = $true)]
    [string]$PathToTestPackageContent
)

# TODO: Add your test logic here
# Use $PathToTestPackageContent to access test files and dependencies
```

#### Example Pipeline Structure

```txt
TestPackagePipeline/
├── 1.setup.ps1          # Environment setup and prerequisites
├── 2.install-deps.ps1   # Install test dependencies
├── 3.run-tests.ps1      # Execute your actual tests
├── 4.finalize.ps1       # Collect results
└── helpers/             # TODO: Optional helper files directory
    ├── test-utilities.psm1
    ├── data-processor.py
    ├── config-validator.js
    └── shared-functions.ps1
```

#### Adding Helper Code and Utilities

You can include additional code files alongside your numbered PowerShell scripts to organize and reuse functionality. The QAOps Bridge will only execute the numbered `.ps1` files as entry points, but from those scripts you can call any other code you need.

**Examples of helper files you might add:**

* **PowerShell modules** (`.psm1`) for reusable PowerShell functions
* **Python scripts** (`.py`) for data processing or API interactions
* **JavaScript/Node.js files** (`.js`) for web automation or JSON processing
* **Configuration files** (`.json`, `.xml`, `.yaml`) for script settings
* **Additional PowerShell scripts** without numbers for utility functions
* **Batch files** (`.bat`) or shell scripts for system operations

**Usage pattern:**

```powershell
# In your numbered script (e.g., 3.run-tests.ps1)
param (
    [Parameter(Mandatory = $true)]
    [string]$PathToTestPackageContent
)

# TODO: Call your helper code from here
# Examples:
# . "$PSScriptRoot\helpers\shared-functions.ps1"
# python "$PSScriptRoot\helpers\data-processor.py"
# node "$PSScriptRoot\helpers\config-validator.js"
```

### Tests

The `Tests` directory contains static test files that are committed to the repository and don't require harvesting.

### Dependencies

The `Dependencies` directory contains static dependency files that are committed to the repository and required by tests in the `Tests` directory.

#### When to Use the Dependencies Directory

* **Third-party libraries** specific to your test package
* **Configuration files** required by static tests
* **Resource files** like images, data files, or certificates
* **Custom assemblies** built specifically for this test package

#### Directory Organization

```txt
Dependencies/
├── libraries/
│   ├── custom-test-framework.dll    # Custom testing utilities
│   ├── data-validation.dll          # Validation libraries
│   └── protocol-helpers.dll         # Protocol-specific helpers
├── config/
│   ├── logging.config               # Logging configuration
│   ├── database.connectionstring    # Database connection settings
│   └── api-endpoints.json           # API configuration
├── resources/
│   ├── test-data.xml                # Static test data files
│   ├── certificates/                # Security certificates
│   │   ├── test-cert.pfx
│   │   └── ca-root.crt
│   └── images/
│       ├── reference-screenshot.png
│       └── ui-baseline.jpg
└── scripts/
    ├── setup-environment.ps1        # Environment setup scripts
    └── database-seed.sql             # Database initialization
```

### qaops.config.xml

If you need to customize the QAOps test execution behavior, adapt the `qaops.config.xml` file to configure package installation settings.

#### Configuration Options

* **Package Installation**: Control whether the dmtest package is installed on DataMiner
  * Set to `false` if your test only requires PowerShell script execution
  * Your PowerShell scripts in `TestPackagePipeline` will still execute regardless of this setting

#### Example Configuration

Create your `qaops.config.xml` file like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<QAOpsConfig>
  <!-- Set to false if you only need PowerShell script execution -->
  <PerformPackageInstallation>true</PerformPackageInstallation>
</QAOpsConfig>
```

This allows you to choose between different testing approaches:

* **Full DataMiner Tests**: Keep `PerformPackageInstallation` as `true` to install the dmtest package and run comprehensive automation tests
* **Lightweight Tests**: Set to `false` to skip package installation and run only PowerShell-based tests for faster execution
