using UnityEngine;
using System.Collections;

public static class Const
{
	public const int Open = 1;
	public const int Closed = 2;
	public const int Unvisited = 3;
	public const int Blocked = 4;
	public const int Unit = 5;
	public const int OnRoute = 6;
	
	public const int LAYEREDTILE_NotLayered = 0;
	public const int LAYEREDTILE_Bridge = 1;
	public const int LAYEREDTILE_Tunnel = 2;
	
	public const float ASTAR_Costinc = 10.0f;
	public const float ASTAR_CostDinc = 14.0f;
	
	public const int BLOCKINGLEVEL_Normal = 0;
	public const int BLOCKINGLEVEL_Occupied = 1;
	public const int BLOCKINGLEVEL_OccupiedStatic = 2;
	
	public const int DIRECTION_Right = 0;
	public const int DIRECTION_Down = 1;
	public const int DIRECTION_Left = 2;
	public const int DIRECTION_Up = 3;
}