using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace LF2.IDE
{
	// Process suspension and allocations for the 32-bit LF2 address space.
	internal static class RemoteProcess
	{
		const uint ProcessAllAccess = 0x001F0FFF;

		public static void Suspend(int pid)
		{
			if (Platform.IsWindows)
				WithHandle(pid, delegate(IntPtr handle) {
					if (NtSuspendProcess(handle) != 0) throw new InvalidOperationException("Failed to suspend process.");
				});
			else
			{
				if (kill(pid, 19) != 0) throw new Win32Exception(Marshal.GetLastWin32Error());
				var deadline = System.Diagnostics.Stopwatch.StartNew();
				while (!IsStopped(pid))
				{
					if (deadline.ElapsedMilliseconds > 5000)
					{
						kill(pid, 18);
						throw new TimeoutException("Timed out suspending process.");
					}
					System.Threading.Thread.Sleep(1);
				}
			}
		}

		public static void Resume(int pid)
		{
			if (Platform.IsWindows)
				WithHandle(pid, delegate(IntPtr handle) {
					if (NtResumeProcess(handle) != 0) throw new InvalidOperationException("Failed to resume process.");
				});
			else if (kill(pid, 18) != 0) throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public static long Alloc(ProcessMemory mem, int size)
		{
			if (size <= 0) throw new ArgumentOutOfRangeException("size");
			if (Platform.IsWindows)
			{
				long result = 0;
				WithHandle(mem.Pid, delegate(IntPtr handle) {
					IntPtr address = VirtualAllocEx(handle, IntPtr.Zero, new IntPtr(size), 0x3000, 4);
					if (address == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
					result = unchecked((uint)address.ToInt64());
					if (result >= 0x80000000L)
					{
						VirtualFreeEx(handle, address, IntPtr.Zero, 0x8000);
						throw new InvalidOperationException("Remote allocation is outside the low 32-bit address space.");
					}
				});
				return result;
			}
			return AllocateLinux(mem, size);
		}

		static void WithHandle(int pid, Action<IntPtr> action)
		{
			IntPtr handle = OpenProcess(ProcessAllAccess, false, pid);
			if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
			try { action(handle); }
			finally { CloseHandle(handle); }
		}

		static long AllocateLinux(ProcessMemory mem, int size)
		{
			int pid = mem.Pid;
			bool stopped = IsStopped(pid);
			if (IntPtr.Size != 8) throw new PlatformNotSupportedException("Linux remote mmap injection requires a 64-bit IDE process.");
			bool attached = false, haveRegs = false;
			Registers original = new Registers();
			IntPtr regs = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Registers)));
			try
			{
				Trace(pid, 16, IntPtr.Zero, IntPtr.Zero);
				attached = true;
				WaitForStop(pid);
				Trace(pid, 12, IntPtr.Zero, regs);
				original = (Registers)Marshal.PtrToStructure(regs, typeof(Registers));
				haveRegs = true;
				long gadget = FindSyscall(mem, false);
				Registers call = original;
				call.rip = (ulong)gadget;
				call.cs = 0x33;
				call.rax = 9; call.orig_rax = ulong.MaxValue;
				call.rdi = 0; call.rsi = (ulong)size; call.rdx = 3;
				call.r10 = 0x62; call.r8 = ulong.MaxValue; call.r9 = 0;
				bool compat = false;
				Marshal.StructureToPtr(call, regs, false);
				try { Trace(pid, 13, IntPtr.Zero, regs); }
				catch (Win32Exception)
				{
					if ((original.cs & 0xffff) != 0x23) throw;
					compat = true;
					gadget = FindSyscall(mem, true);
					call = original;
					call.rip = (ulong)gadget; call.orig_rax = ulong.MaxValue;
					call.rax = 192; call.rbx = 0x40000000; call.rcx = (ulong)size;
					call.rdx = 3; call.rsi = 0x22; call.rdi = uint.MaxValue; call.rbp = 0;
					Marshal.StructureToPtr(call, regs, false);
					Trace(pid, 13, IntPtr.Zero, regs);
				}
				int signal;
				do
				{
					Trace(pid, 9, IntPtr.Zero, IntPtr.Zero);
					signal = WaitForStop(pid);
					if (signal != 5 && signal != 19)
						throw new InvalidOperationException("Unexpected signal during remote allocation: " + signal);
				} while (signal == 19);
				Trace(pid, 12, IntPtr.Zero, regs);
				Registers result = (Registers)Marshal.PtrToStructure(regs, typeof(Registers));
				if (result.rip != (ulong)gadget + 2)
					throw new InvalidOperationException("Remote syscall did not complete.");
				long address = compat ? unchecked((int)result.rax) : unchecked((long)result.rax);
				if (address < 0 && address >= -4095) throw new Win32Exception((int)-address, "Remote mmap failed");
				if (address <= 0 || address >= 0x80000000L) throw new InvalidOperationException("Remote mmap returned an address outside the 32-bit low address space.");
				return address;
			}
			finally
			{
				try
				{
					if (attached)
					{
						try
						{
							if (haveRegs)
							{
								Marshal.StructureToPtr(original, regs, false);
								Trace(pid, 13, IntPtr.Zero, regs);
							}
						}
						finally { Trace(pid, 17, IntPtr.Zero, stopped ? new IntPtr(19) : IntPtr.Zero); }
					}
				}
				finally { Marshal.FreeHGlobal(regs); }
			}
		}

		static long FindSyscall(ProcessMemory memory, bool compat)
		{
			int pid = memory.Pid;
				foreach (string line in File.ReadLines("/proc/" + pid + "/maps"))
				{
					string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length < 2 || parts[1] != "r-xp") continue;
					string[] range = parts[0].Split('-');
					long start = long.Parse(range[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					long end = long.Parse(range[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					byte[] buffer = new byte[65536];
					bool previous = false;
					for (long pos = start; pos < end; )
					{
						int count = (int)Math.Min(buffer.Length, end - pos);
						try { memory.Read(pos, buffer, 0, count); }
						catch (IOException) { break; }
						for (int i = 0; i < count; i++)
						{
							if (previous && buffer[i] == (compat ? 0x80 : 0x05)) return pos + i - 1;
							previous = buffer[i] == (compat ? 0xcd : 0x0f);
						}
						pos += count;
					}
				}

			throw new InvalidOperationException("No syscall instruction found in the target process.");
		}

		static int WaitForStop(int pid)
		{
			int status;
			int result;
			do { result = waitpid(pid, out status, 0); } while (result < 0 && Marshal.GetLastWin32Error() == 4);
			if (result != pid) throw new Win32Exception(Marshal.GetLastWin32Error());
			if ((status & 0xff) != 0x7f) throw new InvalidOperationException("Target process exited during remote allocation.");
			return (status >> 8) & 0xff;
		}

		static void Trace(int pid, int request, IntPtr address, IntPtr data)
		{
			if (ptrace(request, pid, address, data) == new IntPtr(-1))
				throw new Win32Exception(Marshal.GetLastWin32Error(), "ptrace failed (request " + request + ")");
		}

		static bool IsStopped(int pid)
		{
			foreach (string line in File.ReadLines("/proc/" + pid + "/status"))
				if (line.StartsWith("State:", StringComparison.Ordinal))
				{
					string state = line.Substring(6).TrimStart();
					return state.StartsWith("T", StringComparison.Ordinal) || state.StartsWith("t", StringComparison.Ordinal);
				}
			throw new InvalidOperationException("Cannot read target process state.");
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Registers
		{
			public ulong r15, r14, r13, r12, rbp, rbx, r11, r10, r9, r8;
			public ulong rax, rcx, rdx, rsi, rdi, orig_rax, rip, cs, eflags, rsp, ss;
			public ulong fs_base, gs_base, ds, es, fs, gs;
		}

		[DllImport("libc", SetLastError = true)] static extern int kill(int pid, int signal);
		[DllImport("libc", SetLastError = true)] static extern IntPtr ptrace(int request, int pid, IntPtr address, IntPtr data);
		[DllImport("libc", SetLastError = true)] static extern int waitpid(int pid, out int status, int options);
		[DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
		[DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, IntPtr size, uint allocationType, uint protection);
		[DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualFreeEx(IntPtr process, IntPtr address, IntPtr size, uint freeType);
		[DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
		[DllImport("ntdll.dll")] static extern int NtSuspendProcess(IntPtr process);
		[DllImport("ntdll.dll")] static extern int NtResumeProcess(IntPtr process);
	}
}
