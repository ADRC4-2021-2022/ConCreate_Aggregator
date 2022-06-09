/*Class Tile
{
    Vector3Int index;
    Grid grid;
    List<TilePatterns> possiblePatterns;

}

Class TilePattern
{
    int TileIndex; //every tile index needs to be unique
    GameObject TilePrefab;
    List<Connection> Connections = new List<Connection>(6); //one connection for every axis direction
}

Class Connection
{
    //If a tile has a certain connection, it can connect to these possible Neighbours
    List<Tile> possibleNeigbours;
}

Class WFC
{
    //this will be the generator for your WFC

    void CreateGrid()
    {

    }

    void SelectNextTile()
    {
        //select the tile with the least amount of options
    }

    void PropogateGrid(List<Tiles> lastAdjustedTiles) //Make sure you don't get infinite loops
    {
        //Find all the neighbours of the lastAdjustedTiles that are not set yet

        //Save a list of every tile that has been changed

        //Reduce the possible tile patterns in the neighbours according to the connections
     

        //Run PropogateGrid with the next changed tiles
    }

    //This function should run every time the amount of options in a tile is reduced to 1
    void AssignTilePattern()
    {
        //Create the geometry of the tile inside the grid
        //instatiate prefab
    }
    
    void GlobalOperations()
    {
        //eg. reducing the posibilities next to a street
    }

    //For further implementation
    void BackTrack()
    {
        //When a tile has no possible options, return to the last operation that reduced the options in the tile
        //Remove the selected option that made the grid invalid
        //Select another option.
        //How many backTracks steps do you store
    }

}

Class WFCGrid{
    Tile[,,] Tiles;
    //All the operations with grid
}*/