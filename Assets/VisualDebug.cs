using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
	public class VisualDebug : MonoBehaviour 
	{
		static VisualDebug instance = null;
		void Awake()
		{
			if(instance == null)
			{
				instance = this;
			}
			else
			{
				Destroy (this.gameObject);
			}
		}

		public static VisualDebug Instance {
			get 
			{
				if(instance == null)
				{
					// First try and find if there's one in the scene already
					Object foundObject = FindObjectOfType(typeof(VisualDebug));

					if(foundObject != null && foundObject is VisualDebug)
					{
						instance = foundObject as VisualDebug;
					}
					else
					{
						// None was found so have to make a new one
						GameObject newGameObject = new GameObject("VisualDebug", typeof(VisualDebug));
						instance = newGameObject.GetComponent<VisualDebug>();
					}
				}
				return instance;
			}
		}

		List<Point> pointsToDraw = new List<Point>();
		List<Line> linesToDraw = new List<Line>();
		//List<Polygon> polygonsToDraw = new List<Polygon>();
		List<Text> textToDraw = new List<Text>();

		class Line
		{
			public Vector3 from;
			public Vector3 to;
			public Color color = Color.white;
            public float thickness = 1;

			public Line (Vector3 from, Vector3 to, Color color, float thickness)
			{
				this.from = from;
				this.to = to;
				this.color = color;
				this.thickness = thickness;
            }
		}

		class Point
		{
			public Vector3 Position;
			public Color Color;
			public float Size = 0.4f;

			public Point (Vector3 position, Color color)
			{
				this.Position = position;
				this.Color = color;
			}

			public Point (Vector3 position, Color color, float size)
			{
				this.Position = position;
				this.Color = color;
				this.Size = size;
			}
		}

		class Text
		{
			public Vector3 Position;
			public string Message;

			public Text (Vector3 position, string message)
			{
				this.Position = position;
				this.Message = message;
			}
		}

		void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			for (int i = 0; i < pointsToDraw.Count; i++) 
			{
				Gizmos.color = pointsToDraw[i].Color;
				Gizmos.DrawSphere(pointsToDraw[i].Position, pointsToDraw[i].Size);
			}

			for (int i = 0; i < linesToDraw.Count; i++)
            {
                if (linesToDraw[i].thickness == 1)
                {
                    Gizmos.color = linesToDraw[i].color;
                    Gizmos.DrawLine(linesToDraw[i].from, linesToDraw[i].to);
                }
                else
                {
                    UnityEditor.Handles.color = linesToDraw[i].color;
                    UnityEditor.Handles.DrawAAPolyLine(linesToDraw[i].thickness, linesToDraw[i].from, linesToDraw[i].to);
                }
            }

            GUIStyle style = new GUIStyle(GUI.skin.label);
			style.fontSize *= 3;
			for (int i = 0; i < textToDraw.Count; i++) 
			{
				UnityEditor.Handles.Label(textToDraw[i].Position, textToDraw[i].Message, style);
			}

			//SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);
		
			//for (int i = 0; i < polygonsToDraw.Count; i++) 
			//{
			//	Color color = polygonsToDraw[i].Vertices[0].Color;
			//	SabreGraphics.DrawPolygons(new Color(color.r,color.g,color.b, 0.3f), color, polygonsToDraw[i]);
			//}
		}
		
		public static void ClearAll()
		{
			Instance.pointsToDraw.Clear ();
			//Instance.polygonsToDraw.Clear ();
			Instance.linesToDraw.Clear ();
			Instance.textToDraw.Clear ();
		}
		
		public static void AddPoint(Vector3 point)
		{
			Instance.pointsToDraw.Add (new Point(point, Color.green));
		}

		public static void AddPoint(Vector3 point, Color color, float size = 0.4f)
		{
			Instance.pointsToDraw.Add (new Point(point, color, size));
		}

        public static void AddPoints(params Vector3[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Instance.pointsToDraw.Add(new Point(points[i], Color.green));
            }
        }


        public static void AddText(Vector3 point, string message)
		{
			Instance.textToDraw.Add (new Text(point, message));
		}

		public static void AddLine(Vector3 from, Vector3 to, Color color, float thickness = 1)
		{
			Instance.linesToDraw.Add (new Line(from, to, color, thickness));
		}

		public static void AddLinePolygon(Vector3[] points, Color color)
		{
			for (int i = 0; i < points.Length; i++) 
			{
				Instance.linesToDraw.Add (new Line(points[i], points[(i+1)%points.Length], color, 1));
			}
		}

        public static void AddLinePolygon(List<Vector3> points, Color color)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Instance.linesToDraw.Add(new Line(points[i], points[(i + 1) % points.Count], color, 1));
            }
        }

        //public static void AddLinePolygon(Vertex[] points, Color color)
        //{
        //    for (int i = 0; i < points.Length; i++)
        //    {
        //        Instance.linesToDraw.Add(new Line(points[i].Position, points[(i + 1) % points.Length].Position, color, 1));
        //    }
        //}

        //public static void AddLinePolygon(List<Vertex> points, Color color)
        //{
        //    for (int i = 0; i < points.Count; i++)
        //    {
        //        Instance.linesToDraw.Add(new Line(points[i].Position, points[(i + 1) % points.Count].Position, color, 1));
        //    }
        //}

//        public static void AddPolygon(Sabresaurus.SabreCSG.Polygon polygon, Color color)
//		{
//			Polygon polygonCopy = polygon.DeepCopy();
//			polygonCopy.SetColor(color);
//			Instance.polygonsToDraw.Add(polygonCopy);
////			for (int i = 0; i < polygon.Vertices.Length; i++) 
////			{
////				AddLine(polygon.Vertices[i].Position, polygon.Vertices[(i+1)%polygon.Vertices.Length].Position, color);
////			}
		//}

		//public static void AddPolygons(params Sabresaurus.SabreCSG.Polygon[] polygons)
		//{
		//	for (int i = 0; i < polygons.Length; i++) 
		//	{
		//		Instance.polygonsToDraw.Add(polygons[i]);
		//	}
		//}
		//public static void AddPolygons(List<Sabresaurus.SabreCSG.Polygon> polygons)
		//{
		//	for (int i = 0; i < polygons.Count; i++) 
		//	{
		//		Instance.polygonsToDraw.Add(polygons[i]);
		//	}
		//}
	}
}
