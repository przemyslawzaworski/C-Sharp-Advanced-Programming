// C# OpenGL simple framework
// Compile with Visual Studio C# command-line: csc OpenGL.cs

using System;
using System.Runtime.InteropServices;

class Program
{

//////////////////////////////// Load functions from DLLs

	[DllImport("user32.dll")]
	static extern bool PeekMessage(out MSG lpMsg, uint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
	
	[DllImport("user32.dll")]
	static extern short GetAsyncKeyState(System.Int32 vKey);

	[DllImport("user32.dll")]
	static extern IntPtr GetDC( IntPtr hWnd);

	[DllImport("user32.dll")]
	static extern bool DestroyWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	static extern IntPtr CreateWindowEx(int dwExStyle, UInt16 regResult, string lpWindowName, UInt32 dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

	[DllImport("user32.dll")]
	static extern System.UInt16 RegisterClassEx([In] ref WNDCLASSEX lpWndClass);

	[DllImport("user32.dll")]
	static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	static extern void PostQuitMessage(int nExitCode);

	[DllImport("user32.dll")]
	static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

	[DllImport("user32.dll")]
	static extern bool TranslateMessage([In] ref MSG lpMsg);

	[DllImport("user32.dll")]
	static extern IntPtr DispatchMessage([In] ref MSG lpmsg);	
	
	[DllImport("gdi32.dll")]
	static extern int ChoosePixelFormat(IntPtr hdc, [In] ref PIXELFORMATDESCRIPTOR ppfd);

	[DllImport("gdi32.dll")]
	static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PIXELFORMATDESCRIPTOR ppfd);	
	   
	[DllImport("opengl32.dll")]
	static extern bool wglSwapLayerBuffers(IntPtr param0, uint param1);   
	   
	[DllImport("opengl32.dll")]
	static extern void glRects(short x1, short y1, short x2, short y2 );

	[DllImport("opengl32.dll")]
	public static extern IntPtr wglGetProcAddress(string name);

	[DllImport("opengl32.dll")]
	static extern IntPtr wglCreateContext(IntPtr hDC);

	[DllImport("opengl32.dll")]
	static extern bool wglMakeCurrent(IntPtr hDC, IntPtr hglrc);

	[DllImport("opengl32.dll")]
	static extern bool wglDeleteContext(IntPtr hglrc);
	
	[DllImport("opengl32.dll")]
	static extern IntPtr glGetString(uint name);

//////////////////////////////// Declare delegates

	delegate void PFNWGLSWAPINTERVALEXTPROC (int interval);
	static PFNWGLSWAPINTERVALEXTPROC wglSwapIntervalEXT;
	
	delegate uint PFNGLCREATEPROGRAMPROC ();
	static PFNGLCREATEPROGRAMPROC glCreateProgram;
	
	delegate uint PFNGLCREATESHADERPROC(int type);
	static PFNGLCREATESHADERPROC glCreateShader;
	
	delegate void PFNGLSHADERSOURCEPROC(uint shader, int count, string[] source, int[] length);
	static PFNGLSHADERSOURCEPROC glShaderSource;	

	delegate void PFNGLCOMPILESHADERPROC(uint shader);
	static PFNGLCOMPILESHADERPROC glCompileShader;

	delegate void PFNGLATTACHSHADERPROC(uint program, uint shader);
	static PFNGLATTACHSHADERPROC glAttachShader;

	delegate void PFNGLLINKPROGRAMPROC(uint program);
	static PFNGLLINKPROGRAMPROC glLinkProgram;

	delegate void PFNGLUSEPROGRAMPROC(uint program);
	static PFNGLUSEPROGRAMPROC glUseProgram;

	delegate int PFNGLGETUNIFORMLOCATIONPROC(uint program, string name);
	static PFNGLGETUNIFORMLOCATIONPROC glGetUniformLocation;

	delegate void PFNGLUNIFORM1FPROC(int location, float v0);
	static PFNGLUNIFORM1FPROC glUniform1f;
	
	delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
	static WndProc WindowProcPointer = WindowProc;

//////////////////////////////// Declare structures	

	struct PIXELFORMATDESCRIPTOR { public ushort nSize; public ushort nVersion; public uint dwFlags; }
	 
	struct MSG { public IntPtr hwnd; public UInt32 message; public UIntPtr wParam; public UIntPtr lParam; public UInt32 time; public POINT pt;}  

	struct POINT { public Int32 x; public Int32 Y; } 
	
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

//////////////////////////////// Get delegates for OpenGL function pointers	

	static void glInit ()
	{
		glCreateProgram = Marshal.GetDelegateForFunctionPointer<PFNGLCREATEPROGRAMPROC>(wglGetProcAddress("glCreateProgram"));	
		wglSwapIntervalEXT = Marshal.GetDelegateForFunctionPointer<PFNWGLSWAPINTERVALEXTPROC>(wglGetProcAddress("wglSwapIntervalEXT"));
		glCreateShader = Marshal.GetDelegateForFunctionPointer<PFNGLCREATESHADERPROC>(wglGetProcAddress("glCreateShader"));
		glShaderSource = Marshal.GetDelegateForFunctionPointer<PFNGLSHADERSOURCEPROC>(wglGetProcAddress("glShaderSource"));
		glCompileShader = Marshal.GetDelegateForFunctionPointer<PFNGLCOMPILESHADERPROC>(wglGetProcAddress("glCompileShader"));	
		glAttachShader = Marshal.GetDelegateForFunctionPointer<PFNGLATTACHSHADERPROC>(wglGetProcAddress("glAttachShader"));
		glLinkProgram = Marshal.GetDelegateForFunctionPointer<PFNGLLINKPROGRAMPROC>(wglGetProcAddress("glLinkProgram"));	
		glUseProgram = Marshal.GetDelegateForFunctionPointer<PFNGLUSEPROGRAMPROC>(wglGetProcAddress("glUseProgram"));	
		glGetUniformLocation = Marshal.GetDelegateForFunctionPointer<PFNGLGETUNIFORMLOCATIONPROC>(wglGetProcAddress("glGetUniformLocation"));		
		glUniform1f = Marshal.GetDelegateForFunctionPointer<PFNGLUNIFORM1FPROC>(wglGetProcAddress("glUniform1f"));		
	}

//////////////////////////////// GLSL Fragment Shader

	static string FragmentShader = 
		@"
		#version 450 
		layout (location=0)
		out vec4 fragColor;
		uniform float iTime;

		const mat3 rotationMatrix = mat3(1.0,0.0,0.0,0.0,0.47,-0.88,0.0,0.88,0.47);

		float hash(float p)
		{
			uint x = uint(p  + 16777041.);
			x = 1103515245U*((x >> 1U)^(x));
			uint h32 = 1103515245U*((x)^(x>>3U));
			uint n =  h32^(h32 >> 16);
			return float(n)*(1.0/float(0xffffffffU));
		}

		float noise( vec3 x )
		{
			vec3 p = floor(x);
			vec3 f = fract(x);
			f = f*f*(3.0-2.0*f);
			float n = p.x + p.y*57.0 + 113.0*p.z;
			return mix(mix(mix( hash(n+0.0), hash(n+1.0),f.x),mix( hash(n+57.0 ), hash(n+58.0 ),f.x),f.y),
				mix(mix( hash(n+113.0), hash(n+114.0),f.x),mix( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
		} 

		vec4 map( vec3 p )
		{
			float d = 0.2 - p.y;
			vec3 q = p  - vec3(0.0,1.0,0.0)*iTime;
			float f  = 0.50000*noise( q ); q = q*2.02 - vec3(0.0,1.0,0.0)*iTime;
			f += 0.25000*noise( q ); q = q*2.03 - vec3(0.0,1.0,0.0)*iTime;
			f += 0.12500*noise( q ); q = q*2.01 - vec3(0.0,1.0,0.0)*iTime;
			f += 0.06250*noise( q ); q = q*2.02 - vec3(0.0,1.0,0.0)*iTime;
			f += 0.03125*noise( q );
			d = clamp( d + 4.5*f, 0.0, 1.0 );
			vec3 col = mix( vec3(1.0,0.9,0.8), vec3(0.4,0.1,0.1), d ) + 0.05*sin(p);
			return vec4( col, d );
		}

		vec3 raymarch( vec3 ro, vec3 rd )
		{
			vec4 s = vec4( 0,0,0,0 );
			float t = 0.0;
			for( int i=0; i<128; i++ )
			{
				if( s.a > 0.99 ) break;
				vec3 p = ro + t*rd;
				vec4 k = map( p );
				k.rgb *= mix( vec3(3.0,1.5,0.15), vec3(0.5,0.5,0.5), clamp( (p.y-0.2)/2.0, 0.0, 1.0 ) );
				k.a *= 0.5;
				k.rgb *= k.a;
				s = s + k*(1.0-s.a);
				t += 0.05;
			}
			return clamp( s.xyz, 0.0, 1.0 );
		}

		void main ()
		{
			vec2 iResolution = vec2(800, 480);
			vec3 ro = vec3(0.0,4.9,-40.);
			vec3 rd = normalize(vec3((2.0*gl_FragCoord.xy-iResolution.xy)/iResolution.y,2.0)) * rotationMatrix;
			vec3 volume = raymarch( ro, rd );
			volume = volume*0.5 + 0.5*volume*volume*(3.0-2.0*volume);
			fragColor = vec4( volume, 1.0 );
		}
		";

//////////////////////////////// Main function with WindowProc callback

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
		bool exit = false;
		MSG msg;
		WNDCLASSEX win = new WNDCLASSEX();
		win.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
		win.style = (int) (1 | 2 ); 
		win.hbrBackground = (IntPtr) 1 +1 ;
		win.cbClsExtra = 0;
		win.cbWndExtra = 0;
		win.hInstance = System.Diagnostics.Process.GetCurrentProcess().Handle;
		win.hIcon = IntPtr.Zero;
		win.hCursor = LoadCursor(IntPtr.Zero, (int)32515);
		win.lpszMenuName = null;
		win.lpszClassName = "Demo";
		win.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WindowProcPointer);
		win.hIconSm = IntPtr.Zero;
		IntPtr hwnd = CreateWindowEx(0, RegisterClassEx(ref win), "Demo", 0xcf0000 | 0x10000000, 0, 0, 800, 480, IntPtr.Zero, IntPtr.Zero, win.hInstance, IntPtr.Zero);	
		IntPtr hdc = GetDC(hwnd);
		PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR();
		pfd.dwFlags = 0x00000001u;
		SetPixelFormat(hdc, ChoosePixelFormat(hdc, ref pfd), ref pfd);
		wglMakeCurrent(hdc, wglCreateContext(hdc));
		Console.WriteLine("Graphics Processing Unit: " + Marshal.PtrToStringAnsi(glGetString(0x1F01)));
		glInit();
		wglSwapIntervalEXT(0);
		uint p = glCreateProgram();
		uint s = glCreateShader(0x8B30);
		glShaderSource(s, 1, new[]{ FragmentShader }, new[]{ FragmentShader.Length });
		glCompileShader(s);
		glAttachShader(p, s);
		glLinkProgram(p);
		glUseProgram(p);
		int time = glGetUniformLocation(p, "iTime");
		float start = Environment.TickCount * 0.001f;
		while (!exit)
		{
			while(PeekMessage(out msg, 0, 0, 0, 0x0001))
			{
				if( msg.message == 0x0012 ) exit = true;
				TranslateMessage( ref msg );
				DispatchMessage( ref msg );
			}
			glUniform1f(time, (Environment.TickCount * 0.001f) - start);
			glRects(-1, -1, 1, 1); 
			wglSwapLayerBuffers(hdc, 0x00000001);
		}
		DestroyWindow(hwnd);
	}
}