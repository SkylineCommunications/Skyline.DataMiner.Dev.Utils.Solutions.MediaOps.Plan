# Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan

This documentation describes how to use the public API exposed by `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`.
The API is intended to be used when developing custom solutions based on the MediaOps.PLAN solution.

## Installation

Add the NuGet package to your solution:

```bash
dotnet add package Skyline.DataMiner.Solutions.MediaOps.Plan
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
| [Getting Started](Documentation/Getting%20Started.md) | Installation, prerequisites, and basic usage |
| [Quick Reference](Documentation/Quick%20Reference.md) | Common code snippets for repositories, querying, and connectivity |
| [Advanced Topics](Documentation/Advanced%20Topics.md) | Caching, subscriptions, validation, logging, and error handling |

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