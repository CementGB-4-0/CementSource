using System.Collections.Generic;
using CementGB.Mod.Utilities;
using Il2CppCostumes;
using UnityEngine.AddressableAssets;

namespace CementGB.Mod.CustomContent.Costumes;

public class CustomCostumeRefHolder(AssetReferenceT<CostumeObject> dataRef)
{
    public CostumeObject Data
    {
        get
        {
            if (_costumeData) return _costumeData;
            var costumeDataHandle = dataRef.LoadAssetAsync<CostumeObject>();
            costumeDataHandle.HandleSynchronousAddressableOperation();
            _costumeData = costumeDataHandle.Result;
            _costumeData._uid = CostumeDatabase.Instance.NewUID();
            return _costumeData;
        }
    }

    public bool IsValid(string primaryKey) => Data && Data.name == primaryKey && Data.CostumeItems.Length > 0;
    
    private CostumeObject _costumeData;
    
    private Dictionary<string, AssetReference> _optionalRefs = new(); // TODO: Make all ref holders have a base class/interface they all inherit from
}