using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace LockFlow
{
    public partial class LockFlow : Form
    {
        private FileSystemWatcher watcher;
        private Socket serverSocket;

        // win API funkcije
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private bool isServerRunning = false;
        public LockFlow()
        {
            InitializeComponent();
            watcher = new FileSystemWatcher();

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /*public char[,] BifidMatrix(string keyword)
        {
            keyword = new string(keyword.Distinct().ToArray()).ToLower();
            //string code = "abcdefghiklmnopqrstuvwxyz"; // 5x5 bez J, 6x6 sve sa brojevima
            string code = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+[]{}|;:',.<>?/`~÷€£¥°\u25A0\u2665\u2666"; // 10x10

            foreach (char c in code)
            {
                if (!keyword.Contains(c))
                {
                    keyword += c; // dodajemo ono sto nije u kljucu
                }
            }

            char[,] mat = new char[10, 10];
            int index = 0;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    mat[i, j] = keyword[index++];
                }
            }

            // stampa matrice

            *//*string matrixText = "";
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    matrixText += mat[i, j] + " ";
                }
                matrixText += Environment.NewLine;
            }
            label1.Text = matrixText;*//*

            return mat;
        }*/
        public string BifidEncryptTxt(string text, char[,] mat)
        {
            //text = text.Replace(" ", ""); // bez razmaka
            //text = text.Replace("j", "i");
            text = text.Replace(" ", "\u25A0"); // kvadrat
            text = text.Replace("\n", "\u2665"); // srce
            text = text.Replace("\t", "\u2666"); // dijamant



            List<int> redovi = new List<int>();
            List<int> kolone = new List<int>();

            foreach (char c in text)
            {
                bool found = false;
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        if (mat[i, j] == c)
                        {
                            redovi.Add(i);
                            kolone.Add(j);
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
            }

            List<int> result = new List<int>(redovi.Count + kolone.Count);

            for (int j = 0; j < redovi.Count; j++)
            {
                result.Add(redovi[j]);
            }

            for (int j = 0; j < kolone.Count; j++)
            {
                result.Add(kolone[j]);
            }


            string encryptText = "";
            for (int i = 0; i < result.Count - 1; i = i + 2)
            {
                encryptText += mat[result[i], result[i + 1]].ToString() + " "; // dodajemo razmak;
            }

            // ukloni poslednji razmak
            encryptText = encryptText.Trim();

            for (int i = 70; i < encryptText.Length; i += 71) // dodajem novi red nakon 70 karaktera radi lepseg izgleda encrypt fajla :)
            {
                encryptText = encryptText.Insert(i, "\n");
            }

            return encryptText;
        }
        public string BifidDecryptTxt(string encryptedText, char[,] mat)
        {
            encryptedText = encryptedText.Replace("\n", ""); // ukloni nove redove od enkripcije
            encryptedText = encryptedText.Replace(" ", ""); // ukloni razmake od enkripcije

            List<int> result = new List<int>();

            foreach (char c in encryptedText)
            {
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        if (mat[i, j] == c)
                        {
                            result.Add(i);
                            result.Add(j);
                        }
                    }
                }
            }

            string originalText = "";

            int half = result.Count / 2;
            List<int> rows = result.GetRange(0, half);
            List<int> cols = result.GetRange(half, half);

            for (int i = 0; i < rows.Count; i++)
            {
                originalText += mat[rows[i], cols[i]].ToString();
            }

            originalText = originalText.Replace("\u25A0", " ");
            originalText = originalText.Replace("\u2665", "\n");
            originalText = originalText.Replace("\u2666", "\t");

            return originalText;
        }


        public byte[,] BifidMatrix(string keyword)
        {
            keyword = new string(keyword.Distinct().ToArray()).ToLower(); // bez duplikata + lower

            List<byte> allBytes = Enumerable.Range(0, 256).Select(x => (byte)x).ToList(); // sve bajt vrednosti

            List<byte> matrixBytes = new List<byte>();
            foreach (char c in keyword)
            {
                byte b = (byte)c;
                if (allBytes.Contains(b))
                {
                    matrixBytes.Add(b);
                    allBytes.Remove(b);
                }
            }

            matrixBytes.AddRange(allBytes); // dodavanje ostalih bajtova koji nisu u kljucu

            byte[,] mat = new byte[16, 16];
            int index = 0;

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    mat[i, j] = matrixBytes[index++];
                }
            }

            return mat;
        }
        public byte[] BifidEncryptOther(byte[] data, byte[,] mat)
        {
            List<int> redovi = new List<int>();
            List<int> kolone = new List<int>();

            foreach (byte b in data)
            {
                bool found = false;
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        if (mat[i, j] == b)
                        {
                            redovi.Add(i);
                            kolone.Add(j);
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
            }

            List<int> result = new List<int>(redovi.Count + kolone.Count);

            for (int j = 0; j < redovi.Count; j++)
            {
                result.Add(redovi[j]);
            }

            for (int j = 0; j < kolone.Count; j++)
            {
                result.Add(kolone[j]);
            }

            List<byte> encryptedBytes = new List<byte>();
            for (int i = 0; i < result.Count - 1; i += 2)
            {
                encryptedBytes.Add(mat[result[i], result[i + 1]]);
            }

            return encryptedBytes.ToArray();
        }
        public byte[] BifidDecryptOther(byte[] encryptedData, byte[,] mat)
        {
            List<int> redoviKolone = new List<int>();

            foreach (byte b in encryptedData)
            {
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        if (mat[i, j] == b)
                        {
                            redoviKolone.Add(i);
                            redoviKolone.Add(j);
                            break;
                        }
                    }
                }
            }

            int half = redoviKolone.Count / 2;
            List<int> redovi = redoviKolone.GetRange(0, half);
            List<int> kolone = redoviKolone.GetRange(half, half);

            List<byte> originalData = new List<byte>();
            for (int i = 0; i < redovi.Count; i++)
            {
                originalData.Add(mat[redovi[i], kolone[i]]);
            }

            return originalData.ToArray();
        }

        private uint RotateLeft(uint value, int shift)
        {
            return (value << shift) | (value >> (32 - shift));
        }
        private uint RotateRight(uint value, int shift)
        {
            return (value >> shift) | (value << (32 - shift));
        }
        private byte[] ExpandKey(string key, int desiredLength)
        {
            // produzavanje kljuca od 26 karaktera za 32 za rc6
            byte[] keyBytes = new byte[desiredLength];
            byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes(key);

            for (int i = 0; i < desiredLength; i++)
            {
                keyBytes[i] = originalBytes[i % originalBytes.Length]; // ciklicno
            }

            return keyBytes;
        }
        private uint[] GenerateS(byte[] key, int R)
        {
            int c = key.Length / 4; // br 32-bitnih reci kljuca
            uint[] L = new uint[c];

            for (int i = 0; i < c; i++)
            {
                L[i] = BitConverter.ToUInt32(key, i * 4);
            }

            int S_size = 2 * R + 4;
            uint[] S = new uint[S_size];

            // konstante za rc6 za difuziju
            uint Pw = 0xB7E15163;
            uint Qw = 0x9E3779B9;

            S[0] = Pw;
            for (int i = 1; i < S_size; i++)
            {
                S[i] = S[i - 1] + Qw;
            }

            // mesanje L i S, medjusobno se kombinuju i rotiraju
            uint A = 0, B = 0;
            int iIndex = 0, jIndex = 0;
            int iterations = 3 * Math.Max(S_size, c);

            for (int i = 0; i < iterations; i++)
            {
                S[iIndex] = RotateLeft(S[iIndex] + A + B, 3);
                A = S[iIndex];

                L[jIndex] = RotateLeft(L[jIndex] + A + B, (int)(A + B));
                B = L[jIndex];

                iIndex = (iIndex + 1) % S_size;
                jIndex = (jIndex + 1) % c;
            }

            return S;
        }

        public byte[] RC6Encrypt(byte[] data, byte[] key)
        {
            const int R = 20; // br rundi, default 20
            uint[] S = GenerateS(key, R);

            if (data.Length % 16 != 0)
            {
                Array.Resize(ref data, data.Length + (16 - data.Length % 16));
            }

            byte[] encrypted = new byte[data.Length];

            for (int i = 0; i < data.Length; i += 16)
            {
                // 4 32-bitne reci
                uint A = BitConverter.ToUInt32(data, i);
                uint B = BitConverter.ToUInt32(data, i + 4);
                uint C = BitConverter.ToUInt32(data, i + 8);
                uint D = BitConverter.ToUInt32(data, i + 12);

                B += S[0];
                D += S[1];
                for (int j = 1; j <= R; j++)
                {
                    uint t = RotateLeft(B * (2 * B + 1), 5);
                    uint u = RotateLeft(D * (2 * D + 1), 5); // mnozimo D i B

                    A = RotateLeft(A ^ t, (int)u) + S[2 * j];
                    C = RotateLeft(C ^ u, (int)t) + S[2 * j + 1]; // rotiraj i xor-uj za difuziju bitova

                    (A, B, C, D) = (B, C, D, A); // rotiranje reci, mesanje
                }

                A += S[2 * R + 2];
                C += S[2 * R + 3]; // dodavanje kljuceva na kraju rundi

                Buffer.BlockCopy(BitConverter.GetBytes(A), 0, encrypted, i, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(B), 0, encrypted, i + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(C), 0, encrypted, i + 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(D), 0, encrypted, i + 12, 4);
            }
            return encrypted;
        }
        public byte[] RC6Decrypt(byte[] encrypted, byte[] key)
        {
            const int R = 20; // broj rundi
            uint[] S = GenerateS(key, R);

            byte[] original = new byte[encrypted.Length];

            for (int i = 0; i < encrypted.Length; i += 16)
            {
                uint A = BitConverter.ToUInt32(encrypted, i);
                uint B = BitConverter.ToUInt32(encrypted, i + 4);
                uint C = BitConverter.ToUInt32(encrypted, i + 8);
                uint D = BitConverter.ToUInt32(encrypted, i + 12);

                C -= S[2 * R + 3];
                A -= S[2 * R + 2];
                for (int j = R; j >= 1; j--)
                {
                    (A, B, C, D) = (D, A, B, C);
                    uint u = RotateLeft(D * (2 * D + 1), 5);
                    uint t = RotateLeft(B * (2 * B + 1), 5);
                    C = RotateRight(C - S[2 * j + 1], (int)t) ^ u;
                    A = RotateRight(A - S[2 * j], (int)u) ^ t;
                }
                B -= S[0];
                D -= S[1];

                Buffer.BlockCopy(BitConverter.GetBytes(A), 0, original, i, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(B), 0, original, i + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(C), 0, original, i + 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(D), 0, original, i + 12, 4);
            }
            return original;
        }

        public byte[] GenerateIV(int length)
        {
            byte[] iv = new byte[length];
            /*using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv); // bajtovi za nasumicni inicijalizacijski vektor
            }*/
            RandomNumberGenerator.Fill(iv);
            return iv;
        }
        public byte[] OFBRC6(byte[] tekst, byte[] key)
        {
            int k = 16; // duzina bloka
            int brojBlokova = (tekst.Length + k - 1) / k;

            // gen iv
            byte[] niv = GenerateIV(k);
            byte[] kriptovano = new byte[niv.Length + tekst.Length];

            // prvo iv a zatim sifrovani text
            Array.Copy(niv, 0, kriptovano, 0, niv.Length);

            for (int i = 0; i < brojBlokova; i++)
            {
                byte[] rc6Result = RC6Encrypt(niv, key);
                int preostalo = tekst.Length - i * k;
                int blokDuzina = Math.Min(k, preostalo); // ako je poslednji blok manji od k, uzmi samo preostale bajtove
                byte[] blok = new byte[blokDuzina];

                // kopiramo samo validne bajtove u blok
                Array.Copy(tekst, i * k, blok, 0, blokDuzina);

                // XOR sa RC6 rezultatom
                byte[] rezultat = new byte[blokDuzina];
                for (int j = 0; j < blokDuzina; j++)
                {
                    rezultat[j] = (byte)(blok[j] ^ rc6Result[j]);
                }

                int startIndex = niv.Length + i * k;
                Array.Copy(rezultat, 0, kriptovano, startIndex, blokDuzina);

                niv = rc6Result; // novi iv za sledeci blok
            }

            return kriptovano;
        }
        public byte[] OFBRC6Decrypt(byte[] kriptovano, byte[] key)
        {
            int k = 16; // duzina bloka
            int brojBlokova = (kriptovano.Length - k) / k; // manje duzina iv-a
            byte[] niv = new byte[k];
            byte[] tekst = new byte[kriptovano.Length - k];

            // uzimamo iv
            Array.Copy(kriptovano, 0, niv, 0, k);

            for (int i = 0; i < brojBlokova; i++)
            {
                byte[] rc6Result = RC6Encrypt(niv, key);
                byte[] blok = new byte[k];

                Array.Copy(kriptovano, k + i * k, blok, 0, k);

                byte[] rezultat = new byte[blok.Length];
                for (int j = 0; j < blok.Length; j++)
                {
                    rezultat[j] = (byte)(blok[j] ^ rc6Result[j]);
                }

                Array.Copy(rezultat, 0, tekst, i * k, rezultat.Length);

                niv = rc6Result; // novi iv za sledeci blok
            }

            return tekst;
        }



        private void LockFlow_Load(object sender, EventArgs e)
        {
            radioButton1.Checked = false;
            radioButton2.Checked = false;
            radioButton5.Checked = false;
            radioButton6.Checked = false;

            int radius = 50; // ivice

            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(this.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(this.Width - radius, this.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, this.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();

            this.Region = new Region(path);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                radioButton2.Checked = false;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                radioButton1.Checked = false;
            }
        }
        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                radioButton6.Checked = false;
                checkBox1.Checked = false;
                checkBox1.Enabled = false;
                groupBox12.Size = new Size(97, 76);
            }
        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked)
            {
                radioButton5.Checked = false;
                groupBox12.Size = new Size(192, 76);
            }
            checkBox1.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "All Files (*.*)|*.*|Text Files (*.txt)|*.txt|Image Files (*.png;*.jpg)|*.png;*.jpg";
                fileDialog.Title = "Select a File";

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string path = fileDialog.FileName;

                    textBox1.Text = path;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
            string random = "abcdefghiklmnopqrstuvwxyz";

            List<char> randomList = random.ToList(); // string u listu za removeAt

            char[] key = new char[25];

            for (int i = 0; i < 25; i++)
            {
                int index = new Random().Next(randomList.Count); // rand ind
                key[i] = randomList[index];
                randomList.RemoveAt(index); // bez duplikata
            }

            textBox3.Text = new string(key);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                if (!File.Exists(textBox1.Text))
                {
                    MessageBox.Show("Fajl ne postoji! Unesite ispravnu putanju.", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrWhiteSpace(textBox3.Text))
                {
                    string key = textBox3.Text;
                    byte[,] mat = BifidMatrix(key);
                    byte[] fileContent = File.ReadAllBytes(textBox1.Text);
                    //label6.Text = BitConverter.ToString(fileContent);


                    byte[] rc6Key = ExpandKey(key, 32);

                    if (radioButton1.Checked)
                    {
                        byte[]? encryptedContent = null;
                        if (radioButton5.Checked)
                        {
                            encryptedContent = BifidEncryptOther(fileContent, mat);
                            //label6.Text = BitConverter.ToString(encryptedContent);
                        }
                        else if (radioButton6.Checked)
                        {
                            if (!checkBox1.Checked)
                            {
                                encryptedContent = RC6Encrypt(fileContent, rc6Key);
                            }
                            else
                            {
                                encryptedContent = OFBRC6(fileContent, rc6Key);
                            }
                        }
                        else
                        {
                            MessageBox.Show("IZABERITE ALGORITAM!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                            saveFileDialog.Title = "Save Encrypted File";
                            saveFileDialog.DefaultExt = "txt";
                            saveFileDialog.FileName = (Path.GetFileNameWithoutExtension(textBox1.Text) + "_encrypted" + Path.GetExtension(textBox1.Text)); // preporuceni FileName

                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                string outputFilePath = saveFileDialog.FileName;
                                try
                                {
                                    File.WriteAllBytes(outputFilePath, encryptedContent);
                                    MessageBox.Show($"Fajl je uspešno sačuvan na: {outputFilePath}", "Uspeh!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Došlo je do greške prilikom čuvanja fajla: {ex.Message}", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                    else if (radioButton2.Checked)
                    {
                        byte[]? decryptedContent = null;
                        if (radioButton5.Checked)
                        {
                            decryptedContent = BifidDecryptOther(fileContent, mat);
                        }
                        else if (radioButton6.Checked)
                        {
                            if (!checkBox1.Checked)
                            {
                                decryptedContent = RC6Decrypt(fileContent, rc6Key);
                            }
                            else
                            {
                                decryptedContent = OFBRC6Decrypt(fileContent, rc6Key);
                            }
                        }
                        else
                        {
                            MessageBox.Show("IZABERITE ALGORITAM!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                            saveFileDialog.Title = "Save Encrypted File";
                            saveFileDialog.DefaultExt = "txt";
                            saveFileDialog.FileName = (Path.GetFileNameWithoutExtension(textBox1.Text) + "_decrypted" + Path.GetExtension(textBox1.Text)); // preporuceni FileName

                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                string outputFilePath = saveFileDialog.FileName;
                                try
                                {
                                    File.WriteAllBytes(outputFilePath, decryptedContent);
                                    MessageBox.Show($"Fajl je uspešno sačuvan na: {outputFilePath}", "Uspeh!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Došlo je do greške prilikom čuvanja fajla: {ex.Message}", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Generišite ključ!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Izaberite datoteku!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            string targetDir = textBox4.Text;

            if (!Directory.Exists(targetDir))
            {
                MessageBox.Show("Direktorijum ne postoji! Unesite ispravnu putanju.", "Upozorenje", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string[] files = Directory.GetFiles(targetDir); // svi files
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string fileExtension = Path.GetExtension(file).TrimStart('.');
                listBox1.Items.Add($"[{fileExtension.ToUpper()}] {fileName}");
            }

            watcher = new FileSystemWatcher
            {
                Path = targetDir,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.Attributes, // na velicinu, naziv i zadnji upis reaguje
                Filter = "*.*" // sve extenzije
            };

            watcher.Created += OnFileCreated;
            watcher.Renamed += OnFileRenamed; // pise da ne radimo changed i deleted ali ne i renamed.
                                              // Potreban mi je jer pri kreiranju fajla Windows automatski dodeli npr. "New Text Document" za txt fajl pre nego mu ja dam naziv
                                              // ako se file napravi iz cmd-a, ne ukljucuje se rename, logicno


            watcher.EnableRaisingEvents = true;
            MessageBox.Show("Praćenje foldera je započeto!", "Obaveštenje", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e) // koristim Invoke jer promenu komponenti izvrsava samo glavna nit...
        {                                                               // kod Created i Renamed jer su dogadjaji koji su pozvani iz pozadinskih niti (FSW koristi 2. nit za pracenje promena)
            string filePath = e.FullPath;

            string fileName = Path.GetFileName(filePath);
            string fileExtension = Path.GetExtension(filePath).TrimStart('.');

            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = "Unknown";  // nepoznat tip
            }

            // ima bug gde mi duplira fajlove u listi pa ih ucitavam iznova kao refresh
            /*if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(() => { listBox1.Items.Add($"[{fileExtension.ToUpper()}] {fileName}"); }));
            }
            else
            {
                listBox1.Items.Add($"[{fileExtension.ToUpper()}] {fileName}");
            }*/

            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(() => listBox1.Items.Clear()));
            }
            else
            {
                listBox1.Items.Clear();
            }

            string targetDir = textBox4.Text;
            string[] files = Directory.GetFiles(targetDir); // svi files
            foreach (string file in files)
            {
                string fileName1 = Path.GetFileName(file);
                string fileExtension1 = Path.GetExtension(file).TrimStart('.');
                if (listBox1.InvokeRequired)
                {
                    listBox1.Invoke(new Action(() =>
                        listBox1.Items.Add($"[{fileExtension1.ToUpper()}] {fileName1}")
                    ));
                }
                else
                {
                    listBox1.Items.Add($"[{fileExtension1.ToUpper()}] {fileName1}");
                }

            }

            if (label1.InvokeRequired)
            {
                label1.Invoke(new Action(() =>
                {
                    label1.ForeColor = Color.Green;
                    label1.Font = new System.Drawing.Font(label1.Font, FontStyle.Underline);
                    label1.Text = $">{fileName} dodat u direktorijum!";
                }));
            }
            else
            {
                label1.ForeColor = Color.Green;
                label1.Font = new System.Drawing.Font(label1.Font, FontStyle.Underline);
                label1.Text = $">{fileName} dodat u direktorijum!";
            }


            try // auto encrypt kad se doda fajl u dir
            {

                if (string.IsNullOrWhiteSpace(textBox3.Text))
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => { MessageBox.Show($"Nedostaje kljuc!\nFajl {fileName} neće biti enkriptovan!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning); }));
                    }
                    else
                    {
                        MessageBox.Show($"Nedostaje kljuc!\nFajl {fileName} neće biti enkriptovan!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else if (string.IsNullOrWhiteSpace(textBox5.Text))
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => { MessageBox.Show("Nedostaje Destination Path!\nFajl neće biti enkriptovan!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning); }));
                    }
                    else
                    {
                        MessageBox.Show("Nedostaje Destination Path!\nFajl neće biti enkriptovan!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else if (textBox4.Text == textBox5.Text)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => { MessageBox.Show("Target i Destination direktorijumi moraju biti razliciti!\nFajl neće biti enkriptovan!", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error); }));
                    }
                    else
                    {
                        MessageBox.Show("Target i Destination direktorijumi moraju biti razliciti!\nFajl neće biti enkriptovan!", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    byte[] fileContent = File.ReadAllBytes(filePath);
                    string key = textBox3.Text;
                    byte[,] mat = BifidMatrix(key);
                    byte[]? encryptedContent = null;

                    byte[] rc6Key = ExpandKey(key, 32);
                    if (radioButton5.Checked)
                    {
                        encryptedContent = BifidEncryptOther(fileContent, mat);
                    }
                    else if (radioButton6.Checked)
                    {
                        if (!checkBox1.Checked)
                        {
                            encryptedContent = RC6Encrypt(fileContent, rc6Key);
                        }
                        else
                        {
                            encryptedContent = OFBRC6(fileContent, rc6Key);
                        }
                    }
                    else
                    {
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() => { MessageBox.Show("IZABERITE ALGORITAM!\nFajl neće biti enkriptovan!", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error); }));
                        }
                        else
                        {
                            MessageBox.Show("IZABERITE ALGORITAM!\nFajl neće biti enkriptovan!", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    string outputFilePath = textBox5.Text + "\\" + (Path.GetFileNameWithoutExtension(filePath) + "_encrypted" + Path.GetExtension(filePath));

                    //File.WriteAllText(outputFilePath, encryptedContent);
                    if (encryptedContent != null)
                        File.WriteAllBytes(outputFilePath, encryptedContent);

                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => { MessageBox.Show($"Fajl {fileName} je enkriptovan!", "Obaveštenje:", MessageBoxButtons.OK, MessageBoxIcon.Information); }));
                    }
                    else
                    {
                        MessageBox.Show($"Fajl {fileName} je enkriptovan!", "Obaveštenje:", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // messBox se izvrsava samo u glavnoj niti jer interaguje sa korisnikom
                    }
                }
            }
            catch (Exception ex)
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => { MessageBox.Show($"Došlo je do greške: {ex.Message}", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error); }));
                }
                else
                {
                    MessageBox.Show($"Došlo je do greške: {ex.Message}", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            Thread.Sleep(800);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            string oldFileName = Path.GetFileName(e.OldFullPath);
            string newFileName = Path.GetFileName(e.FullPath);
            string fileExtension = Path.GetExtension(e.FullPath).TrimStart('.');

            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = "Unknown"; // nepoznat tip
            }

            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(() =>
                {
                    for (int i = listBox1.Items.Count - 1; i >= 0; i--)
                    {
                        if (listBox1.Items[i] != null && listBox1.Items[i].ToString().Contains(oldFileName))
                        {
                            listBox1.Items.RemoveAt(i);
                        }
                    }
                    listBox1.Items.Add($"[{fileExtension.ToUpper()}] {newFileName}");
                    listBox1.Refresh();
                }));
            }
            else
            {
                for (int i = listBox1.Items.Count - 1; i >= 0; i--)
                {
                    if (listBox1.Items[i] != null && listBox1.Items[i].ToString().Contains(oldFileName))
                    {
                        listBox1.Items.RemoveAt(i);
                    }
                }
                listBox1.Items.Add($"[{fileExtension.ToUpper()}] {newFileName}");
                listBox1.Refresh();
            }
            if (label1.InvokeRequired)
            {
                label1.Invoke(new Action(() =>
                {
                    label1.ForeColor = Color.Blue;
                    label1.Font = new System.Drawing.Font(label1.Font, FontStyle.Underline);
                    label1.Text = $">{oldFileName} je izmenjen!";
                }));
            }
            else
            {
                label1.ForeColor = Color.Blue;
                label1.Font = new System.Drawing.Font(label1.Font, FontStyle.Underline);
                label1.Text = $">{oldFileName} je izmenjen!";
            }
            Thread.Sleep(800);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a Folder";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;
                    textBox4.Text = folderPath;
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a Folder";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;
                    textBox5.Text = folderPath;
                }
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void panel4_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            panel5.Visible = true;
            panel6.Visible = false;
            panel7.Visible = false;
            panel8.Visible = true;
            panel9.Visible = false;
            panel10.Visible = true;
            panel11.Visible = false;
            panel12.Visible = false;
            panel13.Visible = false;

            radioButton1.Checked = false;
            radioButton2.Checked = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            panel5.Visible = false;
            panel6.Visible = true;
            panel7.Visible = false;
            panel8.Visible = false;
            panel9.Visible = true;
            panel10.Visible = true;
            panel11.Visible = false;
            panel12.Visible = false;
            panel13.Visible = false;

            listBox1.Items.Clear();
            watcher.EnableRaisingEvents = false;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            panel5.Visible = false;
            panel6.Visible = false;
            panel7.Visible = true;
            panel8.Visible = false;
            panel9.Visible = false;
            panel10.Visible = true;
            panel11.Visible = true;
            panel12.Visible = false;
            panel13.Visible = false;

            listBox1.Items.Clear();
            watcher.EnableRaisingEvents = false;
            radioButton1.Checked = false;
            radioButton2.Checked = false;
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/akgram",
                UseShellExecute = true
            });
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/akgram",
                UseShellExecute = true
            });
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            panel5.Visible = false;
            panel6.Visible = false;
            panel7.Visible = false;
            panel8.Visible = false;
            panel9.Visible = false;
            panel10.Visible = false;
            panel11.Visible = false;
            panel12.Visible = true;
            panel13.Visible = true;
        }


        ///////////////////////  SERVER
        private async void button12_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                label3.ForeColor = Color.Red;
                UpdateStatus(label3, "Unesite port!");
                return;
            }

            await ServerAsync();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                isServerRunning = false;
                serverSocket?.Close();
                label3.ForeColor = Color.White;
                UpdateStatus(label3, "Server je zaustavljen.");
            }
            catch (Exception ex)
            {
                label3.ForeColor = Color.Red;
                UpdateStatus(label3, $"Greška u prekidu slušanja: \n{ex.Message}");
            }
        }

        private void UpdateStatus(Label statusLabel, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => statusLabel.Text = message));
            }
            else
            {
                statusLabel.Text = message;
            }
        }

        async Task ServerAsync()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, Int32.Parse(textBox2.Text)));
                serverSocket.Listen(5);

                isServerRunning = true;

                label3.ForeColor = Color.Green;
                UpdateStatus(label3, "Server je spreman i osluškuje konekcije");

                while (isServerRunning)
                {
                    Socket clientSocket = await serverSocket.AcceptAsync();
                    UpdateStatus(label3, "Klijent je povezan");

                    // Rukovanje sa klijentom u posebnoj task
                    await Task.Run(() => HandleClientAsyncBasic(clientSocket));
                }
            }
            catch (Exception)
            {
                if (isServerRunning)
                {
                    label3.ForeColor = Color.Red;
                    UpdateStatus(label3, $"Greška! Proveri Port.");
                }
                else
                {
                    UpdateStatus(label3, "Server je zaustavljen.");
                }
            }
            finally
            {
                serverSocket?.Close();
                isServerRunning = false;
            }
        }

        private async Task HandleClientAsyncBasic(Socket clientSocket)
        {
            try
            {
                using (NetworkStream networkStream = new NetworkStream(clientSocket))
                using (var reader = new BinaryReader(networkStream, Encoding.UTF8, leaveOpen: true))
                {
                    string fileName = reader.ReadString(); // ime fajla
                    long fileSize = reader.ReadInt64(); // velicina
                    int hashLength = reader.ReadInt32(); // duzina hash-a
                    byte[] receivedHash = reader.ReadBytes(hashLength); // hash
                    byte[] fileBytes = reader.ReadBytes((int)fileSize); // sadrzaj

                    // provera hash-a
                    using (SHA1 sha1 = SHA1.Create())
                    {
                        byte[] computedHash = sha1.ComputeHash(fileBytes);
                        bool isValid = receivedHash.SequenceEqual(computedHash);

                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string filePath = Path.Combine(desktopPath, fileName);
                        File.WriteAllBytes(filePath, fileBytes);

                        if (isValid)
                        {
                            label3.ForeColor = Color.Green;
                            UpdateStatus(label3, "Fajl je uspešno primljen i verifikovan!");
                            MessageBox.Show($"Fajl je snimljen na Desktop:\n{filePath}", "Uspešno!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            label3.ForeColor = Color.Red;
                            UpdateStatus(label3, "Integritet fajla nije prošao proveru.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                label3.ForeColor = Color.Red;
                UpdateStatus(label3, $"Greška pri rukovanju sa klijentom: \n{ex.Message}");
            }
            finally
            {
                clientSocket.Close();
            }
        }

        private string GenerateSHA1HashFromBytes(byte[] fileBytes)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(fileBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }


        //////////////////// CLIENT
        private void button15_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "All Files (*.*)|*.*|Text Files (*.txt)|*.txt|Image Files (*.png;*.jpg)|*.png;*.jpg";
                fileDialog.Title = "Select a File";

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string path = fileDialog.FileName;
                    textBox6.Text = path;
                }
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (!File.Exists(textBox6.Text))
            {
                MessageBox.Show("Fajl ne postoji! Unesite ispravnu putanju.", "Upozorenje", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            label6.ForeColor = Color.White;
            label6.Font = new System.Drawing.Font(label6.Font, FontStyle.Underline | FontStyle.Bold);
            label6.Text = $">{GenerateSHA1Hash(textBox6.Text)}";
        }

        private string GenerateSHA1Hash(string filePath)
        {
            using (var sha1 = SHA1.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha1.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private void SendFile(string filePath)
        {
            try
            {
                byte[] fileContent = File.ReadAllBytes(filePath);
                string key = textBox3.Text;
                byte[,] mat = BifidMatrix(key);
                byte[]? encryptedContent = null;

                if (!string.IsNullOrWhiteSpace(textBox3.Text))
                {
                    byte[] rc6Key = ExpandKey(key, 32);
                    if (radioButton5.Checked)
                    {
                        encryptedContent = BifidEncryptOther(fileContent, mat);
                    }
                    else if (radioButton6.Checked)
                    {
                        if (!checkBox1.Checked)
                        {
                            encryptedContent = RC6Encrypt(fileContent, rc6Key);
                        }
                        else
                        {
                            encryptedContent = OFBRC6(fileContent, rc6Key);
                        }
                    }
                    else
                    {
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() => { MessageBox.Show("IZABERITE ALGORITAM!\nFajl neće biti enkriptovan!", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error); }));
                        }
                        else
                        {
                            MessageBox.Show("IZABERITE ALGORITAM!\nFajl neće biti enkriptovan!", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Generišite ključ!", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }




                byte[] fileBytes = encryptedContent;
                string fileName = Path.GetFileName(filePath);
                long fileSize = fileBytes.Length;

                // generisanje SHA1 hash-a
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] hashBytes = sha1.ComputeHash(fileBytes);

                    using (var client = new TcpClient(textBox8.Text, Convert.ToInt32(textBox7.Text)))
                    using (var stream = client.GetStream())
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        // slanje podataka: ime, velicina, duzina hash-a, hash bajtovi, sadrzaj fajla
                        writer.Write(fileName); // string
                        writer.Write(fileSize); // long
                        writer.Write(hashBytes.Length); // int
                        writer.Write(hashBytes); // byte[]
                        writer.Write(fileBytes); // byte[]
                    }
                }

                label5.ForeColor = Color.Green;
                UpdateStatus(label5, "Fajl sa hash-om je poslat!");
                textBox6.Text = "";
                UpdateStatus(label6, "");
            }
            catch (Exception)
            {
                label5.ForeColor = Color.Red;
                UpdateStatus(label5, "Fajl sa hash-om nije poslat!");
                MessageBox.Show("Server nije aktivan ili ne osluškuje na unetoj IP adresi i portu.", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            string filePath = textBox6.Text;
            if (!string.IsNullOrEmpty(filePath))
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Fajl ne postoji! Unesite ispravnu putanju.", "Upozorenje!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SendFile(filePath);
            }
            else
            {
                MessageBox.Show("Izaberite fajl pre slanja.", "Greška!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
