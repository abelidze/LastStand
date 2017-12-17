using UnityEngine;
using System.Collections;

public class MiniMap : MonoBehaviour
{

	void Start()
	{
		//Find map bounds
		RaycastHit hit;
		Ray ray = GetComponent<Camera>().ViewportPointToRay (new Vector3(0,0,0));
		Ray ray2 = GetComponent<Camera>().ViewportPointToRay (new Vector3(1,1,0));
		
		Physics.Raycast (ray, out hit, Mathf.Infinity, 1 << 16);
		Vector3 bottomLeft = hit.point;
		
		Physics.Raycast (ray2, out hit, Mathf.Infinity, 1 << 16);
		Vector3 topRight = hit.point;
		
		Camera.main.GetComponent<Moving>().SetBoundries(bottomLeft.x, bottomLeft.z, topRight.x, topRight.z);

		gameObject.SetActive(false);
	}
}
