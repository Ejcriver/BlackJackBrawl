using UnityEngine;

public class ShowHideCanvasOnKey : MonoBehaviour
{
    public Canvas targetCanvas;
    public KeyCode toggleKey = KeyCode.Tab;
    private bool isVisible = true;

    void Start()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();
        if (targetCanvas != null)
            targetCanvas.enabled = isVisible;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            if (targetCanvas != null)
                targetCanvas.enabled = isVisible;

            if (isVisible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
