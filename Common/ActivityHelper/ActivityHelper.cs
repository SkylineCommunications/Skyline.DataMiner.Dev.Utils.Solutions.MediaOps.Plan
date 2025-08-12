namespace Skyline.DataMiner.MediaOps.Plan.ActivityHelper
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides helper methods for tracking activities using OpenTelemetry.
    /// </summary>
    public static class ActivityHelper
    {
        /// <summary>
        /// The name of the OpenTelemetry source for the MediaOps Plan API.
        /// </summary>
        public static readonly string ApiSourceName = "Skyline.DataMiner.MediaOps.Plan.API";
        internal static readonly ActivitySource ActivitySource = new ActivitySource(ApiSourceName);

        /// <summary>
        /// Starts an activity with the specified name and executes the provided action within that activity context.
        /// </summary>
        /// <param name="className">The name of the class from which logic is tracked.</param>
        /// <param name="methodName">The name of the method from which logic is tracked.</param>
        /// <param name="action">The action to track.</param>
        public static void Track(string className, string methodName, Action<Activity> action)
        {
            using var act = ActivitySource.StartActivity($"{className}.{methodName}", ActivityKind.Server);
            try
            {
                action(act);
            }
            catch (Exception ex)
            {
                act?.AddTag("error", "true");
                act?.AddEvent(new ActivityEvent("exception", default, new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName
},
                    { "exception.message", ex.Message },
                    { "exception.stacktrace", ex.StackTrace }
                }));

                throw;
            }
        }

        /// <summary>
        /// Starts an activity with the specified name and executes the provided function within that activity context, returning the result.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="className">The name of the class from which logic is tracked.</param>
        /// <param name="methodName">The name of the method from which logic is tracked.</param>
        /// <param name="func">The function to track.</param>
        /// <returns></returns>
        public static T Track<T>(string className, string methodName, Func<Activity, T> func)
        {
            using var act = ActivitySource.StartActivity($"{className}.{methodName}", ActivityKind.Server);
            try
            {
                return func(act);
            }
            catch (Exception ex)
            {
                act?.AddTag("error", "true");
                act?.AddEvent(new ActivityEvent("exception", default, new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                    { "exception.stacktrace", ex.StackTrace }
                }));

                throw;
            }
        }
    }
}