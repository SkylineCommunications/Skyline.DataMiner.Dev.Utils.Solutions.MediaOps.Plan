namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;

	using SLDataGateway.API.Types.Querying;

	internal class WorkflowsRepository : Repository, IWorkflowsRepository
	{
		private readonly WorkflowFilterTranslator filterTranslator = new WorkflowFilterTranslator();

		public WorkflowsRepository(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		public Workflow Complete(Workflow workflow)
		{
			if (workflow == null)
			{
				throw new ArgumentNullException(nameof(workflow));
			}

			return Complete(workflow.Id);
		}

		public Workflow Complete(Guid workflowId)
		{
			var workflow = Read(workflowId);
			if (workflow == null)
			{
				return null;
			}

			if (!DomWorkflowHandler.TryComplete(PlanApi, [workflow], out var result))
			{
				result.ThrowSingleException(workflow.Id);
			}

			return new Workflow(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Workflow> Complete(IEnumerable<Workflow> workflows)
		{
			if (workflows == null)
			{
				throw new ArgumentNullException(nameof(workflows));
			}

			return Complete(workflows.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<Workflow> Complete(IEnumerable<Guid> workflowIds)
		{
			if (workflowIds == null)
			{
				throw new ArgumentNullException(nameof(workflowIds));
			}

			var workflows = Read(workflowIds.ToArray());
			if (workflows == null || !workflows.Any())
			{
				return Array.Empty<Workflow>();
			}

			if (!DomWorkflowHandler.TryComplete(PlanApi, workflows.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Workflow(PlanApi, x)).ToList();
		}

		public long Count()
		{
			return Count(new TRUEFilterElement<Workflow>());
		}

		public long Count(FilterElement<Workflow> filter)
		{
			if (filter.isEmpty())
			{
				return 0;
			}

			throw new NotImplementedException();
		}

		public long Count(IQuery<Workflow> query)
		{
			return Count(query.Filter);
		}

		public IReadOnlyCollection<Workflow> Create(IEnumerable<Workflow> oToCreate)
		{
			if (oToCreate == null)
			{
				throw new ArgumentNullException(nameof(oToCreate));
			}

			var list = oToCreate.ToList();

			var existing = list.Where(x => !x.IsNew);
			if (existing.Any())
			{
				throw new InvalidOperationException("Not possible to use method Create for existing workflows. Use CreateOrUpdate or Update instead.");
			}

			if (!DomWorkflowHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Workflow(PlanApi, x)).ToList();
		}

		public Workflow Create(Workflow oToCreate)
		{
			if (oToCreate == null)
			{
				throw new ArgumentNullException(nameof(oToCreate));
			}

			if (!oToCreate.IsNew)
			{
				throw new InvalidOperationException("Not possible to use method Create for existing workflow. Use CreateOrUpdate or Update instead.");
			}

			if (!DomWorkflowHandler.TryCreateOrUpdate(PlanApi, [oToCreate], out var result))
			{
				result.ThrowSingleException(oToCreate.Id);
			}

			return new Workflow(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Workflow> CreateOrUpdate(IEnumerable<Workflow> oToCreateOrUpdate)
		{
			if (oToCreateOrUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToCreateOrUpdate));
			}

			var list = oToCreateOrUpdate.ToList();

			if (!DomWorkflowHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Workflow(PlanApi, x)).ToList();
		}

		public void Delete(Guid apiObjectId)
		{
			var toDelete = Read(apiObjectId);
			if (toDelete == null)
			{
				return;
			}

			if (!DomWorkflowHandler.TryDelete(PlanApi, [toDelete], out var result))
			{
				result.ThrowSingleException(toDelete.Id);
			}
		}

		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			var toDelete = Read(apiObjectIds.ToArray());

			if (!DomWorkflowHandler.TryDelete(PlanApi, toDelete?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}
		}

		public void Delete(IEnumerable<Workflow> oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Select(x => x.Id).ToArray());
		}

		public void Delete(Workflow oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		public IEnumerable<Workflow> Read(FilterElement<Workflow> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<Workflow>();
			}

			return ActivityHelper.Track(nameof(WorkflowsRepository), nameof(Read), act =>
			{
				var domFilter = filterTranslator.Translate(filter);
				IEnumerable<Workflow> Iterator()
				{
					foreach (var domWorkflow in PlanApi.DomHelpers.SlcWorkflowHelper.GetWorkflows(domFilter))
					{
						yield return new Workflow(PlanApi, domWorkflow);
					}
				}

				return Iterator();
			});
		}

		public IEnumerable<Workflow> Read(IQuery<Workflow> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return Read(query.Filter);
		}

		public IEnumerable<Workflow> Read()
		{
			return Read(new TRUEFilterElement<Workflow>());
		}

		public Workflow Read(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentNullException(nameof(id));
			}

			return ActivityHelper.Track(nameof(WorkflowsRepository), nameof(Read), act =>
			{
				act?.AddTag("WorkflowId", id);
				var workflow = Read(WorkflowExposers.Id.Equal(id)).FirstOrDefault();

				if (workflow == null)
				{
					act?.AddTag("Hit", false);
					return null;
				}

				act?.AddTag("Hit", true);
				return workflow;
			});
		}

		public IEnumerable<Workflow> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<Workflow>();
			}

			return Read(new ORFilterElement<Workflow>(ids.Select(x => WorkflowExposers.Id.Equal(x)).ToArray()));
		}

		public IEnumerable<SDM.IPagedResult<Workflow>> ReadPaged()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Workflow>> ReadPaged(int pageSize)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Workflow>> ReadPaged(FilterElement<Workflow> filter)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Workflow>> ReadPaged(IQuery<Workflow> query)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Workflow>> ReadPaged(FilterElement<Workflow> filter, int pageSize)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Workflow>> ReadPaged(IQuery<Workflow> query, int pageSize)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyCollection<Workflow> Update(IEnumerable<Workflow> oToUpdate)
		{
			if (oToUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToUpdate));
			}

			var list = oToUpdate.ToList();

			var newWorkflows = list.Where(x => x.IsNew);
			if (newWorkflows.Any())
			{
				throw new InvalidOperationException("Not possible to use method Update for new workflows. Use Create or CreateOrUpdate instead.");
			}

			if (!DomWorkflowHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Workflow(PlanApi, x)).ToList();
		}

		public Workflow Update(Workflow oToUpdate)
		{
			if (oToUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToUpdate));
			}

			if (oToUpdate.IsNew)
			{
				throw new InvalidOperationException("Not possible to use method Update for new workflow. Use Create or CreateOrUpdate instead.");
			}

			if (!DomWorkflowHandler.TryCreateOrUpdate(PlanApi, [oToUpdate], out var result))
			{
				result.ThrowSingleException(oToUpdate.Id);
			}

			return new Workflow(PlanApi, result.SuccessfulItems.Single());
		}
	}
}
