using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class GetPathThread : ThreadedJob 
{
	private Cell m_CurrentTile;
	private Vector3 m_TargetTile;
	private int m_BlockingLevel;
	
	private RTSObject m_ObjectCalling;
	
	private List<Vector3> m_Result;
	
	private static object m_Lock = new object();
	
	public delegate void CallBackDelegate();
	private CallBackDelegate m_CallBackFunction;
	
	public GetPathThread(RTSObject objectCalling, Cell currentTile, Vector3 targetTile, int blockingLevel, CallBackDelegate callBackFunction = null)
	{
		m_ObjectCalling = objectCalling;
		m_CurrentTile = currentTile;
		m_TargetTile = targetTile;
		m_BlockingLevel = blockingLevel;
		m_CallBackFunction = callBackFunction;
	}
	
	protected override void ThreadFunction ()
	{
		//Lock the threaded function, this forces the threads to wait for eachother so they don't try to alter the grid at the same time
		lock (m_Lock)
		{
			A_Star aStar = new A_Star();
			m_Result = aStar.FindVectorPath (m_CurrentTile, m_TargetTile, m_BlockingLevel);
		}
	}

	protected override void OnFinished ()
	{
		//Once we're finished give the calling object it's path
		if(m_ObjectCalling != null)
		{
			m_ObjectCalling.GetComponent<RTSObject>().SetPath(m_Result);
			
			if (m_CallBackFunction != null)
			{
				m_CallBackFunction();
			}
		}
	}
}