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
    public void Start()
    {
        if (!SaveData.Instance.RemoveAds)
        {
        }
    }

    public void ShowInterstitialAd()
    {
        Debug.LogError("Not Implemented");
    }
    public void ShowRewardedAd()
    {
        Debug.LogError("Not Implemented");
    }
    public bool CanShowRewardedAd()
    {
        Debug.LogError("Not Implemented");
        return false;
    }

    internal void DestroyBanner()
    {
        Debug.LogError("Not Implemented");
    }
}



