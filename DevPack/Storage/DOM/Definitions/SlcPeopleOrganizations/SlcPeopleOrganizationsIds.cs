namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Sections;

	// This module deliberately owns its identities here, without a dependency on
	// the P&O DevPack or the Plan instance wrappers, so it can be extracted later.
	internal static class SlcPeopleOrganizationsIds
	{
		internal const string ModuleId = "(slc)people_organizations";

		internal static class Definitions
		{
			internal static DomDefinitionId People { get; } = new(new Guid("1624a01d-de2e-4c36-849c-3ee048a971ee")) { ModuleId = ModuleId };
			internal static DomDefinitionId Teams { get; } = new(new Guid("5150bc87-7e63-4b41-a839-64da947a1986")) { ModuleId = ModuleId };
			internal static DomDefinitionId Experience { get; } = new(new Guid("6474a13e-3de9-42f8-b3d9-aa49a1fcab7d")) { ModuleId = ModuleId };
			internal static DomDefinitionId Role { get; } = new(new Guid("86b3dff8-764e-4378-9d19-de4bc8de05c2")) { ModuleId = ModuleId };
			internal static DomDefinitionId Category { get; } = new(new Guid("bc1fe15f-a85d-4601-ab26-0329a9acf61a")) { ModuleId = ModuleId };
			internal static DomDefinitionId Organizations { get; } = new(new Guid("c769a960-e401-4cb6-8099-b0563b270c52")) { ModuleId = ModuleId };
		}

		internal static class Sections
		{
			internal static class RoleInformation
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("03ba0fd8-114a-4f97-b392-7ac8a1f76803")) { ModuleId = ModuleId };
				internal static FieldDescriptorID Role { get; } = new(new Guid("4f63fdbd-a40c-482f-b8ff-8b20c701b481"));
			}

			internal static class Billing
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("0c47b6fe-c60f-4378-a85c-3f98dc431828")) { ModuleId = ModuleId };
				internal static FieldDescriptorID Email { get; } = new(new Guid("2c15790f-bc55-4170-a010-2b04bc0c0477"));
				internal static FieldDescriptorID Phone { get; } = new(new Guid("af20db8c-f972-47eb-8fb2-761cbfce58e9"));
				internal static FieldDescriptorID StreetAddress { get; } = new(new Guid("7bb6ce04-3298-4bfa-b70f-0b84735900f8"));
				internal static FieldDescriptorID City { get; } = new(new Guid("d72e8805-d539-42ca-8553-4aaa0b2267e9"));
				internal static FieldDescriptorID Zip { get; } = new(new Guid("ba7f898d-80a1-4c2a-a5c0-b7fe591535e2"));
				internal static FieldDescriptorID Country { get; } = new(new Guid("7f58452b-df20-4e58-927e-be9533d6f15f"));
			}

			internal static class OrganizationInformation
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("141a95ca-4b5b-4086-b329-209ffe8f1af8")) { ModuleId = ModuleId };
				internal static FieldDescriptorID Name { get; } = new(new Guid("88b88d71-b123-4f56-8aae-a40df08ac875"));
				internal static FieldDescriptorID Category { get; } = new(new Guid("ff3ed352-de62-4a1f-ad03-303b4778585e"));
				internal static FieldDescriptorID Icon { get; } = new(new Guid("b4a4365f-6148-46ac-be62-31a3d49a1a7f"));
			}

			internal static class Contracts
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("21ffec53-130a-4a66-9eb3-50194d2c1e43")) { ModuleId = ModuleId };
				internal static FieldDescriptorID Contract { get; } = new(new Guid("a12295ee-465e-4b0a-9152-4ed92d4f1508"));
			}

			internal static class Team
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("30aef0e9-658a-436a-ad71-6a48d88faa0b")) { ModuleId = ModuleId };
				internal static FieldDescriptorID TeamReference { get; } = new(new Guid("144d3379-1393-47ca-a35a-0facc9cf4127"));
				internal static FieldDescriptorID TeamRole { get; } = new(new Guid("444f1e2e-4a66-49af-9a94-07d4783324ec"));
			}

			internal static class Organization
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("397134f8-19eb-4eec-9021-8b1af51d0c1b")) { ModuleId = ModuleId };
				internal static FieldDescriptorID OrganizationReference { get; } = new(new Guid("57695f03-2643-4303-82f5-8164291747f3"));
			}

			internal static class ExperienceInformation
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("5c4be963-44ea-4ae2-911f-164f92534cb1")) { ModuleId = ModuleId };
				internal static FieldDescriptorID Experience { get; } = new(new Guid("065992de-67f1-4d34-b470-759e5b64b088"));
			}

			internal static class ResourcePool
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("8341db0f-59bf-4a71-a124-67f8c5fbbb06")) { ModuleId = ModuleId };
				internal static FieldDescriptorID LinkedResourcePool { get; } = new(new Guid("73a606b5-fd80-4ea0-819d-5a3b8de60323"));
			}

			internal static class TeamInformation
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("b4abf511-028c-42c1-b0d0-dc750043f1d2")) { ModuleId = ModuleId };
				internal static FieldDescriptorID Name { get; } = new(new Guid("33d47f2a-8e2d-4bbf-93e4-d478208f8678"));
				internal static FieldDescriptorID Email { get; } = new(new Guid("7c2bd233-8e15-459b-9f24-55ed80e904b9"));
				internal static FieldDescriptorID Description { get; } = new(new Guid("25bf4968-ee66-4443-9e8a-8df5d7355b12"));
				internal static FieldDescriptorID Bookable { get; } = new(new Guid("9b79a28d-fb4c-4ca4-9a97-ac8b1353ffc3"));
				internal static FieldDescriptorID Icon { get; } = new(new Guid("0100b348-bfd0-47df-972c-10f46222e2cc"));
				internal static FieldDescriptorID Skills { get; } = new(new Guid("c6a22574-2b96-4cdd-b357-1c7ed221f797"));
			}

			internal static class Resource
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("ceadbd12-73f7-474e-9d1f-effbc9d81684")) { ModuleId = ModuleId };
				internal static FieldDescriptorID LinkedResource { get; } = new(new Guid("6339a363-ec0f-42bb-86aa-4858e984b7d8"));
			}

			internal static class CategoryInformation
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("e47d729d-1658-41bc-8931-88f4c055b955")) { ModuleId = ModuleId };
				internal static FieldDescriptorID Category { get; } = new(new Guid("c86376b4-c309-4d08-8b0e-83ec69809d58"));
			}

			internal static class PeopleInformation
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("f38e6d21-e481-4348-b639-445a510592ec")) { ModuleId = ModuleId };
				internal static FieldDescriptorID FullName { get; } = new(new Guid("a7fc4201-9af5-4b29-a9b8-df24d6334d08"));
				internal static FieldDescriptorID ExperienceLevel { get; } = new(new Guid("de319ab8-ea47-42cd-a674-c47762ca24f9"));
				internal static FieldDescriptorID ProfileImage { get; } = new(new Guid("aeb3be52-849a-41d6-afc9-c6c53ef38097"));
				internal static FieldDescriptorID Skills { get; } = new(new Guid("42ffa004-0729-44b3-a864-14cc0fc5fab4"));
			}

			internal static class ContactInfo
			{
				internal static SectionDefinitionID Id { get; } = new(new Guid("fcf83fa0-0d38-4fe6-ad55-4d36e4457f91")) { ModuleId = ModuleId };
				internal static FieldDescriptorID Email { get; } = new(new Guid("cf8ae3ad-a363-499b-b99c-8ffcabcb5abb"));
				internal static FieldDescriptorID Phone { get; } = new(new Guid("2f683912-7e68-4fa3-862f-5e71ad7d1633"));
				internal static FieldDescriptorID StreetAddress { get; } = new(new Guid("05096697-68b8-4082-b9ec-d77b709377ea"));
				internal static FieldDescriptorID City { get; } = new(new Guid("dd2dcc95-0540-4e01-880b-1d57d5bb9f24"));
				internal static FieldDescriptorID Country { get; } = new(new Guid("1ace7be9-0d34-4d3a-a6f4-fb9c79adc199"));
				internal static FieldDescriptorID Zip { get; } = new(new Guid("8477ba67-09c2-49af-9743-9e43ce08228a"));
			}
		}

		internal static class Behaviors
		{
			internal static class People
			{
				internal static DomBehaviorDefinitionId Id { get; } = new(new Guid("1636e21e-f44d-4343-8a09-c09780399c74")) { ModuleId = ModuleId };
				internal const string Draft = "6dff1f60-7dd0-4f5b-90c4-9d57d016837f";
				internal const string Active = "a24206da-82e0-4d6e-919a-480ecbaf69be";
				internal const string Deprecated = "ac9a7d7c-a7e9-4f95-a901-83011c3e2c32";
				internal const string Edit = "b054b3c5-1b2e-4330-9e05-7229a1940fd3";
			}

			internal static class Organizations
			{
				internal static DomBehaviorDefinitionId Id { get; } = new(new Guid("80136b8f-4c76-4b7c-a1dc-526d3a9bdf5a")) { ModuleId = ModuleId };
				internal const string Draft = "d30e85a3-c1f3-47c4-acab-1f10bd9b6183";
				internal const string Active = "1957a4b0-e265-4dca-a304-4c0911f8a4a4";
				internal const string Deprecated = "c2f1fcf9-9697-4b80-8260-4e07fcbdb4a4";
				internal const string Edit = "ff1ad67d-7d01-4f32-8161-724995cc88fe";
			}

			internal static class Teams
			{
				internal static DomBehaviorDefinitionId Id { get; } = new(new Guid("a7b03edc-0a89-4f02-9fa3-5458cd11d43e")) { ModuleId = ModuleId };
				internal const string Draft = "62a27e81-5dd2-44f4-b381-3071c16d0469";
				internal const string Active = "ee06eef1-8e3a-444d-a0d3-adc44b1993e1";
				internal const string Deprecated = "d9dff92b-4fd2-4e63-b238-2f102d734378";
				internal const string Edit = "e78b3fd0-d82e-4cd2-ae2a-12a9792644fe";
			}
		}
	}
}
