using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Cell
{
	public int I;
	public int J;
	public int Size;
	public int Status;
	
	public List<Cell> AccessibleTiles = new List<Cell>();
	public List<Cell> LayeredTiles;
	
	public Vector3 Center;
	
	
	public RTSObject OccupiedBy;
	public bool Occupied = false;
	public bool OccupiedStatic = false;
	public bool ExpectingArrival = false;

	public float Cost;
	public bool Start;
	
	public bool End;

	public float hCost;
	public float fCost;
	
	public Cell ParentTile;

	public float Costinc = 10.0f;
	public float CostDinc = 14.0f;
	
	//Constructor----------------------------------------------------------
	public Cell(int i, int j, Vector3 center)
	{
		I = i;
		J = j;
		//Size = Grid.TileSize;
		Center = center;
		Status = Const.Unvisited;
		LayeredTiles = new List<Cell>();
	}
	

	//Methods------------------------------------------------------------------------
	
	public void Evaluate()
	{
		LayeredTiles.Clear ();
		Status = Const.Unvisited;
		Center.y = Terrain.activeTerrain.SampleHeight(Center);
		EvaluateTile();
	}

	private void EvaluateTile()
	{
		float halfSize = Size/2.0f;
		
		//Check bridges and tunnels
		Vector3 bottomLeft = Center + new Vector3(-halfSize, 0, -halfSize);
		Vector3 bottomRight = Center + new Vector3(halfSize, 0, -halfSize);
		Vector3 topRight = Center + new Vector3(halfSize, 0, halfSize);
		Vector3 topLeft = Center + new Vector3(-halfSize, 0, halfSize);
		
		float startOffset = 5.0f;
		
		Ray rayUp = new Ray(Center + (Vector3.down*startOffset), Vector3.up);
		Ray rayUp2 = new Ray(bottomLeft + (Vector3.down*startOffset), Vector3.up);
		Ray rayUp3 = new Ray(bottomRight + (Vector3.down*startOffset), Vector3.up);
		Ray rayUp4 = new Ray(topRight + (Vector3.down*startOffset), Vector3.up);
		Ray rayUp5 = new Ray(topLeft + (Vector3.down*startOffset), Vector3.up);
		
		RaycastHit hitCenter;
		
		//Are we within the block indent?
		if (I < Map.BlockIndent || I >= Map.Width-Map.BlockIndent || J < Map.BlockIndent || J >= Map.Length-Map.BlockIndent)
		{
			//Within the indent
			Status = Const.Blocked;
			return;
		}
		
		//Check steepness of terrain
		//Calculate corner points		
		Terrain terrain = Terrain.activeTerrain;
		float minHeight = Mathf.Min (terrain.SampleHeight (Center), terrain.SampleHeight (bottomLeft), terrain.SampleHeight (bottomRight), terrain.SampleHeight (topRight), terrain.SampleHeight (topLeft));
		float maxHeight = Mathf.Max (terrain.SampleHeight (Center), terrain.SampleHeight (bottomLeft), terrain.SampleHeight (bottomRight), terrain.SampleHeight (topRight), terrain.SampleHeight (topLeft));
				
		if (maxHeight-minHeight > Map.MaxSteepness)
		{
			//Too steep
			Status = Const.Blocked;
			//m_Buildable = false;
			return;
		}
		
		//Check for obstacles		
		//We want to ignore units, terrain, tunnels and bridges, so cast against everything apart from layers 8, 9, 11, 18, 19 and 20
		LayerMask layerMask = ~(1 << 8 | 1 << 10 | 1 << 11 );//~(1 << 8 | 1 << 9 | 1 << 11 | 1 << 18 | 1 << 19 | 1 << 20);
		
		//Need to raycast against center and all 4 corner points
		bool result1 = Physics.Raycast (rayUp, Mathf.Infinity, layerMask);
		bool result2 = Physics.Raycast (rayUp2, Mathf.Infinity, layerMask);
		bool result3 = Physics.Raycast (rayUp3, Mathf.Infinity, layerMask);
		bool result4 = Physics.Raycast (rayUp4, Mathf.Infinity, layerMask);
		bool result5 = Physics.Raycast (rayUp5, Mathf.Infinity, layerMask);
		
		if (result1 || result2 || result3 || result4 || result5)
		{
			//We've hit something above us, so we can't build on this tile, but is it passable?
			//m_Buildable = false;
			
			if (result1)
			{
				if (Physics.Raycast(rayUp, out hitCenter, Mathf.Infinity, layerMask))
				{
					float distance = Vector3.Distance (Center, hitCenter.point);
					if (distance < Map.PassableHeight)
					{
						Status = Const.Blocked;
						//m_Buildable = false;
						return;
					}
				}
			}
			
			if (result2)
			{
				if (Physics.Raycast(rayUp2, out hitCenter, Mathf.Infinity, layerMask))
				{
					float distance = Vector3.Distance (Center, hitCenter.point);
					if (distance < Map.PassableHeight)
					{
						Status = Const.Blocked;
						//m_Buildable = false;
						return;
					}
				}
			}
			
			if (result3)
			{
				if (Physics.Raycast(rayUp3, out hitCenter, Mathf.Infinity, layerMask))
				{
					float distance = Vector3.Distance (Center, hitCenter.point);
					if (distance < Map.PassableHeight)
					{
						Status = Const.Blocked;
						//m_Buildable = false;
						return;
					}
				}
			}
			
			if (result4)
			{
				if (Physics.Raycast(rayUp4, out hitCenter, Mathf.Infinity, layerMask))
				{
					float distance = Vector3.Distance (Center, hitCenter.point);
					if (distance < Map.PassableHeight)
					{
						Status = Const.Blocked;
						//m_Buildable = false;
						return;
					}
				}
			}
			
			if (result5)
			{
				if (Physics.Raycast(rayUp5, out hitCenter, Mathf.Infinity, layerMask))
				{
					float distance = Vector3.Distance (Center, hitCenter.point);
					if (distance < Map.PassableHeight)
					{
						Status = Const.Blocked;
						//m_Buildable = false;
						return;
					}
				}
			}
		}		
		else
		{
			//We haven't hit anything, so we're buildable and open
			//m_Buildable = true;
		}
	}
	
	public void SetOccupied(RTSObject occupiedBy, bool occupiedStatic)
	{
		OccupiedBy = occupiedBy;
		Occupied = true;
		OccupiedStatic = occupiedStatic;
	}
	
	public void NoLongerOccupied(RTSObject occupiedBy)
	{
		OccupiedBy = null;
		Occupied = false;
		OccupiedStatic = false;
	}
}