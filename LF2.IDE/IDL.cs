using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using LF2;

namespace LF2.IDE
{
	public enum MsgType : byte
	{
		Info,
		Warning,
		Error,
	}

	public delegate void Logger(string msg, string title, MsgType msgType);

	internal static class IDL
	{
		private sealed class WindowOwner : System.Windows.Forms.IWin32Window
		{
			public IntPtr Handle { get; private set; }
			public WindowOwner(IntPtr handle) { Handle = handle; }
		}

		public static int InstantLoad(string data, int procId, DataType dataType, int datId,
			ObjectType objType, IntPtr hMainWindow, Logger logFunc)
		{
			Logger logger = logFunc ?? ((message, title, type) =>
				System.Windows.Forms.MessageBox.Show(new WindowOwner(hMainWindow), message,
					title ?? "Instant Data Loader", System.Windows.Forms.MessageBoxButtons.OK,
					type == MsgType.Error ? System.Windows.Forms.MessageBoxIcon.Error :
					type == MsgType.Warning ? System.Windows.Forms.MessageBoxIcon.Warning :
					System.Windows.Forms.MessageBoxIcon.Information));
			return IdlLoader.InstantLoad(data, procId, dataType, datId, objType, logger);
		}

		public static int ReadDataTxt(string dataTxtContent, out ObjectData[] objects,
			out BackgroundData[] backgrounds, IntPtr hMainWindow)
		{
			objects = null;
			backgrounds = null;
			try
			{
				var obj = new List<ObjectData>();
				var bg = new List<BackgroundData>();
				string[] tokens = dataTxtContent.Split(new[] { ' ', '\t', '\r', '\n' },
					StringSplitOptions.RemoveEmptyEntries);
				int section = 0;
				for (int i = 0; i < tokens.Length; i++)
				{
					switch (tokens[i])
					{
						case "<object>": section = 1; break;
						case "<background>": section = 2; break;
						case "<object_end>":
						case "<background_end>": section = 0; break;
						case "id:":
							if (section == 1)
								obj.Add(new ObjectData { id = int.Parse(tokens[++i]) });
							else if (section == 2)
								bg.Add(new BackgroundData { id = int.Parse(tokens[++i]) });
							break;
						case "type:":
							if (section == 1 && obj.Count > 0)
							{
								ObjectData value = obj[obj.Count - 1];
								value.type = (ObjectType)int.Parse(tokens[++i]);
								obj[obj.Count - 1] = value;
							}
							break;
						case "file:":
							if (section == 1 && obj.Count > 0)
							{
								ObjectData value = obj[obj.Count - 1];
								value.file = tokens[++i];
								obj[obj.Count - 1] = value;
							}
							else if (section == 2 && bg.Count > 0)
							{
								BackgroundData value = bg[bg.Count - 1];
								value.file = tokens[++i];
								bg[bg.Count - 1] = value;
							}
							break;
					}
				}
				objects = obj.ToArray();
				backgrounds = bg.ToArray();
				return 0;
			}
			catch (Exception ex)
			{
				System.Windows.Forms.MessageBox.Show(new WindowOwner(hMainWindow), ex.Message,
					"data.txt Reading Error", System.Windows.Forms.MessageBoxButtons.OK,
					System.Windows.Forms.MessageBoxIcon.Error);
				return -1;
			}
		}
	}

	public struct ObjectData
	{
		public int id;
		public ObjectType type;
		public string file;
	}

	public struct BackgroundData
	{
		public int id;
		public string file;
	}


	// Not directly used in interop calls
	public struct DataTxt
	{
		public ObjectData[] objects;
		public BackgroundData[] backgrounds;
	}
}
