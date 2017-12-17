using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RTSObject))]
public class EnemyAI : RTSObject
{
	//Enemy constants
	public float speed = 10.0f;
	public float gravity = -4.0f;
	public float HP = 100f;
	public float DAMAGE = 10f;
	private float damage_timer;

	//Control
	private RaycastHit hit;
	private Rigidbody rig;
	public LayerMask layer;
	private Transform target;
	public float radius = 20f;
	private ParticleSystem Part;

	//Path finding
	
	private float wall_delay;
	private Vector3 last_move;
	
	public RTSObject self;
	
	private bool Request = false;

	
	private List<Vector3> m_Path;
	private object m_PathLock = new object();
	private object m_PathChangedLock = new object();
	private bool m_PathChanged = false;
	
	//This variable needs to be locked as it can be accessed from multiple threads
	protected List<Vector3> Path
	{
		get
		{
			List<Vector3> tempValue;
			lock (m_PathLock)
			{
				tempValue = m_Path;
			}
			return tempValue;
		}
		set
		{
			lock (m_PathLock)
			{
				m_Path = value;
			}
			
			//Set path changed to true, this is so the UI thread can pick up a change and carry on execution
			PathChanged = true;
		}
	}
	
	//This variable needs to be locked as it can be accessed from multiple threads
	private bool PathChanged
	{
		get
		{
			bool tempValue;
			lock (m_PathChangedLock)
			{
				tempValue = m_PathChanged;
			}
			return tempValue;
		}
		set
		{
			lock (m_PathChangedLock)
			{
				m_PathChanged = value;
			}
		}
	}

	//Animation
	private Animation anim;
	enum CharS
	{
		Forward = 0,
		Back = 1,
		Left = 2,
		Right = 3,
		Idle = 4,
		Attack = 5,
	}
	private CharS charS;
	private List<string> CHAR = new List<string>();


	void Start()
	{
		//Инициализация
		anim = GetComponent<Animation>();
		rig = GetComponent<Rigidbody>();
		Part = GetComponentInChildren<ParticleSystem>();
		Part.Stop();
		foreach(AnimationState clip in anim)
		{
			CHAR.Add(clip.name);
		}

		damage_timer = Time.time;



		target = transform;

		//PATHFINDING
		self = GetComponent<RTSObject>();
		UpdateCurrentTile();

		charS = CharS.Idle;
	}
	
	void Update()
	{
		if(HP<=0)
		{
			Network.Destroy(GetComponent<NetworkView>().viewID);
			Network.RemoveRPCs(GetComponent<NetworkView>().viewID);
		}
		
		if(target == transform)
		{
			float dist = 100000000.0f;
			foreach(GameObject go in GameObject.FindGameObjectsWithTag("Player"))
			{
				if(go.GetComponent<PlayerVars>().HP>0)
				{
					if((go.transform.position-transform.position).magnitude<dist)
					{
						target = go.transform;
						dist = (go.transform.position-transform.position).magnitude;
						ACell = Map.GetClosestTile(target.position);
						CurrentCell = Map.GetClosestTile(transform.position);
						FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, target.position, Const.BLOCKINGLEVEL_Normal));
					}
				}
			}
	   	}
		else
		{
			if(!target)
			{
				target = transform;
			}
			else
			{
				if((target.position - transform.position).magnitude < radius)
				{
					Quaternion rot = Quaternion.LookRotation(new Vector3((target.position - transform.position).x,0f,(target.position - transform.position).z));
					transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
					
					if(Time.time - damage_timer >= 2f) damage_timer = Time.time;

					float DMG = Time.time - damage_timer;
					if(Time.time - damage_timer >=1f)
					{
						target.GetComponent<PlayerVars>().HP-=DAMAGE*DMG;
						if(Part.isStopped) Part.Play();
						damage_timer = Time.time;
					}
					
					charS = CharS.Attack;

					UpdateCurrentTile();
					
					//if(CurrentCell.Occupied) CurrentCell.NoLongerOccupied(self);
				}
				//Если мы не в точке назначения
				else if (Path != null && Path.Count > 0)
				{
					//Двигаемся согласно пути
					MoveForward();
					if(Part.isPlaying) Part.Stop();
					
					//Обновить текущий тайл
					UpdateCurrentTile();
				}

				if((ACell != null)&&(ACell != Map.GetClosestTile(target.position)))
				{
					ACell = Map.GetClosestTile(target.position);
					CurrentCell = Map.GetClosestTile(transform.position);
					FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, target.position, Const.BLOCKINGLEVEL_Normal));
				}

				if(target.GetComponent<PlayerVars>().HP<=0)
				{
					target = transform;
					
					charS = CharS.Idle;
				}
			}
		}

		//Контроллер анимаций
		if(anim)
		{
			anim.CrossFade(CHAR[(int)charS]);
		}
	}


	private void UpdateCurrentTile()
	{
		//What sort of tile are we on
		Cell currentTile = CurrentCell;
		CurrentCell = Map.GetClosestTile(transform.position);
		
		if (currentTile != null && currentTile != CurrentCell)
		{
			//We've changed tiles, make sure the old tile is no longer occupied
			currentTile.NoLongerOccupied(self);
			if(!CurrentCell.Occupied)
			{

				//Since we've changed, our next tile should equal the target tile
				if (CurrentCell != TCell)
				{
					//If it doesn't equal then we've drifted off the tiles slightly, set the current tile the target tile
					CurrentCell = TCell;
				}
				
				
				if(Path != null)
				{
					if(Path.Count == 1)
					{
						CurrentCell.SetOccupied(self, true);
					}
					else
					{
						CurrentCell.SetOccupied(self, false);
					}
				}
			}
		}
	}

	private void MoveForward()
	{
		if (TCell != null)
		{
			if (!TCell.Occupied || TCell.OccupiedBy == self)
			{				
				//КОСТЫЛЬ!
				Vector3 temp_move = new Vector3((Path[0]-transform.position).x,0,(Path[0]-transform.position).z).normalized;
				
				if(Physics.SphereCast(transform.position, 1.2f, transform.forward, out hit, speed/4,layer))
				{
					temp_move+=hit.normal;
					last_move = temp_move;
					wall_delay = Time.time;
				}
				
				
				if(Time.time-wall_delay < 0.25f)
				{
					temp_move = last_move;
				}
				//КОНЕЦ КОСТЫЛЯ!
				
				//Поворот модели в сторону вектора передвижения
				Quaternion rot = Quaternion.LookRotation(temp_move);
				transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
				
				//rig.AddForce(temp_move*speed);
				charS = CharS.Forward;
				
				
				//Назначаем анимацию и вектор скорости
				rig.velocity = new Vector3(transform.forward.x*speed,gravity,transform.forward.z*speed);

				if (Vector3.Distance (new Vector3(transform.position.x,0f,transform.position.z), Path[0]) < 1f)
				{
					Path.RemoveAt (0);
					if (Path.Count > 0)
					{
						TCell = Map.GetClosestTile (Path[0]);
					}
					else
					{
						TCell = null;
						Path = null;
						
						rig.velocity = Vector3.zero;
						charS = CharS.Idle;
					}
				}
			}
			

			else if (TCell.OccupiedStatic)
			{
				//We're occupied static, lets find a path without static obstacles
				if (!Request)
				{
					Request = true;
					FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, target.position, Const.BLOCKINGLEVEL_OccupiedStatic, ThreadCallBack));
				}			
			}
			else if (TCell.Occupied)
			{
				//We want to wait, unless the unit on the occupied tile is waiting for this tile
				if (!Request) //&& Map.GetClosestTile(TCell.OccupiedBy.transform.position) == CurrentCell)
				{
					//Set target tile to null, this will stop the other unit from also trtying to find a dynamic path, but it will get updated once the thread finishes
					//TCell = null;
					Request = true;
					FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, target.position, Const.BLOCKINGLEVEL_Occupied, ThreadCallBack));
				}
			}
		}
	}




	void OnDestroy()
	{
		CurrentCell.NoLongerOccupied(self);
	}


	public override void SetPath(List<Vector3> path)
	{
		Path = path;
		/*Визуализация пути
		foreach(GameObject go in FindObjectsOfType<GameObject>()) 
		{
			if(go.name == "Capsule") Destroy(go);
		}
		for(int k=0;k<Path.Count;k++)
		{
			GameObject og = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			og.transform.position = Path[k];
			og.transform.localScale = new Vector3(1,1,1);
			og.GetComponent<Renderer>().material = new Material(Shader.Find("Specular"));
			og.GetComponent<Renderer>().material.color = new Color(128f,0f,0f);
			og.GetComponent<Collider>().enabled = false;
		}*/
		TCell = Map.GetClosestTile(path[0]);
	}
	
	private void ThreadCallBack()
	{
		Request = false;
	}
}