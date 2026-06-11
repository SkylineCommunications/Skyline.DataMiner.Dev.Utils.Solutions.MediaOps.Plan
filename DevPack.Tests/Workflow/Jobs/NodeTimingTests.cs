namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	public sealed class NodeTimingTests
	{
		private static readonly DateTimeOffset PreRollStart = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
		private static readonly DateTimeOffset Start = new DateTimeOffset(2025, 1, 1, 10, 5, 0, TimeSpan.Zero);
		private static readonly DateTimeOffset CurrentTime = new DateTimeOffset(2025, 1, 1, 10, 30, 0, TimeSpan.Zero);
		private static readonly DateTimeOffset End = new DateTimeOffset(2025, 1, 1, 11, 0, 0, TimeSpan.Zero);
		private static readonly DateTimeOffset PostRollEnd = new DateTimeOffset(2025, 1, 1, 11, 5, 0, TimeSpan.Zero);

		private static readonly Guid JobId = Guid.NewGuid();

		#region Apply - scheduled states (Draft/Tentative/Confirmed)

		[DataTestMethod]
		[DataRow(JobState.Draft)]
		[DataRow(JobState.Tentative)]
		[DataRow(JobState.Confirmed)]
		public void Apply_ScheduledState_SetsNodeSpanToPreRollAndPostRoll(JobState state)
		{
			var node = NewNode();
			var graph = GraphWith(node);

			JobNodeTimingResolver.Apply(state, DefaultWindow(), CurrentTime, graph);

			Assert.AreEqual(PreRollStart, node.Start);
			Assert.AreEqual(PostRollEnd, node.End);
		}

		[TestMethod]
		public void Apply_Draft_AssignsTimingsToBrandNewNode()
		{
			var node = NewNode();
			Assert.IsTrue(node.IsNew, "Precondition: a freshly created node is new.");
			var graph = GraphWith(node);

			JobNodeTimingResolver.Apply(JobState.Draft, DefaultWindow(), CurrentTime, graph);

			Assert.AreEqual(PreRollStart, node.Start);
			Assert.AreEqual(PostRollEnd, node.End);
		}

		[TestMethod]
		public void Apply_Draft_IsIdempotentWhenReappliedWithSameWindow()
		{
			var node = NewNode();
			var graph = GraphWith(node);
			var window = DefaultWindow();

			JobNodeTimingResolver.Apply(JobState.Draft, window, CurrentTime, graph);
			var startAfterFirstApply = node.Start;
			var endAfterFirstApply = node.End;

			JobNodeTimingResolver.Apply(JobState.Draft, window, CurrentTime, graph);

			Assert.AreEqual(startAfterFirstApply, node.Start);
			Assert.AreEqual(endAfterFirstApply, node.End);
			Assert.AreEqual(PreRollStart, node.Start);
			Assert.AreEqual(PostRollEnd, node.End);
		}

		#endregion

		#region Apply - running state

		[TestMethod]
		public void Apply_Running_NewNode_StartsAtCurrentTimeAndEndsAtPostRoll()
		{
			var node = NewNode();
			var graph = GraphWith(node);

			JobNodeTimingResolver.Apply(JobState.Running, DefaultWindow(), CurrentTime, graph);

			Assert.AreEqual(CurrentTime, node.Start);
			Assert.AreEqual(PostRollEnd, node.End);
		}

		[TestMethod]
		public void Apply_Running_ExistingActiveNode_KeepsStartAndAlignsEndToPostRoll()
		{
			var extendedPostRollEnd = PostRollEnd.AddMinutes(30);
			var node = ExistingNode(Start, End);
			var graph = GraphWith(node);

			var window = Window(PreRollStart, Start, End.AddMinutes(30), extendedPostRollEnd);
			JobNodeTimingResolver.Apply(JobState.Running, window, CurrentTime, graph);

			Assert.AreEqual(Start, node.Start);
			Assert.AreEqual(extendedPostRollEnd, node.End);
		}

		[TestMethod]
		public void Apply_Running_AlreadyEndedNode_IsLeftUntouched()
		{
			var endedAt = CurrentTime.AddMinutes(-5);
			var node = ExistingNode(PreRollStart, endedAt);
			var graph = GraphWith(node);

			JobNodeTimingResolver.Apply(JobState.Running, DefaultWindow(), CurrentTime, graph);

			Assert.AreEqual(PreRollStart, node.Start);
			Assert.AreEqual(endedAt, node.End);
		}

		[TestMethod]
		public void Apply_Running_SwappedOutNode_IsTruncatedRestoredAndReplacementStartsAtCurrentTime()
		{
			var oldNode = ExistingNode(Start, PostRollEnd);
			var graph = GraphWith(oldNode);

			var newNode = NewNode();
			graph.Swap(oldNode, newNode);

			JobNodeTimingResolver.Apply(JobState.Running, DefaultWindow(), CurrentTime, graph);

			// The swapped-out node keeps its start, has its end truncated to the current time and is restored into the graph.
			Assert.AreEqual(Start, oldNode.Start);
			Assert.AreEqual(CurrentTime, oldNode.End);
			Assert.IsTrue(graph.Nodes.Contains(oldNode));

			// The replacement node runs from the current time until the post-roll end.
			Assert.AreEqual(CurrentTime, newNode.Start);
			Assert.AreEqual(PostRollEnd, newNode.End);

			Assert.AreEqual(2, graph.Nodes.Count);
		}

		#endregion

		#region Apply - frozen states (Completed/Canceled)

		[DataTestMethod]
		[DataRow(JobState.Completed)]
		[DataRow(JobState.Canceled)]
		public void Apply_FrozenState_LeavesNodeTimingsUntouched(JobState state)
		{
			var nodeStart = Start.AddMinutes(1);
			var nodeEnd = End.AddMinutes(1);
			var node = ExistingNode(nodeStart, nodeEnd);
			var graph = GraphWith(node);

			JobNodeTimingResolver.Apply(state, DefaultWindow(), CurrentTime, graph);

			Assert.AreEqual(nodeStart, node.Start);
			Assert.AreEqual(nodeEnd, node.End);
		}

		#endregion

		#region Validate - change-aware state rules

		[DataTestMethod]
		[DataRow(JobState.Draft)]
		[DataRow(JobState.Tentative)]
		[DataRow(JobState.Confirmed)]
		public void Validate_ScheduledState_AllowsTimingChange(JobState state)
		{
			var requested = Window(PreRollStart, Start.AddMinutes(1), End, PostRollEnd);

			var errors = JobNodeTimingResolver.Validate(JobId, state, requested, DefaultWindow(), CurrentTime);

			Assert.AreEqual(0, errors.Count);
		}

		[TestMethod]
		public void Validate_NewJob_WithoutOriginal_ReturnsNoErrors()
		{
			var errors = JobNodeTimingResolver.Validate(JobId, JobState.Draft, DefaultWindow(), null, CurrentTime);

			Assert.AreEqual(0, errors.Count);
		}

		[TestMethod]
		public void Validate_UnchangedWindow_ReturnsNoErrors()
		{
			var errors = JobNodeTimingResolver.Validate(JobId, JobState.Running, DefaultWindow(), DefaultWindow(), CurrentTime);

			Assert.AreEqual(0, errors.Count);
		}

		[TestMethod]
		public void Validate_Running_StartAlreadyOccurred_ReturnsChangeNotAllowed()
		{
			var requested = Window(PreRollStart, Start.AddMinutes(5), End, PostRollEnd);

			var errors = JobNodeTimingResolver.Validate(JobId, JobState.Running, requested, DefaultWindow(), CurrentTime);

			var error = errors.OfType<JobStartChangeNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(JobId, error.Id);
			Assert.AreEqual(requested.Start, error.Value);
		}

		[TestMethod]
		public void Validate_Running_PreRollAlreadyOccurred_ReturnsChangeNotAllowed()
		{
			var requested = Window(PreRollStart.AddMinutes(-5), Start, End, PostRollEnd);

			var errors = JobNodeTimingResolver.Validate(JobId, JobState.Running, requested, DefaultWindow(), CurrentTime);

			var error = errors.OfType<JobPreRollStartChangeNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(requested.PreRollStart, error.Value);
		}

		[TestMethod]
		public void Validate_Running_FutureEndMovedToPast_ReturnsChangeNotAllowed()
		{
			var requested = Window(PreRollStart, Start, CurrentTime.AddMinutes(-5), PostRollEnd);

			var errors = JobNodeTimingResolver.Validate(JobId, JobState.Running, requested, DefaultWindow(), CurrentTime);

			var error = errors.OfType<JobEndChangeNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(requested.End, error.Value);
		}

		[TestMethod]
		public void Validate_Running_FutureEndExtendedFurther_ReturnsNoErrors()
		{
			var requested = Window(PreRollStart, Start, End.AddMinutes(30), PostRollEnd.AddMinutes(30));

			var errors = JobNodeTimingResolver.Validate(JobId, JobState.Running, requested, DefaultWindow(), CurrentTime);

			Assert.AreEqual(0, errors.Count);
		}

		[DataTestMethod]
		[DataRow(JobState.Completed)]
		[DataRow(JobState.Canceled)]
		public void Validate_FrozenState_RejectsAnyTimingChange(JobState state)
		{
			var requested = Window(PreRollStart, Start, End, PostRollEnd.AddMinutes(30));

			var errors = JobNodeTimingResolver.Validate(JobId, state, requested, DefaultWindow(), CurrentTime);

			var error = errors.OfType<JobPostRollEndChangeNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(requested.PostRollEnd, error.Value);
			Assert.AreEqual("Timings of a completed or canceled job cannot be changed.", error.ErrorMessage);
		}

		#endregion

		#region ValidateTimingChainOrdering - baseline-independent ordering

		[TestMethod]
		public void ValidateTimingChainOrdering_ValidChain_ReturnsNoErrors()
		{
			var errors = JobNodeTimingResolver.ValidateTimingChainOrdering(JobId, DefaultWindow());

			Assert.AreEqual(0, errors.Count);
		}

		[TestMethod]
		public void ValidateTimingChainOrdering_PreRollAfterStart_ReturnsPreRollError()
		{
			var window = Window(Start.AddMinutes(1), Start, End, PostRollEnd);

			var errors = JobNodeTimingResolver.ValidateTimingChainOrdering(JobId, window);

			Assert.AreEqual(1, errors.Count);
			Assert.IsInstanceOfType(errors[0], typeof(JobInvalidPreRollError));
		}

		[TestMethod]
		public void ValidateTimingChainOrdering_EndBeforeStart_ReturnsTimingError()
		{
			var window = Window(PreRollStart, Start, Start.AddMinutes(-1), PostRollEnd);

			var errors = JobNodeTimingResolver.ValidateTimingChainOrdering(JobId, window);

			Assert.AreEqual(1, errors.Count);
			Assert.IsInstanceOfType(errors[0], typeof(JobInvalidTimingError));
		}

		[TestMethod]
		public void ValidateTimingChainOrdering_PostRollBeforeEnd_ReturnsPostRollError()
		{
			var window = Window(PreRollStart, Start, End, End.AddMinutes(-1));

			var errors = JobNodeTimingResolver.ValidateTimingChainOrdering(JobId, window);

			Assert.AreEqual(1, errors.Count);
			Assert.IsInstanceOfType(errors[0], typeof(JobInvalidPostRollError));
		}

		#endregion

		#region JobTimingWindow - snapshot and change detection

		[TestMethod]
		public void JobTimingWindow_FromJob_CapturesAllBoundaries()
		{
			var job = new Job
			{
				PreRollStart = PreRollStart,
				Start = Start,
				End = End,
				PostRollEnd = PostRollEnd,
			};

			var window = JobTimingWindow.FromJob(job);

			Assert.AreEqual(PreRollStart, window.PreRollStart);
			Assert.AreEqual(Start, window.Start);
			Assert.AreEqual(End, window.End);
			Assert.AreEqual(PostRollEnd, window.PostRollEnd);
		}

		[TestMethod]
		public void JobTimingWindow_GetChanges_NoChange_ReturnsAnyFalse()
		{
			var window = DefaultWindow();

			var changes = window.GetChanges(window);

			Assert.IsFalse(changes.Any);
			Assert.IsFalse(changes.AnyStartTimingChanged);
			Assert.IsFalse(changes.AnyEndTimingChanged);
		}

		[TestMethod]
		public void JobTimingWindow_GetChanges_StartSideChange_FlagsStartSideOnly()
		{
			var requested = Window(PreRollStart.AddMinutes(-1), Start, End, PostRollEnd);

			var changes = requested.GetChanges(DefaultWindow());

			Assert.IsTrue(changes.Any);
			Assert.IsTrue(changes.PreRollStartChanged);
			Assert.IsTrue(changes.AnyStartTimingChanged);
			Assert.IsFalse(changes.AnyEndTimingChanged);
			Assert.IsFalse(changes.StartChanged);
			Assert.IsFalse(changes.EndChanged);
			Assert.IsFalse(changes.PostRollEndChanged);
		}

		[TestMethod]
		public void JobTimingWindow_GetChanges_EndSideChange_FlagsEndSideOnly()
		{
			var requested = Window(PreRollStart, Start, End, PostRollEnd.AddMinutes(1));

			var changes = requested.GetChanges(DefaultWindow());

			Assert.IsTrue(changes.Any);
			Assert.IsTrue(changes.PostRollEndChanged);
			Assert.IsTrue(changes.AnyEndTimingChanged);
			Assert.IsFalse(changes.AnyStartTimingChanged);
		}

		#endregion

		#region Helpers

		private static JobResourceNode NewNode()
		{
			return new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
		}

		private static JobResourceNode ExistingNode(DateTimeOffset start, DateTimeOffset end)
		{
			var node = NewNode();
			node.Start = start;
			node.End = end;
			node.IsNew = false;
			return node;
		}

		private static NodeGraph<JobNode> GraphWith(params JobNode[] nodes)
		{
			var graph = new NodeGraph<JobNode>();
			foreach (var node in nodes)
			{
				graph.Add(node);
			}

			return graph;
		}

		private static JobTimingWindow Window(DateTimeOffset preRollStart, DateTimeOffset start, DateTimeOffset end, DateTimeOffset postRollEnd)
		{
			return new JobTimingWindow(preRollStart, start, end, postRollEnd);
		}

		private static JobTimingWindow DefaultWindow()
		{
			return Window(PreRollStart, Start, End, PostRollEnd);
		}

		#endregion
	}
}
