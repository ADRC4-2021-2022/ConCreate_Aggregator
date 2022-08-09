using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WFC_Aggregator : MonoBehaviour
{
    #region VARIABLES
    public IEnumerator ExteriorWallsPlacementCoroutine;
    public IEnumerator AutoWallPlacementCoroutine;

    public Vector3 _tileSize;

    private ConstraintSolver _solver; //WFC algorithm
    private List<GameObject> _setTilesInCurrentLayer;

    private readonly float _overlapTolerance = 0.05f; // used by compute penetration
    private readonly float _vertexDistanceTolerance = 0.1f;
    private readonly float _tileToPartMaxDistance = 4f;

    private bool _hasBeenInitialised = false;

    private List<Part> _exteriorWallParts = new(); // LIBRARY OF EXTERIOR WALL PARTS
    private List<Part> _wallParts = new(); // LIBRARY OF WALL PARTS
    private List<Part> _placedExteriorWallParts = new();
    private List<Part> _placedWallParts = new();

    private int _currentWallLayer = 0;
    private int _wallFailureCounter = 0;
    private int _exteriorWallFailureCounter = 0;
    private readonly int _failureTolerance = 10;

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
            }
            return false;
        }).Select(g => new Part(g)).ToList();
        //Add one more arch so that there are more arches into the aggregation
        GameObject prefab08P_c = Resources.Load<GameObject>("Prefabs/PartsForWFC/SMALLARCH");
        _wallParts.Add(new Part(prefab08P_c));
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
            _setTilesInCurrentLayer = _solver.GetTilesByYLayer(_currentWallLayer).Select(t => t.CurrentGo).Where(GO => GO != null).ToList();
        }
    }

    public void PlaceExteriorWallPartRandomPosition()
    {
        var setTilesInCurrentLayer = _solver.ExteriorWallsByYLayer[_currentWallLayer];
        var randomWallGO = setTilesInCurrentLayer[Random.Range(0, setTilesInCurrentLayer.Count)];
        for (int i = 0; i < _exteriorWallParts.Count; i++)
        {
            Part part = _exteriorWallParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;

            var minPoint = randomWallGO.GetComponent<MeshCollider>().ClosestPoint(Vector3.zero);

            for (int k = 0; k < 4; k++)
            {
                part.PlaceFirstPart(minPoint + extents, Quaternion.Euler(new Vector3(0, 90 * k, 0)));
                bool isInsideWithPositiveExtents = !IsColliding(part, _placedExteriorWallParts) && !IsColliding(part, _placedWallParts) && IsInsideExteriorWalls(part);
                if (isInsideWithPositiveExtents)
                {
                    _exteriorWallParts.Remove(part);
                    _placedExteriorWallParts.Add(part);
                    part.Name = $"{part.Name} added {_placedExteriorWallParts.Count} (wall)";
                    return;
                }
                else
                {
                    part.ResetPart();
                    Debug.Log($"First part {part.Name} was outside");
                }
                part.PlaceFirstPart(minPoint - extents, Quaternion.Euler(new Vector3(0, 90 * k, 0)));
                bool isInsideWithNegativeExtents = !IsColliding(part, _placedExteriorWallParts) && !IsColliding(part, _placedWallParts) && IsInsideWalls(part);
                if (isInsideWithNegativeExtents)
                {
                    _exteriorWallParts.Remove(part);
                    _placedExteriorWallParts.Add(part);
                    part.Name = $"{part.Name} added {_placedExteriorWallParts.Count} (wall)";
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
        var randomWallGO = _setTilesInCurrentLayer[Random.Range(0, _setTilesInCurrentLayer.Count)];
        for (int i = 0; i < _wallParts.Count; i++)
        {
            Part part = _wallParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;

            GameObject wallGO = null;
            for (int j = 0; j < randomWallGO.transform.childCount; j++)
            {
                var child = randomWallGO.transform.GetChild(j);
                if (child.name.Equals("Wall")) wallGO = child.GetChild(0).gameObject;
            }
            if (wallGO == null) Debug.Log($"Failed to find wall GO for tile {randomWallGO}");
            var minPoint = wallGO.GetComponent<MeshCollider>().ClosestPoint(Vector3.zero);

            for (int k = 0; k < 4; k++)
            {
                part.PlaceFirstPart(minPoint + extents, Quaternion.Euler(new Vector3(0, 90 * k, 0)));
                bool isInsideWithPositiveExtents = !IsColliding(part, _placedWallParts) && !IsColliding(part, _placedExteriorWallParts) && IsInsideWalls(part);
                if (isInsideWithPositiveExtents)
                {
                    _wallParts.Remove(part);
                    _placedWallParts.Add(part);
                    part.Name = $"{part.Name} placed {_placedWallParts.Count} (wall)";
                    return;
                }
                else
                {
                    part.ResetPart();
                    Debug.Log($"First part {part.Name} was outside");
                }
                part.PlaceFirstPart(minPoint - extents, Quaternion.Euler(new Vector3(0, 90 * k, 0)));
                bool isInsideWithNegativeExtents = !IsColliding(part, _placedWallParts) && !IsColliding(part, _placedExteriorWallParts) && IsInsideWalls(part);
                if (isInsideWithNegativeExtents)
                {
                    _wallParts.Remove(part);
                    _placedWallParts.Add(part);
                    part.Name = $"{part.Name} placed {_placedWallParts.Count} (wall)";
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
    private bool PlaceNextExteriorWallPart()
    {
        // find all the available connections in the entire building made up of placed parts
        List<Connection> availableConnectionsInCurrentBuilding = new();
        foreach (Part placedPart in _placedExteriorWallParts)
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
        foreach (Part unplacedPart in _exteriorWallParts)
        {
            foreach (Connection connection in unplacedPart.Connections)
            {
                if (connection.Available)
                {
                    availableConnectionsInUnplacedParts.Add(connection);
                }
            }
        }

        foreach (Connection unplacedConnection in availableConnectionsInUnplacedParts)
        {
            unplacedConnection.ThisPart.PositionPart(randomAvailableConnectionInCurrentBuilding, unplacedConnection);

            if (IsColliding(unplacedConnection.ThisPart, _placedExteriorWallParts) || IsColliding(unplacedConnection.ThisPart, _placedWallParts) || !IsInsideExteriorWalls(unplacedConnection.ThisPart))
            {
                //the part collided, so go to the next part in the list
                unplacedConnection.ThisPart.ResetPart();
            }
            else
            {
                //Set the part as placed
                unplacedConnection.ThisPart.PlacePart(unplacedConnection);
                Debug.Log($"{_exteriorWallFailureCounter} wall failures");
                _exteriorWallFailureCounter = 0;

                randomAvailableConnectionInCurrentBuilding.Available = false;
                string newName = $"{unplacedConnection.ThisPart.Name} placed {_placedExteriorWallParts.Count + 1} (wall)";
                unplacedConnection.ThisPart.Name = newName;
                _exteriorWallParts.Remove(unplacedConnection.ThisPart);
                _placedExteriorWallParts.Add(unplacedConnection.ThisPart);
                //_screenRecorder.SaveScreen();
                return true;
            }
        }
        return false;
    }

    private bool PlaceNextWallPart()
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
                if (connection.Available)
                {
                    availableConnectionsInUnplacedParts.Add(connection);
                }
            }
        }

        availableConnectionsInUnplacedParts.Shuffle();
        foreach (Connection unplacedConnection in availableConnectionsInUnplacedParts)
        {
            unplacedConnection.ThisPart.PositionPart(randomAvailableConnectionInCurrentBuilding, unplacedConnection);

            if (IsColliding(unplacedConnection.ThisPart, _placedWallParts) || IsColliding(unplacedConnection.ThisPart, _placedExteriorWallParts) || !IsInsideWalls(unplacedConnection.ThisPart))
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
                //_screenRecorder.SaveScreen();
                return true;
            }
        }
        return false;
    }
    #endregion

    #region NECESSARY STUFF (check connections compatibility/collision/bounds)
    private bool IsInsideExteriorWalls(Part part)
    {
        var vertices = part.Collider.sharedMesh.vertices;
        foreach (var vertex in vertices)
        {
            bool foundNearbyTileForVertex = false;
            foreach (var wallGO in _solver.ExteriorWallsByYLayer[_currentWallLayer]/*.Where(GO => GO != null)*/)
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
            foreach (var tile in _setTilesInCurrentLayer)
            {
                //check if the part is within an acceptable distance to be able to collide
                Vector3 tilePosition = tile.transform.position;
                Vector3 partPosition = part.GOPart.transform.position;
                if ((tilePosition - partPosition).magnitude < _tileToPartMaxDistance)
                {
                    var vertexToWorldSpace = part.GOPart.transform.TransformPoint(vertex); // take the world position of the vertex
                    List<GameObject> wallGOs = new();
                    for (int i = 0; i < tile.transform.childCount; i++)
                    {
                        var child = tile.transform.GetChild(i);
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
                        break; // go to next vertex
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

    public List<Part> GetLSystemPattern(int sequenceLength)
    {
        var finalPattern = new List<string>();

        var axiom = "WALL1";
        var columnRule = new List<string>() {
            "WALL1"
        };
        var smallWallRule = new List<string>() {
            "SMALLARCH",
            "BIGARCH",
            "WALL2",
            "COLUMN"
        };
        var smallArchRule = new List<string>() {
            "BIGARCH"
        };
        var bigArchRule = new List<string>() {
            "WALL2"
        };
        var bigWallRule = new List<string>() {
            "COLUMN"
        };

        List<string> lastSequence = null;
        for (int i = 0; i < sequenceLength; i++)
        {
            if (lastSequence == null)
            {
                lastSequence = new List<string>() { axiom };
                finalPattern.Add(axiom);
            }
            else
            {
                var currentSequence = new List<string>();
                foreach (var partName in lastSequence)
                {
                    switch (partName)
                    {
                        case "COLUMN":
                            currentSequence.AddRange(columnRule);
                            finalPattern.AddRange(columnRule);
                            break;
                        case "WALL1":
                            currentSequence.AddRange(smallWallRule);
                            finalPattern.AddRange(smallWallRule);
                            break;
                        case "SMALLARCH":
                            currentSequence.AddRange(smallArchRule);
                            finalPattern.AddRange(smallArchRule);
                            break;
                        case "BIGARCH":
                            currentSequence.AddRange(bigArchRule);
                            finalPattern.AddRange(bigArchRule);
                            break;
                        case "WALL2":
                            currentSequence.AddRange(bigWallRule);
                            finalPattern.AddRange(bigWallRule);
                            break;
                    }
                }
                lastSequence = currentSequence;
            }
        }
        var finalPartsList = new List<Part>();
        foreach (var partName in finalPattern)
        {
            GameObject partGO = Resources.Load<GameObject>($"Prefabs/PartsForWFC/{partName}");
            finalPartsList.Add(new Part(partGO));
        }
        return finalPartsList;
    }
    #endregion

    #region BUTTONS FOR COROUTINES
    private IEnumerator ExteriorWallsPlacement()
    {
        Debug.Log("Exterior walls placement started");
        _exteriorWallFailureCounter = 0;
        while (true)
        {
            _exteriorWallParts = GetLSystemPattern(5);
            if (!PlaceNextExteriorWallPart()) _exteriorWallFailureCounter++;
            if (_exteriorWallFailureCounter > 0)
            {
                _exteriorWallParts = GetLSystemPattern(5);
                PlaceExteriorWallPartRandomPosition();
                _exteriorWallFailureCounter = 0;
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
            if (!PlaceNextWallPart()) _wallFailureCounter++;
            if (_wallFailureCounter > _failureTolerance)
            {
                PlaceWallPartRandomPosition();
                _wallFailureCounter = 0;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    public void OnAggregateNextFloorButtonClicked()
    {
        if (ExteriorWallsPlacementCoroutine != null)
        {
            StopCoroutine(ExteriorWallsPlacementCoroutine);
            ExteriorWallsPlacementCoroutine = null;
        }
        if (AutoWallPlacementCoroutine != null)
        {
            StopCoroutine(AutoWallPlacementCoroutine);
            AutoWallPlacementCoroutine = null;
        }
        _currentWallLayer++;
        _solver.SetOnlyFloorToBeVisibleOnYLayer(_currentWallLayer);
        _setTilesInCurrentLayer = _solver.GetTilesByYLayer(_currentWallLayer).Select(t => t.CurrentGo).Where(GO => GO != null).ToList();
    }

    public void OnAutoWallPlacementButtonClicked()
    {
        LoadWallPartPrefabs();
        PlaceWallPartRandomPosition();
        AutoWallPlacementCoroutine = AutoWallPlacement();
        StartCoroutine(AutoWallPlacementCoroutine);
    }

    public void StopAutoWallPlacement()
    {
        StopCoroutine(AutoWallPlacementCoroutine);
        Debug.Log("Auto wall placement stopped by user");
        AutoWallPlacementCoroutine = null;
    }

    public void OnExteriorWallsPlacementButtonClicked()
    {
        _exteriorWallParts = GetLSystemPattern(5);
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

    public void PlaceBalconies()
    {
        foreach (var arch in _placedExteriorWallParts.Where(p => p.PrefabName == "BIGARCH"))
        {
            var balconyPrefab = Resources.Load<GameObject>("Prefabs/PartsForWFC/BALCONY");
            var archPos = arch.GOPart.transform.position;
            var archSize = arch.Collider.sharedMesh.bounds.size;
            GameObject balcony = GameObject.Instantiate(balconyPrefab, archPos, arch.GOPart.transform.rotation);
            var balconySize = balcony.transform.GetChild(0).GetChild(0).GetComponent<Collider>().bounds.size;

            balcony.transform.position = new Vector3(archPos.x, archPos.y - archSize.y / 2 - balconySize.y / 2, archPos.z);
            //balcony.transform.position = new Vector3(archPos.x, archPos.y - archSize.y / 2 - balconySize.y / 2, archPos.z - balconySize.z / 2);
            //if (IsInsideFloors(balcony))
            //{
            //    balcony.transform.position = new Vector3(archPos.x - balconySize.x / 2, archPos.y - archSize.y / 2 - balconySize.y / 2, archPos.z);
            //    if (IsInsideFloors(balcony))
            //    {
            //        balcony.transform.position = new Vector3(archPos.x + balconySize.x / 2, archPos.y - archSize.y / 2 - balconySize.y / 2, archPos.z);
            //        if (IsInsideFloors(balcony))
            //        {
            //            balcony.transform.position = new Vector3(archPos.x, archPos.y - archSize.y / 2 - balconySize.y / 2, archPos.z + balconySize.z / 2);
            //            if (IsInsideFloors(balcony))
            //            {
            //                Debug.Log($"Failed to place balcony for {arch.Name}");
            //                Destroy(balcony);
            //            }
            //        }
            //    }
            //}
        }
    }

    private bool IsInsideFloors(GameObject balcony)
    {
        var vertices = balcony.transform.GetChild(0).GetChild(0).GetComponent<MeshCollider>().sharedMesh.vertices;
        foreach (var vertex in vertices)
        {
            bool foundNearbyTileForVertex = false;
            foreach (var tile in _setTilesInCurrentLayer)
            {
                //check if the part is within an acceptable distance to be able to collide
                Vector3 tilePosition = tile.transform.position;
                Vector3 balconyPos = balcony.transform.position;
                if ((tilePosition - balconyPos).magnitude < _tileToPartMaxDistance)
                {
                    var vertexToWorldSpace = balcony.transform.TransformPoint(vertex); // take the world position of the vertex
                    List<GameObject> floorGOs = new();
                    for (int i = 0; i < tile.transform.childCount; i++)
                    {
                        var child = tile.transform.GetChild(i);
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
                        break; // go to next vertex
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
    #endregion
}
