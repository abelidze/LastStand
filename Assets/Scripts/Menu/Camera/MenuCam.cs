using UnityEngine;
using System.Collections;

public class MenuCam : MonoBehaviour
{
	public Connection equip;
	public float speed = 5f;

	void Update()
	{
		if(equip.GetFinding() == 1)
		{
			if(equip.GameStart>=3f)
			{
				transform.position = Vector3.Lerp(transform.position,new Vector3(5,-350,-10),Time.deltaTime/speed);
				transform.rotation = Quaternion.Slerp(transform.rotation,Quaternion.Euler(30,0,0),Time.deltaTime*speed);
			}
			else
			{
				transform.position = Vector3.Lerp(transform.position,new Vector3(5,-164,-10),Time.deltaTime*speed);
				transform.rotation = Quaternion.Slerp(transform.rotation,Quaternion.Euler(30,0,0),Time.deltaTime*speed);
			}
		}
		else if(equip.GetWindow() == 2)
		{
			transform.position = Vector3.Slerp(transform.position,new Vector3(7,6,-10),Time.deltaTime*speed);
			transform.rotation = Quaternion.Slerp(transform.rotation,Quaternion.Euler(10,0,0),Time.deltaTime*speed);
		}
		else
		{
			transform.position = Vector3.Lerp(transform.position,new Vector3(5,9,-10),Time.deltaTime*speed);
			transform.rotation = Quaternion.Slerp(transform.rotation,Quaternion.Euler(30,0,0),Time.deltaTime*speed);
		}
	}
}
