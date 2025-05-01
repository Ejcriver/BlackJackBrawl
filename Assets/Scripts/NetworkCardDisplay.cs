using Unity.Netcode;
using UnityEngine;

public class NetworkCardDisplay : NetworkBehaviour
{
    public SpriteRenderer spriteRenderer;

    // Called after spawn to set the card image on all clients
    [ClientRpc]
    public void SetCardSpriteClientRpc(string spritePath)
    {
        Debug.Log($"[NetworkCardDisplay] SetCardSpriteClientRpc called with spritePath: {spritePath}");
        var sprite = Resources.Load<Sprite>(spritePath);
        Debug.Log($"[NetworkCardDisplay] Sprite loaded: {(sprite != null ? "YES" : "NO")}");
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        Debug.Log($"[NetworkCardDisplay] SpriteRenderer found: {(spriteRenderer != null ? "YES" : "NO")}");
        if (spriteRenderer != null && sprite != null)
            spriteRenderer.sprite = sprite;
        else if (sprite == null)
            Debug.LogError($"[NetworkCardDisplay] Sprite not found at path: {spritePath}");
        else if (spriteRenderer == null)
            Debug.LogError($"[NetworkCardDisplay] SpriteRenderer not found on card prefab.");
    }
}
