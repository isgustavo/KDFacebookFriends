using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour {

	public Text idText;
	public Text nameText;
	public Text highScoreText;

	public Text kText;
	public Text dText;

	public void SetValue (Player player) {
		idText.text = player.m_Id;
		nameText.text = player.m_Name;
		highScoreText.text = player.m_Highscore.ToString ();
		if (player.m_Kd != null) {
			kText.text = player.m_Kd.k.ToString ();
			dText.text = player.m_Kd.d.ToString ();
		} 
		if (!player.m_IsFriend) {
			
		}
	}




}
