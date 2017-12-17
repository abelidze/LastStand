using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RTSObject))]
public class Controller: RTSObject
{
	//Player constants
	public float speed = 10.0f;
	public float gravity = -4.0f;

	//Control
	private RaycastHit hit;
	public LayerMask layer;
	private Rigidbody rig;
	private PlayerVars VARS;
	public Transform target;
	private float damage_timer;


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
		Idle = 0,
		Run = 1,
		Attack = 2,
		Death = 3,
	}
	private CharS charS;

	//Interpolation
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private float HPP = 100f;
	private Vector3 syncStartPosition = new Vector3(50f,50f,50f);
	private Vector3 syncEndPosition = new Vector3(50f,50f,50f);
	private int ch;
	private CharS chr = CharS.Idle;
	private Quaternion rtIN;
	private Quaternion rtOUT;


	void Start()
	{
		//Инициализация
		anim = GetComponent<Animation>();
		rig = GetComponent<Rigidbody>();
		VARS = GetComponent<PlayerVars>();

		rig.isKinematic = !GetComponent<NetworkView>().isMine;

		//Дабы не случилось форсмажоров
		transform.position = new Vector3(transform.position.x,51.4f,transform.position.z);

		Physics.IgnoreLayerCollision(10, 10, true);
		Physics.IgnoreLayerCollision(10, 11, true);

		charS = CharS.Idle;

		damage_timer = Time.time;

		//PATHFINDING
		self = GetComponent<RTSObject>();
		UpdateCurrentTile();
		
		DontDestroyOnLoad(this);
	}

	void Update()
	{
		//Если объект создан на данном компьютере
		if(GetComponent<NetworkView>().isMine)
		{
			LayerMask lrr = (1 << 12);
			if(!Physics.Raycast(transform.position,-transform.up,10f,lrr))
			{
				if(VARS.HP>0)
				{
					//Пускаем луч в землю, находим точку передвижения
					#if UNITY_ANDROID
					Touch[] touches = Input.touches;
					
					if(touches.Length == 1)
					if(touches[0].phase == TouchPhase.Began)
					#else
					if(Input.GetMouseButtonDown(1))
					#endif
					{
						Ray rc = Camera.main.ScreenPointToRay(Input.mousePosition);
						if (Physics.Raycast(rc, out hit, Mathf.Infinity))
						{
							if(hit.transform.tag == "Ground")
							{
								target = null;
								ACell = Map.GetClosestTile(hit.point);
								CurrentCell = Map.GetClosestTile(transform.position);
								FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, hit.point, Const.BLOCKINGLEVEL_Normal));
								Instantiate(Resources.Load("PointClick",typeof(GameObject)),hit.point,Quaternion.Euler(90,0,0));
							}
							if((hit.transform.tag == "Player")||(hit.transform.tag == "Enemy"))
							{
								target = hit.transform;
								if(target == transform)
								{
									transform.rotation = Quaternion.LookRotation((hit.point - transform.position).normalized);
									transform.rotation = Quaternion.Euler(0,transform.rotation.eulerAngles.y,0);
									target = null;
									TCell = null;
									Path = null;
									
									rig.velocity = Vector3.zero;
									charS = CharS.Idle;
								}
								else
								{
									ACell = Map.GetClosestTile(target.position);
									CurrentCell = Map.GetClosestTile(transform.position);
									FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, target.position, Const.BLOCKINGLEVEL_Normal));
								}
							}
						}
					}
					
					//Если мы не в точке назначения
					if (Path != null && Path.Count > 0)
					{
						//Двигаемся согласно пути
						MoveForward();
						
						//Обновить текущий тайл
						UpdateCurrentTile();
					}


					if(target!=null)
					{
						if((target.position - transform.position).magnitude < VARS.Range)
						{
							TCell = null;
							Path = null;
							
							charS = CharS.Attack;
							//Поворот модели в сторону противника
							Quaternion rot = Quaternion.LookRotation((target.position - transform.position).normalized);
							transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
							transform.rotation = Quaternion.Euler(0,transform.rotation.eulerAngles.y,0);

							if(Time.time - damage_timer >= VARS.AttackSpeed+1f) damage_timer = Time.time;

							if(target.tag == "Player")
							{
								if(target.GetComponent<PlayerVars>().HP<=0) target = null;
								else if(Time.time - damage_timer >= VARS.AttackSpeed) damage_timer = Time.time;
							}
							if(target.tag == "Enemy")
							{
								if(target.GetComponent<EnemyAI>().HP>0)
								{
									float DMG = Time.time - damage_timer;
									if(DMG >= VARS.AttackSpeed)
									{
										target.GetComponent<EnemyAI>().HP-=VARS.Damage*DMG;
										damage_timer = Time.time;
									}
								}
								else target = null;
							}
						}
						else if((ACell != null)&&(ACell != Map.GetClosestTile(target.position)))
						{
							ACell = Map.GetClosestTile(target.position);
							FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, target.position, Const.BLOCKINGLEVEL_OccupiedStatic));
						}
					}
					else if(charS == CharS.Attack) charS = CharS.Idle;
				}
				//Смерть
				else
				{
					charS = CharS.Death;
					TCell = null;
					Path = null;
					rig.velocity = Vector3.zero;

					GameObject.FindGameObjectWithTag("GameController").GetComponent<Connection>().Respawn = GetComponent<Controller>();
				}
			}


			if(Input.GetKeyUp("z")) VARS.HP = 0;

		}
		//Если это клон других игроков, то назначаем принятые переменные в методе OnSerializeNetworkView
		else
		{
			syncTime += Time.deltaTime;
			rig.position = Vector3.Slerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);

			Cell curcell = Map.GetClosestTile(transform.position);
			if(CurrentCell != curcell)
			{
				CurrentCell.NoLongerOccupied(self);

				CurrentCell = curcell;
				CurrentCell.SetOccupied(self,false);
			}

			if(chr == CharS.Idle || chr == CharS.Attack)
			{
				if(charS == CharS.Death) VARS.HP = VARS.HPMAX;
				CurrentCell.SetOccupied(self,true);
			}


			charS = chr;
			VARS.HP = HPP;
			transform.rotation = rtOUT;
			if(charS == CharS.Attack)
			{
				if(Time.time - damage_timer >= VARS.AttackSpeed+1f) damage_timer = Time.time;

				Vector3 pos = new Vector3(transform.position.x,1.6f,transform.position.z);
				if(Physics.Raycast(pos, transform.forward, out hit, speed/4))
				{
					float DMG = Time.time - damage_timer;
					if(DMG >= VARS.AttackSpeed)
					{
						if(hit.transform.tag == "Player")
						{
							hit.transform.GetComponent<PlayerVars>().HP-=VARS.Damage*DMG;
							damage_timer = Time.time;
						}

						if(hit.transform.tag == "Enemy")
						{
							hit.transform.GetComponent<EnemyAI>().HP-=VARS.Damage*DMG;
							damage_timer = Time.time;
						}
					}
				}
			}
		}
		//Контроллер анимаций
		if(anim)
		{
			switch(charS)
			{
				case CharS.Idle:
				anim.CrossFade("idle");
				break;
				
				case CharS.Run:
				anim.CrossFade("run");
				break;
				
				case CharS.Attack:
				anim.CrossFade("attack");
				if(CurrentCell.Occupied) CurrentCell.NoLongerOccupied(self);
				break;
				
				case CharS.Death:
				anim.CrossFade("death");
				break;
			}
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
			if((!TCell.Occupied || TCell.OccupiedBy == self) || Path.Count <= 1)
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
				transform.rotation = Quaternion.Euler(0,transform.rotation.eulerAngles.y,0);
				
				//rig.AddForce(temp_move*speed);
				//Назначаем анимацию и вектор скорости
				rig.velocity = new Vector3(transform.forward.x*speed,gravity,transform.forward.z*speed);
				charS = CharS.Run;
				
				if (Vector3.Distance (transform.position, Path[0]) < speed/8)
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
					Debug.Log ("STATIC");
					//rig.velocity = Vector3.zero;
					FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, ACell.Center, Const.BLOCKINGLEVEL_OccupiedStatic, ThreadCallBack));
				}			
			}
			else if (TCell.Occupied)
			{
				//We want to wait, unless the unit on the occupied tile is waiting for this tile
				if (!Request) //&& Map.GetClosestTile(TCell.OccupiedBy.transform.position) == CurrentCell
				{
					//Set target tile to null, this will stop the other unit from also trtying to find a dynamic path, but it will get updated once the thread finishes
					TCell = null;
					Request = true;
					Debug.Log ("DYNAMIC");

					FindObjectOfType<ThreadManager>().AddPathfindingThread (new GetPathThread(self, CurrentCell, ACell.Center, Const.BLOCKINGLEVEL_Occupied, ThreadCallBack));
				}
			}
		}
	}
	
	public override void SetPath(List<Vector3> path)
	{
		Path = path;
		/*Визуализация пути
		foreach(GameObject go in FindObjectsOfType<GameObject>()) 
		{
			if(go.name == "Sphere") Destroy(go);
		}
		for(int k=0;k<Path.Count;k++)
		{
			GameObject og = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			og.transform.position = Path[k];
			og.transform.localScale = new Vector3(1,1,1);
			og.GetComponent<Renderer>().material = new Material(Shader.Find("Specular"));
			og.GetComponent<Renderer>().material.color = new Color(0f,0f,128f);
			og.GetComponent<SphereCollider>().enabled = false;
		}*/
		TCell = Map.GetClosestTile(path[0]);
	}
	
	private void ThreadCallBack()
	{
		Request = false;
	}

	public void Respawn()
	{
		VARS.HP = VARS.HPMAX;
		charS = CharS.Idle;
		TCell = Map.GetClosestTile(transform.position);
		UpdateCurrentTile();
	}
	
	
	
	
	//Обмен значениями данного скрипта
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		Vector3 syncPosition = Vector3.zero;
		Vector3 syncVelocity = Vector3.zero;
		int ch = (int)charS;
		float hp = VARS.HP;
		Quaternion rtIN = transform.rotation;
		if (stream.isWriting)
		{
			syncPosition = rig.position;
			stream.Serialize(ref syncPosition);
			
			syncVelocity = rig.velocity;
			stream.Serialize(ref syncVelocity);

			stream.Serialize(ref rtIN);

			stream.Serialize(ref ch);
			stream.Serialize(ref hp);
		}
		else
		{
			stream.Serialize(ref syncPosition);
			stream.Serialize(ref syncVelocity);
			stream.Serialize(ref rtIN);
			stream.Serialize(ref ch);
			stream.Serialize(ref hp);

			chr = (CharS)ch;
			HPP = hp;
			rtOUT = rtIN;

			//Интерполяция передвижения
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
			
			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = rig.position;
		}
	}
}