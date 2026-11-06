namespace RT_MediaOps.Plan.Storage.DOM
{
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcProperties;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcRelationships;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	[TestClass]
	[TestCategory("UnitTest")]
	public sealed class DomSchemaParityTests
	{
		// Independently captured from SLC-S-MediaOps.Plan at 7a79bf3768337de562d06bd1bde0a6c062951fe5.
		// Resource Studio's Flow Rules delta is from ab6c315db41a3f0b1a682059108ccdfb2d3f4f90.
		// P&O also matches the user-approved Rysomez.zip export (Collaboration excluded).
		// Hashes cover complete schema objects, including nested fields and status rules.
		// See DomSchemaSnapshot for the only excluded platform/serialization metadata.
		[TestMethod]
		[DataRow("(slc)people_organizations", "ModuleSettings", "(slc)people_organizations", "68ef510671667d63fe323639e8b3d1248f323760d00471d8ea08aae061b36497")]
		[DataRow("(slc)people_organizations", "DomBehaviorDefinitions", "1636e21e-f44d-4343-8a09-c09780399c74", "2f6ca67bb82700092044626ab4681a71ec5ffa7cf127a73c6534219b84e9f9f5")]
		[DataRow("(slc)people_organizations", "DomBehaviorDefinitions", "80136b8f-4c76-4b7c-a1dc-526d3a9bdf5a", "9de2343e473644b0be011ddddc69cec278e968b360f7a9911080aa8b40540187")]
		[DataRow("(slc)people_organizations", "DomBehaviorDefinitions", "a7b03edc-0a89-4f02-9fa3-5458cd11d43e", "bbc9f4eacc2707036339b0ec980ba1f0f467581fe860ddd59f1a91f2abf4ecb4")]
		[DataRow("(slc)people_organizations", "DomDefinitions", "1624a01d-de2e-4c36-849c-3ee048a971ee", "cda06f3f4e684940f272346a318468f9fe8fd0cdff0d9d8b8770b65c8bca738f")]
		[DataRow("(slc)people_organizations", "DomDefinitions", "5150bc87-7e63-4b41-a839-64da947a1986", "e82788edd55abea3c465bd5047a5bd5a416be619a3c2cd4f02f473c6feced0a9")]
		[DataRow("(slc)people_organizations", "DomDefinitions", "6474a13e-3de9-42f8-b3d9-aa49a1fcab7d", "2c712a8cbe717e46d608b3b04a95ab10afd17b0e3251cf982ea54b9e5ff50a0f")]
		[DataRow("(slc)people_organizations", "DomDefinitions", "86b3dff8-764e-4378-9d19-de4bc8de05c2", "0585435505b149d9af5ed4f2c6659538662ed1a26f2cd09625dc03a0c2a7ca7d")]
		[DataRow("(slc)people_organizations", "DomDefinitions", "bc1fe15f-a85d-4601-ab26-0329a9acf61a", "045abfbfc2b42d02c810e242c8528a0ee10f48fb5dba576bb0fa780aead1c577")]
		[DataRow("(slc)people_organizations", "DomDefinitions", "c769a960-e401-4cb6-8099-b0563b270c52", "056320910372ffddbebf85e40b33b6a0d61d1fc6a1448b64c0318dcda2d2eb29")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "03ba0fd8-114a-4f97-b392-7ac8a1f76803", "230f145f777ddffd968c3bd6a87a740438cf03b66c47789104fb5dd2a7922828")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "0c47b6fe-c60f-4378-a85c-3f98dc431828", "84eae9ca8df5a9aeabbd850122bbdf100e44e7209fee71fe1ad259bd2cc778c3")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "141a95ca-4b5b-4086-b329-209ffe8f1af8", "1ceb73e98c51dcdd9dc65da057c0be72a8ed485aafeceef8fca0b0664f0b72f2")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "21ffec53-130a-4a66-9eb3-50194d2c1e43", "d40b9fb259f2faca2cd150006f055e17763dae3c8d905203d43f4d02abfb8e8a")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "30aef0e9-658a-436a-ad71-6a48d88faa0b", "62901b08cfc6642227512078bfa4c1c05248c722677ae141043a9f4752a26d5a")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "397134f8-19eb-4eec-9021-8b1af51d0c1b", "3d74d0fda4902bb93673cf9b5341e0cb641b8cf46cb0f7505f9bb2144e06cca6")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "5c4be963-44ea-4ae2-911f-164f92534cb1", "c165818335374165ff2daf2fb11704fd5024e3eb203b966b83bb620a449edcf8")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "8341db0f-59bf-4a71-a124-67f8c5fbbb06", "7d1db540c3fa5677db5ee6d6cfeef7a5fcbdb8e4abf287c544c1de9a64480d1e")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "b4abf511-028c-42c1-b0d0-dc750043f1d2", "e906bde8086fb45e702c922e2c5fe2fe23774b3d72f4187cbefac8537a9274e7")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "ceadbd12-73f7-474e-9d1f-effbc9d81684", "e96031ba3d6669af0268afea666ce66c6638ea0603692e0e4ed666fb787ab4cc")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "e47d729d-1658-41bc-8931-88f4c055b955", "9d7967ef9c416b22ba52672d98644a4ffa0407d2754e7b95f45a052b2c629179")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "f38e6d21-e481-4348-b639-445a510592ec", "f1f2348bf1a5155e78ac71341754e41a081caa98c16f1558ff362642f9758f26")]
		[DataRow("(slc)people_organizations", "SectionDefinitions", "fcf83fa0-0d38-4fe6-ad55-4d36e4457f91", "db08af06460d38681791bd7ff9286dd15a97a354b271d1b57112d680b9a0dd7a")]
		[DataRow("(slc)properties", "ModuleSettings", "(slc)properties", "be9508bd6b343f32564ea9b40605894965592ed99b4a6ce8bac41f55e1024255")]
		[DataRow("(slc)properties", "DomDefinitions", "e6d22478-3415-4795-9b00-6f2a85a610e3", "01df2bd2fa686b717bf13f784de5c3c1686641adadc9ea6782dfc81f57e79b35")]
		[DataRow("(slc)properties", "DomDefinitions", "f2c92f3c-1a57-4f2d-a403-dd8968d15202", "714289769f930176245af5568f9b20423c7d820fb764699df69f60a681cef076")]
		[DataRow("(slc)properties", "SectionDefinitions", "2997b2a1-d651-4f93-b0db-4de54fb36d35", "2377f1d15117a0166429974ff2f23e2dc4e23f64ab3c9701c0d39d826daa18df")]
		[DataRow("(slc)properties", "SectionDefinitions", "4654bab7-bffb-4b9a-abb1-9b7048baa187", "e44caac6af6b4c13276d4779dc528a22b96522d960520181e6ab10d3cba2b3a7")]
		[DataRow("(slc)properties", "SectionDefinitions", "8354c390-f0d9-478d-a8bf-c4677d646a2d", "0a1fb7d4fd2ffb8d109216c5abbd16b7bc7505b63a03cb179ead49f04ae683da")]
		[DataRow("(slc)properties", "SectionDefinitions", "abcd2029-3e87-4182-bf8e-23b824ea6fba", "b803d5c7c6e2a0427d7798611abf796fcf7ecbb83a163f89b4b7bb8c0d316da6")]
		[DataRow("(slc)properties", "SectionDefinitions", "dd8a31be-7bf8-402f-85b9-3eeb5d0eec5e", "405fb1fd0d904aa2c124291baa27b1098ac9c1efaf10d85388653b181631a2f5")]
		[DataRow("(slc)relationships", "ModuleSettings", "(slc)relationships", "9498290fd73b43c3da41b3c3bb80ce0df7abdcb3e9ebffd494736126055bfc19")]
		[DataRow("(slc)relationships", "DomDefinitions", "7ab57219-6b21-4abb-83a4-f5c22de38f20", "27ac670c116023181e2ab1d3bf6a3a8717a4993795a79b320dbd3c121f3191f3")]
		[DataRow("(slc)relationships", "DomDefinitions", "fcedb769-e515-414b-8e64-7093104f6d66", "5e6fe1ec549915331e73d5d5393b36a9997bf53e4743da614cf68161f1db1b9c")]
		[DataRow("(slc)relationships", "SectionDefinitions", "ac97c003-884e-48d7-9ff3-e84b13a349be", "3f35de3431c88fa95137e32c43f262d82bf61d8a31aed3bef6e0fdd2f8bee9be")]
		[DataRow("(slc)relationships", "SectionDefinitions", "f903f60d-bfdf-4316-b138-22b5a1c9c890", "c401cec6d314a787d31a66f2cc1cfc22930bdea54493818a32893dcdd7be1d5f")]
		[DataRow("(slc)resource_studio", "ModuleSettings", "(slc)resource_studio", "9ab53b33a645ecc6bbbdc498c66f6adbf7beb62cd618c04c79aa708c9bca3ebc")]
		[DataRow("(slc)resource_studio", "DomBehaviorDefinitions", "6bac6e39-e58d-43c0-b354-81119f5828dc", "8babb3e5093a3bb3f07dadac4965034760cbfddabeaeccc11ade668a8f005891")]
		[DataRow("(slc)resource_studio", "DomBehaviorDefinitions", "cc539721-a544-415b-a45c-8ce7ed102975", "132994ea40e68a31e93a0df26df40446f5456b59ceaf9a8818ee39be734b7444")]
		[DataRow("(slc)resource_studio", "DomDefinitions", "60450534-b5bd-43c2-b088-37e171725fa0", "a4751d9e714a64ef25b3ee258990484daae4d1d71925938ad7c42889cc1fee6a")]
		[DataRow("(slc)resource_studio", "DomDefinitions", "a8262bd8-6f6c-47d4-964e-87de26d1e32e", "13c902f1447272ce97aef33b8e94ea90dba0929a5385dc89540d3a672790de1c")]
		[DataRow("(slc)resource_studio", "DomDefinitions", "d2afc1dc-f39c-49c8-a70f-9120bfbfc0a0", "cb4b91a2ac8b5347b5034052eb950c7279279d6b1f057cd9ef518e1402e16a15")]
		[DataRow("(slc)resource_studio", "DomDefinitions", "dad3646a-caaa-4971-b550-c618e562f5cc", "eba6034697316a28a768fd13944bd4c3973a5601dd82fd6fe1c23308bb736214")]
		[DataRow("(slc)resource_studio", "DomDefinitions", "f9314dd0-a74f-407d-9c85-8b2e6a99e63b", "fe40d8d86ea16fef46ad406fd7968afad3f698fa08685fbb7b03d81f5e1ddba4")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "08995f51-a59d-4ac9-a224-9481dbc4c782", "d0403de49c4ac6459616772341961a799b3165edaad1165c7470101aab9bfeff")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "09bd71e9-813b-45d8-8110-e6068d5038e1", "e7a8fb4c4a7e5aef78dcb1129c5165e5188ce1e5744ffed426f4c294099b6a1f")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "2377a750-f9e6-4a91-9d5c-4b9a4bee25ac", "6c66541e65088f97d9e46278316d86b00f19fffd3ba9bb81fcc0363aa4a3d964")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "2e079f3e-6f71-46bf-8fc8-d4203ad448a6", "09eac2c1c5e1b2491945432b0a514bf6e764beb0073a19d2ac00a29444453a4a")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "5837f8d5-96a9-4570-8bf2-89f39dd9d404", "6457ac0221c7f351bcd6117077678e8336d03f05701fbfdc24698e80519ac750")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "5a5c6591-a833-48d5-8fd4-10733cb1eddb", "8f41df062db9881df30372eee1b370e7e731fe40bd1941311c7f52e800a1b0cd")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "5e15622c-bc1e-4e84-a996-f7086c1e9b92", "79db1a5ffb3da411ae950bad6fef35f7e6dcc7628c7c9d0fd08c39faabee56e3")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "6477c6b5-34df-4379-86ed-1d098f42d0e7", "0bade92a41cb9af455c23b8bd335b896ea7441f913c56126c7d007e2249d3d78")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "702ec27e-46b7-4edf-a3ea-42aa26a8aa24", "87de5702115b0236af6816c244c44301ce1a0176685cd738ef3f22178ed28b8e")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "7dbc545c-6c08-4964-a945-568cb4fabe02", "60f2079e89837665355eedc2ea435d7d92001cb89a8fd302dd7cc994c0161aae")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "8d6d47d1-493d-4dec-9add-6203a908d7e6", "7889c16e150f7f2c0415df90b1080b4ac6948265a537bdb871efa23bd5d28855")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "930c7bca-fff9-49f7-b141-1cba4aa28afd", "b9a042dded26b5910a391364c0936290cf10d6573d173c0d1e977686640e41d9")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "962d3326-b0d5-4d03-9f53-bba208996af7", "614c7733e3f33c4d7f0195728028f84a222e935e1d7383f5d9a522d65a81ccf2")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "972a33ff-fec9-4172-87b8-5523a49a49a5", "48c12a816eff22f3ff8c25a9e5f71641b154ef582945766d1b17e31963d2adce")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "9a61f4e9-fbda-444d-9380-faecd866b0d6", "9f011724c7bb236c7a7b918ad350d3e66618c650976654e1aef839ffabda1d3c")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "a40b6d82-494a-4dd3-a37d-2e813dc1973d", "a25149ada301802a323ddebd089d4b5cc8486721c833311599e125c624a9f966")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "a78570b8-317b-419b-8969-ce1271f29908", "f24cd43ae87ecd2e5be8a7743d160148928fe059b6e9b7e9834e575b9c8a5aef")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "c1d7fa6b-7a9c-4096-9ccc-f5a6571bb6c5", "2e9fee81acf36c852ee452ec668dbb57a1af7f0be4d8beb8558653a24fda483b")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "c26bfeef-41b7-4ab5-b26f-5213c8801b71", "a536e1145b9bc6278a4430ad45f459710a1c6238061a0146f5a5e4bd6d40dc22")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "c81204b6-0c99-4f2b-8112-5184b2fda31f", "74343f8a81ea7482c10e056cd5048a304a836f1ae107872bd6ab62e523e0c64b")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "c94edccb-c079-4e61-90df-ac3d19d96fc5", "42aa2412d5abd929928a72e70368792ee745c2031468ba8671da6392776e642e")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "ff481176-1bbe-4216-ad77-214fd50ebdf3", "46d3d94394169360bdd7b1eb3ebad3040eeeaea00eca5202df99f8969dc335ca")]
		[DataRow("(slc)resource_studio", "SectionDefinitions", "fffb2bf2-61c2-441b-9ce1-7624681f26cb", "a04645922e6d6693137bde2f57513e4ed3c0889b8e58857162e7f4e72e5c9c88")]
		[DataRow("(slc)workflow", "ModuleSettings", "(slc)workflow", "b1b60b105b141ad2713bb041d01bb2d3704d957d3c483a4daa422fac713fd93d")]
		[DataRow("(slc)workflow", "DomBehaviorDefinitions", "4db4650b-c81b-48df-b48f-d6f47e69a829", "5a396210bad7a948209fbdd36f53a7a0a6f11a2e621c78ce5660ef5b91184523")]
		[DataRow("(slc)workflow", "DomBehaviorDefinitions", "6ee464d6-d84b-4d92-9135-ed95a6f69e87", "012cb351638a41ef294a61ecb3a076f3b1b4a7fd21e2e54e83291f0d500f2757")]
		[DataRow("(slc)workflow", "DomBehaviorDefinitions", "7d764b5d-37fc-4b74-b9a3-22183b4162f7", "acea7a9d035bd87b4f1de3377ad3ebf96399086062d1d4c07bd39e7ba65c0248")]
		[DataRow("(slc)workflow", "DomDefinitions", "03a014b3-0a74-4abd-aefd-ead1f50c27d3", "9c963adcc27c4644f1a8b3c3de23a3823b7907f62e5226439e17081d3054c561")]
		[DataRow("(slc)workflow", "DomDefinitions", "094b7503-2cd8-4e86-a189-c33076cabfbc", "37eb9d0a75120a4eb5dfdd501e83fab610c957fd2f6a07a50428eea8a999f9ab")]
		[DataRow("(slc)workflow", "DomDefinitions", "89828ce8-7647-440e-81fa-6de9d24019b3", "77742747aabf53489a24123170d0d9fd25273ff2f4f7ba2fd84bbade547d139f")]
		[DataRow("(slc)workflow", "DomDefinitions", "aec69c0d-8f7c-44a7-aa21-6aa741ea6135", "c3c25e9c7122baf5a6f5b1a40e90843cf1cefdc45ca459583f7fb1490fa764f4")]
		[DataRow("(slc)workflow", "DomDefinitions", "b5106b24-851f-4525-9dbc-72b1c0f18b7a", "ea52c10ae0dc2347d0a784a841bcd98928732ff069cd4d3365745c6dcdc738ab")]
		[DataRow("(slc)workflow", "DomDefinitions", "e1ed621e-197d-4003-95f2-5c0722e37c75", "3c6321dc1a529dd4bda2bff1a8a06022e3bb7f538551d06d643f9abb81b16b64")]
		[DataRow("(slc)workflow", "DomDefinitions", "e4bd1568-1bac-4e72-913c-a175e2267c91", "a9d8e78bbd353876039d0fc54fa8c1fb0f72d9b7f809b6b10b7fc3235fa26730")]
		[DataRow("(slc)workflow", "DomDefinitions", "e811303b-7bd3-48bf-a7e1-ae8aad9247f4", "54a6b0c9f9ccdb2d3f62c0e2c0257467f5cba736527a1b50ef483a5c62941d17")]
		[DataRow("(slc)workflow", "SectionDefinitions", "19f2d7c6-ed16-4e05-bd02-78016a4c8a9d", "85c4197fba1856755e3454d70060a19999515b22a4a1b3f85572b85ce7cf2afd")]
		[DataRow("(slc)workflow", "SectionDefinitions", "22f75d97-9bf3-4d90-b769-f48c0fce025f", "beda8805c68a3d085b4066be762b7c6f68459bb299a5e7dc4719dcfe43cd784f")]
		[DataRow("(slc)workflow", "SectionDefinitions", "28a99ebf-efd3-4046-9592-35e448f54e6a", "de7cfc18df10be139300385dfefa86e065094ad44963237ee6537bcacf983532")]
		[DataRow("(slc)workflow", "SectionDefinitions", "3a4fc9fc-7a4d-4840-a79a-7a889a0d6a42", "5e0f856bc2cdbe5533a671eddf1727de153c89a6c1b3e85c023f82d2feeb7e0c")]
		[DataRow("(slc)workflow", "SectionDefinitions", "43a36804-3409-4498-8620-eaec80238fcc", "e06900d20599bffa43bde8c533518f9092bc69f2ce48d7ece76132ce45f677b7")]
		[DataRow("(slc)workflow", "SectionDefinitions", "5d0df0b7-38ee-4297-851c-df6b616bddaa", "5f9e8f467eafbb2b31c99396fe755947dcc0343612432065263cefc93c763b17")]
		[DataRow("(slc)workflow", "SectionDefinitions", "6f964b2c-9682-4a44-9c91-bc3a5871a794", "c132298a3ae6f367ea622e091556d1ee2a013c5d61f8bb55a05f57b6c2b63315")]
		[DataRow("(slc)workflow", "SectionDefinitions", "717f9a98-a705-4740-86ec-05204ef6dd28", "51459afd6af70283ffc95db8e9c11d26c9b2fceb2eef746ece26705a8c0bc5c9")]
		[DataRow("(slc)workflow", "SectionDefinitions", "7467b898-571c-4dfd-b5e9-cf444480a12f", "b5907a780a46f4fe61fc69639e5ce9b74379dd105afb8fc12086f92449553bb4")]
		[DataRow("(slc)workflow", "SectionDefinitions", "74d2d947-36be-41f0-babd-9a5b986d928b", "718fa13d96b241096a516327ec9ddae64fcccf102be4bf205b1600a589b796c8")]
		[DataRow("(slc)workflow", "SectionDefinitions", "76908a6e-09bc-490a-ba34-2e7e23acd450", "128856d562883322e7631ff01e1aaee951b00b8ddc076b887f8887a14567acc0")]
		[DataRow("(slc)workflow", "SectionDefinitions", "86ffc829-5846-4737-b5fd-a2411c8bb884", "addaafac60884b955676215371118d24e49bb1aa25099349dec5103c768b5333")]
		[DataRow("(slc)workflow", "SectionDefinitions", "8e46264a-8100-49a1-bcef-076490b25da1", "54dc1cac4814bbd144382fc50c0966955a7e6f4117a47af06a1f8a7b9b9a90d3")]
		[DataRow("(slc)workflow", "SectionDefinitions", "b41107d1-0e7f-4089-8162-990d47377d4c", "0078469231eb6cab5101070fe992c75fd62f111df9795cbe2a459cf4be7491b0")]
		[DataRow("(slc)workflow", "SectionDefinitions", "b8ccf030-5d0c-42ac-8a00-06f430bbedf2", "e7c6c4a5ef6182acec71605348417ada06510587f69343fc0f5dd5326c1ad48b")]
		[DataRow("(slc)workflow", "SectionDefinitions", "bce8a3ba-4f5d-4a8b-840f-dfb5940b8d4c", "904c7bf526617b9c69338ebf4cd39bcce40d01d97e3dd39eaa0d348e7529efc3")]
		[DataRow("(slc)workflow", "SectionDefinitions", "bd8f007f-fc61-47e5-83f8-0e708217db29", "b26c61f16e406f56dbc2c93e11505c3802b2e3535a750bacfa3787c0c35533bf")]
		[DataRow("(slc)workflow", "SectionDefinitions", "dd1d3774-c681-47d3-950a-3783e3533bf9", "c01cc670d3377ba3a4e1927a8da3814b5002349303e5fc74c8bae5ad275e93df")]
		[DataRow("(slc)workflow", "SectionDefinitions", "f81c94fc-a43d-4f77-9e3d-d9a34dc9cf27", "170cb25cb53024320a21e94a6500fefb35d305325f98a15112d797fa76ec19c5")]
		[DataRow("(slc)workflow", "SectionDefinitions", "fd2df49f-05a5-4bc8-9072-2e19dd88d145", "6d18b336bed14f954cc81f2b0e9bf920669e5c7fd2f1f0831c6f73b999bd5667")]
		public void Declaration_MatchesApprovedSchema(string moduleId, string kind, string id, string expectedHash)
		{
			var module = CreateModule(moduleId);
			var json = kind switch
			{
				"ModuleSettings" => module.ModuleSettings.ToJson(),
				"DomDefinitions" => module.Definitions.Single(x => x.Definition.ID.Id == Guid.Parse(id)).Definition.ToJson(),
				"DomBehaviorDefinitions" => module.BehaviorDefinitions.Single(x => x.ID.Id == Guid.Parse(id)).ToJson(),
				"SectionDefinitions" => module.Definitions.SelectMany(x => x.SectionDefinitions).Concat(module.AdditionalSectionDefinitions)
					.First(x => x.GetID().Id == Guid.Parse(id)).ToJson(),
				_ => throw new ArgumentException("Unknown schema object kind.", nameof(kind)),
			};

			Assert.AreEqual(expectedHash, DomSchemaSnapshot.Hash(json), $"{moduleId}/{kind}/{id}: {DomSchemaSnapshot.Canonicalize(json)}");
		}

		private static IDomModuleInfo CreateModule(string moduleId)
		{
			return moduleId switch
			{
				"(slc)people_organizations" => new SlcPeopleOrganizationsDomModule(),
				"(slc)properties" => new SlcPropertiesDomModule(),
				"(slc)relationships" => new SlcRelationshipsDomModule(),
				"(slc)resource_studio" => new SlcResourceStudioDomModule(),
				"(slc)workflow" => new SlcWorkflowDomModule(),
				_ => throw new ArgumentException("Unknown module ID.", nameof(moduleId)),
			};
		}
	}
}
