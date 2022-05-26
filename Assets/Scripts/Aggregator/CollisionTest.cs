using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CollisionTest : MonoBehaviour
{
    #region VARIABLES
    //text with info about collision during play mode (top left game scene)
    [SerializeField]
    private Text _debugText;

    [SerializeField]
    private Text _lastPartPlacedText;

    //to decide whether aggregate parts according to length or not
    [SerializeField]
    private Toggle _toggleConnectionMatching;

    private IEnumerator _autoPlacementCoroutine;

    //private readonly float _radius = 25f; // check for penetration within a radius of 50m (radius of a sphere)
    private readonly int _maxNeighboursToCheck = 50; // how many neighbouring colliders to check in IsColliding function
    private readonly float _overlapTolerance = 0.03f; // used by compute penetration
    private readonly float _connectionTolerance = 0.2f; // tolerance of matching connections
    private readonly float _vertexDistanceTolerance = 0.05f; // tolerance used when checking for vertices inside BBs
    private bool _connectionMatchingEnabled = true;

    private Collider[] _neighbours; // "ingredient" of compute penetration
    private Vector3 _collisionTestSpherePosition = Vector3Int.zero; // "ingredient" of compute penetration

    private List<Part> _parts = new(); // LIBRARY OF PARTS
    private List<Part> _floorParts = new(); // LIBRARY OF FLOOR PARTS
    private List<Part> _placedParts = new();
    private List<Part> _placedFloorParts = new();

    GameObject _boundingBox;
    GameObject _floorBB;
    #endregion

    public void Start()
    {
        _boundingBox = GameObject.Find("bb_walls_typo2");
        _floorBB = GameObject.Find("bb_floor_typo2");

        // check if the toggle for matching/non matching connections is on/off
        _toggleConnectionMatching.onValueChanged.AddListener(delegate { _connectionMatchingEnabled = !_connectionMatchingEnabled; });

        LoadPartPrefabs();
        _neighbours = new Collider[_maxNeighboursToCheck]; // initialize neighbors' array
        PlaceFirstPart();
        EnableAllConnections();

        LoadFloorPartPrefabs();
        PlaceFirstFloorPart();
        EnableAllFloorConnections();

        _autoPlacementCoroutine = AutoPlacement();
    }

    #region LOADING PREFABS (wall/floor)
    private void LoadPartPrefabs()
    {
        if (_parts.Count > 0)
        {
            _parts.ForEach(p => GameObject.Destroy(p.GOPart));
        }

        //Load all the prefabs
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

        //Select the prefabs with tag Part
        _parts = prefabs.Where(g =>
        {
            var childCount = g.transform.childCount;
            for (int j = 0; j < childCount; ++j)
            {
                var child = g.transform.GetChild(j);
                if (child.CompareTag("onlyWallConn") || child.CompareTag("bothWallFloorConn")) return true;
                // Do something based on tag
            }
            return false;
        }).Select(g => new Part(g)).ToList();

        EnableAllConnections();
    }

    private void LoadFloorPartPrefabs()
    {
        if (_floorParts.Count > 0)
        {
            _floorParts.ForEach(p => GameObject.Destroy(p.GOPart));
        }

        //Load all the prefabs
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

        //Select the prefabs with tag Part
        _floorParts = prefabs.Where(g =>
        {
            var childCount = g.transform.childCount;
            for (int j = 0; j < childCount; ++j)
            {
                var child = g.transform.GetChild(j);
                if (child.CompareTag("onlyFloorConn") || child.CompareTag("bothWallFloorConn")) return true;
                // Do something based on tag
            }
            return false;
        }).Select(g => new Part(g)).ToList();

        EnableAllFloorConnections();
    }
    #endregion

    #region ENABLE CONNECTIONS (wall/floor)
    private void EnableAllConnections()
    {
        _parts.ForEach(part => part.Connections.ForEach(connection => connection.Available = true));
    }

    private void EnableAllFloorConnections()
    {
        _floorParts.ForEach(part => part.Connections.ForEach(connection => connection.Available = true));
    }
    #endregion

    #region PLACING FIRST PARTS (wall/floor)
    private void PlaceFirstPart()
    {
        int rndPartIndex = Random.Range(0, _parts.Count);
        Part randomPart = _parts[rndPartIndex];
        var size = randomPart.Collider.sharedMesh.bounds.size;
        Debug.Log(size);
        var extents = size / 2;


        randomPart.PlaceFirstPart(new Vector3(extents.x - size.x, extents.y + 0.3f, extents.z - size.z) + Vector3.one * 0.02f, Quaternion.Euler(new Vector3(0, 0, 0)));
        bool isInside = CheckPartInBounds(_boundingBox, randomPart);
        if (isInside)
        {
            _parts.Remove(randomPart);
            _placedParts.Add(randomPart);
            randomPart.Name = $"{randomPart.Name} added {_placedParts.Count} (wall)";
        }
        else
        {
            randomPart.ResetPart();
            Debug.Log($"First part {randomPart.Name} was outside");
        }
        
    }

    private void PlaceFirstFloorPart()
    {
        int rndPartIndex = Random.Range(0, _floorParts.Count);
        Part randomPart = _floorParts[rndPartIndex];
        var size = randomPart.Collider.sharedMesh.bounds.size;
        var extents = size / 2;

        randomPart.PlaceFirstPart(new Vector3(extents.x - size.x, extents.z, -extents.y) + Vector3.one * 0.02f, Quaternion.Euler(new Vector3(90, 0, 0)));
        bool isInside = CheckPartInBounds(_floorBB, randomPart);
        if (isInside)
        {
            _floorParts.Remove(randomPart);
            _placedFloorParts.Add(randomPart);
            randomPart.Name = $"{randomPart.Name} added {_placedFloorParts.Count} (floor)";
        }
        else
        {
            randomPart.ResetPart();
            Debug.Log($"First part {randomPart.Name} was outside");
        }

    }
    #endregion

    #region PLACING NEXT PARTS (wall/floor)
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

            if (IsColliding(unplacedConnection.ThisPart, _placedParts) || !CheckPartInBounds(_boundingBox, unplacedConnection.ThisPart))
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

        availableConnectionsInUnplacedFloorParts.Shuffle();
        foreach (Connection unplacedConnection in availableConnectionsInUnplacedFloorParts)
        {
            unplacedConnection.ThisPart.PositionPart(randomAvailableConnectionInFloor, unplacedConnection);

            if (IsColliding(unplacedConnection.ThisPart, _placedFloorParts) || !CheckPartInBounds(_floorBB, unplacedConnection.ThisPart))
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
    #endregion

    #region NECESSARY STUFF (check connections compatibility/collision/bounds)
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

    bool CheckPartInBounds(GameObject boundingMesh, Part partToCheck)
    {
        bool isInBounds;
        //float distance;
        //Vector3 direction;

        List<Transform> boundingMeshes = new();
        if (boundingMesh.transform.childCount > 0)
        {
            for (int j = 0; j < boundingMesh.transform.childCount; j++)
            {
                boundingMeshes.Add(boundingMesh.transform.GetChild(j));
            }
        }
        else boundingMeshes.Add(boundingMesh.transform);

        isInBounds = IsValidPlacement(boundingMeshes, partToCheck);
        //Debug.Log($"{isInBounds}, dist {distance}, direction {direction}");
        //var extents = part.Collider.bounds.size / 2;
        //float x = extents.x;
        //float y = extents.y;
        //float z = extents.z;

        //if (direction.x != 0 && direction.y != 0 && direction.z != 0) return isInBounds && distance > x && distance > y && distance > z;
        //if (direction.x != 0 && direction.z != 0) return isInBounds && distance > x && distance > z;
        //if (direction.x != 0 && direction.y != 0) return isInBounds && distance > x && distance > y;
        //if (direction.y != 0 && direction.z != 0) return isInBounds && distance > y && distance > z;
        //if (direction.x != 0) return isInBounds && distance > x;
        //if (direction.y != 0) return isInBounds && distance > y;
        //if (direction.z != 0) return isInBounds && distance > z;
        //}
        //else
        //{
        //    isInBounds = part.CheckInsideBoundingBoxWithChildren(new List<Transform>() { boundingBox.transform });
        //    Debug.Log($"{isInBounds}, dist {distance}, direction {direction}");

        //    return isInBounds;
        //}

        return isInBounds;
    }

    /// <summary>
    /// Checks if a part has been placed in a valid position (i.e. inside a given bounding box)
    /// </summary>
    /// <param name="boxes">List of transforms of bounding boxes (i.e. floor BB or wall BB)</param>
    /// <param name="partToCheck">Part to check (i.e. the new part we are trying to add into the building)</param>
    /// <returns></returns>
    public bool IsValidPlacement(List<Transform> boxes, Part partToCheck)
    {
        var partMesh = partToCheck.Collider.sharedMesh;
        var partVertices = partMesh.vertices;
        foreach (var bb in boxes)
        {
            var bbCollider = bb.GetComponent<Collider>();
            //if (bbCollider is MeshCollider) bbCollider = bb.GetComponent<MeshCollider>();
            //else if (bbCollider is BoxCollider) bbCollider = bb.GetComponent<BoxCollider>();
            int verticesInBounds = 0;
            foreach (var vertex in partVertices)
            {
                var vertexToWorldSpace = partToCheck.GOPart.transform.TransformPoint(vertex);
                //var vertGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //vertGo.transform.localPosition = vertexToWorldSpace;
                //vertGo.transform.localScale = Vector3.one * 0.05f;

                if ((vertexToWorldSpace - bbCollider.ClosestPoint(vertexToWorldSpace)).magnitude > _vertexDistanceTolerance) verticesInBounds++;
                //if ((transVertex - bbCollider.ClosestPointOnBounds(transVertex)).magnitude > _vertexDistanceTolerance) return false;
                //if (!bbCollider.bounds.Contains(transVertex)) return false;
                //if (!Util.PointInsideCollider(transVertex, bbCollider)) return false;
            }
            if (verticesInBounds > 4 && verticesInBounds < partVertices.Count()) return false;
        }
        return true;
    }
    #endregion

    #region BUTTONS FOR COROUTINES
    private IEnumerator AutoPlacement()
    {
        Debug.Log("Auto wall placement started");
        for (int i = 0; i < 250; i++)
        {
            PlaceNextPart();
            LoadPartPrefabs();
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("Auto wall placement finished");
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator AutoFloorPlacement()
    {
        for (int i = 0; i < 250; i++)
        {
            PlaceNextFloorPart();
            LoadFloorPartPrefabs();
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
        _autoPlacementCoroutine = AutoPlacement();
        StartCoroutine(_autoPlacementCoroutine);
    }

    public void OnStopButtonClicked()
    {
        StopCoroutine(_autoPlacementCoroutine);
        Debug.Log("Auto wall placement stopped");
    }

    public void OnAutoFloorPlacementButtonClicked()
    {
        StartCoroutine(AutoFloorPlacement());
    }
    #endregion
}
