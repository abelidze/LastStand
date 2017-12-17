using UnityEngine;
using System.Collections;

public class PointClick : MonoBehaviour
{
	void Update()
	{
		transform.Translate(0f,-5f*Time.deltaTime,0f,Space.World);
		transform.localScale = new Vector3(transform.localScale.x+Mathf.Abs(transform.position.y/500f),transform.localScale.y+Mathf.Abs(transform.position.y/500f),transform.localScale.z);

		if(transform.position.y<-2.5f) DestroyObject(gameObject);
	}
}
