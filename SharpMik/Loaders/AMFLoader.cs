﻿using System;
using System.IO;

using SharpMik.Interfaces;
using SharpMik.Attributes;
using SharpMik.Common;

namespace SharpMik.Loaders
{

	[ModFileExtentions(".amf")]
	public class AMFLoader : IModLoader
	{
		/*========== Module structure */

		class AMFHEADER
		{
			public string id;           /* AMF file marker */
			public byte version;            /* upper major, lower nibble minor version number */
			public string SongName;     /* ASCIIZ SongName */
			public byte numsamples;     /* number of samples saved */
			public byte numorders;
			public ushort numtracks;        /* number of tracks saved */
			public byte numchannels;        /* number of channels used  */
			public sbyte[] panpos = new sbyte[32];      /* voice pan positions */
			public byte songbpm;
			public byte songspd;
		}

		class AMFSAMPLE
		{
			public byte type;
			public string samplename;
			public string filename;
			public uint offset;
			public uint length;
			public ushort c2spd;
			public byte volume;
			public uint reppos;
			public uint repend;
		}

		class AMFNOTE
		{
			public byte note, instr, volume, fxcnt;
			public byte[] effect = new byte[3];
			public sbyte[] parameter = new sbyte[3];

			public void Copy(AMFNOTE amfnote)
			{
				note = amfnote.note;
				instr = amfnote.instr;
				volume = amfnote.volume;
				fxcnt = amfnote.fxcnt;

				for (var i = 0; i < 3; i++)
				{
					effect[i] = amfnote.effect[i];
					parameter[i] = amfnote.parameter[i];
				}
			}

			public void Clear()
			{
				note = instr = volume = fxcnt = 0;

				for (var i = 0; i < 3; i++)
				{
					effect[i] = 0;
					parameter[i] = 0;
				}
			}
		}

		/*========== Loader variables */

		AMFHEADER mh;
		const int AMFTEXTLEN = 22;
		const string AMF_Version = "DSMI Module Format 0.0";
		static AMFNOTE[] track;

		public AMFLoader()
		{
			m_ModuleType = "AMF";
			m_ModuleVersion = "AMF (DSMI Advanced Module Format)";
		}

		public override bool Test()
		{
			string id;
			byte ver;

			id = m_Reader.Read_String(3);

			if (id == "AMF")
			{
				ver = m_Reader.Read_byte();

				if (ver is >= 10 and <= 14)
				{
					return true;
				}
			}

			return false;
		}

		public override bool Init()
		{
			mh = new AMFHEADER();
			track = new AMFNOTE[64];

			return true;
		}

		bool AMF_UnpackTrack()
		{
			uint tracksize;
			byte row, cmd;
			sbyte arg;

			/* empty track */
			for (var i = 0; i < 64; i++)
			{
				track[i].Clear();
			}

			/* read packed track */
			if (m_Reader != null)
			{
				tracksize = m_Reader.Read_Intel_ushort();
				;
				tracksize += ((uint)m_Reader.Read_byte()) << 16;
				if (tracksize != 0)
				{
					while (tracksize-- != 0)
					{
						row = m_Reader.Read_byte();
						cmd = m_Reader.Read_byte();
						arg = m_Reader.Read_sbyte();
						/* unexpected end of track */
						if (tracksize == 0)
						{
							if ((row == 0xff) && (cmd == 0xff) && (arg == -1))
							{
								break;
							}
							/* the last triplet should be FF FF FF, but this is not
   always the case... maybe a bug in m2amf ? 
else
	return false;
*/

						}
						/* invalid row (probably unexpected end of row) */
						if (row >= 64)
						{
							return false;
						}

						if (cmd < 0x7f)
						{
							/* note, vol */
							track[row].note = cmd;
							track[row].volume = (byte)(arg + 1);
						}
						else
						  if (cmd == 0x7f)
						{
							/* duplicate row */
							if ((arg < 0) && (row + arg >= 0))
							{
								track[row].Copy(track[row + arg]);
								//memcpy(track+row,track+(row+arg),sizeof(AMFNOTE));
							}
						}
						else
						  if (cmd == 0x80)
						{
							/* instr */
							track[row].instr = (byte)(arg + 1);
						}
						else
						  if (cmd == 0x83)
						{
							/* volume without note */
							track[row].volume = (byte)(arg + 1);
						}
						else
						  if (cmd == 0xff)
						{
							/* apparently, some M2AMF version fail to estimate the
							   size of the compressed patterns correctly, and end
							   up with blanks, i.e. dead triplets. Those are marked
							   with cmd == 0xff. Let's ignore them. */
						}
						else
						  if (track[row].fxcnt < 3)
						{
							/* effect, param */
							if (cmd > 0x97)
							{
								return false;
							}

							track[row].effect[track[row].fxcnt] = (byte)(cmd & 0x7f);
							track[row].parameter[track[row].fxcnt] = arg;
							track[row].fxcnt++;
						}
						else
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		byte[] AMF_ConvertTrack()
		{
			int row, fx4memory = 0;

			/* convert track */
			UniReset();
			for (row = 0; row < 64; row++)
			{
				if (track[row].instr != 0)
				{
					UniInstrument(track[row].instr - 1);
				}

				if (track[row].note > Constants.Octave)
				{
					UniNote(track[row].note - Constants.Octave);
				}

				/* AMF effects */
				while (track[row].fxcnt-- != 0)
				{
					var inf = track[row].parameter[track[row].fxcnt];

					switch (track[row].effect[track[row].fxcnt])
					{
						case 1: /* Set speed */
							UniEffect(Commands.UNI_S3MEFFECTA, inf);
							break;
						case 2: /* Volume slide */
							if (inf != 0)
							{
								UniWriteByte(Commands.UNI_S3MEFFECTD);
								if (inf >= 0)
								{
									UniWriteByte((inf & 0xf) << 4);
								}
								else
								{
									UniWriteByte((-inf) & 0xf);
								}
							}

							break;
						/* effect 3, set channel volume, done in UnpackTrack */
						case 4: /* Porta up/down */
							if (inf != 0)
							{
								if (inf > 0)
								{
									UniEffect(Commands.UNI_S3MEFFECTE, inf);
									fx4memory = (int)Commands.UNI_S3MEFFECTE;
								}
								else
								{
									UniEffect(Commands.UNI_S3MEFFECTF, -inf);
									fx4memory = (int)Commands.UNI_S3MEFFECTF;
								}
							}
							else if (fx4memory != 0)
							{
								UniEffect(fx4memory, 0);
							}

							break;
						/* effect 5, "Porta abs", not supported */
						case 6: /* Porta to note */
							UniEffect(Commands.UNI_ITEFFECTG, inf);
							break;
						case 7: /* Tremor */
							UniEffect(Commands.UNI_S3MEFFECTI, inf);
							break;
						case 8: /* Arpeggio */
							UniPTEffect(0x0, inf);
							break;
						case 9: /* Vibrato */
							UniPTEffect(0x4, inf);
							break;
						case 0xa: /* Porta + Volume slide */
							UniPTEffect(0x3, 0);
							if (inf != 0)
							{
								UniWriteByte(Commands.UNI_S3MEFFECTD);
								if (inf >= 0)
								{
									UniWriteByte((inf & 0xf) << 4);
								}
								else
								{
									UniWriteByte((-inf) & 0xf);
								}
							}

							break;
						case 0xb: /* Vibrato + Volume slide */
							UniPTEffect(0x4, 0);
							if (inf != 0)
							{
								UniWriteByte(Commands.UNI_S3MEFFECTD);
								if (inf >= 0)
								{
									UniWriteByte((inf & 0xf) << 4);
								}
								else
								{
									UniWriteByte((-inf) & 0xf);
								}
							}

							break;
						case 0xc: /* Pattern break (in hex) */
							UniPTEffect(0xd, inf);
							break;
						case 0xd: /* Pattern jump */
							UniPTEffect(0xb, inf);
							break;
						/* effect 0xe, "Sync", not supported */
						case 0xf: /* Retrig */
							UniEffect(Commands.UNI_S3MEFFECTQ, inf & 0xf);
							break;
						case 0x10: /* Sample offset */
							UniPTEffect(0x9, inf);
							break;
						case 0x11: /* Fine volume slide */
							if (inf != 0)
							{
								UniWriteByte(Commands.UNI_S3MEFFECTD);
								if (inf >= 0)
								{
									UniWriteByte(((inf & 0xf) << 4) | 0xf);
								}
								else
								{
									UniWriteByte(0xf0 | ((-inf) & 0xf));
								}
							}

							break;
						case 0x12: /* Fine portamento */
							if (inf != 0)
							{
								if (inf > 0)
								{
									UniEffect(Commands.UNI_S3MEFFECTE, 0xf0 | (inf & 0xf));
									fx4memory = (int)Commands.UNI_S3MEFFECTE;
								}
								else
								{
									UniEffect(Commands.UNI_S3MEFFECTF, 0xf0 | ((-inf) & 0xf));
									fx4memory = (int)Commands.UNI_S3MEFFECTF;
								}
							}
							else if (fx4memory != 0)
							{
								UniEffect(fx4memory, 0);
							}

							break;
						case 0x13: /* Delay note */
							UniPTEffect(0xe, 0xd0 | (inf & 0xf));
							break;
						case 0x14: /* Note cut */
							UniPTEffect(0xc, 0);
							track[row].volume = 0;
							break;
						case 0x15: /* Set tempo */
							UniEffect(Commands.UNI_S3MEFFECTT, inf);
							break;
						case 0x16: /* Extra fine portamento */
							if (inf != 0)
							{
								if (inf > 0)
								{
									UniEffect(Commands.UNI_S3MEFFECTE, 0xe0 | ((inf >> 2) & 0xf));
									fx4memory = (int)Commands.UNI_S3MEFFECTE;
								}
								else
								{
									UniEffect(Commands.UNI_S3MEFFECTF, 0xe0 | (((-inf) >> 2) & 0xf));
									fx4memory = (int)Commands.UNI_S3MEFFECTF;
								}
							}
							else if (fx4memory != 0)
							{
								UniEffect(fx4memory, 0);
							}

							break;
						case 0x17: /* Panning */
							if (inf > 64)
							{
								UniEffect(Commands.UNI_ITEFFECTS0, 0x91); /* surround */
							}
							else
							{
								UniPTEffect(0x8, (inf == 64) ? 255 : (inf + 64) << 1);
							}

							m_Module.Flags |= Constants.UF_PANNING;
							break;
					}

				}

				if (track[row].volume != 0)
				{
					UniVolEffect(ITColumnEffect.VOL_VOLUME, track[row].volume - 1);
				}

				UniNewline();
			}

			return UniDup();
		}

		public override bool Load(int curious)
		{
			int t, u, realtrackcnt, realsmpcnt, defaultpanning;
			AMFSAMPLE s;
			Sample q;
			ushort[] track_remap;
			uint samplepos;
			uint fileend;

			var channel_remap = new sbyte[16];

			/* try to read module header  */
			mh.id = m_Reader.Read_String(3);
			mh.version = m_Reader.Read_byte();
			mh.SongName = m_Reader.Read_String(32);
			mh.numsamples = m_Reader.Read_byte();
			mh.numorders = m_Reader.Read_byte();
			mh.numtracks = m_Reader.Read_Intel_ushort();
			mh.numchannels = m_Reader.Read_byte();
			if ((mh.numchannels == 0) || (mh.numchannels > (mh.version >= 12 ? 32 : 16)))
			{
				m_LoadError = MMERR_NOT_A_MODULE;
				return false;
			}

			if (mh.version >= 11)
			{
				Array.Clear(mh.panpos, 0, 32);
				m_Reader.Read_bytes(mh.panpos, (mh.version >= 13) ? 32 : 16);
			}
			else
			{
				m_Reader.Read_bytes(channel_remap, 16);
			}

			if (mh.version >= 13)
			{
				mh.songbpm = m_Reader.Read_byte();
				if (mh.songbpm < 32)
				{
					m_LoadError = MMERR_NOT_A_MODULE;
					return false;
				}

				mh.songspd = m_Reader.Read_byte();
				if (mh.songspd > 32)
				{
					m_LoadError = MMERR_NOT_A_MODULE;
					return false;
				}
			}
			else
			{
				mh.songbpm = 125;
				mh.songspd = 6;
			}

			if (m_Reader.isEOF())
			{
				m_LoadError = MMERR_LOADING_HEADER;
				return false;
			}

			/* set module variables */
			m_Module.InitialSpeed = mh.songspd;
			m_Module.InitialTempo = mh.songbpm;

			var version = AMF_Version.ToCharArray();

			version[AMFTEXTLEN - 3] = (char)('0' + (mh.version / 10));
			version[AMFTEXTLEN - 1] = (char)('0' + (mh.version % 10));

			m_Module.ModType = new string(version);
			m_Module.NumChannels = mh.numchannels;
			m_Module.NumTracks = (ushort)(mh.numorders * mh.numchannels);
			if (mh.numtracks > m_Module.NumTracks)
			{
				m_Module.NumTracks = mh.numtracks;
			}

			m_Module.NumTracks++;  /* add room for extra, empty track */
			m_Module.SongName = mh.SongName;
			m_Module.NumPositions = mh.numorders;
			m_Module.NumPatterns = mh.numorders;
			m_Module.RestartPosition = 0;
			m_Module.Flags |= Constants.UF_S3MSLIDES;
			/* XXX whenever possible, we should try to determine the original format.
			   Here we assume it was S3M-style wrt bpmlimit... */
			m_Module.BpmLimit = 32;

			/*
			 * Play with the panning table. Although the AMF format embeds a
			 * panning table, if the module was a MOD or an S3M with default
			 * panning and didn't use any panning commands, don't flag
			 * UF_PANNING, to use our preferred panning table for this case.
			 */
			defaultpanning = 1;
			for (t = 0; t < 32; t++)
			{
				if (mh.panpos[t] > 64)
				{
					m_Module.Panning[t] = Constants.PAN_SURROUND;
					defaultpanning = 0;
				}
				else
					if (mh.panpos[t] == 64)
				{
					m_Module.Panning[t] = Constants.PAN_RIGHT;
				}
				else
				{
					m_Module.Panning[t] = (ushort)((mh.panpos[t] + 64) << 1);
				}
			}

			if (defaultpanning != 0)
			{
				for (t = 0; t < m_Module.NumChannels; t++)
				{
					if (m_Module.Panning[t] == (((t + 1) & 2) != 0 ? Constants.PAN_RIGHT : Constants.PAN_LEFT))
					{
						defaultpanning = 0; /* not MOD canonical panning */
						break;
					}
				}
			}

			if (defaultpanning != 0)
			{
				m_Module.Flags |= Constants.UF_PANNING;
			}

			m_Module.NumInstruments = m_Module.NumSamples = mh.numsamples;

			m_Module.Positions = new ushort[m_Module.NumPositions];
			for (t = 0; t < m_Module.NumPositions; t++)
			{
				m_Module.Positions[t] = (ushort)t;
			}

			m_Module.AllocTracks();
			m_Module.AllocPatterns();

			/* read AMF order table */
			for (t = 0; t < m_Module.NumPatterns; t++)
			{
				if (mh.version >= 14)
				{
					/* track size */
					m_Module.PatternRows[t] = m_Reader.Read_Intel_ushort();
				}

				if (mh.version >= 10)
				{
					m_Reader.Read_Intel_ushorts(m_Module.Patterns, t * m_Module.NumChannels, m_Module.NumChannels);
				}
				else
				{
					for (u = 0; u < m_Module.NumChannels; u++)
					{
						m_Module.Patterns[(t * m_Module.NumChannels) + channel_remap[u]] = m_Reader.Read_Intel_ushort();
					}
				}
			}

			if (m_Reader.isEOF())
			{
				m_LoadError = MMERR_LOADING_HEADER;
				return false;
			}

			/* read sample information */
			m_Module.AllocSamples();

			for (t = 0; t < m_Module.NumInstruments; t++)
			{
				q = m_Module.Samples[t];
				s = new AMFSAMPLE
				{
					/* try to read sample info */
					type = m_Reader.Read_byte(),
					samplename = m_Reader.Read_String(32),
					filename = m_Reader.Read_String(13),
					offset = m_Reader.Read_Intel_uint(),
					length = m_Reader.Read_Intel_uint(),
					c2spd = m_Reader.Read_Intel_ushort()
				};
				if (s.c2spd == 8368)
				{
					s.c2spd = 8363;
				}

				s.volume = m_Reader.Read_byte();
				/* "the tribal zone.amf" and "the way its gonna b.amf" by Maelcum
                 * are the only version 10 files I can find, and they have 32 bit
                 * reppos and repend, not 16. */
				if (mh.version >= 10)
				{ // Was 11
					s.reppos = m_Reader.Read_Intel_uint();
					s.repend = m_Reader.Read_Intel_uint();
				}
				else
				{
					s.reppos = m_Reader.Read_Intel_ushort();
					s.repend = s.length;
				}

				if (m_Reader.isEOF())
				{
					m_LoadError = MMERR_LOADING_SAMPLEINFO;
					return false;
				}

				q.samplename = s.samplename;
				q.speed = s.c2spd;
				q.volume = s.volume;
				if (s.type != 0)
				{
					q.seekpos = s.offset;
					q.length = s.length;
					q.loopstart = s.reppos;
					q.loopend = s.repend;
					if ((s.repend - s.reppos) > 2)
					{
						q.flags |= Constants.SF_LOOP;
					}
				}
			}

			/* read track table */
			track_remap = new ushort[mh.numtracks + 1];
			m_Reader.Read_Intel_ushorts(track_remap, 1, mh.numtracks);
			if (m_Reader.isEOF())
			{
				track_remap = null;
				m_LoadError = MMERR_LOADING_TRACK;
				return false;
			}

			for (realtrackcnt = t = 0; t <= mh.numtracks; t++)
			{
				if (realtrackcnt < track_remap[t])
				{
					realtrackcnt = track_remap[t];
				}
			}

			for (t = 0; t < m_Module.NumPatterns * m_Module.NumChannels; t++)
			{
				m_Module.Patterns[t] = (ushort)((m_Module.Patterns[t] <= mh.numtracks) ? track_remap[m_Module.Patterns[t]] - 1 : realtrackcnt);
			}

			track_remap = null;

			/* unpack tracks */
			for (t = 0; t < realtrackcnt; t++)
			{
				if (m_Reader.isEOF())
				{
					m_LoadError = MMERR_LOADING_TRACK;
					return false;
				}

				if (!AMF_UnpackTrack())
				{
					m_LoadError = MMERR_LOADING_TRACK;
					return false;
				}

				if ((m_Module.Tracks[t] = AMF_ConvertTrack()) == null)
				{
					return false;
				}
			}
			/* add an extra void track */
			UniReset();
			for (t = 0; t < 64; t++)
			{
				UniNewline();
			}

			m_Module.Tracks[realtrackcnt++] = UniDup();
			for (t = realtrackcnt; t < m_Module.NumTracks; t++)
			{
				m_Module.Tracks[t] = null;
			}

			/* compute sample offsets */
			samplepos = (uint)m_Reader.Tell();

			if (m_Reader.isEOF())
			{
				m_LoadError = MMERR_LOADING_SAMPLEINFO;
				return false;
			}

			m_Reader.Seek(0, SeekOrigin.End);
			fileend = (uint)m_Reader.Tell();

			m_Reader.Seek((int)samplepos, SeekOrigin.Begin);

			for (realsmpcnt = t = 0; t < m_Module.NumSamples; t++)
			{
				if (realsmpcnt < m_Module.Samples[t].seekpos)
				{
					realsmpcnt = (int)m_Module.Samples[t].seekpos;
				}
			}

			for (t = 1; t <= realsmpcnt; t++)
			{
				var place = t;
				u = 0;

				do
				{
					if (++u == m_Module.NumSamples)
					{
						m_LoadError = MMERR_LOADING_SAMPLEINFO;
						return false;
					}

					q = m_Module.Samples[place];
					place++;
				} while (q.seekpos != t);

				q.seekpos = samplepos;
				samplepos += q.length;
			}

			if (samplepos > fileend)
			{
				m_LoadError = MMERR_LOADING_SAMPLEINFO;
				return false;
			}

			return true;
		}

		public override void Cleanup()
		{
			track = null;
			mh = null;
		}

		public override string LoadTitle() => throw new NotImplementedException();
	}
}
