using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace LF2.IDE
{
	/// <summary>
	/// Reads and writes another process's memory: ReadProcessMemory/WriteProcessMemory on Windows,
	/// /proc/&lt;pid&gt;/mem on Linux (LF2 running under Wine). On Linux the kernel only allows this for
	/// child processes unless kernel.yama.ptrace_scope is 0, so start the game from LF2 IDE.
	/// </summary>
	public sealed class ProcessMemory : IDisposable
	{
		public readonly int Pid;
		readonly IntPtr handle;      // Windows
		readonly FileStream mem;     // Linux

		ProcessMemory(int pid, IntPtr handle, FileStream mem)
		{
			Pid = pid;
			this.handle = handle;
			this.mem = mem;
		}

		/// <summary>Opens the process for reading and (if <paramref name="write"/>) writing. Throws on failure.</summary>
		public static ProcessMemory Open(int pid, bool write = true)
		{
			if (Platform.IsWindows)
			{
				const uint PROCESS_VM_READ = 0x10, PROCESS_VM_WRITE = 0x20, PROCESS_VM_OPERATION = 0x8, PROCESS_QUERY_INFORMATION = 0x400;
				uint access = PROCESS_VM_READ | PROCESS_QUERY_INFORMATION | (write ? PROCESS_VM_WRITE | PROCESS_VM_OPERATION : 0);
				IntPtr h = OpenProcess(access, false, pid);
				if (h == IntPtr.Zero)
					throw new Win32Exception();
				return new ProcessMemory(pid, h, null);
			}
			try
			{
				return new ProcessMemory(pid, IntPtr.Zero,
					new FileStream("/proc/" + pid + "/mem", FileMode.Open, write ? FileAccess.ReadWrite : FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.None));
			}
			catch (UnauthorizedAccessException ex)
			{
				throw new UnauthorizedAccessException("Cannot access the memory of process " + pid + ". Wine may start the game outside LF2 IDE's process tree, so Linux can block memory access. " +
					"Allow it with: sudo sysctl kernel.yama.ptrace_scope=0", ex);
			}
		}

		public void Read(long address, byte[] buffer, int offset, int count)
		{
			if (mem == null)
			{
				IntPtr done;
				var tmp = offset == 0 ? buffer : new byte[count];
				if (!ReadProcessMemory(handle, new IntPtr(address), tmp, new IntPtr(count), out done) || done.ToInt64() != count)
					throw new IOException(string.Format("Cannot read {0} bytes at 0x{1:X8} of process {2}", count, address, Pid));
				if (tmp != buffer)
					Buffer.BlockCopy(tmp, 0, buffer, offset, count);
				return;
			}
			mem.Seek(address, SeekOrigin.Begin);
			int read = 0;
			while (read < count)
			{
				int n = mem.Read(buffer, offset + read, count - read);
				if (n <= 0)
					throw new IOException(string.Format("Cannot read {0} bytes at 0x{1:X8} of process {2}", count, address, Pid));
				read += n;
			}
		}

		public byte[] Read(long address, int count)
		{
			var buffer = new byte[count];
			Read(address, buffer, 0, count);
			return buffer;
		}

		public void Write(long address, byte[] data, int offset, int count)
		{
			if (mem == null)
			{
				IntPtr done;
				var tmp = offset == 0 ? data : new byte[count];
				if (tmp != data)
					Buffer.BlockCopy(data, offset, tmp, 0, count);
				if (!WriteProcessMemory(handle, new IntPtr(address), tmp, new IntPtr(count), out done) || done.ToInt64() != count)
					throw new IOException(string.Format("Cannot write {0} bytes at 0x{1:X8} of process {2}", count, address, Pid));
				return;
			}
			mem.Seek(address, SeekOrigin.Begin);
			mem.Write(data, offset, count);
			mem.Flush();
		}

		public void Write(long address, byte[] data) { Write(address, data, 0, data.Length); }

		public byte ReadByte(long address) { return Read(address, 1)[0]; }
		public short ReadInt16(long address) { return BitConverter.ToInt16(Read(address, 2), 0); }
		public int ReadInt32(long address) { return BitConverter.ToInt32(Read(address, 4), 0); }
		public void WriteInt32(long address, int value) { Write(address, BitConverter.GetBytes(value)); }

		public void Dispose()
		{
			if (mem != null)
				mem.Dispose();
			else if (handle != IntPtr.Zero)
				CloseHandle(handle);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr OpenProcess(uint access, bool inheritHandle, int pid);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool ReadProcessMemory(IntPtr process, IntPtr address, [Out] byte[] buffer, IntPtr size, out IntPtr read);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] buffer, IntPtr size, out IntPtr written);

		[DllImport("kernel32.dll")]
		static extern bool CloseHandle(IntPtr handle);
	}
}
