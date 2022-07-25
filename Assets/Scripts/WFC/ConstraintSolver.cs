using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ConstraintSolver : MonoBehaviour
{
    #region public fields
    public Vector3Int GridDimensions;
    public GameObject WFCAggregator;
    public Vector3 TileSize;
    public Tile[,,] TileGrid { private set; get; }
    public List<TilePattern> _patternLibrary;
    List<TileConnection> _connections;

    public GameObject GOEmptyTilePrefab;
    public GameObject[] GOPatternPrefabs;
    public GameObject[] GOFloorLayers;
    public List<GameObject> TileGOs;

    public Dictionary<int, List<GameObject>> ExteriorWallsByYLayer;
    #endregion

    #region private fields
    private IEnumerator _propagateStep;
    public readonly int EmptyTilePatternIndex = 999;
    public ScreenRecorder _screenRecorder;
    #endregion

    void Start()
    {
        GridDimensions = new Vector3Int(30, 15, 30); // Based on 4x3x4 TileSize, GridDimensions should be 19x4x10, plus any margin needed around the grid
        TileSize = new Vector3(4, 3.05f, 4);

        ExteriorWallsByYLayer = new Dictionary<int, List<GameObject>>();
        for (int y = 0; y < GridDimensions.y; y++)
        {
            ExteriorWallsByYLayer[y] = new List<GameObject>();
        }

        //Add all connections
        _connections = new List<TileConnection>();

        _connections.Add(new TileConnection(ConnectionType.WFC_connOrange, "WFC_connOrange"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connYellow, "WFC_connYellow"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connBlue, "WFC_connBlue"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connApple, "WFC_connApple"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connBlack, "WFC_connBlack"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connPink, "WFC_connPink"));

        _patternLibrary = new List<TilePattern>();

        //Add empty tile pattern
        _patternLibrary.Add(new TilePattern(EmptyTilePatternIndex, GOEmptyTilePrefab, _connections));

        //Add all other patterns
        for (int i = 0; i < GOPatternPrefabs.Length; i++)
        {
            var goPattern = GOPatternPrefabs[i];
            _patternLibrary.Add(new TilePattern(i, goPattern, _connections));
        }

        _propagateStep = PropagateStep();
        InitialiseWFCGrid();
        StartCoroutine(_propagateStep);
    }

    private void Update()
    {
        if (Keyboard.current[Key.Digit1].wasPressedThisFrame)
        {
            AggregateExteriorWalls();
        }
        if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
        {
            AggregateWallParts();
        }
        if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
        {
            AggregateNextFloor();
        }
    }

    #region private functions
    /// <summary>
    /// Get list of tileIndices within the geometry of the site
    /// </summary>
    private List<Vector3Int> GetValidIndices()
    {
        var validIndices = new List<Vector3Int>();
        foreach (var floorLayer in GOFloorLayers)
        {
            validIndices.AddRange(GetValidIndicesInYLayer(floorLayer.transform));
        }
        return validIndices;
    }

    private void DisableTilesNotInSite(List<Vector3Int> validIndices)
    {
        var visitedIndices = new List<Vector3Int>();
        foreach (var tile in TileGrid)
        {
            if (!validIndices.Contains(tile.Index))
            {
                // Set this tile to be an emptyTile
                tile.PossiblePatterns = new List<TilePattern>() { _patternLibrary.Find(p => p.Index == EmptyTilePatternIndex) };
                visitedIndices.Add(tile.Index);
                for (int i = 0; i < 6; i++) // Each tile can have 6 possible neighbours: +x, -x, +y, -y, +z, -z
                {
                    var neighbours = tile.GetNeighbours();
                    var neighbour = neighbours[i];
                    if (neighbour != null && !neighbour.Set && validIndices.Contains(neighbour.Index) && !visitedIndices.Contains(neighbour.Index) && !visitedIndices.Contains(tile.Index))
                    {
                        visitedIndices.Add(tile.Index);

                        int opposite;
                        if (i == 0) opposite = 1;
                        else if (i == 1) opposite = 0;
                        else if (i == 2) opposite = 3;
                        else if (i == 3) opposite = 2;
                        else if (i == 4) opposite = 5;
                        else opposite = 4;

                        //New list of PossiblePatterns should be current list, but with any TilePatterns that do NOT have an exterior wall connection on the opposite face taken out
                        var possiblePatterns = neighbour.PossiblePatterns.Where(p => p.Index != EmptyTilePatternIndex && p.HasFaceWithConnectionType(opposite, ConnectionType.WFC_connApple)).ToList();
                        neighbour.PossiblePatterns = possiblePatterns;
                        Debug.Log($"Tile EMPTY [{tile.Index.x}, {tile.Index.y}, {tile.Index.z}]: Has {neighbour.NumberOfPossiblePatterns} possible patterns");
                        tile.CurrentGo = GameObject.Instantiate(GOEmptyTilePrefab);
                        tile.CurrentGo.name = $"Tile EMPTY [{tile.Index.x}, {tile.Index.y}, {tile.Index.z}]";
                        tile.CurrentGo.transform.position = tile.RealWorldPosition;
                        TileGOs.Add(tile.CurrentGo);
                        tile.CurrentTile = _patternLibrary.Find(p => p.Index == EmptyTilePatternIndex);
                    }
                }

            }
        }
        Debug.Log($"Number of tiles visited: {visitedIndices.Count}");
        Debug.Log($"Number of tiles with 0 possible patterns: {GetTilesFlattened().Where(t => t.NumberOfPossiblePatterns == 0).ToList().Count}");
    }

    private List<Vector3Int> GetValidIndicesInYLayer(Transform layerMeshes)
    {
        var validIndicesCurrentLayer = new List<Vector3Int>();
        foreach (Transform child in layerMeshes)
        {
            validIndicesCurrentLayer.Add(Util.RealPositionToIndex(child.GetComponent<MeshCollider>().bounds.center, TileSize));
        }
        return validIndicesCurrentLayer;
    }

    //Create the tile grid
    private void MakeTiles()
    {
        TileGrid = new Tile[GridDimensions.x, GridDimensions.y, GridDimensions.z];
        for (int x = 0; x < GridDimensions.x; x++)
        {
            for (int y = 0; y < GridDimensions.y; y++)
            {
                for (int z = 0; z < GridDimensions.z; z++)
                {
                    TileGrid[x, y, z] = new Tile(_screenRecorder, new Vector3Int(x, y, z), _patternLibrary.Where(p => p.Index != EmptyTilePatternIndex).ToList(), this, TileSize);
                }
            }
        }
        Debug.Log($"Total number of tiles created: {TileGrid.Length}");
    }

    private IEnumerator PropagateStep()
    {
        while (GetNextTile())
        {
            yield return new WaitForSeconds(0.25f);
        }
    }

    private bool GetNextTile()
    {
        List<Tile> unsetTiles = GetUnsetTiles();

        //Check if there still are tiles to set
        if (unsetTiles.Count == 0)
        {
            Debug.Log("All tiles are set");
            return false;
        }

        var tileWithLowestPossiblePatternCount = unsetTiles.OrderBy(p => p.NumberOfPossiblePatterns).First();

        //Assign one of the possible patterns to the tile
        tileWithLowestPossiblePatternCount.AssignRandomPossiblePattern();

        return true;
    }

    private List<Tile> GetTilesFlattened()
    {
        List<Tile> tiles = new List<Tile>();
        for (int x = 0; x < GridDimensions.x; x++)
        {
            for (int y = 0; y < GridDimensions.y; y++)
            {
                for (int z = 0; z < GridDimensions.z; z++)
                {
                    tiles.Add(TileGrid[x, y, z]);
                }
            }
        }
        return tiles;
    }

    /// <summary>
    /// Get a list of "set" tiles for a particular floor, according to the current layer
    /// </summary>
    /// <param name="yLayer">The current layer</param>
    /// <returns>list of tiles for the current layer</returns>
    public List<Tile> GetTilesByYLayer(int yLayer)
    {
        List<Tile> tiles = new List<Tile>();
        for (int x = 0; x < GridDimensions.x; x++)
        {
            for (int z = 0; z < GridDimensions.z; z++)
            {
                if (TileGrid[x, yLayer, z].Set) tiles.Add(TileGrid[x, yLayer, z]);
            }
        }
        return tiles;
    }
    #endregion

    #region public functions
    private void InitialiseWFCGrid()
    {
        // destroy old tiles everytime we run WFC
        foreach (var GO in TileGOs)
        {
            GameObject.Destroy(GO);
        }

        //Set up the tile grid
        MakeTiles();

        // build tiles according to the site geometry
        var validIndices = GetValidIndices();
        Debug.Log($"Number of valid indices (i.e. tiles in site bounds): {validIndices.Count}");
        DisableTilesNotInSite(validIndices);

        //keep trying to place a first tile, until we get one which doesn't reduce any of it's neighbours' PossibilePatterns.Count to 0
        // (the AssignRandomPossiblePattern function handles this, returning true if it managed to place a tile in a valid way; false if not)
        while (true)
        {
            //var randomIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
            var randomIndex = validIndices[0]; // start on the first available tile
            if (TileGrid[randomIndex.x, randomIndex.y, randomIndex.z].AssignRandomPossiblePattern()) return;
        }
    }

    public List<Tile> GetUnsetTiles()
    {
        List<Tile> unsetTiles = new List<Tile>();

        //Loop over all the tiles and check which ones are not set
        foreach (var tile in GetTilesFlattened())
        {
            if (!tile.Set && tile.NumberOfPossiblePatterns > 1) unsetTiles.Add(tile);
        }
        return unsetTiles;
    }

    public int GetGroundFloorLayerNumber()
    {
        return GetValidIndices().Min(idx => idx.y);
    }
    #endregion

    #region Buttons
    //this function removes the colored sides used for connections
    public void ToggleConnectionVisibility()
    {
        foreach (var tile in TileGrid)
        {
            if (tile.Set)
            {
                tile.ToggleVisibility();
            }
        }
    }

    public void DeleteConnectionsForExporting()
    {
        foreach (var connectionType in (ConnectionType[])Enum.GetValues(typeof(ConnectionType)))
        {
            var connectionsWithTag = GameObject.FindGameObjectsWithTag(connectionType.ToString());
            foreach (var connection in connectionsWithTag)
            {
                Destroy(connection);
            }
        }
    }

    public void ToggleSiteVisibility()
    {
        foreach (var floorLayer in GOFloorLayers)
        {
            foreach (var renderer in floorLayer.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.enabled = !renderer.enabled;
            }
        }
    }

    public void ToggleTilesVisibility()
    {
        var tiles = GetTilesFlattened().Where(t => t.CurrentGo != null);
        foreach (var tile in tiles)
        {
            if (tile.Index.y == GetGroundFloorLayerNumber())
            {
                var children = Util.GetChildObjectsByLayer(tile.CurrentGo.transform, LayerMask.NameToLayer("Wall"));
                foreach (var child in children)
                {
                    var renderers = child.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = !renderer.enabled;
                    }
                }
            }
            else
            {
                var walls = Util.GetChildObjectsByLayer(tile.CurrentGo.transform, LayerMask.NameToLayer("Wall"));
                foreach (var child in walls)
                {
                    var renderers = child.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = !renderer.enabled;
                    }
                }
                var floors = Util.GetChildObjectsByLayer(tile.CurrentGo.transform, LayerMask.NameToLayer("Floor"));
                foreach (var child in floors)
                {
                    var renderers = child.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = !renderer.enabled;
                    }
                }
            }
        }
    }

    public void SetOnlyFloorToBeVisibleOnYLayer(int currentYLayer)
    {
        var tiles = GetTilesFlattened().Where(t => t.CurrentGo != null);
        foreach (var tile in tiles)
        {
            if (tile.Index.y == currentYLayer)
            {
                var walls = Util.GetChildObjectsByLayer(tile.CurrentGo.transform, LayerMask.NameToLayer("Wall"));
                foreach (var child in walls)
                {
                    var renderers = child.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = false;
                    }
                }
                var floors = Util.GetChildObjectsByLayer(tile.CurrentGo.transform, LayerMask.NameToLayer("Floor"));
                foreach (var child in floors)
                {
                    var renderers = child.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = true;
                    }
                }
            }
        }
    }

    public void AggregateExteriorWalls()
    {
        var aggregator = WFCAggregator.GetComponent<WFC_Aggregator>();
        aggregator.Initialise(TileSize, this);
        if (aggregator.ExteriorWallsPlacementCoroutine == null)
        {
            aggregator.OnExteriorWallsPlacementButtonClicked();
        }
        else
        {
            aggregator.StopExteriorWallsPlacement();
        }
    }

    public void AggregateWallParts()
    {
        var aggregator = WFCAggregator.GetComponent<WFC_Aggregator>();
        aggregator.Initialise(TileSize, this);
        aggregator.PlaceWallPartRandomPosition();
        if (aggregator.AutoWallPlacementCoroutine == null)
        {
            aggregator.OnAutoWallPlacementButtonClicked();
        }
        else
        {
            aggregator.StopAutoWallPlacement();
        }
    }

    public void StartStopWFCCoroutine()
    {
        if (_propagateStep != null)
        {
            StopCoroutine(_propagateStep);
            _propagateStep = null;
        }
        else
        {
            InitialiseWFCGrid();
            _propagateStep = PropagateStep();
            StartCoroutine(_propagateStep);
        }
    }

    public void AggregateNextFloor()
    {
        var aggregator = WFCAggregator.GetComponent<WFC_Aggregator>();
        aggregator.OnAggregateNextFloorButtonClicked();
    }
    #endregion
}
