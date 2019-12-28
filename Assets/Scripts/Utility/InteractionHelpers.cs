using UnityEngine;

public static class InteractionHelpers {
    public static T GetClickedElement<T> ()
    where T : class {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint (Input.mousePosition);
        Vector2 mousePos2D = new Vector2 (mousePos.x, mousePos.y);

        RaycastHit2D hit = Physics2D.Raycast (mousePos2D, Vector2.zero);

        if (hit.collider != null) {
            return hit.collider.gameObject.GetComponent<T> ();
        }

        return null;
    }
}