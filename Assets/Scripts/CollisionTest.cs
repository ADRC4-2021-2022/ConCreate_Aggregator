using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CollisionTest : MonoBehaviour
{
    //text with info about collision during play mode (top left game scene)
    [SerializeField]
    private Text _debugText;

    //to decide whether aggregate parts according to length or not
    [SerializeField]
    private Toggle _toggleConnectionMatching;

    private readonly float _radius = 25f; // check for penetration within a radius of 50m (radius of a sphere)
    private readonly int _maxNeighboursToCheck = 50; // how many neighbouring colliders to check in IsColliding function
    private readonly float _overlapTolerance = 0.03f; // used by compute penetration
    private readonly float _connectionTolerance = 0.1f; // tolerance of matching connections
    private bool _connectionMatchingEnabled = true;

    private Collider[] _neighbours; // "ingredient" of compute penetration
    private Vector3 _collisionTestSpherePosition = Vector3Int.zero; // "ingredient" of compute penetration

    private List<Part> _parts = new(); // LIBRARY OF PARTS
    private List<Part> _placedParts = new();

    GameObject _boundingBox;

    public void Start()
    {
        _boundingBox = GameObject.Find("BoundingBox");

        // check if the toggle for matching/non matching connections is on/off
        _toggleConnectionMatching.onValueChanged.AddListener(delegate { _connectionMatchingEnabled = !_connectionMatchingEnabled; });

        for (int i = 0; i < 50; i++)
        {
            //Load all the prefabs
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

            //Select the prefabs with tag Part
            _parts.AddRange(prefabs.Where(g => g.CompareTag("Part")).Select(g => new Part(g)).ToList());
        }
        _neighbours = new Collider[_maxNeighboursToCheck]; // initialize neighbors' array
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
        // find all the available connections in the entire building made up of placed parts
        List<Connection> availableConnectionsInCurrentBuilding = new();
        foreach (Part placedPart in _placedParts)
        {
            foreach (Connection connection in placedPart.Connections)
            {
                if (connection.Available) availableConnectionsInCurrentBuilding.Add(connection);
            }
        }

        // take random connection
        int randomIndexInCurrentBuilding = Random.Range(0, availableConnectionsInCurrentBuilding.Count);
        Connection randomAvailableConnectionInCurrentBuilding = availableConnectionsInCurrentBuilding[randomIndexInCurrentBuilding];

        // get list of av connections in UNPLACED PARTS
        List<Connection> availableConnectionsInUnplacedParts = new();
        foreach (Part unplacedPart in _parts)
        {
            foreach (Connection connection in unplacedPart.Connections)
            {
                // compatible = the toggle is on and we want matching connections
                if (connection.Available && AreConnectionsCompatible(randomAvailableConnectionInCurrentBuilding, connection))
                {
                    availableConnectionsInUnplacedParts.Add(connection);
                }
            }
        }

        foreach (Connection unplacedConnection in availableConnectionsInUnplacedParts)
        {
            unplacedConnection.ThisPart.PositionPart(randomAvailableConnectionInCurrentBuilding, unplacedConnection);

            if (IsColliding(unplacedConnection.ThisPart) || !CheckPartInBounds(unplacedConnection.ThisPart))
            {
                //the part collided, so go to the next part in the list
                unplacedConnection.ThisPart.ResetPart();
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
    /// Check if two connections are of compatible length/width, within a margin of tolerance
    /// (If connection matching is off, this function returns true by default)
    /// </summary>
    /// <param name="connectionInBuilding">A connection in the current building</param>
    /// <param name="connectionToPlace">A potential connection, to be checked for compatibility</param>
    /// <returns>True if within tolerable measurements, false if not</returns>
    private bool AreConnectionsCompatible(Connection connectionInBuilding, Connection connectionToPlace)
    {
        if (!_connectionMatchingEnabled) return true;

        float connectionLength = connectionInBuilding.Length;
        float minLength = connectionLength - _connectionTolerance;
        float maxLength = connectionLength + _connectionTolerance;

        float connectionWidth = connectionInBuilding.Width;
        float minWidth = connectionWidth - _connectionTolerance;
        float maxWidth = connectionWidth + _connectionTolerance;

        return connectionToPlace.Length > minLength && connectionToPlace.Length < maxLength
                && connectionToPlace.Width > minWidth && connectionToPlace.Width < maxWidth;
    }

    /// <summary>
    /// ComputePenetration method: tells direction and distance in order to avoid collision between 2 objects
    /// </summary>
    /// <returns>True if collision is found, false if not</returns>
    private bool IsColliding(Part newPart)
    {

        var thisCollider = newPart.Collider;
        if (!thisCollider)
        {
            Debug.Log($"{newPart.Name} has no collider attached!");
            return false; // nothing to do without a collider attached
        }

        //create the sphere with the features created on top
        //int count = Physics.OverlapSphereNonAlloc(_collisionTestSpherePosition, _radius, _neighbours, 6);

        // Iterate through the neighbours' colliders and check if their collider is colliding with the part's one
        foreach (Part nextPart in _placedParts)
        {
            var otherCollider = nextPart.Collider;

            if (nextPart.GOPart == newPart.GOPart)
            {
                continue; // skip ourself
            }
                

            Vector3 otherPosition = otherCollider.gameObject.transform.position;
            Quaternion otherRotation = otherCollider.gameObject.transform.rotation;

            bool isOverlapping = Physics.ComputePenetration(
                thisCollider, thisCollider.gameObject.transform.position, thisCollider.gameObject.transform.rotation,
                otherCollider, otherPosition, otherRotation,
                out Vector3 direction, out float distance);

            // overlapping colliders and too big overlap --> IsColliding = true --> not place the part
            if (isOverlapping && distance > _overlapTolerance)
            {
                _debugText.text += $"Part {newPart.Name} collision info:\n" +
                    $"Colliding with {otherCollider.gameObject.name}:\n" +
                    $"Direction: {direction.x}, {direction.y}, {direction.z}\n" +
                    $"Distance: {distance} meters\n\n";
                return true;
            }
        }
        // if we reach this point there's no collision --> place part
        return false;
    }

    bool CheckPartInBounds(Part part)
    {
        List<Transform> boundingBoxes = new List<Transform>();
        for (int j = 0; j < _boundingBox.transform.childCount; j++)
        {
            boundingBoxes.Add(_boundingBox.transform.GetChild(j));
        }
        bool isInBounds = part.CheckInsideBoundingBox(boundingBoxes, out float distance, out Vector3 direction);
        Debug.Log($"{isInBounds}, dist {distance}");
        return isInBounds && distance > 0.5f;
    }

    private IEnumerator AutoPlacement()
    {
        for (int i = 0; i < 50; i++)
        {

            PlaceNextPart();
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(1f);
    }

    public void OnPlaceNextPartButtonClicked()
    {
        PlaceNextPart();
    }

    public void OnAutoPlacementButtonClicked()
    {
        StartCoroutine(AutoPlacement());
    }

    // visualize the sphere of compute penetration (in which checking for collision)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_collisionTestSpherePosition, _radius);
    }
}
