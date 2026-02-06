using HarmonyLib;


namespace NO_Tactitools.Core;

/// <summary>
/// A helper class to cache Harmony <see cref="Traverse"/> instances for repeatedly accessing a field on an object.
/// This improves performance by avoiding the overhead of creating a new Traverse instance every time the value is needed for the same object.
/// </summary>
/// <typeparam name="TObject">The type of the object containing the field.</typeparam>
/// <typeparam name="TValue">The type of the value stored in the field.</typeparam>
/// <param name="fieldName">The name of the field to access.</param>
/// <example>
/// <code>
/// // Define a cache for a private int field named "health" in a Player class
/// private static TraverseCache&lt;Player, int&gt; _healthCache = new TraverseCache&lt;Player, int&gt;("health");
/// 
/// public void Update() {
///     Player player = GetPlayer();
///     // Efficiently get the value
///     int currentHealth = _healthCache.GetValue(player);
/// }
/// </code>
/// </example>
public class TraverseCache<TObject, TValue>(string fieldName) where TObject : class {
    // this post was made by George Gang
    private TObject _cachedObject;
    private Traverse _traverse;

    /// <summary>
    /// Retrieves the value of the field for the specified object instance.
    /// Updates the cache if the object instance has changed since the last call.
    /// </summary>
    /// <param name="currentObject">The object instance to retrieve the value from.</param>
    /// <param name="silent">If true, suppresses the log message when the cache is updated.</param>
    /// <returns>The value of the field.</returns>
    public TValue GetValue(TObject currentObject, bool silent = false) {
        if (_traverse == null || _cachedObject != currentObject) {
            _cachedObject = currentObject;
            _traverse = Traverse.Create(currentObject).Field(fieldName);
            if (!silent)
                Plugin.Log("[TraverseCache<" + typeof(TObject).Name.ToString() + ", " + typeof(TValue).Name.ToString() + ">] "
                +"Cached field '" + fieldName.ToString() 
                + "' for object of type '" + typeof(TObject).Name.ToString() + "'.");
        }
        return _traverse.GetValue<TValue>();
    }

    /// <summary>
    /// Clears the cached object and traverse instance.
    /// </summary>
    public void Reset() {
        _cachedObject = null;
        _traverse = null;
    }
}