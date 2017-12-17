using UnityEngine;
using System.Collections;

public class Moving : MonoBehaviour
{
	private Vector3 move;
	public float speed = 20f;

	//Camera Variables
	public float m_MaxFieldOfView = 85.0f;
	public float m_MinFieldOfView = 20.0f;
	
	public float ScrollSpeed = 8.0f;
	public float ScrollAcceleration = 30.0f;
	public float ScrollZone = 64f;
	private Rect BOUND;


	public Transform target;
	
	public float ZoomRate = 500.0f;
	
	const float lasty = 32.4f;
	public float lastz = 0f;

	private float duration = 0f;

	private bool atScreenEdge = false;
	private bool canPan = false;


	void Update()
	{
		move = Vector3.zero;

		if(Input.GetKeyDown("e")) canPan = canPan ^ true;

		#if UNITY_ANDROID
		Touch[] touches = Input.touches;

		if(touches.Length>0)
		{
			if(touches.Length == 2)
			{
				if(touches[0].phase == TouchPhase.Moved)
				{
					move.x = -touches[0].deltaPosition.x;
					move.z = -touches[0].deltaPosition.y;
				}
			}
		}
		#else
		if(canPan)
		{
			if (Input.mousePosition.x <= ScrollZone)
			{			
				move.x = -1;
				
				atScreenEdge = true;
			}
			
			if (Input.mousePosition.x >= Screen.width-ScrollZone)
			{
				move.x = 1;
				
				atScreenEdge = true;
			}
			
			if (Input.mousePosition.y <= ScrollZone)
			{
				move.z = -1;
				
				atScreenEdge = true;
			}
			
			if (Input.mousePosition.y >= Screen.height-ScrollZone)
			{
				move.z = 1;
				
				atScreenEdge = true;
			}

			if((move == Vector3.zero)&&(atScreenEdge)) atScreenEdge = false;

			if(Input.GetAxis("Mouse ScrollWheel")!=0) Zoom();

			if(transform.position.y<lasty)
			if(Camera.main.fieldOfView > m_MinFieldOfView + (m_MaxFieldOfView-m_MinFieldOfView)/2f)
			{
				transform.position = Vector3.Lerp(transform.position,new Vector3(transform.position.x,lasty,transform.position.z),Time.deltaTime*4f);
				if(lasty-transform.position.y<0.01f) transform.position = new Vector3(transform.position.x,lasty,transform.position.z);
			}
		}

		if (atScreenEdge)
		{
			duration += Time.deltaTime;
			move*=duration;
		}
		else
		{
			duration = 0;
			move = new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));
		}
		if(Input.GetButton("Jump")) transform.position = new Vector3(target.position.x,transform.position.y,target.position.z-10f+lastz);
		#endif

		transform.Translate(move*Time.deltaTime*speed, Space.World);
		CheckEdgeMovement();
	}

	private void CheckEdgeMovement()
	{
		Ray r1 = Camera.main.ViewportPointToRay (new Vector3(0,1,0));
		Ray r2 = Camera.main.ScreenPointToRay (new Vector3(Screen.width-1,Screen.height-1,0));
		Ray r3 = Camera.main.ViewportPointToRay (new Vector3(0,0,0));
		
		float left, right, top, bottom;
		
		RaycastHit h1;
		
		Physics.Raycast (r1, out h1, Mathf.Infinity, 1<< 16);		
		left = h1.point.x;
		top = h1.point.z;
		
		Physics.Raycast (r2, out h1, Mathf.Infinity, 1<< 16);
		right = h1.point.x;
		
		Physics.Raycast (r3, out h1, Mathf.Infinity, 1<< 16);
		bottom = h1.point.z;
		
		if (left < BOUND.xMin)
		{
			Camera.main.transform.Translate (new Vector3(BOUND.xMin-left,0,0), Space.World);
		}
		else if (right > BOUND.xMax)
		{
			Camera.main.transform.Translate (new Vector3(BOUND.xMax-right,0,0), Space.World);
		}
		
		if (bottom < BOUND.yMin)
		{
			Camera.main.transform.Translate (new Vector3(0,0,BOUND.yMin-bottom), Space.World);
		}
		else if (top > BOUND.yMax)
		{
			Camera.main.transform.Translate (new Vector3(0,0,BOUND.yMax-top), Space.World);
		}
	}

	public void Zoom()
	{
		Camera.main.fieldOfView -= Input.GetAxis("Mouse ScrollWheel")*ZoomRate*Time.deltaTime;

		if (Camera.main.fieldOfView < m_MinFieldOfView) 
		{
			Camera.main.fieldOfView = m_MinFieldOfView;
		}
		else if(Camera.main.fieldOfView > m_MaxFieldOfView)
		{
			Camera.main.fieldOfView = m_MaxFieldOfView;
		}
		else if(Camera.main.fieldOfView < m_MinFieldOfView + (m_MaxFieldOfView-m_MinFieldOfView)/2f)
		{
			Vector3 posrot = transform.position;
			if(Input.GetButton("Jump")) posrot = new Vector3(target.position.x,0f,target.position.z);
			else posrot = new Vector3(Camera.main.transform.position.x,0f,Camera.main.transform.position.z+10f);

			float prev = transform.position.z;

			Camera.main.transform.RotateAround(posrot,new Vector3(1f,0f,0f),-Input.GetAxis("Mouse ScrollWheel")*ZoomRate*Time.deltaTime);

			lastz += transform.position.z - prev;
		}
		else
		{
			Camera.main.transform.rotation = Quaternion.Euler(75f,0f,0f);
			lastz = 0f;
		}
		if(transform.position.y>lasty) transform.position = new Vector3(transform.position.x,lasty,transform.position.z);
	}

	
	public void SetBoundries(float minX, float minY, float maxX, float maxY)
	{
		BOUND = new Rect();
		BOUND.xMin = minX;
		BOUND.xMax = maxX;
		BOUND.yMin = minY+1;
		BOUND.yMax = maxY;
	}
}