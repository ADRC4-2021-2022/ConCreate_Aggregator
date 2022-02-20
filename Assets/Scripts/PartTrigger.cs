using System.Collections.Generic;
using UnityEngine;

public class PartTrigger : MonoBehaviour
{
    #region public fields
    public Part ConnectedPart;
    public List<GameObject> Collisions = new List<GameObject>();
    #endregion

    #region private fields
    #endregion

    #region Monobehaviour functions
    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log($"Colliding with {collider.attachedRigidbody}");
        Collisions.Add(collider.gameObject);
    }

    private void OnTriggerExit(Collider collider)
    {
        Debug.Log($"No longer colliding with {collider.attachedRigidbody}");
        Collisions.Remove(collider.gameObject);
    }
    #endregion

    #region public functions

    #endregion
    #region private functions

    #endregion

    #region Canvas function

    #endregion
}
