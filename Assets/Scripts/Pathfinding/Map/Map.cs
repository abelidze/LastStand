using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;

[ExecuteInEditMode]
public class Map : MonoBehaviour
{
	//Singleton
	public static Map main;
	
	//Member variables
	private static bool m_ShowGrid = true;	
	private static bool m_ShowOpenTiles = true;
	private static bool m_ShowClosedTiles = true;
	
	private static float m_TileSize = 4f;
	private static int m_Width = 25;
	private static int m_Length = 25;
	private static float m_WidthOffset = 0;
	private static float m_LengthOffset = 0;
	private static float m_MaxSteepness = 2f;
	private static float m_PassableHeight = 0f;
	private static int m_BlockIndent = 1;
	
	private static Cell[,] m_Grid;
	
	private static List<Vector3> debugAlgo = new List<Vector3>();
	
	//Properties
	public static bool ShowGrid
	{
		get
		{
			return m_ShowGrid;
		}
		set
		{
			if (m_Grid == null)
			{
				Initialise ();
			}
			
			m_ShowGrid = value;
		}
	}
	
	public static bool ShowOpenTiles
	{
		get
		{
			return m_ShowOpenTiles;
		}
		set
		{
			m_ShowOpenTiles = value;
		}
	}
	
	public static bool ShowClosedTiles
	{
		get
		{
			return m_ShowClosedTiles;
		}
		set
		{
			m_ShowClosedTiles = value;
		}
	}

	public static float TileSize
	{
		get
		{
			return m_TileSize;
		}
		set
		{
			if (Equals (m_TileSize, value))
			{
				return;
			}
			
			m_TileSize = value;
			
			Initialise ();
		}
	}
	
	public static int Width
	{
		get
		{
			return m_Width;
		}
		set
		{
			if (Equals (m_Width, value))
			{
				return;
			}
		
			m_Width = value;
			
			Initialise ();
		}
	}
	
	public static int Length
	{
		get
		{
			return m_Length;
		}
		set
		{
			if (Equals (m_Length, value))
			{
				return;
			}
			
			m_Length = value;
			
			Initialise ();
		}
	}
	
	public static float WidthOffset
	{
		get
		{
			return m_WidthOffset;
		}
		set
		{
			if (Equals (m_WidthOffset, value))
			{
				return;
			}
			
			m_WidthOffset = value;
			
			Initialise ();
		}
	}
	
	public static float LengthOffset
	{
		get
		{
			return m_LengthOffset;
		}
		set
		{
			if (Equals (m_LengthOffset, value))
			{
				return;
			}
			
			m_LengthOffset = value;
			
			Initialise ();
		}
	}
	
	public static float MaxSteepness
	{
		get
		{
			return m_MaxSteepness;
		}
		set
		{
			if (Equals (m_MaxSteepness, value))
			{
				return;
			}
			
			m_MaxSteepness = value;
			
			Initialise ();
		}
	}
	
	public static float PassableHeight
	{
		get
		{
			return m_PassableHeight;
		}
		set
		{
			if (Equals (m_PassableHeight, value))
			{
				return;
			}
			
			m_PassableHeight = value;
			
			Initialise ();
		}
	}
	
	public static int BlockIndent
	{
		get
		{
			return m_BlockIndent;
		}
		set
		{
			if (Equals (m_BlockIndent, value))
			{
				return;
			}
			
			if (value > 0)
			{
				m_BlockIndent = value;
			}
			else
			{
				m_BlockIndent = 1;
			}
			
			Initialise ();
		}
	}
	
	void Awake()
	{
		main = this;
	}
	
	void Start()
	{
		if (Application.isPlaying)
		{
			StartCoroutine (InitialiseAsRoutine ());
		}
	}
	
	void OnDrawGizmos()
	{
		if (m_ShowGrid && Application.isEditor && m_Grid != null)
		{
			foreach (Cell tile in m_Grid)
			{
				if (tile.Status == Const.Blocked) 
				{
					if (ShowClosedTiles)
					{
						Gizmos.color = Color.red;
						Gizmos.DrawWireCube (tile.Center, new Vector3(m_TileSize, 0.1f, m_TileSize));
					}
				}
				else
				{
					if (ShowOpenTiles)
					{
						if(tile.Occupied) Gizmos.color = Color.green;
						else Gizmos.color = Color.white;
						Gizmos.DrawWireCube (tile.Center, new Vector3(m_TileSize, 0.1f, m_TileSize));
					}
				}
			}
		}
		
		for (int i = 1; i < debugAlgo.Count; i++)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine (debugAlgo[i-1], debugAlgo[i]);
		}
	}
	
	public static IEnumerator InitialiseAsRoutine()
	{
		//ILevelLoader levelLoader = ManagerResolver.Resolve<ILevelLoader>();
		
		m_Grid = new Cell[Width, Length];
		
		//Create tiles
		for (int i=0; i<Width; i++)
		{
			for (int j=0; j<Length; j++)
			{
				float xCenter = m_WidthOffset + ((i*m_TileSize) + (m_TileSize/2.0f));
				float zCenter = m_LengthOffset + ((j*m_TileSize) + (m_TileSize/2.0f));
				Vector3 center = new Vector3(xCenter, 0, zCenter);				
				center.y = Terrain.activeTerrain.SampleHeight(center);
				
				m_Grid[i,j] = new Cell(i, j, center);
			}
		}
		
		//if (levelLoader != null) levelLoader.ChangeText ("Evaluating tiles");
		yield return null;

		//Evaluate
		for (int i=0; i<Width; i++)
		{
			for (int j=0; j<Length; j++)
			{
				m_Grid[i,j].Evaluate();
			}
		}
		
		//if (levelLoader != null) levelLoader.ChangeText ("Populating internal array");
		yield return null;
		
		//Now all the tiles have been initialised, we need to populate the tiles internal array with accessible tiles
		for (int i=0; i<Width; i++)
		{
			for (int j=0; j<Length; j++)
			{
				FindAccessibleTiles (m_Grid[i,j]);
			}
		}

		//if(!Network.isServer) Network.Connect(GameObject.FindObjectOfType<Connection>().remoteIP,GameObject.FindObjectOfType<Connection>().remotePort);
	}
	
	public static void Initialise()
	{
		m_Grid = new Cell[Width, Length];
		
		//Create tiles
		for (int i=0; i<Width; i++)
		{
			for (int j=0; j<Length; j++)
			{
				float xCenter = m_WidthOffset + ((i*m_TileSize) + (m_TileSize/2.0f));
				float zCenter = m_LengthOffset + ((j*m_TileSize) + (m_TileSize/2.0f));
				Vector3 center = new Vector3(xCenter, 0, zCenter);				
				center.y = Terrain.activeTerrain.SampleHeight(center);
				
				m_Grid[i,j] = new Cell(i, j, center);
			}
		}

		//Evaluate
		for (int i=0; i<Width; i++)
		{
			for (int j=0; j<Length; j++)
			{
				m_Grid[i,j].Evaluate();
			}
		}
	}
	
	public static Cell GetClosestTile(Vector3 position)
	{
		int iValue = (int)((position.x - m_WidthOffset)/m_TileSize);
		int jValue = (int)((position.z - m_LengthOffset)/m_TileSize);
		
		if (iValue < 0) iValue = 0;
		else if (iValue >= Width) iValue = Width-1;
		
		if (jValue < 0) jValue = 0;
		else if (jValue >= Length) jValue = Length-1;
		
		Cell tileToReturn = m_Grid[iValue, jValue];
		
		float distance = Mathf.Abs (tileToReturn.Center.y - position.y);
		Cell lTile = null;
		foreach (Cell tile in tileToReturn.LayeredTiles)
		{
			if (Mathf.Abs (tile.Center.y - position.y) < distance)
			{
				lTile = tile;
				distance = Mathf.Abs (tile.Center.y - position.y);
			}
		}
		
		if (lTile != null)
		{
			tileToReturn = lTile;
		}

		return tileToReturn;
	}
	
	public static Cell GetClosestAvailableTile(Vector3 position)
	{
		debugAlgo.Clear ();
		int iValue = (int)((position.x - m_WidthOffset)/m_TileSize);
		int jValue = (int)((position.z - m_LengthOffset)/m_TileSize);
		
		if (iValue < 0) iValue = 0;
		else if (iValue >= Width) iValue = Width-1;
		
		if (jValue < 0) jValue = 0;
		else if (jValue >= Length) jValue = Length-1;
		
		Cell tileToReturn = m_Grid[iValue, jValue];
		
		if (tileToReturn.LayeredTiles.Count > 0)
		{
			if (Mathf.Abs (tileToReturn.Center.y - position.y) > Mathf.Abs (tileToReturn.LayeredTiles[0].Center.y - position.y))
			{
				tileToReturn = tileToReturn.LayeredTiles[0];
			}
		}
		
		if (tileToReturn.Status == Const.Blocked)
		{
			//Need to iterate to find closest available tile
			int directionCounter = Const.DIRECTION_Right;
			int widthCounter = 1;
			int lengthCounter = 1;
			int IValue = tileToReturn.I;
			int JValue = tileToReturn.J;
			
		    while (tileToReturn.Status == Const.Blocked)
			{
				int counter;
				
				//If we're travelling left or right use the width counter, up or down use the length counter
				if (directionCounter == Const.DIRECTION_Right || directionCounter == Const.DIRECTION_Left)
				{
					counter = widthCounter;
				}
				else
				{
					counter = lengthCounter;
				}
				
				for (int i=0; i<counter; i++)
				{
					switch (directionCounter)
					{
					case Const.DIRECTION_Right:
						//Increase I value (go right)
						IValue++;
						
						//Check if we're at the width so we don't get an exception
						if (IValue >= Width)
						{
							//We're past the width, decrease I value
							IValue = Width-1;
							
							//Set JValue to whatever it is minus lengthcounter (no point checking tiles we've already checked!)
							JValue = JValue - lengthCounter;
							
							//Since we've skipped all the downward tiles, go left
							directionCounter = Const.DIRECTION_Left;
							
							//Update the length counter as we're skipping it out
							lengthCounter++;
						}
						else
						{
							//Have we travelled far enough?
							if (i == widthCounter - 1)
							{
								//We've travelled as far as we want to, change the direction and increase the width counter
								directionCounter = Const.DIRECTION_Down;
								widthCounter++;
							}
						}
						
						break;
						
					case Const.DIRECTION_Down:
						
						JValue--;
						if (JValue < 0)
						{
							JValue = 0;
							
							IValue = IValue - widthCounter;
							
							directionCounter = Const.DIRECTION_Up;
							
							widthCounter++;
						}
						else
						{
							if (i == lengthCounter - 1)
							{
								directionCounter = Const.DIRECTION_Left;
								lengthCounter++;
							}
						}
						
						break;
						
					case Const.DIRECTION_Left:
						
						IValue--;
						if (IValue < 0)
						{
							IValue = 0;
							
							JValue = JValue + lengthCounter;
							
							directionCounter = Const.DIRECTION_Right;
							
							lengthCounter++;
						}
						else
						{
							if (i == widthCounter - 1)
							{
								directionCounter = Const.DIRECTION_Up;
								widthCounter++;
							}
						}
						
						break;
						
					case Const.DIRECTION_Up:
						
						JValue++;
						if (JValue >= Length)
						{
							JValue = Length-1;
							
							IValue = IValue + widthCounter;
							
							directionCounter = Const.DIRECTION_Down;
							
							widthCounter++;
						}
						else
						{
							if (i == lengthCounter - 1)
							{
								directionCounter = Const.DIRECTION_Right;
								lengthCounter++;
							}
						}
						
						break;
					}
					
					tileToReturn = m_Grid[IValue, JValue];
					debugAlgo.Add (tileToReturn.Center);
				}
			}
		}
		
		return tileToReturn;
	}
	
	public static Cell GetClosestAvailableFreeTile(Vector3 position)
	{
		int iValue = (int)((position.x - m_WidthOffset)/m_TileSize);
		int jValue = (int)((position.z - m_LengthOffset)/m_TileSize);
		
		if (iValue < 0) iValue = 0;
		else if (iValue >= Width) iValue = Width-1;
		
		if (jValue < 0) jValue = 0;
		else if (jValue >= Length) jValue = Length-1;
		
		Cell tileToReturn = m_Grid[iValue, jValue];
		
		float yVal = Mathf.Abs (tileToReturn.Center.y - position.y);
		foreach (Cell tile in tileToReturn.LayeredTiles)
		{
			if (Mathf.Abs (tile.Center.y - position.y) < yVal)
			{
				yVal = Mathf.Abs (tile.Center.y - position.y);
				tileToReturn = tile;
			}
		}
		
		if (tileToReturn.Status == Const.Blocked || tileToReturn.ExpectingArrival)
		{
			//Need to iterate to find closest available tile
			int directionCounter = Const.DIRECTION_Right;
			int widthCounter = 1;
			int lengthCounter = 1;
			int IValue = tileToReturn.I;
			int JValue = tileToReturn.J;
			
		    while (tileToReturn.Status == Const.Blocked || tileToReturn.ExpectingArrival)
			{
				int counter;
				
				//If we're travelling left or right use the width counter, up or down use the length counter
				if (directionCounter == Const.DIRECTION_Right || directionCounter == Const.DIRECTION_Left)
				{
					counter = widthCounter;
				}
				else
				{
					counter = lengthCounter;
				}
				
				for (int i=0; i<counter; i++)
				{
					switch (directionCounter)
					{
					case Const.DIRECTION_Right:
						//Increase I value (go right)
						IValue++;
						
						//Check if we're at the width so we don't get an exception
						if (IValue >= Width)
						{
							//We're past the width, decrease I value
							IValue = Width-1;
							
							//Set JValue to whatever it is minus lengthcounter (no point checking tiles we've already checked!)
							JValue = JValue - lengthCounter;
							
							//Since we've skipped all the downward tiles, go left
							directionCounter = Const.DIRECTION_Left;
							
							//Update the length counter as we're skipping it out
							lengthCounter++;
						}
						else
						{
							//Have we travelled far enough?
							if (i == widthCounter - 1)
							{
								//We've travelled as far as we want to, change the direction and increase the width counter
								directionCounter = Const.DIRECTION_Down;
								widthCounter++;
							}
						}
						
						break;
						
					case Const.DIRECTION_Down:
						
						JValue--;
						if (JValue < 0)
						{
							JValue = 0;
							
							IValue = IValue - widthCounter;
							
							directionCounter = Const.DIRECTION_Up;
							
							widthCounter++;
						}
						else
						{
							if (i == lengthCounter - 1)
							{
								directionCounter = Const.DIRECTION_Left;
								lengthCounter++;
							}
						}
						
						break;
						
					case Const.DIRECTION_Left:
						
						IValue--;
						if (IValue < 0)
						{
							IValue = 0;
							
							JValue = JValue + lengthCounter;
							
							directionCounter = Const.DIRECTION_Right;
							
							lengthCounter++;
						}
						else
						{
							if (i == widthCounter - 1)
							{
								directionCounter = Const.DIRECTION_Up;
								widthCounter++;
							}
						}
						
						break;
						
					case Const.DIRECTION_Up:
						
						JValue++;
						if (JValue >= Length)
						{
							JValue = Length-1;
							
							IValue = IValue + widthCounter;
							
							directionCounter = Const.DIRECTION_Down;
							
							widthCounter++;
						}
						else
						{
							if (i == lengthCounter - 1)
							{
								directionCounter = Const.DIRECTION_Right;
								lengthCounter++;
							}
						}
						
						break;
					}
					
					tileToReturn = m_Grid[IValue, JValue];
					
					yVal = Mathf.Abs (tileToReturn.Center.y - position.y);
					foreach (Cell tile in tileToReturn.LayeredTiles)
					{
						if (Mathf.Abs (tile.Center.y - position.y) < yVal)
						{
							yVal = Mathf.Abs (tile.Center.y - position.y);
							tileToReturn = tile;
						}
					}
				}
			}
		}
		
		return tileToReturn;
	}
	
	public static Cell GetClosestArrivalTile(Vector3 position)
	{
		int iValue = (int)((position.x - m_WidthOffset)/m_TileSize);
		int jValue = (int)((position.z - m_LengthOffset)/m_TileSize);
		
		if (iValue < 0) iValue = 0;
		else if (iValue >= Width) iValue = Width-1;
		
		if (jValue < 0) jValue = 0;
		else if (jValue >= Length) jValue = Length-1;
		
		Cell tileToReturn = m_Grid[iValue, jValue];
		
		float yVal = Mathf.Abs (tileToReturn.Center.y - position.y);
		foreach (Cell tile in tileToReturn.LayeredTiles)
		{
			if (Mathf.Abs (tile.Center.y - position.y) < yVal)
			{
				yVal = Mathf.Abs (tile.Center.y - position.y);
				tileToReturn = tile;
			}
		}
		
		if (tileToReturn.Status == Const.Blocked || tileToReturn.ExpectingArrival)
		{
			Queue<Cell> tilesToCheck = new Queue<Cell>();
			tilesToCheck.Enqueue (tileToReturn);
			
			while (tilesToCheck.Count > 0)
			{
				tileToReturn = tilesToCheck.Dequeue ();
				
				foreach (Cell tile in tileToReturn.AccessibleTiles)
				{
					if (tile.Status != Const.Blocked && !tile.ExpectingArrival)
					{
						return tile;
					}
					
					if (!tilesToCheck.Contains (tile))
					{
						tilesToCheck.Enqueue (tile);
					}
				}
			}
			
			tileToReturn = null;
		}
		
		return tileToReturn;
	}
	
	public static void FindAccessibleTiles(Cell tile)
	{
		//Need to find which tiles this tile can travel to
		try
		{
			Cell tileLeft = m_Grid[tile.I-1, tile.J];
			Cell tileRight = m_Grid[tile.I+1, tile.J];
			Cell tileUp = m_Grid[tile.I, tile.J+1];
			Cell tileDown = m_Grid[tile.I, tile.J-1];
			
			Cell topLeft = m_Grid[tile.I-1, tile.J+1];
			Cell topRight = m_Grid[tile.I+1, tile.J+1];
			Cell bottomRight = m_Grid[tile.I+1, tile.J-1];
			Cell bottomLeft = m_Grid[tile.I-1, tile.J-1];
			
			CheckTileConnection (tile, tileLeft);
			CheckTileConnection (tile, tileRight);
			CheckTileConnection (tile, tileUp);
			CheckTileConnection (tile, tileDown);
			CheckTileConnection (tile, topLeft);
			CheckTileConnection (tile, topRight);
			CheckTileConnection (tile, bottomRight);
			CheckTileConnection (tile, bottomLeft);
		}
		catch 
		{
			//Silently ignored, bad coder!
		}
	}
	
	private static void CheckTileConnection(Cell currentTile, Cell tileToCheck)
	{
		float acceptableHeightDiff = 2.5f;
		if (tileToCheck.Status == Const.Unvisited && Mathf.Abs (tileToCheck.Center.y - currentTile.Center.y) < acceptableHeightDiff)
		{
			currentTile.AccessibleTiles.Add (tileToCheck);
		}
		
		//Check if any layered tiles are accessible
		foreach (Cell layeredTile in tileToCheck.LayeredTiles)
		{
			if (Mathf.Abs (currentTile.Center.y - layeredTile.Center.y) < acceptableHeightDiff)
			{
				currentTile.AccessibleTiles.Add (layeredTile);
			}
		}
		
		//Check if the current tile's layered tiles can access any of the other tiles
		foreach (Cell currentLayeredTile in currentTile.LayeredTiles)
		{
			if (Mathf.Abs (currentLayeredTile.Center.y - tileToCheck.Center.y) < acceptableHeightDiff && tileToCheck.Status == Const.Unvisited)
			{
				currentLayeredTile.AccessibleTiles.Add (tileToCheck);
			}
			
			foreach (Cell layeredTile in tileToCheck.LayeredTiles)
			{
				if (Mathf.Abs (currentLayeredTile.Center.y - layeredTile.Center.y) < acceptableHeightDiff)
				{
					currentLayeredTile.AccessibleTiles.Add (layeredTile);
				}
			}
		}
	}
	
	public static void AddLayeredTile(int gridI, int gridJ, float height, bool isBridge, Collider collider)
	{
		Vector3 baseCenter = m_Grid[gridI, gridJ].Center;
		Vector3 centerPos = new Vector3(baseCenter.x, height, baseCenter.z);
		m_Grid[gridI, gridJ].LayeredTiles.Add (new Cell(gridI, gridJ, centerPos));
	}
	
	public static void AssignGrid(Cell[,] grid)
	{
		m_Grid = grid;
	}
}