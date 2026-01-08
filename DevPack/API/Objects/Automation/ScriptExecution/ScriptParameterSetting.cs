namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a script parameter that can be assigned to an automation script.
    /// </summary>
    public class ScriptParameterSetting
    {
        /// <summary>
        /// Initializes a new instance of the ScriptParameter class.
        /// </summary>
        /// <param name="name">The name of the script parameter. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is null or empty.</exception>
        public ScriptParameterSetting(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// Gets the name of the script parameter.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets or sets the script parameter value.
        /// </summary>
        public string Value { get; set; }
    }
}
