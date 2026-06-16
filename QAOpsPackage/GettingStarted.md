# Getting Started with Skyline DataMiner DevOps

Welcome to the Skyline DataMiner DevOps environment!
This quick-start guide will help you get up and running.
For more details and comprehensive instructions, please visit [DataMiner Docs](https://docs.dataminer.services/).

## Creating a DataMiner test package

This project is configured to create a .dmtest file every time you build the project.

When you compile or build the project, you will find the generated .dmtest in the standard output folder, which is typically the *bin* folder of your project.

When you publish the project, a corresponding item will be created in the online DataMiner Catalog.

## Adding extra artifacts in the same solution

You can right-click the solution and select *Add* > *New Project*. This will allow you to select DataMiner project templates (e.g. adding additional Automation scripts).

> [!NOTE]
> Connectors are currently not supported for this.

You can also add new projects by using the dotnet-cli. For the sake of stability, we recommend always using an *sln* solution with all projects included.

```bash
    dotnet new sln
    dotnet new dataminer-user-defined-api-project -o MyUserDefinedApiFromGithub -auth MyName
    dotnet sln add MyUserDefinedApiFromGithub
```

Every *Skyline.DataMiner.SDK* project within the solution, except other DataMiner package projects, will by default be included within the .dmapp created by this project. You can customize this behavior using the *PackageContent/ProjectReferences.xml* file. This allows you to add filters to include or exclude projects as needed.

## Importing from DataMiner

You can import specific items directly from a DataMiner Agent using DIS:

1. In Visual Studio, connect to an Agent via *Extensions* > *DIS* > *DMA* > *Connect*.

1. If your Agent is not listed, add it by going to *Extensions* > *DIS* > *Settings* and clicking *Add* on the DMA tab.

1. Once connected, import the DataMiner artifacts you want:

   1. In your *Solution Explorer*, navigate to folders such as *PackageContent/Dashboards* or *PackageContent/LowCodeApps*.
   1. Right-click, and select *Add*.
   1. Select e.g. *Import DataMiner Dashboard/Low-Code App*, depending on what you want to import.

## Adding content from the Catalog

You can reference and include additional content from the Catalog using the *PackageContent/CatalogReferences.xml* file provided in this project.

For the SDK to be able to download the referenced items from the Catalog, configure a user secret in Visual Studio:

1. Obtain an *Organization Key* from [admin.dataminer.services](https://admin.dataminer.services/) with the following scopes:
   - *Register catalog items*
   - *Read catalog items*
   - *Download catalog versions*

1. Securely store the key using Visual Studio User Secrets:

   1. Right-click the project and select *Manage User Secrets*.

   1. Add the key in the following format:

      ```json
      { 
        "skyline": {
          "sdk": {
            "dataminertoken": "MyKeyHere"
          }
        }
      }
      ```

## Executing additional code on installation

Open the `QAOpsPackage.cs` file to write custom installation code. Common actions include creating elements, services, or views.

> [!TIP]
> Type `clGetDms` in the .cs file and press Tab twice to insert a snippet that gives you access to the *IDms* classes, making DataMiner manipulation easier.

## Adding configuration files

If your installation code needs configuration files (e.g. .json, .xml), you can add these to the *SetupContent* folder, which can be accessed during installation.

Access them in your code using:

```csharp
string setupContentPath = installer.GetSetupContentDirectory();
```


## Publishing to the Catalog

This project was created with support for publishing to the DataMiner Catalog. You can publish your artifact manually through Visual Studio or by setting up a CI/CD workflow.

You can adjust the information displayed in the Catalog by modifying the `README.md`, the `manifest.yml`, or the images in the `CatalogInformation` folder.

To add a custom icon, place an image named `custom-icon` in the `images` folder, using one of the supported extensions: `.jpg`, `.jpeg`, `.png`, `.bmp`, `.tif`, `.tiff`, or `.webp`.
## Enabling Publishing to the Catalog

**OOPS!** This project was created without support for publishing to the Catalog.
If you intended to publish to the Catalog, you may have set up the project incorrectly.

Please consider the following options:

- Remove this project and create a new DataMiner Test Package Project with .dmtest creation and Catalog support enabled.
- If Catalog publishing is not required, review your project setup to ensure it aligns with your goals.

