# Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan

This repository contains the public API of the `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan` NuGet package (C# namespace `Skyline.DataMiner.Solutions.MediaOps.Plan`), used when developing custom solutions based on the MediaOps.PLAN solution.

## Installation

Add the NuGet package to your solution:

```bash
dotnet add package Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan
```

Depending on your project type, one of the following additional packages is also required:

- Automation scripts: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Automation`
- Protocols: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.Protocol`
- GQI Ad-hoc data sources and custom operators: `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan.GQI`

> [!NOTE]
> This library targets `.NET Framework 4.8`.

## Documentation

| Document | Description |
| -------- | ----------- |
| [What's New Since 1.5](Documentation/What%27s%20New%20Since%201.5.md) | Summary of changes since 1.5.x, split into stable (1.6.x) and prerelease (1.7.0-alpha) |
| [Getting Started](Documentation/Getting%20Started.md) | Installation, prerequisites, and the core concepts (resources, jobs, workflows, recurring jobs, node graphs, properties, relationships) |
| [Quick Reference](Documentation/Quick%20Reference.md) | Copyable snippets for every repository: CRUD, job/recurring job lifecycle, node graphs, properties, relationships, querying, and synchronization |
| [Advanced Topics](Documentation/Advanced%20Topics.md) | State machines, job timing rules, configuration state, data references, eligible resources, SRM synchronization, logging, and installation checks |

External resources:

- [DataMiner Docs](https://docs.dataminer.services/) - Official DataMiner documentation

## About DataMiner

DataMiner is a transformational platform that provides vendor-independent control and monitoring of devices and services. Out of the box and by design, it addresses key challenges such as security, complexity, multi-cloud, and much more. It has a pronounced open architecture and powerful capabilities enabling users to evolve easily and continuously.

The foundation of DataMiner is its powerful and versatile data acquisition and control layer. With DataMiner, there are no restrictions to what data users can access. Data sources may reside on premises, in the cloud, or in a hybrid setup.

A unique catalog of 7000+ connectors already exists. In addition, you can leverage DataMiner Development Packages to build your own connectors (also known as "protocols" or "drivers").

> **Note**
> See also: [About DataMiner](https://aka.dataminer.services/about-dataminer).

## About Skyline Communications

At Skyline Communications, we deal with world-class solutions that are deployed by leading companies around the globe. Check out [our proven track record](https://aka.dataminer.services/about-skyline) and see how we make our customers' lives easier by empowering them to take their operations to the next level.
