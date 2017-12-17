using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Connection : MonoBehaviour
{
	public HostData remoteIP;
	public string masterIP;
	public int remotePort = 12564;
	public Controller Respawn;
	public List<Transform> Platforms;

	public int InvWidth = 8;
	public int InvHeight = 4;

	public Texture loading;
	public Texture LOGO;
	
	public class PlayerInfo
	{
		public int id = 0;
		public string username = "noname";
		public NetworkPlayer player;
	}
	
	public float GameStart = 0f;

	private bool enter_name = false;
	private string player_name = "";
	private int player_id = 0;
	private List<PlayerInfo> playerList = new List<PlayerInfo>();
	private List<bool> PlayerIDs = new List<bool>();
	private PlayerInfo delete;

	private int window;
	private int finding;
	private float angle;
	private float timer;
	private float waiting;
	private HostData[] hostData;
	private bool readyPlay = false;
	private int readyPlayers = 0;
	private List<HostData> badS = new List<HostData>();

	private bool reconnect = false;

	//Console
	struct Log
	{
		public string message;
		public string stackTrace;
		public LogType type;
	}

	static readonly Dictionary<LogType, Color> logTypeColors = new Dictionary<LogType, Color>()
	{
		{ LogType.Assert, Color.white },
		{ LogType.Error, Color.red },
		{ LogType.Exception, Color.red },
		{ LogType.Log, Color.white },
		{ LogType.Warning, Color.yellow },
	};

	private List<Log> logs = new List<Log>();
	private bool enableConsole = false;
	private string command = "";
	private Vector2 scrollPosition;
	private bool collapse;

	GUIContent clearLabel = new GUIContent("Clear", "Clear the contents of the console.");
	GUIContent collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");

	private float serverTime;

	void Start()
	{
		DontDestroyOnLoad(this);;
	}

	void Awake()
	{
		//Запрос имени игрока из реестра
		player_name = PlayerPrefs.GetString("player_name", "");
		if(!PlayerPrefs.HasKey("player_name")) enter_name = true;
		if(PlayerPrefs.HasKey("server_ip")) reconnect = true;
		
		masterIP = PlayerPrefs.GetString("master_server","127.0.0.1");
		MasterServer.ipAddress = masterIP;
		Network.natFacilitatorIP = masterIP;
		MasterServer.port = 23466;
		Network.natFacilitatorPort = 50005;
	}

	void ConsoleLog(string message, string stackTrace, LogType type)
	{
		logs.Add(new Log() {
			message = message,
			stackTrace = stackTrace,
			type = type,
		});
	}

	public int GetWindow()
	{
		return window;
	}
	
	public int GetFinding()
	{
		return finding;
	}

	void GetName(int id)
	{
		//Окно ввода имени
		GUILayout.BeginVertical();
		GUILayout.Space(10);
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.Label("Enter your name:");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		player_name = GUILayout.TextField(player_name,15);
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		
		GUILayout.Space(80);

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		if(player_name.Length>=1)
		{
			if((GUILayout.Button("Save"))&&(enter_name))
			{
				enter_name = false;
				PlayerPrefs.SetString("player_name", player_name);
			}
		}
		else GUILayout.Label("Enter a name to continue...");
		
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		
		GUILayout.EndVertical();
	}

	void ShowProfile(int id)
	{
		//Профиль
		GUILayout.BeginVertical();
		GUILayout.Space(10);
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.Label("Player name: ");
		player_name = GUILayout.TextField(player_name,15);
		GUILayout.Space(10);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.Label("Player class: ");
		GUILayout.Label("Corsair");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.Label("Rating: ");
		GUILayout.Label("4k");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.Label("Winrate: ");
		GUILayout.Label("75%");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.Label("Likes: ");
		GUILayout.Label("8");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();

		GUILayout.Space(10);
		
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		if(player_name.Length>=1)
		{
			if(GUILayout.Button("Save Profile"))
			{
				PlayerPrefs.SetString("player_name", player_name);
			}
		}
		else GUILayout.Label("Enter a name to continue...");
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		
		GUILayout.EndVertical();
	}

	void OnEnable ()
	{
		Application.logMessageReceived += ConsoleLog;
	}
	
	void OnDisable ()
	{
		Application.logMessageReceived -= ConsoleLog;
	}

	//Консоль
	void ConsoleWindow(int id)
	{
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		
		// Iterate through the recorded logs.
		for (int i = 0; i < logs.Count; i++)
		{
			Log log = logs[i];
			
			// Combine identical messages if collapse option is chosen.
			if(collapse)
			{
				bool messageSameAsPrevious = i > 0 && log.message == logs[i - 1].message;
				
				if(messageSameAsPrevious) continue;
			}
			
			GUI.contentColor = logTypeColors[log.type];
			GUILayout.Label(log.message);
		}
		
		GUILayout.EndScrollView();
		
		GUI.contentColor = Color.white;

		//Ввод
		GUILayout.BeginHorizontal();
		command = GUILayout.TextField(command);
		GUILayout.EndHorizontal();

		//Управление
		GUILayout.BeginHorizontal();
		
		if(GUILayout.Button(clearLabel)) logs.Clear();
		
		collapse = GUILayout.Toggle(collapse, collapseLabel, GUILayout.ExpandWidth(false));
		
		GUILayout.EndHorizontal();

		//GUI.DragWindow(titleBarRect);
	}

	
	void Update()
	{
		if(Input.GetKeyUp(KeyCode.BackQuote)) enableConsole ^= true;
		if((enableConsole)&&(command!=""))
		if(Input.GetKeyUp(KeyCode.Return))
		{
			Debug.Log(command);
			if(command[0] == '/')
			{
				command = command.Remove(0,1);
				string[] args = command.Split(new char[] {' '}, System.StringSplitOptions.RemoveEmptyEntries);
				switch(args[0])
				{
					case "startserver":
					if(Network.peerType == NetworkPeerType.Disconnected)
					{
						Debug.Log ("Start Server Console");
						if((args.Length>1)&&(args[1] != null)) remotePort = int.Parse(args[1]);
						Network.InitializeServer(3, remotePort, !(Network.HavePublicAddress()));

						serverTime = Time.time;
						window = 5;
					}
					else Debug.Log ("Failed. Already have a connection");
					break;

					case "connect":
					if(Network.peerType == NetworkPeerType.Disconnected)
					{
						if((args.Length>1)&&(args[1]!=null))
						{
							if((args.Length>2)&&(args[2] != null)) remotePort = int.Parse(args[2]);

							NetworkConnectionError conn = Network.Connect(args[1],remotePort);
							if(conn == NetworkConnectionError.NoError)
							{
								Debug.Log("Connecting...");
								window = 5;
								playerList = new List<PlayerInfo>();
							}
							else Debug.Log("Connection failed");
						}
						else Debug.Log("Invalid IP");
					}
					else Debug.Log ("Failed. Already have a connection");
					break;

					case "debug":
					Debug.Log (window);
					Debug.Log (finding);
					Debug.Log (waiting);
					break;

					default:
					Debug.Log ("Unknown command");
					break;
				}
			}

			command = "";
		}

		if((finding<2)&&(GameStart>0f))
		{
			GameStart+=Time.deltaTime;
			if(GameStart>=3f)
			{
				foreach(PlayerInfo pl in playerList)
				{
					if(Platforms[pl.id].GetComponent<Collider>().enabled)
					{
						Platforms[pl.id].GetComponent<Collider>().enabled = false;
						Platforms[pl.id].GetComponent<Rigidbody>().velocity = new Vector3(0f,-20f,0f);
					}
				}
			}
			if(GameStart>=6f)
			{
				finding = 2;
				GameStart = 0f;
				Application.LoadLevel(1);
			}
		}
	}

	void OnGUI()
	{
		if(enableConsole)
		{
			GUILayout.Window(123,new Rect(0, 0, Screen.width, Screen.height*0.5f),ConsoleWindow, "CONSOLE");
			GUI.enabled = false;
		}
		else GUI.enabled = true;
		{
			//Кнопки
			if(finding<2)
			{
				if(GUI.Button(new Rect(Screen.width*0.375f,0,Screen.width/4,Screen.height/6),"PLAY"))
				{
					waiting = 0f;
					remotePort = 12564;
					GameStart = 0f;
					if(window == 3) window = 0;
					else
					{
						timer = Time.time;
						window = 3;
						remoteIP = new HostData();
						hostData = null;
						MasterServer.ClearHostList();
						playerList = new List<PlayerInfo>();
						badS = new List<HostData>();
					}
					if(Network.peerType != NetworkPeerType.Disconnected)
					{
						Network.Disconnect();
						if(Network.isServer)
						{
							MasterServer.UnregisterHost();
							if(finding == 1)
							{
								foreach(GameObject go in FindObjectsOfType<GameObject>()) 
								{
									if(go.tag == "LobbyPlayer") DestroyObject(go);
								}
								Debug.Log ("Destroyed");
							}
						}
					}
					else finding = 0;
				}
				
				if(GUI.Button(new Rect(Screen.width/8,0,Screen.width/4,Screen.height/7),"EQUIPMENT"))
				{
					GameStart = 0f;
					if(window == 2) window = 0;
					else window = 2;
					if(Network.peerType != NetworkPeerType.Disconnected)
					{
						Network.Disconnect();
						if(Network.isServer)
						{
							MasterServer.UnregisterHost();
							if(finding == 1)
							{
								foreach(GameObject go in FindObjectsOfType<GameObject>()) 
								{
									if(go.tag == "LobbyPlayer") DestroyObject(go);
								}
								Debug.Log ("Destroyed");
							}
						}
					}
					else finding = 0;
				}
				
				if(GUI.Button(new Rect(0,0,Screen.width/8,Screen.height/8),"PROFILE"))
				{
					GameStart = 0f;
					if(window == 1) window = 0;
					else window = 1;
					if(Network.peerType != NetworkPeerType.Disconnected)
					{
						Network.Disconnect();
						if(Network.isServer)
						{
							MasterServer.UnregisterHost();
							if(finding == 1)
							{
								foreach(GameObject go in FindObjectsOfType<GameObject>()) 
								{
									if(go.tag == "LobbyPlayer") DestroyObject(go);
								}
								Debug.Log ("Destroyed");
							}
						}
					}
					else finding = 0;
				}
				
				if(GUI.Button(new Rect(Screen.width*0.625f,0,Screen.width/4,Screen.height/7),"OPTIONS"))
				{
					GameStart = 0f;
					if(window == 4) window = 0;
					else window = 4;
					if(Network.peerType != NetworkPeerType.Disconnected)
					{
						Network.Disconnect();
						if(Network.isServer)
						{
							MasterServer.UnregisterHost();
							if(finding == 1)
							{
								foreach(GameObject go in FindObjectsOfType<GameObject>()) 
								{
									if(go.tag == "LobbyPlayer") DestroyObject(go);
								}
								Debug.Log ("Destroyed");
							}
						}
					}
					else finding = 0;
				}
				
				if(GUI.Button(new Rect(Screen.width*0.875f,0,Screen.width/8,Screen.height/8),"QUIT"))
				{
					if(Network.peerType != NetworkPeerType.Disconnected)
					{
						Network.Disconnect();
						if(Network.isServer) MasterServer.UnregisterHost();
					}
					Application.Quit();
				}
			}

			//Окна
			switch(window)
			{
				case 0:
				if(enter_name) GUILayout.Window(9,new Rect(Screen.width/2,Screen.height/2-100,300,200),GetName, "Player Name");
				else GUI.DrawTexture(new Rect(Screen.width/2f,Screen.height/4f,Screen.height*0.75f,Screen.height*0.5f),LOGO);
				if(reconnect)
				{
					if(Network.peerType != NetworkPeerType.Connecting)
					{
						if(GUI.Button(new Rect(Screen.width*0.4f,Screen.height*0.8f,Screen.width*0.2f,Screen.height*0.1f),"RECONNECT"))
						{
							remoteIP = new HostData();
							remoteIP.gameName = player_name+"Rec";
							remoteIP.gameType = "Normal";
							remoteIP.ip = PlayerPrefs.GetString("server_ip","127.0.0.1").Split(new char[] {' '}, System.StringSplitOptions.RemoveEmptyEntries);
							remoteIP.port = PlayerPrefs.GetInt("server_port",12564);
							remoteIP.guid = PlayerPrefs.GetString("server_guid");
							remoteIP.useNat = (PlayerPrefs.HasKey("server_nat"));
							remoteIP.playerLimit = 4;
							
							finding = 2;
							window = 3;
							Application.LoadLevel(1);
						}
						if(GUI.Button(new Rect(Screen.width*0.6f+16f,Screen.height*0.8f,Screen.height*0.1f,Screen.height*0.1f),"X"))
						{
							if(PlayerPrefs.HasKey("server_ip"))
							{
								PlayerPrefs.DeleteKey("server_ip");
								PlayerPrefs.DeleteKey("server_port");
								PlayerPrefs.DeleteKey("server_guid");
								if(PlayerPrefs.HasKey("server_nat")) PlayerPrefs.DeleteKey("server_nat");
							}
							reconnect = false;
						}
					}
				}
				break;
				
				case 1:
				GUILayout.Window(9,new Rect(Screen.width/2,Screen.height/4f,Screen.height*0.75f,Screen.height*0.5f),ShowProfile, "Player profile");
				break;
				
				case 2:
				GUI.BeginGroup(new Rect(Screen.width/2, Screen.height/5, Screen.width/2.5f, Screen.height*0.75f));
				GUI.Box(new Rect(0, 0, Screen.width/2.5f, Screen.height*0.75f), "EQUIPMENT");
				
				float InvW = (Screen.width/2.5f)/(InvWidth)*0.75f;
				float InvH = (Screen.height*0.75f)/(InvHeight)*0.75f;
				float IW = InvW/3f;
				float IH = InvH/3f;;
				
				for(int j=0;j<InvHeight;j++)
				for(int i=0;i<InvWidth;i++)
				{
					GUI.Button(new Rect(IW/2+IW*i+i*InvW,IH/2+IH*j+j*InvH,InvW,InvH),"");
				}
				
				GUI.EndGroup();
				break;
				
				case 3:
				if(finding == 0)
				{
					GUI.BeginGroup(new Rect(Screen.width/2-Screen.width/2.5f, Screen.height/5, Screen.width/1.25f, Screen.height*0.75f));

					readyPlayers = 0;

					GUI.Box(new Rect(0, 0, Screen.width/1.25f, Screen.height*0.75f), "SEARCHING GAME");
					//Загрузка
					Rect mainRect = new Rect(Screen.width/2.5f-Screen.height/6, Screen.height/2.5f-Screen.height/6, Screen.height/3, Screen.height/3); //loading.width, loading.height);
					
					angle+=Time.deltaTime*120f;
					
					if(angle>360f) angle-=360f;
					
					Matrix4x4 iniMatrix = GUI.matrix;
					GUIUtility.RotateAroundPivot(angle, new Vector2(mainRect.x + mainRect.width / 2, mainRect.y + mainRect.height / 2));
					GUI.DrawTexture(mainRect, loading);
					GUI.matrix = iniMatrix;
					
					if((waiting>5)&&(!Network.isServer))
					{
						Debug.Log ("Start Server Wait");
						waiting = 0;
						serverTime = Time.time;
						Network.InitializeServer(3, remotePort, !(Network.HavePublicAddress()));
					}
					if(Time.time-timer >= 2f)
					{
						if((!Network.isClient)&&(Network.connections.Length<2))
						{
							RefreshList();

							timer = Time.time;
							if(!Network.isServer) waiting++;
							
							//Мастерсервер
							if(hostData!=null)
							{
								if(hostData.Length>0)
								{
									for(int i=0; i<hostData.Length; i++)
									{
										if(!badS.Contains(hostData[i]))
										if(hostData[i].connectedPlayers<hostData[i].playerLimit)
										{
											remoteIP = hostData[i];
											if(Network.isServer)
											{
												if((hostData[i].gameName != player_name)&&(float.Parse(hostData[i].comment)>serverTime))
												{
													Debug.Log ("Connect 2");
													waiting = 2;
													Network.Disconnect();
													MasterServer.UnregisterHost();
												}
												else
												{
													badS.Add(hostData[i]);
													continue;
												}
											}
											foreach(string ipp in remoteIP.ip)
												Debug.Log(ipp);
											NetworkConnectionError conn = Network.Connect(remoteIP);
											if(conn == NetworkConnectionError.NoError)
											{
												Debug.Log ("Connect 1");
												finding = 1;
												MasterServer.ClearHostList();
												break;
											}
											else badS.Add(hostData[i]);
										}
									}
									
									MasterServer.ClearHostList();
								}
							}
						}
					}
					GUI.EndGroup();
				}

				if(finding == 1)
				{
					GUI.BeginGroup(new Rect(Screen.width/2f-Screen.width/4f, Screen.height/2f, Screen.width/2f, Screen.height/4f));
					if(Network.isClient) GUI.Box(new Rect(0, 0, Screen.width/2f, Screen.height/4f), "Room " + remoteIP.gameName);
					else
					{
						GUI.Box(new Rect(0, 0, Screen.width/2f, Screen.height/4f), "Room " + player_name);
						if((readyPlayers == Network.connections.Length)&&(readyPlay)&&(GameStart<=0f))
						{
							GetComponent<NetworkView>().RPC("StartGame", RPCMode.Others);
							foreach(PlayerInfo pl in playerList)
							{
								Platforms[pl.id].GetComponent<Animation>().Play("Close");
							}
							window = 3;
							GameStart = 1f;
							//MasterServer.UnregisterHost();
						}
					}

					if(GUI.Toggle(new Rect(Screen.width/4f,Screen.height/16f*3f,Screen.width/8f,Screen.height/8f),readyPlay,"READY"))
					{
						if(!readyPlay)
						{
							GetComponent<NetworkView>().RPC("ReadyGame", RPCMode.Server,true);
							
							readyPlay = true;
						}
					}
					else if(readyPlay)
					{
						GetComponent<NetworkView>().RPC("ReadyGame", RPCMode.Server,false);
						
						readyPlay = false;
					}
					int i = 0;
					foreach(PlayerInfo player in playerList)
					{
						GUI.Label(new Rect(16f, Screen.height/32f+25f*i,100f,20f),player.id+". "+player.username);
						i+=1;
					}
					GUI.EndGroup();
				}

				if(finding == 2)
				{
					string ipaddress = Network.player.externalIP;
					string port = Network.player.externalPort.ToString();
					GUI.Label(new Rect(20,100,250,40),"IP Adress: "+ipaddress+":"+port);
					GUI.Label(new Rect(20,150,250,40),"Player ID: "+player_id+" - "+player_name);
					if(GUI.Button (new Rect(10,10,150,80),"Disconnect"))
					{
						// Отключение от Сервера
						if(Network.isServer) GetComponent<NetworkView>().RPC("ShutDown", RPCMode.Others);
						Network.Disconnect(200);
						Application.LoadLevel(0);

						if(PlayerPrefs.HasKey("server_ip"))
						{
							PlayerPrefs.DeleteKey("server_ip");
							PlayerPrefs.DeleteKey("server_port");
							PlayerPrefs.DeleteKey("server_guid");
							if(PlayerPrefs.HasKey("server_nat")) PlayerPrefs.DeleteKey("server_nat");
						}
						DestroyObject(gameObject);
						//Network.Destroy(pl);
					}
					if(Respawn != null)
					if(GUI.Button (new Rect(Screen.width/2-200,Screen.height/2-50,400,100),"Respawn")) 
					{
						Respawn.Respawn();
						Respawn = null;
					}
				}
				break;
				
				case 4:
				GUI.BeginGroup(new Rect(Screen.width/2, Screen.height/4f, Screen.height*0.75f,Screen.height*0.5f));
				GUI.Box(new Rect(0, 0, Screen.height*0.75f,Screen.height*0.5f), "Options");
				//Регулятор громкости
				GUI.Label(new Rect(5, 29, 50, 20), "Volume Level: ");
				AudioListener.volume = GUI.HorizontalSlider(new Rect(55, 35, 240, 20), AudioListener.volume, 0, 1);
				//Выбор качества картинки
				GUI.Label(new Rect(5, 70, 250, 20), "Graphic Level: ");
				if (GUI.Toggle(new Rect(10, 90, 100, 20), QualitySettings.GetQualityLevel() == 0, "Fastest"))
					QualitySettings.SetQualityLevel(0);
				if (GUI.Toggle(new Rect(10, 110, 100, 20), QualitySettings.GetQualityLevel() == 1, "Fast"))
					QualitySettings.SetQualityLevel(1);
				if (GUI.Toggle(new Rect(10, 130, 100, 20), QualitySettings.GetQualityLevel() == 2, "Simple"))
					QualitySettings.SetQualityLevel(2);
				if (GUI.Toggle(new Rect(10, 150, 100, 20), QualitySettings.GetQualityLevel() == 3, "Good"))
					QualitySettings.SetQualityLevel(3);
				if (GUI.Toggle(new Rect(10, 170, 100, 20), QualitySettings.GetQualityLevel() == 4, "Beautiful"))
					QualitySettings.SetQualityLevel(4);
				if (GUI.Toggle(new Rect(10, 190, 100, 20), QualitySettings.GetQualityLevel() == 5, "Fantastic"))
					QualitySettings.SetQualityLevel(5);
				
				GUI.Label(new Rect(10, 250, 70, 20),"MasterIP: ");
				masterIP = GUI.TextField(new Rect(80, 250, 110, 20),masterIP,15);

				if(masterIP != Network.natFacilitatorIP)
				{
					MasterServer.ipAddress = masterIP;
					Network.natFacilitatorIP = masterIP;
					PlayerPrefs.SetString("master_server",masterIP);
				}

				if(GUI.Button(new Rect(10, 280, 120, 30),"Reset all data"))
				{
					PlayerPrefs.DeleteAll();
					reconnect = false;
					enter_name = true;
				}
				
				//QualitySettings.antiAliasing = 8; //Значение может быть 0,2,4 или 8.
				
				/*
					QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable; Выключена
					QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable; Включена
					*/
				
				GUI.EndGroup();
				break;

				case 5:
				GUI.BeginGroup(new Rect(Screen.width/2-Screen.width/2.5f, Screen.height/5, Screen.width/1.25f, Screen.height*0.75f));
				if(Network.isClient) GUI.Box(new Rect(0, 0, Screen.width/1.25f, Screen.height*0.75f), "Room Client");
				else
				{
					GUI.Box(new Rect(0, 0, Screen.width/1.25f, Screen.height*0.75f), "Room Server");
					if((readyPlayers == Network.connections.Length)&&(readyPlay)&&(finding!=2))
					{
						GetComponent<NetworkView>().RPC("StartGame", RPCMode.Others);
						finding = 2;
						window = 3;
						Application.LoadLevel(1);
					}
				}
				
				if(GUI.Toggle(new Rect(Screen.width/2,Screen.height/3*2,Screen.width/8,Screen.height/8),readyPlay,"READY"))
				{
					if(!readyPlay)
					{
						GetComponent<NetworkView>().RPC("ReadyGame", RPCMode.Server,true);
						
						readyPlay = true;
					}
				}
				else if(readyPlay)
				{
					GetComponent<NetworkView>().RPC("ReadyGame", RPCMode.Server,false);
					
					readyPlay = false;
				}
				
				int l = 0;
				foreach(PlayerInfo player in playerList)
				{
					GUI.Label(new Rect(Screen.width/2-Screen.width/2.5f, Screen.height/5+25f*l,100f,20f),player.id+". "+player.username);
					l+=1;
				}

				GUI.EndGroup();
				break;
			}
		}
	}

	//Обновление списка
	void RefreshList()
	{
		MasterServer.RequestHostList("Normal");
		hostData = MasterServer.PollHostList();
	}
	
	//При загрузке сервера
	void OnLevelWasLoaded(int level)
	{
		switch(level)
		{
			case 0:
			//В меню уничтожаем созданных игроков
			foreach(GameObject go in FindObjectsOfType<GameObject>()) 
			{
				if(go.tag == "Player") DestroyObject(go);
			}
			break;

			case 1:
			//Отсылаем всем объектам, что игра загружена			
			if(reconnect)
			{
				NetworkConnectionError conn = Network.Connect(remoteIP);
				if(conn == NetworkConnectionError.NoError)
				{
					Debug.Log("Connecting...");
					reconnect = false;
				}
				else
				{
					Network.Disconnect();
					Application.LoadLevel(0);
				}
			}
			else
			{
				foreach(GameObject go in FindObjectsOfType<GameObject>()) 
				{
					go.SendMessage("OnLoad", SendMessageOptions.DontRequireReceiver); 
				}
			}
			//if(Network.isServer) MasterServer.UnregisterHost();
			break;
		}
	}
	
	void OnServerInitialized()
	{
		while(Network.player.externalPort == 65535)
		{
			StartCoroutine(WaitSec(1f));
		}
		
		MasterServer.RegisterHost("Normal", player_name, serverTime.ToString());

		playerList = new List<PlayerInfo>();
		
		PlayerIDs = new List<bool>();
		PlayerIDs.Add(true);
		PlayerIDs.Add(false);
		PlayerIDs.Add(false);
		PlayerIDs.Add(false);

		player_id = 0;

		GetComponent<NetworkView>().RPC("SignInGame", RPCMode.AllBuffered, Network.player, player_name, player_id);
	}

	IEnumerator WaitSec(float t)
	{
		yield return new WaitForSeconds(t);
	}
	
	void OnDisconnectedFromServer(NetworkDisconnection info)
	{
		if(finding == 1)
		{
			foreach(GameObject go in FindObjectsOfType<GameObject>()) 
			{
				if(go.tag == "LobbyPlayer") DestroyObject(go);
			}
			Debug.Log ("Destroyed");
		}
		finding = 0;
		window = 0;
	}
	
	void OnConnectedToServer()
	{
		playerList = new List<PlayerInfo>();
		GetComponent<NetworkView>().RPC("SignAccess", RPCMode.Server, Network.player, 0);
		//Application.LoadLevel(1);
	}

	//Вызывается на всех объектах при загрузке игры
	void OnLoad()
	{
		float x = 50f;
		float y = 50f;

		switch(player_id)
		{
			case 0:
			x = 39f;
			y = 61f;
			break;

			case 1:
			x = 61f;
			y = 61f;
			break;
			
			case 2:
			x = 39f;
			y = 39f;
			break;
			
			case 3:
			x = 61f;
			y = 39f;
			break;
		}
		Network.Instantiate(Resources.Load("DROP",typeof(GameObject)),new Vector3(x,50f,y), Quaternion.Euler(0,0,0), 0);
		Camera.main.GetComponent<Moving>().target = (Network.Instantiate(Resources.Load("Robo",typeof(GameObject)),new Vector3(x,51.4f,y), Quaternion.Euler(0,0,0), 0) as GameObject).transform;
	}

	void OnPlayerConnected(NetworkPlayer player)
	{
		if(finding == 0) finding = 1;
		Debug.Log("Player connected from: " + player.ipAddress +":" + player.port);
	}

	void OnPlayerDisconnected(NetworkPlayer player)
	{
		if((finding == 1)&&(Network.connections.Length<2))
		{
			finding = 0;
		}
		foreach(PlayerInfo info in playerList)
		{
			if(info.player == player) delete = info;
		}
		PlayerIDs[delete.id] = false;

		Debug.Log("Player " + delete.username +" has disconnected");

		Network.RemoveRPCs(player);
		Network.DestroyPlayerObjects(player);
		
		GetComponent<NetworkView>().RPC("LeftGame", RPCMode.Others, player);
		DestroyObject(Platforms[delete.id].GetChild(11).gameObject);
		playerList.Remove(delete);
	}	
	
	//RPC функции, обмен информацией
	[RPC]
	void ShutDown()
	{
		if(PlayerPrefs.HasKey("server_ip"))
		{
			PlayerPrefs.DeleteKey("server_ip");
			PlayerPrefs.DeleteKey("server_port");
			PlayerPrefs.DeleteKey("server_guid");
			if(PlayerPrefs.HasKey("server_nat")) PlayerPrefs.DeleteKey("server_nat");
		}
		Network.Disconnect(200);
		Application.LoadLevel(0);
		DestroyObject(gameObject);
	}

	[RPC]
	void SignAccess(NetworkPlayer player,int id)
	{
		if(Network.isServer)
		{
			for(int j=1;j<4;j++)
			{
				if(PlayerIDs[j] == false)
				{
					id = j;
					PlayerIDs[j] = true;
					break;
				}
			}
			GetComponent<NetworkView>().RPC("SignAccess", player, player, id);
		}
		else if(player == Network.player)
		{
			GetComponent<NetworkView>().RPC("SignInGame", RPCMode.AllBuffered, Network.player, player_name, id);
		}
	}

	[RPC]
	void SignInGame(NetworkPlayer player, string name, int id)
	{
		PlayerInfo info = new PlayerInfo();
		info.username = name;
		info.player = player;
		info.id = id;
		if(Network.isServer)
		{
			Debug.Log("Server");
		}
		else
		{
			Debug.Log("Client");
		}
		Debug.Log(name+" "+info.id.ToString());
		playerList.Add(info);

		if(finding < 2)
		{
			GameObject pltmp = (Instantiate(Resources.Load("R-Robo",typeof(GameObject)),new Vector3(Platforms[info.id].position.x,-177.1f,Platforms[info.id].position.z), Quaternion.Euler(0,180,0)) as GameObject);
			pltmp.transform.SetParent(Platforms[info.id]);
		}
		
		if(Network.player == player)
		{
			player_id = info.id;
			
			if(finding == 2) 
			{
				foreach(GameObject go in FindObjectsOfType<GameObject>()) 
				{
					go.SendMessage("OnLoad", SendMessageOptions.DontRequireReceiver); 
				}
			}
			//Network.Instantiate(Resources.Load("R-Robo",typeof(GameObject)),new Vector3(Platforms[player_id].position.x,0.9f,Platforms[player_id].position.z), Quaternion.Euler(0,180,0),1);
		}
	}

	[RPC]
	void LeftGame(NetworkPlayer player)
	{
		if(Network.isClient)
		{
			foreach(PlayerInfo info in playerList)
			{
				if(info.player == player) delete = info;
			}
			DestroyObject(Platforms[delete.id].GetChild(11).gameObject);
			playerList.Remove(delete);
		}
	}

	[RPC]
	void ReadyGame(bool ready)
	{
		if(ready) readyPlayers++;
		else readyPlayers--;
	}

	[RPC]
	void StartGame()
	{
		/*
		Network.Disconnect(200);
		finding = 2;
		Application.LoadLevel(1);*/

		string ips = "";
		foreach(string ipp in remoteIP.ip)
		{
			ips+=" "+ipp;
		}
		ips = ips.Remove(0,1);
		PlayerPrefs.SetString("server_ip",ips);
		PlayerPrefs.SetInt("server_port",remoteIP.port);
		PlayerPrefs.SetString("server_guid",remoteIP.guid);
		if(remoteIP.useNat) PlayerPrefs.SetInt("server_nat",0);

		foreach(PlayerInfo pl in playerList)
		{
			Platforms[pl.id].GetComponent<Animation>().Play("Close");
		}
		window = 3;
		GameStart = 1f;
	}
}