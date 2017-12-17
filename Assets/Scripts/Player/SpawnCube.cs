using UnityEngine;
using System.Collections;

public class SpawnCube : MonoBehaviour
{

	void Start()
	{
		Map.GetClosestTile(transform.position).Status = Const.Blocked;
	}

	void OnMouseDown()
	{
		Network.Destroy(GetComponent<NetworkView>().viewID);
		Network.RemoveRPCs(GetComponent<NetworkView>().viewID);
	}

	void OnDestroy()
	{
		Map.GetClosestTile(transform.position).Status = Const.Unvisited;
	}
}