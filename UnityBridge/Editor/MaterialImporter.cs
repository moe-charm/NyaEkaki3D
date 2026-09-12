using System;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BakeImporter
    {
        public static BakeImportResult ImportMaterial(string manifestPath,string assetParent="Assets",Transform sceneParent=null,bool createSceneInstance=false)
        {
            var source=MaterialBakeStore.Read(manifestPath);
            return ImportValidated(source.Geometry,null,assetParent,sceneParent,createSceneInstance,source,identity:BakeOutputIdentity.Read(manifestPath,source.Geometry));
        }
        internal static BakeImportResult VerifyMaterialFailure(string manifestPath,Action<string> checkpoint)
        {
            var source=MaterialBakeStore.Read(manifestPath);
            return ImportValidated(source.Geometry,null,"Assets",null,false,source,checkpoint,identity:BakeOutputIdentity.Read(manifestPath,source.Geometry));
        }
    }
}
