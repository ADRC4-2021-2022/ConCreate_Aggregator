using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Visualises the minimum translation vectors required to separate apart from other colliders found in a given radius
// Attach to a GameObject that has a Collider attached.
public class CollisionTest : MonoBehaviour
{
    [SerializeField]
    private Text _debugText;

    private readonly float _radius = 25f; // check for penetration within a radius of 50m (radius of a sphere)
    private readonly int _maxNeighboursToCheck = 50; // how many neighbouring colliders to check in CollisionCheck
    private readonly float _overlapTolerance = 0.03f;
    private Collider[] _neighbours;
    private Vector3 _collisionTestSpherePosition = Vector3Int.zero;

    private List<Part> _parts = new();
    private List<Part> _placedParts = new();

    public void Start()
    {
        for (int i = 0; i < 50; i++)
        {
            //Load all the prefabs
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

            //Select the prefabs with tag Part
            _parts.AddRange(prefabs.Where(g => g.CompareTag("Part")).Select(g => new Part(g)).ToList());
        }
        _neighbours = new Collider[_maxNeighboursToCheck];
        PlaceFirstPart();
        EnableAllConnections();
        
    }

    private void EnableAllConnections()
    {
        _parts.ForEach(part => part.Connections.ForEach(connection => connection.Available = true));
    }

    private void PlaceFirstPart()
    {
        int rndPartIndex = Random.Range(0, _parts.Count);
        Part randomPart = _parts[rndPartIndex];

        int rndZ = Random.Range(0, 4);
        int rndY = Random.Range(0, 4);

        randomPart.PlaceFirstPart(Vector3.zero, Quaternion.Euler(new Vector3(0, rndY * 90, rndZ * 90)));
        _parts.Remove(randomPart);
        _placedParts.Add(randomPart);
        randomPart.Name = $"{randomPart.Name} added {_placedParts.Count}";
    }

    private void PlaceNextPart()
    {
        List<Connection> availableConnectionsInCurrentBuilding = new();
        foreach (Part placedPart in _placedParts)
        {
            foreach (Connection connection in placedPart.Connections)
            {
                if (connection.Available) availableConnectionsInCurrentBuilding.Add(connection);
            }
        }

        int randomIndexInCurrentBuilding = Random.Range(0, availableConnectionsInCurrentBuilding.Count);
        Connection randomAvailableConnectionInCurrentBuilding = availableConnectionsInCurrentBuilding[randomIndexInCurrentBuilding];

        List<Connection> availableConnectionsInUnplacedParts = new();
        foreach (Part unplacedPart in _parts)
        {
            foreach (Connection connection in unplacedPart.Connections)
            {
                if (connection.Available) availableConnectionsInUnplacedParts.Add(connection);
            }
        }

        //int randomIndexInUnplacedParts = Random.Range(0, availableConnectionsInUnplacedParts.Count);
        //Connection randomAvailableConnectionsInUnplacedParts = availableConnectionsInUnplacedParts[randomIndexInUnplacedParts];

        foreach (Connection unplacedConnection in availableConnectionsInUnplacedParts)
        {
            unplacedConnection.ThisPart.PositionPart(randomAvailableConnectionInCurrentBuilding, unplacedConnection);
            
            if (CheckCollision())
            {
                //the part collided, so reset the part
                unplacedConnection.ThisPart.ResetPart();
                //remove the tried connection from the list of possible connections
                /*availableConnectionsInUnplacedParts.Remove(unplacedConnection);
                //RegenerateVoxelGrid(currentPart);
                if (availableConnectionsInUnplacedParts.Count <= 0)
                {
                    randomAvailableConnectionInCurrentBuilding.Available = false;
                    Debug.LogWarning($"{randomAvailableConnectionInCurrentBuilding.GOConnection.name} for " +
                        $"{randomAvailableConnectionInCurrentBuilding.ThisPart.Name} doesn't have any possible connecting parts");
                    randomAvailableConnectionInCurrentBuilding.Visible = true;
                    continue;
                }*/
            }
            else
            {
                //Set the part as placed
                unplacedConnection.ThisPart.PlacePart(unplacedConnection);
                randomAvailableConnectionInCurrentBuilding.Available = false;
                unplacedConnection.ThisPart.Name = $"{unplacedConnection.ThisPart.Name}";
                _parts.Remove(unplacedConnection.ThisPart);
                _placedParts.Add(unplacedConnection.ThisPart);
                return;
            }
        }
    }

    /// <summary>
    /// Check for collisions with a particular part
    /// </summary>
    /// <param name="partToCheck">The Part to be checked</param>
    /// <returns>True if collision is found, false if not</returns>
    private bool CheckCollision()
    {
        foreach (Part partToCheck in _placedParts)
        {
            var thisCollider = partToCheck.Collider;
            if (!thisCollider)
            {
                Debug.Log($"{partToCheck.Name} has no collider attached!");
                continue; // nothing to do without a Collider attached
            }

            //create the sphere with the features created on top
            int count = Physics.OverlapSphereNonAlloc(_collisionTestSpherePosition, _radius, _neighbours);
            // Iterate through the neighbouring colliders of the collider that this script is attached to
            for (int i = 0; i < count; ++i)
            {
                var otherCollider = _neighbours[i];

                if (otherCollider == thisCollider)
                    continue; // skip ourself

                Vector3 otherPosition = otherCollider.gameObject.transform.position;
                Quaternion otherRotation = otherCollider.gameObject.transform.rotation;

                bool isOverlapping = Physics.ComputePenetration(
                    thisCollider, thisCollider.gameObject.transform.position, thisCollider.gameObject.transform.rotation,
                    otherCollider, otherPosition, otherRotation,
                    out Vector3 direction, out float distance
                );

                // draw a line showing the depenetration direction if overlapped
                if (isOverlapping && distance > _overlapTolerance)
                {
                    _debugText.text += $"Part {partToCheck.Name} collision info:\n"+
                        $"Colliding with {otherCollider.gameObject.name}:\n" +
                        $"Direction: {direction.x}, {direction.y}, {direction.z}\n" +
                        $"Distance: {distance} meters\n\n";
                    return true;
                }
            }
        }
        return false;
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(880, 16, 200, 50), "Place Next Part"))
        {
            PlaceNextPart();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_collisionTestSpherePosition, _radius);
    }
}
