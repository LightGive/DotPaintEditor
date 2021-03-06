﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

public class PaintEditor : EditorWindow
{

	//ウィンドウ座標
	private const float WINDOW_POS_X = 300.0f;
	private const float WINDOW_POS_Y = 20.0f;
	private const float WINDOW_W = 400.0f;
	private const float WINDOW_H = 700.0f;

	//ドット絵描画座標
	private const float OFFSET_POS_X = 10;
	private const float OFFSET_POS_Y = 310;

	//一辺の最大値
	private const float MAX_SIDE = 380;

	//ウィンドウ
	public static PaintEditor window;
	public static Rect[,] rectList = new Rect[64, 64];
	public static Color[,] rectColor = new Color[64, 64];
	public static bool[,] bucketChange = new bool[64, 64]; 


	//出力する時のファイル名
	public static string imgName = "ImageName";

	//ドット数の高さと幅の初期値
	public static int width = 8;
	public static int height = 8;

	//マウスの座標
	private Vector2 mousePos = Vector2.zero;

	//現在選択している色
	private Color nowColor = Color.black;

	//パレット
	private Color pallet1 = Color.black;
	private Color pallet2 = Color.blue;
	private Color pallet3 = Color.white;

	private Texture2D whiteTexture = Texture2D.whiteTexture;
	private GlidLineStr glidLineStr = GlidLineStr.Black;
	private Tools tools = Tools.Pen;

	/// <summary>
	/// グリッド線の色
	/// </summary>
	private Color[] glidColor = new Color[]
	{
		Color.clear,
		Color.black,
		Color.white
	};

	/// <summary>
	/// グリッド線の文字列
	/// </summary>
	public enum GlidLineStr
	{
		None,
		Black,
		White,
	}

	/// <summary>
	/// ツールリスト
	/// </summary>
	public enum Tools
	{
		Pen,
		Eraser,
		Backet,
	}

	/// <summary>
	/// ペイントツールを開く
	/// </summary>
	[MenuItem("Tools/DotPaintTool/PaintTool")]
	public static void OpenPaintTool()
	{
		Init();
		window = EditorWindow.GetWindow<PaintEditor>("PaintTool");
		window.position = new Rect(WINDOW_POS_X, WINDOW_POS_Y, WINDOW_W, WINDOW_H);
		window.Show();
	}

	/// <summary>
	/// ペイントツールを開く時の初期化
	/// </summary>
	public static void Init()
	{
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
				rectColor[i, j] = Color.clear;
		}
	}

	/// <summary>
	/// ペイントツールのレイアウト
	/// </summary>
	void OnGUI()
	{
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("CanvasSize", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		EditorGUILayout.LabelField("Width  : " + width.ToString("00"));
		EditorGUILayout.LabelField("Height : " + height.ToString("00"));
		EditorGUILayout.EndVertical();
		EditorGUILayout.BeginVertical();
		EditorGUILayout.Space();
		if (GUILayout.Button("NewImage"))
		{
			if (UnityEditor.EditorUtility.DisplayDialog("Notice", "Do you , but you really want disappear ?", "Yes", "No"))
			{
				CreateNewImage.CreateWindow();
			}
		}

		if (GUILayout.Button("AllDelete"))
		{
			Init();
		}

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);

		EditorGUILayout.BeginVertical();

		nowColor = EditorGUILayout.ColorField("NowColor", nowColor);

		//パレット
		EditorGUILayout.BeginHorizontal();
		pallet1 = EditorGUILayout.ColorField("Color1", pallet1);
		if (GUILayout.Button("Select"))
			nowColor = pallet1;
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		pallet2 = EditorGUILayout.ColorField("Color2", pallet2);
		if (GUILayout.Button("Select"))
			nowColor = pallet2;
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		pallet3 = EditorGUILayout.ColorField("Color3", pallet3);
		if (GUILayout.Button("Select"))
			nowColor = pallet3;
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.EndVertical();

		EditorGUILayout.Space();
		GUILayout.Label("GridLine", EditorStyles.boldLabel);
		glidLineStr = (GlidLineStr)GUILayout.Toolbar((int)glidLineStr, new string[] { "None", "Black", "White" });
		tools = (Tools)GUILayout.Toolbar((int)tools, new string[] { "Pen", "Eraser", "Bucket" });
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Exprot", EditorStyles.boldLabel);

		//出力ボタンが押されたとき
		if (GUILayout.Button("PNG Export"))
			ExportImg();

		var max = width > height ? width : height;

		if (max == 0)
			return;

		var side = MAX_SIDE / max;
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				var rect = new Rect((i * side) + OFFSET_POS_X, (j * side) + OFFSET_POS_Y, side, side);
				rectList[i, j] = rect;
				GUI.color = rectColor[i, j];
				GUI.DrawTexture(rect, whiteTexture);
			}
		}

		for (int i = 0; i <= width; i++)
		{
			Handles.color = glidColor[(int)glidLineStr];
			Handles.DrawLine(new Vector2((i * side) + OFFSET_POS_X, OFFSET_POS_Y),
							 new Vector2((i * side) + OFFSET_POS_X, (side * height) + OFFSET_POS_Y));
		}
		for (int i = 0; i <= height; i++)
		{
			Handles.color = glidColor[(int)glidLineStr];
			Handles.DrawLine(new Vector2(OFFSET_POS_X, (i * side) + OFFSET_POS_Y),
							 new Vector2((side * width) + OFFSET_POS_X, (i * side) + OFFSET_POS_Y));
		}


		wantsMouseMove = true;
		var action = Event.current;

		if (action.type == EventType.MouseDrag ||
			action.type == EventType.MouseDown)
		{
			mousePos = action.mousePosition;

			var xx = 0;
			var yy = 0;

			if (mousePos.x > (side * width) + OFFSET_POS_X ||
				mousePos.x < OFFSET_POS_X)
				xx = -1;

			if (mousePos.y > (side * height) + OFFSET_POS_Y ||
				mousePos.y < OFFSET_POS_Y)
				yy = -1;

			if (xx != -1 && yy != -1)
			{

				int ii = width - 1;
				while (ii >= 0)
				{
					if (mousePos.x > (ii * side) + OFFSET_POS_X)
					{
						xx = ii;
						break;
					}
					ii--;
				}

				int jj = height - 1;
				while (jj >= 0)
				{
					if (mousePos.y > (jj * side) + OFFSET_POS_Y)
					{
						yy = jj;
						break;
					}
					jj--;
				}
				switch (tools)
				{
					case Tools.Pen:
						rectColor[xx, yy] = nowColor;
						break;
					case Tools.Eraser:
						rectColor[xx, yy] = Color.clear;
						break;
					case Tools.Backet:

						for (int i = 0; i < 64; i++)
						{
							for (int j = 0; j < 64; j++)
								bucketChange[i, j] = false;
						}

						Bucket(xx, yy);

						rectColor[xx, yy] = nowColor;
						for (int i = 0; i < 64; i++)
						{
							for(int j = 0; j < 64; j++)
							{
								if(bucketChange[i, j])
									rectColor[i, j] = nowColor;
							}
						}

						break;
				}

				Repaint();
			}
		}
	}

	/// <summary>
	/// 画像ファイルを出力する
	/// </summary>
	void ExportImg()
	{
		Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

		//Texture2Dに色情報を入れる
		for (int i = width - 1; i >= 0; i--)
		{
			for (int j = height - 1; j >= 0; j--)
			{
				tex.SetPixel(i, j, rectColor[i, height - j - 1]);
			}
		}

		//エンコードに入れる用の変数
		byte[] imgData = null;

		//ファイルのパス
		string filePath = "";

		imgData = tex.EncodeToPNG();
		filePath = EditorUtility.SaveFilePanel("Save Texture", "", imgName + ".png", "png");


		if (filePath.Length > 0)
		{
			File.WriteAllBytes(filePath, imgData);
		}
	}


	/// <summary>
	/// Bucketツールを使ったとき
	/// 再帰呼び出し関数
	/// </summary>
	/// <param name="i">X</param>
	/// <param name="j">Y</param>
	void Bucket(int i, int j)
	{
		//塗る配列をTrueにする
		bucketChange[i, j] = true;

		//添え字が配列内、かつ隣が同じ色の時、かつチェック済でないところ
		if (i < 63 && rectColor[i, j] == rectColor[i + 1, j] && !bucketChange[i + 1, j])
			Bucket(i + 1, j);
		if (i > 0 && rectColor[i, j] == rectColor[i -1, j] && !bucketChange[i-1, j])
			Bucket(i - 1, j);
		if (j < 63 && rectColor[i, j] == rectColor[i, j + 1] && !bucketChange[i, j + 1])
			Bucket(i, j + 1);
		if (j >0 && rectColor[i, j] == rectColor[i, j - 1] && !bucketChange[i, j-1])
			Bucket(i, j - 1);

	}
}



/// <summary>
///	幅と高さを決めるウィンドウ
/// </summary>
public class CreateNewImage : EditorWindow
{
	private const float WINDOW_W = 250.0f;
	private const float WINDOW_H = 150.0f;

	public static CreateNewImage window;

	/// <summary>
	/// 新しくドット絵を編集するとき
	/// </summary>
	[MenuItem("Tools/DotPaintTool/NewImage")]
	public static void CreateWindow()
	{
		window = EditorWindow.GetWindow<CreateNewImage>("NEW IMAGE");
		window.position = new Rect(
			(Screen.width),
			(Screen.height),
			WINDOW_W,
			WINDOW_H);

		window.ShowPopup();

	}

	void OnGUI()
	{
		GUILayout.Space(10);
		GUILayout.Label("CREATE NEW IMAGE", EditorStyles.boldLabel);
		PaintEditor.imgName = EditorGUILayout.TextField("ImageName", PaintEditor.imgName);
		PaintEditor.width = Mathf.Clamp(EditorGUILayout.IntField("WIDTH ", PaintEditor.width), 1, 64);
		PaintEditor.height = Mathf.Clamp(EditorGUILayout.IntField("HEIGHT", PaintEditor.height), 1, 64);
		GUILayout.Space(20);
		if (GUILayout.Button("Create"))
		{
			PaintEditor.OpenPaintTool();
			this.Close();
		}
		if (GUILayout.Button("Cancel"))
		{
			this.Close();
		}

	}
}
