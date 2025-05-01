using UnityEngine;

public class ShopInteractable : MonoBehaviour
{
    public GameObject shopUIPrefab;
    private GameObject shopUIInstance;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Instantiate and show the shop UI if not already present
            if (shopUIInstance == null && shopUIPrefab != null)
            {
                shopUIInstance = Instantiate(shopUIPrefab);
                var shopController = shopUIInstance.GetComponent<ShopUIController>();
                if (shopController != null)
                    shopController.ShowShop();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Hide and destroy the shop UI when player leaves
            if (shopUIInstance != null)
            {
                var shopController = shopUIInstance.GetComponent<ShopUIController>();
                if (shopController != null)
                    shopController.HideShop();
                Destroy(shopUIInstance);
                shopUIInstance = null;
            }
        }
    }
}
