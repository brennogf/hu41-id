using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ControliD;
using Microsoft.Win32;
using System.Data;
using static HU41ID.Criptografia;
using static System.Windows.Forms.LinkLabel;

namespace HU41ID
{
    public partial class HU41ID : Form
    {
        #region initilization

        public CIDBio idbio = new CIDBio();
        public Thread th, t;
        public Stopwatch cronometro1 = new Stopwatch();


        public HU41ID()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        struct IdentifyRet
        {
            public RetCode ret;
            public long id;
        }

        struct FingerImage
        {
            public RetCode ret;
            public byte[] imageBuf;
            public uint width;
            public uint height;
        }

        public async void NovaThread()
        {
        repeat:
            try
            {
                while (true)
                {
                    var identify = await Task.Run(() =>
                    {
                        return new IdentifyRet
                        {
                            ret = idbio.CaptureAndIdentify(out long id, out int score, out int quality),
                            id = id
                        };
                    });
                    if (identify.ret == RetCode.SUCCESS)
                    {
                        String nip = identify.id.ToString();
                        nip = nip.PadLeft(8, '0');
                        SendKeys.SendWait(nip);
                        SendKeys.SendWait("{ENTER}");
                    }
                }
            }
            catch
            {
                goto repeat;
            }
        }

        private void HU41_Load(object sender, EventArgs e)
        {
            Thread th = new Thread(NovaThread);
            th.Start();
            ReloadIDs();
            this.Visible = false;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            notifyIcon1.Visible = true;

            try
            {
                string caminho = @"C:\Arquivos de Programas\HU41-ID\HU41-ID.exe";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key.SetValue(caminho, "\"" + Application.ExecutablePath + "\"");
                }
            }
            catch
            {
                
            }
        }

        private void HU41_FormClosed(object sender, FormClosedEventArgs e)
        {
            CIDBio.Terminate();
        }

        #endregion

        #region identification

        private void ReloadIDs()
        {
            try
            {
                String nip;
                var ret = idbio.GetTemplateIDs(out long[] ids);
                if (ret == RetCode.SUCCESS)
                {
                    label4.Text = "0";
                    iDsList.Items.Clear();
                    foreach (var id in ids)
                    {
                        nip = id.ToString();
                        nip = nip.PadLeft(8, '0');
                        iDsList.Items.Add(nip);
                        label4.Text = ids.Length.ToString();
                    }
                }
            }
            catch
            {

            }
        }

        private void IdentifyLog_TextChanged(object sender, EventArgs e)
        {
            // set the current caret position to the end
            IdentifyLog.SelectionStart = IdentifyLog.Text.Length;
            // scroll it automatically
            IdentifyLog.ScrollToCaret();
        }

        #endregion

        private void button1_Click_1(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Deseja realmente EXCLUIR esse usuário?", "Atenção", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    Thread th = new Thread(NewThread);
                    th.Start();
                }
        }

        public void NewThread()
        {
            try
            {
                button1.Enabled = false;
                iDsList.Enabled = false;
                enrollBtn.Enabled = false;
                enrollIDTextBox.Enabled = false;
                textBox3.Enabled = false;
                button3.Enabled = false;
                menuStrip1.Enabled = false;
                String item = iDsList.SelectedItems[0].ToString();
                idbio.DeleteTemplate((long)Convert.ToDouble(item));
                IdentifyLog.Text += "NIP " + item + " excluído com sucesso!\r\n";
                iDsList.Items.Remove(iDsList.SelectedItems[0]);
                double totalUsers = Convert.ToDouble(label4.Text) - 1;
                label4.Text = totalUsers.ToString();
            }
            catch (Exception ex)
            {
                if (ex.Message == "O índice estava fora dos limites da matriz.")
                {
                    IdentifyLog.Text += "Insira um NIP...\r\n";
                }
                else
                {
                    IdentifyLog.Text += "Erro: " + ex.Message + "\r\n";
                }
            }
            finally
            {
                button1.Enabled = true;
                iDsList.Enabled = true;
                enrollBtn.Enabled = true;
                enrollIDTextBox.Enabled = true;
                textBox3.Enabled = true;
                button3.Enabled = true;
                menuStrip1.Enabled = true;
            }
        }

        private async void enrollBtn_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (enrollIDTextBox.TextLength == 8)
                {
                    button1.Enabled = false;
                    iDsList.Enabled = false;
                    enrollBtn.Enabled = false;
                    enrollIDTextBox.Enabled = false;
                    textBox3.Enabled = false;
                    button3.Enabled = false;
                    menuStrip1.Enabled = false;
                    long id = long.Parse(enrollIDTextBox.Text);
                    {
                        if (!iDsList.Items.Contains(enrollIDTextBox.Text))
                        {
                            IdentifyLog.Text += "Pressione o dedo 5 vezes...\r\n";
                            CaptureImage();
                            var ret = await Task.Run(() =>
                            {
                                return idbio.CaptureAndEnroll(id);
                            });
                            if (ret < RetCode.SUCCESS)
                            {
                                IdentifyLog.Text += "Erro: " + CIDBio.GetErrorMessage(ret) + "\r\n";
                            }
                            else
                            {
                                IdentifyLog.Text += "NIP " + enrollIDTextBox.Text + " cadastrado com sucesso!\r\n";
                                iDsList.Items.Add(enrollIDTextBox.Text);
                                double totalUsers = Convert.ToDouble(label4.Text) + 1;
                                label4.Text = totalUsers.ToString();
                                enrollIDTextBox.Clear();
                                enrollIDTextBox.Focus();
                            }
                        }
                        else
                        {
                            IdentifyLog.Text += "Esse NIP já está cadastrado...\r\n";
                        }
                    }

                }
                else if (enrollIDTextBox.TextLength == 0)
                {
                    IdentifyLog.Text += "Digite um NIP...\r\n";
                }
                else
                {
                    IdentifyLog.Text += "Digite um NIP válido...\r\n";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Template was already in device database")
                {
                    IdentifyLog.Text += "NIP já cadastrado...\r\n";
                }
                else
                {
                    IdentifyLog.Text += "Erro: " + ex.Message + "\r\n";
                }
            }
            finally
            {
                button1.Enabled = true;
                iDsList.Enabled = true;
                enrollBtn.Enabled = true;
                enrollIDTextBox.Enabled = true;
                textBox3.Enabled = true;
                button3.Enabled = true;
                menuStrip1.Enabled = true;
                enrollIDTextBox.Clear();
                enrollIDTextBox.Focus();
            }
        }

        private async void CaptureImage()
        {
            var img = await Task.Run(() => {
                return new FingerImage
                {
                    ret = idbio.CaptureImage(out byte[] imageBuf, out uint width, out uint height),
                    imageBuf = imageBuf,
                    width = width,
                    height = height
                };
            });

            if (img.ret < RetCode.SUCCESS)
            {
                IdentifyLog.Text += "Erro: " + CIDBio.GetErrorMessage(img.ret) + "\r\n";
                fingerImage.Image = null;
            }
            else
            {
                RenderImage(img.imageBuf, img.width, img.height);
            }
        }

        public static Bitmap ImageBufferToBitmap(byte[] imageBuf, uint width, uint height)
        {
            Bitmap img = new Bitmap((int)width, (int)height);
            for (int x = 0; x < img.Width; x++)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    var color = Color.FromArgb(imageBuf[x + img.Width * y], imageBuf[x + img.Width * y], imageBuf[x + img.Width * y]);
                    img.SetPixel(x, y, color);
                }
            }
            return img;
        }

        private void RenderImage(byte[] imageBuf, uint width, uint height)
        {
            var img = ImageBufferToBitmap(imageBuf, width, height);
            fingerImage.Image = img;
            fingerImage.Width = img.Width;
            fingerImage.Height = img.Height;
        }

        private void HU41_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            menuStrip1.Visible = false;
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            panel1.Visible = true;
            textBox1.Focus();
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CIDBio.Terminate();
            Application.Exit();
        }

        private void sobreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Desenvolvido por Brenno Givigier");
        }

        private void HU41_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
                this.ShowInTaskbar = false;
                this.WindowState = FormWindowState.Minimized;
                menuStrip1.Visible = false;
            }

        }

        private void enrollIDTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != (char)8)
            {
                e.Handled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string key = "Criptografia";
                Criptografia crip = new Criptografia(CryptProvider.DES);
                crip.Key = key;

                if (!File.Exists(@"C:\Arquivos de Programas\HU41-ID\password"))
                {
                    StreamWriter writer = new StreamWriter(@"C:\Arquivos de Programas\HU41-ID\password");
                    writer.Write(crip.Encrypt(textBox2.Text));
                    writer.Close();
                }

                    StreamReader sr = new StreamReader(@"C:\Arquivos de Programas\HU41-ID\password");
                    string line = sr.ReadLine();

                    if ((textBox1.Text == "admin") & (textBox2.Text == crip.Decrypt(line)))
                    {
                        panel1.Visible = false;
                        menuStrip1.Visible = true;
                        textBox1.Clear();
                        textBox2.Clear();
                        enrollIDTextBox.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Login e/ou senha incorretos!");
                        textBox2.Clear();
                        textBox2.Focus();
                    }
            } catch {

            }
        }

        private void IdentifyLog_TextChanged_1(object sender, EventArgs e)
        {
            IdentifyLog.SelectionStart = IdentifyLog.Text.Length;
            IdentifyLog.ScrollToCaret();
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button2_Click(this, new EventArgs());
            }
        }

        private void enrollIDTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                enrollBtn_Click_1(this, new EventArgs());
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if(textBox3.Text.Length == 8)
                {
                    int aux = 0;
                    for (int i = 0; i < iDsList.Items.Count; i++)
                    {
                        if (iDsList.Items[i].ToString() == textBox3.Text)
                        {
                            iDsList.SetSelected(i, true);
                            aux = 1;
                        }
                    
                    }
                    if (aux == 0)
                    {
                        MessageBox.Show("NIP não encontrado!");
                    }
                } else
                {
                    MessageBox.Show("Digite um NIP válido!");
                }
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            } finally
            {
                textBox3.Clear();
                textBox3.Focus();
            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button3_Click(this, new EventArgs());
            }
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != (char)8)
            {
                e.Handled = true;
            }
        }

        private void salvarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Title = "Salvar Arquivo";
            saveFileDialog1.Filter = "Backup File|.bkp";
            saveFileDialog1.FilterIndex = 0;
            saveFileDialog1.FileName = "Backup_" + DateTime.Now.ToString("dd-MM-yyyy");
            saveFileDialog1.DefaultExt = ".bkp";
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                button1.Enabled = false;
                iDsList.Enabled = false;
                enrollBtn.Enabled = false;
                enrollIDTextBox.Enabled = false;
                textBox3.Enabled = false;
                button3.Enabled = false;
                menuStrip1.Enabled = false;
                panel2.Visible = true;
                label7.Text = "Executando backup...";
                timer1.Enabled = true;
                ativarCronometro();

                t = new Thread(backup);
                t.Start();
            }
        }

        public void backup()
        {
            try
            {
                long[] ids;
                idbio.GetTemplateIDs(out ids);
                string[] temp = new string[ids.Length];

                for (int i = 0; i < ids.Length; i++)
                {
                    idbio.GetTemplate(ids[i], out temp[i]);
                }
                FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create);

                StreamWriter writer = new StreamWriter(fs);
                for (int i = 0; i < ids.Length; i++)
                {
                    writer.WriteLine(ids[i] + ":" + temp[i]);
                }
                writer.Close();
                IdentifyLog.Text += "Backup realizado com sucesso!\r\n";
            } catch {
                panel2.Visible = false;
            } finally
            {
                button1.Enabled = true;
                iDsList.Enabled = true;
                enrollBtn.Enabled = true;
                enrollIDTextBox.Enabled = true;
                textBox3.Enabled = true;
                button3.Enabled = true;
                menuStrip1.Enabled = true;
                panel2.Visible = false;
                timer1.Enabled = false;
                desativarCronometro();
            }
        }

        private void carregarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Backup File (.bkp)|*.bkp";
            openFileDialog1.Title = "Carregar Arquivo";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.DefaultExt = ".bkp";
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                button1.Enabled = false;
                iDsList.Enabled = false;
                enrollBtn.Enabled = false;
                enrollIDTextBox.Enabled = false;
                textBox3.Enabled = false;
                button3.Enabled = false;
                menuStrip1.Enabled = false;
                panel2.Visible = true;
                label7.Text = "Executando restore...";
                timer1.Enabled = true;
                ativarCronometro();

                th = new Thread(restoreBackup);
                th.Start();
            }
        }

        public void restoreBackup()
        {
            string line = "";
            string[] parts;
            double cont = 0;

            try
            {
                 StreamReader sr = new StreamReader(openFileDialog1.FileName);
                 while (line != null)
                 {
                     line = sr.ReadLine();
                     if (line != null)
                     {
                         parts = line.Split(':');
                         idbio.SaveTemplate((long)Convert.ToDouble(parts[0]), parts[1]);
                         parts[0] = parts[0].PadLeft(8, '0');
                     if (!iDsList.Items.Contains(parts[0]))
                     {
                         iDsList.Items.Add(parts[0]);
                         cont++;
                     }   
                 }
            }
            sr.Close();
            double contAlt = Convert.ToDouble(label4.Text) + cont;
            label4.Text = contAlt.ToString();

                IdentifyLog.Text += "Restore realizado com sucesso!\r\n";
            }
            catch
            {
                panel2.Visible = false;
            } finally
            {
                button1.Enabled = true;
                iDsList.Enabled = true;
                enrollBtn.Enabled = true;
                enrollIDTextBox.Enabled = true;
                textBox3.Enabled = true;
                button3.Enabled = true;
                menuStrip1.Enabled = true;
                panel2.Visible = false;
                timer1.Enabled = false;
                desativarCronometro();
            }
        }

        private void excluirTudoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string Prompt = "Tem certeza que deseja EXCLUIR TUDO? Se sim, digite EXCLUIR para confirmar.";
            string Titulo = "Atenção";
            string Resultado = Microsoft.VisualBasic.Interaction.InputBox(Prompt, Titulo, "", 150, 150);
            string password = "EXCLUIR";

             Resultado = Resultado.TrimStart();

             if (Resultado == password)
             {
                iDsList.Items.Clear();
                idbio.DeleteAllTemplates();
                label4.Text = "0";
                IdentifyLog.Text += "As digitais foram excluídas com sucesso!\r\n";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Value < 100)
                progressBar1.Value += 2;
            else 
                progressBar1.Value = 0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
            timer1.Enabled = false;

            if (label7.Text == "Executando restore...")
            {
                th.Interrupt();
                th.Abort();
            } else
            {
                t.Interrupt();
                t.Abort();
            } 
        }

        private void TmrCronometro_Tick(object sender, EventArgs e)
        {
            if (cronometro1.Elapsed.Hours < 10)
                lblHour.Text = "0" + cronometro1.Elapsed.Hours.ToString();
            else
                lblHour.Text = cronometro1.Elapsed.Hours.ToString();

            if (cronometro1.Elapsed.Minutes < 10)
                lblMinute.Text = "0" + cronometro1.Elapsed.Minutes.ToString();
            else
                lblMinute.Text = cronometro1.Elapsed.Minutes.ToString();

            if (cronometro1.Elapsed.Seconds < 10)
                lblSecond.Text = "0" + cronometro1.Elapsed.Seconds.ToString();
            else
                lblSecond.Text = cronometro1.Elapsed.Seconds.ToString();
        }

        public void ativarCronometro()
        {
            cronometro1.Start();
        }

        private void atualizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadIDs();
        }

        private void compararNIPsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog2.Filter = "CSVV Files|*.csv";
            openFileDialog2.Title = "Selecione a planilha";
            if (openFileDialog2.ShowDialog() != DialogResult.OK) return;
            string inputFile = openFileDialog2.FileName;

            saveFileDialog2.Filter = "CSV Files|*.csv";
            saveFileDialog2.Title = "Selecione o local para salvar a planilha";

            if (saveFileDialog2.ShowDialog() != DialogResult.OK) return;
            string outputFile = saveFileDialog2.FileName;
            StreamWriter writer = new StreamWriter(outputFile);

            DataTable inputData = new DataTable();
            using (var reader = new StreamReader(inputFile))
            {
                //inputData.Load(reader);
            }

            DataRow[] rows = null;
            for(int i = 0; i < iDsList.Items.Count; i++)
            {
                rows = inputData.Select("NIP = '" + iDsList.Items[i] + "'");

                if (rows != null)
                {
                    writer.WriteLine(rows);
                    rows = null;
                }
            }
        }

        private void alterarSenhaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string Prompt = "Digite a nova senha.";
            string Titulo = "Atenção";
            string Resultado = Microsoft.VisualBasic.Interaction.InputBox(Prompt, Titulo, "", 150, 150);
            string key = "Criptografia";
            Criptografia crip = new Criptografia(CryptProvider.DES);
            crip.Key = key;

            StreamWriter writer = new StreamWriter(@"C:\Arquivos de Programas\HU41-ID\password");
            writer.Write(crip.Encrypt(Resultado));
            writer.Close();
            IdentifyLog.Text += "A senha foi alterada com sucesso!\r\n";
        }

        public void desativarCronometro()
        {
            cronometro1.Stop();
            cronometro1.Reset();
        }
    }
}
