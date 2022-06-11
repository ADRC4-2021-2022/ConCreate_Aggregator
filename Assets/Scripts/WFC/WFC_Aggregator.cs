using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WFC_Aggregator : MonoBehaviour
{
    #region VARIABLES
    private IEnumerator _autoWallPlacementCoroutine;
    private IEnumerator _autoFloorPlacementCoroutine;

    public Vector3 _tileSize;
    private ConstraintSolver _solver;
    private readonly int _maxNeighboursToCheck = 50; // how many neighbouring colliders to check in IsColliding function
    private readonly float _overlapTolerance = 0.03f; // used by compute penetration
    private readonly float _connectionTolerance = 0.2f; // tolerance of matching connections
    private readonly float _vertexDistanceTolerance = 0.1f;
    private bool _connectionMatchingEnabled = true;
    private bool _hasBeenInitialised = false;

    private Collider[] _neighbours; // "ingredient" of compute penetration
    private Vector3 _collisionTestSpherePosition = Vector3Int.zero; // "ingredient" of compute penetration

    private List<Part> _wallParts = new(); // LIBRARY OF WALL PARTS
    private List<Part> _floorParts = new(); // LIBRARY OF FLOOR PARTS
    private List<Part> _placedWallParts = new();
    private List<Part> _placedFloorParts = new();

    private int _currentFloorLayer = 0;
    private int _currentWallLayer = 0;
    #endregion

    public void Start()
    {
        _neighbours = new Collider[_maxNeighboursToCheck]; // initialize neighbors' array
        _autoWallPlacementCoroutine = AutoWallPlacement();
        _autoFloorPlacementCoroutine = AutoFloorPlacement();
    }

    #region LOADING PREFABS (wall/floor)
    private void LoadWallPartPrefabs()
    {
        if (_wallParts.Count > 0)
        {
            _wallParts.ForEach(p => GameObject.Destroy(p.GOPart));
        }

        //Load all the prefabs
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/Parts");

        //Select the prefabs with tag Part
        _wallParts = prefabs.Where(g =>
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
        _wallParts.Add(new Part(prefab08P_c));
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
        _wallParts.ForEach(part => part.Connections.ForEach(connection => connection.Available = true));
    }

    private void EnableAllFloorConnections()
    {
        _floorParts.ForEach(part => part.Connections.ForEach(connection => connection.Available = true));
    }
    #endregion

    #region PLACING FIRST PARTS (wall/floor)
    public void Initialise(Vector3 tileSize, ConstraintSolver constraintSolver)
    {
        if (!_hasBeenInitialised)
        {
            _hasBeenInitialised = true;
            _tileSize = tileSize;
            _solver = constraintSolver;
            LoadWallPartPrefabs();
            LoadFloorPartPrefabs();
            PlaceFirstWallPart();
            PlaceFirstFloorPart();
        }
    }

    public void PlaceFirstWallPart()
    {
        _wallParts.Shuffle();
        var firstTile = _solver.GetSetTilesByYLayer(_currentWallLayer).First();
        for (int i = 0; i < _wallParts.Count; i++)
        {
            Part part = _wallParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;
            GameObject wallGO = null;
            for (int j = 0; j < firstTile.CurrentGo.transform.childCount; j++)
            {
                var child = firstTile.CurrentGo.transform.GetChild(j);
                if (child.name.Equals("Wall")) wallGO = child.GetChild(0).gameObject;
            }
            if (wallGO == null) Debug.Log($"Failed to find wall GO for tile {firstTile}");
            var minPoint = wallGO.GetComponent<MeshCollider>().ClosestPoint(Vector3.zero);
            for (int k = 0; k < 4; k++)
            {
                part.PlaceFirstPart(minPoint + extents, Quaternion.Euler(new Vector3(0, 90 * k, 0)));
                bool isInsideWithPositiveExtents = IsInsideWalls(part);
                if (isInsideWithPositiveExtents)
                {
                    _wallParts.Remove(part);
                    _placedWallParts.Add(part);
                    part.Name = $"{part.Name} added {_placedWallParts.Count} (wall)";
                    return;
                }
                else
                {
                    part.ResetPart();
                    Debug.Log($"First part {part.Name} was outside");
                }
                part.PlaceFirstPart(minPoint - extents, Quaternion.Euler(new Vector3(0, 90 * k, 0)));
                bool isInsideWithNegativeExtents = IsInsideWalls(part);
                if (isInsideWithNegativeExtents)
                {
                    _wallParts.Remove(part);
                    _placedWallParts.Add(part);
                    part.Name = $"{part.Name} added {_placedWallParts.Count} (wall)";
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

    public void PlaceFirstFloorPart()
    {
        _floorParts.Shuffle();
        var firstTile = _solver.GetSetTilesByYLayer(_currentFloorLayer).First();
        for (int i = 0; i < _floorParts.Count; i++)
        {
            Part part = _floorParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;
            GameObject floorGO = null;
            for (int j = 0; j < firstTile.CurrentGo.transform.childCount; j++)
            {
                var child = firstTile.CurrentGo.transform.GetChild(j);
                if (child.name.Equals("Floor")) floorGO = child.GetChild(0).gameObject;
            }
            if (floorGO == null) Debug.Log($"Failed to find floor GO for tile {firstTile}");
            var minPoint = floorGO.GetComponent<MeshCollider>().ClosestPoint(Vector3.zero);
            for (int k = 0; k < 4; k++)
            {
                part.PlaceFirstPart(new Vector3(minPoint.x + extents.x, extents.z, minPoint.z + extents.y), Quaternion.Euler(new Vector3(90, 90 * k, 0)));
                bool isInsideWithPositiveExtents = IsInsideFloors(part);
                if (isInsideWithPositiveExtents)
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
        foreach (Part placedPart in _placedWallParts)
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
        foreach (Part unplacedPart in _wallParts)
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

            if (IsColliding(unplacedConnection.ThisPart, _placedWallParts) || !IsInsideWalls(unplacedConnection.ThisPart))
            {
                //the part collided, so go to the next part in the list
                unplacedConnection.ThisPart.ResetPart();
            }
            else
            {
                //Set the part as placed
                unplacedConnection.ThisPart.PlacePart(unplacedConnection);
                randomAvailableConnectionInCurrentBuilding.Available = false;
                string newName = $"{unplacedConnection.ThisPart.Name} placed {_placedWallParts.Count + 1} (wall)";
                Vector3 unplacedPartPos = unplacedConnection.ThisPart.GOPart.transform.position;
                unplacedConnection.ThisPart.Name = newName;
                _wallParts.Remove(unplacedConnection.ThisPart);
                _placedWallParts.Add(unplacedConnection.ThisPart);
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

            if (IsColliding(unplacedConnection.ThisPart, _placedFloorParts) || !IsInsideFloors(unplacedConnection.ThisPart))
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

    private bool IsInsideWalls(Part part)
    {
        var vertices = part.Collider.sharedMesh.vertices;
        foreach (var vertex in vertices)
        {
            bool foundNearbyTileForVertex = false;
            foreach (var tile in _solver.GetSetTilesByYLayer(_currentWallLayer))
            {
                var vertexToWorldSpace = part.GOPart.transform.TransformPoint(vertex); // take the world position of the vertex
                GameObject wallGO = null;
                for (int i = 0; i < tile.CurrentGo.transform.childCount; i++)
                {
                    var child = tile.CurrentGo.transform.GetChild(i);
                    if (child.name.Equals("Wall")) wallGO = child.GetChild(0).gameObject;
                }
                if (wallGO == null) return false;
                if ((vertexToWorldSpace - wallGO.GetComponent<MeshCollider>().ClosestPoint(vertexToWorldSpace)).magnitude < _vertexDistanceTolerance)
                {
                    foundNearbyTileForVertex = true;
                    break; // go to next vertex
                }
            }
            if (!foundNearbyTileForVertex) return false;
        }
        return true;
    }

    private bool IsInsideFloors(Part part)
    {
        var vertices = part.Collider.sharedMesh.vertices;
        foreach (var vertex in vertices)
        {
            bool foundNearbyTileForVertex = false;
            foreach (var tile in _solver.GetSetTilesByYLayer(_currentFloorLayer))
            {
                var vertexToWorldSpace = part.GOPart.transform.TransformPoint(vertex); // take the world position of the vertex
                GameObject floorGO = null;
                for (int i = 0; i < tile.CurrentGo.transform.childCount; i++)
                {
                    var child = tile.CurrentGo.transform.GetChild(i);
                    if (child.name.Equals("Floor")) floorGO = child.GetChild(0).gameObject;
                }
                if (floorGO == null) return false;
                if ((vertexToWorldSpace - floorGO.GetComponent<MeshCollider>().ClosestPoint(vertexToWorldSpace)).magnitude < _vertexDistanceTolerance)
                {
                    foundNearbyTileForVertex = true;
                    break; // go to next vertex
                }
            }
            if (!foundNearbyTileForVertex) return false;
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
    private IEnumerator AutoWallPlacement()
    {
        Debug.Log("Auto wall placement started");
        for (int i = 0; i < 250; i++)
        {
            PlaceNextWallPart();
            LoadWallPartPrefabs();
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("Auto wall placement finished");
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator AutoFloorPlacement()
    {
        Debug.Log("Auto floor placement started");
        for (int i = 0; i < 250; i++)
        {
            PlaceNextFloorPart();
            LoadFloorPartPrefabs();
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("Auto floor placement finished");
        yield return new WaitForSeconds(1f);
    }

    public void OnAutoWallPlacementButtonClicked()
    {
        _autoWallPlacementCoroutine = AutoWallPlacement();
        StartCoroutine(_autoWallPlacementCoroutine);
    }

    public void OnAutoFloorPlacementButtonClicked()
    {
        _autoFloorPlacementCoroutine = AutoFloorPlacement();
        StartCoroutine(_autoFloorPlacementCoroutine);
    }
    #endregion
}
