using UnityEngine;
using System.Collections;

namespace Kabam {
    public class KabamSODABomb : MonoBehaviour {

        // Update is called once per frame
        virtual protected void Update() {
            SODACheckForClick();
        }

        virtual protected void SODACheckForClick() {
            Vector3? pos = null;
        
            if (Input.touchCount == 1) {
                pos = Input.touches[0].position;
            } else if (Input.GetMouseButtonDown(0)) {
                pos = Input.mousePosition;
            }
        
            if (pos.HasValue) {
                if (this.gameObject.guiTexture != null) {
                    // Handle clicks on GUI elements.
                    GUILayer guiLayer = Camera.main.GetComponent<GUILayer>();
                    if (guiLayer.HitTest(pos.Value) == this.gameObject.guiTexture) {
                        Debug.Log("Kabam SODA GUI Texture Clicked");
                        SendMessageUpwards("SODAStartGUI");
                    }
                } else {
                    // Handle clicks on scene elements.
                    Ray ray = Camera.main.ScreenPointToRay(pos.Value);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit)) {
                        if (hit.collider != null && hit.collider.gameObject == this.gameObject) {
                            Debug.Log("Kabam SODA Scene Object Clicked");
                            SendMessageUpwards("SODAStartGUI");
                        }
                    }
                }
            }
        }

        virtual public void SODAOnVisibilityChange(bool visible) {
            Debug.Log("KabamSODABomb Visibility Change: " + visible);
            if (gameObject.renderer != null) {
                gameObject.renderer.enabled = visible;
            }
            if (gameObject.guiTexture != null) {
                gameObject.guiTexture.enabled = visible;
            }
        }
    }

}
