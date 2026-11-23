# Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan

This package provides the MediaOps Plan API for managing resources, workflows, jobs, properties, and relationships. It targets .NET Framework 4.8.

## Installing the DOM schema

On a client created by the Plan DevPack, call the `InstallDomModules()` extension in `Skyline.DataMiner.Solutions.MediaOps.Plan.API` to reconcile Properties, Relationships, Resource Studio, and Workflow. When deploying both schemas through the DevPacks, run the People & Organizations installer first. Neither installing the NuGet package nor creating an API client runs this step.

Use the `2.0.X` installer only with a compatible schema. It does not include Flow Rules and can soft-delete fields from a newer schema. See [Installation and Setup](https://github.com/SkylineCommunications/Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan/blob/2.0.X/Documentation/Advanced%20Topics.md#installation-and-setup) for additional guidance.
