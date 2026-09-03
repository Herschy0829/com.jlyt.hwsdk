package com.unity3d.player;

import android.app.Activity;
import android.content.Context;
import android.os.Bundle;
import android.util.Log;

import com.google.firebase.FirebaseApp;
import com.google.firebase.analytics.FirebaseAnalytics;
import com.hw.hwadssdk.HwAdsInterface;
import com.hw.hwadssdk.HwAdsInterstitialListener;
import com.hw.hwadssdk.HwAdsRewardVideoListener;

public class HWAdsBridge {

    private static HWAdsBridge _instance;

    public static HWAdsBridge getInstance(){
        if(_instance==null){
            _instance=new HWAdsBridge();
        }
        return _instance;
    }

    private void Log(String str){
        Log.d("##HWAdsBridge==>",str);
    }

    public void initSdk(Context context, String gbid, String apptoken){

        Log("Init! gbid:"+gbid+" ;apptoken:"+apptoken);
        HwAdsInterface.initSDK(context,gbid,apptoken,"yes","no","no");

    }

    public void setUserId(String id){
        Log("SetUserId! id:"+id);
    //    HwAdsInterface.setUserID(id);
    }


    public void setInterListener(HwAdsInterstitialListener listener) {
        Log("SetInterListener!");
        HwAdsInterface.setHwAdsInterstitialListener(listener);
    }

    public boolean  isInterLoaded(){
        boolean isLoaded=HwAdsInterface.isInterLoad();
        Log("isInterLoaded:"+isLoaded);
        return isLoaded;
    }
    public void showInter(){
        boolean isFacebookInter = HwAdsInterface.isFacebookInter();
        Log("ShowInter!  isFBInter:"+isFacebookInter);
        HwAdsInterface.showInter();
    }

    public void setRewardListener(HwAdsRewardVideoListener listener) {
        Log("setRewardListener!");
        HwAdsInterface.setHwAdsRewardedVideoListener(listener);
    }
    public boolean  isRewardLoaded(){
        boolean  isLoaded=HwAdsInterface.isRewardLoad();
        Log("isRewardLoaded:"+isLoaded);
        return isLoaded;
    }
    public void showReward(String str){
        Log("ShowReward!");
        HwAdsInterface.showReward(str);
    }

    public void adJustEvent( String EventToken, String category, String action, String label){
        Log("adJustEvent! EventToken:"+EventToken+" ;category:"+category+" ;action:"+action+" ;label:"+label);
        HwAdsInterface.HwAnalyticsUserNew(  EventToken, category, action, label);
    }

    //category 购买key值；必须是"HwPurchase"
    // number 内购本地化金额；
    //currency  本地化单位
    // purchaseToken购买token；
    // productId 商品ID，
    // purchaseType 内购type：商品类型，1是订阅，0是普通商品
    // orderId 订单ID
    // adjustDifferentPurchaseToken adjust不同购买事件的token; 推荐本地存储购买金额；根据金额所处在的区间段，来传不同的token，建议至少分3段
    public void purchaseEvent(String number,String currency,String token,String productId,int type,String orderId,String adjustToken){

        Log("PurchaseEvent! number:"+number+" ;currency:"+currency+" ;token:"+token+" ;productId:"+productId+" ;type:"+type+" ;orderId:"+orderId+" ;adjustToken:"+adjustToken);
        HwAdsInterface.HwAnalyticsPurchaseSecondVerify("HwPurchase", number, currency, token, productId, type, orderId, adjustToken);
    }

    public void firebaseEvent(String eventName,String eventKey,String eventValue){
        String str="Firebase 发送事件！事件名："+eventName+";   事件Key:"+eventKey+";   事件值："+eventValue;
        Log(str);
        Bundle bundle = new Bundle();
        bundle.putString(eventKey, eventValue);
        FirebaseAnalytics.getInstance(GetUnityActivity()).logEvent(eventName, bundle);
    }

    // 919 新增数数
    public void hwAdjustAddTA(String ta_distinct_id, String ta_account_id){
        Log("hwAdjustAddTA! distinctid:"+ta_distinct_id+";accountid:"+ta_account_id);
        HwAdsInterface.hwAdjustAddTa_distinct_id(ta_distinct_id,ta_account_id);
    }

    public void SetRemoveAdsStatus(boolean value){
        Log("hwAdjustAddTA! SetRemoveAdsStatus:"+value);
        //设置用户是否购买了去广告（只针对激励视频类型）,true为已购买去广告，false为未购买去广告的，此接口以每次进游戏的传值为准
        HwAdsInterface.setRemoveAdsStatus(value);
    }


    public void TrackRewardButtonClick(String str){
        Log("hwAdjustAddTA! TrackRewardButtonClick:"+str);
        //记录激励按钮点击事件,传参为激励按钮点位的名字，只要用户点击了就调用，不需要判断广告是否加载成功
        HwAdsInterface.trackRewardButtonClick(str);
    }


    //untiyActivity
    private static Activity _unityActivity=null;
    /**
     * 获取unity活动的上下文
     * @return
     */
    public Activity GetUnityActivity(){
        if(null == _unityActivity) {
            try {
                Class<?> classtype = Class.forName("com.unity3d.player.UnityPlayer");
                Activity activity = (Activity) classtype.getDeclaredField("currentActivity").get(classtype);
                _unityActivity = activity;
            } catch (ClassNotFoundException e) {

            } catch (IllegalAccessException e) {

            } catch (NoSuchFieldException e) {

            }
        }
        return _unityActivity;
    }
}
