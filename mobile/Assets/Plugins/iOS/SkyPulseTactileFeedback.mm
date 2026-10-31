// UIKit stays on its main thread. No Unity callbacks, input events or save writes.
#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>

namespace {
    static UIImpactFeedbackGenerator *softFeedback;
    static UIImpactFeedbackGenerator *rigidFeedback;
    static __weak UIView *feedbackView;

    static UIView *ActiveView()
    {
        UIApplication *app=UIApplication.sharedApplication;
        if(app.applicationState!=UIApplicationStateActive)return nil;
        for(UIScene *scene in app.connectedScenes)
        {
            if(scene.activationState!=UISceneActivationStateForegroundActive ||
               ![scene isKindOfClass:UIWindowScene.class])continue;
            for(UIWindow *window in ((UIWindowScene *)scene).windows)
                if(window.isKeyWindow)return window.rootViewController.view ?: window;
        }
        return nil;
    }
    static bool EnsureGenerators()
    {
        UIView *view=ActiveView();if(!view)return false;
        if(softFeedback && rigidFeedback && feedbackView==view)return true;
        if(@available(iOS 17.5,*))
        {
            // Modern generators attach to the actual active view. If the host
            // view changes, retire the old interactions instead of accumulating.
            if(feedbackView)
            {
                if(softFeedback)[feedbackView removeInteraction:(id<UIInteraction>)softFeedback];
                if(rigidFeedback)[feedbackView removeInteraction:(id<UIInteraction>)rigidFeedback];
            }
            softFeedback=[UIImpactFeedbackGenerator feedbackGeneratorWithStyle:UIImpactFeedbackStyleSoft forView:view];
            rigidFeedback=[UIImpactFeedbackGenerator feedbackGeneratorWithStyle:UIImpactFeedbackStyleRigid forView:view];
        }
        else
        {
            // Deployment remains iOS15; style-only construction supports that
            // existing device range while the preferred API is used on17.5+.
            softFeedback=[[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleSoft];
            rigidFeedback=[[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleRigid];
        }
        feedbackView=view;return true;
    }
    static void OnMain(void (^action)(void))
    {
        if(NSThread.isMainThread){action();return;}
        // A busy/background transition must not replay stale tactile events.
        CFAbsoluteTime submitted=CFAbsoluteTimeGetCurrent();
        dispatch_async(dispatch_get_main_queue(),^{
            if(CFAbsoluteTimeGetCurrent()-submitted<.12)action();
        });
    }
}
extern "C" void SkyPulseTactilePrepare(void)
{
    OnMain(^{if(EnsureGenerators()){[softFeedback prepare];[rigidFeedback prepare];}});
}
extern "C" void SkyPulseTactilePulse(int impact)
{
    OnMain(^{
        if(!EnsureGenerators())return;
        UIImpactFeedbackGenerator *generator=impact?rigidFeedback:softFeedback;
        [generator impactOccurredWithIntensity:impact?.68:.55];
        // Apple recommends preparing again when another event may follow soon.
        // The system lets this prepared state expire automatically when idle.
        [generator prepare];
    });
}
