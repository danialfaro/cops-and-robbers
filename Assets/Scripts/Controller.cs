using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
       
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {

        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];
                
        //TODO: Inicializar matriz a 0's
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
                //Debug.Log(i.ToString() + " - " + j.ToString());
            }
        }

        //TODO: Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                // por cada tile i comprobamos si son adyacentes al resto de tiles j
                if(i == j) matriu[i, j] = 1;

                // [0, 1] tile 0 es adyacente a la 1?
                
                if(j == i + 8) matriu[i, j] = 1; //arriba
                if(j == i - 8) matriu[i, j] = 1; //abajo

                // j = 8  i = 9
                if (j == i - 1 && i % 8 != 0) matriu[i, j] = 1; //izquierda
                if (j == i + 1 && j % 8 != 0) matriu[i, j] = 1; //derecha

                //Debug.Log(i.ToString() + " - " + j.ToString() + " : " + matriu[i, j]);
                

            }
        }        

        //TODO: Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if(matriu[i, j] == 1) tiles[i].adjacency.Add(j);
            }
        }

    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        /*TODO: Cambia el código de abajo para hacer lo siguiente
        - Elegimos una casilla aleatoria entre las seleccionables que puede ir el caco
        - Movemos al caco a esa casilla
        - Actualizamos la variable currentTile del caco a la nueva casilla
        */
        List<Tile> selectables = new List<Tile>();
        
        foreach (Tile tile in tiles)
        {            
            if (tile.selectable) selectables.Add(tile); 
        }

        GameObject nearestCop;
        int d1 = DistanceBetweenNodes(tiles[clickedTile], tiles[cops[0].GetComponent<CopMove>().currentTile]);
        int d2 = DistanceBetweenNodes(tiles[clickedTile], tiles[cops[1].GetComponent<CopMove>().currentTile]);
        if (d1 <= d2) nearestCop = cops[0];
        else nearestCop = cops[1];


        Tile farest = null;
        int fastDistance = 0;
        foreach (Tile tileSelectable in selectables)
        {
            int d = DistanceBetweenNodes(tileSelectable, tiles[nearestCop.GetComponent<CopMove>().currentTile]);

            if (farest == null) {
                farest = tileSelectable;
                fastDistance = d;
            }

            if (d > fastDistance)
            {
                farest = tileSelectable;
                fastDistance = d;
            }
        }

        Tile randomTile = selectables[Random.Range(0, selectables.Count)];

        robber.GetComponent<RobberMove>().MoveToTile(randomTile);
        robber.GetComponent<RobberMove>().currentTile = randomTile.numTile;

    }

    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;

    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop, int distance = 3)
    {

        int indexcurrentTile;

        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;
        
        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        //TODO: Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        //Tendrás que cambiar este código por el BFS

        Tile originalTile = tiles[indexcurrentTile];
        
        originalTile.visited = true;
        //originalTile.parent = originalTile;
        nodes.Enqueue(originalTile);
        
        while (nodes.Count > 0)
        {
            Tile nextNode = nodes.Dequeue();
            //Debug.Log(nextNode.numTile);

            foreach (int next in nextNode.adjacency)
            {                
                if (!tiles[next].visited)
                {
                    nodes.Enqueue(tiles[next]);

                    tiles[next].parent = nextNode;
                    tiles[next].distance = nextNode.distance + 1;
                    tiles[next].visited = true;

                    if (tiles[next].distance < distance) tiles[next].selectable = true;
                    //Debug.Log(tiles[next].numTile + " d(" + tiles[next].distance + ")");
                }
            }
        }

    }    

    public int DistanceBetweenNodes(Tile node, Tile target)
    {

        Tile originalTile = node;

        Queue<Tile> nodes = new Queue<Tile>();

        originalTile.visited = true;
        //originalTile.parent = originalTile;
        nodes.Enqueue(originalTile);

        while (nodes.Count > 0)
        {
            Tile nextNode = nodes.Dequeue();
            //Debug.Log(nextNode.numTile);

            foreach (int next in nextNode.adjacency)
            {
                if (!tiles[next].visited)
                {
                    nodes.Enqueue(tiles[next]);

                    tiles[next].parent = nextNode;
                    tiles[next].distance = nextNode.distance + 1;
                    tiles[next].visited = true;

                    if (tiles[next].numTile == target.numTile) return tiles[next].distance;
                    //Debug.Log(tiles[next].numTile + " d(" + tiles[next].distance + ")");
                }
            }
        }

        return 0;
    }



}
