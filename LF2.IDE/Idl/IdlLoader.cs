using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using LF2;

namespace LF2.IDE
{
	internal static class IdlLoader
	{
		const int FrameSize = 376, FramesOffset = 1956, DataSize = 152368;
		const int SpawnSize = 224, PhaseSize = 13504, StageSize = 1350408, BackgroundSize = 2448;
		const long FilesAddress = 0x458B00 + 2004;

		sealed class LoadException : Exception
		{
			public LoadException(string message) : base(message)
			{
			}
		}
		sealed class Token
		{
			public string Text;
			public int Line, Column;
		}
		sealed class Parser
		{
			readonly List<Token> tokens = new List<Token>();
			readonly Logger log;
			int index;
			public int Warnings;
			public Parser(string text, Logger logger)
			{
				log = logger;
				int line = 1, col = 1, start = -1, tokenLine = 1, tokenCol = 1;
				for (int i = 0; i <= text.Length; i++)
				{
					char c = i == text.Length ? ' ' : text[i];
					bool space = c == ' ' || c == '\t' || c == '\r' || c == '\n';
					if (space && start >= 0)
					{
						tokens.Add(new Token { Text = text.Substring(start, i - start), Line = tokenLine, Column = tokenCol });
						start = -1;
					}
					else if (!space && start < 0)
					{
						start = i;
						tokenLine = line;
						tokenCol = col;
					}
					if (c == '\n')
					{
						line++;
						col = 1;
					}
					else
						col++;
				}
			}
			public bool More
			{
				get
				{
					return index < tokens.Count;
				}
			}
			public Token Next()
			{
				if (!More)
					throw new LoadException("Unexpected end of data");
				return tokens[index++];
			}
			public int Int()
			{
				Token t = Next();
				int value;
				if (!int.TryParse(t.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
					throw new LoadException("Invalid integer '" + t.Text + "' in line " + t.Line + " at col " + t.Column);
				return value;
			}
			public double Double()
			{
				Token t = Next();
				double value;
				if (!double.TryParse(t.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
					throw new LoadException("Invalid number '" + t.Text + "' in line " + t.Line + " at col " + t.Column);
				return value;
			}
			public void Unknown(Token t)
			{
				if (log != null)
					log(string.Format("Unhandled token: '{0}' in line {1} at col {2}", t.Text, t.Line, t.Column), null, MsgType.Warning);
			}
			public void Warning(string message)
			{
				if (log == null)
					return;
				log(message, null, MsgType.Warning);
				if (++Warnings >= 10)
					throw new LoadException("Warning level too high");
			}
			public void String(byte[] dst, int offset, int capacity, string label, bool truncate)
			{
				Token t = Next();
				byte[] s = Encoding.GetEncoding(1252).GetBytes(t.Text);
				if (s.Length >= capacity)
				{
					string message =
						string.Format("Length {0} for {1} is overflow, it {2} be less than {3}: \"{4}\" in line: {5} at col: {6}", s.Length,
									  label, truncate ? "should" : "must", capacity, t.Text, t.Line, t.Column);
					if (!truncate)
						throw new LoadException(message);
					Warning(message);
				}
				Buffer.BlockCopy(s, 0, dst, offset, Math.Min(s.Length, capacity - 1));
			}
		}
		static int Get(byte[] b, int p)
		{
			return BitConverter.ToInt32(b, p);
		}
		static uint GetPointer(byte[] b, int p)
		{
			return BitConverter.ToUInt32(b, p);
		}
		static void Set(byte[] b, int p, int value)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(value), 0, b, p, 4);
		}
		static void SetDouble(byte[] b, int p, double value)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(value), 0, b, p, 8);
		}
		static int Offset<T>(string name)
		{
			return (int)Marshal.OffsetOf(typeof(T), name);
		}
		static bool IntField<T>(Parser p, Token t, byte[] b, int at)
		{
			string name = t.Text.TrimEnd(':');
			var f = typeof(T).GetField(name);
			if (f == null || f.FieldType != typeof(int) || name.StartsWith("unknown", StringComparison.Ordinal))
				return false;
			Set(b, at + Offset<T>(name), p.Int());
			return true;
		}
		static void Preserve(byte[] source, byte[] dest, int offset, int count)
		{
			Buffer.BlockCopy(source, offset, dest, offset, count);
		}

		sealed class ParsedObject
		{
			public readonly byte[] Data = new byte[DataSize];
			public readonly List<byte[]>[] Bdys = new List<byte[]>[400], Itrs = new List<byte[]>[400];
		}
		static byte[] ParsePoint<T>(Parser p, string end)
		{
			byte[] result = new byte[Marshal.SizeOf(typeof(T))];
			while (p.More)
			{
				Token t = p.Next();
				if (t.Text == end)
					return result;
				if (typeof(T) == typeof(Itr) && (t.Text == "catchingact:" || t.Text == "caughtact:"))
				{
					int o = t.Text == "catchingact:" ? 48 : 56;
					Set(result, o, p.Int());
					Set(result, o + 4, p.Int());
				}
				else if (!IntField<T>(p, t, result, 0))
					p.Unknown(t);
			}
			throw new LoadException("Missing " + end);
		}
		static void Bounds(byte[] frame, int offset, List<byte[]> items)
		{
			if (items == null || items.Count == 0)
				return;
			int x = Get(items[0], 4), y = Get(items[0], 8), right = x + Get(items[0], 12), bottom = y + Get(items[0], 16);
			foreach (byte[] item in items)
			{
				int a = Get(item, 4), b = Get(item, 8);
				x = Math.Min(x, a);
				y = Math.Min(y, b);
				right = Math.Max(right, a + Get(item, 12));
				bottom = Math.Max(bottom, b + Get(item, 16));
			}
			Set(frame, offset, x);
			Set(frame, offset + 4, y);
			Set(frame, offset + 8, right - x);
			Set(frame, offset + 12, bottom - y);
		}
		static ParsedObject ParseObject(Parser p, byte[] old, ObjectType type)
		{
			ParsedObject obj = new ParsedObject();
			byte[] data = obj.Data;
			if (old != null)
			{
				Preserve(old, data, 4, 4);
				Preserve(old, data, 152, 124);
				Preserve(old, data, 596, 410);
				Preserve(old, data, 1126, 50);
				Preserve(old, data, 1176, 608);
				Preserve(old, data, 1788, 168);
				for (int j = 0; j < 4; j++)
				{
					int o = 276 + 80 * j;
					Preserve(old, data, o + 20, 4);
					Preserve(old, data, o + 28, 16);
					Preserve(old, data, o + 52, 28);
				}
				for (int j = 0; j < 400; j++)
				{
					int o = FramesOffset + j * FrameSize;
					Preserve(old, data, o + 32, 4);
					Preserve(old, data, o + 120, 8);
					Preserve(old, data, o + 204, 12);
					Preserve(old, data, o + 252, 44);
					Preserve(old, data, o + 344, 4);
					Preserve(old, data, o + 368, 8);
				}
			}
			Set(data, 1784, (int)type);
			while (p.More)
			{
				Token t = p.Next();
				if (t.Text == "<bmp_begin>")
				{
					while (p.More)
					{
						t = p.Next();
						if (t.Text == "<bmp_end>")
							break;
						if (t.Text.StartsWith("file", StringComparison.Ordinal))
						{
							for (int j = 0; j < 9; j++)
								p.Next();
							continue;
						}
						if (t.Text == "name:")
						{
							p.String(data, 152356, 12, "name", false);
							continue;
						}
						if (t.Text == "head:" || t.Text == "small:" || t.Text == "weapon_hit_sound:" || t.Text == "weapon_drop_sound:" ||
							t.Text == "weapon_broken_sound:")
						{
							p.Next();
							continue;
						}
						string name = t.Text.TrimEnd(':');
						var f = typeof(DataFile).GetField(name);
						if (f != null && f.FieldType == typeof(double))
							SetDouble(data, Offset<DataFile>(name), p.Double());
						else if (!IntField<DataFile>(p, t, data, 0))
							p.Unknown(t);
					}
				}
				else if (t.Text == "<weapon_strength_list>")
				{
					int entry = -1;
					while (p.More)
					{
						t = p.Next();
						if (t.Text == "<weapon_strength_list_end>")
							break;
						if (t.Text == "entry:")
						{
							if (++entry >= 4)
								throw new LoadException("More than 4 weapon strength entry is overflow");
							p.Next();
							p.String(data, 1006 + entry * 30, 30, "entry name", true);
						}
						else if (entry >= 0 && IntField<WeaponStrength>(p, t, data, 276 + entry * 80))
						{
						}
						else
							p.Unknown(t);
					}
				}
				else if (t.Text == "<frame>")
				{
					int id = p.Int();
					if (id < 0 || id >= 400)
						throw new LoadException("Frame id is out of range: " + id);
					int frame = FramesOffset + id * FrameSize;
					p.String(data, frame + 348, 20, "frame caption", true);
					var bdys = new List<byte[]>();
					var itrs = new List<byte[]>();
					while (p.More)
					{
						t = p.Next();
						if (t.Text == "<frame_end>")
						{
							data[frame] = 1;
							break;
						}
						if (t.Text == "bdy:")
							bdys.Add(ParsePoint<Bdy>(p, "bdy_end:"));
						else if (t.Text == "itr:")
							itrs.Add(ParsePoint<Itr>(p, "itr_end:"));
						else if (t.Text == "bpoint:")
							Buffer.BlockCopy(ParsePoint<Bpoint>(p, "bpoint_end:"), 0, data, frame + 128, 8);
						else if (t.Text == "cpoint:")
							Buffer.BlockCopy(ParsePoint<Cpoint>(p, "cpoint_end:"), 0, data, frame + 136, 68);
						else if (t.Text == "wpoint:")
							Buffer.BlockCopy(ParsePoint<Wpoint>(p, "wpoint_end:"), 0, data, frame + 216, 36);
						else if (t.Text == "opoint:")
							Buffer.BlockCopy(ParsePoint<Opoint>(p, "opoint_end:"), 0, data, frame + 88, 32);
						else if (t.Text == "sound:")
							p.Next();
						else if (!IntField<Frame>(p, t, data, frame))
							p.Unknown(t);
					}
					obj.Bdys[id] = bdys;
					obj.Itrs[id] = itrs;
					Set(data, frame + 296, itrs.Count);
					Set(data, frame + 300, bdys.Count);
					Bounds(data, frame + 312, itrs);
					Bounds(data, frame + 328, bdys);
				}
				else
					p.Unknown(t);
			}
			return obj;
		}
		static byte[] ParseBackground(Parser p, byte[] old)
		{
			byte[] data = new byte[BackgroundSize];
			if (old != null)
				Preserve(old, data, 2084, 364);
			int layer = -1;
			bool inLayer = false;
			while (p.More)
			{
				Token t = p.Next();
				string key = t.Text;
				if (key == "layer:")
				{
					if (++layer >= 30)
						throw new LoadException("Layer count " + (layer + 1) + " is overflow, it must be less or equal to 30");
					p.String(data, 32 + 30 * layer, 30, "layer bitmap path", false);
					inLayer = true;
				}
				else if (key == "layer_end")
				{
					Set(data, 28, layer + 1);
					inLayer = false;
				}
				else if (inLayer)
				{
					int offset = key == "transparency:" ? 1004
								 : key == "width:"		? 1124
								 : key == "x:"			? 1244
								 : key == "y:"			? 1364
								 : key == "height:"		? 1484
								 : key == "loop:"		? 1604
								 : key == "c1:"			? 1724
								 : key == "c2:"			? 1844
								 : key == "cc:"			? 1964
														: -1;
					if (offset < 0)
						p.Unknown(t);
					else
						Set(data, offset + 4 * layer, p.Int());
				}
				else if (key == "name:")
					p.String(data, 972, 32, "name", false);
				else if (key == "shadow:")
					p.String(data, 932, 40, "shadow", false);
				else if (key == "width:")
					Set(data, 0, p.Int());
				else if (key == "zboundary:")
				{
					Set(data, 4, p.Int());
					Set(data, 8, p.Int());
				}
				else if (key == "shadowsize:")
				{
					Set(data, 20, p.Int());
					Set(data, 24, p.Int());
				}
				else
					p.Unknown(t);
			}
			return data;
		}
		static byte[] ParseStages(Parser p, byte[] old)
		{
			byte[] data = new byte[StageSize * 60];
			for (int s = 0; s < 60; s++)
			{
				int stage = s * StageSize;
				Set(data, stage, -1);
				for (int i = 0; i < 100; i++)
				{
					int phase = stage + 8 + i * PhaseSize;
					Set(data, phase + 13496, -1);
					for (int j = 0; j < 60; j++)
					{
						int spawn = phase + 56 + j * SpawnSize;
						Set(data, spawn + 172, -1);
						if (old != null)
						{
							Preserve(old, data, spawn, 172);
							Preserve(old, data, spawn + 220, 4);
						}
					}
				}
			}
			int stageId = -1, phaseId = -1, spawnId = -1, stageAt = -1, phaseAt = -1, spawnAt = -1;
			bool stageOpen = false, phaseOpen = false;
			while (p.More)
			{
				Token t = p.Next();
				string key = t.Text;
				if (key == "<stage>")
				{
					if (stageOpen)
						p.Warning(string.Format("Stage recursion in line {0} at col {1}", t.Line, t.Column));
					stageOpen = true;
					stageId = -1;
					phaseId = -1;
					phaseOpen = false;
				}
				else if (key == "<stage_end>")
				{
					if (stageId < 0)
						throw new LoadException("Stage id could not be received");
					stageOpen = false;
				}
				else if (!stageOpen)
					continue;
				else if (key == "<phase>")
				{
					if (stageId < 0)
						throw new LoadException("Stage id could not be received");
					if (++phaseId >= 100)
						throw new LoadException("More than 100 phases in stage");
					phaseAt = stageAt + 8 + phaseId * PhaseSize;
					spawnId = -1;
					phaseOpen = true;
				}
				else if (key == "<phase_end>")
				{
					Set(data, stageAt, phaseId + 1);
					phaseOpen = false;
				}
				else if (!phaseOpen && key == "id:")
				{
					stageId = p.Int();
					if (stageId < 0 || stageId >= 60)
						throw new LoadException("Stage id is out of range: " + stageId);
					stageAt = stageId * StageSize;
				}
				else if (phaseOpen && key == "bound:")
					Set(data, phaseAt, p.Int());
				else if (phaseOpen && key == "music:")
					p.String(data, phaseAt + 4, 52, "phase background music (bgm)", false);
				else if (phaseOpen && key == "id:")
				{
					if (++spawnId >= 60)
						throw new LoadException("More than 60 spawns in phase");
					spawnAt = phaseAt + 56 + spawnId * SpawnSize;
					Set(data, spawnAt + 172, p.Int());
					Set(data, spawnAt + 176, 80 + Get(data, phaseAt));
					Set(data, spawnAt + 180, 500);
					Set(data, spawnAt + 184, 1);
					Set(data, spawnAt + 200, 9);
				}
				else if (phaseOpen && spawnId >= 0 && key == "<boss>")
					Set(data, spawnAt + 216, 2);
				else if (phaseOpen && spawnId >= 0 && key == "<soldier>")
				{
					Set(data, spawnAt + 216, 1);
					Set(data, spawnAt + 184, 50);
				}
				else if (phaseOpen && spawnId >= 0 && key == "when_clear_goto_phase:")
					Set(data, phaseAt + 13496, p.Int());
				else if (phaseOpen && spawnId >= 0 && key == "ratio:")
					SetDouble(data, spawnAt + 208, p.Double());
				else if (phaseOpen && spawnId >= 0)
					IntField<Spawn>(p, t, data, spawnAt);
			}
			return data;
		}
		static long FileManagerAddress(ProcessMemory mem)
		{
			uint address = unchecked((uint)mem.ReadInt32(FilesAddress));
			if (address == 0)
				throw new LoadException("Could not read process memory: LF2 is not started");
			return address;
		}
		static void WriteArray(ProcessMemory mem, byte[] data, byte[] old, int frame, List<byte[]> items, int countOffset, int ptrOffset,
							   int itemSize)
		{
			if (items == null || items.Count == 0)
				return;
			uint oldPtr = GetPointer(old, frame + ptrOffset);
			int oldCount = Get(old, frame + countOffset);
			long address = oldPtr != 0 && items.Count <= oldCount ? oldPtr : RemoteProcess.Alloc(mem, checked(items.Count * itemSize));
			byte[] bytes = new byte[items.Count * itemSize];
			for (int i = 0; i < items.Count; i++)
				Buffer.BlockCopy(items[i], 0, bytes, i * itemSize, itemSize);
			mem.Write(address, bytes);
			Set(data, frame + ptrOffset, unchecked((int)(uint)address));
		}
		public static int InstantLoad(string data, int procId, DataType dataType, int datId, ObjectType objType, Logger log)
		{
			try
			{
				Parser p = new Parser(data, log);
				using (ProcessMemory mem = ProcessMemory.Open(procId))
				{
					RemoteProcess.Suspend(procId);
					try
					{
						long files = FileManagerAddress(mem);
						if (dataType == DataType.Background)
						{
							if (datId < 0 || datId >= 50)
								throw new LoadException("Background index is out of range: " + datId);
							long address = files + 81026480L + datId * BackgroundSize;
							byte[] old = mem.Read(address, BackgroundSize);
							mem.Write(address, ParseBackground(p, old));
						}
						else if (dataType == DataType.Stage)
						{
							long address = files + 2000;
							byte[] old = mem.Read(address, StageSize * 60);
							mem.Write(address, ParseStages(p, old));
						}
						else if (dataType == DataType.Char)
						{
							if (datId < 0 || datId >= 500)
								throw new LoadException("Object index is out of range: " + datId);
							long address = unchecked((uint)mem.ReadInt32(files + datId * 4));
							if (address == 0)
								throw new LoadException("Object is not loaded: " + datId);
							byte[] old = mem.Read(address, DataSize);
							ParsedObject obj = ParseObject(p, old, objType);
							for (int i = 0; i < 400; i++)
							{
								int frame = FramesOffset + i * FrameSize;
								WriteArray(mem, obj.Data, old, frame, obj.Bdys[i], 300, 308, 40);
								WriteArray(mem, obj.Data, old, frame, obj.Itrs[i], 296, 304, 80);
							}
							mem.Write(address, obj.Data);
						}
						else
							throw new LoadException("Unknown data type: " + dataType);
					}
					finally
					{
						RemoteProcess.Resume(procId);
					}
				}
				return p.Warnings > 0 ? 1 : 0;
			}
			catch (LoadException ex)
			{
				if (log != null)
					log(ex.Message, null, MsgType.Error);
				return 2;
			}
			catch (System.ComponentModel.Win32Exception ex)
			{
				if (log != null)
					log("Could not access process: " + ex.Message, null, MsgType.Error);
				return 2;
			}
			catch (System.IO.IOException ex)
			{
				if (log != null)
					log("Could not access process memory: " + ex.Message, null, MsgType.Error);
				return 2;
			}
			catch (UnauthorizedAccessException ex)
			{
				if (log != null)
					log("Could not access process: " + ex.Message, null, MsgType.Error);
				return 2;
			}
			catch (Exception ex)
			{
				if (log != null)
					log(ex.ToString(), "Unhandled Error", MsgType.Error);
				return -1;
			}
		}
	}
}
