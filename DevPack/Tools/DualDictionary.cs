namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Tools
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a dictionary that stores items by both a key and a name.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class DualDictionary<TKey, TValue>
    {
        /// <summary>
        /// A dictionary to store items by their key.
        /// </summary>
        private readonly ConcurrentDictionary<TKey, TValue> byKey = new ConcurrentDictionary<TKey, TValue>();

        /// <summary>
        /// A dictionary to store items by their name.
        /// </summary>
        private readonly ConcurrentDictionary<string, TValue> byName = new ConcurrentDictionary<string, TValue>();

        /// <summary>
        /// A function to extract the name of an item.
        /// </summary>
        private readonly Func<TValue, string> getName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DualDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="getName">A function that returns the name of an item given the value.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="getName"/> is null.</exception>
        public DualDictionary(Func<TValue, string> getName)
        {
            this.getName = getName ?? throw new ArgumentNullException(nameof(getName));
        }

        /// <summary>
        /// Gets a collection containing the items.
        /// </summary>
        public IEnumerable<TValue> Values => byKey.Values;

        /// <summary>
        /// Adds an item to the dictionary.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        /// <param name="item">The item to add.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when an item with the same key or name already exists.
        /// </exception>
        public void Add(TKey key, TValue item)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var name = getName(item);

            if (name == null)
            {
                throw new ArgumentException("Item name cannot be null.");
            }

            // Check for duplicates
            if (KeyExists(key))
            {
                throw new ArgumentException($"An item with the key '{key}' already exists.");
            }

            if (NameExists(name))
            {
                throw new ArgumentException($"An item with the name '{name}' already exists.");
            }

            // Add to dictionaries
            if (!byKey.TryAdd(key, item))
            {
                throw new InvalidOperationException($"Failed to add item with key '{key}'.");
            }

            if (!byName.TryAdd(name, item))
            {
                // Rollback the addition to `byKey` to maintain consistency
                byKey.TryRemove(key, out _);
                throw new InvalidOperationException($"Failed to add item with name '{name}'.");
            }
        }

        /// <summary>
        /// Attempts to add an item to the dictionaries by its key and name.
        /// </summary>
        /// <param name="key">The key of the item to add.</param>
        /// <param name="item">The item to add.</param>
        /// <returns>
        /// True if the item was successfully added to both dictionaries; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the key or item is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the item's name is null or empty.
        /// </exception>
        public bool TryAdd(TKey key, TValue item)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var name = getName(item);

            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Item name cannot be null or empty.", nameof(item));
            }

            // Ensure no conflicts exist
            if (KeyExists(key) || NameExists(name))
            {
                return false;
            }

            // Attempt to add the item to both dictionaries
            if (byKey.TryAdd(key, item))
            {
                if (byName.TryAdd(name, item))
                {
                    return true;
                }
                else
                {
                    // Rollback the addition to byKey if byName fails
                    byKey.TryRemove(key, out _);
                }
            }

            return false;
        }

        /// <summary>
        /// Adds or updates an item in the dictionary.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        /// <param name="item">The item to add or update.</param>
        public void AddOrUpdate(TKey key, TValue item)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (KeyExists(key))
            {
                Update(key, item);
            }
            else
            {
                Add(key, item);
            }
        }

        /// <summary>
        /// Updates an existing item in the dictionary.
        /// </summary>
        /// <param name="key">The key associated with the item to update.</param>
        /// <param name="item">The updated item.</param>
        /// <exception cref="ArgumentException">Thrown when an item with the given key does not exist or an item with the same name already exists.</exception>
        public void Update(TKey key, TValue item)
        {
            if (!TryGetByKey(key, out var currentItem))
            {
                throw new ArgumentException($"An item with key '{key}' does not exist.");
            }

            var newName = getName(item);
            var currentName = getName(currentItem);

            if (newName == null)
            {
                throw new ArgumentException("The item's name cannot be null.");
            }

            if (newName.Equals(currentName))
            {
                // Name unchanged, simple update
                byKey[key] = item;
                byName[newName] = item;
            }
            else
            {
                // Check if the new name is already in use
                if (NameExists(newName))
                {
                    throw new ArgumentException($"An item with the name '{newName}' already exists.");
                }

                // Update the dictionaries
                byKey[key] = item;
                byName[newName] = item;

                // Remove the old name
                if (!byName.TryRemove(currentName, out _))
                {
                    throw new InvalidOperationException($"Failed to remove the old name '{currentName}'.");
                }
            }
        }

        /// <summary>
        /// Retrieves an item by its key.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        /// <returns>The item associated with the specified key.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the key is not found.</exception>
        public TValue GetByKey(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (byKey.TryGetValue(key, out TValue value))
            {
                return value;
            }

            throw new KeyNotFoundException("Key not found.");
        }

        /// <summary>
        /// Retrieves an item by its name.
        /// </summary>
        /// <param name="name">The name associated with the item.</param>
        /// <returns>The item associated with the specified name.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the name is not found.</exception>
        public TValue GetByName(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }

            if (byName.TryGetValue(name, out TValue value))
            {
                return value;
            }

            throw new KeyNotFoundException("Name not found.");
        }

        /// <summary>
        /// Tries to retrieve an item by its key.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        /// <param name="value">When this method returns, contains the item if found; otherwise, the default value for the type of the item.</param>
        /// <returns><see langword="true"/> if the item was found; otherwise, <see langword="false"/>.</returns>
        public bool TryGetByKey(TKey key, out TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return byKey.TryGetValue(key, out value);
        }

        /// <summary>
        /// Tries to retrieve an item by its name.
        /// </summary>
        /// <param name="name">The name associated with the item.</param>
        /// <param name="value">When this method returns, contains the item if found; otherwise, the default value for the type of the item.</param>
        /// <returns><see langword="true"/> if the item was found; otherwise, <see langword="false"/>.</returns>
        public bool TryGetByName(string name, out TValue value)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }

            return byName.TryGetValue(name, out value);
        }

        /// <summary>
        /// Removes an item from the dictionary by its key.
        /// </summary>
        /// <param name="key">The key of the item to remove.</param>
        public void RemoveByKey(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (byKey.TryRemove(key, out TValue value))
            {
                string name = getName(value);
                if (name != null)
                {
                    byName.TryRemove(name, out _);
                }
            }
        }

        /// <summary>
        /// Checks if an item with the specified key exists in the dictionary.
        /// </summary>
        /// <param name="key">The key to check for existence.</param>
        /// <returns>
        /// True if an item with the specified key exists; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the key is null.</exception>
        public bool KeyExists(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return byKey.ContainsKey(key);
        }

        /// <summary>
        /// Checks if an item with the specified name exists in the dictionary.
        /// </summary>
        /// <param name="name">The name to check for existence.</param>
        /// <returns>
        /// True if an item with the specified name exists; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public bool NameExists(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }

            return byName.ContainsKey(name);
        }
    }
}
