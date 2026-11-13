using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class ParkingAnimationManager : MonoBehaviour
{
    //Camera
    public GameObject parkingViewCamera;
    //Way Point
    public Transform wayPointCloseToCar;
    public Transform wayPointFarFromCar;
    //Passengers
    public GameObject[] passengers;
    int passengerNumber;
    //Car RigidBody
    [HideInInspector]public Rigidbody playerRigidbody;
    GameObject tempObject;
    Transform temp;
    public static ParkingAnimationManager instance = null;
    private void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        passengerNumber = Random.Range(0, passengers.Length-1);
        Debug.Log("Passenger Number : " + passengerNumber);
        tempObject = passengers[passengerNumber];
        temp = tempObject.transform.parent;
        parkingViewCamera.SetActive(false);
    }

    public void ShowPickUpAnimation()
    {
        playerRigidbody.isKinematic = true;
        //Show Animation Camera
        parkingViewCamera.SetActive(true);
        //Show Passenger walking
        temp.position = wayPointFarFromCar.position;
        temp.rotation = wayPointFarFromCar.rotation;
        temp.LookAt(wayPointCloseToCar);
        tempObject.SetActive(true);
        temp.DOMove(wayPointCloseToCar.position, 3f);
        Invoke("PickUpAnimationCompleted", 3f);
    }
    public void ShowDropAnimation()
    {
        playerRigidbody.isKinematic = true;
        //Show Animation Camera
        parkingViewCamera.SetActive(true);
        //Show Passenger walking
        temp.position = wayPointCloseToCar.position;
        temp.rotation = wayPointCloseToCar.rotation;
        temp.LookAt(wayPointFarFromCar);
        tempObject.SetActive(true);
        temp.DOMove(wayPointFarFromCar.position, 3f);
        Invoke("DropAnimationCompleted", 3f);
    }
    void HideParkingAnimation()
    {
        playerRigidbody.isKinematic = false;
        parkingViewCamera.SetActive(false);
        foreach (GameObject gm in passengers)
        {
            gm.SetActive(false);
        }
    }
    void PickUpAnimationCompleted()
    {
        //ScreenFade
        GN_GameController.instance.FadeNow();
        HideParkingAnimation();
        GameManager.Instance.TaskComplete();
    }
    void DropAnimationCompleted()
    {
        HideParkingAnimation();
        GameManager.Instance.TaskComplete();
    }
}
