using SharpMik.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace MikModUnitTest
{
	public partial class Form1 : Form
	{
		MemSharpMikTest m_SharpTest;
		Process m_ExeProcess;

		UnitTestOptions m_Options = new();

		const string s_OptionsFileName = "options.xml";

		bool m_Loading;

		Thread m_RunThread;

		float m_CSTestTime;

		float m_TestTime;

		public Form1()
		{
			InitializeComponent();

			StopButton.Enabled = false;
			m_SharpTest = new MemSharpMikTest();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			UnitTestHelpers.FindRepeats();
			if (UnitTestHelpers.ReadXML(s_OptionsFileName, ref m_Options))
			{
				m_Loading = true;
				textBox1.Text = m_Options.MikModCExe;

				textBox3.Text = m_Options.TestModFolder;
				textBox4.Text = m_Options.TestDirectory;
				checkBox1.Checked = m_Options.CopyBrokenMods;
				m_Loading = false;
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			m_SharpTest.ShutDown();

			if (m_ExeProcess != null)
			{
				m_RunThread.Abort();
				m_ExeProcess.Close();
			}
		}

		void button2_Click(object sender, EventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Filter = "*.exe|*.exe"
			};

			var result = dialog.ShowDialog();

			if (result == DialogResult.OK)
			{
				textBox1.Text = dialog.FileName;
			}
		}

		void button4_Click(object sender, EventArgs e)
		{
			var dialog = new FolderBrowserDialog();

			var result = dialog.ShowDialog();

			if (result == DialogResult.OK)
			{
				textBox3.Text = dialog.SelectedPath;
			}
		}

		void Start_Click(object sender, EventArgs e)
		{
			Start.Enabled = false;

			if (!File.Exists(m_Options.MikModCExe))
			{
				_ = MessageBox.Show("C mikMod exe could not be found");
			}
			else
			{
				StopButton.Enabled = true;

				textBox1.Enabled = false;

				textBox3.Enabled = false;
				textBox4.Enabled = false;
				button1.Enabled = false;
				button2.Enabled = false;
				button4.Enabled = false;

				m_RunThread = new Thread(new ThreadStart(TestThread))
				{
					Name = "Test Thread"
				};
				m_RunThread.Start();
			}
		}

		void ResetButtons()
		{
			MethodInvoker action = delegate
			{
				Start.Enabled = true;

				StopButton.Enabled = false;

				textBox1.Enabled = true;
				textBox3.Enabled = true;
				textBox4.Enabled = true;
				button1.Enabled = true;
				button2.Enabled = true;
				button4.Enabled = true;
			};

			_ = BeginInvoke(action);
		}

		void Result_Enter(object sender, EventArgs e)
		{

		}

		void button1_Click(object sender, EventArgs e)
		{
			var dialog = new FolderBrowserDialog();

			var result = dialog.ShowDialog();

			if (result == DialogResult.OK)
			{
				textBox4.Text = dialog.SelectedPath;
			}
		}

		void textBox1_TextChanged(object sender, EventArgs e) => UpdateSaveFile();

		void textBox2_TextChanged(object sender, EventArgs e) => UpdateSaveFile();

		void textBox3_TextChanged(object sender, EventArgs e) => UpdateSaveFile();

		void textBox4_TextChanged(object sender, EventArgs e) => UpdateSaveFile();

		void UpdateSaveFile()
		{
			if (!m_Loading)
			{
				m_Options.MikModCExe = textBox1.Text;
				m_Options.TestModFolder = textBox3.Text;
				m_Options.TestDirectory = textBox4.Text;
				m_Options.CopyBrokenMods = checkBox1.Checked;

				_ = UnitTestHelpers.WriteXML(s_OptionsFileName, m_Options);
			}
		}

		void TestThread()
		{
			var totalStart = DateTime.Now;
			s_StopThread = false;
			//List<String> modFiles = new List<string>();
			var testResults = new List<TestResult>();

			//modFiles.Clear();

			var files = Directory.EnumerateFiles(m_Options.TestModFolder, "*.*", SearchOption.AllDirectories);

			var modFiles = new List<string>();

			foreach (var name in files)
			{
				if (Helpers.MatchesExtensions(name))
				{
					modFiles.Add(name);
				}
			}

			var count = 0;

			MethodInvoker action = delegate
			{
				progressBar2.Maximum = modFiles.Count();
				progressBar2.Value = 0;
			};
			_ = progressBar2.BeginInvoke(action);

			var total = 0.0f;
			var passed = 0;
			var failed = 0;

			action = delegate
			{
				label7.Text = string.Format("Testing {0} of {1} mods in {5} seconds, Passed: {2}, Failed: {3}, Passing percentage {4}", count, modFiles.Count(), passed, failed, 0.0, (DateTime.Now - totalStart).TotalSeconds);
			};
			_ = label7.BeginInvoke(action);

			foreach (var fileName in modFiles)
			{
				var result = new TestResult();

				var start = DateTime.Now;
				if (DoStreamTest(fileName, ref result))
				{
					result = TestWavAndStream(fileName);
				}

				var span = DateTime.Now - start;

				result.CTime = m_CTestTime;
				result.CSharpTime = m_CSTestTime;
				result.TotalTestTime = (float)span.TotalSeconds;
				testResults.Add(result);

				if (!result.Passed && checkBox1.Checked)
				{
					var failedFolder = Path.Combine(m_Options.TestDirectory, "Failed");
					var justFileName = Path.GetFileName(fileName);
					var failName = Path.Combine(failedFolder, justFileName);

					if (!File.Exists(failName))
					{
						if (!Directory.Exists(failedFolder))
						{
							_ = Directory.CreateDirectory(Path.Combine(m_Options.TestDirectory, "Failed"));
						}

						File.Copy(fileName, failName);
					}
				}

				// Write out the results after each test, this will help if the app crashes.
				_ = UnitTestHelpers.WriteXML(Path.Combine(m_Options.TestDirectory, "test.xml"), testResults);

				total += result.MatchPercentage;
				count++;

				var data = new object[7];
				data[0] = Path.GetFileName(fileName);
				data[1] = result.MatchPercentage;
				data[3] = m_CTestTime;
				data[4] = m_CSTestTime;
				data[5] = m_TestTime;
				data[6] = span.TotalSeconds;

				if (result.Passed)
				{
					data[2] = Properties.Resources.Pass;
					passed++;
				}
				else
				{
					data[2] = Properties.Resources.Fail;
					failed++;
				}

				if (IsHandleCreated)
				{
					action = delegate
					{
						progressBar2.Value = count;
					};
					_ = progressBar2.BeginInvoke(action);

					action = delegate
					{
						_ = dataGridView1.Rows.Add(data);
					};
					_ = dataGridView1.BeginInvoke(action);

					action = delegate
					{
						label7.Text = string.Format("Testing {0} of {1} mods in {5} seconds, Passed: {2}, Failed: {3}, Passing percentage {4}", count, modFiles.Count(), passed, failed, total / count, (DateTime.Now - totalStart).TotalSeconds);
						//label7.Text = String.Format("Testing {0} of {1} mods, Passed: {2}, Failed: {3}, Passing percentage {4}", count, modFiles.Count(), passed, failed, total / count);
					};
					_ = label7.BeginInvoke(action);
				}

				if (s_StopThread)
				{
					break;
				}
			}

			s_StopThread = false;
			ResetButtons();
		}

		bool DoStreamTest(string fileName, ref TestResult result)
		{
			m_SharpTest.Start(fileName);
			if (!RunMikC(fileName))
			{
				result.ModName = fileName;
				result.Passed = true;
				result.Error = "MikMod failed to load file, so no chance for SharpMik";
				result.MatchPercentage = 100.0f;
			}
			else
			{
				while (m_SharpTest.IsRunning())
				{
					Thread.Sleep(100);
				}

				if (string.IsNullOrEmpty(m_SharpTest.ErrorMessage))
				{
					m_CSTestTime = m_SharpTest.TimeTaken;
					return true;
				}
				else
				{
					result.ModName = fileName;
					result.Passed = true;
					result.Error = m_SharpTest.ErrorMessage;
					result.MatchPercentage = 100.0f;
				}
			}

			return false;
		}

		float m_CTestTime;

		bool RunMikC(string mod)
		{
			var start = DateTime.Now;
			var startInfo = new ProcessStartInfo
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				FileName = m_Options.MikModCExe,
				Arguments = "\"" + mod + "\" \"" + Path.Combine(m_Options.TestDirectory, "mikmodC.wav") + "\""
			};
			//startInfo.RedirectStandardOutput = true;

			using (m_ExeProcess = Process.Start(startInfo))
			{
				m_ExeProcess.PriorityClass = ProcessPriorityClass.RealTime;
				m_ExeProcess.WaitForExit();
			}

			m_ExeProcess = null;

			var span = DateTime.Now - start;
			m_CTestTime = (float)span.TotalSeconds;

			var mikModCWav = Path.Combine(m_Options.TestDirectory, "mikmodC.wav");

			if (File.Exists(mikModCWav))
			{
				var info = new FileInfo(mikModCWav);

				if (info.Length > 44)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		TestResult TestWavAndStream(string mod)
		{
			var start = DateTime.Now;
			var result = new TestResult
			{
				ModName = mod
			};
			var diffBytes = 0;
			var reportedMissMatch = false;

			var mikModCWav = Path.Combine(m_Options.TestDirectory, "mikmodC.wav");

			try
			{
				if (File.Exists(mikModCWav))
				{
					var mikModCReader = new BinaryReader(new FileStream(mikModCWav, FileMode.Open));

					var cSize = mikModCReader.BaseStream.Length;
					var cSharpSize = m_SharpTest.MemStream.Length;

					if (cSize == 44) // header size of a wav
					{
						// MikMod can't process the mod either so no point checking.
						// Mark it as a pass but put a note in about it.

						result.Error = "MikMod wasn't able to load the mod, so no point checking";
						result.Passed = true;
						result.MatchPercentage = 100;
					}
					else
					{
						if (cSize != cSharpSize + 44)
						{
							result.Error = "file sizes don't match";
							result.Passed = false;
						}
						else
						{
							byte cByte;
							byte cSharpByte;

							_ = mikModCReader.BaseStream.Seek(44, SeekOrigin.Begin);
							_ = m_SharpTest.MemStream.Seek(0, SeekOrigin.Begin);
							var size = cSize - 44;
							for (long i = 0; i < size; i++)
							{
								cByte = mikModCReader.ReadByte();
								cSharpByte = (byte)m_SharpTest.MemStream.ReadByte();

								if (cByte != cSharpByte)
								{
									if (!reportedMissMatch)
									{
										reportedMissMatch = true;
										Console.WriteLine("First miss matched byte is at: " + i);
										result.FirstMissMatchByte = i;
									}

									diffBytes++;
								}
							}

							result.MatchPercentage = 100.0f * (cSize - diffBytes) / cSize;
							if (diffBytes > 0)
							{
								result.Passed = false;
							}
							else
							{
								result.Passed = true;
							}
						}
					}

					mikModCReader.Close();
					mikModCReader.Dispose();
				}
			}
			catch (Exception ex)
			{
				result.Error = ex.Message;
				result.Passed = false;
			}

			var span = DateTime.Now - start;
			m_TestTime = (float)span.TotalSeconds;

			return result;
		}

		static bool s_StopThread;
		void Stop_Click(object sender, EventArgs e) => s_StopThread = true;

		void checkBox1_CheckedChanged(object sender, EventArgs e) => UpdateSaveFile();
	}

	public class TestResult
	{
		public string ModName { get; set; }
		public float MatchPercentage { get; set; }
		public string Error { get; set; }
		public bool Passed { get; set; }
		public long FirstMissMatchByte { get; set; }
		public float CTime { get; set; }
		public float CSharpTime { get; set; }
		public float TotalTestTime { get; set; }
	}

}
