using UnityEngine;
using System.Collections;

public class RotatePlayer : MonoBehaviour
{
	private float rot;
	private float _mouse;
	private Animation _anim;

	void Start()
	{
		_anim = GetComponentInChildren<Animation>();
	}


	void Update()
	{
		if(!_anim.isPlaying) _anim.CrossFade("idle");
	}

	void OnMouseDown()
	{
		_mouse = Input.mousePosition.x;
	}

	void OnMouseDrag()
	{
		rot+=(_mouse-Input.mousePosition.x)*Time.deltaTime*45f;
		if(rot>360) rot-=360;
		if(rot<0) rot+=360;
		transform.rotation = Quaternion.Euler(0,rot,0);
		_mouse = Input.mousePosition.x;
	}
}
