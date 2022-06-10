using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using Unity.VisualScripting;


public class ConstraintSolver : MonoBehaviour
{

    #region Serialized fields
    [SerializeField]
    public Vector3Int GridDimensions;
    [SerializeField]
    public Vector3 TileSize = new Vector3(4, 3, 4);


    #endregion

    #region public fields

    public GameObject[] GOPatternPrefabs;

    #endregion

    #region private fields
    public Tile[,,] TileGrid { private set; get; }
    List<TilePattern> _patternLibrary;
    List<TileConnection> _connections;

    public Vector3Int Index { get; private set; }


    private TilePattern _mat_ConPink;    //00
    private TilePattern _mat_ConYellow;  //01
    private TilePattern _mat_ConBlue;    //02
    private TilePattern _mat_Orange;     //03
    private TilePattern _mat_Cyan;       //04
    private TilePattern _mat_Green;      //05

    private IEnumerator _propogateStep;
    private bool _isCollapsing = false;


    #endregion

    #region Unity Standard Methods

    void Start()
    {
        //Add all connections
        _connections = new List<TileConnection>();

        _connections.Add(new TileConnection(ConnectionType.con0, "WFC_conn0"));
        _connections.Add(new TileConnection(ConnectionType.conYellow, "WFC_connYellow"));
        _connections.Add(new TileConnection(ConnectionType.conBlue, "WFC_connBlue"));
        _connections.Add(new TileConnection(ConnectionType.conGreen, "WFC_connGreen"));
        _connections.Add(new TileConnection(ConnectionType.conTopBottom, "WFC_connTopBottom"));

        //Add all patterns
        _patternLibrary = new List<TilePattern>();
        for (int i = 0; i < GOPatternPrefabs.Length; i++)
        {
            var goPattern = GOPatternPrefabs[i];
            _patternLibrary.Add(new TilePattern(i, goPattern, _connections));
        }

        //Set up the tile grid
        MakeTiles();
        // add a random tile to a random position
        TileGrid[2, 0, 2].AssignPattern(_patternLibrary[1]);

        GetNextTile();

        //look into making this into a bounding box
        _propogateStep = PropogateStep();
    }


    //public void GetPlan()
    //{

    //    PlanCreation.CreatePlanFromTiles(GetTilesFlattened());
    //}

    //Buttons, Unity Buttons on Canvas are not compadible with script
    #endregion

    #region private functions

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


    private IEnumerator PropogateStep()
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
        // <summary>
        // get all unset tiles -> tiles that have no tile pattern assinged (tile.CurrentPattern)
        // for each of the unset, get the PossibleConnections
        // sort your unset tiles by the length of possible connection -> 0 == smallest lenght
        // get index 0 from the unset
        // do tile.AssingPattern() and assign a random tile pattern from its PossibleConnections
        // OUTSIDE THIS METHOD: Reapeat until no more tiles are left unset
        // <summary>

        List<Tile> UnsetTiles = GetUnsetTiles();

        //Check if there still are tiles to set

        if (UnsetTiles.Count == 0)
        {
            Debug.Log("all tiles are set");
            return;
        }

        //this is currently not going to give you the lowest tile
        List<Tile> lowestTiles = new List<Tile>();
        int lowestTile = int.MaxValue;

        //PropogateGrid on the set tile                     

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
        int rndIndex = Random.Range(0, lowestTiles.Count);
        Tile tileToSet = lowestTiles[rndIndex];

        Debug.Log(" Random Index " + lowestTiles.Count);


        //Assign one of the possible patterns to the tile
        tileToSet.AssignRandomPossiblePattern();
    }

    //Cardinal Directions Establishment 
    public List<Vector3Int> GetTileDirectionList()
    {
        List<Vector3Int> tileDirections = new List<Vector3Int>();
        foreach (Vector3Int tileDirection in Util.Directions)
        {
            if (Util.CheckInBounds(GridDimensions, Index))
            {
                tileDirections.Add((Vector3Int)tileDirection);
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

    //this function removes the colored sides used for connections
    public void ToggleConnectionVisibility()
    {
        foreach (var tile in TileGrid)
        {
            if (tile.Set)
            {
                tile.VisibilitySwitch();
            }
        }

    }

    #endregion
}



