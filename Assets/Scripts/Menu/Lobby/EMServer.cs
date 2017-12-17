using UnityEngine;
using System.Collections;

public class EMServer:MonoBehaviour
{
	public string masterServerURL;
	public string gameType;
	public string gameName;
	public string comment = "";
	public float delayBetweenUpdates = 4.0f;
	private HostData[] hostData;

	
	//MServer = gameObject.AddComponent<EMServer>();
	//MServer.masterServerURL = "http://murr.tf9.ru/"

	void Awake()
	{
		Object[] gos = FindObjectsOfType(typeof(EMServer));
		if(gos.Length > 1) Destroy (gameObject);
		else DontDestroyOnLoad(this);
	}

	public HostData[] PollHostList() 
	{
		return hostData;
	}
	
	public IEnumerator RequestHostList(string type)
	{
		string url = masterServerURL+"QueryMS.php?gameType="+type;
		Debug.Log ("Looking for URL " + url);
		WWW www = new WWW (url);
		yield return www;
		if (www.text == "")
		{
			hostData = null;
			yield break;
		}
		string[] hosts = www.text.Split(";"[0]);
		hostData = new HostData[hosts.Length];
		int index = 0;
		foreach(string host in hosts)
		{
			string[] data = host.Split(","[0]);
			hostData[index] = new HostData ();
			hostData[index].ip = new string[1];
			hostData[index].ip[0] = data[0];
			hostData[index].port = int.Parse(data[1]);
			hostData[index].useNat = (data[2] == "1");
			hostData[index].guid = data[3];
			hostData[index].gameType = data[4];
			hostData[index].gameName = data[5];
			hostData[index].connectedPlayers = int.Parse(data[6]);
			hostData[index].playerLimit = int.Parse(data[7]);
			hostData[index].passwordProtected = (data[8] == "1");
			hostData[index].comment = data[9];
			index ++;
		}
	}

	public void ClearHostList()
	{
		hostData = null;
	}
	
	IEnumerator RegistrationLoop()
	{
		while(Network.isServer)
		{
			var url = masterServerURL+"UpdateHost.php";
			url += "?gameType="+gameType;
			url += "&gameName="+gameName;
			Debug.Log(url);
			var www = new WWW (url);
			yield return www;
			yield return new WaitForSeconds(delayBetweenUpdates);
		}
	}
	
	public IEnumerator RegisterHost(string type, string name, string com)
	{
		gameType = type;
		gameName = name;
		comment = com;
		string url = masterServerURL+"RegisterHost.php";
		url += "?gameType="+gameType;
		url += "&gameName="+gameName;
		url += "&comment="+comment;
		url += "&useNat="+!Network.HavePublicAddress();
		url += "&connectedPlayers="+(Network.connections.Length + 1);
		url += "&playerLimit="+Network.maxConnections;
		url += "&internalIp="+Network.player.ipAddress;
		url += "&internalPort="+Network.player.port;
		url += "&externalIp="+Network.player.externalIP;
		url += "&externalPort="+Network.player.externalPort;
		url += "&guid="+Network.player.guid;
		url += "&passwordProtected="+(Network.incomingPassword != "" ? 1 : 0);
		Debug.Log(url);
		var www = new WWW (url);
		yield return www;
		StartCoroutine(RegistrationLoop());
	}
	
	void OnPlayerConnected(NetworkPlayer player)
	{
		string url = masterServerURL+"UpdatePlayers.php";
		url += "?gameType="+gameType;
		url += "&gameName="+gameName;
		url += "&connectedPlayers="+(Network.connections.Length + 1);
		Debug.Log ("url " + url);
		WWW www = new WWW (url);
		Debug.Log (www.text);
	}
	
	void OnPlayerDisconnected(NetworkPlayer player)
	{
		string url = masterServerURL+"UpdatePlayers.php";
		url += "?gameType="+gameType;
		url += "&gameName="+gameName;
		url += "&connectedPlayers="+Network.connections.Length;
		Debug.Log ("url " + url);
		WWW www = new WWW (url);
		Debug.Log (www.text);
	}
	
	IEnumerator OnDisconnectedFromServer(NetworkDisconnection info)
	{
		if (Network.isServer)
		{
			StartCoroutine(UnregisterHost());
			yield break;
		}
	}

	public IEnumerator UnregisterHost()
	{
		string url = masterServerURL+"UnregisterHost.php";
		url += "?gameType="+gameType;
		url += "&gameName="+gameName;
		WWW www = new WWW (url);
		yield return www;
	}
}
