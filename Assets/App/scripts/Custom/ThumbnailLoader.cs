using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ThumbnailLoader : MonoBehaviour {

	public YoutubePlayer ytPlayer;
	public RawImage Thumbnail;
	public Image VideoSymbol;
//	public string rootUrl;
	public Text message;



	// Use this for initialization
	void Start () {
		Thumbnail.enabled = false;
//		VideoSymbol.enabled = false;

		StartCoroutine (LoadThumbnail());

//		Debug.Log ("ThumbnailLoader: Start");
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	IEnumerator LoadThumbnail(){

//		ytPlayer.youtubeUr

		string locUrl = ytPlayer.youtubeUrl;
		int idSt = locUrl.IndexOf ("be/");
		string locId = locUrl.Substring (idSt + 3);		// extract video id from player

		string imgUrl = "https://i.ytimg.com/vi/" + locId + "/hqdefault.jpg";


		Debug.Log (imgUrl);

		//WWW www = new WWW (imgUrl);
		UnityWebRequest www = UnityWebRequestTexture.GetTexture(imgUrl);

		//yield return www;
		yield return www.SendWebRequest();

		//Debug.Log ("did load?" + www.isDone + "," + www.error + "," + www.bytes.Length);
			Debug.Log("did load?" + www.isDone + ", " + www.error);

		if (!www.isNetworkError && !www.isHttpError){	// www.error == null) {
			
			message.text = "";

			//www.LoadImageIntoTexture(Thumbnail.texture as Texture2D);
			Thumbnail.texture = DownloadHandlerTexture.GetContent(www);
			Thumbnail.enabled = true;
//			VideoSymbol.enabled = true;

			string imgUrl2 = "https://i.ytimg.com/vi/" + locId + "/maxresdefault.jpg";
			//WWW www2 = new WWW (imgUrl2);
			UnityWebRequest www2 = UnityWebRequestTexture.GetTexture(imgUrl2);

			yield return www2.SendWebRequest();

			//if (www2.error == null) {
			if (!www2.isNetworkError && !www2.isHttpError)
			{
				//www2.LoadImageIntoTexture(Thumbnail.texture as Texture2D);
				Thumbnail.texture = DownloadHandlerTexture.GetContent(www2);
			}
				

		} else {

			message.text = "Thumbnail error:\n" + www.error;

			Thumbnail.enabled = false;

			//WWW www3 = new WWW (ytPlayer.youtubeUrl);
			UnityWebRequest www3 = UnityWebRequest.Get(ytPlayer.youtubeUrl);

			yield return www3.SendWebRequest();

			//if (www3.error)
			if (www3.isHttpError || www3.isNetworkError) {
				message.text = "No Internet Connection";
			}
		}

//		Thumbnail.te

		//		ThumbnailTexture;

//		www.LoadImageIntoTexture(ThumbnailTexture as Texture2D);

		//		img.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0, 0));

	}
}
