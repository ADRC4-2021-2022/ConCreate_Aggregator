using System.Collections.Generic;
using UnityEngine;

public class PartTrigger : MonoBehaviour
{
    #region public fields
    public Part ConnectedPart;
    public List<GameObject> collisions { get; private set; }
    #endregion

    #region private fields
    #endregion

    #region Monobehaviour functions
    private void Start()
    {
        collisions = new List<GameObject>();
    }

    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log($"Colliding with {collider.attachedRigidbody}");
        collisions.Add(collider.gameObject);
    }

    private void OnTriggerExit(Collider collider)
    {
        Debug.Log($"No longer colliding with {collider.attachedRigidbody}");
        collisions.Remove(collider.gameObject);
    }
    #endregion

    #region public functions

    #endregion
    #region private functions

    #endregion

    #region Canvas function

    #endregion
}
