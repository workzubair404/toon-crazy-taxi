using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParkingCollider : MonoBehaviour
{
	public bool isPickUp = true;
	Image parkingImage;
    // Start is called before the first frame update
    void Start()
    {
		parkingImage = GN_GameController.instance.ParkingImage;
    }


    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
			Debug.Log("Player Exited");
			parkingImage.fillAmount = 0f;
			parkingImage.gameObject.SetActive(false);
		}
	}
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
			Debug.Log("Player Entered");
			parkingImage.fillAmount = 0f;
			parkingImage.gameObject.SetActive(true);
		}

	}
	void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			Debug.Log("Player Stay");
			if (parkingImage.fillAmount < 1 )
			{
				parkingImage.fillAmount += Time.deltaTime/3; // * 0.25f;
			}
			else
			{
                parkingImage.gameObject.SetActive(false);
                if (isPickUp)
                {
					ParkingAnimationManager.instance.ShowPickUpAnimation();
					this.gameObject.SetActive(false);
                }
                else
                {
					ParkingAnimationManager.instance.ShowDropAnimation();
					this.gameObject.SetActive(false);
				}
			}
		}
	}
	
}
