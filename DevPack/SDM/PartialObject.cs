namespace Skyline.DataMiner.SDM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Represents an entity of type <typeparamref name="T"/> of which only a selected subset of fields was read.
	/// </summary>
	/// <typeparam name="T">The type of the entity.</typeparam>
	/// <remarks>
	/// Temporary definition that mirrors the class announced by the SDM team. Remove once it ships in Skyline.DataMiner.Dev.Utils.SDM.Abstractions.
	/// The <see cref="Id"/> is always available, it doesn't need to be selected.
	/// Accessing a field that was not selected throws an <see cref="InvalidOperationException"/> with the <c>Get</c> methods
	/// and returns <see langword="false"/> with the <c>TryGet</c> methods.
	/// </remarks>
	public class PartialObject<T>
	{
		private readonly Dictionary<FieldExposer, IReadOnlyList<object>> valuesByField;

		/// <summary>
		/// Initializes a new instance of the <see cref="PartialObject{T}"/> class.
		/// </summary>
		/// <param name="id">The unique identifier of the entity.</param>
		/// <param name="valuesByField">The values per selected field. A field without values is selected but has no value.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>, or when a field does not belong to <typeparamref name="T"/> or is provided more than once.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="valuesByField"/> or one of its fields is <see langword="null"/>.</exception>
		public PartialObject(Guid id, IEnumerable<KeyValuePair<FieldExposer, IEnumerable<object>>> valuesByField)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("The identifier can't be empty.", nameof(id));
			}

			if (valuesByField == null)
			{
				throw new ArgumentNullException(nameof(valuesByField));
			}

			Id = id;
			this.valuesByField = new Dictionary<FieldExposer, IReadOnlyList<object>>(FieldExposerComparer.Instance);

			foreach (var entry in valuesByField)
			{
				FieldSelection.ValidateField<T>(entry.Key, nameof(valuesByField));

				if (this.valuesByField.ContainsKey(entry.Key))
				{
					throw new ArgumentException($"Field '{entry.Key.fieldName}' is provided more than once.", nameof(valuesByField));
				}

				this.valuesByField[entry.Key] = (entry.Value ?? Enumerable.Empty<object>()).Where(x => x != null).ToList();
			}
		}

		/// <summary>
		/// Gets the unique identifier of the entity.
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// Determines whether the specified field was selected.
		/// </summary>
		/// <param name="exposer">The exposer of the field.</param>
		/// <returns><see langword="true"/> if the field was selected; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="exposer"/> is <see langword="null"/>.</exception>
		public bool HasField(FieldExposer exposer)
		{
			if (exposer == null)
			{
				throw new ArgumentNullException(nameof(exposer));
			}

			return valuesByField.ContainsKey(exposer);
		}

		/// <summary>
		/// Gets the single value of the specified field.
		/// </summary>
		/// <typeparam name="TField">The type of the field.</typeparam>
		/// <param name="exposer">The exposer of the field.</param>
		/// <returns>The value, or the default value of <typeparamref name="TField"/> when the field has no value.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="exposer"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the field was not selected, has multiple values or a value that is not of type <typeparamref name="TField"/>.</exception>
		public TField GetValue<TField>(Exposer<T, TField> exposer)
		{
			var values = GetSelectedValues(exposer);

			if (values.Count == 0)
			{
				return default;
			}

			if (values.Count > 1)
			{
				throw new InvalidOperationException($"Field '{exposer.fieldName}' has {values.Count} values, use {nameof(GetValues)} instead.");
			}

			return Cast<TField>(exposer, values[0]);
		}

		/// <summary>
		/// Tries to get the single value of the specified field.
		/// </summary>
		/// <typeparam name="TField">The type of the field.</typeparam>
		/// <param name="exposer">The exposer of the field.</param>
		/// <param name="value">The value, or the default value of <typeparamref name="TField"/> when <see langword="false"/> is returned.</param>
		/// <returns><see langword="true"/> if the field was selected and has exactly one value of type <typeparamref name="TField"/>; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="exposer"/> is <see langword="null"/>.</exception>
		public bool TryGetValue<TField>(Exposer<T, TField> exposer, out TField value)
		{
			if (exposer == null)
			{
				throw new ArgumentNullException(nameof(exposer));
			}

			value = default;

			if (!valuesByField.TryGetValue(exposer, out var values) || values.Count != 1 || !(values[0] is TField typed))
			{
				return false;
			}

			value = typed;
			return true;
		}

		/// <summary>
		/// Gets all values of the specified field, for example when the field is part of a section that can occur multiple times.
		/// </summary>
		/// <typeparam name="TField">The type of the values.</typeparam>
		/// <param name="exposer">The exposer of the field.</param>
		/// <returns>The values, or an empty collection when the field has no value.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="exposer"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the field was not selected or has a value that is not of type <typeparamref name="TField"/>.</exception>
		public IEnumerable<TField> GetValues<TField>(Exposer<T, TField> exposer)
		{
			return GetValuesOfField<TField>(exposer);
		}

		/// <summary>
		/// Gets all values of the specified list field, for example the resources referenced by the nodes of a job.
		/// </summary>
		/// <typeparam name="TField">The type of the values.</typeparam>
		/// <param name="exposer">The exposer of the list field.</param>
		/// <returns>The values, or an empty collection when the field has no value.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="exposer"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the field was not selected or has a value that is not of type <typeparamref name="TField"/>.</exception>
		public IEnumerable<TField> GetValues<TField>(DynamicListExposer<T, TField> exposer)
		{
			return GetValuesOfField<TField>(exposer);
		}

		/// <summary>
		/// Tries to get all values of the specified field.
		/// </summary>
		/// <typeparam name="TField">The type of the values.</typeparam>
		/// <param name="exposer">The exposer of the field.</param>
		/// <param name="values">The values, or an empty collection when <see langword="false"/> is returned.</param>
		/// <returns><see langword="true"/> if the field was selected and has at least one value, all of type <typeparamref name="TField"/>; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="exposer"/> is <see langword="null"/>.</exception>
		public bool TryGetValues<TField>(Exposer<T, TField> exposer, out IEnumerable<TField> values)
		{
			return TryGetValuesOfField(exposer, out values);
		}

		/// <summary>
		/// Tries to get all values of the specified list field.
		/// </summary>
		/// <typeparam name="TField">The type of the values.</typeparam>
		/// <param name="exposer">The exposer of the list field.</param>
		/// <param name="values">The values, or an empty collection when <see langword="false"/> is returned.</param>
		/// <returns><see langword="true"/> if the field was selected and has at least one value, all of type <typeparamref name="TField"/>; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="exposer"/> is <see langword="null"/>.</exception>
		public bool TryGetValues<TField>(DynamicListExposer<T, TField> exposer, out IEnumerable<TField> values)
		{
			return TryGetValuesOfField(exposer, out values);
		}

		private IEnumerable<TField> GetValuesOfField<TField>(FieldExposer exposer)
		{
			return GetSelectedValues(exposer).Select(x => Cast<TField>(exposer, x)).ToList();
		}

		private bool TryGetValuesOfField<TField>(FieldExposer exposer, out IEnumerable<TField> values)
		{
			if (exposer == null)
			{
				throw new ArgumentNullException(nameof(exposer));
			}

			values = Enumerable.Empty<TField>();

			if (!valuesByField.TryGetValue(exposer, out var stored) || stored.Count == 0 || !stored.All(x => x is TField))
			{
				return false;
			}

			values = stored.Cast<TField>().ToList();
			return true;
		}

		private static TField Cast<TField>(FieldExposer exposer, object value)
		{
			if (value is TField typed)
			{
				return typed;
			}

			throw new InvalidOperationException($"Field '{exposer.fieldName}' has a value of type '{value.GetType().Name}' instead of '{typeof(TField).Name}'.");
		}

		private IReadOnlyList<object> GetSelectedValues(FieldExposer exposer)
		{
			if (exposer == null)
			{
				throw new ArgumentNullException(nameof(exposer));
			}

			if (!valuesByField.TryGetValue(exposer, out var values))
			{
				throw new InvalidOperationException($"Field '{exposer.fieldName}' was not selected. Add it with WithFields to read it.");
			}

			return values;
		}
	}
}
