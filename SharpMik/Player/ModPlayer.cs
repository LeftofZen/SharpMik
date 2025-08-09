using SharpMik.Common;
using System;

namespace SharpMik.Player
{
	/*
	 * This file is a mishmash of player functions and mod update and effects.
	 * 
	 * The best thing to happen to this file would be to split into effects and player
	 * and making the whole thing not built on statics.
	 * 3600+ lines of code in 1 class ftw!
	 */
	public static class ModPlayer
	{
		const int HIGH_OCTAVE = 2; /* number of above-range octaves */
		const ushort LOGFAC = 2 * 16;

		delegate int effectDelegate(ushort tick, ushort flags, MpControl a, Module mod, short channel);

		public enum PlayerState
		{
			Started,
			Stopped,
			Updated,
		}

		public delegate void PlayerStateChangedEvent(PlayerState state);

		public static event PlayerStateChangedEvent PlayStateChangedHandle;

		public static Module s_Module { get; internal set; }

		public static bool SetFixedRandom { get; set; }

		#region static data tables

		static readonly ushort[] oldperiods = [
									0x6b00, 0x6800, 0x6500, 0x6220, 0x5f50, 0x5c80,
									0x5a00, 0x5740, 0x54d0, 0x5260, 0x5010, 0x4dc0,
									0x4b90, 0x4960, 0x4750, 0x4540, 0x4350, 0x4160,
									0x3f90, 0x3dc0, 0x3c10, 0x3a40, 0x38b0, 0x3700
									];

		static readonly byte[] VibratoTable = [
									0, 24, 49, 74, 97,120,141,161,180,197,212,224,235,244,250,253,
									255,253,250,244,235,224,212,197,180,161,141,120, 97, 74, 49, 24
									];

		static readonly byte[] avibtab = [
									0, 1, 3, 4, 6, 7, 9,10,12,14,15,17,18,20,21,23,
									24,25,27,28,30,31,32,34,35,36,38,39,40,41,42,44,
									45,46,47,48,49,50,51,52,53,54,54,55,56,57,57,58,
									59,59,60,60,61,61,62,62,62,63,63,63,63,63,63,63,
									64,63,63,63,63,63,63,63,62,62,62,61,61,60,60,59,
									59,58,57,57,56,55,54,54,53,52,51,50,49,48,47,46,
									45,44,42,41,40,39,38,36,35,34,32,31,30,28,27,25,
									24,23,21,20,18,17,15,14,12,10, 9, 7, 6, 4, 3, 1
								];

		static readonly uint[] lintab = [
									535232,534749,534266,533784,533303,532822,532341,531861,
									531381,530902,530423,529944,529466,528988,528511,528034,
									527558,527082,526607,526131,525657,525183,524709,524236,
									523763,523290,522818,522346,521875,521404,520934,520464,
									519994,519525,519057,518588,518121,517653,517186,516720,
									516253,515788,515322,514858,514393,513929,513465,513002,
									512539,512077,511615,511154,510692,510232,509771,509312,
									508852,508393,507934,507476,507018,506561,506104,505647,
									505191,504735,504280,503825,503371,502917,502463,502010,
									501557,501104,500652,500201,499749,499298,498848,498398,
									497948,497499,497050,496602,496154,495706,495259,494812,
									494366,493920,493474,493029,492585,492140,491696,491253,
									490809,490367,489924,489482,489041,488600,488159,487718,
									487278,486839,486400,485961,485522,485084,484647,484210,
									483773,483336,482900,482465,482029,481595,481160,480726,
									480292,479859,479426,478994,478562,478130,477699,477268,
									476837,476407,475977,475548,475119,474690,474262,473834,
									473407,472979,472553,472126,471701,471275,470850,470425,
									470001,469577,469153,468730,468307,467884,467462,467041,
									466619,466198,465778,465358,464938,464518,464099,463681,
									463262,462844,462427,462010,461593,461177,460760,460345,
									459930,459515,459100,458686,458272,457859,457446,457033,
									456621,456209,455797,455386,454975,454565,454155,453745,
									453336,452927,452518,452110,451702,451294,450887,450481,
									450074,449668,449262,448857,448452,448048,447644,447240,
									446836,446433,446030,445628,445226,444824,444423,444022,
									443622,443221,442821,442422,442023,441624,441226,440828,
									440430,440033,439636,439239,438843,438447,438051,437656,
									437261,436867,436473,436079,435686,435293,434900,434508,
									434116,433724,433333,432942,432551,432161,431771,431382,
									430992,430604,430215,429827,429439,429052,428665,428278,
									427892,427506,427120,426735,426350,425965,425581,425197,
									424813,424430,424047,423665,423283,422901,422519,422138,
									421757,421377,420997,420617,420237,419858,419479,419101,
									418723,418345,417968,417591,417214,416838,416462,416086,
									415711,415336,414961,414586,414212,413839,413465,413092,
									412720,412347,411975,411604,411232,410862,410491,410121,
									409751,409381,409012,408643,408274,407906,407538,407170,
									406803,406436,406069,405703,405337,404971,404606,404241,
									403876,403512,403148,402784,402421,402058,401695,401333,
									400970,400609,400247,399886,399525,399165,398805,398445,
									398086,397727,397368,397009,396651,396293,395936,395579,
									395222,394865,394509,394153,393798,393442,393087,392733,
									392378,392024,391671,391317,390964,390612,390259,389907,
									389556,389204,388853,388502,388152,387802,387452,387102,
									386753,386404,386056,385707,385359,385012,384664,384317,
									383971,383624,383278,382932,382587,382242,381897,381552,
									381208,380864,380521,380177,379834,379492,379149,378807,
									378466,378124,377783,377442,377102,376762,376422,376082,
									375743,375404,375065,374727,374389,374051,373714,373377,
									373040,372703,372367,372031,371695,371360,371025,370690,
									370356,370022,369688,369355,369021,368688,368356,368023,
									367691,367360,367028,366697,366366,366036,365706,365376,
									365046,364717,364388,364059,363731,363403,363075,362747,
									362420,362093,361766,361440,361114,360788,360463,360137,
									359813,359488,359164,358840,358516,358193,357869,357547,
									357224,356902,356580,356258,355937,355616,355295,354974,
									354654,354334,354014,353695,353376,353057,352739,352420,
									352103,351785,351468,351150,350834,350517,350201,349885,
									349569,349254,348939,348624,348310,347995,347682,347368,
									347055,346741,346429,346116,345804,345492,345180,344869,
									344558,344247,343936,343626,343316,343006,342697,342388,
									342079,341770,341462,341154,340846,340539,340231,339924,
									339618,339311,339005,338700,338394,338089,337784,337479,
									337175,336870,336566,336263,335959,335656,335354,335051,
									334749,334447,334145,333844,333542,333242,332941,332641,
									332341,332041,331741,331442,331143,330844,330546,330247,
									329950,329652,329355,329057,328761,328464,328168,327872,
									327576,327280,326985,326690,326395,326101,325807,325513,
									325219,324926,324633,324340,324047,323755,323463,323171,
									322879,322588,322297,322006,321716,321426,321136,320846,
									320557,320267,319978,319690,319401,319113,318825,318538,
									318250,317963,317676,317390,317103,316817,316532,316246,
									315961,315676,315391,315106,314822,314538,314254,313971,
									313688,313405,313122,312839,312557,312275,311994,311712,
									311431,311150,310869,310589,310309,310029,309749,309470,
									309190,308911,308633,308354,308076,307798,307521,307243,
									306966,306689,306412,306136,305860,305584,305308,305033,
									304758,304483,304208,303934,303659,303385,303112,302838,
									302565,302292,302019,301747,301475,301203,300931,300660,
									300388,300117,299847,299576,299306,299036,298766,298497,
									298227,297958,297689,297421,297153,296884,296617,296349,
									296082,295815,295548,295281,295015,294749,294483,294217,
									293952,293686,293421,293157,292892,292628,292364,292100,
									291837,291574,291311,291048,290785,290523,290261,289999,
									289737,289476,289215,288954,288693,288433,288173,287913,
									287653,287393,287134,286875,286616,286358,286099,285841,
									285583,285326,285068,284811,284554,284298,284041,283785,
									283529,283273,283017,282762,282507,282252,281998,281743,
									281489,281235,280981,280728,280475,280222,279969,279716,
									279464,279212,278960,278708,278457,278206,277955,277704,
									277453,277203,276953,276703,276453,276204,275955,275706,
									275457,275209,274960,274712,274465,274217,273970,273722,
									273476,273229,272982,272736,272490,272244,271999,271753,
									271508,271263,271018,270774,270530,270286,270042,269798,
									269555,269312,269069,268826,268583,268341,268099,267857
								];

		static readonly ushort[] logtab = [
								(LOGFAC*907),(LOGFAC*900),(LOGFAC*894),(LOGFAC*887),
								(LOGFAC*881),(LOGFAC*875),(LOGFAC*868),(LOGFAC*862),
								(LOGFAC*856),(LOGFAC*850),(LOGFAC*844),(LOGFAC*838),
								(LOGFAC*832),(LOGFAC*826),(LOGFAC*820),(LOGFAC*814),
								(LOGFAC*808),(LOGFAC*802),(LOGFAC*796),(LOGFAC*791),
								(LOGFAC*785),(LOGFAC*779),(LOGFAC*774),(LOGFAC*768),
								(LOGFAC*762),(LOGFAC*757),(LOGFAC*752),(LOGFAC*746),
								(LOGFAC*741),(LOGFAC*736),(LOGFAC*730),(LOGFAC*725),
								(LOGFAC*720),(LOGFAC*715),(LOGFAC*709),(LOGFAC*704),
								(LOGFAC*699),(LOGFAC*694),(LOGFAC*689),(LOGFAC*684),
								(LOGFAC*678),(LOGFAC*675),(LOGFAC*670),(LOGFAC*665),
								(LOGFAC*660),(LOGFAC*655),(LOGFAC*651),(LOGFAC*646),
								(LOGFAC*640),(LOGFAC*636),(LOGFAC*632),(LOGFAC*628),
								(LOGFAC*623),(LOGFAC*619),(LOGFAC*614),(LOGFAC*610),
								(LOGFAC*604),(LOGFAC*601),(LOGFAC*597),(LOGFAC*592),
								(LOGFAC*588),(LOGFAC*584),(LOGFAC*580),(LOGFAC*575),
								(LOGFAC*570),(LOGFAC*567),(LOGFAC*563),(LOGFAC*559),
								(LOGFAC*555),(LOGFAC*551),(LOGFAC*547),(LOGFAC*543),
								(LOGFAC*538),(LOGFAC*535),(LOGFAC*532),(LOGFAC*528),
								(LOGFAC*524),(LOGFAC*520),(LOGFAC*516),(LOGFAC*513),
								(LOGFAC*508),(LOGFAC*505),(LOGFAC*502),(LOGFAC*498),
								(LOGFAC*494),(LOGFAC*491),(LOGFAC*487),(LOGFAC*484),
								(LOGFAC*480),(LOGFAC*477),(LOGFAC*474),(LOGFAC*470),
								(LOGFAC*467),(LOGFAC*463),(LOGFAC*460),(LOGFAC*457),
								(LOGFAC*453),(LOGFAC*450),(LOGFAC*447),(LOGFAC*443),
								(LOGFAC*440),(LOGFAC*437),(LOGFAC*434),(LOGFAC*431)
							];

		static readonly sbyte[] PanbrelloTable = [
								  0,  2,  3,  5,  6,  8,  9, 11, 12, 14, 16, 17, 19, 20, 22, 23,
								 24, 26, 27, 29, 30, 32, 33, 34, 36, 37, 38, 39, 41, 42, 43, 44,
								 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 56, 57, 58, 59,
								 59, 60, 60, 61, 61, 62, 62, 62, 63, 63, 63, 64, 64, 64, 64, 64,
								 64, 64, 64, 64, 64, 64, 63, 63, 63, 62, 62, 62, 61, 61, 60, 60,
								 59, 59, 58, 57, 56, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46,
								 45, 44, 43, 42, 41, 39, 38, 37, 36, 34, 33, 32, 30, 29, 27, 26,
								 24, 23, 22, 20, 19, 17, 16, 14, 12, 11,  9,  8,  6,  5,  3,  2,
								  0,- 2,- 3,- 5,- 6,- 8,- 9,-11,-12,-14,-16,-17,-19,-20,-22,-23,
								-24,-26,-27,-29,-30,-32,-33,-34,-36,-37,-38,-39,-41,-42,-43,-44,
								-45,-46,-47,-48,-49,-50,-51,-52,-53,-54,-55,-56,-56,-57,-58,-59,
								-59,-60,-60,-61,-61,-62,-62,-62,-63,-63,-63,-64,-64,-64,-64,-64,
								-64,-64,-64,-64,-64,-64,-63,-63,-63,-62,-62,-62,-61,-61,-60,-60,
								-59,-59,-58,-57,-56,-56,-55,-54,-53,-52,-51,-50,-49,-48,-47,-46,
								-45,-44,-43,-42,-41,-39,-38,-37,-36,-34,-33,-32,-30,-29,-27,-26,
								-24,-23,-22,-20,-19,-17,-16,-14,-12,-11,- 9,- 8,- 6,- 5,- 3,- 2
							];
		#endregion

		static byte NumberOfVoices(Module mod) => ModDriver.md_sngchn < mod.NumVoices ? ModDriver.md_sngchn : mod.NumVoices;

		// This block of Code is for all the module effects.
		#region effects

		/*========== Protracker effects */

		static void DoArpeggio(ushort tick, ushort flags, MpControl a, byte style)
		{
			var note = a.main.note;

			if (a.arpmem != 0)
			{
				switch (style)
				{
					case 0:     /* mod style: N, N+x, N+y */
						switch (tick % 3)
						{
							/* case 0: unchanged */
							case 1:
								note += (byte)(a.arpmem >> 4);
								break;
							case 2:
								note += (byte)(a.arpmem & 0xf);
								break;
						}

						break;
					case 3:     /* okt arpeggio 3: N-x, N, N+y */
						switch (tick % 3)
						{
							case 0:
								note -= (byte)(a.arpmem >> 4);
								break;
							/* case 1: unchanged */
							case 2:
								note += (byte)(a.arpmem & 0xf);
								break;
						}

						break;
					case 4:     /* okt arpeggio 4: N, N+y, N, N-x */
						switch (tick % 4)
						{
							/* case 0, case 2: unchanged */
							case 1:
								note += (byte)(a.arpmem & 0xf);
								break;
							case 3:
								note -= (byte)(a.arpmem >> 4);
								break;
						}

						break;
					case 5:     /* okt arpeggio 5: N-x, N+y, N, and nothing at tick 0 */
						if (tick == 0)
						{
							break;
						}

						switch (tick % 3)
						{
							/* case 0: unchanged */
							case 1:
								note -= (byte)(a.arpmem >> 4);
								break;
							case 2:
								note += (byte)(a.arpmem & 0xf);
								break;
						}

						break;
				}

				a.main.period = GetPeriod(flags, (ushort)(note << 1), a.speed);
				a.ownper = 1;
			}
		}

		static int DoPTEffect0(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat == 0 && (flags & Constants.UF_ARPMEM) == Constants.UF_ARPMEM)
				{
					_ = a.arpmem;
				}
				else
				{
					a.arpmem = dat;
				}
			}

			if (a.main.period != 0)
			{
				DoArpeggio(tick, flags, a, 0);
			}

			return 0;
		}

		static int DoPTEffect1(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0 && dat != 0)
			{
				a.slidespeed = (ushort)(dat << 2);
			}

			if (a.main.period != 0)
			{
				if (tick != 0)
				{
					a.tmpperiod -= a.slidespeed;
				}
			}

			return 0;
		}

		static int DoPTEffect2(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0 && dat != 0)
			{
				a.slidespeed = (ushort)(dat << 2);
			}

			if (a.main.period != 0)
			{
				if (tick != 0)
				{
					a.tmpperiod += a.slidespeed;
				}
			}

			return 0;
		}

		static void DoToneSlide(ushort tick, MpControl a)
		{
			if (a.main.fadevol == 0)
			{
				a.main.kick = (byte)((a.main.kick == Constants.KICK_NOTE) ? Constants.KICK_NOTE : Constants.KICK_KEYOFF);
			}
			else
			{
				a.main.kick = (byte)((a.main.kick == Constants.KICK_NOTE) ? Constants.KICK_ENV : Constants.KICK_ABSENT);
			}

			if (tick != 0)
			{

				/* We have to slide a.main.period towards a.wantedperiod, so compute
                   the difference between those two values */
				var dist = a.main.period - a.wantedperiod;

				/* if they are equal or if portamentospeed is too big ...*/
				if (dist == 0 || a.portspeed > Math.Abs(dist))
				{
					/* ...make tmpperiod equal tperiod */
					a.tmpperiod = a.main.period = a.wantedperiod;
				}
				else if (dist > 0)
				{
					a.tmpperiod -= a.portspeed;
					a.main.period -= a.portspeed; /* dist>0, slide up */
				}
				else
				{
					a.tmpperiod += a.portspeed;
					a.main.period += a.portspeed; /* dist<0, slide down */
				}
			}
			else
			{
				a.tmpperiod = a.main.period;
			}

			a.ownper = 1;
		}

		static int DoPTEffect3(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if ((tick == 0) && (dat != 0))
			{
				a.portspeed = (ushort)(dat << 2);
			}

			if (a.main.period != 0)
			{
				DoToneSlide(tick, a);
			}

			return 0;
		}

		static void DoVibrato(ushort tick, MpControl a)
		{
			byte q;
			ushort temp = 0;    /* silence warning */

			if (tick == 0)
			{
				return;
			}

			q = (byte)((a.vibpos >> 2) & 0x1f);

			switch (a.wavecontrol & 3)
			{
				case 0: /* sine */
					temp = VibratoTable[q];
					break;
				case 1: /* ramp down */
					q <<= 3;
					if (a.vibpos < 0)
					{
						q = (byte)(255 - q);
					}

					temp = q;
					break;
				case 2: /* square wave */
					temp = 255;
					break;
				case 3: /* random wave */
					temp = (ushort)GetRandom(256);
					break;
			}

			temp *= a.vibdepth;
			temp >>= 7;
			temp <<= 2;

			if (a.vibpos >= 0)
			{
				a.main.period = (ushort)(a.tmpperiod + temp);
			}
			else
			{
				a.main.period = (ushort)(a.tmpperiod - temp);
			}

			a.ownper = 1;

			if (tick != 0)
			{
				a.vibpos += (sbyte)a.vibspd;
			}
		}

		static int DoPTEffect4(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0)
				{
					a.vibdepth = (byte)(dat & 0xf);
				}

				if ((dat & 0xf0) != 0)
				{
					a.vibspd = (byte)((dat & 0xf0) >> 2);
				}
			}

			if (a.main.period != 0)
			{
				DoVibrato(tick, a);
			}

			return 0;
		}

		static void DoVolSlide(MpControl a, byte dat)
		{
			if ((dat & 0xf) != 0)
			{
				a.tmpvolume -= (short)(dat & 0x0f);
				if (a.tmpvolume < 0)
				{
					a.tmpvolume = 0;
				}
			}
			else
			{
				a.tmpvolume += (short)(dat >> 4);
				if (a.tmpvolume > 64)
				{
					a.tmpvolume = 64;
				}
			}
		}

		static int DoPTEffect5(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (a.main.period != 0)
			{
				DoToneSlide(tick, a);
			}

			if (tick != 0)
			{
				DoVolSlide(a, dat);
			}

			return 0;
		}

		/* DoPTEffect6 after DoPTEffectA */

		static int DoPTEffect7(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte dat;
			byte q;
			ushort temp = 0;    /* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0)
				{
					a.trmdepth = (byte)(dat & 0xf);
				}

				if ((dat & 0xf0) != 0)
				{
					a.trmspd = (byte)((dat & 0xf0) >> 2);
				}
			}

			if (a.main.period != 0)
			{
				q = (byte)((a.trmpos >> 2) & 0x1f);

				switch ((a.wavecontrol >> 4) & 3)
				{
					case 0: /* sine */
						temp = VibratoTable[q];
						break;
					case 1: /* ramp down */
						q <<= 3;
						if (a.trmpos < 0)
						{
							q = (byte)(255 - q);
						}

						temp = q;
						break;
					case 2: /* square wave */
						temp = 255;
						break;
					case 3: /* random wave */
						temp = (ushort)GetRandom(256);
						break;
				}

				temp *= a.trmdepth;
				temp >>= 6;

				if (a.trmpos >= 0)
				{
					a.volume = (short)(a.tmpvolume + temp);
					if (a.volume > 64)
					{
						a.volume = 64;
					}
				}
				else
				{
					a.volume = (short)(a.tmpvolume - temp);
					if (a.volume < 0)
					{
						a.volume = 0;
					}
				}

				a.ownvol = 1;

				if (tick != 0)
				{
					a.trmpos += (sbyte)a.trmspd;
				}
			}

			return 0;
		}

		static int DoPTEffect8(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (mod.PanFlag)
			{
				a.main.panning = dat;
				mod.Panning[channel] = dat;
			}

			return 0;
		}

		static int DoPTEffect9(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat != 0)
				{
					a.soffset = (ushort)(dat << 8);
				}

				a.main.start = a.hioffset | a.soffset;

				if ((a.main.s != null) && (a.main.start > a.main.s.length))
				{
					var result = a.main.s.flags & (Constants.SF_LOOP | Constants.SF_BIDI);
					//int test = (Common.SF_LOOP|Common.SF_BIDI);
					if (result != 0)
					{
						a.main.start = a.main.s.loopstart;
					}
					else
					{
						a.main.start = a.main.s.length;
					}
				}
			}

			return 0;
		}

		static int DoPTEffectA(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick != 0)
			{
				DoVolSlide(a, dat);
			}

			return 0;
		}

		static int DoPTEffect6(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			if (a.main.period != 0)
			{
				DoVibrato(tick, a);
			}

			_ = DoPTEffectA(tick, flags, a, mod, channel);

			return 0;
		}

		static int DoPTEffectB(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();

			if (tick != 0 || mod.PatternDelayCounter2 != 0)
			{
				return 0;
			}

			/* Vincent Voois uses a nasty trick in "Universal Bolero" */
			if (dat == mod.Playback_SongPosition && mod.PatternNewStartPosition == mod.Playback_PatternRowNumber)
			{
				return 0;
			}

			if (!mod.Loop && mod.PatternNewStartPosition == 0 &&
				(dat < mod.Playback_SongPosition ||
					(mod.Playback_SongPosition == (mod.NumPositions - 1) && mod.PatternNewStartPosition == 0) ||
					(dat == mod.Playback_SongPosition && (flags & Constants.UF_NOWRAP) == Constants.UF_NOWRAP)
				))
			{
				/* if we don't loop, better not to skip the end of the
                    pattern, after all... so:
                mod.patbrk=0; */
				mod.PositionJumpFlag = 3;
			}
			else
			{
				/* if we were fading, adjust... */
				if (mod.Playback_SongPosition == (mod.NumPositions - 1))
				{
					mod.Volume = (short)(mod.InitialVolume > 128 ? 128 : mod.InitialVolume);
				}

				mod.Playback_SongPosition = dat;
				mod.PositionJumpFlag = 2;
				mod.Playback_PatternRowNumber = 0;

				if ((flags & Constants.UF_FT2QUIRKS) == Constants.UF_FT2QUIRKS)
				{
					mod.PatternNewStartPosition = 0;
				}
			}

			return 0;
		}

		static int DoPTEffectC(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick != 0)
			{
				return 0;
			}

			if (dat == byte.MaxValue)
			{
				a.anote = dat = 0; /* note cut */
			}
			else
				if (dat > 64)
			{
				dat = 64;
			}

			a.tmpvolume = dat;

			return 0;
		}

		static int DoPTEffectD(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if ((tick != 0) || (mod.PatternDelayCounter2 != 0))
			{
				return 0;
			}

			if ((mod.Positions[mod.Playback_SongPosition] != Constants.LAST_PATTERN) && (dat > mod.PatternRows[mod.Positions[mod.Playback_SongPosition]]))
			{
				dat = (byte)mod.PatternRows[mod.Positions[mod.Playback_SongPosition]];
			}

			mod.PatternNewStartPosition = dat;
			if (mod.PositionJumpFlag == 0)
			{
				/* don't ask me to explain this code - it makes
				   backwards.s3m and children.xm (heretic's version) play
				   correctly, among others. Take that for granted, or write
				   the page of comments yourself... you might need some
				   aspirin - Miod */
				if ((mod.Playback_SongPosition == mod.NumPositions - 1) && (dat != 0) && (mod.Loop ||
							   (mod.Positions[mod.Playback_SongPosition] == (mod.NumPatterns - 1)
								&& (flags & Constants.UF_NOWRAP) != Constants.UF_NOWRAP)))
				{
					mod.Playback_SongPosition = 0;
					mod.PositionJumpFlag = 2;
				}
				else
				{
					mod.PositionJumpFlag = 3;
				}
			}

			return 0;
		}

		static void DoEEffects(ushort tick, ushort flags, MpControl a, Module mod,
			short channel, byte dat)
		{
			var nib = (byte)(dat & 0xf);

			switch (dat >> 4)
			{
				case 0x0: /* hardware filter toggle, not supported */
					break;
				case 0x1: /* fineslide up */
					if (a.main.period != 0)
					{
						if (tick == 0)
						{
							a.tmpperiod -= (byte)(nib << 2);
						}
					}

					break;
				case 0x2: /* fineslide dn */
					if (a.main.period != 0)
					{
						if (tick == 0)
						{
							a.tmpperiod += (byte)(nib << 2);
						}
					}

					break;
				case 0x3: /* glissando ctrl */
					a.glissando = nib;
					break;
				case 0x4: /* set vibrato waveform */
					a.wavecontrol &= 0xf0;
					a.wavecontrol |= nib;
					break;
				case 0x5: /* set finetune */
					if (a.main.period != 0)
					{
						if ((flags & Constants.UF_XMPERIODS) == Constants.UF_XMPERIODS)
						{
							a.speed = (byte)(nib + 128);
						}
						else
						{
							a.speed = Constants.finetune[nib];
						}

						a.tmpperiod = GetPeriod(flags, (ushort)(a.main.note << 1), a.speed);
					}

					break;
				case 0x6: /* set patternloop */
					if (tick != 0)
					{
						break;
					}

					if (nib != 0)
					{ /* set reppos or repcnt ? */
						/* set repcnt, so check if repcnt already is set, which means we
						   are already looping */
						if (a.pat_repcnt != 0)
						{
							a.pat_repcnt--; /* already looping, decrease counter */
						}
						else
						{
#if BLAH
				/* this would make walker.xm, shipped with Xsoundtracker,
				   play correctly, but it's better to remain compatible
				   with FT2 */
				if ((!(flags&UF_NOWRAP))||(a.pat_reppos!=Constants.POS_NONE))
#endif
							a.pat_repcnt = nib; /* not yet looping, so set repcnt */
						}

						if (a.pat_repcnt != 0)
						{ /* jump to reppos if repcnt>0 */
							if (a.pat_reppos == Constants.POS_NONE)
							{
								a.pat_reppos = (short)(mod.Playback_PatternRowNumber - 1);
							}

							if (a.pat_reppos == -1)
							{
								mod.pat_repcrazy = 1;
								mod.Playback_PatternRowNumber = 0;
							}
							else
							{
								mod.Playback_PatternRowNumber = (ushort)a.pat_reppos;
							}
						}
						else
						{
							a.pat_reppos = Constants.POS_NONE;
						}
					}
					else
					{
						a.pat_reppos = (short)(mod.Playback_PatternRowNumber - 1); /* set reppos - can be (-1) */

						/* emulate the FT2 pattern loop (E60) bug:
						 * http://milkytracker.org/docs/MilkyTracker.html#fxE6x
						 * roadblas.xm plays correctly with this. */
						if ((flags & Constants.UF_FT2QUIRKS) == Constants.UF_FT2QUIRKS)
						{
							mod.PatternNewStartPosition = mod.Playback_PatternRowNumber;
						}
					}

					break;
				case 0x7: /* set tremolo waveform */
					a.wavecontrol &= 0x0f;
					a.wavecontrol |= (byte)(nib << 4);
					break;
				case 0x8: /* set panning */
					if (mod.PanFlag)
					{
						if (nib <= 8)
						{
							nib <<= 4;
						}
						else
						{
							nib *= 17;
						}

						a.main.panning = nib;
						mod.Panning[channel] = nib;
					}

					break;
				case 0x9: /* retrig note */
					/* do not retrigger on tick 0, until we are emulating FT2 and effect
					   data is zero */
					if (tick == 0 && !((flags & Constants.UF_FT2QUIRKS) == Constants.UF_FT2QUIRKS && (nib == 0)))
					{
						break;
					}
					/* only retrigger if data nibble > 0, or if tick 0 (FT2 compat) */
					if (nib != 0 || tick == 0)
					{
						if (a.retrig == 0)
						{
							/* when retrig counter reaches 0, reset counter and restart
							   the sample */
							if (a.main.period != 0)
							{
								a.main.kick = Constants.KICK_NOTE;
							}

							a.retrig = (sbyte)nib;
						}

						a.retrig--; /* countdown */
					}

					break;
				case 0xa: /* fine volume slide up */
					if (tick != 0)
					{
						break;
					}

					a.tmpvolume += nib;
					if (a.tmpvolume > 64)
					{
						a.tmpvolume = 64;
					}

					break;
				case 0xb: /* fine volume slide dn  */
					if (tick != 0)
					{
						break;
					}

					a.tmpvolume -= nib;
					if (a.tmpvolume < 0)
					{
						a.tmpvolume = 0;
					}

					break;
				case 0xc: /* cut note */
					/* When tick reaches the cut-note value, turn the volume to
					   zero (just like on the amiga) */
					if (tick >= nib)
					{
						a.tmpvolume = 0; /* just turn the volume down */
					}

					break;
				case 0xd: /* note delay */
					/* delay the start of the sample until tick==nib */
					if (tick == 0)
					{
						a.main.notedelay = nib;
					}
					else if (a.main.notedelay != 0)
					{
						a.main.notedelay--;
					}

					break;
				case 0xe: /* pattern delay */
					if (tick == 0)
					{
						if (mod.PatternDelayCounter2 == 0)
						{
							mod.PatterDelayCounter1 = (byte)(nib + 1); /* only once, when tick=0 */
						}
					}

					break;
				case 0xf: /* invert loop, not supported  */
					break;
			}
		}

		static int DoPTEffectE(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			DoEEffects(tick, flags, a, mod, channel, s_UniTrack.UniGetByte());

			return 0;
		}

		static int DoPTEffectF(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick != 0 || mod.PatternDelayCounter2 != 0)
			{
				return 0;
			}

			if (mod.ExtendedSpeedFlag && (dat >= mod.BpmLimit))
			{
				mod.bpm = dat;
			}
			else
			  if (dat != 0)
			{
				mod.SongSpeed = (ushort)((dat >= mod.BpmLimit) ? mod.BpmLimit - 1 : dat);
				mod.vbtick = 0;
			}

			return 0;
		}

		/*========== Scream Tracker effects */

		static int DoS3MEffectA(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var speed = s_UniTrack.UniGetByte();

			if (tick != 0 || mod.PatternDelayCounter2 != 0)
			{
				return 0;
			}

			if (speed > 128)
			{
				speed -= 128;
			}

			if (speed != 0)
			{
				mod.SongSpeed = speed;
				mod.vbtick = 0;
			}

			return 0;
		}

		static void DoS3MVolSlide(ushort tick, ushort flags, MpControl a, byte inf)
		{
			byte lo, hi;

			if (inf != 0)
			{
				a.s3mvolslide = inf;
			}
			else
			{
				inf = a.s3mvolslide;
			}

			lo = (byte)(inf & 0xf);
			hi = (byte)(inf >> 4);

			if (lo == 0)
			{
				if ((tick != 0) || (flags & Constants.UF_S3MSLIDES) == Constants.UF_S3MSLIDES)
				{
					a.tmpvolume += hi;
				}
			}
			else
			  if (hi == 0)
			{
				if ((tick != 0) || (flags & Constants.UF_S3MSLIDES) == Constants.UF_S3MSLIDES)
				{
					a.tmpvolume -= lo;
				}
			}
			else
			  if (lo == 0xf)
			{
				if (tick == 0)
				{
					a.tmpvolume += (short)(hi != 0 ? hi : 0xf);
				}
			}
			else
			  if (hi == 0xf)
			{
				if (tick == 0)
				{
					a.tmpvolume -= (short)(lo != 0 ? lo : 0xf);
				}
			}
			else
			{
				return;
			}

			if (a.tmpvolume < 0)
			{
				a.tmpvolume = 0;
			}
			else if (a.tmpvolume > 64)
			{
				a.tmpvolume = 64;
			}
		}

		static int DoS3MEffectD(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			DoS3MVolSlide(tick, flags, a, s_UniTrack.UniGetByte());

			return 1;
		}

		static void DoS3MSlideDn(ushort tick, MpControl a, byte inf)
		{
			byte hi, lo;

			if (inf != 0)
			{
				a.slidespeed = inf;
			}
			else
			{
				inf = (byte)a.slidespeed;
			}

			hi = (byte)(inf >> 4);
			lo = (byte)(inf & 0xf);

			if (hi == 0xf)
			{
				if (tick == 0)
				{
					a.tmpperiod += (ushort)(lo << 2);
				}
			}
			else
			  if (hi == 0xe)
			{
				if (tick == 0)
				{
					a.tmpperiod += lo;
				}
			}
			else
			{
				if (tick != 0)
				{
					a.tmpperiod += (ushort)(inf << 2);
				}
			}
		}

		static int DoS3MEffectE(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (a.main.period != 0)
			{
				DoS3MSlideDn(tick, a, dat);
			}

			return 0;
		}

		static void DoS3MSlideUp(ushort tick, MpControl a, byte inf)
		{
			byte hi, lo;

			if (inf != 0)
			{
				a.slidespeed = inf;
			}
			else
			{
				inf = (byte)a.slidespeed;
			}

			hi = (byte)(inf >> 4);
			lo = (byte)(inf & 0xf);

			if (hi == 0xf)
			{
				if (tick == 0)
				{
					a.tmpperiod -= (ushort)(lo << 2);
				}
			}
			else
			  if (hi == 0xe)
			{
				if (tick == 0)
				{
					a.tmpperiod -= lo;
				}
			}
			else
			{
				if (tick != 0)
				{
					a.tmpperiod -= (ushort)(inf << 2);
				}
			}
		}

		static int DoS3MEffectF(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (a.main.period != 0)
			{
				DoS3MSlideUp(tick, a, dat);
			}

			return 0;
		}

		static int DoS3MEffectI(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte inf, on, off;

			inf = s_UniTrack.UniGetByte();
			if (inf != 0)
			{
				a.s3mtronof = inf;
			}
			else
			{
				inf = a.s3mtronof;
				if (inf == 0)
				{
					return 0;
				}
			}

			if (tick == 0)
			{
				return 0;
			}

			on = (byte)((inf >> 4) + 1);
			off = (byte)((inf & 0xf) + 1);
			a.s3mtremor %= (byte)(on + off);
			a.volume = (short)((a.s3mtremor < on) ? a.tmpvolume : 0);
			a.ownvol = 1;
			a.s3mtremor++;

			return 0;
		}

		static int DoS3MEffectQ(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var inf = s_UniTrack.UniGetByte();
			if (a.main.period != 0)
			{
				if (inf != 0)
				{
					a.s3mrtgslide = (byte)(inf >> 4);
					a.s3mrtgspeed = (byte)(inf & 0xf);
				}

				/* only retrigger if low nibble > 0 */
				if (a.s3mrtgspeed > 0)
				{
					if (a.retrig == 0)
					{
						/* when retrig counter reaches 0, reset counter and restart the
                           sample */
						if (a.main.kick != Constants.KICK_NOTE)
						{
							a.main.kick = Constants.KICK_KEYOFF;
						}

						a.retrig = (sbyte)a.s3mrtgspeed;

						if ((tick != 0) || (flags & Constants.UF_S3MSLIDES) == Constants.UF_S3MSLIDES)
						{
							switch (a.s3mrtgslide)
							{
								case 1:
								case 2:
								case 3:
								case 4:
								case 5:
									a.tmpvolume -= (short)(1 << (a.s3mrtgslide - 1));
									break;
								case 6:
									a.tmpvolume = (short)(2 * a.tmpvolume / 3);
									break;
								case 7:
									a.tmpvolume >>= 1;
									break;
								case 9:
								case 0xa:
								case 0xb:
								case 0xc:
								case 0xd:
									a.tmpvolume += (short)(1 << (a.s3mrtgslide - 9));
									break;
								case 0xe:
									a.tmpvolume = (short)((3 * a.tmpvolume) >> 1);
									break;
								case 0xf:
									a.tmpvolume = (short)(a.tmpvolume << 1);
									break;
							}

							if (a.tmpvolume < 0)
							{
								a.tmpvolume = 0;
							}
							else if (a.tmpvolume > 64)
							{
								a.tmpvolume = 64;
							}
						}
					}

					a.retrig--; /* countdown  */
				}
			}

			return 0;
		}

		static int DoS3MEffectR(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte dat, q;
			ushort temp = 0;    /* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0)
				{
					a.trmdepth = (byte)(dat & 0xf);
				}

				if ((dat & 0xf0) != 0)
				{
					a.trmspd = (byte)((dat & 0xf0) >> 2);
				}
			}

			q = (byte)((a.trmpos >> 2) & 0x1f);

			switch ((a.wavecontrol >> 4) & 3)
			{
				case 0: /* sine */
					temp = VibratoTable[q];
					break;
				case 1: /* ramp down */
					q <<= 3;
					if (a.trmpos < 0)
					{
						q = (byte)(255 - q);
					}

					temp = q;
					break;
				case 2: /* square wave */
					temp = 255;
					break;
				case 3: /* random */
					temp = (ushort)GetRandom(256);
					break;
			}

			temp *= a.trmdepth;
			temp >>= 7;

			if (a.trmpos >= 0)
			{
				a.volume = (short)(a.tmpvolume + temp);
				if (a.volume > 64)
				{
					a.volume = 64;
				}
			}
			else
			{
				a.volume = (short)(a.tmpvolume - temp);
				if (a.volume < 0)
				{
					a.volume = 0;
				}
			}

			a.ownvol = 1;

			if (tick != 0)
			{
				a.trmpos += (sbyte)a.trmspd;
			}

			return 0;
		}

		static int DoS3MEffectT(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var tempo = s_UniTrack.UniGetByte();

			if (tick != 0 || mod.PatternDelayCounter2 != 0)
			{
				return 0;
			}

			mod.bpm = (ushort)((tempo < 32) ? 32 : tempo);

			return 0;
		}

		static int DoS3MEffectU(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte dat, q;
			ushort temp = 0;    /* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0)
				{
					a.vibdepth = (byte)(dat & 0xf);
				}

				if ((dat & 0xf0) != 0)
				{
					a.vibspd = (byte)((dat & 0xf0) >> 2);
				}
			}
			else
				if (a.main.period != 0)
			{
				q = (byte)((a.vibpos >> 2) & 0x1f);

				switch (a.wavecontrol & 3)
				{
					case 0: /* sine */
						temp = VibratoTable[q];
						break;
					case 1: /* ramp down */
						q <<= 3;
						if (a.vibpos < 0)
						{
							q = (byte)(255 - q);
						}

						temp = q;
						break;
					case 2: /* square wave */
						temp = 255;
						break;
					case 3: /* random */
						temp = (ushort)GetRandom(256);
						break;
				}

				temp *= a.vibdepth;
				temp >>= 8;

				if (a.vibpos >= 0)
				{
					a.main.period = (ushort)(a.tmpperiod + temp);
				}
				else
				{
					a.main.period = (ushort)(a.tmpperiod - temp);
				}

				a.ownper = 1;

				a.vibpos += (sbyte)a.vibspd;
			}

			return 0;
		}

		/*========== Envelope helpers */

		static int DoKeyOff(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			a.main.keyoff |= Constants.KEY_OFF;
			if (((a.main.volflg & Constants.EF_ON) != Constants.EF_ON) || (a.main.volflg & Constants.EF_LOOP) == Constants.EF_LOOP)
			{
				a.main.keyoff = Constants.KEY_KILL;
			}

			return 0;
		}

		static int DoKeyFade(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if ((tick >= dat) || (tick == mod.SongSpeed - 1))
			{
				a.main.keyoff = Constants.KEY_KILL;
				if ((a.main.volflg & Constants.EF_ON) != Constants.EF_ON)
				{
					a.main.fadevol = 0;
				}
			}

			return 0;
		}

		/*========== Fast Tracker effects */

		/* DoXMEffect6 after DoXMEffectA */

		static int DoXMEffectA(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte inf, lo, hi;

			inf = s_UniTrack.UniGetByte();
			if (inf != 0)
			{
				a.s3mvolslide = inf;
			}
			else
			{
				inf = a.s3mvolslide;
			}

			if (tick != 0)
			{
				lo = (byte)(inf & 0xf);
				hi = (byte)(inf >> 4);

				if (hi == 0)
				{
					a.tmpvolume -= lo;
					if (a.tmpvolume < 0)
					{
						a.tmpvolume = 0;
					}
				}
				else
				{
					a.tmpvolume += hi;
					if (a.tmpvolume > 64)
					{
						a.tmpvolume = 64;
					}
				}
			}

			return 0;
		}

		static int DoXMEffect6(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			if (a.main.period != 0)
			{
				DoVibrato(tick, a);
			}

			return DoXMEffectA(tick, flags, a, mod, channel);
		}

		static int DoXMEffectE1(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat != 0)
				{
					a.fportupspd = dat;
				}

				if (a.main.period != 0)
				{
					a.tmpperiod -= (ushort)(a.fportupspd << 2);
				}
			}

			return 0;
		}

		static int DoXMEffectE2(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat != 0)
				{
					a.fportdnspd = dat;
				}

				if (a.main.period != 0)
				{
					a.tmpperiod += (ushort)(a.fportdnspd << 2);
				}
			}

			return 0;
		}

		static int DoXMEffectEA(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat != 0)
				{
					a.fslideupspd = dat;
				}
			}

			a.tmpvolume += a.fslideupspd;
			if (a.tmpvolume > 64)
			{
				a.tmpvolume = 64;
			}

			return 0;
		}

		static int DoXMEffectEB(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat != 0)
				{
					a.fslidednspd = dat;
				}
			}

			a.tmpvolume -= a.fslidednspd;
			if (a.tmpvolume < 0)
			{
				a.tmpvolume = 0;
			}

			return 0;
		}

		static int DoXMEffectG(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			mod.Volume = (short)(s_UniTrack.UniGetByte() << 1);
			if (mod.Volume > 128)
			{
				mod.Volume = 128;
			}

			return 0;
		}

		static int DoXMEffectH(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var inf = s_UniTrack.UniGetByte();

			if (tick != 0)
			{
				if (inf != 0)
				{
					mod.GlobalSlide = inf;
				}
				else
				{
					inf = mod.GlobalSlide;
				}

				if ((inf & 0xf0) == 0xf0)
				{
					inf &= 0xf0;
				}

				mod.Volume = (short)(mod.Volume + (((inf >> 4) - (inf & 0xf)) * 2));

				if (mod.Volume < 0)
				{
					mod.Volume = 0;
				}
				else if (mod.Volume > 128)
				{
					mod.Volume = 128;
				}
			}

			return 0;
		}

		static int DoXMEffectL(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if ((tick == 0) && (a.main.i != null))
			{
				ushort points;
				var i = a.main.i;
				MpVoice aout;

				if ((aout = a.slave) != null)
				{
					if (aout.venv.env != null)
					{
						points = (ushort)i.volenv[i.volpts - 1].pos;
						aout.venv.p = aout.venv.env[(dat > points) ? points : dat].pos;
					}

					if (aout.penv.env != null)
					{
						points = (ushort)i.panenv[i.panpts - 1].pos;
						aout.penv.p = aout.penv.env[(dat > points) ? points : dat].pos;
					}
				}
			}

			return 0;
		}

		static int DoXMEffectP(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte inf, lo, hi;
			short pan;

			inf = s_UniTrack.UniGetByte();
			if (!mod.PanFlag)
			{
				return 0;
			}

			if (inf != 0)
			{
				a.pansspd = inf;
			}
			else
			{
				inf = a.pansspd;
			}

			if (tick != 0)
			{
				lo = (byte)(inf & 0xf);
				hi = (byte)(inf >> 4);

				/* slide right has absolute priority */
				if (hi != 0)
				{
					lo = 0;
				}

				pan = (short)(((a.main.panning == Constants.PAN_SURROUND) ? Constants.PAN_CENTER : a.main.panning) + hi - lo);
				a.main.panning = (short)((pan < Constants.PAN_LEFT) ? Constants.PAN_LEFT : (pan > Constants.PAN_RIGHT ? Constants.PAN_RIGHT : pan));
			}

			return 0;
		}

		static int DoXMEffectX1(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (dat != 0)
			{
				a.ffportupspd = dat;
			}
			else
			{
				dat = a.ffportupspd;
			}

			if (a.main.period != 0)
			{
				if (tick == 0)
				{
					a.main.period -= dat;
					a.tmpperiod -= dat;
					a.ownper = 1;
				}
			}

			return 0;
		}

		static int DoXMEffectX2(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var dat = s_UniTrack.UniGetByte();
			if (dat != 0)
			{
				a.ffportdnspd = dat;
			}
			else
			{
				dat = a.ffportdnspd;
			}

			if (a.main.period != 0)
			{
				if (tick == 0)
				{
					a.main.period += dat;
					a.tmpperiod += dat;
					a.ownper = 1;
				}
			}

			return 0;
		}

		/*========== Impulse Tracker effects */

		static void DoITToneSlide(ushort tick, MpControl a, byte dat)
		{
			if (dat != 0)
			{
				a.portspeed = dat;
			}

			/* if we don't come from another note, ignore the slide and play the note
               as is */
			if (a.oldnote == 0 || a.main.period == 0)
			{
				return;
			}

			if ((tick == 0) && (a.newsamp != 0))
			{
				a.main.kick = Constants.KICK_NOTE;
				a.main.start = -1;
			}
			else
			{
				a.main.kick = (byte)((a.main.kick == Constants.KICK_NOTE) ? Constants.KICK_ENV : Constants.KICK_ABSENT);
			}

			if (tick != 0)
			{

				/* We have to slide a.main.period towards a.wantedperiod, compute the
                   difference between those two values */
				var dist = a.main.period - a.wantedperiod;

				/* if they are equal or if portamentospeed is too big... */
				if ((dist == 0) || ((a.portspeed << 2) > Math.Abs(dist)))
				{
					/* ... make tmpperiod equal tperiod */
					a.tmpperiod = a.main.period = a.wantedperiod;
				}
				else
				  if (dist > 0)
				{
					a.tmpperiod -= (ushort)(a.portspeed << 2);
					a.main.period -= (ushort)(a.portspeed << 2); /* dist>0 slide up */
				}
				else
				{
					a.tmpperiod += (ushort)(a.portspeed << 2);
					a.main.period += (ushort)(a.portspeed << 2); /* dist<0 slide down */
				}
			}
			else
			{
				a.tmpperiod = a.main.period;
			}

			a.ownper = 1;
		}

		static int DoITEffectG(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			DoITToneSlide(tick, a, s_UniTrack.UniGetByte());

			return 0;
		}

		static void DoITVibrato(ushort tick, MpControl a, byte dat)
		{
			byte q;
			ushort temp = 0;

			if (tick == 0)
			{
				if ((dat & 0x0f) != 0)
				{
					a.vibdepth = (byte)(dat & 0xf);
				}

				if ((dat & 0xf0) != 0)
				{
					a.vibspd = (byte)((dat & 0xf0) >> 2);
				}
			}

			if (a.main.period == 0)
			{
				return;
			}

			q = (byte)((a.vibpos >> 2) & 0x1f);

			switch (a.wavecontrol & 3)
			{
				case 0: /* sine */
					temp = VibratoTable[q];
					break;
				case 1: /* square wave */
					temp = 255;
					break;
				case 2: /* ramp down */
					q <<= 3;
					if (a.vibpos < 0)
					{
						q = (byte)(255 - q);
					}

					temp = q;
					break;
				case 3: /* random */
					temp = (ushort)GetRandom(256);
					break;
			}

			temp *= a.vibdepth;
			temp >>= 8;
			temp <<= 2;

			if (a.vibpos >= 0)
			{
				a.main.period = (ushort)(a.tmpperiod + temp);
			}
			else
			{
				a.main.period = (ushort)(a.tmpperiod - temp);
			}

			a.ownper = 1;

			a.vibpos += (sbyte)a.vibspd;
		}

		static int DoITEffectH(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			DoITVibrato(tick, a, s_UniTrack.UniGetByte());

			return 0;
		}

		static int DoITEffectI(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte inf, on, off;

			inf = s_UniTrack.UniGetByte();
			if (inf != 0)
			{
				a.s3mtronof = inf;
			}
			else
			{
				inf = a.s3mtronof;
				if (inf == 0)
				{
					return 0;
				}
			}

			on = (byte)(inf >> 4);
			off = (byte)(inf & 0xf);

			a.s3mtremor %= (byte)(on + off);
			a.volume = (short)((a.s3mtremor < on) ? a.tmpvolume : 0);
			a.ownvol = 1;
			a.s3mtremor++;

			return 0;
		}

		static int DoITEffectM(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			a.main.chanvol = (sbyte)s_UniTrack.UniGetByte();
			if (a.main.chanvol > 64)
			{
				a.main.chanvol = 64;
			}
			else if (a.main.chanvol < 0)
			{
				a.main.chanvol = 0;
			}

			return 0;
		}

		static int DoITEffectN(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte inf, lo, hi;

			inf = s_UniTrack.UniGetByte();

			if (inf != 0)
			{
				a.chanvolslide = inf;
			}
			else
			{
				inf = a.chanvolslide;
			}

			lo = (byte)(inf & 0xf);
			hi = (byte)(inf >> 4);

			if (hi == 0)
			{
				a.main.chanvol -= (sbyte)lo;
			}
			else
			  if (lo == 0)
			{
				a.main.chanvol += (sbyte)hi;
			}
			else
			  if (hi == 0xf)
			{
				if (tick == 0)
				{
					a.main.chanvol -= (sbyte)lo;
				}
			}
			else
			  if (lo == 0xf)
			{
				if (tick == 0)
				{
					a.main.chanvol += (sbyte)hi;
				}
			}

			if (a.main.chanvol < 0)
			{
				a.main.chanvol = 0;
			}
			else if (a.main.chanvol > 64)
			{
				a.main.chanvol = 64;
			}

			return 0;
		}

		static int DoITEffectP(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte inf, lo, hi;
			short pan;

			inf = s_UniTrack.UniGetByte();
			if (inf != 0)
			{
				a.pansspd = inf;
			}
			else
			{
				inf = a.pansspd;
			}

			if (!mod.PanFlag)
			{
				return 0;
			}

			lo = (byte)(inf & 0xf);
			hi = (byte)(inf >> 4);

			pan = (short)((a.main.panning == Constants.PAN_SURROUND) ? Constants.PAN_CENTER : a.main.panning);

			if (hi == 0)
			{
				pan += (short)(lo << 2);
			}
			else
			  if (lo == 0)
			{
				pan -= (short)(hi << 2);
			}
			else
			  if (hi == 0xf)
			{
				if (tick == 0)
				{
					pan += (short)(lo << 2);
				}
			}
			else
			  if (lo == 0xf)
			{
				if (tick == 0)
				{
					pan -= (short)(hi << 2);
				}
			}

			a.main.panning = (short)((pan < Constants.PAN_LEFT) ? Constants.PAN_LEFT : (pan > Constants.PAN_RIGHT ? Constants.PAN_RIGHT : pan));

			return 0;
		}

		static int DoITEffectT(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte tempo;
			short temp;

			tempo = s_UniTrack.UniGetByte();

			if (mod.PatternDelayCounter2 != 0)
			{
				return 0;
			}

			temp = (byte)mod.bpm;
			if ((tempo & 0x10) == 0x10)
			{
				temp += (byte)(tempo & 0x0f);
			}
			else
			{
				temp -= tempo;
			}

			mod.bpm = (ushort)((temp > 255) ? 255 : (temp < 1 ? 1 : temp));

			return 0;
		}

		static int DoITEffectU(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte dat, q;
			ushort temp = 0;    /* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0)
				{
					a.vibdepth = (byte)(dat & 0xf);
				}

				if ((dat & 0xf0) != 0)
				{
					a.vibspd = (byte)((dat & 0xf0) >> 2);
				}
			}

			if (a.main.period != 0)
			{
				q = (byte)((a.vibpos >> 2) & 0x1f);

				switch (a.wavecontrol & 3)
				{
					case 0: /* sine */
						temp = VibratoTable[q];
						break;
					case 1: /* square wave */
						temp = 255;
						break;
					case 2: /* ramp down */
						q <<= 3;
						if (a.vibpos < 0)
						{
							q = (byte)(255 - q);
						}

						temp = q;
						break;
					case 3: /* random */
						temp = (ushort)GetRandom(256);
						break;
				}

				temp *= a.vibdepth;
				temp >>= 8;

				if (a.vibpos >= 0)
				{
					a.main.period = (ushort)(a.tmpperiod + temp);
				}
				else
				{
					a.main.period = (ushort)(a.tmpperiod - temp);
				}

				a.ownper = 1;

				a.vibpos += (sbyte)a.vibspd;
			}

			return 0;
		}

		static int DoITEffectW(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte inf, lo, hi;

			inf = s_UniTrack.UniGetByte();

			if (inf != 0)
			{
				mod.GlobalSlide = inf;
			}
			else
			{
				inf = mod.GlobalSlide;
			}

			lo = (byte)(inf & 0xf);
			hi = (byte)(inf >> 4);

			if (lo == 0)
			{
				if (tick != 0)
				{
					mod.Volume += hi;
				}
			}
			else
			  if (hi == 0)
			{
				if (tick != 0)
				{
					mod.Volume -= lo;
				}
			}
			else
			  if (lo == 0xf)
			{
				if (tick == 0)
				{
					mod.Volume += hi;
				}
			}
			else
			  if (hi == 0xf)
			{
				if (tick == 0)
				{
					mod.Volume -= lo;
				}
			}

			if (mod.Volume < 0)
			{
				mod.Volume = 0;
			}
			else if (mod.Volume > 128)
			{
				mod.Volume = 128;
			}

			return 0;
		}

		static int DoITEffectY(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte dat, q;
			var temp = 0;   /* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0)
				{
					a.panbdepth = (byte)(dat & 0xf);
				}

				if ((dat & 0xf0) != 0)
				{
					a.panbspd = (sbyte)((dat & 0xf0) >> 4);
				}
			}

			if (mod.PanFlag)
			{
				q = a.panbpos;

				switch (a.panbwave)
				{
					case 0: /* sine */
						temp = PanbrelloTable[q];
						break;
					case 1: /* square wave */
						temp = (q < 0x80) ? 64 : 0;
						break;
					case 2: /* ramp down */
						q <<= 3;
						temp = q;
						break;
					case 3: /* random */
						temp = GetRandom(256);
						break;
				}

				temp *= a.panbdepth;
				temp = (temp / 8) + mod.Panning[channel];

				a.main.panning = (short)((temp < Constants.PAN_LEFT) ? Constants.PAN_LEFT : (temp > Constants.PAN_RIGHT ? Constants.PAN_RIGHT : temp));
				a.panbpos += (byte)a.panbspd;

			}

			return 0;
		}

		/* Impulse/Scream Tracker Sxx effects.
           All Sxx effects share the same memory space. */
		static int DoITEffectS0(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte dat, inf, c;

			dat = s_UniTrack.UniGetByte();
			inf = (byte)(dat & 0xf);
			c = (byte)(dat >> 4);

			if (dat == 0)
			{
				c = a.sseffect;
				inf = a.ssdata;
			}
			else
			{
				a.sseffect = c;
				a.ssdata = inf;
			}

			switch (c)
			{
				case (byte)ExtentedEffects.SS_GLISSANDO: /* S1x set glissando voice */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0x30 | inf));
					break;

				case (byte)ExtentedEffects.SS_FINETUNE: /* S2x set finetune */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0x50 | inf));
					break;

				case (byte)ExtentedEffects.SS_VIBWAVE: /* S3x set vibrato waveform */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0x40 | inf));
					break;

				case (byte)ExtentedEffects.SS_TREMWAVE: /* S4x set tremolo waveform */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0x70 | inf));
					break;

				case (byte)ExtentedEffects.SS_PANWAVE: /* S5x panbrello */
					a.panbwave = inf;
					break;

				case (byte)ExtentedEffects.SS_FRAMEDELAY: /* S6x delay x number of frames (patdly) */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0xe0 | inf));
					break;

				case (byte)ExtentedEffects.SS_S7EFFECTS: /* S7x instrument / NNA commands */
					DoNNAEffects(mod, a, inf);
					break;

				case (byte)ExtentedEffects.SS_PANNING: /* S8x set panning position */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0x80 | inf));
					break;

				case (byte)ExtentedEffects.SS_SURROUND: /* S9x set surround sound */
				{
					if (mod.PanFlag)
					{
						a.main.panning = Constants.PAN_SURROUND;
						mod.Panning[channel] = Constants.PAN_SURROUND;
					}

					break;
				}
				case (byte)ExtentedEffects.SS_HIOFFSET: /* SAy set high order sample offset yxx00h */
				{
					if (tick == 0)
					{
						a.hioffset = (uint)(inf << 16);
						a.main.start = a.hioffset | a.soffset;

						if ((a.main.s != null) && (a.main.start > a.main.s.length))
						{
							a.main.start = (a.main.s.flags & (Constants.SF_LOOP | Constants.SF_BIDI)) != 0
								? a.main.s.loopstart
								: a.main.s.length;
						}
					}

					break;
				}

				case (byte)ExtentedEffects.SS_PATLOOP: /* SBx pattern loop */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0x60 | inf));
					break;

				case (byte)ExtentedEffects.SS_NOTECUT: /* SCx notecut */
					if (inf == 0)
					{
						inf = 1;
					}

					DoEEffects(tick, flags, a, mod, channel, (byte)(0xC0 | inf));
					break;

				case (byte)ExtentedEffects.SS_NOTEDELAY: /* SDx notedelay */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0xD0 | inf));
					break;

				case (byte)ExtentedEffects.SS_PATDELAY: /* SEx patterndelay */
					DoEEffects(tick, flags, a, mod, channel, (byte)(0xE0 | inf));
					break;
			}

			return 0;
		}

		/*========== Impulse Tracker Volume/Pan Column effects */

		/*
         * All volume/pan column effects share the same memory space.
         */

		static int DoVolEffects(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte c, inf;

			c = s_UniTrack.UniGetByte();
			inf = s_UniTrack.UniGetByte();

			if ((c == 0) && (inf == 0))
			{
				c = a.voleffect;
				inf = a.voldata;
			}
			else
			{
				a.voleffect = c;
				a.voldata = inf;
			}

			if (c != 0)
			{
				switch (c)
				{
					case (byte)ITColumnEffect.VOL_VOLUME:
						if (tick != 0)
						{
							break;
						}

						if (inf > 64)
						{
							inf = 64;
						}

						a.tmpvolume = inf;
						break;
					case (byte)ITColumnEffect.VOL_PANNING:
						if (mod.PanFlag)
						{
							a.main.panning = inf;
						}

						break;
					case (byte)ITColumnEffect.VOL_VOLSLIDE:
						DoS3MVolSlide(tick, flags, a, inf);
						return 1;
					case (byte)ITColumnEffect.VOL_PITCHSLIDEDN:
						if (a.main.period != 0)
						{
							DoS3MSlideDn(tick, a, inf);
						}

						break;
					case (byte)ITColumnEffect.VOL_PITCHSLIDEUP:
						if (a.main.period != 0)
						{
							DoS3MSlideUp(tick, a, inf);
						}

						break;
					case (byte)ITColumnEffect.VOL_PORTAMENTO:
						DoITToneSlide(tick, a, inf);
						break;
					case (byte)ITColumnEffect.VOL_VIBRATO:
						DoITVibrato(tick, a, inf);
						break;
				}
			}

			return 0;
		}

		/*========== UltraTracker effects */

		static int DoULTEffect9(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var offset = s_UniTrack.UniGetWord();

			if (offset != 0)
			{
				a.ultoffset = offset;
			}

			a.main.start = a.ultoffset << 2;
			if ((a.main.s != null) && (a.main.start > a.main.s.length))
			{
				a.main.start = (a.main.s.flags & (Constants.SF_LOOP | Constants.SF_BIDI)) != 0
					? a.main.s.loopstart
					: a.main.s.length;
			}

			return 0;
		}

		/*========== OctaMED effects */

		static int DoMEDSpeed(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			var speed = s_UniTrack.UniGetWord();
			mod.bpm = speed;
			return 0;
		}

		static int DoMEDEffectF1(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			DoEEffects(tick, flags, a, mod, channel, (byte)(0x90 | (mod.SongSpeed / 2)));
			return 0;
		}

		static int DoMEDEffectF2(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			DoEEffects(tick, flags, a, mod, channel, (byte)(0xd0 | (mod.SongSpeed / 2)));
			return 0;
		}

		static int DoMEDEffectF3(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			DoEEffects(tick, flags, a, mod, channel, (byte)(0x90 | (mod.SongSpeed / 3)));
			return 0;
		}

		/*========== Oktalyzer effects */

		static int DoOktArp(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			byte dat, dat2;

			dat2 = s_UniTrack.UniGetByte(); /* arpeggio style */
			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat == 0 && (flags & Constants.UF_ARPMEM) == Constants.UF_ARPMEM)
				{
					_ = a.arpmem;
				}
				else
				{
					a.arpmem = dat;
				}
			}

			if (a.main.period != 0)
			{
				DoArpeggio(tick, flags, a, dat2);
			}

			return 0;
		}

		static int DoNothing(ushort tick, ushort flags, MpControl a, Module mod, short channel)
		{
			s_UniTrack.UniSkipOpcode();
			return 0;
		}

		static void DoNNAEffects(Module mod, MpControl a, byte dat)
		{
			int t;
			MpVoice aout;

			dat &= 0xf;
			aout = a.slave ?? null;

			switch (dat)
			{
				case 0x0: /* past note cut */
					for (t = 0; t < NumberOfVoices(mod); t++)
					{
						if (mod.Voice[t].master == a)
						{
							mod.Voice[t].main.fadevol = 0;
						}
					}

					break;
				case 0x1: /* past note off */
					for (t = 0; t < NumberOfVoices(mod); t++)
					{
						if (mod.Voice[t].master == a)
						{
							mod.Voice[t].main.keyoff |= Constants.KEY_OFF;
							if ((mod.Voice[t].venv.flg & Constants.EF_ON) != Constants.EF_ON ||
							   (mod.Voice[t].venv.flg & Constants.EF_LOOP) == Constants.EF_LOOP)
							{
								mod.Voice[t].main.keyoff = Constants.KEY_KILL;
							}
						}
					}

					break;
				case 0x2: /* past note fade */
					for (t = 0; t < NumberOfVoices(mod); t++)
					{
						if (mod.Voice[t].master == a)
						{
							mod.Voice[t].main.keyoff |= Constants.KEY_FADE;
						}
					}

					break;
				case 0x3: /* set NNA note cut */
					a.main.nna = (byte)((a.main.nna & ~Constants.NNA_MASK) | Constants.NNA_CUT);
					break;
				case 0x4: /* set NNA note continue */
					a.main.nna = (byte)((a.main.nna & ~Constants.NNA_MASK) | Constants.NNA_CONTINUE);
					break;
				case 0x5: /* set NNA note off */
					a.main.nna = (byte)((a.main.nna & ~Constants.NNA_MASK) | Constants.NNA_OFF);
					break;
				case 0x6: /* set NNA note fade */
					a.main.nna = (byte)((a.main.nna & ~Constants.NNA_MASK) | Constants.NNA_FADE);
					break;
				case 0x7: /* disable volume envelope */
					if (aout != null)
					{
						aout.main.volflg = (byte)(aout.main.volflg & ~Constants.EF_ON);
					}

					break;
				case 0x8: /* enable volume envelope  */
					if (aout != null)
					{
						aout.main.volflg |= Constants.EF_ON;
					}

					break;
				case 0x9: /* disable panning envelope */
					if (aout != null)
					{
						aout.main.panflg = (byte)(aout.main.panflg & ~Constants.EF_ON);
					}

					break;
				case 0xa: /* enable panning envelope */
					if (aout != null)
					{
						aout.main.panflg |= Constants.EF_ON;
					}

					break;
				case 0xb: /* disable pitch envelope */
					if (aout != null)
					{
						aout.main.pitflg = (byte)(aout.main.panflg & ~Constants.EF_ON);
					}

					break;
				case 0xc: /* enable pitch envelope */
					if (aout != null)
					{
						aout.main.pitflg |= Constants.EF_ON;
					}

					break;
			}
		}

		static readonly effectDelegate[] effects =
		[
			DoNothing,		/* 0 */
			DoNothing,		/* UNI_NOTE */
			DoNothing,		/* UNI_INSTRUMENT */
			DoPTEffect0,	/* UNI_PTEFFECT0 */
			DoPTEffect1,	/* UNI_PTEFFECT1 */
			DoPTEffect2,	/* UNI_PTEFFECT2 */
			DoPTEffect3,	/* UNI_PTEFFECT3 */
			DoPTEffect4,	/* UNI_PTEFFECT4 */
			DoPTEffect5,	/* UNI_PTEFFECT5 */
			DoPTEffect6,	/* UNI_PTEFFECT6 */
			DoPTEffect7,	/* UNI_PTEFFECT7 */
			DoPTEffect8,	/* UNI_PTEFFECT8 */
			DoPTEffect9,	/* UNI_PTEFFECT9 */
			DoPTEffectA,	/* UNI_PTEFFECTA */
			DoPTEffectB,	/* UNI_PTEFFECTB */
			DoPTEffectC,	/* UNI_PTEFFECTC */
			DoPTEffectD,	/* UNI_PTEFFECTD */
			DoPTEffectE,	/* UNI_PTEFFECTE */
			DoPTEffectF,	/* UNI_PTEFFECTF */
			DoS3MEffectA,	/* UNI_S3MEFFECTA */
			DoS3MEffectD,	/* UNI_S3MEFFECTD */
			DoS3MEffectE,	/* UNI_S3MEFFECTE */
			DoS3MEffectF,	/* UNI_S3MEFFECTF */
			DoS3MEffectI,	/* UNI_S3MEFFECTI */
			DoS3MEffectQ,	/* UNI_S3MEFFECTQ */
			DoS3MEffectR,	/* UNI_S3MEFFECTR */
			DoS3MEffectT,	/* UNI_S3MEFFECTT */
			DoS3MEffectU,	/* UNI_S3MEFFECTU */
			DoKeyOff,		/* UNI_KEYOFF */
			DoKeyFade,		/* UNI_KEYFADE */
			DoVolEffects,	/* UNI_VOLEFFECTS */
			DoPTEffect4,	/* UNI_XMEFFECT4 */
			DoXMEffect6,	/* UNI_XMEFFECT6 */
			DoXMEffectA,	/* UNI_XMEFFECTA */
			DoXMEffectE1,	/* UNI_XMEFFECTE1 */
			DoXMEffectE2,	/* UNI_XMEFFECTE2 */
			DoXMEffectEA,	/* UNI_XMEFFECTEA */
			DoXMEffectEB,	/* UNI_XMEFFECTEB */
			DoXMEffectG,	/* UNI_XMEFFECTG */
			DoXMEffectH,	/* UNI_XMEFFECTH */
			DoXMEffectL,	/* UNI_XMEFFECTL */
			DoXMEffectP,	/* UNI_XMEFFECTP */
			DoXMEffectX1,	/* UNI_XMEFFECTX1 */
			DoXMEffectX2,	/* UNI_XMEFFECTX2 */
			DoITEffectG,	/* UNI_ITEFFECTG */
			DoITEffectH,	/* UNI_ITEFFECTH */
			DoITEffectI,	/* UNI_ITEFFECTI */
			DoITEffectM,	/* UNI_ITEFFECTM */
			DoITEffectN,	/* UNI_ITEFFECTN */
			DoITEffectP,	/* UNI_ITEFFECTP */
			DoITEffectT,	/* UNI_ITEFFECTT */
			DoITEffectU,	/* UNI_ITEFFECTU */
			DoITEffectW,	/* UNI_ITEFFECTW */
			DoITEffectY,	/* UNI_ITEFFECTY */
			DoNothing,		/* UNI_ITEFFECTZ */
			DoITEffectS0,	/* UNI_ITEFFECTS0 */
			DoULTEffect9,	/* UNI_ULTEFFECT9 */
			DoMEDSpeed,		/* UNI_MEDSPEED */
			DoMEDEffectF1,	/* UNI_MEDEFFECTF1 */
			DoMEDEffectF2,	/* UNI_MEDEFFECTF2 */
			DoMEDEffectF3,	/* UNI_MEDEFFECTF3 */
			DoOktArp,		/* UNI_OKTARP */
		];

		#endregion ()

		#region Player Functions
		static readonly Random s_Random = new();

		static readonly InternalModuleFormat s_UniTrack = new();

		static uint s_RandomSeed = 100;
		static uint FixedRandom(int ceil)
		{
			s_RandomSeed += 6;
			var result = s_RandomSeed;

			while (result > ceil)
			{
				result = 6;
			}

			return result;
		}

		static int GetRandom(int ceil)
			=> SetFixedRandom
				? (int)FixedRandom(ceil)
				: s_Random.Next(ceil);

		public static void Player_Start(Module mod)
		{
			int t;

			if (mod == null)
			{
				return;
			}

			if (!ModDriver.MikMod_Active())
			{
				_ = ModDriver.MikMod_EnableOutput();
			}

			mod.Forbid = false;

			if (s_Module != mod)
			{
				/* new song is being started, so completely stop out the old one. */
				if (s_Module != null)
				{
					s_Module.Forbid = true;
				}

				for (t = 0; t < ModDriver.md_sngchn; t++)
				{
					ModDriver.Voice_Stop_internal((byte)t);
				}
			}

			s_Module = mod;
		}

		public static void Player_Stop() => Player_Stop_internal();

		static void Player_Stop_internal()
		{
			if (ModDriver.SoundFXChannel == 0)
			{
				ModDriver.MikMod_DisableOutput_internal();
			}

			if (s_Module != null)
			{
				s_Module.Forbid = true;
			}

			s_Module = null;
		}

		public static void Player_Exit_internal(Module mod)
		{
			if (mod == null)
			{
				return;
			}

			/* Stop playback if necessary */
			if (mod == s_Module)
			{
				Player_Stop_internal();
				s_Module = null;
			}

			// Leave the rest of the freeing of space to the GC
		}

		public static bool Player_Mute_Channel(MuteOptions option, params int[] list)
		{
			var result = false;
			if (s_Module != null)
			{
				switch (option)
				{
					case MuteOptions.MuteRangeInclusive:
					{
						if (list.Length == 2)
						{
							var start = list[0];
							var end = list[1];
							if (start < end && end < s_Module.NumChannels && start > -1)
							{
								for (var i = start; i < end; i++)
								{
									Player_Mute_Channel(i);
								}

								result = true;
							}
						}

						break;
					}

					case MuteOptions.MuteRangeExclusive:
					{
						if (list.Length == 2)
						{
							var start = list[0] + 1;
							var end = list[1] - 1;
							if (start < end && end < s_Module.NumChannels && start > -1)
							{
								for (var i = start; i < end; i++)
								{
									Player_Mute_Channel(i);
								}

								result = true;
							}
						}

						break;
					}

					case MuteOptions.MuteList:
					{
						for (var i = 0; i < list.Length; i++)
						{
							Player_Mute_Channel(list[i]);
						}

						break;
					}

					case MuteOptions.MuteAll:
					{
						for (var i = 0; i < s_Module.Control.Length; i++)
						{
							Player_Mute_Channel(i);
						}

						break;
					}

					default:
						break;
				}
			}

			return result;
		}

		public static void Player_Mute_Channel(int channel)
		{
			if (s_Module != null)
			{
				if (channel < s_Module.Control.Length)
				{
					s_Module.Control[channel].muted = 1;
				}
			}
		}

		public static bool Player_UnMute_Channel(MuteOptions option, params int[] list)
		{
			var result = false;
			if (s_Module != null)
			{
				switch (option)
				{
					case MuteOptions.MuteRangeInclusive:
					{
						if (list.Length == 2)
						{
							var start = list[0];
							var end = list[1];
							if (start < end && end < s_Module.NumChannels && start > -1)
							{
								for (var i = start; i < end; i++)
								{
									Player_UnMute_Channel(i);
								}

								result = true;
							}
						}

						break;
					}

					case MuteOptions.MuteRangeExclusive:
					{
						if (list.Length == 2)
						{
							var start = list[0] + 1;
							var end = list[1] - 1;
							if (start < end && end < s_Module.NumChannels && start > -1)
							{
								for (var i = start; i < end; i++)
								{
									Player_UnMute_Channel(i);
								}

								result = true;
							}
						}

						break;
					}

					case MuteOptions.MuteList:
					{
						for (var i = 0; i < list.Length; i++)
						{
							Player_UnMute_Channel(list[i]);
						}

						break;
					}

					case MuteOptions.MuteAll:
					{
						for (var i = 0; i < s_Module.Control.Length; i++)
						{
							Player_UnMute_Channel(i);
						}

						break;
					}

					default:
						break;
				}
			}

			return result;
		}

		public static void Player_UnMute_Channel(int channel)
		{
			if (s_Module != null)
			{
				if (channel < s_Module.Control.Length)
				{
					s_Module.Control[channel].muted = 0;
				}
			}
		}

		public static void Player_SetPosition(ushort pos)
		{
			if (s_Module != null)
			{
				byte t;

				s_Module.Forbid = true;
				if (pos >= s_Module.NumPositions)
				{
					pos = s_Module.NumPositions;
				}

				s_Module.PositionJumpFlag = 2;
				s_Module.PatternNewStartPosition = 0;
				s_Module.Playback_SongPosition = (short)pos;
				s_Module.vbtick = s_Module.SongSpeed;

				for (t = 0; t < NumberOfVoices(s_Module); t++)
				{
					ModDriver.Voice_Stop_internal(t);
					s_Module.Voice[t].main.i = null;
					s_Module.Voice[t].main.s = null;
				}

				for (t = 0; t < s_Module.NumChannels; t++)
				{
					s_Module.Control[t].main.i = null;
					s_Module.Control[t].main.s = null;
				}

				s_Module.Forbid = false;

				if (pos == 0)
				{
					Player_Init_internal(s_Module);
				}
			}
		}

		public static bool Player_Init(Module mod)
		{
			mod.ExtendedSpeedFlag = true;
			mod.PanFlag = true;
			mod.Wrap = false;
			mod.Loop = true;
			mod.Fadeout = false;

			mod.RelativeSpeedFactor = 0;

			mod.Control = new MpControl[mod.NumChannels];
			for (var i = 0; i < mod.NumChannels; i++)
			{
				mod.Control[i] = new MpControl();
			}

			mod.Voice = new MpVoice[ModDriver.md_sngchn];
			for (var i = 0; i < ModDriver.md_sngchn; i++)
			{
				mod.Voice[i] = new MpVoice();
			}

			/* mod->numvoices was used during loading to clamp md_sngchn.
               After loading it's used to remember how big mod->voice is.
            */
			mod.NumVoices = ModDriver.md_sngchn;

			Player_Init_internal(mod);
			return false;
		}

		static void Player_Init_internal(Module mod)
		{
			int t;

			for (t = 0; t < mod.NumChannels; t++)
			{
				mod.Control[t].main.chanvol = (sbyte)mod.ChannelVolume[t];
				mod.Control[t].main.panning = (short)mod.Panning[t];
			}

			mod.SongTime = 0;
			mod.SongRemainder = 0;

			mod.pat_repcrazy = 0;
			mod.Playback_SongPosition = 0;

			if (mod.InitialSpeed != 0)
			{
				mod.SongSpeed = (ushort)(mod.InitialSpeed < 32 ? mod.InitialSpeed : 32);
			}
			else
			{
				mod.SongSpeed = 6;
			}

			mod.Volume = (short)(mod.InitialVolume > 128 ? 128 : mod.InitialVolume);

			mod.vbtick = mod.SongSpeed;
			mod.PatterDelayCounter1 = 0;
			mod.PatternDelayCounter2 = 0;
			mod.bpm = (ushort)(mod.InitialTempo < 32 ? 32 : mod.InitialTempo);
			mod.RealChannels = 0;

			mod.Playback_PatternRowNumber = 0;
			mod.PositionJumpFlag = 2; /* make sure the player fetches the first note */
			mod.NumRowsOnCurrentPattern = ushort.MaxValue;
			mod.PatternNewStartPosition = 0;
		}

		public static bool Player_Active()
		{
			var result = false;

			if (s_Module != null)
			{
				result = s_Module.Playback_SongPosition < s_Module.NumPositions;
			}

			return result;
		}

		public static void Player_NextPosition()
		{
			if (s_Module != null)
			{
				int t;

				s_Module.Forbid = true;
				s_Module.PositionJumpFlag = 3;
				s_Module.PatternNewStartPosition = 0;
				s_Module.vbtick = s_Module.SongSpeed;

				for (t = 0; t < NumberOfVoices(s_Module); t++)
				{
					ModDriver.Voice_Stop_internal((byte)t);
					s_Module.Voice[t].main.i = null;
					s_Module.Voice[t].main.s = null;
				}

				for (t = 0; t < s_Module.NumChannels; t++)
				{
					s_Module.Control[t].main.i = null;
					s_Module.Control[t].main.s = null;
				}

				s_Module.Forbid = false;
			}
		}

		public static void Player_PrevPosition()
		{
			if (s_Module != null)
			{
				int t;

				s_Module.Forbid = true;
				s_Module.PositionJumpFlag = 1;
				s_Module.PatternNewStartPosition = 0;
				s_Module.vbtick = s_Module.SongSpeed;

				for (t = 0; t < NumberOfVoices(s_Module); t++)
				{
					ModDriver.Voice_Stop_internal((byte)t);
					s_Module.Voice[t].main.i = null;
					s_Module.Voice[t].main.s = null;
				}

				for (t = 0; t < s_Module.NumChannels; t++)
				{
					s_Module.Control[t].main.i = null;
					s_Module.Control[t].main.s = null;
				}

				s_Module.Forbid = false;
			}
		}

		static bool Player_Paused_internal() => s_Module?.Forbid == true;

		public static bool Player_Paused() => Player_Paused_internal();

		public static void Player_TogglePause()
		{
			if (s_Module != null)
			{
				s_Module.Forbid = !s_Module.Forbid;
				ModDriver.Driver_Pause(s_Module.Forbid);
			}
		}

		public static void Player_HandleTick()
		{
			short channel;
			int max_volume;

			if (s_Module != null && s_Module.Playback_SongPosition >= s_Module.NumPositions)
			{
				Player_Stop();

				PlayStateChangedHandle?.Invoke(PlayerState.Stopped);

				return;
			}

			if ((s_Module?.Forbid != false) || (s_Module.Playback_SongPosition >= s_Module.NumPositions))
			{
				//ModDriver.isplaying = false;
				return;
			}

			PlayStateChangedHandle?.Invoke(PlayerState.Updated);

			/* update time counter (sngtime is in milliseconds (in fact 2^-10)) */
			s_Module.SongRemainder += (1 << 9) * 5; /* thus 2.5*(1<<10), since fps=0.4xtempo */
			s_Module.SongTime += (uint)(s_Module.SongRemainder / s_Module.bpm);
			s_Module.SongRemainder %= s_Module.bpm;

			if (++s_Module.vbtick >= s_Module.SongSpeed)
			{
				if (s_Module.pat_repcrazy != 0)
				{
					s_Module.pat_repcrazy = 0; /* play 2 times row 0 */
				}
				else
				{
					s_Module.Playback_PatternRowNumber++;
				}

				s_Module.vbtick = 0;

				/* process pattern-delay. s_Module.patdly2 is the counter and s_Module.patdly is
				   the command memory. */
				if (s_Module.PatterDelayCounter1 != 0)
				{
					s_Module.PatternDelayCounter2 = s_Module.PatterDelayCounter1;
					s_Module.PatterDelayCounter1 = 0;
				}

				if (s_Module.PatternDelayCounter2 != 0)
				{
					/* patterndelay active */
					if (--s_Module.PatternDelayCounter2 != 0)
					{
						/* so turn back s_Module.patpos by 1 */
						if (s_Module.Playback_PatternRowNumber != 0)
						{
							s_Module.Playback_PatternRowNumber--;
						}
					}
				}

				/* do we have to get a new patternpointer ? (when s_Module.patpos reaches the
				   pattern size, or when a patternbreak is active) */
				if ((s_Module.Playback_PatternRowNumber >= s_Module.NumRowsOnCurrentPattern) && (s_Module.NumRowsOnCurrentPattern > 0) && (s_Module.PositionJumpFlag == 0))
				{
					s_Module.PositionJumpFlag = 3;
				}

				if (s_Module.PositionJumpFlag != 0)
				{
					s_Module.Playback_PatternRowNumber = (ushort)(s_Module.NumRowsOnCurrentPattern != 0 ? (s_Module.PatternNewStartPosition % s_Module.NumRowsOnCurrentPattern) : 0);
					s_Module.pat_repcrazy = 0;
					s_Module.Playback_SongPosition += (short)(s_Module.PositionJumpFlag - 2);

					for (channel = 0; channel < s_Module.NumChannels; channel++)
					{
						s_Module.Control[channel].pat_reppos = -1;
					}

					s_Module.PatternNewStartPosition = 0;
					s_Module.PositionJumpFlag = 0;

					if (s_Module.Playback_SongPosition < 0)
					{
						s_Module.Playback_SongPosition = (short)(s_Module.NumPositions - 1);
					}

					/* handle the "---" (end of song) pattern since it can occur
					   *inside* the module in some formats */
					if ((s_Module.Playback_SongPosition >= s_Module.NumPositions) || (s_Module.Positions[s_Module.Playback_SongPosition] == Constants.LAST_PATTERN))
					{
						if (!s_Module.Wrap)
						{
							return;
						}

						if ((s_Module.Playback_SongPosition = (short)s_Module.RestartPosition) == 0)
						{
							s_Module.Volume = (short)(s_Module.InitialVolume > 128 ? 128 : s_Module.InitialVolume);

							if (s_Module.InitialSpeed != 0)
							{
								s_Module.SongSpeed = (ushort)(s_Module.InitialSpeed < 32 ? s_Module.InitialSpeed : 32);
							}
							else
							{
								s_Module.SongSpeed = 6;
							}

							s_Module.bpm = (ushort)(s_Module.InitialTempo < 32 ? 32 : s_Module.InitialTempo);
						}
					}
				}

				if (s_Module.PatternDelayCounter2 == 0)
				{
					pt_Notes(s_Module);
				}
			}

			/* Fade global volume if enabled and we're playing the last pattern */
			if (((s_Module.Playback_SongPosition == s_Module.NumPositions - 1) ||
				 (s_Module.Positions[s_Module.Playback_SongPosition + 1] == Constants.LAST_PATTERN)) &&
				s_Module.Fadeout)
			{
				max_volume = s_Module.NumRowsOnCurrentPattern != 0 ? (s_Module.NumRowsOnCurrentPattern - s_Module.Playback_PatternRowNumber) * 128 / s_Module.NumRowsOnCurrentPattern : 0;
			}
			else
			{
				max_volume = 128;
			}

			pt_EffectsPass1(s_Module);

			if ((s_Module.Flags & Constants.UF_NNA) == Constants.UF_NNA)
			{
				pt_NNA(s_Module);
			}

			pt_SetupVoices(s_Module);

			pt_EffectsPass2(s_Module);

			pt_UpdateVoices(s_Module, max_volume);

		}

		#endregion

		#region pt functions
		static void pt_UpdateVoices(Module mod, int max_volume)
		{
			short envpan, envvol, envpit, channel;
			ushort playperiod;
			int vibval, vibdpt;
			uint tmpvol;

			MpVoice aout;
			Instrument i;
			Sample s;

			mod.TotalChannels = mod.RealChannels = 0;
			for (channel = 0; channel < NumberOfVoices(mod); channel++)
			{
				aout = mod.Voice[channel];
				i = aout.main.i;
				s = aout.main.s;

				if (s == null || s.length == 0)
				{
					continue;
				}

				if (aout.main.period < 40)
				{
					aout.main.period = 40;
				}
				else if (aout.main.period > 50000)
				{
					aout.main.period = 50000;
				}

				if (aout.main.kick is Constants.KICK_NOTE or Constants.KICK_KEYOFF)
				{
					ModDriver.Voice_Play_internal((sbyte)channel, s,
						(uint)((aout.main.start == -1) ? ((s.flags & Constants.SF_UST_LOOP) == Constants.SF_UST_LOOP ? s.loopstart : 0) : aout.main.start));
					aout.main.fadevol = 32768;
					aout.aswppos = 0;
				}

				envvol = 256;
				envpan = Constants.PAN_CENTER;
				envpit = 32;
				if (i != null && ((aout.main.kick == Constants.KICK_NOTE) || (aout.main.kick == Constants.KICK_ENV)))
				{
					if ((aout.main.volflg & Constants.EF_ON) == Constants.EF_ON)
					{
						envvol = StartEnvelope(aout.venv, aout.main.volflg,
						  i.volpts, i.volsusbeg, i.volsusend,
						  i.volbeg, i.volend, i.volenv, aout.main.keyoff);
					}

					if ((aout.main.panflg & Constants.EF_ON) == Constants.EF_ON)
					{
						envpan = StartEnvelope(aout.penv, aout.main.panflg,
						  i.panpts, i.pansusbeg, i.pansusend,
						  i.panbeg, i.panend, i.panenv, aout.main.keyoff);
					}

					if ((aout.main.pitflg & Constants.EF_ON) == Constants.EF_ON)
					{
						envpit = StartEnvelope(aout.cenv, aout.main.pitflg,
						  i.pitpts, i.pitsusbeg, i.pitsusend,
						  i.pitbeg, i.pitend, i.pitenv, aout.main.keyoff);
					}

					if ((aout.cenv.flg & Constants.EF_ON) == Constants.EF_ON)
					{
						aout.masterperiod = GetPeriod(mod.Flags, (ushort)(aout.main.note << 1), aout.master.speed);
					}
				}
				else
				{
					if ((aout.main.volflg & Constants.EF_ON) == Constants.EF_ON)
					{
						envvol = ProcessEnvelope(aout, aout.venv, 256);
					}

					if ((aout.main.panflg & Constants.EF_ON) == Constants.EF_ON)
					{
						envpan = ProcessEnvelope(aout, aout.penv, Constants.PAN_CENTER);
					}

					if ((aout.main.pitflg & Constants.EF_ON) == Constants.EF_ON)
					{
						envpit = ProcessEnvelope(aout, aout.cenv, 32);
					}
				}

				if (aout.main.kick == Constants.KICK_NOTE)
				{
					aout.main.kick_flag = 1;
				}

				aout.main.kick = Constants.KICK_ABSENT;

				tmpvol = aout.main.fadevol; /* max 32768 */
				tmpvol *= (uint)aout.main.chanvol;  /* * max 64 */
				tmpvol *= (uint)aout.main.outvolume;    /* * max 256 */
				tmpvol /= 256 * 64;           /* tmpvol is max 32768 again */
				aout.totalvol = tmpvol >> 2;    /* used to determine samplevolume */
				tmpvol *= (uint)envvol;             /* * max 256 */
				tmpvol *= (uint)mod.Volume;         /* * max 128 */
				tmpvol /= 128 * 256 * 128;

				/* fade out */
				if (mod.Playback_SongPosition >= mod.NumPositions)
				{
					tmpvol = 0;
				}
				else
				{
					tmpvol = (uint)(tmpvol * max_volume / 128);
				}

				if ((aout.masterchn != -1) && mod.Control[aout.masterchn].muted != 0)
				{
					ModDriver.Voice_SetVolume_internal((sbyte)channel, 0);
				}
				else
				{
					ModDriver.Voice_SetVolume_internal((sbyte)channel, (ushort)tmpvol);

					if ((tmpvol != 0) && (aout.master != null) && (aout.master.slave == aout))
					{
						mod.RealChannels++;
					}

					mod.TotalChannels++;
				}

				if (aout.main.panning == Constants.PAN_SURROUND)
				{
					ModDriver.Voice_SetPanning_internal((sbyte)channel, Constants.PAN_SURROUND);
				}
				else
					if (mod.PanFlag && (aout.penv.flg & Constants.EF_ON) == Constants.EF_ON)
				{
					ModDriver.Voice_SetPanning_internal((sbyte)channel, (uint)DoPan(envpan, aout.main.panning));
				}
				else
				{
					ModDriver.Voice_SetPanning_internal((sbyte)channel, (uint)aout.main.panning);
				}

				if (aout.main.period != 0 && s.vibdepth != 0)
				{
					switch (s.vibtype)
					{
						case 0:
							vibval = avibtab[s.avibpos & 127];
							if ((aout.avibpos & 0x80) == 0x80)
							{
								vibval = -vibval;
							}

							break;
						case 1:
							vibval = 64;
							if ((aout.avibpos & 0x80) == 0x80)
							{
								vibval = -vibval;
							}

							break;
						case 2:
							vibval = 63 - (((aout.avibpos + 128) & 255) >> 1);
							break;
						default:
							vibval = (((aout.avibpos + 128) & 255) >> 1) - 64;
							break;
					}
				}
				else
				{
					vibval = 0;
				}

				if ((s.vibflags & Constants.AV_IT) == Constants.AV_IT)
				{
					if ((aout.aswppos >> 8) < s.vibdepth)
					{
						aout.aswppos += s.vibsweep;
						vibdpt = aout.aswppos;
					}
					else
					{
						vibdpt = s.vibdepth << 8;
					}

					vibval = (vibval * vibdpt) >> 16;
					if (aout.mflag)
					{
						if ((mod.Flags & Constants.UF_LINEAR) != Constants.UF_LINEAR)
						{
							vibval >>= 1;
						}

						aout.main.period -= (ushort)vibval;
					}
				}
				else
				{
					/* do XM style auto-vibrato */
					if ((aout.main.keyoff & Constants.KEY_OFF) != Constants.KEY_OFF)
					{
						if (aout.aswppos < s.vibsweep)
						{
							vibdpt = aout.aswppos * s.vibdepth / s.vibsweep;
							aout.aswppos++;
						}
						else
						{
							vibdpt = s.vibdepth;
						}
					}
					else
					{
						/* keyoff . depth becomes 0 if final depth wasn't reached or
						   stays at final level if depth WAS reached */
						if (aout.aswppos >= s.vibsweep)
						{
							vibdpt = s.vibdepth;
						}
						else
						{
							vibdpt = 0;
						}
					}

					vibval = (vibval * vibdpt) >> 8;
					aout.main.period -= (ushort)vibval;
				}

				/* update vibrato position */
				aout.avibpos = (ushort)((aout.avibpos + s.vibrate) & 0xff);

				/* process pitch envelope */
				playperiod = aout.main.period;

				if ((aout.main.pitflg & Constants.EF_ON) == Constants.EF_ON && (envpit != 32))
				{
					long p1;

					envpit -= 32;
					if ((aout.main.note << 1) + envpit <= 0)
					{
						envpit = (short)-(aout.main.note << 1);
					}

					p1 = GetPeriod(mod.Flags, (ushort)((aout.main.note << 1) + envpit), aout.master.speed) - aout.masterperiod;
					if (p1 > 0)
					{
						if ((ushort)(playperiod + p1) <= playperiod)
						{
							p1 = 0;
							aout.main.keyoff |= Constants.KEY_OFF;
						}
					}
					else if (p1 < 0)
					{
						if ((ushort)(playperiod + p1) >= playperiod)
						{
							p1 = 0;
							aout.main.keyoff |= Constants.KEY_OFF;
						}
					}

					playperiod += (ushort)p1;
				}

				if (aout.main.fadevol == 0)
				{ /* check for a dead note (fadevol=0) */
					ModDriver.Voice_Stop_internal((byte)channel);
					mod.TotalChannels--;
					if ((tmpvol != 0) && (aout.master != null) && (aout.master.slave == aout))
					{
						mod.RealChannels--;
					}
				}
				else
				{
					ModDriver.Voice_SetFrequency_internal((sbyte)channel, GetFrequency(mod.Flags, playperiod));

					/* if keyfade, start substracting fadeoutspeed from fadevol: */
					if ((i != null) && (aout.main.keyoff & Constants.KEY_FADE) == Constants.KEY_FADE)
					{
						if (aout.main.fadevol >= i.volfade)
						{
							aout.main.fadevol -= i.volfade;
						}
						else
						{
							aout.main.fadevol = 0;
						}
					}
				}

				ModDriver.Bpm = (ushort)(mod.bpm + mod.RelativeSpeedFactor);
				if (ModDriver.Bpm < 32)
				{
					ModDriver.Bpm = 32;
				}
				else if (((mod.Flags & Constants.UF_HIGHBPM) != Constants.UF_HIGHBPM) && ModDriver.Bpm > 255)
				{
					ModDriver.Bpm = 255;
				}
			}
		}

		static void pt_EffectsPass2(Module mod)
		{
			short channel;
			MpControl a;
			byte c;

			for (channel = 0; channel < mod.NumChannels; channel++)
			{
				a = mod.Control[channel];

				if (a.row == null)
				{
					continue;
				}

				s_UniTrack.UniSetRow(a.row, a.rowPos);

				while ((c = s_UniTrack.UniGetByte()) != 0)
				{
					if (c == (byte)Commands.UNI_ITEFFECTS0)
					{
						c = s_UniTrack.UniGetByte();
						if ((c >> 4) == (int)ExtentedEffects.SS_S7EFFECTS)
						{
							DoNNAEffects(mod, a, (byte)(c & 0xf));
						}
					}
					else
					{
						s_UniTrack.UniSkipOpcode();
					}
				}
			}
		}

		static void pt_SetupVoices(Module mod)
		{
			short channel;
			MpControl a;
			MpVoice aout;

			for (channel = 0; channel < mod.NumChannels; channel++)
			{
				a = mod.Control[channel];

				if (a.main.notedelay != 0)
				{
					continue;
				}

				if (a.main.kick == Constants.KICK_NOTE)
				{
					/* if no channel was cut above, find an empty or quiet channel
					   here */
					if ((mod.Flags & Constants.UF_NNA) == Constants.UF_NNA)
					{
						if (a.slave == null)
						{
							int newchn;

							if ((newchn = MP_FindEmptyChannel(mod)) != -1)
							{
								a.slavechn = (byte)newchn;
								a.slave = mod.Voice[newchn];
							}
						}
					}
					else
					{
						a.slavechn = (byte)channel;
						a.slave = mod.Voice[channel];
					}

					/* assign parts of MP_VOICE only done for a KICK_NOTE */
					if ((aout = a.slave) != null)
					{
						if (aout.mflag && aout.master != null)
						{
							aout.master.slave = null;
						}

						aout.master = a;
						a.slave = aout;
						aout.masterchn = channel;
						aout.mflag = true;
					}
				}
				else
				{
					aout = a.slave;
				}

				if (aout != null)
				{
					a.main.CloneTo(aout.main);
					//aout.main = a.main.Clone();
				}

				a.main.kick = Constants.KICK_ABSENT;
			}
		}

		static void pt_NNA(Module mod)
		{
			short channel;
			MpControl a;

			for (channel = 0; channel < mod.NumChannels; channel++)
			{
				a = mod.Control[channel];

				if (a.main.kick == Constants.KICK_NOTE)
				{
					if (a.slave != null)
					{
						var aout = a.slave;
						if ((aout.main.nna & Constants.NNA_MASK) != 0)
						{
							/* Make sure the old MP_VOICE channel knows it has no
							   master now ! */
							a.slave = null;
							/* assume the channel is taken by NNA */
							aout.mflag = false;

							switch (aout.main.nna)
							{
								case Constants.NNA_CONTINUE: /* continue note, do nothing */
									break;
								case Constants.NNA_OFF: /* note off */
									aout.main.keyoff |= Constants.KEY_OFF;
									if (((aout.main.volflg & Constants.EF_ON) != Constants.EF_ON) ||
										  (aout.main.volflg & Constants.EF_LOOP) == Constants.EF_LOOP)
									{
										aout.main.keyoff = Constants.KEY_KILL;
									}

									break;
								case Constants.NNA_FADE:
									aout.main.keyoff |= Constants.KEY_FADE;
									break;
							}
						}
					}

					if (a.dct != Constants.DCT_OFF)
					{
						int t;

						for (t = 0; t < NumberOfVoices(mod); t++)
						{
							if ((!ModDriver.Voice_Stopped_internal((sbyte)t)) &&
							   (mod.Voice[t].masterchn == channel) &&
							   (a.main.sample == mod.Voice[t].main.sample))
							{
								var kill = false;
								switch (a.dct)
								{
									case Constants.DCT_NOTE:
										if (a.main.note == mod.Voice[t].main.note)
										{
											kill = true;
										}

										break;
									case Constants.DCT_SAMPLE:
										if (a.main.handle == mod.Voice[t].main.handle)
										{
											kill = true;
										}

										break;
									case Constants.DCT_INST:
										kill = true;
										break;
								}

								if (kill)
								{
									switch (a.dca)
									{
										case Constants.DCA_CUT:
											mod.Voice[t].main.fadevol = 0;
											break;
										case Constants.DCA_OFF:
											mod.Voice[t].main.keyoff |= Constants.KEY_OFF;
											if (((mod.Voice[t].main.volflg & Constants.EF_ON) != Constants.EF_ON) ||
												(mod.Voice[t].main.volflg & Constants.EF_LOOP) == Constants.EF_LOOP)
											{
												mod.Voice[t].main.keyoff = Constants.KEY_KILL;
											}

											break;
										case Constants.DCA_FADE:
											mod.Voice[t].main.keyoff |= Constants.KEY_FADE;
											break;
									}
								}
							}
						}
					}
				} /* if (a.main.kick==KICK_NOTE) */
			}
		}

		static void pt_EffectsPass1(Module mod)
		{
			short channel;
			MpControl a;
			MpVoice aout;
			int explicitslides;

			for (channel = 0; channel < mod.NumChannels; channel++)
			{
				a = mod.Control[channel];

				if ((aout = a.slave) != null)
				{
					a.main.fadevol = aout.main.fadevol;
					a.main.period = aout.main.period;
					if (a.main.kick == Constants.KICK_KEYOFF)
					{
						a.main.keyoff = aout.main.keyoff;
					}
				}

				if (a.row == null)
				{
					continue;
				}

				s_UniTrack.UniSetRow(a.row, a.rowPos);

				a.ownper = a.ownvol = 0;
				explicitslides = pt_playeffects(mod, channel, a);

				/* continue volume slide if necessary for XM and IT */
				if ((mod.Flags & Constants.UF_BGSLIDES) == Constants.UF_BGSLIDES)
				{
					if (explicitslides == 0 && a.sliding != 0)
					{
						DoS3MVolSlide(mod.vbtick, mod.Flags, a, 0);
					}
					else if (a.tmpvolume != 0)
					{
						a.sliding = (sbyte)explicitslides;
					}
				}

				if (a.ownper == 0)
				{
					a.main.period = a.tmpperiod;
				}

				if (a.ownvol == 0)
				{
					a.volume = a.tmpvolume;
				}

				if (a.main.s != null)
				{
					if (a.main.i != null)
					{
						a.main.outvolume = (short)((a.volume * a.main.s.globvol * a.main.i.globvol) >> 10);
					}
					else
					{
						a.main.outvolume = (short)((a.volume * a.main.s.globvol) >> 4);
					}

					if (a.main.outvolume > 256)
					{
						a.main.outvolume = 256;
					}
					else if (a.main.outvolume < 0)
					{
						a.main.outvolume = 0;
					}
				}
			}
		}

		static int pt_playeffects(Module mod, short channel, MpControl a)
		{
			var tick = mod.vbtick;
			var flags = mod.Flags;
			byte c;
			var explicitslides = 0;

			effectDelegate f;

			while ((c = s_UniTrack.UniGetByte()) != 0)
			{
				f = effects[c];

				if (f != DoNothing)
				{
					a.sliding = 0;
				}

				explicitslides |= f(tick, flags, a, mod, channel);

			}

			return explicitslides;
		}

		static void pt_Notes(Module mod)
		{
			short channel;
			MpControl a;
			byte c, inst;
			int tr, funky; /* funky is set to indicate note or instrument change */

			for (channel = 0; channel < mod.NumChannels; channel++)
			{
				a = mod.Control[channel];

				if (mod.Playback_SongPosition >= mod.NumPositions)
				{
					tr = mod.NumTracks;
					mod.NumRowsOnCurrentPattern = 0;
				}
				else
				{
					tr = mod.Patterns[(mod.Positions[mod.Playback_SongPosition] * mod.NumChannels) + channel];
					mod.NumRowsOnCurrentPattern = mod.PatternRows[mod.Positions[mod.Playback_SongPosition]];
				}

				if (tr < mod.NumTracks)
				{
					var place = InternalModuleFormat.UniFindRow(mod.Tracks[tr], mod.Playback_PatternRowNumber);
					a.row = mod.Tracks[tr];
					a.rowPos = place;
				}
				else
				{
					a.row = null;
				}

				a.newsamp = 0;
				if (mod.vbtick == 0)
				{
					a.main.notedelay = 0;
				}

				if (a.row == null)
				{
					continue;
				}

				s_UniTrack.UniSetRow(a.row, a.rowPos);
				funky = 0;

				while ((c = s_UniTrack.UniGetByte()) != 0)
				{
					switch (c)
					{
						case (byte)Commands.UNI_NOTE:
						{
							funky |= 1;
							a.oldnote = a.anote;
							a.anote = s_UniTrack.UniGetByte();
							a.main.kick = Constants.KICK_NOTE;
							a.main.start = -1;
							a.sliding = 0;

							/* retrig tremolo and vibrato waves ? */
							if ((a.wavecontrol & 0x80) != 0x80)
							{
								a.trmpos = 0;
							}

							if ((a.wavecontrol & 0x08) != 0x08)
							{
								a.vibpos = 0;
							}

							if (a.panbwave == 0)
							{
								a.panbpos = 0;
							}

							break;
						}

						case (byte)Commands.UNI_INSTRUMENT:
						{

							inst = s_UniTrack.UniGetByte();
							if (inst >= mod.NumInstruments)
							{
								break; /* safety valve */
							}

							funky |= 2;
							a.main.i = (mod.Flags & Constants.UF_INST) == Constants.UF_INST ? mod.Instruments[inst] : null;
							a.retrig = 0;
							a.s3mtremor = 0;
							a.ultoffset = 0;
							a.main.sample = inst;
							break;
						}

						default:
						{
							s_UniTrack.UniSkipOpcode();
							break;
						}
					}
				}

				if (funky != 0)
				{
					Instrument i;
					Sample s;

					if ((i = a.main.i) != null)
					{
						if (i.samplenumber[a.anote] >= mod.NumSamples)
						{
							continue;
						}

						s = mod.Samples[i.samplenumber[a.anote]];
						a.main.note = i.samplenote[a.anote];
					}
					else
					{
						a.main.note = a.anote;
						s = mod.Samples[a.main.sample];
					}

					if (a.main.s != s)
					{
						a.main.s = s;
						a.newsamp = a.main.period;
					}

					/* channel or instrument determined panning ? */
					a.main.panning = (short)mod.Panning[channel];
					if ((s.flags & Constants.SF_OWNPAN) == Constants.SF_OWNPAN)
					{
						a.main.panning = s.panning;
					}
					else if ((i != null) && (i.flags & Constants.IF_OWNPAN) == Constants.IF_OWNPAN)
					{
						a.main.panning = i.panning;
					}

					a.main.handle = s.handle;
					a.speed = s.speed;

					if (i != null)
					{
						if (mod.PanFlag && (i.flags & Constants.IF_PITCHPAN) == Constants.IF_PITCHPAN
							&& (a.main.panning != Constants.PAN_SURROUND))
						{
							a.main.panning +=
								(short)((a.anote - i.pitpancenter) * i.pitpansep / 8);
							if (a.main.panning < Constants.PAN_LEFT)
							{
								a.main.panning = Constants.PAN_LEFT;
							}
							else if (a.main.panning > Constants.PAN_RIGHT)
							{
								a.main.panning = Constants.PAN_RIGHT;
							}
						}

						a.main.pitflg = i.pitflg;
						a.main.volflg = i.volflg;
						a.main.panflg = i.panflg;
						a.main.nna = i.nnatype;
						a.dca = i.dca;
						a.dct = i.dct;
					}
					else
					{
						a.main.pitflg = a.main.volflg = a.main.panflg = 0;
						a.main.nna = a.dca = 0;
						a.dct = Constants.DCT_OFF;
					}

					if ((funky & 2) == 2) /* instrument change */
					{
						/* IT random volume variations: 0:8 bit fixed, and one bit for
							sign. */
						a.volume = a.tmpvolume = s.volume;
						if ((s != null) && (i != null))
						{
							if (i.rvolvar != 0)
							{
								a.volume = a.tmpvolume = (short)(s.volume + (byte)(s.volume * (i.rvolvar * GetRandom(512)) / 25600));

								if (a.volume < 0)
								{
									a.volume = a.tmpvolume = 0;
								}
								else if (a.volume > 64)
								{
									a.volume = a.tmpvolume = 64;
								}
							}

							if (mod.PanFlag && (a.main.panning != Constants.PAN_SURROUND))
							{
								a.main.panning += (short)(a.main.panning * (i.rpanvar * GetRandom(512)) / 25600);
								if (a.main.panning < Constants.PAN_LEFT)
								{
									a.main.panning = Constants.PAN_LEFT;
								}
								else if (a.main.panning > Constants.PAN_RIGHT)
								{
									a.main.panning = Constants.PAN_RIGHT;
								}
							}
						}
					}

					a.wantedperiod = a.tmpperiod = GetPeriod(mod.Flags, (ushort)(a.main.note << 1), a.speed);
					a.main.keyoff = Constants.KEY_KICK;
				}
			}
		}
		#endregion

		#region Implementation functions
		static ushort GetPeriod(ushort flags, ushort note, uint speed)
		{
			if ((flags & Constants.UF_XMPERIODS) == Constants.UF_XMPERIODS)
			{
				if ((flags & Constants.UF_LINEAR) == Constants.UF_LINEAR)
				{
					return GetLinearPeriod(note, speed);
				}
				else
				{
					return GetLogPeriod(note, speed);
				}
			}
			else
			{
				return GetOldPeriod(note, speed);
			}
		}

		internal static ushort GetLinearPeriod(ushort note, uint fine)
		{
			var t = (int)(((((20 + (2 * HIGH_OCTAVE)) * Constants.Octave) + 2 - note) * 32) - (fine >> 1));
			return (ushort)t;
		}

		static ushort GetLogPeriod(ushort note, uint fine)
		{
			ushort n, o;
			ushort p1, p2;
			uint i;

			n = (ushort)(note % (2 * Constants.Octave));
			o = (ushort)(note / (2 * Constants.Octave));
			i = (uint)((n << 2) + (fine >> 4)); /* n*8 + fine/16 */

			p1 = logtab[i];

			// More MikMod running off buffers, sigh
			if (i + 1 < logtab.Length)
			{
				p2 = logtab[i + 1];
			}
			else
			{
				p2 = 512;
			}

			return (ushort)(Interpolate((short)(fine >> 4), 0, 15, (short)p1, (short)p2) >> o);
		}

		static ushort GetOldPeriod(ushort note, uint speed)
		{
			ushort n, o;

			/* This happens sometimes on badly converted AMF, and old MOD */
			if (speed == 0)
			{
				throw new Exception("mplayer: getoldperiod() called with note=" + note + ", speed=0 !");
				//return 4242; /* <- prevent divide overflow.. (42 hehe) */
			}

			n = (ushort)(note % (2 * Constants.Octave));
			o = (ushort)(note / (2 * Constants.Octave));
			return (ushort)(((8363 * (uint)oldperiods[n]) >> o) / speed);
		}

		static short InterpolateEnv(short p, EnvPt a, EnvPt b) => Interpolate(p, a.pos, b.pos, a.val, b.val);

		static short Interpolate(short p, short p1, short p2, short v1, short v2)
		{
			if ((p1 == p2) || (p == p1))
			{
				return v1;
			}

			return (short)(v1 + ((p - p1) * (v2 - v1) / (p2 - p1)));
		}

		static short StartEnvelope(EnvPr t, byte flg, byte pts, byte susbeg, byte susend, byte beg, byte end, EnvPt[] p, byte keyoff)
		{
			t.flg = flg;
			t.pts = pts;
			t.susbeg = susbeg;
			t.susend = susend;
			t.beg = beg;
			t.end = end;
			t.env = p;
			t.p = 0;
			t.a = 0;
			t.b = (ushort)(((t.flg & Constants.EF_SUSTAIN) == Constants.EF_SUSTAIN && ((keyoff & Constants.KEY_OFF) != Constants.KEY_OFF)) ? 0 : 1);

			/* Imago Orpheus sometimes stores an extra initial point in the envelope */
			if ((t.pts >= 2) && (t.env[0].pos == t.env[1].pos))
			{
				t.a++;
				t.b++;
			}

			/* Fit in the envelope, still */
			if (t.a >= t.pts)
			{
				t.a = (ushort)(t.pts - 1);
			}

			if (t.b >= t.pts)
			{
				t.b = (ushort)(t.pts - 1);
			}

			// The original C code just let a be larger then the env buffer, and thus end up taking what ever value
			// was in memory at that point. this can easily happen if t.pts is 0 and then when we take 1 away its
			// the max value of a ushort. 
			// We can't do that in C#, so I'm deciding to just return 0 at that point.
			if (t.a < t.env.Length)
			{
				return t.env[t.a].val;
			}
			else
			{
				return 0;
			}
		}

		static int MP_FindEmptyChannel(Module mod)
		{
			var a = 0;
			uint t, k, tvol, pp;

			for (t = 0; t < NumberOfVoices(mod); t++)
			{
				if (((mod.Voice[t].main.kick == Constants.KICK_ABSENT) ||
					 (mod.Voice[t].main.kick == Constants.KICK_ENV)) &&
				   ModDriver.Voice_Stopped_internal((sbyte)t))
				{
					return (int)t;
				}
			}

			tvol = 0xffffff;
			t = uint.MaxValue;

			for (k = 0; k < NumberOfVoices(mod); k++, a++)
			{
				/* allow us to take over a nonexisting sample */
				if (mod.Voice[a].main.s == null)
				{
					return (int)k;
				}

				if (mod.Voice[a].main.kick is Constants.KICK_ABSENT or Constants.KICK_ENV)
				{
					pp = mod.Voice[a].totalvol << ((mod.Voice[a].main.s.flags & Constants.SF_LOOP) == Constants.SF_LOOP ? 1 : 0);
					if ((mod.Voice[a].master != null) && (mod.Voice[a] == mod.Voice[a].master.slave))
					{
						pp <<= 2;
					}

					if (pp < tvol)
					{
						tvol = pp;
						t = k;
					}
				}
			}

			if (tvol > 8000 * 7)
			{
				return -1;
			}

			return (int)t;
		}

		static short ProcessEnvelope(MpVoice aout, EnvPr t, short v)
		{
			if ((t.flg & Constants.EF_ON) != 0)
			{
				byte a, b;      /* actual points in the envelope */
				ushort p;       /* the 'tick counter' - real point being played */

				a = (byte)t.a;
				b = (byte)t.b;
				p = (ushort)t.p;

				/*
				 * Sustain loop on one point (XM type).
				 * Not processed if KEYOFF.
				 * Don't move and don't interpolate when the point is reached
				 */
				if ((t.flg & Constants.EF_SUSTAIN) != 0 && t.susbeg == t.susend &&
				   (aout.main.keyoff & Constants.KEY_OFF) == 0 && p == t.env[t.susbeg].pos)
				{
					v = t.env[t.susbeg].val;
				}
				else
				{
					/*
					 * All following situations will require interpolation between
					 * two envelope points.
					 */

					/*
					 * Sustain loop between two points (IT type).
					 * Not processed if KEYOFF.
					 */
					/* if we were on a loop point, loop now */
					if ((t.flg & Constants.EF_SUSTAIN) != 0 && (aout.main.keyoff & Constants.KEY_OFF) == 0 &&
					   a >= t.susend)
					{
						a = t.susbeg;
						b = (byte)((t.susbeg == t.susend) ? a : a + 1);
						p = (ushort)t.env[a].pos;
						v = t.env[a].val;
					}
					else
						/*
						 * Regular loop.
						 * Be sure to correctly handle single point loops.
						 */
						if ((t.flg & Constants.EF_LOOP) != 0 && a >= t.end)
					{
						a = t.beg;
						b = (byte)(t.beg == t.end ? a : a + 1);
						p = (ushort)t.env[a].pos;
						v = t.env[a].val;
					}
					else
							/*
							 * Non looping situations.
							 */
							if (a != b)
					{
						v = InterpolateEnv((short)p, t.env[a], t.env[b]);
					}
					else
					{
						if (a < t.env.Length)
						{
							v = t.env[a].val;
						}
						else
						{
							v = 0;
						}
					}

					/*
					 * Start to fade if the volume envelope is finished.
					 */

					// More bounds checking, not sure about this one!
					var place = (uint)(t.pts - 1);
					if (place > t.env.Length || (place < t.env.Length && p >= t.env[place].pos))
					{
						if ((t.flg & Constants.EF_VOLENV) != 0)
						{
							aout.main.keyoff |= Constants.KEY_FADE;
							if (v == 0)
							{
								aout.main.fadevol = 0;
							}
						}
					}
					else
					{
						p++;
						/* did pointer reach point b? */
						if (p >= t.env[b].pos)
						{
							a = b++; /* shift points a and b */
						}
					}

					t.a = a;
					t.b = b;
					t.p = (short)p;
				}
			}

			return v;
		}

		static short DoPan(short envpan, short pan)
		{
			var newpan = pan + ((envpan - Constants.PAN_CENTER) * (128 - Math.Abs(pan - Constants.PAN_CENTER)) / 128);
			return (short)((newpan < Constants.PAN_LEFT) ? Constants.PAN_LEFT : (newpan > Constants.PAN_RIGHT ? Constants.PAN_RIGHT : newpan));
		}

		/* XM linear period to frequency conversion */
		internal static uint GetFrequency(ushort flags, uint period)
		{
			unchecked
			{
				if ((flags & Constants.UF_LINEAR) != 0)
				{
					var shift = ((int)period / 768) - HIGH_OCTAVE;

					if (shift >= 0)
					{
						return lintab[period % 768] >> shift;
					}
					else
					{
						return lintab[period % 768] << (-shift);
					}
				}
				else
				{
					return (uint)(8363L * 1712L / (period != 0 ? period : 1));
				}
			}
		}
		#endregion

		/* Get current module order */

		/* Get current module row */

	}
}
