using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CollisionTest : MonoBehaviour
{
    #region VARIABLES
    public GameObject WallsPrefab;

    public GameObject FloorPrefab;

    //to decide whether to aggregate parts according to length or not
    [SerializeField]
    private Toggle _toggleConnectionMatching;

    private IEnumerator _autoPlacementCoroutine;

    //private readonly float _radius = 25f; // check for penetration within a radius of 50m (radius of a sphere)
    private readonly int _maxNeighboursToCheck = 50; // how many neighbouring colliders to check in IsColliding function
    private readonly float _overlapTolerance = 0.03f; // used by compute penetration
    private readonly float _connectionTolerance = 0.2f; // tolerance of matching connections
    private readonly float _vertexDistanceTolerance = 0.1f; // tolerance used when checking for vertices inside BBs
    private bool _connectionMatchingEnabled = true;

    private Collider[] _neighbours; // "ingredient" of compute penetration
    private Vector3 _collisionTestSpherePosition = Vector3Int.zero; // "ingredient" of compute penetration

    private List<Part> _parts = new(); // LIBRARY OF PARTS
    private List<Part> _floorParts = new(); // LIBRARY OF FLOOR PARTS
    private List<Part> _placedParts = new();
    private List<Part> _placedFloorParts = new();

    private GameObject[] _boundingBoxes = new GameObject[10]; // to give a max of bb on yLayer (walls bb)
    private GameObject[] _floorBBs = new GameObject[10]; // to give a max of bb on yLayer (floors bb)
    private int currentYLayer = 0;
    private float _floorPlusWallsHeight = 3.05f;
    #endregion

    public void Start()
    {
        // instantiate wall and floor bb on current layer
        var newBB = Instantiate(WallsPrefab, new Vector3(0, 0.35f, 0), Quaternion.Euler(new Vector3(-90, 0, 0)));
        newBB.name = "BoundingBox" + currentYLayer;
        _boundingBoxes[currentYLayer] = newBB;

        var newFloor = Instantiate(FloorPrefab, new Vector3(0, 0.35f, 0), Quaternion.Euler(new Vector3(-90, 0, 0)));
        newFloor.name = "FloorBB" + currentYLayer;
        _floorBBs[currentYLayer] = newFloor;

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

    private void Update()
    {
        //raycast to position a part on clicked position
        if (Input.GetMouseButtonDown(0)) RaycastToMousePosition();
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
    private void PlaceFirstPart()
    {
        _parts.Shuffle();
        for (int i = 0; i < _parts.Count; i++)
        {
            Part part = _parts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;
            part.PlaceFirstPart(new Vector3(extents.x - size.x, extents.y + (currentYLayer * _floorPlusWallsHeight), extents.z - size.z) + new Vector3(-0.02f, 0.35f, -0.02f), Quaternion.Euler(new Vector3(0, 0, 0)));
            bool isInside = CheckPartInBounds(_boundingBoxes[currentYLayer], part);
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

    private void PlaceFirstFloorPart()
    {
        _floorParts.Shuffle();
        for (int i = 0; i < _floorParts.Count; i++)
        {
            Part part = _floorParts[i];
            var size = part.Collider.sharedMesh.bounds.size;
            var extents = size / 2;

            part.PlaceFirstPart(new Vector3(extents.x - size.x, extents.z + (currentYLayer * _floorPlusWallsHeight), -extents.y), Quaternion.Euler(new Vector3(90, 0, 0)));
            bool isInside = CheckPartInBounds(_floorBBs[currentYLayer], part);
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

            if (IsColliding(unplacedConnection.ThisPart, _placedParts) || !CheckPartInBounds(_boundingBoxes[currentYLayer], unplacedConnection.ThisPart))
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

            if (IsColliding(unplacedConnection.ThisPart, _placedFloorParts) || !CheckPartInBounds(_floorBBs[currentYLayer], unplacedConnection.ThisPart))
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
            if (isOverlapping && distance > _overlapTolerance) return true;
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
    /// Checks if a part has been placed in a valid position (i.e. inside a given bounding box) using VERTICES
    /// </summary>
    /// <param name="boxes">List of transforms of bounding boxes (i.e. floor BB or wall BB)</param>
    /// <param name="partToCheck">Part to check (i.e. the new part we are trying to add into the building)</param>
    /// <returns></returns>
    public bool IsValidPlacement(List<Transform> boxes, Part partToCheck)
    {
        ////Check if the centre of the mesh is inside the boxes
        //Vector3 centre = partToCheck.Collider.gameObject.GetComponent<Renderer>().bounds.center;
        //bool isInBounds = false;
        //for (int i = 0; i < boxes.Count; i++)
        //{
        //    if (boxes[i].GetComponent<Renderer>().bounds.Contains(centre))
        //    {
        //        isInBounds = true;
        //    }
        //}
        //if (!isInBounds) return false;

        var partMesh = partToCheck.Collider.sharedMesh;
        var partVertices = partMesh.vertices;
        var totalVerticesInPart = partMesh.vertices.Count();
        var vertexCounts = new int[boxes.Count()];
        for (int i = 0; i < boxes.Count; i++)
        {
            var bbCollider = boxes[i].GetComponent<Collider>();
            //if (bbCollider is MeshCollider) bbCollider = bb.GetComponent<MeshCollider>();
            //else if (bbCollider is BoxCollider) bbCollider = bb.GetComponent<BoxCollider>();
            int verticesInBounds = 0;
            foreach (var vertex in partVertices)
            {
                var vertexToWorldSpace = partToCheck.GOPart.transform.TransformPoint(vertex);
                //var vertGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //vertGo.transform.localPosition = vertexToWorldSpace;
                //vertGo.transform.localScale = Vector3.one * 0.05f;

                if ((vertexToWorldSpace - bbCollider.ClosestPoint(vertexToWorldSpace)).magnitude < _vertexDistanceTolerance) verticesInBounds++;
                //if ((transVertex - bbCollider.ClosestPointOnBounds(transVertex)).magnitude > _vertexDistanceTolerance) return false;
                //if (!bbCollider.bounds.Contains(transVertex)) return false;
                //if (!Util.PointInsideCollider(transVertex, bbCollider)) return false;
            }
            vertexCounts[i] = verticesInBounds;
            if (verticesInBounds == totalVerticesInPart) return true;
            //if (verticesInBounds > 4 && verticesInBounds < totalVerticesInPart) return false;
        }
        // if the total number of vertices for the part can be summed from the number of vertices in any pair of bounding boxes, then that is also okay
        return false;
    }
    #endregion

    #region CLICK > RAYCAST > TRY TO PLACE PART WHERE CLICKED
    // if what we hit is a collider > check if the collider has boundingbox tag >
    // yes > create new vector3 position where to place the part according to the thinnest axis of the wall
    // no > just debug, don't do anything
    private void RaycastToMousePosition()
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
    }

    // part placement after raycast
    private void TryPlacePartAtPosition(Vector3 pos)
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
                bool isInside = CheckPartInBounds(_boundingBoxes[currentYLayer], part);
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

    public void OnNewLayerOnTopButtonClicked()
    {
        var newBB = Instantiate(WallsPrefab, _boundingBoxes[currentYLayer].transform.position + new Vector3(0, _floorPlusWallsHeight, 0), _boundingBoxes[currentYLayer].transform.rotation);
        newBB.name = "BoundingBox" + currentYLayer;

        var newFloor = Instantiate(FloorPrefab, _floorBBs[currentYLayer].transform.position + new Vector3(0, _floorPlusWallsHeight, 0), _floorBBs[currentYLayer].transform.rotation);
        newFloor.name = "FloorBB" + currentYLayer;

        currentYLayer++;
        _boundingBoxes[currentYLayer] = newBB;
        _floorBBs[currentYLayer] = newFloor;
        PlaceFirstPart();
        PlaceFirstFloorPart();
    }
    #endregion
}
