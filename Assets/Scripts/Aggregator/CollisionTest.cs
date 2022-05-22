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

    [SerializeField]
    private Text _lastPartPlacedText;

    //to decide whether aggregate parts according to length or not
    [SerializeField]
    private Toggle _toggleConnectionMatching;

    private IEnumerator _autoPlacementCoroutine;

    private readonly float _radius = 25f; // check for penetration within a radius of 50m (radius of a sphere)
    private readonly int _maxNeighboursToCheck = 50; // how many neighbouring colliders to check in IsColliding function
    private readonly float _overlapTolerance = 0.03f; // used by compute penetration
    private readonly float _connectionTolerance = 0.2f; // tolerance of matching connections
    private bool _connectionMatchingEnabled = true;

    private Collider[] _neighbours; // "ingredient" of compute penetration
    private Vector3 _collisionTestSpherePosition = Vector3Int.zero; // "ingredient" of compute penetration

    private List<Part> _parts = new(); // LIBRARY OF PARTS
    private List<Part> _floorParts = new(); // LIBRARY OF FLOOR PARTS
    private List<Part> _placedParts = new();
    private List<Part> _placedFloorParts = new();

    GameObject _boundingBox;
    GameObject _floorBB;

    public void Start()
    {
        _boundingBox = GameObject.Find("BoundingBox2");
        _floorBB = GameObject.Find("FloorBB");

        // check if the toggle for matching/non matching connections is on/off
        _toggleConnectionMatching.onValueChanged.AddListener(delegate { _connectionMatchingEnabled = !_connectionMatchingEnabled; });

        LoadPartPrefabs();
        _neighbours = new Collider[_maxNeighboursToCheck]; // initialize neighbors' array
        PlaceFirstPart();
        EnableAllConnections();

        //LoadFloorPartPrefabs();
        //PlaceFirstFloorPart();
        //EnableAllFloorConnections();

        _autoPlacementCoroutine = AutoPlacement();
    }

    private void LoadPartPrefabs()
    {
        for (int i = 0; i < 100; i++)
        {
            //Load all the prefabs
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

            //Select the prefabs with tag Part
            _parts.AddRange(prefabs.Where(g => g.CompareTag("Part")).Select(g => new Part(g)).ToList());
        }
        EnableAllConnections();
    }

    private void LoadFloorPartPrefabs()
    {
        for (int i = 0; i < 100; i++)
        {
            //Load all the prefabs
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

            //Select the prefabs with tag Part
            _floorParts.AddRange(prefabs.Where(g =>
            {
                var childCount = g.transform.childCount;
                for (int j = 0; j < childCount; ++j)
                {
                    var child = g.transform.GetChild(j);
                    if (child.CompareTag("onlyFloorConn") || child.CompareTag("bothWallFloorConn")) return true;
                    // Do something based on tag
                }
                return false;
            }).Select(g => new Part(g)).ToList());
        }
        EnableAllFloorConnections();
    }

    private void EnableAllConnections()
    {
        _parts.ForEach(part => part.Connections.ForEach(connection => connection.Available = true));
    }

    private void EnableAllFloorConnections()
    {
        _floorParts.ForEach(part => part.Connections.ForEach(connection => connection.Available = true));
    }

    private void PlaceFirstPart()
    {
        //int rndPartIndex = Random.Range(0, _parts.Count);
        //Part randomPart = _parts[rndPartIndex];

        //int rndZ = Random.Range(0, 4);
        //int rndY = Random.Range(0, 4);

        Part firstPart = _parts.Find(part => part.Name == "04P 1(Clone)");
        firstPart.PlaceFirstPart(new Vector3(-0.263f, 1.35f, -0.203f), Quaternion.Euler(new Vector3(0, 0, 0)));
        _parts.Remove(firstPart);
        _placedParts.Add(firstPart);
        firstPart.Name = $"{firstPart.Name} added {_placedParts.Count} (wall)";
        CheckPartInBounds(firstPart, _boundingBox);
    }

    private void PlaceFirstFloorPart()
    {
        //int rndPartIndex = Random.Range(0, _floorParts.Count);
        //Part randomPart = _floorParts[rndPartIndex];
        //var extents = randomPart.Collider.size / 2;
        Part firstPart = _floorParts.Find(part => part.Name == "02P 1(Clone)");
        firstPart.PlaceFirstPart(new Vector3(-2.89f, 0.26f, -1.36f), Quaternion.Euler(new Vector3(90, 0, 0)));
        _floorParts.Remove(firstPart);
        _placedFloorParts.Add(firstPart);
        firstPart.Name = $"{firstPart.Name} added {_placedFloorParts.Count} (floor)";
        CheckPartInBounds(firstPart, _floorBB);
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
                if (connection.Available && AreConnectionsCompatible(randomAvailableConnectionInCurrentBuilding, connection)
                     && (connection.GOConnection.CompareTag("onlyWallConn") || connection.GOConnection.CompareTag("bothWallFloorConn")))
                {
                    availableConnectionsInUnplacedParts.Add(connection);
                }
            }
        }

        availableConnectionsInUnplacedParts.Shuffle();
        foreach (Connection unplacedConnection in availableConnectionsInUnplacedParts)
        {
            unplacedConnection.ThisPart.PositionPart(randomAvailableConnectionInCurrentBuilding, unplacedConnection);

            if (IsColliding(unplacedConnection.ThisPart, _placedParts) || !CheckPartInBounds(unplacedConnection.ThisPart, _boundingBox))
            {
                //the part collided, so go to the next part in the list
                unplacedConnection.ThisPart.ResetPart();
            }
            else
            {
                //Set the part as placed
                unplacedConnection.ThisPart.PlacePart(unplacedConnection);
                randomAvailableConnectionInCurrentBuilding.Available = false;
                string newName = $"{unplacedConnection.ThisPart.Name} placed {_placedParts.Count + 1} (wall)";
                Vector3 unplacedPartPos = unplacedConnection.ThisPart.GOPart.transform.position;
                _lastPartPlacedText.text = $"Last Part Placed:\n{newName}\n\nPosition:\n{unplacedPartPos.x}, {unplacedPartPos.y}, {unplacedPartPos.z}";
                unplacedConnection.ThisPart.Name = newName;
                _parts.Remove(unplacedConnection.ThisPart);
                _placedParts.Add(unplacedConnection.ThisPart);
                return;
            }
        }
    }

    private void PlaceNextFloorPart()
    {
        // find all the available connections in the entire building made up of placed parts
        List<Connection> availableConnectionsInFloor = new();
        foreach (Part placedPart in _placedFloorParts)
        {
            foreach (Connection connection in placedPart.Connections)
            {
                if (connection.Available)
                    availableConnectionsInFloor.Add(connection);
            }
        }

        // take random connection
        int randomIndexInFloor = Random.Range(0, availableConnectionsInFloor.Count);
        Connection randomAvailableConnectionInFloor = availableConnectionsInFloor[randomIndexInFloor];

        // get list of av connections in UNPLACED PARTS
        List<Connection> availableConnectionsInUnplacedFloorParts = new();
        foreach (Part unplacedPart in _floorParts)
        {
            foreach (Connection connection in unplacedPart.Connections)
            {
                // compatible = the toggle is on and we want matching connections
                if (connection.Available && AreConnectionsCompatible(randomAvailableConnectionInFloor, connection)
                    && (connection.GOConnection.CompareTag("onlyFloorConn") || connection.GOConnection.CompareTag("bothWallFloorConn")))
                {
                    availableConnectionsInUnplacedFloorParts.Add(connection);
                }
            }
        }

        foreach (Connection unplacedConnection in availableConnectionsInUnplacedFloorParts)
        {
            unplacedConnection.ThisPart.PositionPart(randomAvailableConnectionInFloor, unplacedConnection);

            if (IsColliding(unplacedConnection.ThisPart, _placedFloorParts) || !CheckPartInBounds(unplacedConnection.ThisPart, _floorBB))
            {
                //the part collided, so go to the next part in the list
                unplacedConnection.ThisPart.ResetPart();
            }
            else
            {
                //Set the part as placed
                unplacedConnection.ThisPart.PlacePart(unplacedConnection);
                randomAvailableConnectionInFloor.Available = false;
                string newName = $"{unplacedConnection.ThisPart.Name} placed {_placedFloorParts.Count + 1} (floor)";
                Vector3 unplacedPartPos = unplacedConnection.ThisPart.GOPart.transform.position;
                _lastPartPlacedText.text = $"Last Part Placed:\n{newName}\n\nPosition:\n{unplacedPartPos.x}, {unplacedPartPos.y}, {unplacedPartPos.z}";
                unplacedConnection.ThisPart.Name = newName;
                _floorParts.Remove(unplacedConnection.ThisPart);
                _placedFloorParts.Add(unplacedConnection.ThisPart);
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

        float connectionWidth = connectionInBuilding.Properties.ConnectionWidth;
        float minWidth = connectionWidth - _connectionTolerance;
        float maxWidth = connectionWidth + _connectionTolerance;

        return connectionToPlace.Properties.ConnectionWidth > minWidth && connectionToPlace.Properties.ConnectionWidth < maxWidth;
    }

    /// <summary>
    /// ComputePenetration method: tells direction and distance in order to avoid collision between 2 objects
    /// </summary>
    /// <returns>True if collision is found, false if not</returns>
    private bool IsColliding(Part newPart, List<Part> parts)
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
        foreach (Part nextPart in parts)
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

    bool CheckPartInBounds(Part part, GameObject boundingBox)
    {
        bool isInBounds;
        float distance;
        Vector3 direction;
        if (boundingBox.transform.childCount > 0)
        {
            List<Transform> boundingBoxes = new();
            for (int j = 0; j < boundingBox.transform.childCount; j++)
            {
                boundingBoxes.Add(boundingBox.transform.GetChild(j));
            }
            isInBounds = part.CheckInsideBoundingBoxWithChildren(boundingBoxes, out distance, out direction);
        }
        else
        {
            isInBounds = part.CheckInsideBoundingBox(boundingBox.transform, out distance, out direction);
        }
        Debug.Log($"{isInBounds}, dist {distance}, direction {direction}");
        var extents = part.Collider.size / 2;
        float x = extents.x;
        float y = extents.y;
        float z = extents.z;

        if (direction.x != 0 && direction.y != 0 && direction.z != 0) return isInBounds && distance > x && distance > y && distance > z;
        if (direction.x != 0 && direction.z != 0) return isInBounds && distance > x && distance > z;
        if (direction.x != 0 && direction.y != 0) return isInBounds && distance > x && distance > y;
        if (direction.y != 0 && direction.z != 0) return isInBounds && distance > y && distance > z;
        if (direction.x != 0) return isInBounds && distance > x;
        if (direction.y != 0) return isInBounds && distance > y;
        if (direction.z != 0) return isInBounds && distance > z;

        return false;
    }


    private IEnumerator AutoPlacement()
    {
        for (int i = 0; i < 250; i++)
        {
            if (i % 50 == 0)
            {
                LoadPartPrefabs();
            }
            PlaceNextPart();
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator AutoFloorPlacement()
    {
        for (int i = 0; i < 250; i++)
        {
            if (i % 50 == 0)
            {
                LoadFloorPartPrefabs();
            }
            PlaceNextFloorPart();
        }
        yield return new WaitForSeconds(1f);
    }

    public void OnPlaceNextPartButtonClicked()
    {
        PlaceNextPart();
    }

    public void OnAutoPlacementButtonClicked()
    {
        _autoPlacementCoroutine = AutoPlacement();
        StartCoroutine(_autoPlacementCoroutine);
    }

    public void OnStopButtonClicked()
    {
        StopCoroutine(_autoPlacementCoroutine);
    }

    public void OnAutoFloorPlacementButtonClicked()
    {
        StartCoroutine(AutoFloorPlacement());
    }

    // visualize the sphere of compute penetration (in which checking for collision)
    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_collisionTestSpherePosition, _radius);
    }*/
}
