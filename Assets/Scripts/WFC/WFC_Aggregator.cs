using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WFC_Aggregator : MonoBehaviour
{
    #region VARIABLES
    public IEnumerator ExteriorWallsPlacementCoroutine;
    public IEnumerator AutoWallPlacementCoroutine;
    public IEnumerator AutoFloorPlacementCoroutine;

    public Vector3 _tileSize;
    private ConstraintSolver _solver;
    private readonly int _maxNeighboursToCheck = 50; // how many neighbouring colliders to check in IsColliding function
    private readonly float _overlapTolerance = 0.03f; // used by compute penetration
    private readonly float _connectionTolerance = 0.2f; // tolerance of matching connections
    private readonly float _vertexDistanceTolerance = 0.1f;
    private readonly float _tileToPartMaxDistance = 4f;
    private bool _connectionMatchingEnabled = true;
    private bool _hasBeenInitialised = false;

    private List<Part> _wallParts = new(); // LIBRARY OF WALL PARTS
    private List<Part> _floorParts = new(); // LIBRARY OF FLOOR PARTS
    private List<Part> _placedWallParts = new();
    private List<Part> _placedFloorParts = new();

    private int _currentFloorLayer = 0;
    private int _currentWallLayer = 0;
    private int _floorFailureCounter = 0;
    private int _wallFailureCounter = 0;
    private readonly int _failureTolerance = 10;

    //_saveRecorder.SaveScreen() - and look for a folder in the project with screenshots of frames saved
    [SerializeField]
    private ScreenRecorder _screenRecorder;
    #endregion

    #region LOADING PREFABS (wall/floor)
    private void LoadWallPartPrefabs()
    {
        if (_wallParts.Count > 0)
        {
            _wallParts.ForEach(p => GameObject.Destroy(p.GOPart));
        }

        //Load all the prefabs
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Prefabs/PartsForWFC");

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
            _currentWallLayer = _solver.GetGroundFloorLayerNumber();
            _currentFloorLayer = _currentWallLayer;
        }
    }

    public void PlaceExteriorWallPartRandomPosition()
    {
        _wallParts.Shuffle();
        var setTilesInCurrentLayer = _solver.ExteriorWallsByYLayer[_currentWallLayer];
        //var setTilesInCurrentLayer = _solver.TileGOs.Where(GO => Util.RealPositionToIndex(GO.transform.position, _tileSize).y == _currentWallLayer).ToList();
        var randomWallGO = setTilesInCurrentLayer[Random.Range(0, setTilesInCurrentLayer.Count)];
        for (int i = 0; i < _wallParts.Count; i++)
        {
            Part part = _wallParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;

            var minPoint = randomWallGO.GetComponent<MeshCollider>().ClosestPoint(Vector3.zero);
            for (int k = 0; k < 4; k++)
            {
                part.PlaceFirstPart(minPoint + extents, Quaternion.Euler(new Vector3(0, 90 * k, 0)));
                bool isInsideWithPositiveExtents = !IsColliding(part, _placedWallParts) && IsInsideExteriorWalls(part);
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
                bool isInsideWithNegativeExtents = !IsColliding(part, _placedWallParts) && IsInsideWalls(part);
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
        Debug.Log("Failed to place new part in random position");
    }

    public void PlaceWallPartRandomPosition()
    {
        _wallParts.Shuffle();
        var setTilesInCurrentLayer = _solver.GetTilesByYLayer(_currentWallLayer).Where(t => t.CurrentGo != null).ToList();
        //var setTilesInCurrentLayer = _solver.TileGOs.Where(GO => Util.RealPositionToIndex(GO.transform.position, _tileSize).y == _currentWallLayer).ToList();
        var randomTile = setTilesInCurrentLayer[Random.Range(0, setTilesInCurrentLayer.Count)];
        for (int i = 0; i < _wallParts.Count; i++)
        {
            Part part = _wallParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;
            GameObject wallGO = null;
            for (int j = 0; j < randomTile.CurrentGo.transform.childCount; j++)
            {
                var child = randomTile.CurrentGo.transform.GetChild(j);
                if (child.name.Equals("Wall")) wallGO = child.GetChild(0).gameObject;
            }
            if (wallGO == null) Debug.Log($"Failed to find wall GO for tile {randomTile}");
            var minPoint = wallGO.GetComponent<MeshCollider>().ClosestPoint(Vector3.zero);
            for (int k = 0; k < 4; k++)
            {
                part.PlaceFirstPart(minPoint + extents, Quaternion.Euler(new Vector3(0, 90 * k, 0)));
                bool isInsideWithPositiveExtents = !IsColliding(part, _placedWallParts) && IsInsideWalls(part);
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
                bool isInsideWithNegativeExtents = !IsColliding(part, _placedWallParts) && IsInsideWalls(part);
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
        Debug.Log("Failed to place new part in random position");
    }

    public void PlaceFloorPartRandomPosition()
    {
        _floorParts.Shuffle();
        var setTilesInCurrentLayer = _solver.GetTilesByYLayer(_currentFloorLayer).Where(t => t.CurrentGo != null).ToList();
        //var setTilesInCurrentLayer = _solver.TileGOs.Where(GO => Util.RealPositionToIndex(GO.transform.position, _tileSize).y == _currentFloorLayer).ToList();
        var randomTile = setTilesInCurrentLayer[Random.Range(0, setTilesInCurrentLayer.Count)];
        for (int i = 0; i < _floorParts.Count; i++)
        {
            Part part = _floorParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;
            GameObject floorGO = null;
            for (int j = 0; j < randomTile.CurrentGo.transform.childCount; j++)
            {
                var child = randomTile.CurrentGo.transform.GetChild(j);
                if (child.name.Equals("Floor")) floorGO = child.GetChild(0).gameObject;
            }
            if (floorGO == null) Debug.Log($"Failed to find floor GO for tile {randomTile}");
            var minPoint = floorGO.GetComponent<MeshCollider>().ClosestPoint(Vector3.zero);
            for (int k = 0; k < 4; k++)
            {
                part.PlaceFirstPart(new Vector3(minPoint.x + extents.x, extents.z, minPoint.z + extents.y), Quaternion.Euler(new Vector3(90, 90 * k, 0)));
                bool isInside = !IsColliding(part, _placedFloorParts) && IsInsideFloors(part);
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
        Debug.Log("Failed to place new part in random position");
    }
    #endregion

    #region PLACING NEXT PARTS (wall/floor)
    /// <summary>
    /// Places next part in the walls
    /// </summary>
    /// <param name="exteriorWalls">True if we want to place the exterior walls, false if we want to place interior walls</param>
    /// <returns></returns>
    private bool PlaceNextWallPart(bool exteriorWalls)
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

            if (IsColliding(unplacedConnection.ThisPart, _placedWallParts)
                || (!exteriorWalls && !IsInsideWalls(unplacedConnection.ThisPart))
                || (exteriorWalls && !IsInsideExteriorWalls(unplacedConnection.ThisPart)))
            {
                //the part collided, so go to the next part in the list
                unplacedConnection.ThisPart.ResetPart();
            }
            else
            {
                //Set the part as placed
                unplacedConnection.ThisPart.PlacePart(unplacedConnection);
                Debug.Log($"{_wallFailureCounter} wall failures");
                _wallFailureCounter = 0;

                randomAvailableConnectionInCurrentBuilding.Available = false;
                string newName = $"{unplacedConnection.ThisPart.Name} placed {_placedWallParts.Count + 1} (wall)";
                unplacedConnection.ThisPart.Name = newName;
                _wallParts.Remove(unplacedConnection.ThisPart);
                _placedWallParts.Add(unplacedConnection.ThisPart);
                return true;
            }
        }
        return false;
    }

    private bool PlaceNextFloorPart()
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
                Debug.Log($"{_floorFailureCounter} floor failures");
                _floorFailureCounter = 0;

                randomAvailableConnectionInFloor.Available = false;
                string newName = $"{unplacedConnection.ThisPart.Name} placed {_placedFloorParts.Count + 1} (floor)";
                unplacedConnection.ThisPart.Name = newName;
                _floorParts.Remove(unplacedConnection.ThisPart);
                _placedFloorParts.Add(unplacedConnection.ThisPart);
                return true;
            }
        }
        return false;
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

    private bool IsInsideExteriorWalls(Part part)
    {
        var vertices = part.Collider.sharedMesh.vertices;
        foreach (var vertex in vertices)
        {
            bool foundNearbyTileForVertex = false;
            foreach (var wallGO in _solver.ExteriorWallsByYLayer[_currentWallLayer].Where(GO => GO != null))
            {
                //check if the part is within an acceptable distance to be able to collide
                Vector3 tilePosition = wallGO.transform.position;
                Vector3 partPosition = part.GOPart.transform.position;
                if ((tilePosition - partPosition).magnitude < _tileToPartMaxDistance)
                {
                    var vertexToWorldSpace = part.GOPart.transform.TransformPoint(vertex); // take the world position of the vertex
                    if ((vertexToWorldSpace - wallGO.GetComponent<MeshCollider>().ClosestPoint(vertexToWorldSpace)).magnitude < _vertexDistanceTolerance)
                    {
                        foundNearbyTileForVertex = true;
                        break; // go to next vertex
                    }
                }
                if (foundNearbyTileForVertex)
                {
                    break;
                }
            }
            if (!foundNearbyTileForVertex)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsInsideWalls(Part part)
    {
        var vertices = part.Collider.sharedMesh.vertices;
        foreach (var vertex in vertices)
        {
            bool foundNearbyTileForVertex = false;
            foreach (var tile in _solver.GetTilesByYLayer(_currentWallLayer).Where(t => t.CurrentGo != null))
            {
                //check if the part is within an acceptable distance to be able to collide
                Vector3 tilePosition = tile.CurrentGo.transform.position;
                Vector3 partPosition = part.GOPart.transform.position;
                if ((tilePosition - partPosition).magnitude < _tileToPartMaxDistance)
                {
                    var vertexToWorldSpace = part.GOPart.transform.TransformPoint(vertex); // take the world position of the vertex
                    List<GameObject> wallGOs = new();
                    for (int i = 0; i < tile.CurrentGo.transform.childCount; i++)
                    {
                        var child = tile.CurrentGo.transform.GetChild(i);
                        if (child.gameObject.layer == 7)
                        {
                            for (int j = 0; j < child.transform.childCount; j++)
                            {
                                wallGOs.Add(child.GetChild(j).gameObject);
                            }
                        }
                    }
                    if (wallGOs.Count == 0) return false;
                    foreach (var wallGO in wallGOs)
                    {
                        if ((vertexToWorldSpace - wallGO.GetComponent<MeshCollider>().ClosestPoint(vertexToWorldSpace)).magnitude < _vertexDistanceTolerance)
                        {
                            foundNearbyTileForVertex = true;
                            break; // go to next vertex
                        }
                    }
                    if (foundNearbyTileForVertex)
                    {
                        break;
                    }
                }
            }
            if (!foundNearbyTileForVertex)
            {
                return false;
            }

        }
        return true;
    }

    private bool IsInsideFloors(Part part)
    {

        var vertices = part.Collider.sharedMesh.vertices;
        foreach (var vertex in vertices)
        {
            bool foundNearbyTileForVertex = false;
            var vertexToWorldSpace = part.GOPart.transform.TransformPoint(vertex); // take the world position of the vertex

            foreach (Tile tile in _solver.GetTilesByYLayer(_currentFloorLayer).Where(t => t.CurrentGo != null))
            {
                Vector3 tilePosition = tile.CurrentGo.transform.position;
                Vector3 partPosition = part.GOPart.transform.position;
                if ((tilePosition - partPosition).magnitude < _tileToPartMaxDistance)
                {
                    List<GameObject> floorGOs = new();
                    for (int i = 0; i < tile.CurrentGo.transform.childCount; i++)
                    {
                        var child = tile.CurrentGo.transform.GetChild(i);
                        if (child.gameObject.layer == 8)
                        {
                            for (int j = 0; j < child.transform.childCount; j++)
                            {
                                floorGOs.Add(child.GetChild(j).gameObject);
                            }
                        }
                    }
                    if (floorGOs.Count == 0) return false;
                    foreach (var floorGO in floorGOs)
                    {
                        if ((vertexToWorldSpace - floorGO.GetComponent<MeshCollider>().ClosestPoint(vertexToWorldSpace)).magnitude < _vertexDistanceTolerance)
                        {
                            foundNearbyTileForVertex = true;
                            break; // go to next vertex
                        }
                    }
                    if (foundNearbyTileForVertex)
                    {
                        break;
                    }
                }
            }
            if (!foundNearbyTileForVertex) return false;
        }
        return true;
    }

    private List<GameObject> GetChildrenByLayer(GameObject parent, int layer)
    {
        List<GameObject> childrenGO = new List<GameObject>();
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i);
            if (child.gameObject.layer == layer)
            {
                for (int j = 0; j < child.transform.childCount; j++)
                {
                    childrenGO.Add(child.GetChild(j).gameObject);
                }
            }
        }
        return childrenGO;
    }

    /// <summary>
    /// Refactored version of the isInsideFloor and isInsideWall functions. Doesn't work at the moment.
    /// </summary>
    /// <param name="part"></param>
    /// <param name="floorWallLayer"></param>
    /// <returns></returns>
    private bool IsInsideTiles(Part part, int floorWallLayer)
    {
        var tiles = _solver.GetTilesByYLayer(_currentFloorLayer);

        // take the world positions of the vertices
        Vector3[] vertexPositions = part.Collider.sharedMesh.vertices.Select(v => part.GOPart.transform.TransformPoint(v)).ToArray();
        BitArray vertexInside = new BitArray(vertexPositions.Length, false);


        //run over all the tiles
        foreach (var tile in tiles)
        {
            //Check if the part is within a certain distance of the tile
            Vector3 tilePosition = tile.CurrentGo.transform.position;
            Vector3 partPosition = part.GOPart.transform.position;
            if (Vector3.Distance(tilePosition, partPosition) > _tileToPartMaxDistance)
                break;

            //check if the centrepoint of the part is in a tile
            bool centrePointInside = true;
            if (!centrePointInside)
                break;


            //check if all the vertices are inside a tile
            for (int i = 0; i < vertexPositions.Length; i++)
            {
                List<GameObject> floorGOs = GetChildrenByLayer(tile.CurrentGo, 8);
                foreach (var floorGO in floorGOs)
                {
                    if ((vertexPositions[i] - floorGO.GetComponent<MeshCollider>().ClosestPoint(vertexPositions[i])).magnitude < _vertexDistanceTolerance)
                    {
                        vertexInside[i] = true;
                        break; // go to next vertex
                    }
                }
            }
        }

        return !vertexInside.Cast<bool>().Contains(false);
    }

    /// <summary>
    /// ComputePenetration method: tells direction and distance in order to avoid collision between 2 objects
    /// </summary>
    /// <returns>True if collision is found, false if not</returns>
    private bool IsColliding(Part newPart, List<Part> parts)
    {
        if (parts.Count == 0) return false;

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
    private IEnumerator ExteriorWallsPlacement()
    {
        Debug.Log("Exterior walls placement started");
        _wallFailureCounter = 0;
        while (true)
        {
            LoadWallPartPrefabs();
            if (!PlaceNextWallPart(exteriorWalls: true)) _wallFailureCounter++;
            if (_wallFailureCounter > _failureTolerance)
            {
                PlaceExteriorWallPartRandomPosition();
                _wallFailureCounter = 0;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator AutoWallPlacement()
    {
        Debug.Log("Auto wall placement started");
        _wallFailureCounter = 0;
        while (true)
        {
            LoadWallPartPrefabs();
            if (!PlaceNextWallPart(exteriorWalls: false)) _wallFailureCounter++;
            if (_wallFailureCounter > _failureTolerance)
            {
                PlaceWallPartRandomPosition();
                _wallFailureCounter = 0;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator AutoFloorPlacement()
    {
        Debug.Log("Auto floor placement started");
        _floorFailureCounter = 0;
        while (true)
        {
            LoadFloorPartPrefabs();
            if (!PlaceNextFloorPart()) _floorFailureCounter++;
            if (_floorFailureCounter > _failureTolerance)
            {
                PlaceFloorPartRandomPosition();
                _floorFailureCounter = 0;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    public void OnAutoWallPlacementButtonClicked()
    {
        LoadWallPartPrefabs();
        PlaceWallPartRandomPosition();
        AutoWallPlacementCoroutine = AutoWallPlacement();
        StartCoroutine(AutoWallPlacementCoroutine);
    }

    public void OnAutoFloorPlacementButtonClicked()
    {
        LoadFloorPartPrefabs();
        PlaceFloorPartRandomPosition();
        AutoFloorPlacementCoroutine = AutoFloorPlacement();
        StartCoroutine(AutoFloorPlacementCoroutine);
    }

    public void StopAutoWallPlacement()
    {
        StopCoroutine(AutoWallPlacementCoroutine);
        Debug.Log("Auto wall placement stopped by user");
        AutoWallPlacementCoroutine = null;
    }

    public void OnExteriorWallsPlacementButtonClicked()
    {
        LoadWallPartPrefabs();
        PlaceExteriorWallPartRandomPosition();
        ExteriorWallsPlacementCoroutine = ExteriorWallsPlacement();
        StartCoroutine(ExteriorWallsPlacementCoroutine);
    }

    public void StopExteriorWallsPlacement()
    {
        StopCoroutine(ExteriorWallsPlacementCoroutine);
        Debug.Log("Exterior walls placement stopped by user");
        ExteriorWallsPlacementCoroutine = null;
    }

    public void StopAutoFloorPlacement()
    {
        StopCoroutine(AutoFloorPlacementCoroutine);
        Debug.Log("Auto floor placement stopped by user");
        AutoFloorPlacementCoroutine = null;
    }
    #endregion
}
