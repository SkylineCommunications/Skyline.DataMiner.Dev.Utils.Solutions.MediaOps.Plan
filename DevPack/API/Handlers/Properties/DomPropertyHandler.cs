namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	using DomPropertyDefinition = Storage.DOM.SlcProperties.PropertyInstance;


	internal class DomPropertyHandler : DomInstanceApiObjectValidator<DomPropertyDefinition>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomPropertyHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Property> apiProperties, out DomInstanceBulkOperationResult<PropertyInstance> result)
		{
			var handler = new DomPropertyHandler(planApi);
			handler.CreateOrUpdate(apiProperties);

			result = new DomInstanceBulkOperationResult<DomPropertyDefinition>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Property> apiProperties, out DomInstanceBulkOperationResult<PropertyInstance> result, PropertyDeleteOptions options = null)
		{
			var handler = new DomPropertyHandler(planApi);
			handler.Delete(apiProperties, options ?? PropertyDeleteOptions.GetDefaults());

			result = new DomInstanceBulkOperationResult<DomPropertyDefinition>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var toCreate = apiProperties.Where(x => x.IsNew).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateNames(apiProperties);
			ValidateTypeSpecifics(apiProperties);
		}

		private void Delete(ICollection<Property> apiProperties, PropertyDeleteOptions options)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}
		}

		private void ValidateIdsNotInUse(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiProperties.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (objectsRequiringValidation.Count == 0)
			{
				return;
			}

			var objectsWithDuplicateIds = objectsRequiringValidation
				.GroupBy(o => o.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.ToList();

			foreach (var property in objectsWithDuplicateIds)
			{
				var error = new PropertyDuplicateIdError
				{
					ErrorMessage = $"Property '{property.Name}' has a duplicate ID.",
					Id = property.Id,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcPropertiesHelper.GetPropertiesInstances(objectsRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, $"ID is already in use by a Properties instance.", [foundInstance.ID.Id]);

				var error = new PropertyIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateNames(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiProperties.ToList();

			foreach (var property in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
			{
				var error = new PropertyInvalidNameError
				{
					ErrorMessage = "Name cannot be empty.",
					Id = property.Id,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}

			foreach (var property in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
			{
				var error = new PropertyInvalidNameError
				{
					ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = property.Id,
					Name = property.Name,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}

			var objectsWithDuplicateNames = objectsRequiringValidation
				.GroupBy(property => property.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var property in objectsWithDuplicateNames)
			{
				var error = new PropertyDuplicateNameError
				{
					ErrorMessage = $"Property '{property.Name}' has a duplicate name.",
					Id = property.Id,
					Name = property.Name,
				};

				ReportError(property.Id, error);
			}
		}

		private void ValidateTypeSpecifics(ICollection<Property> apiProperties)
		{
			ValidateStringProperties(apiProperties.OfType<StringProperty>().ToList());
			ValidateDiscreteProperties(apiProperties.OfType<DiscreteProperty>().ToList());
			ValidateFileProperties(apiProperties.OfType<FileProperty>().ToList());
		}

		private void ValidateStringProperties(ICollection<StringProperty> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			foreach (var property in apiProperties)
			{
				if (property.SizeLimit <= 0)
				{
					var error = new PropertyInvalidStringSizeLimitError
					{
						ErrorMessage = "Size limit must be greater than 0.",
						SizeLimit = property.SizeLimit,
						Id = property.Id,
					};

					ReportError(property.Id, error);
					continue;
				}

				if (property.SizeLimit > 8000)
				{
					var error = new PropertyInvalidStringSizeLimitError
					{
						ErrorMessage = "Size limit cannot exceed 8000.",
						SizeLimit = property.SizeLimit,
						Id = property.Id,
					};

					ReportError(property.Id, error);
					continue;
				}

				if (!string.IsNullOrEmpty(property.DefaultValue)
					&& !InputValidator.HasValidTextLength(property.DefaultValue, property.SizeLimit))
				{
					var error = new PropertyInvalidStringDefaultValueError
					{
						ErrorMessage = $"Default value exceeds maximum length of {property.SizeLimit} characters.",
						Id = property.Id,
						DefaultValue = property.DefaultValue,
					};

					ReportError(property.Id, error);
				}
			}
		}

		private void ValidateDiscreteProperties(ICollection<DiscreteProperty> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			foreach (var property in apiProperties)
			{
				if (property.Discretes.Count == 0)
				{
					var error = new PropertyNoDiscretesError
					{
						ErrorMessage = "Empty discretes list is not allowed.",
						Id = property.Id,
					};

					ReportError(property.Id, error);
					continue;
				}

				var discretesWithInvalidLength = property.Discretes.Where(d => !InputValidator.HasValidTextLength(d)).ToList();
				if (discretesWithInvalidLength.Count > 0)
				{
					var error = new PropertyDiscreteInvalidLengthError
					{
						ErrorMessage = $"{discretesWithInvalidLength.Count} discrete(s) exceed(s) the maximum length of {InputValidator.DefaultMaxTextLength} characters.",
						Id = property.Id,
						InvalidDiscretes = discretesWithInvalidLength,
					};

					ReportError(property.Id, error);
					continue;
				}

				var duplicateDiscretes = property.Discretes
					.GroupBy(x => x.Trim())
					.Where(g => g.Count() > 1)
					.SelectMany(g => g)
					.ToList();

				if (duplicateDiscretes.Count != 0)
				{
					var error = new PropertyDuplicateDiscretesError
					{
						ErrorMessage = $"The property defines the following duplicate discretes: {String.Join(", ", duplicateDiscretes)}.",
						Id = property.Id,
						Discretes = duplicateDiscretes,
					};

					ReportError(property.Id, error);
				}
			}
		}

		private void ValidateFileProperties(ICollection<FileProperty> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			throw new NotImplementedException();
		}
	}
}
