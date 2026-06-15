namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Connection
{
	using System;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	internal sealed class TrackedSubscriptionUpdate : ITrackedSubscriptionUpdate
	{
		private readonly Action _executeAction;
		private Action _onAfterInitialEventsAction;
		private Action _onFinishedAction;

		public TrackedSubscriptionUpdate(Action executeAction)
		{
			_executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
		}

		public int MarkerID => throw new NotImplementedException();

		public DMSMessage[] Execute()
		{
			_executeAction.Invoke();
			_onAfterInitialEventsAction?.Invoke();
			_onFinishedAction?.Invoke();

			return Array.Empty<DMSMessage>();
		}

		public DMSMessage[] ExecuteAndWait(TimeSpan? timeout = null)
		{
			var task = Task.Factory.StartNew(Execute, TaskCreationOptions.LongRunning);
			return task.Result;
		}

		public ITrackedSubscriptionUpdate OnAfterInitialEvents(Action action)
		{
			_onAfterInitialEventsAction = action ?? throw new ArgumentNullException(nameof(action));

			return this;
		}

		public ITrackedSubscriptionUpdate OnEndUpdating(Action action)
		{
			throw new NotImplementedException();
		}

		public ITrackedSubscriptionUpdate OnFinished(Action action)
		{
			_onFinishedAction = action ?? throw new ArgumentNullException(nameof(action));

			return this;
		}

		public ITrackedSubscriptionUpdate OnStage(SubscriptionStage stage, Action action)
		{
			throw new NotImplementedException();
		}

		public ITrackedSubscriptionUpdate OnStartUpdating(Action action)
		{
			throw new NotImplementedException();
		}
	}
}
