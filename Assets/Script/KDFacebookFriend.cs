using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Facebook.Unity;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using UnityEngine.UI;

public class Player {

	public string m_Id;
	public bool m_IsFriend;
	public string m_Name;
	public int m_Highscore;
	public KD m_Kd;

	public Player (string id, bool friend, string name) {

		this.m_Id = id;
		this.m_IsFriend = friend;
		this.m_Name = name;
	}

	public Player (string id, bool friend, string name, int highScore, int k, int d) {

		this.m_Id = id;
		this.m_IsFriend = friend;
		this.m_Name = name;
		this.m_Highscore = highScore;
		this.m_Kd = new KD (k, d);
	}
		
}

public class KD {

	public int k;
	public int d;

	public KD (int k, int d) {
		this.k = k;
		this.d = d;
	}

}


public class KDFacebookFriend : MonoBehaviour {

	private Player player;
	private List<Player> players = new List<Player> ();

	public GameObject m_ListContainer;
	public GameObject m_Cell;

	public InputField idText;

	private DatabaseReference reference;

	void Start () {

		FB.Init (SetInit, OnHideUnity);
	}


	void SetInit() {

		if (FB.IsLoggedIn) {

			//LoadFbStats ();
		} else {

			Debug.Log ("FB is not logged in");
		}
	}

	void OnHideUnity(bool isGameShown) {

		if (!isGameShown) {
			Time.timeScale = 0;
		} else {
			Time.timeScale = 1;
		}

	}


	public void LogInAction () {

		Debug.Log ("Log in");

		List<string> permissions = new List<string> ();
		permissions.Add ("public_profile");

		FB.LogInWithReadPermissions (permissions, AuthCallBack);

	}

	public void SetRandomHighScoreAction () {

		int value = UnityEngine.Random.Range (100, 999);

		var scoreData = new Dictionary<string, string> ();
		scoreData ["score"] = value.ToString ();

		FB.API ("/me/scores", HttpMethod.POST, delegate (IGraphResult result) {
			Debug.Log("Set score: "+ result.RawResult);
		}, scoreData);
	}

	public void KillAction () {

		reference.Child("users").Child(player.m_Id).Child("KD").Child(idText.text).GetValueAsync().ContinueWith(task => {
			if (task.IsFaulted) {
				Debug.Log ("Firebase error:" + task.Exception );
			}
			else if (task.IsCompleted) {
				DataSnapshot snapshot = task.Result;

				if (snapshot.Value != null) {

					int k = Int32.Parse(snapshot.Child("k").Value.ToString ()); 
					int d = Int32.Parse(snapshot.Child("d").Value.ToString ()); 

					k += 1;

					KD playerKill = new KD (k, d);
					string json = JsonUtility.ToJson(playerKill);
					reference.Child("users").Child(player.m_Id).Child("KD").Child(idText.text).SetRawJsonValueAsync(json);

					KD playerDead = new KD (d, k);
					string json2 = JsonUtility.ToJson(playerDead);
					reference.Child("users").Child(idText.text).Child("KD").Child(player.m_Id).SetRawJsonValueAsync(json2);

				} else {

					KD initialKill = new KD (1, 0);
					string json = JsonUtility.ToJson(initialKill);
					reference.Child("users").Child(player.m_Id).Child("KD").Child(idText.text).SetRawJsonValueAsync(json);


					KD initialDead = new KD (0, 1);
					string json2 = JsonUtility.ToJson(initialDead);
					reference.Child("users").Child(idText.text).Child("KD").Child(player.m_Id).SetRawJsonValueAsync(json2);

				}


			}	
		});

	}

	public void RefreshListAction () {

		FB.API ("/app/scores?fields=score,user.limit(30)", HttpMethod.GET, ScoreCallBack);
	}

	void AuthCallBack(IResult result) {

		if (result.Error != null) {
			
			Debug.Log (result.Error);
		} else {
			if (FB.IsLoggedIn) {

				FB.API ("/me?fields=id,first_name", HttpMethod.GET, PlayerInfoCallBack);
				//FB.API ("/me/picture?type=square&height=128&width=128", HttpMethod.GET, DisplayProfilePic);

			} else {
				Debug.Log ("FB is not logged in");
			}
		}
	}

	void PlayerInfoCallBack (IResult result) {

		if (result.Error != null) {
			
			Debug.Log (result.Error);
		} else {
			
			string id = result.ResultDictionary["id"].ToString ();
			string name = result.ResultDictionary ["first_name"].ToString ();
			player = new Player (id, false, name);

			FB.API ("/app/scores?fields=score,user.limit(30)", HttpMethod.GET, ScoreCallBack);
		} 

	}

	private void ScoreCallBack (IResult result) {

		players = new List<Player> ();
		IDictionary<string, object> data = result.ResultDictionary;
		List<object> listObj = (List<object>) data ["data"];

		SetFriends (listObj);

	}

	private void SetFriends (List<object> listObj) {

		FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://kdfacebookfriends.firebaseio.com/");
		reference = FirebaseDatabase.DefaultInstance.RootReference;

		reference.Child ("users").Child (player.m_Id).Child ("KD").GetValueAsync ().ContinueWith (task => {
			if (task.IsFaulted) {
				Debug.Log ("Firebase error:" + task.Exception );
			} else if (task.IsCompleted) {
				DataSnapshot snapshot = task.Result;

				foreach (object obj in listObj) {

					var entry = (Dictionary<string, object>) obj;
					var user = (Dictionary<string, object>) entry ["user"];

					string id = user ["id"].ToString ();
					string name = user ["name"].ToString ();
					string highScore = entry ["score"].ToString ();
					int k = 0, d = 0;

					if (snapshot.Child(id).Value != null) {
						k = Int32.Parse(snapshot.Child(id).Child("k").Value.ToString ()); 
						d = Int32.Parse(snapshot.Child(id).Child("d").Value.ToString ()); 
					} 

					if (id == player.m_Id) {
						player.m_Highscore = Int32.Parse(highScore);
						players.Add (player);
					} else {
						
						Player player = new Player (id, true, name, Int32.Parse(highScore), k , d);
						players.Add (player);
					}
				}

				UpdateList ();

			}
		});


	}
		
	public void UpdateList () {
		

		for (int i = 0; i < players.Count; i++) {
			Debug.Log ("i"+ i);
			GameObject obj;
			if (m_ListContainer.transform.childCount > 0 && m_ListContainer.transform.childCount > i) {
				obj = m_ListContainer.transform.GetChild (i).gameObject;

			} else {
				obj = Instantiate (m_Cell);
				obj.transform.SetParent (m_ListContainer.transform);
			}

			Cell cell = obj.GetComponent<Cell> ();
			cell.SetValue (players[i]);
		}
		Debug.Log ("refresh list");
	}
		
}


