//
//  HwAdsInterface.m
//  iOS_SDK_V9.2.0
//
//  Created by cuili qi on 2022/4/28.
//

#import "HwAdsInterface.h"
#import <FirebaseAnalytics/FirebaseAnalytics.h>
#import "AdjustSdk/ADJEvent.h"
#import "AdjustSdk/Adjust.h"

@implementation HwAdsInterface
static HwAdsInterface *hwAdsInterfaceInstance;
+ (id) sharedInstance{
    if(hwAdsInterfaceInstance == nil){
        NSLog(@"shareInstance");
        hwAdsInterfaceInstance = [[self alloc] init];
    }
    return hwAdsInterfaceInstance;
}
#pragma MARK HwAdsDelegate-激励广告的代理方法

//加载成功  添加delegate
bool hasReward=false;
- (void)hwAdsRewardedVideoLoadSuccess{
    NSLog(@"callback-hwAdsRewardedVideoLoadSuccess");
}
//加载失败
- (void)hwAdsRewardedVideoLoadFail{
    NSLog(@"callback-hwAdsRewardedVideoLoadFail");

}
//播放失败，不给奖励
- (void)hwAdsRewardedVideoPlayFail{
    NSLog(@"callback-hwAdsRewardedVideoPlayFail");
    hasReward=false;

}
//广告展示
- (void)hwAdsRewardedVideoDidAppear{
    NSLog(@"callback-hwAdsRewardedVideoDidAppear");

}
//广告关闭
- (void)hwAdsRewardedVideoClose{
    NSLog(@"callback-hwAdsRewardedVideoClos");
    if(hasReward){
        UnitySendMessage("HwAdsBridge", "RewardCallBack", "true");
    }else{
        UnitySendMessage("HwAdsBridge", "RewardCallBack", "false");
    }
    

}
//广告被点击
- (void)hwAdsRewardedVideoClick{
    NSLog(@"callback-hwAdsRewardedVideoClick");

}
//广告播放完成，给奖励，最好在这里做标记，在close中给奖励
- (void)hwAdsRewardedVideoGiveReward
{
    NSLog(@"callback-hwAdsRewardedVideoGiveReward");
    hasReward=true;

}
//广告即将展示，建议在收到这个回调时，暂停游戏；
- (void)hwAdsRewardedVideoWillAppear{
    NSLog(@"callback-hwAdsRewardedVideoWillAppear");

}
#pragma MARK--BANNE广告的代理方法
- (void)hwAdsBannerLoadSuccess{
    NSLog(@"callback-hwAdsBannerLoadSuccess");

}
#pragma MARK -插屏广告的代理
//插屏加载
- (void)hwAdsInterstitialLoadSuccess{
    NSLog(@"callback-hwAdsInterstitialLoadSuccess");

}
//加载失败
- (void)hwAdsInterstitialLoadFail{
    NSLog(@"callback-hwAdsInterstitialLoadFail");

}
//插屏点击 add 3.0
- (void)hwAdsInterstitialClick{
    NSLog(@"callback-hwAdsInterstitialClick");

}
//插屏播放 add 3.0
- (void)hwAdsInterstitialShow{
    NSLog(@"callback-hwAdsInterstitialShow");

}
//插屏关闭 add 3.0
- (void)hwAdsInterstitialClose{
    NSLog(@"callback-hwAdsInterstitialClose");
    UnitySendMessage("HwAdsBridge", "InterCallBack", "close");

}
@end
void getCountryCode(){
    NSString *deviceName = [[UIDevice currentDevice] name];
    NSString *deviceCountryCode = [[UIDevice currentDevice] systemVersion];
    
    NSString *lanarr = NSLocaleCountryCode;
    NSLog(@"deviceName %@ ",deviceName);
    NSLog(@"deviceCountryCode %@",deviceCountryCode);
    NSLog(@"lanarr %@",lanarr);
}


void initHwSDK(int serverURL){

    HwAdsInterface* hwAdsInterface = [HwAdsInterface sharedInstance];
    //新版本只需要传一个参数
    [[HwAds instance] initSDK:(serverURL) isFirebase:(TRUE) isABTestOpen:(FALSE)];
    //关联回调的代码
    HwAds* hwads = [HwAds instance];
    hwads.hwAdsDelegate = hwAdsInterface;
    hwads.hwAdsInterDelegate = hwAdsInterface;
    hwads.hwAdsBannerDelegate = hwAdsInterface;
}

//banner
void showHwBannerAd(){
    [[HwAds instance] showBanner];
    
}
void hideHwBannerAd(){
    [[HwAds instance] hideBanner];
    
}
BOOL isHwBannerAdLoaded(){
    return [[HwAds instance] isBannerLoad];
}
//inter
void showHwInterAd(){
    [[HwAds instance] showInter];
}
BOOL isHwInterAdLoaded(){
    return [[HwAds instance] isInterLoad];
}
//reward
void showHwRewardAd(char * tag){
    [[HwAds instance] showReward:[NSString stringWithUTF8String:tag]];

}
BOOL isHwRewardAdLoaded(){
    return [[HwAds instance] isRewardLoad];
}

//内购打点
void hwAnalyticsPurchase(char * dollers,char * currency,char *productId,char *productName,int purchaseType,char * orderId,char *purchaseToken){
    
    [[HwAds instance] hwAnalyticsPurchaseByNumberOfDollars:([NSString stringWithUTF8String:dollers]) currency:([NSString stringWithUTF8String:currency]) productId:([NSString stringWithUTF8String:productId]) productName:([NSString stringWithUTF8String:productName]) purchaseType:(purchaseType) orderId:([NSString stringWithUTF8String:orderId]) purchaseToken:([NSString stringWithUTF8String:purchaseToken])];
    
   // [[HwAds instance] hwAnalyticsPurchaseByNumberOfDollars:[NSString stringWithUTF8String:dollers]currency:[NSString stringWithUTF8String:currency]productId:[NSString stringWithUTF8String:productId] purchaseType:purchaseType orderId:[NSString stringWithUTF8String:orderId] purchaseToken:[NSString stringWithUTF8String:purchaseToken]];
    
}

void adJustEvent(char * token){
    [[HwAds instance] hwAdjustEventToken:[NSString stringWithUTF8String:token]];
}

void adJustEventWithParam(char * token,char * timestap,char * session,char * version){
    
    ADJEvent *event = [[ADJEvent alloc] initWithEventToken:[NSString stringWithUTF8String:token]];
    [event addCallbackParameter:@"TimeStamp" value:[NSString stringWithUTF8String:timestap]];
    [event addCallbackParameter:@"Session" value:[NSString stringWithUTF8String:session]];
    [event addCallbackParameter:@"EventVersion" value:[NSString stringWithUTF8String:version]];
    [Adjust trackEvent:event];
}

void firebaseEvent(char * eventName,char * eventKey ,char * eventValue){
    
    [FIRAnalytics logEventWithName:[NSString stringWithUTF8String:eventName]

    parameters:@{[NSString stringWithUTF8String:eventKey]:[NSString stringWithUTF8String:eventValue]}];
}

//sdk版本号
char  hwSdkVersion(){
    NSString * version =[[HwAds instance] sdkVersion];

    return [version UTF8String];
}

BOOL isPrivacyBtnEnable(){
    return [[HwAds instance] isPrivacySettingsButtonEnabled];
}

void openPrivacyForm(){
    [[HwAds instance] presentCMPForm];
}

//上传日志到SDK后台--传
void reportLogByID(int serverURL){
    //[[HwAds instance] reportLogByID:serverURL];
    
}

void addTaId(char * id){
    [[HwAds instance] hwAdjustAddTa_distinct_id:[NSString stringWithUTF8String:id] ta_account_id:@""];
}
// 定义一个接受布尔参数的回调函数类型
typedef void (*UnityBoolCallback)(bool value);

void isAdJustInit(UnityBoolCallback callback) {
    
    [Adjust adidWithCompletionHandler:^(NSString * _Nullable adid) {
        BOOL success = (adid != nil);
        
        NSLog(@"HWLog: Adjust初始化id获取%@，ID: %@", success ? @"成功" : @"失败", adid ?: @"nil");
        
        // 调用传入的回调函数，传递布尔值表示操作成功或失败
        if (callback) {
            callback(true);
        }
    }];
}

void setRemoveAdsStatus(BOOL isNoAd){
    [[HwAds instance] setRemoveAdsStatus:isNoAd];
}

void trackRewardButtonClick(char * adtype){
    [[HwAds instance] trackRewardButtonClick:[NSString stringWithUTF8String:adtype]];
}
