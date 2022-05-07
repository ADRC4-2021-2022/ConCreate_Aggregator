using UnityEngine;
using UnityEngine.UI;

// Visualises the minimum translation vectors required to separate apart from other colliders found in a given radius
// Attach to a GameObject that has a Collider attached.
[ExecuteInEditMode()]
public class ShowPenetration : MonoBehaviour
{
    [SerializeField]
    private Text _depenetrationDirectionText;

    [SerializeField]
    private Text _depenetrationMagnitudeText;

    private float radius = 10f; // check for penetration within a radius of 3m (radius of a sphere)
    private int maxNeighbours = 16; // maximum amount of neighbours in the sphere where the collision is checked

    private Collider[] neighbours;

    public void Start()
    {
        Debug.Log($"Attached GameObject bounds size is: {transform.GetComponent<Collider>().bounds}");
        neighbours = new Collider[maxNeighbours]; // create an empty array to store the neighbours' colliders
    }

    public void OnDrawGizmos()
    {
        var thisCollider = GetComponent<Collider>();
        if (!thisCollider) return; // nothing to do without a Collider attached

        //create the sphere with the features created on top
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, neighbours);

        // Iterate through the neighbouring colliders of the collider that this script is attached to
        for (int i = 0; i < count; ++i)
        {
            var otherCollider = neighbours[i];

            if (otherCollider == thisCollider)
                continue; // skip ourself

            Vector3 otherPosition = otherCollider.gameObject.transform.position;
            Quaternion otherRotation = otherCollider.gameObject.transform.rotation;

            Vector3 direction;
            float distance;
            bool isOverlapping = Physics.ComputePenetration(
                thisCollider, transform.position, transform.rotation,
                otherCollider, otherPosition, otherRotation,
                out direction, out distance
            );

            // draw a line showing the depenetration direction if overlapped
            if (isOverlapping)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(otherPosition, direction * distance);
                _depenetrationDirectionText.text = $"Direction: {direction.x}, {direction.y}, {direction.z}";
                _depenetrationMagnitudeText.text = $"Distance: {distance} meters";
            }
        }
    }
}
