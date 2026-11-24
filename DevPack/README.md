# Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan

This package provides the MediaOps Plan API for managing resources, workflows, jobs, properties, and relationships. It targets .NET Framework 4.8.

## Installing the DOM schema

On a client created by the Plan DevPack, call the `InstallDomModules()` extension in `Skyline.DataMiner.Solutions.MediaOps.Plan.API` to reconcile Properties, Relationships, Resource Studio, and Workflow. When deploying both schemas, run the People & Organizations installer first. Neither installing the NuGet package nor creating an API client runs this step.

See [Installation and Setup](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan/blob/main/Documentation/Advanced%20Topics.md#installation-and-setup) for permissions and reconciliation behavior.
