// C# CUDA simple framework. To run program, you need to have NVIDIA GPU.
// Compile with Visual Studio C# command-line: csc CUDA.cs

using System;
using System.Runtime.InteropServices;

class Program
{
	[DllImport("user32.dll")]
	static extern IntPtr CreateWindowEx(int dwExStyle, UInt16 regResult, string lpWindowName, UInt32 dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);	

	[DllImport("user32.dll")]
	static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	static extern bool DestroyWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	static extern IntPtr DispatchMessage([In] ref MSG lpmsg);	

	[DllImport("user32.dll")]
	static extern short GetAsyncKeyState(System.Int32 vKey);

	[DllImport("user32.dll")]
	static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("user32.dll")]
	static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);	

	[DllImport("user32.dll")]
	static extern bool PeekMessage(out MSG lpMsg, uint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

	[DllImport("user32.dll")]
	static extern void PostQuitMessage(int nExitCode);

	[DllImport("user32.dll")]
	static extern System.UInt16 RegisterClassEx([In] ref WNDCLASSEX lpWndClass);

	[DllImport("user32.dll")]
	static extern bool TranslateMessage([In] ref MSG lpMsg);

	[DllImport("gdi32.dll")]
	static extern int StretchDIBits(IntPtr hdc, int x, int y, int w, int h, int xs, int ys, int sw, int sh, IntPtr bs, [In] ref BITMAPINFO lpbmi, uint usage, uint rop);	

	[DllImport("nvcuda.dll")]
	static extern int cuInit(uint flags);

	[DllImport("nvcuda.dll")]
	static extern int cuDeviceGet(out IntPtr device, int ordinal);

	[DllImport("nvcuda.dll", EntryPoint="cuCtxCreate_v2")]
	static extern int cuCtxCreate(out IntPtr pctx, uint flags, IntPtr device);

	[DllImport("nvcuda.dll", EntryPoint = "cuMemAlloc_v2")]
	static extern int cuMemAlloc(out IntPtr dptr, uint bytesize);

	[DllImport("nvcuda.dll")]
	static extern int cuModuleLoadDataEx(out IntPtr module, IntPtr image, uint numOptions, uint options, uint optionValues);

	[DllImport("nvcuda.dll")]
	static extern int cuModuleGetFunction(out IntPtr hfunc, IntPtr hmod, string name);

	[DllImport("nvcuda.dll")]
	static extern int cuLaunchKernel(IntPtr f, uint gx, uint gy, uint gz, uint bx, uint by, uint bz, uint shared, IntPtr stream, IntPtr[] args, IntPtr[] extra);

	[DllImport("nvcuda.dll", EntryPoint = "cuMemcpyDtoH_v2")]
	static extern int cuMemcpyDtoH(IntPtr dstHost, IntPtr srcDevice, uint byteCount);

	[DllImport("nvcuda.dll", EntryPoint = "cuMemFree_v2")] 
	static extern int cuMemFree(IntPtr dptr);

	struct POINT { public Int32 x; public Int32 Y; } 

	struct MSG { public IntPtr hwnd; public UInt32 message; public UIntPtr wParam; public UIntPtr lParam; public UInt32 time; public POINT pt;}  

	struct WNDCLASSEX
	{
		public int cbSize;
		public int style;
		public IntPtr lpfnWndProc; 
		public int cbClsExtra;
		public int cbWndExtra;
		public IntPtr hInstance;
		public IntPtr hIcon;
		public IntPtr hCursor;
		public IntPtr hbrBackground;
		public string lpszMenuName;
		public string lpszClassName;
		public IntPtr hIconSm;
	}

	struct BITMAPINFOHEADER
	{
		public uint biSize;
		public int biWidth;
		public int biHeight;
		public ushort biPlanes;
		public ushort biBitCount;
		public int biCompression;
		public uint biSizeImage;
		public int biXPelsPerMeter;
		public int biYPelsPerMeter;
		public uint biClrUsed;
		public uint biClrImportant;
	}

	struct RGBQUAD
	{
		public byte rgbBlue;
		public byte rgbGreen;
		public byte rgbRed;
		public byte rgbReserved;
	}

	struct BITMAPINFO 
	{
		public BITMAPINFOHEADER bmiHeader;
		[MarshalAsAttribute( UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct )]
		public RGBQUAD[] bmiColors;
	}

	delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
	static WndProc WindowProcPointer = WindowProc;

	static IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		if (msg==0x0010 || msg==0x0002 || (msg==0x0100 && wParam.ToInt32()==0x1B))
		{
			PostQuitMessage(0); return IntPtr.Zero;
		}
		return DefWindowProc(hWnd, msg, wParam, lParam);
	}

	static void Main()
	{
		int width = 1280;
		int height = 720;
		IntPtr function, device;
		IntPtr zero = IntPtr.Zero;
		float start = Environment.TickCount * 0.001f;
		uint memory = (uint)(width * height * 4);
		cuInit(0);
		cuDeviceGet(out IntPtr cuDevice, 0);
		cuCtxCreate(out IntPtr context, 0, cuDevice);
		cuMemAlloc(out device, memory);
		byte[] source = System.Text.Encoding.ASCII.GetBytes(PTX);
		IntPtr moduleData = Marshal.AllocHGlobal(source.Length);
		Marshal.Copy(source, 0, moduleData, source.Length);
		cuModuleLoadDataEx(out IntPtr module, moduleData, 0, 0, 0);
		cuModuleGetFunction(out function, module, "mainImage");
		GCHandle handle = GCHandle.Alloc(device, GCHandleType.Pinned);
		IntPtr[] addresses = new IntPtr[2] {handle.AddrOfPinnedObject(), zero};
		IntPtr host = Marshal.AllocHGlobal((int)memory);
		IntPtr[] extra = new IntPtr[1];
		IntPtr time = Marshal.AllocHGlobal(sizeof(float));
		float[] array = new float[] {0.0f};
		bool exit = false;
		MSG msg;
		WNDCLASSEX win = new WNDCLASSEX();
		win.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
		win.style = (int) (1 | 2); 
		win.hbrBackground = (IntPtr) 1 + 1 ;
		win.cbClsExtra = 0;
		win.cbWndExtra = 0;
		win.hInstance = System.Diagnostics.Process.GetCurrentProcess().Handle;
		win.hIcon = zero;
		win.hCursor = LoadCursor(zero, (int)32515);
		win.lpszMenuName = null;
		win.lpszClassName = "Demo";
		win.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WindowProcPointer);
		win.hIconSm = zero;
		IntPtr hwnd = CreateWindowEx(0, RegisterClassEx(ref win), "Demo", 0xcf0000 | 0x10000000, 0, 0, width, height, zero, zero, win.hInstance, zero);
		IntPtr hdc = GetDC(hwnd);
		BITMAPINFO bmi = new BITMAPINFO();
		bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
		bmi.bmiHeader.biWidth = width;
		bmi.bmiHeader.biHeight = height;
		bmi.bmiHeader.biPlanes = 1;
		bmi.bmiHeader.biBitCount = 32;
		bmi.bmiHeader.biCompression = 0;
		bmi.bmiHeader.biSizeImage = memory;
		bmi.bmiHeader.biXPelsPerMeter = 0;
		bmi.bmiHeader.biYPelsPerMeter = 0;
		bmi.bmiHeader.biClrUsed = 0;
		bmi.bmiHeader.biClrImportant = 0;
		bmi.bmiColors = new RGBQUAD[1];
		while (!exit)
		{
			while(PeekMessage(out msg, 0, 0, 0, 0x0001))
			{
				if( msg.message == 0x0012 ) exit = true;
				TranslateMessage( ref msg );
				DispatchMessage( ref msg );
			}
			array[0] = (Environment.TickCount * 0.001f) - start;
			Marshal.Copy(array, 0, time, 1);
			addresses[1] = time;
			cuLaunchKernel(function, (uint)width/8, (uint)height/8, 1, 8, 8, 1, 0, zero, addresses, extra);
			cuMemcpyDtoH(host, device, memory);
			StretchDIBits(hdc, 0, 0, width, height, 0, 0, width, height, host, ref bmi, 0, 0x00CC0020);
		}
		handle.Free();
		cuMemFree(device);
		Marshal.FreeHGlobal(host);
		Marshal.FreeHGlobal(moduleData);
		Marshal.FreeHGlobal(time);
		DestroyWindow(hwnd);
	}

	static string PTX = 
	@"
		.version 8.5
		.target sm_52
		.address_size 64
		.visible .entry mainImage(.param .u64 _Z9mainImageP6uchar4f_param_0,.param .f32 _Z9mainImageP6uchar4f_param_1)
		{
			.reg .pred 	%p<2>;
			.reg .b16 	%rs<5>;
			.reg .f32 	%f<163>;
			.reg .b32 	%r<19>;
			.reg .b64 	%rd<5>;
			ld.param.u64 	%rd1, [_Z9mainImageP6uchar4f_param_0];
			ld.param.f32 	%f9, [_Z9mainImageP6uchar4f_param_1];
			mov.u32 	%r7, %ctaid.x;
			mov.u32 	%r8, %ntid.x;
			mov.u32 	%r9, %tid.x;
			mad.lo.s32 	%r1, %r7, %r8, %r9;
			mov.u32 	%r10, %ntid.y;
			mov.u32 	%r11, %ctaid.y;
			mov.u32 	%r12, %tid.y;
			mad.lo.s32 	%r13, %r11, %r10, %r12;
			mul.lo.s32 	%r2, %r13, 1280;
			cvt.rn.f32.u32 	%f12, %r1;
			cvt.rn.f32.u32 	%f13, %r13;
			fma.rn.f32 	%f14, %f12, 0f40000000, 0fC4A00000;
			div.rn.f32 	%f1, %f14, 0f44340000;
			fma.rn.f32 	%f15, %f13, 0f40000000, 0fC4340000;
			div.rn.f32 	%f2, %f15, 0f44340000;
			mov.f32 	%f161, 0f00000000;
			mov.u32 	%r18, 1;
			mov.f32 	%f162, %f161;
			bra.uni 	$L__BB0_1;
		$L__BB0_2:
			cvt.rn.f32.s32 	%f83, %r4;
			mul.f32 	%f84, %f83, 0f439BD99A;
			fma.rn.f32 	%f85, %f83, 0f42FE3333, %f84;
			mul.f32 	%f86, %f83, 0f43374CCD;
			fma.rn.f32 	%f87, %f83, 0f4386C000, %f86;
			add.f32 	%f88, %f85, 0fBFC90FDB;
			div.rn.f32 	%f89, %f88, 0f40C90FDB;
			cvt.rmi.f32.f32 	%f90, %f89;
			sub.f32 	%f91, %f89, %f90;
			fma.rn.f32 	%f92, %f91, 0f40000000, 0fBF800000;
			abs.f32 	%f93, %f92;
			mul.f32 	%f94, %f93, %f93;
			add.f32 	%f95, %f93, %f93;
			sub.f32 	%f97, %f29, %f95;
			mul.f32 	%f98, %f94, %f97;
			fma.rn.f32 	%f99, %f98, 0f40000000, 0fBF800000;
			mul.f32 	%f100, %f99, 0f472AEE8C;
			cvt.rmi.f32.f32 	%f101, %f100;
			sub.f32 	%f102, %f100, %f101;
			add.f32 	%f103, %f87, 0fBFC90FDB;
			div.rn.f32 	%f104, %f103, 0f40C90FDB;
			cvt.rmi.f32.f32 	%f105, %f104;
			sub.f32 	%f106, %f104, %f105;
			fma.rn.f32 	%f107, %f106, 0f40000000, 0fBF800000;
			abs.f32 	%f108, %f107;
			mul.f32 	%f109, %f108, %f108;
			add.f32 	%f110, %f108, %f108;
			sub.f32 	%f111, %f29, %f110;
			mul.f32 	%f112, %f109, %f111;
			fma.rn.f32 	%f113, %f112, 0f40000000, 0fBF800000;
			mul.f32 	%f114, %f113, 0f472AEE8C;
			cvt.rmi.f32.f32 	%f115, %f114;
			sub.f32 	%f116, %f114, %f115;
			fma.rn.f32 	%f117, %f102, %f9, 0f3F800000;
			add.f32 	%f118, %f117, 0f3FC90FDB;
			add.f32 	%f119, %f118, 0fBFC90FDB;
			div.rn.f32 	%f120, %f119, 0f40C90FDB;
			cvt.rmi.f32.f32 	%f121, %f120;
			sub.f32 	%f122, %f120, %f121;
			fma.rn.f32 	%f123, %f122, 0f40000000, 0fBF800000;
			abs.f32 	%f124, %f123;
			mul.f32 	%f125, %f124, %f124;
			add.f32 	%f126, %f124, %f124;
			sub.f32 	%f127, %f29, %f126;
			mul.f32 	%f128, %f125, %f127;
			fma.rn.f32 	%f129, %f128, 0f40000000, 0fBF800000;
			fma.rn.f32 	%f130, %f116, %f9, 0f3F800000;
			add.f32 	%f131, %f130, 0f3FC90FDB;
			add.f32 	%f132, %f131, 0fBFC90FDB;
			div.rn.f32 	%f133, %f132, 0f40C90FDB;
			cvt.rmi.f32.f32 	%f134, %f133;
			sub.f32 	%f135, %f133, %f134;
			fma.rn.f32 	%f136, %f135, 0f40000000, 0fBF800000;
			abs.f32 	%f137, %f136;
			mul.f32 	%f138, %f137, %f137;
			add.f32 	%f139, %f137, %f137;
			sub.f32 	%f140, %f29, %f139;
			mul.f32 	%f141, %f138, %f140;
			fma.rn.f32 	%f142, %f141, 0f40000000, 0fBF800000;
			sub.f32 	%f143, %f1, %f129;
			sub.f32 	%f144, %f2, %f142;
			mul.f32 	%f145, %f144, %f144;
			fma.rn.f32 	%f146, %f143, %f143, %f145;
			sqrt.rn.f32 	%f147, %f146;
			div.rn.f32 	%f149, %f81, %f147;
			fma.rn.f32 	%f162, %f102, %f149, %f5;
			fma.rn.f32 	%f161, %f149, %f116, %f6;
			add.s32 	%r18, %r4, 1;
		$L__BB0_1:
			cvt.rn.f32.s32 	%f16, %r18;
			mul.f32 	%f17, %f16, 0f439BD99A;
			fma.rn.f32 	%f18, %f16, 0f42FE3333, %f17;
			mul.f32 	%f19, %f16, 0f43374CCD;
			fma.rn.f32 	%f20, %f16, 0f4386C000, %f19;
			add.f32 	%f21, %f18, 0fBFC90FDB;
			div.rn.f32 	%f22, %f21, 0f40C90FDB;
			cvt.rmi.f32.f32 	%f23, %f22;
			sub.f32 	%f24, %f22, %f23;
			fma.rn.f32 	%f25, %f24, 0f40000000, 0fBF800000;
			abs.f32 	%f26, %f25;
			mul.f32 	%f27, %f26, %f26;
			add.f32 	%f28, %f26, %f26;
			mov.f32 	%f29, 0f40400000;
			sub.f32 	%f30, %f29, %f28;
			mul.f32 	%f31, %f27, %f30;
			fma.rn.f32 	%f32, %f31, 0f40000000, 0fBF800000;
			mul.f32 	%f33, %f32, 0f472AEE8C;
			cvt.rmi.f32.f32 	%f34, %f33;
			sub.f32 	%f35, %f33, %f34;
			add.f32 	%f36, %f20, 0fBFC90FDB;
			div.rn.f32 	%f37, %f36, 0f40C90FDB;
			cvt.rmi.f32.f32 	%f38, %f37;
			sub.f32 	%f39, %f37, %f38;
			fma.rn.f32 	%f40, %f39, 0f40000000, 0fBF800000;
			abs.f32 	%f41, %f40;
			mul.f32 	%f42, %f41, %f41;
			add.f32 	%f43, %f41, %f41;
			sub.f32 	%f44, %f29, %f43;
			mul.f32 	%f45, %f42, %f44;
			fma.rn.f32 	%f46, %f45, 0f40000000, 0fBF800000;
			mul.f32 	%f47, %f46, 0f472AEE8C;
			cvt.rmi.f32.f32 	%f48, %f47;
			sub.f32 	%f49, %f47, %f48;
			fma.rn.f32 	%f50, %f35, %f9, 0f3F800000;
			add.f32 	%f51, %f50, 0f3FC90FDB;
			add.f32 	%f52, %f51, 0fBFC90FDB;
			div.rn.f32 	%f53, %f52, 0f40C90FDB;
			cvt.rmi.f32.f32 	%f54, %f53;
			sub.f32 	%f55, %f53, %f54;
			fma.rn.f32 	%f56, %f55, 0f40000000, 0fBF800000;
			abs.f32 	%f57, %f56;
			mul.f32 	%f58, %f57, %f57;
			add.f32 	%f59, %f57, %f57;
			sub.f32 	%f60, %f29, %f59;
			mul.f32 	%f61, %f58, %f60;
			fma.rn.f32 	%f62, %f61, 0f40000000, 0fBF800000;
			fma.rn.f32 	%f63, %f49, %f9, 0f3F800000;
			add.f32 	%f64, %f63, 0f3FC90FDB;
			add.f32 	%f65, %f64, 0fBFC90FDB;
			div.rn.f32 	%f66, %f65, 0f40C90FDB;
			cvt.rmi.f32.f32 	%f67, %f66;
			sub.f32 	%f68, %f66, %f67;
			fma.rn.f32 	%f69, %f68, 0f40000000, 0fBF800000;
			abs.f32 	%f70, %f69;
			mul.f32 	%f71, %f70, %f70;
			add.f32 	%f72, %f70, %f70;
			sub.f32 	%f73, %f29, %f72;
			mul.f32 	%f74, %f71, %f73;
			fma.rn.f32 	%f75, %f74, 0f40000000, 0fBF800000;
			sub.f32 	%f76, %f1, %f62;
			sub.f32 	%f77, %f2, %f75;
			mul.f32 	%f78, %f77, %f77;
			fma.rn.f32 	%f79, %f76, %f76, %f78;
			sqrt.rn.f32 	%f80, %f79;
			mov.f32 	%f81, 0f3C75C28F;
			div.rn.f32 	%f82, %f81, %f80;
			fma.rn.f32 	%f5, %f35, %f82, %f162;
			fma.rn.f32 	%f6, %f82, %f49, %f161;
			add.s32 	%r4, %r18, 1;
			setp.eq.s32 	%p1, %r4, 64;
			@%p1 bra 	$L__BB0_3;
			bra.uni 	$L__BB0_2;
		$L__BB0_3:
			add.s32 	%r14, %r2, %r1;
			mul.f32 	%f150, %f6, 0f437F0000;
			mov.f32 	%f151, 0f437F0000;
			min.f32 	%f152, %f150, %f151;
			mov.f32 	%f153, 0f00000000;
			max.f32 	%f154, %f152, %f153;
			mul.f32 	%f155, %f5, 0f437F0000;
			min.f32 	%f156, %f155, %f151;
			max.f32 	%f157, %f156, %f153;
			mul.f32 	%f158, %f155, %f6;
			min.f32 	%f159, %f158, %f151;
			max.f32 	%f160, %f159, %f153;
			cvt.rzi.u32.f32 	%r15, %f154;
			cvt.rzi.u32.f32 	%r16, %f157;
			cvt.rzi.u32.f32 	%r17, %f160;
			cvta.to.global.u64 	%rd2, %rd1;
			mul.wide.u32 	%rd3, %r14, 4;
			add.s64 	%rd4, %rd2, %rd3;
			cvt.u16.u32 	%rs1, %r16;
			cvt.u16.u32 	%rs2, %r15;
			cvt.u16.u32 	%rs3, %r17;
			mov.u16 	%rs4, 255;
			st.global.v4.u8 	[%rd4], {%rs2, %rs1, %rs3, %rs4};
			ret;
		}
	";
}

/* Assembly was generated by command:

nvcc -ptx test.cu

In generated PTX change function name to "mainImage"

*/

/* test.cu:

__device__ float Fract(float x)
{
	return x - floorf(x);
}

__device__ float Clamp(float x, float a, float b) 
{
	return max(min(x, b), a);
}

__device__ float Length(float x, float y) 
{
	return sqrtf(x * x + y * y);
}

__device__ float Sinus (float x)
{
	float q = fabs(Fract((x - 0.017453292f * 90.0f) / (0.017453292f * 360.0f)) * 2.0f - 1.0f);     
	return q * q * (3.0f - 2.0f * q) * 2.0f - 1.0f;
}

__device__ float Cosinus (float x)
{
	return Sinus(x + 1.57079632679f);
}

__global__ void mainImage(uchar4 *fragColor, float iTime)
{
	int width = 1280;
	int height = 720;
	unsigned int x = blockIdx.x * blockDim.x + threadIdx.x;
	unsigned int y = blockIdx.y * blockDim.y + threadIdx.y;
	unsigned int i = x + width * y;
	float2 resolution = make_float2((float)width, (float)height);
	float2 fragCoord = make_float2((float)x, (float)y);
	float uvx = (2.0f * fragCoord.x - resolution.x) / resolution.y;
	float uvy = (2.0f * fragCoord.y - resolution.y) / resolution.y;
	float kx = 0.0f;
	float ky = 0.0f;
	for (int i = 1; i < 64; i++)
	{
		float qx = i * 127.1f + i * 311.7f;
		float qy = i * 269.5f + i * 183.3f;
		float hx = Fract(Sinus(qx) * 43758.5453f);
		float hy = Fract(Sinus(qy) * 43758.5453f);
		float px = Cosinus(hx * iTime + 1.0f);
		float py = Cosinus(hy * iTime + 1.0f);
		float d = 0.015f / Length(uvx - px, uvy - py);
		kx += d * hx;
		ky += d * hy;
	}
	float b = Clamp(255.0f * ky, 0.0f, 255.0f);
	float g = Clamp(255.0f * kx, 0.0f, 255.0f);
	float r = Clamp(255.0f * kx * ky, 0.0f, 255.0f);
	fragColor[i] = make_uchar4(b, g, r, 255);
}
*/