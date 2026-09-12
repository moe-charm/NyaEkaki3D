using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Display-only UV mapping. The same transform drives drawing and picking.</summary>
    sealed class UvViewportTransform
    {
        public float Zoom { get; private set; }=1;
        public Vector2 Pan { get; private set; }
        public void Reset() { Zoom=1;Pan=Vector2.zero; }
        public Vector2 ToLocal(Vector2 uv,float size)=>new Vector2(12,12)+Pan+new Vector2(uv.x,1-uv.y)*(size*Zoom);
        public Vector2 ToUv(Vector2 local,float size)
        {
            var normalized=(local-new Vector2(12,12)-Pan)/(size*Zoom);
            return new Vector2(normalized.x,1-normalized.y);
        }
        public void Translate(Vector2 delta) { Pan+=delta; }
        public void ZoomAt(Vector2 local,float size,float wheel)
        {
            if(size<=0 || float.IsNaN(wheel) || float.IsInfinity(wheel)) return;
            var anchor=ToUv(local,size);
            Zoom=Mathf.Clamp(Zoom*Mathf.Exp(Mathf.Clamp(-wheel*.08f,-4,4)),.1f,32);
            Pan+=local-ToLocal(anchor,size);
        }
    }
}
