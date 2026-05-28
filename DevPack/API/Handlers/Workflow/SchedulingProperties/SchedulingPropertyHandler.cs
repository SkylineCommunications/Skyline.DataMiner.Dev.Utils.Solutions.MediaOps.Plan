namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using DomProperty = Storage.DOM.SlcProperties.PropertyInstance;

	internal class SchedulingPropertyHandler : DomInstanceApiObjectValidator<DomProperty>
	{
		private readonly MediaOpsPlanApi planApi;

		private SchedulingPropertyHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		public static string Scope => "MediaOps";

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Property> apiProperties, out DomInstanceBulkOperationResult<DomProperty> result)
		{
			var handler = new SchedulingPropertyHandler(planApi);
			handler.CreateOrUpdate(apiProperties);

			result = new DomInstanceBulkOperationResult<DomProperty>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Property> apiProperties, out DomInstanceBulkOperationResult<DomProperty> result, PropertyDeleteOptions options = null)
		{
			var handler = new SchedulingPropertyHandler(planApi);
			handler.Delete(apiProperties, options ?? PropertyDeleteOptions.GetDefaults());

			result = new DomInstanceBulkOperationResult<DomProperty>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

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

			var toAssign = apiProperties.Where(x => x.IsNew && string.IsNullOrEmpty(x.Scope)).ToList();
			var toValidate = apiProperties.Except(toAssign).ToList();

			ValidateScopes(toValidate);
			AssignScopes(toAssign);

			DomPropertyHandler.TryCreateOrUpdate(planApi, apiProperties.Where(IsValid).ToList(), out var result);

			foreach (var id in result.UnsuccessfulIds)
			{
				ReportError(id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(id, traceData);
				}
			}

			ReportSuccess(result.SuccessfulItems);
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

			ValidateScopes(apiProperties);

			DomPropertyHandler.TryDelete(planApi, apiProperties.Where(IsValid).ToList(), out var result, options);

			foreach (var id in result.UnsuccessfulIds)
			{
				ReportError(id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(id, traceData);
				}
			}

			ReportSuccess(result.SuccessfulItems);
		}

		private void AssignScopes(ICollection<Property> apiProperties)
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
				property.AssignScope(Scope);
			}
		}

		private void ValidateScopes(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			foreach (var property in apiProperties.Where(x => x.Scope != Scope))
			{
				var error = new SchedulingPropertyInvalidScopeError
				{
					ErrorMessage = $"Property with Id '{property.Id}' has invalid Scope '{property.Scope}'. Expected Scope is '{Scope}'.",
					Scope = property.Scope,
					Id = property.Id,
				};

				ReportError(property.Id, error);
			}
		}
	}
}
