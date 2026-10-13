using System.Collections.Generic;

namespace SkyPulse.Mobile
{
    // A press is an edge, not a held state. The same fence protects pooled
    // puzzle targets and legacy Input touch/mouse polling at scene boundaries.
    public sealed class SkyPulseInputFence
    {
        readonly HashSet<int> held=new HashSet<int>();
        int blockedThroughFrame=-1,lastAcceptedFrame=-1;
        float lastTouchAt=-100f;
        public void ObserveTouch(float now){lastTouchAt=now;}
        public bool Press(int pointer,int frame,float now,bool touch)
        {
            if(touch)ObserveTouch(now);
            if(frame<=blockedThroughFrame || frame==lastAcceptedFrame || held.Contains(pointer) || !touch&&now-lastTouchAt<.45f)return false;
            held.Add(pointer);lastAcceptedFrame=frame;return true;
        }
        public void Release(int pointer){held.Remove(pointer);}
        public void ReleaseAll(){held.Clear();}
        public void Boundary(int frame){blockedThroughFrame=frame;}
    }
}
