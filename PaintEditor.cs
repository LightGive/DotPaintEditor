using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

public class PaintEditor : EditorWindow
{
	private const float WINDOW_W = 400.0f;
	private const float WINDOW_H = 670.0f;
	private const float OFFSET_POS_X = 10;
	private const float OFFSET_POS_Y = 270;
	private const float MAX_SIDE = 380;

	public static int width = 8;
	public static int height = 8;
	public static int xx = 0;
	public static int yy = 0;

	public static PaintEditor window;
	public static Rect[,] rectList = new Rect[64, 64];
	public static Color[,] rectColor = new Color[64, 64];

	public static string imgName = "ImageName";
	private Vector2 mousePos = Vector2.zero;
	private Vector2 rectPos = Vector2.zero;
	private Event action;
	private Color nowColor = Color.black;
	private Color color1 = Color.black;
	private Color color2 = Color.white;

	public static Color[] glidColor = new Color[3];
	private Texture2D whiteTexture = Texture2D.whiteTexture;

	private GlidLine glidLine = GlidLine.Black;
	private Tools tools = Tools.Pen;
	private ExportType exportType = ExportType.PNG;

	public enum GlidLine
	{
		None,
		Black,
		White,
	}

	public enum Tools
	{
		Pen,
		Eraser,
		Backet,
	}

	public enum ExportType
	{
		PNG,
		JPEG
	}

	[MenuItem("Tools/DotPaintTool")]
	public static void OpenPaintTool()
	{
		Init();
		window = EditorWindow.GetWindow<PaintEditor>("PaintTool");
		window.position = new Rect(300, 30, WINDOW_W, WINDOW_H);
		window.Show();
	}
	public static void Init()
	{
		glidColor[0] = Color.clear;
		glidColor[1] = Color.black;
		glidColor[2] = Color.white;

		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
				rectColor[i, j] = Color.clear;
		}
	}

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
			CreateNewImage.CreateWindow();
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		color1 = EditorGUILayout.ColorField("NowColor", color1);
		color2 = EditorGUILayout.ColorField("SubColor", color2);
		EditorGUILayout.EndVertical();
		EditorGUILayout.BeginVertical();
		if (GUILayout.Button("Select"))
			nowColor = color1;
		if (GUILayout.Button("Select"))
			nowColor = color2;

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
		GUILayout.Label("GridLine", EditorStyles.boldLabel);
		glidLine = (GlidLine)GUILayout.Toolbar((int)glidLine, new string[] { "None", "Black", "White" });
		tools = (Tools)GUILayout.Toolbar((int)tools, new string[] { "Pen", "Eraser", "Baket" });
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("ExprotSetting", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		exportType = (ExportType)EditorGUILayout.EnumPopup(exportType);
		if (GUILayout.Button("Export"))
			ExportImg();
		EditorGUILayout.EndHorizontal();


		var max = width > height ? width : height;

		if (max == 0)
			return;


		var side = MAX_SIDE / max;
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				var rect = new Rect(new Vector2((i * side) + OFFSET_POS_X, (j * side) + OFFSET_POS_Y), new Vector2(side, side));
				rectList[i, j] = rect;
				GUI.color = rectColor[i, j];
				GUI.DrawTexture(rect, whiteTexture);
			}
		}

		for (int i = 0; i <= width; i++)
		{
			Handles.color = glidColor[(int)glidLine];
			Handles.DrawLine(new Vector2((i * side) + OFFSET_POS_X, OFFSET_POS_Y),
							 new Vector2((i * side) + OFFSET_POS_X, (side * height) + OFFSET_POS_Y));
		}
		for (int i = 0; i <= height; i++)
		{
			Handles.color = glidColor[(int)glidLine];
			Handles.DrawLine(new Vector2(OFFSET_POS_X, (i * side) + OFFSET_POS_Y),
							 new Vector2((side * width) + OFFSET_POS_X, (i * side) + OFFSET_POS_Y));
		}


		wantsMouseMove = true;
		action = Event.current;
		mousePos = action.mousePosition;

		if (action.type == EventType.MouseDrag ||
			action.type == EventType.MouseDown)
		{

			if (mousePos.x > (side * width) + OFFSET_POS_X ||
				mousePos.x < OFFSET_POS_X)
				xx = -1;
			if (mousePos.y > (side * height) + OFFSET_POS_Y ||
				mousePos.y < OFFSET_POS_Y)
				yy = -1;

			if (xx == -1 || yy == -1) { }
			else
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
						rectColor[xx, yy] = nowColor;
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
		for (int i = width - 1; i >= 0; i--)
		{
			for (int j = height - 1; j >= 0; j--)
			{
				tex.SetPixel(i, j, rectColor[i, height - j - 1]);
			}
		}
		byte[] imgData = null;
		string filePath = "";


		if (exportType == ExportType.PNG)
		{
			imgData = tex.EncodeToPNG();
			filePath = EditorUtility.SaveFilePanel("Save Texture", "", imgName + ".png", "png");
		}
		else if (exportType == ExportType.JPEG)
		{
			imgData = tex.EncodeToJPG();
			filePath = EditorUtility.SaveFilePanel("Save Texture", "", imgName + ".jpeg", "jpeg");
		}

		if (filePath.Length > 0)
		{
			File.WriteAllBytes(filePath, imgData);
		}
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

	[MenuItem("Tools/NewImage")]
	public static void CreateWindow()
	{
		window = EditorWindow.GetWindow<CreateNewImage>("NEW WINDOW");
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
		PaintEditor.imgName = EditorGUILayout.TextField("ImageName",PaintEditor.imgName);
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