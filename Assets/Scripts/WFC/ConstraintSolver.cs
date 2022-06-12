using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ConstraintSolver : MonoBehaviour
{

    #region Serialized fields
    public Vector3Int GridDimensions;
    public GameObject WFCAggregator;
    [SerializeField]
    public Vector3 TileSize = new Vector3(4, 3, 4);
    #endregion

    #region public fields
    public Tile[,,] TileGrid { private set; get; }
    List<TilePattern> _patternLibrary;
    List<TileConnection> _connections;

    public GameObject[] GOPatternPrefabs;
    public GameObject GroundFloor;
    public GameObject FirstFloor;
    public GameObject SecondFloor;
    public GameObject ThirdFloor;

    public Vector3Int Index { get; private set; }

    public List<GameObject> TileGOs;
    #endregion

    #region private fields
    private IEnumerator _propagateStep;
    private bool _isCollapsing = false;
    #endregion

    void Start()
    {
        GridDimensions = new Vector3Int(19, 4, 10);

        //Add all connections
        _connections = new List<TileConnection>();

        _connections.Add(new TileConnection(ConnectionType.WFC_conn0, "WFC_conn0"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connYellow, "WFC_connYellow"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connBlue, "WFC_connBlue"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connGreen, "WFC_connGreen"));
        _connections.Add(new TileConnection(ConnectionType.WFC_connTopBottom, "WFC_connTopBottom"));

        //Add all patterns
        _patternLibrary = new List<TilePattern>();
        for (int i = 0; i < GOPatternPrefabs.Length; i++)
        {
            var goPattern = GOPatternPrefabs[i];
            _patternLibrary.Add(new TilePattern(i, goPattern, _connections));
        }

        RunWFC();

        //look into making this into a bounding box
        _propagateStep = PropagateStep();
    }

    #region private functions
    /// <summary>
    /// Get list of tileIndices within the geometry of the site
    /// </summary>
    private List<Vector3Int> GetValidIndices()
    {
        var layerMeshesGF = GroundFloor.transform;
        var layerMeshes1F = FirstFloor.transform;
        var layerMeshes2F = SecondFloor.transform;
        var layerMeshes3F = ThirdFloor.transform;

        var validIndices = new List<Vector3Int>();
        validIndices.AddRange(GetValidIndicesInYLayer(layerMeshesGF));
        validIndices.AddRange(GetValidIndicesInYLayer(layerMeshes1F));
        validIndices.AddRange(GetValidIndicesInYLayer(layerMeshes2F));
        validIndices.AddRange(GetValidIndicesInYLayer(layerMeshes3F));

        return validIndices;
    }

    private void DisableTilesNotInSite(List<Vector3Int> validIndices)
    {
        foreach (var tile in TileGrid)
        {
            if (!validIndices.Contains(tile.Index))
            {
                tile.PossiblePatterns = new List<TilePattern>();
            }
        }
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
                    TileGrid[x, y, z] = new Tile(new Vector3Int(x, y, z), _patternLibrary, this, TileSize);
                }
            }
        }
    }

    private IEnumerator PropagateStep()
    {
        while (true)
        {
            _isCollapsing = true;
            GetNextTile();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void GetNextTile()
    {
        List<Tile> UnsetTiles = GetUnsetTiles();

        //Check if there still are tiles to set
        if (UnsetTiles.Count == 0)
        {
            Debug.Log("All tiles are set");
            return;
        }

        //this is currently not going to give you the lowest tile
        List<Tile> lowestTiles = new List<Tile>();
        int lowestTile = int.MaxValue;

        //PropagateGrid on the set tile                     
        foreach (Tile tile in UnsetTiles)
        {
            if (tile.NumberOfPossiblePatterns < lowestTile)
            {
                lowestTiles = new List<Tile>() { tile };

                lowestTile = tile.NumberOfPossiblePatterns;

            }
            else if (tile.NumberOfPossiblePatterns == lowestTile)
            {
                lowestTiles.Add(tile);
            }
            else if (tile.NumberOfPossiblePatterns != lowestTile)
            {
                tile.AssignRandomPossiblePattern();
            }

            Debug.Log("Propagating Grid");
        }

        //Select a random tile out of the list
        int rndIndex = UnityEngine.Random.Range(0, lowestTiles.Count);
        Tile tileToSet = lowestTiles[rndIndex];

        Debug.Log("Random Index " + lowestTiles.Count);

        //Assign one of the possible patterns to the tile
        tileToSet.AssignRandomPossiblePattern();
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

    public List<Tile> GetSetTilesByYLayer(int yLayer)
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
    public void RunWFC()
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
        DisableTilesNotInSite(validIndices);

        // add a random tile to a random position
        var randomIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
        TileGrid[randomIndex.x, randomIndex.y, randomIndex.z].AssignPattern(_patternLibrary[1]);

        GetNextTile();
    }

    //Cardinal Directions Establishment 
    public List<Vector3Int> GetTileDirectionList()
    {
        List<Vector3Int> tileDirections = new List<Vector3Int>();
        foreach (Vector3Int tileDirection in Util.Directions)
        {
            if (Util.CheckInBounds(GridDimensions, Index))
            {
                tileDirections.Add(tileDirection);
            }
        }
        return tileDirections;
    }

    //tile to unsetTile, added possibleNeighbours, List<Tile> newPossiblePatterns
    public List<Tile> GetNeighbour(List<TilePattern> newPossiblePatterns)
    {
        List<Tile> possibleNeighbours = new List<Tile>();
        IEnumerable<object> tileDirections = null;
        foreach (var unsetTiles in tileDirections)
        {
            if (unsetTiles == newPossiblePatterns)
            {
                possibleNeighbours.Add((Tile)unsetTiles);
            }
        }
        return possibleNeighbours;
    }

    public List<Tile> GetUnsetTiles()
    {
        List<Tile> unsetTiles = new List<Tile>();

        //Loop over all the tiles and check which ones are not set
        foreach (var tile in GetTilesFlattened())
        {
            if (!tile.Set) unsetTiles.Add(tile);

            Debug.Log(tile.PossiblePatterns.Count);
        }
        Debug.Log(unsetTiles.Count);
        return unsetTiles;
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
        foreach (var renderer in GroundFloor.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = !renderer.enabled;
        }

        foreach (var renderer in FirstFloor.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = !renderer.enabled;
        }

        foreach (var renderer in SecondFloor.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = !renderer.enabled;
        }

        foreach (var renderer in ThirdFloor.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = !renderer.enabled;
        }
    }

    public void ToggleTilesVisibility()
    {
        var tileRenderers = GetTilesFlattened().Where(t => t.CurrentGo != null).SelectMany(t => t.CurrentGo.GetComponentsInChildren<MeshRenderer>());
        foreach (var renderer in tileRenderers)
        {
            renderer.enabled = !renderer.enabled;
        }
    }

    public void AggregateWallParts()
    {
        var aggregator = WFCAggregator.GetComponent<WFC_Aggregator>();
        aggregator.Initialise(TileSize, this);
        aggregator.PlaceFirstWallPart();
        aggregator.OnAutoWallPlacementButtonClicked();
    }

    public void AggregateFloorParts()
    {
        var aggregator = WFCAggregator.GetComponent<WFC_Aggregator>();
        aggregator.Initialise(TileSize, this);
        aggregator.PlaceFirstFloorPart();
        aggregator.OnAutoFloorPlacementButtonClicked();
    }
    #endregion
}
