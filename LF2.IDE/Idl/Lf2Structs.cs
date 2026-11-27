using System;
using System.Runtime.InteropServices;

namespace LF2.IDE
{
	// All addresses in these layouts are 32-bit LF2 addresses, not host pointers.
	[StructLayout(LayoutKind.Explicit, Size = 32)]
	internal struct Opoint
	{
		[FieldOffset(0)]
		public int kind;
		[FieldOffset(4)]
		public int x;
		[FieldOffset(8)]
		public int y;
		[FieldOffset(12)]
		public int action;
		[FieldOffset(16)]
		public int dvx;
		[FieldOffset(20)]
		public int dvy;
		[FieldOffset(24)]
		public int oid;
		[FieldOffset(28)]
		public int facing;
	}
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	internal struct Bpoint
	{
		[FieldOffset(0)]
		public int x;
		[FieldOffset(4)]
		public int y;
	}
	[StructLayout(LayoutKind.Explicit, Size = 68)]
	internal struct Cpoint
	{
		[FieldOffset(0)]
		public int kind;
		[FieldOffset(4)]
		public int x;
		[FieldOffset(8)]
		public int y;
		[FieldOffset(12)]
		public int injury;
		[FieldOffset(12)]
		public int fronthurtact;
		[FieldOffset(16)]
		public int cover;
		[FieldOffset(16)]
		public int backhurtact;
		[FieldOffset(20)]
		public int vaction;
		[FieldOffset(24)]
		public int aaction;
		[FieldOffset(28)]
		public int jaction;
		[FieldOffset(32)]
		public int daction;
		[FieldOffset(36)]
		public int throwvx;
		[FieldOffset(40)]
		public int throwvy;
		[FieldOffset(44)]
		public int hurtable;
		[FieldOffset(48)]
		public int decrease;
		[FieldOffset(52)]
		public int dircontrol;
		[FieldOffset(56)]
		public int taction;
		[FieldOffset(60)]
		public int throwinjury;
		[FieldOffset(64)]
		public int throwvz;
	}
	[StructLayout(LayoutKind.Explicit, Size = 36)]
	internal struct Wpoint
	{
		[FieldOffset(0)]
		public int kind;
		[FieldOffset(4)]
		public int x;
		[FieldOffset(8)]
		public int y;
		[FieldOffset(12)]
		public int weaponact;
		[FieldOffset(16)]
		public int attacking;
		[FieldOffset(20)]
		public int cover;
		[FieldOffset(24)]
		public int dvx;
		[FieldOffset(28)]
		public int dvy;
		[FieldOffset(32)]
		public int dvz;
	}
	[StructLayout(LayoutKind.Explicit, Size = 80)]
	internal struct Itr
	{
		[FieldOffset(0)]
		public int kind;
		[FieldOffset(4)]
		public int x;
		[FieldOffset(8)]
		public int y;
		[FieldOffset(12)]
		public int w;
		[FieldOffset(16)]
		public int h;
		[FieldOffset(20)]
		public int dvx;
		[FieldOffset(24)]
		public int dvy;
		[FieldOffset(28)]
		public int fall;
		[FieldOffset(32)]
		public int arest;
		[FieldOffset(36)]
		public int vrest;
		[FieldOffset(40)]
		public int unknown1;
		[FieldOffset(44)]
		public int effect;
		[FieldOffset(48)]
		public int catchingact1;
		[FieldOffset(52)]
		public int catchingact2;
		[FieldOffset(56)]
		public int caughtact1;
		[FieldOffset(60)]
		public int caughtact2;
		[FieldOffset(64)]
		public int bdefend;
		[FieldOffset(68)]
		public int injury;
		[FieldOffset(72)]
		public int zwidth;
		[FieldOffset(76)]
		public int unknown2;
	}
	[StructLayout(LayoutKind.Explicit, Size = 40)]
	internal struct Bdy
	{
		[FieldOffset(0)]
		public int kind;
		[FieldOffset(4)]
		public int x;
		[FieldOffset(8)]
		public int y;
		[FieldOffset(12)]
		public int w;
		[FieldOffset(16)]
		public int h;
		[FieldOffset(20)]
		public int unknown1;
		[FieldOffset(24)]
		public int unknown2;
		[FieldOffset(28)]
		public int unknown3;
		[FieldOffset(32)]
		public int unknown4;
		[FieldOffset(36)]
		public int unknown5;
	}
	[StructLayout(LayoutKind.Explicit, Size = 376)]
	internal unsafe struct Frame
	{
		[FieldOffset(0)]
		public byte exists;
		[FieldOffset(4)]
		public int pic;
		[FieldOffset(8)]
		public int state;
		[FieldOffset(12)]
		public int wait;
		[FieldOffset(16)]
		public int next;
		[FieldOffset(20)]
		public int dvx;
		[FieldOffset(24)]
		public int dvy;
		[FieldOffset(28)]
		public int dvz;
		[FieldOffset(32)]
		public int unknown1;
		[FieldOffset(36)]
		public int hit_a;
		[FieldOffset(40)]
		public int hit_d;
		[FieldOffset(44)]
		public int hit_j;
		[FieldOffset(48)]
		public int hit_Fa;
		[FieldOffset(52)]
		public int hit_Ua;
		[FieldOffset(56)]
		public int hit_Da;
		[FieldOffset(60)]
		public int hit_Fj;
		[FieldOffset(64)]
		public int hit_Uj;
		[FieldOffset(68)]
		public int hit_Dj;
		[FieldOffset(72)]
		public int hit_ja;
		[FieldOffset(76)]
		public int mp;
		[FieldOffset(80)]
		public int centerx;
		[FieldOffset(84)]
		public int centery;
		[FieldOffset(88)]
		public Opoint opoint;
		[FieldOffset(120)]
		public int unknown2;
		[FieldOffset(124)]
		public int unknown3;
		[FieldOffset(128)]
		public Bpoint bpoint;
		[FieldOffset(136)]
		public Cpoint cpoint;
		[FieldOffset(204)]
		public int unknown4;
		[FieldOffset(208)]
		public int unknown5;
		[FieldOffset(212)]
		public int unknown6;
		[FieldOffset(216)]
		public Wpoint wpoint;
		[FieldOffset(252)]
		public fixed int unknown7[11];
		[FieldOffset(296)]
		public int itr_count;
		[FieldOffset(300)]
		public int bdy_count;
		[FieldOffset(304)]
		public uint itrs;
		[FieldOffset(308)]
		public uint bdys;
		[FieldOffset(312)]
		public int itr_x;
		[FieldOffset(316)]
		public int itr_y;
		[FieldOffset(320)]
		public int itr_w;
		[FieldOffset(324)]
		public int itr_h;
		[FieldOffset(328)]
		public int bdy_x;
		[FieldOffset(332)]
		public int bdy_y;
		[FieldOffset(336)]
		public int bdy_w;
		[FieldOffset(340)]
		public int bdy_h;
		[FieldOffset(344)]
		public int unknown8;
		[FieldOffset(348)]
		public fixed byte fname[20];
		[FieldOffset(368)]
		public uint sound;
		[FieldOffset(372)]
		public int unknown9;
	}
	[StructLayout(LayoutKind.Explicit, Size = 80)]
	internal unsafe struct WeaponStrength
	{
		[FieldOffset(0)]
		public int dvx;
		[FieldOffset(4)]
		public int dvy;
		[FieldOffset(8)]
		public int fall;
		[FieldOffset(12)]
		public int arest;
		[FieldOffset(16)]
		public int vrest;
		[FieldOffset(20)]
		public int unknown1;
		[FieldOffset(24)]
		public int effect;
		[FieldOffset(28)]
		public fixed int unknown2[4];
		[FieldOffset(44)]
		public int bdefend;
		[FieldOffset(48)]
		public int injury;
		[FieldOffset(52)]
		public fixed byte unknown3[28];
	}
	[StructLayout(LayoutKind.Explicit, Size = 152368)]
	internal unsafe struct DataFile
	{
		[FieldOffset(0)]
		public int walking_frame_rate;
		[FieldOffset(4)]
		public int unknown1;
		[FieldOffset(8)]
		public double walking_speed;
		[FieldOffset(16)]
		public double walking_speedz;
		[FieldOffset(24)]
		public int running_frame_rate;
		[FieldOffset(32)]
		public double running_speed;
		[FieldOffset(40)]
		public double running_speedz;
		[FieldOffset(48)]
		public double heavy_walking_speed;
		[FieldOffset(56)]
		public double heavy_walking_speedz;
		[FieldOffset(64)]
		public double heavy_running_speed;
		[FieldOffset(72)]
		public double heavy_running_speedz;
		[FieldOffset(80)]
		public double jump_height;
		[FieldOffset(88)]
		public double jump_distance;
		[FieldOffset(96)]
		public double jump_distancez;
		[FieldOffset(104)]
		public double dash_height;
		[FieldOffset(112)]
		public double dash_distance;
		[FieldOffset(120)]
		public double dash_distancez;
		[FieldOffset(128)]
		public double rowing_height;
		[FieldOffset(136)]
		public double rowing_distance;
		[FieldOffset(144)]
		public int weapon_hp;
		[FieldOffset(148)]
		public int weapon_drop_hurt;
		[FieldOffset(152)]
		public fixed byte unknown2[124];
		[FieldOffset(276)]
		public fixed byte weapon_strength_list[320];
		[FieldOffset(596)]
		public fixed byte unknown3[410];
		[FieldOffset(1006)]
		public fixed byte entry_names[120];
		[FieldOffset(1126)]
		public fixed byte unknown4[50];
		[FieldOffset(1176)]
		public int pic_count;
		[FieldOffset(1180)]
		public fixed byte pic_bmps[400];
		[FieldOffset(1580)]
		public fixed int pic_index[10];
		[FieldOffset(1620)]
		public fixed int pic_width[10];
		[FieldOffset(1660)]
		public fixed int pic_height[10];
		[FieldOffset(1700)]
		public fixed int pic_row[10];
		[FieldOffset(1740)]
		public fixed int pic_col[10];
		[FieldOffset(1780)]
		public int id;
		[FieldOffset(1784)]
		public int type;
		[FieldOffset(1788)]
		public int unknown5;
		[FieldOffset(1792)]
		public fixed byte small_bmp[40];
		[FieldOffset(1832)]
		public int unknown6;
		[FieldOffset(1836)]
		public fixed byte face_bmp[40];
		[FieldOffset(1876)]
		public fixed int unknown7[20];
		[FieldOffset(1956)]
		public fixed byte frames[150400];
		[FieldOffset(152356)]
		public fixed byte name[12];
	}
	[StructLayout(LayoutKind.Explicit, Size = 880)]
	internal unsafe struct Lf2Object
	{
		[FieldOffset(0)]
		public int move_counter;
		[FieldOffset(4)]
		public int run_counter;
		[FieldOffset(8)]
		public int blink;
		[FieldOffset(12)]
		public int unknown1;
		[FieldOffset(16)]
		public int x;
		[FieldOffset(20)]
		public int y;
		[FieldOffset(24)]
		public int z;
		[FieldOffset(28)]
		public fixed byte unknown2[12];
		[FieldOffset(40)]
		public double x_acceleration;
		[FieldOffset(48)]
		public double y_acceleration;
		[FieldOffset(56)]
		public double z_acceleration;
		[FieldOffset(64)]
		public double x_velocity;
		[FieldOffset(72)]
		public double y_velocity;
		[FieldOffset(80)]
		public double z_velocity;
		[FieldOffset(88)]
		public double x_real;
		[FieldOffset(96)]
		public double y_real;
		[FieldOffset(104)]
		public double z_real;
		[FieldOffset(112)]
		public int frame1;
		[FieldOffset(116)]
		public int frame2;
		[FieldOffset(120)]
		public int frame3;
		[FieldOffset(124)]
		public int frame4;
		[FieldOffset(128)]
		public byte facing;
		[FieldOffset(129)]
		public fixed byte unknown3[15];
		[FieldOffset(144)]
		public int ccatcher;
		[FieldOffset(148)]
		public int ctimer;
		[FieldOffset(152)]
		public int weapon_type;
		[FieldOffset(156)]
		public int weapon_held;
		[FieldOffset(160)]
		public int weapon_holder;
		[FieldOffset(164)]
		public int unknown4;
		[FieldOffset(168)]
		public fixed byte unknown5[8];
		[FieldOffset(176)]
		public int fall;
		[FieldOffset(180)]
		public int shake;
		[FieldOffset(184)]
		public int bdefend;
		[FieldOffset(188)]
		public fixed byte unknown6[10];
		[FieldOffset(198)]
		public byte holding_up;
		[FieldOffset(199)]
		public byte holding_down;
		[FieldOffset(200)]
		public byte holding_left;
		[FieldOffset(201)]
		public byte holding_right;
		[FieldOffset(202)]
		public byte holding_a;
		[FieldOffset(203)]
		public byte holding_j;
		[FieldOffset(204)]
		public byte holding_d;
		[FieldOffset(205)]
		public byte up;
		[FieldOffset(206)]
		public byte down;
		[FieldOffset(207)]
		public byte left;
		[FieldOffset(208)]
		public byte right;
		[FieldOffset(209)]
		public byte A;
		[FieldOffset(210)]
		public byte J;
		[FieldOffset(211)]
		public byte D;
		[FieldOffset(212)]
		public byte DrA;
		[FieldOffset(213)]
		public byte DlA;
		[FieldOffset(214)]
		public byte DuA;
		[FieldOffset(215)]
		public byte DdA;
		[FieldOffset(216)]
		public byte DrJ;
		[FieldOffset(217)]
		public byte DlJ;
		[FieldOffset(218)]
		public byte DuJ;
		[FieldOffset(219)]
		public byte DdJ;
		[FieldOffset(220)]
		public byte DJA;
		[FieldOffset(221)]
		public fixed byte unknown7[15];
		[FieldOffset(236)]
		public int arest;
		[FieldOffset(240)]
		public int vrest;
		[FieldOffset(244)]
		public fixed byte unknown8[396];
		[FieldOffset(640)]
		public int attacked_object_num;
		[FieldOffset(644)]
		public fixed byte unknown9[112];
		[FieldOffset(756)]
		public int clone;
		[FieldOffset(760)]
		public int weapon_thrower;
		[FieldOffset(764)]
		public int hp;
		[FieldOffset(768)]
		public int dark_hp;
		[FieldOffset(772)]
		public int max_hp;
		[FieldOffset(776)]
		public int mp;
		[FieldOffset(780)]
		public int reserve;
		[FieldOffset(784)]
		public int unknown10;
		[FieldOffset(788)]
		public int unknown11;
		[FieldOffset(792)]
		public int pic_gain;
		[FieldOffset(796)]
		public int bottle_hp;
		[FieldOffset(800)]
		public fixed byte unknown12[24];
		[FieldOffset(824)]
		public int firzen_counter;
		[FieldOffset(828)]
		public int unknown13;
		[FieldOffset(832)]
		public int armour_multiplier;
		[FieldOffset(836)]
		public int unknown14;
		[FieldOffset(840)]
		public int total_attack;
		[FieldOffset(844)]
		public int hp_lost;
		[FieldOffset(848)]
		public int mp_usage;
		[FieldOffset(852)]
		public int unknown15;
		[FieldOffset(856)]
		public int kills;
		[FieldOffset(860)]
		public int weapon_picks;
		[FieldOffset(864)]
		public int enemy;
		[FieldOffset(868)]
		public int team;
		[FieldOffset(872)]
		public uint data;
	}
	[StructLayout(LayoutKind.Explicit, Size = 224)]
	internal unsafe struct Spawn
	{
		[FieldOffset(0)]
		public fixed int unknown1[43];
		[FieldOffset(172)]
		public int id;
		[FieldOffset(176)]
		public int x;
		[FieldOffset(180)]
		public int hp;
		[FieldOffset(184)]
		public int times;
		[FieldOffset(188)]
		public int reserve;
		[FieldOffset(192)]
		public int join;
		[FieldOffset(196)]
		public int join_reserve;
		[FieldOffset(200)]
		public int act;
		[FieldOffset(204)]
		public int y;
		[FieldOffset(208)]
		public double ratio;
		[FieldOffset(216)]
		public int role;
		[FieldOffset(220)]
		public int unknown2;
	}
	[StructLayout(LayoutKind.Explicit, Size = 13504)]
	internal unsafe struct Phase
	{
		[FieldOffset(0)]
		public int bound;
		[FieldOffset(4)]
		public fixed byte music[52];
		[FieldOffset(56)]
		public fixed byte spawns[13440];
		[FieldOffset(13496)]
		public int when_clear_goto_phase;
	}
	[StructLayout(LayoutKind.Explicit, Size = 1350408)]
	internal unsafe struct Stage
	{
		[FieldOffset(0)]
		public int phase_count;
		[FieldOffset(8)]
		public fixed byte phases[1350400];
	}
	[StructLayout(LayoutKind.Explicit, Size = 2448)]
	internal unsafe struct Background
	{
		[FieldOffset(0)]
		public int bg_width;
		[FieldOffset(4)]
		public int zboundary1;
		[FieldOffset(8)]
		public int zboundary2;
		[FieldOffset(12)]
		public int perspective1;
		[FieldOffset(16)]
		public int perspective2;
		[FieldOffset(20)]
		public int shadowsize1;
		[FieldOffset(24)]
		public int shadowsize2;
		[FieldOffset(28)]
		public int layer_count;
		[FieldOffset(32)]
		public fixed byte layer_bmps[900];
		[FieldOffset(932)]
		public fixed byte shadow_bmp[40];
		[FieldOffset(972)]
		public fixed byte name[32];
		[FieldOffset(1004)]
		public fixed int transparency[30];
		[FieldOffset(1124)]
		public fixed int layer_width[30];
		[FieldOffset(1244)]
		public fixed int layer_x[30];
		[FieldOffset(1364)]
		public fixed int layer_y[30];
		[FieldOffset(1484)]
		public fixed int layer_height[30];
		[FieldOffset(1604)]
		public fixed int layer_loop[30];
		[FieldOffset(1724)]
		public fixed int layer_c1[30];
		[FieldOffset(1844)]
		public fixed int layer_c2[30];
		[FieldOffset(1964)]
		public fixed int layer_cc[30];
		[FieldOffset(2084)]
		public fixed int unknown[91];
	}
	[StructLayout(LayoutKind.Explicit, Size = 81024480)]
	internal unsafe struct StageProxy
	{
		[FieldOffset(0)]
		public fixed byte stages[81024480];
	}
	[StructLayout(LayoutKind.Explicit, Size = 81148880)]
	internal unsafe struct FileManager
	{
		[FieldOffset(0)]
		public fixed uint datas[500];
		[FieldOffset(2000)]
		public fixed byte stages[81024480];
		[FieldOffset(81026480)]
		public fixed byte backgrounds[122400];
	}
	[StructLayout(LayoutKind.Explicit, Size = 2008)]
	internal unsafe struct Game
	{
		[FieldOffset(0)]
		public int state;
		[FieldOffset(4)]
		public fixed byte exists[400];
		[FieldOffset(404)]
		public fixed uint objects[400];
		[FieldOffset(2004)]
		public uint files;
	}
}
