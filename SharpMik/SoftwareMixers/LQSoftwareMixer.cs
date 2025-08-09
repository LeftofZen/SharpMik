using SharpMik.Common;
using SharpMik.Player;
using System;
using System.Diagnostics;

namespace SharpMik.SoftwareMixers
{
	public class LQSoftwareMixer : CommonSoftwareMixer
	{
		const int FRACBITS = 11;
		const int FRACMASK = (1 << FRACBITS) - 1;

		const int BITSHIFT = 9;
		const int CLICK_SHIFT = 6;
		const int CLICK_BUFFER = 1 << CLICK_SHIFT;
		int nLeftNR;
		int nRightNR;

		protected override bool MixerInit()
		{
			m_FracBits = FRACBITS;
			m_ClickBuffer = CLICK_BUFFER;
			m_ReverbMultipler = 1;
			return false;
		}

		protected override uint WriteSamples(sbyte[] buf, uint todo)
		{
			int left, count;
			int t, pan, vol;
			uint bufPlace = 0;
			if (todo > buf.Length)
			{
				throw new Exception("Asked for more then the dest buffer.");
			}

			while (todo != 0)
			{
				if (m_TickLeft == 0)
				{
					if ((m_VcMode & Constants.DMODE_SOFT_MUSIC) == Constants.DMODE_SOFT_MUSIC)
					{
						ModPlayer.Player_HandleTick();
					}

					m_TickLeft = ModDriver.MixFrequency * 125 / (ModDriver.Bpm * 50);
				}

				left = (int)Math.Min(m_TickLeft, todo);

				var bufferPlace = bufPlace;
				m_TickLeft -= left;
				todo -= (uint)left;
				bufPlace += samples2bytes((uint)left);

				while (left != 0)
				{
					var portion = (int)Math.Min(left, m_SamplesThatFit);
					count = m_IsStereo ? (portion << 1) : portion;

					Array.Clear(m_VcTickBuf, 0, TICKLSIZE);

					for (t = 0; t < m_VcSoftChannel; t++)
					{
						m_CurrentVoiceInfo = m_VoiceInfos[t];

						if (m_CurrentVoiceInfo.Kick != 0)
						{
							m_CurrentVoiceInfo.CurrentSampleIndex = ((long)m_CurrentVoiceInfo.Start) << FRACBITS;
							m_CurrentVoiceInfo.Kick = 0;
							m_CurrentVoiceInfo.Active = 1;
						}

						if (m_CurrentVoiceInfo.Frequency == 0)
						{
							m_CurrentVoiceInfo.Active = 0;
						}

						if (m_CurrentVoiceInfo.Active != 0)
						{
							m_CurrentVoiceInfo.CurrentIncrement = ((long)(m_CurrentVoiceInfo.Frequency << FRACBITS)) / ModDriver.MixFrequency;

							if ((m_CurrentVoiceInfo.Flags & Constants.SF_REVERSE) != 0)
							{
								m_CurrentVoiceInfo.CurrentIncrement = -m_CurrentVoiceInfo.CurrentIncrement;
							}

							vol = m_CurrentVoiceInfo.Volume;
							pan = m_CurrentVoiceInfo.Panning;

							m_CurrentVoiceInfo.LeftVolumeOld = m_CurrentVoiceInfo.LeftVolumeFactor;
							m_CurrentVoiceInfo.RightVolumeOld = m_CurrentVoiceInfo.RightVolumeFactor;

							if (m_IsStereo)
							{
								if (pan != Constants.PAN_SURROUND)
								{
									m_CurrentVoiceInfo.LeftVolumeFactor = (vol * (Constants.PAN_RIGHT - pan)) >> 8;
									m_CurrentVoiceInfo.RightVolumeFactor = (vol * pan) >> 8;
								}
								else
								{
									m_CurrentVoiceInfo.LeftVolumeFactor = m_CurrentVoiceInfo.RightVolumeFactor = vol / 2;
								}
							}
							else
							{
								m_CurrentVoiceInfo.LeftVolumeFactor = vol;
							}

							m_IdxSize = (m_CurrentVoiceInfo.Size != 0) ? ((long)m_CurrentVoiceInfo.Size << FRACBITS) - 1 : 0;
							m_IdxlEnd = (m_CurrentVoiceInfo.RepeatEndPosition != 0) ? ((long)m_CurrentVoiceInfo.RepeatEndPosition << FRACBITS) - 1 : 0;

							m_IdxlPos = (long)m_CurrentVoiceInfo.RepeatStartPosition << FRACBITS;

							if (MikDebugger.s_TestModeOn && t == MikDebugger.s_TestChannel)
							{
								Debug.WriteLine("here");
							}

							AddChannel(m_VcTickBuf, portion);

							if (MikDebugger.s_TestModeOn)
							{
								Debug.WriteLine("{0}\t{1}", t, m_VcTickBuf[MikDebugger.s_TestPlace]);
							}
						}
					}

					if ((ModDriver.Mode & Constants.DMODE_NOISEREDUCTION) == Constants.DMODE_NOISEREDUCTION)
					{
						if (m_IsStereo)
						{
							MixLowPass_Stereo(portion);
						}
						else
						{
							MixLowPass_Normal(portion);
						}
					}

					if (ModDriver.Reverb != 0)
					{
						if (m_IsStereo)
						{
							MixReverb_Stereo(portion);
						}
						else
						{
							MixReverb_Mono(portion);
						}
					}

					FireCallBack(portion);

					if ((m_VcMode & Constants.DMODE_16BITS) == Constants.DMODE_16BITS)
					{
						Mix32To16(buf, m_VcTickBuf, count, (int)bufferPlace);
					}
					else
					{
						Mix32To8(buf, m_VcTickBuf, count, (int)bufferPlace);
					}

					bufferPlace += samples2bytes((uint)portion);

					if (bufferPlace > buf.Length)
					{
						return todo;
					}
					else
					{
						left -= portion;
					}
				}
			}

			return todo;
		}

		static int ExtractSample(int[] srce, int size, ref int place)
		{
			var var = srce[place++] >> (BITSHIFT + 16 - size);

			return var;
		}

		static void CheckSample(ref int var, int bound) => var = (var >= bound) ? bound - 1 : (var < -bound) ? -bound : var;

		static void PutSample(sbyte[] deste, ref int destePlace, int var) => deste[destePlace++] = (sbyte)var;

		static void Mix32To16(sbyte[] dste, int[] srce, int count, int dstePlace)
		{
			unchecked
			{
				int x1;
				var srcePlace = 0;

				while (count-- != 0)
				{
					x1 = srce[srcePlace++] >> BITSHIFT;
					x1 = (x1 > short.MaxValue) ? short.MaxValue : (x1 < short.MinValue) ? short.MinValue : x1;

					if (BitConverter.IsLittleEndian)
					{
						dste[dstePlace++] = (sbyte)x1;
						dste[dstePlace++] = (sbyte)(x1 >> 8);
					}
					else
					{
						dste[dstePlace++] = (sbyte)((x1 >> 8) & 0xFF);
						dste[dstePlace++] = (sbyte)(x1 & 0xFF);
					}
				}
			}
		}

		void Mix32To8(sbyte[] dste, int[] srce, int count, int dstePlace)
		{
			int x1, x2, x3, x4;
			int remain;

			var srcePlace = 0;

			remain = count & 3;
			for (count >>= 2; count != 0; count--)
			{
				x1 = ExtractSample(srce, 8, ref srcePlace);
				x2 = ExtractSample(srce, 8, ref srcePlace);
				x3 = ExtractSample(srce, 8, ref srcePlace);
				x4 = ExtractSample(srce, 8, ref srcePlace);

				CheckSample(ref x1, 128);
				CheckSample(ref x2, 128);
				CheckSample(ref x3, 128);
				CheckSample(ref x4, 128);

				PutSample(dste, ref dstePlace, x1 + 128);
				PutSample(dste, ref dstePlace, x2 + 128);
				PutSample(dste, ref dstePlace, x3 + 128);
				PutSample(dste, ref dstePlace, x4 + 128);
			}

			while (remain-- != 0)
			{
				x1 = ExtractSample(srce, 8, ref srcePlace);
				CheckSample(ref x1, 128);
				PutSample(dste, ref dstePlace, x1 + 128);
			}
		}

		void MixReverb_Mono(int count)
		{
			uint speedup;
			int ReverbPct;
			var loc = new uint[8];

			ReverbPct = 92 + (ModDriver.Reverb << 1);

			for (var i = 0; i < 8; i++)
			{
				loc[i] = (uint)(m_RvrIndex % m_Rvc[i]);
			}

			var place = 0;
			while (count-- != 0)
			{
				/* Compute the left channel echo buffers */
				_ = (uint)(m_VcTickBuf[place] >> 3);

				var side = 0;
				speedup = (uint)(m_VcTickBuf[place + side] >> 3);
				for (var channel = 0; channel < 8; channel++)
				{
					m_RvBuf[side][channel][loc[channel]] = (int)(speedup + ((ReverbPct * m_RvBuf[side][channel][loc[channel]]) >> 7));
				}

				/* Prepare to compute actual finalized data */
				m_RvrIndex++;

				for (var i = 0; i < 8; i++)
				{
					loc[i] = (uint)(m_RvrIndex % m_Rvc[i]);
				}

				var value = m_RvBuf[side][0][loc[0]] - m_RvBuf[side][1][loc[1]];
				value += m_RvBuf[side][2][loc[2]] - m_RvBuf[side][3][loc[3]];
				value += m_RvBuf[side][4][loc[4]] - m_RvBuf[side][5][loc[5]];
				value += m_RvBuf[side][6][loc[6]] - m_RvBuf[side][7][loc[7]];

				m_VcTickBuf[place++] += value;
			}
		}

		void MixReverb_Stereo(int count)
		{
			uint speedup;
			int ReverbPct;
			var loc = new uint[8];

			ReverbPct = 92 + (ModDriver.Reverb << 1);

			for (var i = 0; i < 8; i++)
			{
				loc[i] = (uint)(m_RvrIndex % m_Rvc[i]);
			}

			var place = 0;
			while (count-- != 0)
			{
				/* Compute the left channel echo buffers */
				_ = (uint)(m_VcTickBuf[place] >> 3);

				for (var side = 0; side < 2; side++)
				{
					speedup = (uint)(m_VcTickBuf[place + side] >> 3);
					for (var channel = 0; channel < 8; channel++)
					{
						m_RvBuf[side][channel][loc[channel]] = (int)(speedup + ((ReverbPct * m_RvBuf[side][channel][loc[channel]]) >> 7));
					}
				}

				/* Prepare to compute actual finalized data */
				m_RvrIndex++;

				for (var i = 0; i < 8; i++)
				{
					loc[i] = (uint)(m_RvrIndex % m_Rvc[i]);
				}

				for (var side = 0; side < 2; side++)
				{
					var value = m_RvBuf[side][0][loc[0]] - m_RvBuf[side][1][loc[1]];
					value += m_RvBuf[side][2][loc[2]] - m_RvBuf[side][3][loc[3]];
					value += m_RvBuf[side][4][loc[4]] - m_RvBuf[side][5][loc[5]];
					value += m_RvBuf[side][6][loc[6]] - m_RvBuf[side][7][loc[7]];

					m_VcTickBuf[place++] += value;
				}
			}
		}

		void MixLowPass_Stereo(int count)
		{
			int n1 = nLeftNR, n2 = nRightNR;
			var nr = count;
			var place = 0;
			for (; nr != 0; nr--)
			{
				var vnr = m_VcTickBuf[place] >> 1;
				m_VcTickBuf[place] = vnr + n1;
				n1 = vnr;

				vnr = m_VcTickBuf[place + 1] >> 1;
				m_VcTickBuf[place + 1] = vnr + n2;
				n2 = vnr;
				place += 2;
			}

			nLeftNR = n1;
			nRightNR = n2;
		}

		void MixLowPass_Normal(int count)
		{
			var n1 = nLeftNR;
			var nr = count;
			var place = 0;
			for (; nr != 0; nr--)
			{
				var vnr = m_VcTickBuf[place] >> 1;
				m_VcTickBuf[place] = vnr + n1;
				n1 = vnr;
				place++;
			}

			nLeftNR = n1;
		}

		void AddChannel(int[] buff, int todo)
		{
			long end, done;

			var place = 0;

			var s = m_Samples[m_CurrentVoiceInfo.Handle];

			if (s == null)
			{
				m_CurrentVoiceInfo.CurrentSampleIndex = m_CurrentVoiceInfo.Active = 0;
				return;
			}

			while (todo > 0)
			{
				long endpos;

				if ((m_CurrentVoiceInfo.Flags & Constants.SF_REVERSE) == Constants.SF_REVERSE)
				{
					/* The sample is playing in reverse */
					if ((m_CurrentVoiceInfo.Flags & Constants.SF_LOOP) == Constants.SF_LOOP && (m_CurrentVoiceInfo.CurrentSampleIndex < m_IdxlPos))
					{
						/* the sample is looping and has reached the loopstart index */
						if ((m_CurrentVoiceInfo.Flags & Constants.SF_BIDI) == Constants.SF_BIDI)
						{
							/* sample is doing bidirectional loops, so 'bounce' the
							   current index against the idxlpos */
							m_CurrentVoiceInfo.CurrentSampleIndex = m_IdxlPos + (m_IdxlPos - m_CurrentVoiceInfo.CurrentSampleIndex);
							int value = m_CurrentVoiceInfo.Flags;
							value &= ~Constants.SF_REVERSE;
							m_CurrentVoiceInfo.Flags = (ushort)value;
							m_CurrentVoiceInfo.CurrentIncrement = -m_CurrentVoiceInfo.CurrentIncrement;
						}
						else
						{
							/* normal backwards looping, so set the current position to
							   loopend index */
							m_CurrentVoiceInfo.CurrentSampleIndex = m_IdxlEnd - (m_IdxlPos - m_CurrentVoiceInfo.CurrentSampleIndex);
						}
					}
					else
					{
						/* the sample is not looping, so check if it reached index 0 */
						if (m_CurrentVoiceInfo.CurrentSampleIndex < 0)
						{
							/* playing index reached 0, so stop playing this sample */
							m_CurrentVoiceInfo.CurrentSampleIndex = m_CurrentVoiceInfo.Active = 0;
							break;
						}
					}
				}
				else
				{
					/* The sample is playing forward */
					if ((m_CurrentVoiceInfo.Flags & Constants.SF_LOOP) == Constants.SF_LOOP && (m_CurrentVoiceInfo.CurrentSampleIndex >= m_IdxlEnd))
					{
						/* the sample is looping, check the loopend index */
						if ((m_CurrentVoiceInfo.Flags & Constants.SF_BIDI) == Constants.SF_BIDI)
						{
							/* sample is doing bidirectional loops, so 'bounce' the
							   current index against the idxlend */
							m_CurrentVoiceInfo.Flags |= Constants.SF_REVERSE;
							m_CurrentVoiceInfo.CurrentIncrement = -m_CurrentVoiceInfo.CurrentIncrement;
							m_CurrentVoiceInfo.CurrentSampleIndex = m_IdxlEnd - (m_CurrentVoiceInfo.CurrentSampleIndex - m_IdxlEnd);
						}
						else
						{
							/* normal backwards looping, so set the current position
							   to loopend index */
							m_CurrentVoiceInfo.CurrentSampleIndex = m_IdxlPos + (m_CurrentVoiceInfo.CurrentSampleIndex - m_IdxlEnd);
						}
					}
					else
					{
						/* sample is not looping, so check if it reached the last
						   position */
						if (m_CurrentVoiceInfo.CurrentSampleIndex >= m_IdxSize)
						{
							/* yes, so stop playing this sample */
							m_CurrentVoiceInfo.CurrentSampleIndex = m_CurrentVoiceInfo.Active = 0;
							break;
						}
					}
				}

				end = (m_CurrentVoiceInfo.Flags & Constants.SF_REVERSE) == Constants.SF_REVERSE ? (m_CurrentVoiceInfo.Flags & Constants.SF_LOOP) == Constants.SF_LOOP ? m_IdxlPos : 0 : (m_CurrentVoiceInfo.Flags & Constants.SF_LOOP) == Constants.SF_LOOP ? m_IdxlEnd : m_IdxSize;

				/* if the sample is not blocked... */
				if ((end == m_CurrentVoiceInfo.CurrentSampleIndex) || (m_CurrentVoiceInfo.CurrentIncrement == 0))
				{
					done = 0;
				}
				else
				{
					done = Math.Min(((end - m_CurrentVoiceInfo.CurrentSampleIndex) / m_CurrentVoiceInfo.CurrentIncrement) + 1, todo);
					if (done < 0)
					{
						done = 0;
					}
				}

				if (done == 0)
				{
					m_CurrentVoiceInfo.Active = 0;
					break;
				}

				endpos = m_CurrentVoiceInfo.CurrentSampleIndex + (done * m_CurrentVoiceInfo.CurrentIncrement);

				if (m_CurrentVoiceInfo.Volume != 0)
				{
					/* use the 32 bit mixers as often as we can (they're much faster) */
					if ((m_CurrentVoiceInfo.CurrentSampleIndex < 0x7fffffff) && (endpos < 0x7fffffff))
					{
						if ((ModDriver.Mode & Constants.DMODE_INTERP) == Constants.DMODE_INTERP)
						{
							if (m_IsStereo)
							{
								if ((m_CurrentVoiceInfo.Panning == Constants.PAN_SURROUND) && (ModDriver.Mode & Constants.DMODE_SURROUND) == Constants.DMODE_SURROUND)
								{
									m_CurrentVoiceInfo.CurrentSampleIndex = Mix32SurroundInterp(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
								}
								else
								{
									m_CurrentVoiceInfo.CurrentSampleIndex = Mix32StereoInterp(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
								}
							}
							else
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = Mix32MonoInterp(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
							}
						}
						else if (m_IsStereo)
						{
							if ((m_CurrentVoiceInfo.Panning == Constants.PAN_SURROUND) && (ModDriver.Mode & Constants.DMODE_SURROUND) == Constants.DMODE_SURROUND)
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = Mix32SurroundNormal(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
							}
							else
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = Mix32StereoNormal(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
							}
						}
						else
						{
							m_CurrentVoiceInfo.CurrentSampleIndex = Mix32MonoNormal(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
						}
					}
					else
					{
						// do I need to implement the 64bit functions? I hope not!
						throw new NotImplementedException();
						/*
						if((md_mode & DMODE_INTERP)) {
							if(vc_mode & DMODE_STEREO) {
								if((vnf->pan==PAN_SURROUND)&&(md_mode&DMODE_SURROUND))
									vnf->current=MixSurroundInterp
											   (s,ptr,vnf->current,vnf->increment,done);
								else
									vnf->current=MixStereoInterp
											   (s,ptr,vnf->current,vnf->increment,done);
							} else
								vnf->current=MixMonoInterp
											   (s,ptr,vnf->current,vnf->increment,done);
						} else if(vc_mode & DMODE_STEREO) {
							if((vnf->pan==PAN_SURROUND)&&(md_mode&DMODE_SURROUND))
								vnf->current=MixSurroundNormal
											   (s,ptr,vnf->current,vnf->increment,done);
							else
								vnf->current=MixStereoNormal
											   (s,ptr,vnf->current,vnf->increment,done);
						} else
							vnf->current=MixMonoNormal
											   (s,ptr,vnf->current,vnf->increment,done);
						 */
					}
				}
				else
				{
					/* update sample position */
					m_CurrentVoiceInfo.CurrentSampleIndex = endpos;
				}

				todo -= (int)done;
				place += (int)(m_IsStereo ? (done << 1) : done);
			}

		}

		int Mix32MonoNormal(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			short sample;
			var lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;

			while (todo-- != 0)
			{
				sample = srce[index >> FRACBITS];
				index += increment;

				dest[place++] += lvolsel * sample;
			}

			return index;
		}

		int Mix32StereoNormal(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			unchecked
			{
				short sample;
				var lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
				var rvolsel = m_CurrentVoiceInfo.RightVolumeFactor;

				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS];
					index += increment;

					dest[place++] += lvolsel * sample;
					dest[place++] += rvolsel * sample;
				}

				return index;
			}
		}

		int Mix32SurroundNormal(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			short sample;
			var lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			var rvolsel = m_CurrentVoiceInfo.RightVolumeFactor;

			if (lvolsel >= rvolsel)
			{
				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS];
					index += increment;

					dest[place++] += lvolsel * sample;
					dest[place++] -= lvolsel * sample;
				}
			}
			else
			{
				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS];
					index += increment;

					dest[place++] -= rvolsel * sample;
					dest[place++] += rvolsel * sample;
				}
			}

			return index;
		}

		int Mix32MonoInterp(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			int sample;
			var lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			var rampvol = m_CurrentVoiceInfo.RampVolume;

			if (rampvol != 0)
			{
				var oldlvol = m_CurrentVoiceInfo.LeftVolumeOld - lvolsel;
				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS] +
						   (((srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
							* (index & FRACMASK)) >> FRACBITS);
					index += increment;

					dest[place++] += (((lvolsel << CLICK_SHIFT) + (oldlvol * rampvol))
							   * sample) >> CLICK_SHIFT;
					if (--rampvol == 0)
					{
						break;
					}
				}

				m_CurrentVoiceInfo.RampVolume = rampvol;

				if (todo < 0)
				{
					return index;
				}
			}

			while (todo-- != 0)
			{
				sample = srce[index >> FRACBITS] +
					   (((srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
						* (index & FRACMASK)) >> FRACBITS);
				index += increment;

				dest[place++] += lvolsel * sample;
			}

			return index;
		}

		int Mix32StereoInterp(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			int sample;
			var lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			var rvolsel = m_CurrentVoiceInfo.RightVolumeFactor;
			var rampvol = m_CurrentVoiceInfo.RampVolume;

			if (rampvol != 0)
			{
				var oldlvol = m_CurrentVoiceInfo.LeftVolumeOld - lvolsel;
				var oldrvol = m_CurrentVoiceInfo.RightVolumeOld - rvolsel;
				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS] +
						   (((srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
							* (index & FRACMASK)) >> FRACBITS);
					index += increment;

					dest[place++] += (((lvolsel << CLICK_SHIFT) + (oldlvol * rampvol))
							   * sample) >> CLICK_SHIFT;
					dest[place++] += (((rvolsel << CLICK_SHIFT) + (oldrvol * rampvol))
							   * sample) >> CLICK_SHIFT;
					if (--rampvol == 0)
					{
						break;
					}
				}

				m_CurrentVoiceInfo.RampVolume = rampvol;

				if (todo < 0)
				{
					return index;
				}
			}

			while (todo-- != 0)
			{
				sample = srce[index >> FRACBITS] +
					   (((srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
						* (index & FRACMASK)) >> FRACBITS);
				index += increment;

				dest[place++] += lvolsel * sample;
				dest[place++] += rvolsel * sample;
			}

			return index;
		}

		int Mix32SurroundInterp(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			int sample;
			var lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			var rvolsel = m_CurrentVoiceInfo.RightVolumeFactor;
			var rampvol = m_CurrentVoiceInfo.RampVolume;
			int oldvol, vol;

			if (lvolsel >= rvolsel)
			{
				vol = lvolsel;
				oldvol = m_CurrentVoiceInfo.LeftVolumeOld;
			}
			else
			{
				vol = rvolsel;
				oldvol = m_CurrentVoiceInfo.RightVolumeOld;
			}

			if (rampvol != 0)
			{
				oldvol -= vol;
				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS] +
						   (((srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
							* (index & FRACMASK)) >> FRACBITS);
					index += increment;

					sample = (((vol << CLICK_SHIFT) + (oldvol * rampvol))
						   * sample) >> CLICK_SHIFT;
					dest[place++] += sample;
					dest[place++] -= sample;

					if (--rampvol == 0)
					{
						break;
					}
				}

				m_CurrentVoiceInfo.RampVolume = rampvol;
				if (todo < 0)
				{
					return index;
				}
			}

			while (todo-- != 0)
			{
				sample = srce[index >> FRACBITS] +
					   (((srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
						* (index & FRACMASK)) >> FRACBITS);
				index += increment;

				dest[place++] += vol * sample;
				dest[place++] -= vol * sample;
			}

			return index;
		}
	}
}
