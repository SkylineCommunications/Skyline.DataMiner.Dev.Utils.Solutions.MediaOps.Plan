namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using SlcWorkflowIds = Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	/// <summary>
	/// Keeps the Categories app in sync with the category (job type) that is stored on a job.
	/// The category is persisted on the job itself (reusing the JobSource field), but it must also be registered as a
	/// <see cref="CategoryItem"/> so the Categories app is aware of the jobs that belong to a category.
	/// </summary>
	internal static class DomJobCategoryHandler
	{
		internal static void CreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Job> apiJobs)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			// Only touch the Categories app for jobs whose category actually changed. Unchanged jobs (including new
			// jobs without a category) produce no work.
			var changes = apiJobs
				.Select(job => new JobCategoryChange(job))
				.Where(x => x.HasChange)
				.ToList();

			if (changes.Count == 0)
			{
				return;
			}

			var jobsToAssign = changes.Where(x => x.NewCategoryId.HasValue).ToList();
			var jobsToUnassign = changes.Where(x => !x.NewCategoryId.HasValue).Select(x => x.JobId).ToList();

			AssignCategories(planApi, jobsToAssign);
			DeleteCategoryItems(planApi, jobsToUnassign);
		}

		internal static void Delete(MediaOpsPlanApi planApi, ICollection<Job> apiJobs)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			// Only jobs that actually had a category assigned can have a matching category item to remove.
			var jobIds = apiJobs
				.Where(job => !job.IsNew && Guid.TryParse(job.OriginalInstance?.JobInfo.JobSource, out _))
				.Select(job => job.Id)
				.ToList();

			DeleteCategoryItems(planApi, jobIds);
		}

		private static void AssignCategories(MediaOpsPlanApi planApi, ICollection<JobCategoryChange> jobs)
		{
			if (jobs.Count == 0)
			{
				return;
			}

			var distinctCategoryIds = jobs.Select(x => x.NewCategoryId.Value).Distinct().ToList();
			var categoriesById = planApi.Categories.Categories
				.Read(new ORFilterElement<Category>(distinctCategoryIds.Select(id => CategoryExposers.ID.Equal(id)).ToArray()))
				.ToDictionary(x => x.ID);

			// Read the existing category items so they can be updated in place instead of creating duplicates.
			var categoryItemFilter = new ORFilterElement<CategoryItem>(jobs.Select(x => BuildCategoryItemFilter(x.JobId)).ToArray());
			var existingCategoryItemsByJobId = planApi.Categories.CategoryItems
				.Read(categoryItemFilter)
				.SafeToDictionary(x => Guid.Parse(x.InstanceId), x => x);

			var categoryItemsToCreateOrUpdate = new List<CategoryItem>();
			var categoryItemsToDelete = new List<CategoryItem>();

			foreach (var job in jobs)
			{
				existingCategoryItemsByJobId.TryGetValue(job.JobId, out var existingCategoryItem);

				if (categoriesById.TryGetValue(job.NewCategoryId.Value, out var category))
				{
					if (existingCategoryItem != null)
					{
						existingCategoryItem.Category = category;
						categoryItemsToCreateOrUpdate.Add(existingCategoryItem);
					}
					else
					{
						categoryItemsToCreateOrUpdate.Add(new CategoryItem
						{
							Category = category,
							ModuleId = SlcWorkflowIds.ModuleId,
							InstanceId = job.JobId.ToString(),
						});
					}
				}
				else
				{
					planApi.Logger.Information($"Category with ID '{job.NewCategoryId.Value}' not found for job with ID '{job.JobId}'. Skipping category item assignment.");

					// Remove any dangling category item so the job is not left referencing a non-existing category.
					if (existingCategoryItem != null)
					{
						categoryItemsToDelete.Add(existingCategoryItem);
					}
				}
			}

			if (categoryItemsToCreateOrUpdate.Count > 0)
			{
				planApi.Categories.CategoryItems.CreateOrUpdate(categoryItemsToCreateOrUpdate);
			}

			if (categoryItemsToDelete.Count > 0)
			{
				planApi.Categories.CategoryItems.Delete(categoryItemsToDelete);
			}
		}

		private static void DeleteCategoryItems(MediaOpsPlanApi planApi, ICollection<Guid> jobIds)
		{
			if (jobIds.Count == 0)
			{
				return;
			}

			var filter = new ORFilterElement<CategoryItem>(jobIds.Select(BuildCategoryItemFilter).ToArray());
			var categoryItemsToDelete = planApi.Categories.CategoryItems.Read(filter);

			planApi.Categories.CategoryItems.Delete(categoryItemsToDelete);
		}

		private static FilterElement<CategoryItem> BuildCategoryItemFilter(Guid jobId)
		{
			return CategoryItemExposers.ModuleId.Equal(SlcWorkflowIds.ModuleId)
				.AND(CategoryItemExposers.InstanceId.Equal(jobId.ToString()));
		}

		private sealed class JobCategoryChange
		{
			public JobCategoryChange(Job job)
			{
				JobId = job.Id;

				OldCategoryId = !job.IsNew && Guid.TryParse(job.OriginalInstance?.JobInfo.JobSource, out var oldCategoryId)
					? oldCategoryId
					: (Guid?)null;

				NewCategoryId = Guid.TryParse(job.JobTypeCategoryId, out var newCategoryId)
					? newCategoryId
					: (Guid?)null;
			}

			public Guid JobId { get; }

			public Guid? OldCategoryId { get; }

			public Guid? NewCategoryId { get; }

			public bool HasChange => NewCategoryId != OldCategoryId;
		}
	}
}
