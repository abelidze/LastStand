using UnityEngine;
using System.Collections;
using System.Threading;

public abstract class ThreadedJob
{
	private bool m_IsDone = false;
	private object m_Lock = new object();
	private Thread m_Thread = null;
	
	public bool IsDone
	{
		get
		{
			bool tmp;
			lock (m_Lock)
			{
				tmp = m_IsDone;
			}
			return tmp;
		}
		set
		{
			lock (m_Lock)
			{
				m_IsDone = value;
			}
		}
	}
	
	public virtual void Start()
	{
		m_Thread = new Thread(Run);
		m_Thread.Start();
	}
	
	public virtual void Abort()
	{
		if (m_Thread != null)
		{
			m_Thread.Abort();
		}
	}
	
	protected abstract void ThreadFunction();
	
	protected abstract void OnFinished();
	
	public virtual bool Update()
	{
		if (IsDone)
		{
			OnFinished();
			return true;
		}
		
		return false;
	}
	
	private void Run()
	{
		ThreadFunction();
		IsDone = true;
	}
}