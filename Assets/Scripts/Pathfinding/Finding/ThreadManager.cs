using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;

public class ThreadManager : MonoBehaviour
{
	private List<GetPathThread> m_PathFindingThreads = new List<GetPathThread>();
	private List<GameObject> Respawns = new List<GameObject>();
	private float timer;
	private bool cubes = false;
	
	public static ThreadManager main;

	public GameObject ppf;
	
	void Awake()
	{
		main = this;
	}

	// Update is called once per frame
	void Update () 
	{
		//Iterate backwards so we can remove items as we're iterating
		for (int i=m_PathFindingThreads.Count-1; i >= 0; i--)
		{
			if (m_PathFindingThreads[i].Update ())
			{
				m_PathFindingThreads.RemoveAt (i);
			}
		}

		if(cubes)
		if(Input.GetMouseButtonDown(0))
		{
			if(Time.time-timer < 0.25f)
			{
				RaycastHit hit;

				Ray rc = Camera.main.ScreenPointToRay(Input.mousePosition);
				if(Physics.Raycast(rc, out hit, Mathf.Infinity))
				{
					if(hit.transform.tag == "Ground")
					{
						Vector3 pos = Map.GetClosestTile(hit.point).Center;
						pos = new Vector3(pos.x,2f,pos.z);
						Network.Instantiate(ppf,pos,Quaternion.Euler(0,0,0),0);
						timer = -1f;
					}
				}
			}
			timer=Time.time;
		}
	}

	void OnGUI()
	{
		if(GUI.Button(new Rect(170,10,150,80),"Cubes"))
		{
			cubes ^= true;
		}
		if(Network.isServer)
		{
			if(GUI.Button(new Rect(340,10,150,80),"SPAWNERS"))
			{
				if(Respawns.Count>0)
				{
					foreach (GameObject oj in Respawns)
					{
						oj.SetActive(oj.activeSelf^true);
					}
					Respawns.Clear();
				}
				else
				{
					foreach (GameObject ob in GameObject.FindGameObjectsWithTag("Respawn"))
					{
						ob.SetActive(ob.activeSelf^true);
						Respawns.Add (ob);
					}
				}
			}
		}
	}
	
	public void AddPathfindingThread(GetPathThread thread)
	{
		//Add the thread to the list and start it
		m_PathFindingThreads.Add (thread);

		thread.Start ();
	}
	
	void OnDestroy()
	{
		//Threads could be running when we quit, make sure to abort them
		foreach (GetPathThread thread in m_PathFindingThreads)
		{
			thread.Abort ();
		}
	}
}
