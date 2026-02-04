using HarmonyLib;


namespace NO_Tactitools.Core;

public class TraverseCache<TObject, TValue>(string fieldName) where TObject : class {
    // this post was made by George Gang
    private TObject _cachedObject;
    private Traverse _traverse;

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

    public void Reset() {
        _cachedObject = null;
        _traverse = null;
    }
}