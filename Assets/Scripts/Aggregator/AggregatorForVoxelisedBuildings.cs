using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AggregatorForVoxelisedBuildings : MonoBehaviour
{
    #region VARIABLES
    public VoxelGrid Grid;

    public Dictionary<Voxel, List<Collider>> VoxelisedElements;
    private Dictionary<Collider, List<Voxel>> _floorVoxelsAndColliders = new();
    private Dictionary<Collider, List<Voxel>> _wallVoxelsAndColliders = new();
    private Dictionary<int, List<Voxel>> _voxelisedWalls = new();
    private Dictionary<int, List<Voxel>> _voxelisedFloors = new();

    private IEnumerator _autoPlacementCoroutine;

    //private readonly float _radius = 25f; // check for penetration within a radius of 50m (radius of a sphere)
    private readonly int _maxNeighboursToCheck = 50; // how many neighbouring colliders to check in IsColliding function
    private readonly float _overlapTolerance = 0.03f; // used by compute penetration
    private readonly float _connectionTolerance = 0.2f; // tolerance of matching connections
    private float _vertexDistanceTolerance = 0.5f; // tolerance used when checking for vertices inside BBs
    private bool _connectionMatchingEnabled = true;

    private Collider[] _neighbours; // "ingredient" of compute penetration
    private Vector3 _collisionTestSpherePosition = Vector3Int.zero; // "ingredient" of compute penetration

    private List<Part> _parts = new(); // LIBRARY OF PARTS
    private List<Part> _floorParts = new(); // LIBRARY OF FLOOR PARTS
    private List<Part> _placedParts = new();
    private List<Part> _placedFloorParts = new();

    private int _currentFloorLayer = 0;
    private int _currentWallLayer = 0;
    #endregion

    public void Start()
    {
        _neighbours = new Collider[_maxNeighboursToCheck]; // initialize neighbors' array
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
        //Add one more arch so that there are more arches into the aggregation
        GameObject prefab08P_c = Resources.Load<GameObject>("Prefabs/Parts/08P_c");
        _parts.Add(new Part(prefab08P_c));
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
    /// <summary>
    /// Loading parts and get a useful dictionary format
    /// </summary>
    public void Initialise(float voxelSize)
    {
        _vertexDistanceTolerance = voxelSize; // how far a vertex can be from the center of a voxel to be considered inside
        LoadPartPrefabs();
        LoadFloorPartPrefabs();
        foreach (var kv in VoxelisedElements) 
        {
            Collider floorOrWallCollider = new(); // collider that could be floor or wall
            bool isFloorVoxel = false;
            bool isWallVoxel = false;
            foreach (var collider in kv.Value) // foreach collider check which element they belong to, and exclude anything but wall and floor
            {
                if (collider.transform.CompareTag("DECO_Floors"))
                {
                    isFloorVoxel = true;
                    floorOrWallCollider = collider;
                }
                if (collider.transform.CompareTag("DECO_Walls"))
                {
                    if (!isFloorVoxel)
                    {
                        isWallVoxel = true;
                        floorOrWallCollider = collider;
                    }
                }
                if (collider.transform.CompareTag("DECO_Columns")) break;
            }

            if (isFloorVoxel)
            {
                if (_floorVoxelsAndColliders.ContainsKey(floorOrWallCollider)) // if this collider is in the dictionary >
                    _floorVoxelsAndColliders[floorOrWallCollider].Add(kv.Key); // add the voxel to the list of voxels inside the dictionary
                else
                    _floorVoxelsAndColliders[floorOrWallCollider] = new() { kv.Key }; // if not > add the collider to the dictionary
            }
            if (isWallVoxel)
            {
                if (_wallVoxelsAndColliders.ContainsKey(floorOrWallCollider))
                    _wallVoxelsAndColliders[floorOrWallCollider].Add(kv.Key);
                else
                    _wallVoxelsAndColliders[floorOrWallCollider] = new() { kv.Key };
            }
        }

        
        // setting the voxels for each Ylayer for floors
        int yLayer = 0;
        foreach (var collider in _floorVoxelsAndColliders.Keys)
        {
            // just swap the order of the keyvalues inside the dictionary: BECAUSE WE WANT TO RECOGNIZE VOXELS BY LAYER AND NOT COLLIDER ANYMORE (compared to aggregator before deco)
            _voxelisedFloors[yLayer] = _floorVoxelsAndColliders[collider];
            // go to the next layer
            yLayer++;
        }
        // setting the voxels for each Ylayer for walls
        yLayer = 0;
        foreach (var collider in _wallVoxelsAndColliders.Keys)
        {
            // just swap the order of the keyvalues inside the dictionary
            _voxelisedWalls[yLayer] = _wallVoxelsAndColliders[collider];
            yLayer++;
        }
    }

    /// <summary>
    /// Place first wall part to the min corner voxel on a specific layer
    /// </summary>
    public void PlaceFirstWallPart()
    {
        _parts.Shuffle();
        var cornerVoxelCentreX = _voxelisedWalls[_currentWallLayer].Min(v => v.Centre.x);
        var cornerVoxelCentreY = _voxelisedWalls[_currentWallLayer].Min(v => v.Centre.y);
        var cornerVoxelCentreZ = _voxelisedWalls[_currentWallLayer].Min(v => v.Centre.z);
        for (int i = 0; i < _parts.Count; i++)
        {
            Part part = _parts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;
            for (int j = 0; j < 4; j++)
            {
                part.PlaceFirstPart(new Vector3(cornerVoxelCentreX + extents.x, cornerVoxelCentreY + extents.y, cornerVoxelCentreZ + extents.z), Quaternion.Euler(new Vector3(0, 90 * j, 0)));
                bool isInside = IsInsideWallVoxels(part);
                if (isInside)
                {
                    _parts.Remove(part);
                    _placedParts.Add(part);
                    part.Name = $"{part.Name} added {_placedParts.Count} (wall)";
                    return;
                }
                else
                {
                    part.ResetPart();
                    Debug.Log($"First part {part.Name} was outside");
                }
            }
        }
    }

    /// <summary>
    /// Place first floor part to the min corner voxel on a specific layer
    /// </summary>
    public void PlaceFirstFloorPart()
    {
        _floorParts.Shuffle();
        var cornerVoxelCentreX = _voxelisedFloors[_currentFloorLayer].Min(v => v.Centre.x);
        var cornerVoxelCentreY = _voxelisedFloors[_currentFloorLayer].Min(v => v.Centre.y);
        var cornerVoxelCentreZ = _voxelisedFloors[_currentFloorLayer].Min(v => v.Centre.z);
        for (int i = 0; i < _floorParts.Count; i++)
        {
            Part part = _floorParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;
            for (int j = 0; j < 4; j++)
            {
                part.PlaceFirstPart(new Vector3(cornerVoxelCentreX + extents.x, cornerVoxelCentreY, cornerVoxelCentreZ + extents.y), Quaternion.Euler(new Vector3(90, 90 * j, 0)));
                bool isInside = IsInsideFloorVoxels(part);
                if (isInside)
                {
                    _floorParts.Remove(part);
                    _placedFloorParts.Add(part);
                    part.Name = $"{part.Name} added {_placedFloorParts.Count} (floor)";
                    return;
                }
                else
                {
                    part.ResetPart();
                    Debug.Log($"First part {part.Name} was outside");
                }
            }
        }
    }
    #endregion

    #region PLACING NEXT PARTS (wall/floor)
    private void PlaceNextWallPart()
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

            if (IsColliding(unplacedConnection.ThisPart, _placedParts) || !IsInsideWallVoxels(unplacedConnection.ThisPart))
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

            if (IsColliding(unplacedConnection.ThisPart, _placedFloorParts) || !IsInsideFloorVoxels(unplacedConnection.ThisPart))
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
/// Check if a WALL part's vertices are within a certain tolerance in order to be considered inside voxelgrid
/// </summary>
    private bool IsInsideWallVoxels(Part part)
    {
        var vertices = part.Collider.sharedMesh.vertices;
        // check if part (vertices) are all within the voxels of current floor
        foreach (var vertex in vertices)
        {
            bool foundNearbyVoxelForVertex = false; // check in all voxels of a specific voxelised WALL element and Y-LAYER
            foreach (var voxel in _voxelisedWalls[_currentWallLayer])
            {
                var vertexToWorldSpace = part.GOPart.transform.TransformPoint(vertex); // take the world position of the vertex
                if ((vertexToWorldSpace - voxel.Centre).magnitude < _vertexDistanceTolerance)
                {
                    foundNearbyVoxelForVertex = true;
                    break; // go to next vertex
                }
            }
            if (!foundNearbyVoxelForVertex) return false;
        }
        return true;
    }

    /// <summary>
    /// Check if a FLOOR part's vertices are within a certain tolerance in order to be considered inside voxelgrid
    /// </summary>
    private bool IsInsideFloorVoxels(Part part)
    {
        var vertices = part.Collider.sharedMesh.vertices;
        foreach (var vertex in vertices)
        {
            bool foundNearbyVoxelForVertex = false; // check in all voxels of a specific voxelised FLOOR element and Y-LAYER
            foreach (var voxel in _voxelisedFloors[_currentFloorLayer])
            {
                var vertexToWorldSpace = part.GOPart.transform.TransformPoint(vertex); // take the world position of the vertex
                if ((vertexToWorldSpace - voxel.Centre).magnitude < _vertexDistanceTolerance)
                    {
                        foundNearbyVoxelForVertex = true;
                        break; // go to next vertex
                }
            }
            if (!foundNearbyVoxelForVertex) return false;
        }
        return true;
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
                out _, out float distance);

            // overlapping colliders and too big overlap --> IsColliding = true --> not place the part
            if (isOverlapping && distance > _overlapTolerance) return true;
        }
        // if we reach this point there's no collision --> place part
        return false;
    }
    #endregion

    #region CLICK > RAYCAST > TRY TO PLACE PART WHERE CLICKED
    // if what we hit is a collider > check if the collider has boundingbox tag >
    // yes > create new vector3 position where to place the part according to the thinnest axis of the wall
    // no > just debug, don't do anything
    /*private void RaycastToMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var whatWeHit = hit.collider;
            if (whatWeHit.CompareTag("BoundingBox"))
            {
                var hitPoint = hit.point;

                //SPHERES ON MIN AND MAX Y OF BB JUST TO TEST
                //var hitPointGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //hitPointGO.transform.position = hitPoint;
                //hitPointGO.transform.localScale = Vector3.one * 0.5f;
                //hitPointGO.GetComponent<Renderer>().material.color = Color.blue;

                var boundingBoxMinY = whatWeHit.bounds.min.y;

                //var hitPointWithMinBBYGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //hitPointWithMinBBYGO.transform.position = new Vector3(hitPoint.x, boundingBoxMinY, hitPoint.z);
                //hitPointWithMinBBYGO.transform.localScale = Vector3.one * 0.5f;
                //hitPointWithMinBBYGO.GetComponent<Renderer>().material.color = Color.red;

                Debug.Log($"Collider Bounds: {whatWeHit.bounds.size}");
                Debug.Log($"Collider Position: {whatWeHit.bounds.center}");

                var sizeX = whatWeHit.bounds.size.x;
                var sizeZ = whatWeHit.bounds.size.z;
                var minSizeAxis = Mathf.Min(sizeX, sizeZ);
                var centerX = whatWeHit.bounds.center.x;
                var centerZ = whatWeHit.bounds.center.z;

                Vector3 pos;
                if (minSizeAxis == sizeX)
                {
                    pos = new Vector3(centerX, boundingBoxMinY, hitPoint.z);
                }
                else
                {
                    pos = new Vector3(hitPoint.x, boundingBoxMinY, centerZ);
                }

                Debug.Log($"Raycast hit coords: {hit.point} ; boundingBoxMinY: {boundingBoxMinY}");
                TryPlacePartAtPosition(pos);
            }
            else Debug.Log($"Raycast hit {hit.point} but hit {whatWeHit.transform.name}, instead of a GameObject tagged with 'BoundingBox'");
        }
    }*/

    // part placement after raycast
    /*private void TryPlacePartAtPosition(Vector3 pos)
    {
        LoadPartPrefabs();
        _parts.Shuffle();
        for (int i = 0; i < _parts.Count; i++)
        {
            //foreach part take the bounds size in y
            Part part = _parts[i];
            var sizeY = part.Collider.sharedMesh.bounds.size.y;
            // place the part with an offset because the placement happens by center point
            var positionWithYOffset = new Vector3(pos.x, pos.y + (sizeY / 2), pos.z);
            Debug.Log($"positionWithYOffset: {positionWithYOffset}");

            //try to position the part by rotating it in all the directions and checking if it collides and is in bounds
            for (int j = 0; j < 4; j++)
            {
                part.PlaceFirstPart(positionWithYOffset, Quaternion.Euler(new Vector3(0, 90 * j, 0)));
                bool isColliding = IsColliding(part, _placedParts);
                bool isInside = CheckPartInBounds(_boundingBoxes[_currentFloorLayer], part);
                if (isInside && !isColliding)
                {
                    _parts.Remove(part);
                    _placedParts.Add(part);
                    part.Name = $"{part.Name} added {_placedParts.Count} (wall)";
                    return;
                }
                else
                {
                    part.ResetPart();
                    Debug.Log($"First part {part.Name} was outside");
                }
            }
        }
    }*/
    #endregion

    #region BUTTONS FOR COROUTINES
    private IEnumerator AutoPlacement()
    {
        Debug.Log("Auto wall placement started");
        for (int i = 0; i < 250; i++)
        {
            PlaceNextWallPart();
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

    public void OnAutoWallPlacementButtonClicked()
    {
        _autoPlacementCoroutine = AutoPlacement();
        StartCoroutine(_autoPlacementCoroutine);
    }

    public void OnAutoFloorPlacementButtonClicked()
    {
        StartCoroutine(AutoFloorPlacement());
    }
    #endregion
}
