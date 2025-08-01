using System;
using SharpMik.Common;
using SharpMik.Player;

namespace SharpMik.SoftwareMixers
{
	public class HQSoftwareMixer : CommonSoftwareMixer
	{
		const int MAXVOL_FACTOR = 1 << 9;

		const int SAMPLING_SHIFT = 2;
		static readonly uint SAMPLING_FACTOR = 1 << SAMPLING_SHIFT;

		const int FRACBITS = 28;
		static readonly int FRACMASK = (1 << FRACBITS) - 1;

		const int TICKWSIZE = TICKLSIZE * 2;

		const int CLICK_SHIFT_BASE = 6;
		static readonly int CLICK_SHIFT = CLICK_SHIFT_BASE + SAMPLING_SHIFT;
		static readonly int CLICK_BUFFER = 1 << CLICK_SHIFT;

		protected override bool MixerInit()
		{
			m_FracBits = FRACBITS;
			m_ClickBuffer = CLICK_BUFFER;
			m_ReverbMultipler = 10;

			return false;
		}

		int Mix32MonoNormal(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			short sample = 0;
			int i, f;

			while (todo-- != 0)
			{
				i = index >> FRACBITS;
				f = index & FRACMASK;

				sample = (short)(((int)(srce[i] * (FRACMASK + 1L - f)) + (srce[i + 1] * f)) >> FRACBITS);
				index += increment;

				if (m_CurrentVoiceInfo.RampVolume != 0)
				{
					dest[place++] +=
					  ((m_CurrentVoiceInfo.LeftVolumeOld * m_CurrentVoiceInfo.RampVolume +
						  (m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.RampVolume))) *
						sample) >> CLICK_SHIFT;
					m_CurrentVoiceInfo.RampVolume--;
				}
				else
				{
					if (m_CurrentVoiceInfo.Click != 0)
					{
						dest[place++] +=
						  ((m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.Click) *
							  sample) + (m_CurrentVoiceInfo.LastValueLeft * m_CurrentVoiceInfo.Click)) >> CLICK_SHIFT;
						m_CurrentVoiceInfo.Click--;
					}
					else
					{
						dest[place++] += m_CurrentVoiceInfo.LeftVolumeFactor * sample;
					}
				}
			}

			m_CurrentVoiceInfo.LastValueLeft = m_CurrentVoiceInfo.LeftVolumeFactor * sample;

			return index;
		}

		int Mix32StereoNormal(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			short sample = 0;
			int i, f;

			while (todo-- != 0)
			{
				i = index >> FRACBITS;
				f = index & FRACMASK;

				sample = (short)(((srce[i] * (FRACMASK + 1L - f)) + (srce[i + 1] * f)) >> FRACBITS);
				index += increment;

				if (m_CurrentVoiceInfo.RampVolume != 0)
				{
					dest[place++] +=
					  (((m_CurrentVoiceInfo.LeftVolumeOld * m_CurrentVoiceInfo.RampVolume) +
						  (m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.RampVolume))
						) * sample) >> CLICK_SHIFT;
					dest[place++] +=
					  (((m_CurrentVoiceInfo.RightVolumeOld * m_CurrentVoiceInfo.RampVolume) +
						  (m_CurrentVoiceInfo.RightVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.RampVolume))
						) * sample) >> CLICK_SHIFT;
					m_CurrentVoiceInfo.RampVolume--;
				}
				else
				{
					if (m_CurrentVoiceInfo.Click != 0)
					{
						dest[place++] +=
						  ((m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.Click) *
							  sample) + (m_CurrentVoiceInfo.LastValueLeft * m_CurrentVoiceInfo.Click))
							>> CLICK_SHIFT;
						dest[place++] +=
						  ((m_CurrentVoiceInfo.RightVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.Click) *
							  sample) + (m_CurrentVoiceInfo.LastValueRight * m_CurrentVoiceInfo.Click))
							>> CLICK_SHIFT;
						m_CurrentVoiceInfo.Click--;
					}
					else
					{
						dest[place++] += m_CurrentVoiceInfo.LeftVolumeFactor * sample;
						dest[place++] += m_CurrentVoiceInfo.RightVolumeFactor * sample;
					}
				}
			}

			m_CurrentVoiceInfo.LastValueLeft = m_CurrentVoiceInfo.LeftVolumeFactor * sample;
			m_CurrentVoiceInfo.LastValueRight = m_CurrentVoiceInfo.RightVolumeFactor * sample;

			return index;
		}

		int Mix32StereoSurround(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			short sample = 0;
			long whoop;
			int i, f;

			while (todo-- != 0)
			{
				i = index >> FRACBITS;
				f = index & FRACMASK;

				sample = (short)(((srce[i] * (FRACMASK + 1L - f)) + (srce[i + 1] * f)) >> FRACBITS);
				index += increment;

				if (m_CurrentVoiceInfo.RampVolume != 0)
				{
					whoop =
					  ((m_CurrentVoiceInfo.LeftVolumeOld * m_CurrentVoiceInfo.RampVolume +
						  (m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.RampVolume))) *
						sample) >> CLICK_SHIFT;
					dest[place++] += (int)whoop;
					dest[place++] -= (int)whoop;
					m_CurrentVoiceInfo.RampVolume--;
				}
				else
				{
					if (m_CurrentVoiceInfo.Click != 0)
					{
						whoop =
						  ((m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.Click) *
							  sample) +
							(m_CurrentVoiceInfo.LastValueLeft * m_CurrentVoiceInfo.Click)) >> CLICK_SHIFT;
						dest[place++] += (int)whoop;
						dest[place++] -= (int)whoop;
						m_CurrentVoiceInfo.Click--;
					}
					else
					{
						dest[place++] += m_CurrentVoiceInfo.LeftVolumeFactor * sample;
						dest[place++] -= m_CurrentVoiceInfo.LeftVolumeFactor * sample;
					}
				}
			}

			m_CurrentVoiceInfo.LastValueLeft = m_CurrentVoiceInfo.LeftVolumeFactor * sample;
			m_CurrentVoiceInfo.LastValueRight = m_CurrentVoiceInfo.LeftVolumeFactor * sample;

			return index;
		}

		long Mix64MonoNormal(short[] srce, int[] dest, long index, long increment, long todo, int place)
		{
			short sample = 0;
			long i, f;

			while (todo-- != 0)
			{
				i = index >> FRACBITS;
				f = index & FRACMASK;

				sample = (short)(((int)(srce[i] * (FRACMASK + 1L - f)) + (srce[i + 1] * f)) >> FRACBITS);
				index += increment;

				if (m_CurrentVoiceInfo.RampVolume != 0)
				{
					dest[place++] +=
					  ((m_CurrentVoiceInfo.LeftVolumeOld * m_CurrentVoiceInfo.RampVolume +
						  (m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.RampVolume))) *
						sample) >> CLICK_SHIFT;
					m_CurrentVoiceInfo.RampVolume--;
				}
				else
				{
					if (m_CurrentVoiceInfo.Click != 0)
					{
						dest[place++] +=
						  ((m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.Click) *
							  sample) + (m_CurrentVoiceInfo.LastValueLeft * m_CurrentVoiceInfo.Click)) >> CLICK_SHIFT;
						m_CurrentVoiceInfo.Click--;
					}
					else
					{
						dest[place++] += m_CurrentVoiceInfo.LeftVolumeFactor * sample;
					}
				}
			}

			m_CurrentVoiceInfo.LastValueLeft = m_CurrentVoiceInfo.LeftVolumeFactor * sample;

			return index;
		}

		long Mix64StereoNormal(short[] srce, int[] dest, long index, long increment, long todo, int place)
		{
			short sample = 0;
			long i, f;

			while (todo-- != 0)
			{
				i = index >> FRACBITS;
				f = index & FRACMASK;

				sample = (short)(((srce[i] * (FRACMASK + 1L - f)) + (srce[i + 1] * f)) >> FRACBITS);
				index += increment;

				if (m_CurrentVoiceInfo.RampVolume != 0)
				{
					dest[place++] +=
					  (((m_CurrentVoiceInfo.LeftVolumeOld * m_CurrentVoiceInfo.RampVolume) +
						  (m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.RampVolume))
						) * sample) >> CLICK_SHIFT;

					dest[place++] +=
					  (((m_CurrentVoiceInfo.RightVolumeOld * m_CurrentVoiceInfo.RampVolume) +
						  (m_CurrentVoiceInfo.RightVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.RampVolume))
						) * sample) >> CLICK_SHIFT;
					m_CurrentVoiceInfo.RampVolume--;
				}
				else
				{
					if (m_CurrentVoiceInfo.Click != 0)
					{
						dest[place++] +=
						  ((m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.Click) *
							  sample) + (m_CurrentVoiceInfo.LastValueLeft * m_CurrentVoiceInfo.Click))
							>> CLICK_SHIFT;
						dest[place++] +=
						  ((m_CurrentVoiceInfo.RightVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.Click) *
							  sample) + (m_CurrentVoiceInfo.LastValueRight * m_CurrentVoiceInfo.Click))
							>> CLICK_SHIFT;
						m_CurrentVoiceInfo.Click--;
					}
					else
					{
						dest[place++] += m_CurrentVoiceInfo.LeftVolumeFactor * sample;
						dest[place++] += m_CurrentVoiceInfo.RightVolumeFactor * sample;
					}
				}
			}

			m_CurrentVoiceInfo.LastValueLeft = m_CurrentVoiceInfo.LeftVolumeFactor * sample;
			m_CurrentVoiceInfo.LastValueRight = m_CurrentVoiceInfo.RightVolumeFactor * sample;

			return index;
		}

		long Mix64StereoSurround(short[] srce, int[] dest, long index, long increment, long todo, int place)
		{
			short sample = 0;
			long whoop;
			long i, f;

			while (todo-- != 0)
			{
				i = index >> FRACBITS;
				f = index & FRACMASK;

				sample = (short)(((srce[i] * (FRACMASK + 1L - f)) + (srce[i + 1] * f)) >> FRACBITS);
				index += increment;

				if (m_CurrentVoiceInfo.RampVolume != 0)
				{
					whoop =
					  ((m_CurrentVoiceInfo.LeftVolumeOld * m_CurrentVoiceInfo.RampVolume +
						  (m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.RampVolume))) *
						sample) >> CLICK_SHIFT;
					dest[place++] += (int)whoop;
					dest[place++] -= (int)whoop;
					m_CurrentVoiceInfo.RampVolume--;
				}
				else
				{
					if (m_CurrentVoiceInfo.Click != 0)
					{
						whoop =
						  ((m_CurrentVoiceInfo.LeftVolumeFactor * (CLICK_BUFFER - m_CurrentVoiceInfo.Click) *
							  sample) +
							(m_CurrentVoiceInfo.LastValueLeft * m_CurrentVoiceInfo.Click)) >> CLICK_SHIFT;
						dest[place++] += (int)whoop;
						dest[place++] -= (int)whoop;
						m_CurrentVoiceInfo.Click--;
					}
					else
					{
						dest[place++] += m_CurrentVoiceInfo.LeftVolumeFactor * sample;
						dest[place++] -= m_CurrentVoiceInfo.LeftVolumeFactor * sample;
					}
				}
			}

			m_CurrentVoiceInfo.LastValueLeft = m_CurrentVoiceInfo.LeftVolumeFactor * sample;
			m_CurrentVoiceInfo.LastValueRight = m_CurrentVoiceInfo.LeftVolumeFactor * sample;

			return index;
		}

		void AddChannel(int[] buff, int todo)
		{
			long end, done;

			var place = 0;

			var s = m_Samples[m_CurrentVoiceInfo.Handle];

			if (s == null)
			{
				m_CurrentVoiceInfo.CurrentSampleIndex = m_CurrentVoiceInfo.Active = 0;
				m_CurrentVoiceInfo.LastValueLeft = m_CurrentVoiceInfo.LastValueRight = 0;
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

				if (m_CurrentVoiceInfo.Volume != 0 || m_CurrentVoiceInfo.RampVolume != 0)
				{
					/* use the 32 bit mixers as often as we can (they're much faster) */
					if ((m_CurrentVoiceInfo.CurrentSampleIndex < 0x7fffffff) && (endpos < 0x7fffffff))
					{
						if (m_IsStereo)
						{
							if ((m_CurrentVoiceInfo.Panning == Constants.PAN_SURROUND) && (ModDriver.Mode & Constants.DMODE_SURROUND) == Constants.DMODE_SURROUND)
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = Mix32StereoSurround(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
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
						if (m_IsStereo)
						{
							if ((m_CurrentVoiceInfo.Panning == Constants.PAN_SURROUND) && (ModDriver.Mode & Constants.DMODE_SURROUND) == Constants.DMODE_SURROUND)
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = Mix64StereoSurround(s, buff, m_CurrentVoiceInfo.CurrentSampleIndex, m_CurrentVoiceInfo.CurrentIncrement, done, place);
							}
							else
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = Mix64StereoNormal(s, buff, m_CurrentVoiceInfo.CurrentSampleIndex, m_CurrentVoiceInfo.CurrentIncrement, done, place);
							}
						}
						else
						{
							m_CurrentVoiceInfo.CurrentSampleIndex = Mix64MonoNormal(s, buff, m_CurrentVoiceInfo.CurrentSampleIndex, m_CurrentVoiceInfo.CurrentIncrement, done, place);
						}
					}
				}
				else
				{
					m_CurrentVoiceInfo.LastValueLeft = m_CurrentVoiceInfo.LastValueRight = 0;
					/* update sample position */
					m_CurrentVoiceInfo.CurrentSampleIndex = endpos;
				}

				todo -= (int)done;
				place += (int)(m_IsStereo ? (done << 1) : done);
			}

		}

		static void Mix32To16_Normal(sbyte[] dste, int[] srce, int count, int dstePlace)
		{
			int x1, x2, tmpx;
			int i;
			var srcePlace = 0;
			var attenuation = 1;
			var bound = 32768;

			for (count /= (int)SAMPLING_FACTOR; count != 0; count--)
			{
				tmpx = 0;

				for (i = (int)SAMPLING_FACTOR / 2; i != 0; i--)
				{
					x1 = srce[srcePlace++] / (MAXVOL_FACTOR * attenuation);
					x2 = srce[srcePlace++] / (MAXVOL_FACTOR * attenuation);

					x1 = (x1 >= bound) ? bound - 1 : (x1 < -bound) ? -bound : x1;
					x2 = (x2 >= bound) ? bound - 1 : (x2 < -bound) ? -bound : x2;

					tmpx += x1 + x2;
				}

				var val = (int)(tmpx / SAMPLING_FACTOR);

				if (BitConverter.IsLittleEndian)
				{
					dste[dstePlace++] = (sbyte)val;
					dste[dstePlace++] = (sbyte)(val >> 8);
				}
				else
				{
					dste[dstePlace++] = (sbyte)((val >> 8) & 0xFF);
					dste[dstePlace++] = (sbyte)(val & 0xFF);
				}
			}
		}

		static void Mix32To16_Stereo(sbyte[] dste, int[] srce, int count, int dstePlace)
		{
			int x1, x2, x3, x4, tmpx, tmpy;
			int i;
			var srcePlace = 0;
			var attenuation = 1;
			var bound = 32768;

			for (count /= (int)SAMPLING_FACTOR; count != 0; count--)
			{
				tmpx = tmpy = 0;

				for (i = (int)SAMPLING_FACTOR / 2; i != 0; i--)
				{
					x1 = srce[srcePlace++] / (MAXVOL_FACTOR * attenuation);
					x2 = srce[srcePlace++] / (MAXVOL_FACTOR * attenuation);
					x3 = srce[srcePlace++] / (MAXVOL_FACTOR * attenuation);
					x4 = srce[srcePlace++] / (MAXVOL_FACTOR * attenuation);

					x1 = (x1 >= bound) ? bound - 1 : (x1 < -bound) ? -bound : x1;
					x2 = (x2 >= bound) ? bound - 1 : (x2 < -bound) ? -bound : x2;
					x3 = (x3 >= bound) ? bound - 1 : (x3 < -bound) ? -bound : x3;
					x4 = (x4 >= bound) ? bound - 1 : (x4 < -bound) ? -bound : x4;

					tmpx += x1 + x3;
					tmpy += x2 + x4;
				}

				var valx = (int)(tmpx / SAMPLING_FACTOR);
				var valy = (int)(tmpy / SAMPLING_FACTOR);

				if (BitConverter.IsLittleEndian)
				{
					dste[dstePlace++] = (sbyte)valx;
					dste[dstePlace++] = (sbyte)(valx >> 8);

					dste[dstePlace++] = (sbyte)valy;
					dste[dstePlace++] = (sbyte)(valy >> 8);

				}
				else
				{
					dste[dstePlace++] = (sbyte)((valx >> 8) & 0xFF);
					dste[dstePlace++] = (sbyte)(valx & 0xFF);

					dste[dstePlace++] = (sbyte)((valy >> 8) & 0xFF);
					dste[dstePlace++] = (sbyte)(valy & 0xFF);
				}
			}
		}

		protected override uint WriteSamples(sbyte[] buf, uint todo)
		{
			int left, portion = 0;
			int t, pan, vol;

			todo *= SAMPLING_FACTOR;

			var bufferPlace = 0;
			var bufPlace = 0;

			while (todo != 0)
			{
				if (m_TickLeft == 0)
				{
					if ((m_VcMode & Constants.DMODE_SOFT_MUSIC) == Constants.DMODE_SOFT_MUSIC)
					{
						ModPlayer.Player_HandleTick();
					}

					m_TickLeft = ModDriver.MixFrequency * 125 * SAMPLING_FACTOR / (ModDriver.Bpm * 50);
					m_TickLeft &= ~(SAMPLING_FACTOR - 1);
				}

				left = (int)Math.Min(m_TickLeft, todo);

				bufferPlace = bufPlace;

				m_TickLeft -= left;
				todo -= (uint)left;
				bufPlace += (int)(samples2bytes((uint)left) / SAMPLING_FACTOR);

				while (left != 0)
				{
					portion = (int)Math.Min(left, m_SamplesThatFit);
					Array.Clear(m_VcTickBuf, 0, TICKLSIZE);

					for (t = 0; t < m_VcSoftChannel; t++)
					{
						m_CurrentVoiceInfo = m_VoiceInfos[t];

						if (m_CurrentVoiceInfo.Kick != 0)
						{
							m_CurrentVoiceInfo.CurrentSampleIndex = ((long)m_CurrentVoiceInfo.Start) << FRACBITS;
							m_CurrentVoiceInfo.Kick = 0;
							m_CurrentVoiceInfo.Active = 1;
							m_CurrentVoiceInfo.Click = CLICK_BUFFER;
							m_CurrentVoiceInfo.RampVolume = 0;
						}

						if (m_CurrentVoiceInfo.Frequency == 0)
						{
							m_CurrentVoiceInfo.Active = 0;
						}

						if (m_CurrentVoiceInfo.Active != 0)
						{
							m_CurrentVoiceInfo.CurrentIncrement = ((long)(m_CurrentVoiceInfo.Frequency << (FRACBITS - SAMPLING_SHIFT))) / ModDriver.MixFrequency;

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
									m_CurrentVoiceInfo.LeftVolumeFactor = m_CurrentVoiceInfo.RightVolumeFactor = vol * 256 / 480;
								}
							}
							else
							{
								m_CurrentVoiceInfo.LeftVolumeFactor = vol;
							}

							m_IdxSize = (m_CurrentVoiceInfo.Size != 0) ? ((long)m_CurrentVoiceInfo.Size << FRACBITS) - 1 : 0;
							m_IdxlEnd = (m_CurrentVoiceInfo.RepeatEndPosition != 0) ? ((long)m_CurrentVoiceInfo.RepeatEndPosition << FRACBITS) - 1 : 0;
							m_IdxlPos = (long)m_CurrentVoiceInfo.RepeatStartPosition << FRACBITS;

							AddChannel(m_VcTickBuf, portion);
						}
					}

					if (ModDriver.Reverb != 0)
					{
						if (ModDriver.Reverb > 15)
						{
							ModDriver.Reverb = 15;
						}

						//MixReverb(vc_tickbuf, portion);
					}

					FireCallBack(portion);

					if ((m_VcMode & Constants.DMODE_16BITS) == Constants.DMODE_16BITS)
					{
						if (m_IsStereo)
						{
							Mix32To16_Stereo(buf, m_VcTickBuf, portion, bufferPlace);
						}
						else
						{
							Mix32To16_Normal(buf, m_VcTickBuf, portion, bufferPlace);
						}
					}
					else
					{
						//Mix32To8(buf, m_VcTickBuf, count, (int)bufferPlace);
					}

					bufferPlace += (int)(samples2bytes((uint)portion) / SAMPLING_FACTOR);
					left -= portion;
				}
			}

			return todo;
		}
	}
}
