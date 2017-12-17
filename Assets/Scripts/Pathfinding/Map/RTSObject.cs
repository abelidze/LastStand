using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class RTSObject : MonoBehaviour
{
	protected Cell CurrentCell;
	protected Cell TCell;
	protected Cell ACell;

	/*
	public string Name
	{
		get;
		private set;
	}
	
	public int ID
	{
		get;
		private set;
	}

	public int TeamIdentifier
	{
		get;
		private set;
	}
	
	private float m_Health;
	private float m_MaxHealth;

	public abstract void SetSelected();
	public abstract void SetDeselected();
	public abstract void AssignToGroup(int groupNumber);
	public abstract void RemoveFromGroup();
	public abstract void ChangeTeams(int team);

	public float GetHealthRatio()
	{
		return m_Health/m_MaxHealth;
	}
			
	public void TakeDamage(float damage)
	{
		m_Health -= damage;
	}
	*/
	
	public abstract void SetPath(List<Vector3> path);
}