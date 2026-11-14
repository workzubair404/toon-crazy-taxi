using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AdsManager : MonoBehaviour
{
    public delegate void RewardedDelegate();
    public RewardedDelegate rewardedDelegate;

    public static AdsManager instance;
    private void Awake()
    {
        if (instance==null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
    }
    public void ShowInterstitialAd()
    {
        Debug.LogWarning("Not Implemented");
    }
    public void ShowRewardedAd()
    {
        Debug.LogWarning("Not Implemented");
    }
    public bool CanShowRewardedAd()
    {
        Debug.LogWarning("Not Implemented");
        return false;
    }

    internal void DestroyBanner()
    {
        Debug.LogWarning("Not Implemented");
    }
}



