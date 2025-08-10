using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace KerningAdjuster
{
    public partial class Form1 : Form
    {
        List<float> fontWidths;
        byte[] font;
        List<string> codepoint;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog OF = new OpenFileDialog();
            OF.Title = "Select your font_widths file.";
            OF.Filter = "Width table (*.width_table)|*.width_table|All files (*.*)|*.*";

            try
            {
                if (OF.ShowDialog() == DialogResult.OK)
                {
                    byte[] widths = File.ReadAllBytes(OF.FileName);
                    fontWidths = new List<float>();

                    if (sender == null)
                    {
                        for (int i = 0; i < widths.Length; i++)
                        {
                            byte b1 = (byte)((widths[i] >> 4) & 0xF);
                            byte b2 = (byte)(widths[i] & 0xF);

                            fontWidths.Add((float)b1);
                            fontWidths.Add((float)b2);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < widths.Length; i += 4)
                        {
                            byte[] width = widths.Skip(i).Take(4).Reverse().ToArray();
                            fontWidths.Add(BitConverter.ToSingle(width, 0));
                        }
                    }
                }
                else
                    return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: ", ex.Message);
                return;
            }

            OF.Title = "Select your font_static file.";
            OF.Filter = "Font (*.font_static)|*.font_static|All files (*.*)|*.*";

            try
            {
                if (OF.ShowDialog() == DialogResult.OK)
                {
                    font = File.ReadAllBytes(OF.FileName);

                }
                else
                {
                    font = Resource1.eng;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: ", ex.Message);
                return;
            }

            OF.Title = "Select your codepoint.";
            OF.Filter = "Codepoint (*.txt)|*.txt|All files (*.*)|*.*";

            try
            {
                if (OF.ShowDialog() == DialogResult.OK)
                {
                    codepoint = File.ReadAllLines(OF.FileName).ToList();
                }
                else
                {
                    codepoint = Resource1.codep.Split(Environment.NewLine.ToCharArray()).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: ", ex.Message);
                return;
            }

            textBox1.TextChanged -= textBox1_TextChanged;

            FillData();
            NewPreview(textBox1.Text);

            textBox1.TextChanged += textBox1_TextChanged;
        }

        private void FillData()
        {
            dataGridView1.Rows.Clear();

            for (int i = 1; i < codepoint.Count; i++)
            {

                if (fontWidths.Count > i - 1)
                    dataGridView1.Rows.Add(new object[] { codepoint[i], fontWidths[i - 1] });
            }

            textBox1.Text = codepoint[0].Replace("\\n", Environment.NewLine);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            NewPreview(textBox1.Text);
        }

        public void NewPreview(string text)
        {
            List<byte> codes = new List<byte>();

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                    codes.Add((byte)ZeldaMessage.Data.MsgControlCode.LINE_BREAK);
                else if (text[i] == '\r')
                    continue;
                else if (text[i] == ' ')
                    codes.Add(0x20);
                else
                {
                    int idx = codepoint.FindIndex(x => x == text[i].ToString().Trim());

                    if (idx != -1)
                        codes.Add((byte)(idx + 0x1F));
                }
            }

            codes.Add((byte)ZeldaMessage.Data.MsgControlCode.PERSISTENT);

            ZeldaMessage.MessagePreview pr = new ZeldaMessage.MessagePreview(ZeldaMessage.Data.BoxType.None_Black, codes.ToArray(), fontWidths.ToArray(), font, true);
            Bitmap b = pr.GetPreview(0, true, 1.5f);

            if (b != null)
                pictureBox1.Image = b;

        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            List<float> newWidths = new List<float>();

            int curIndex = 0;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells.Count != 2)
                    return;

                float f;
                string val;

                try
                {
                    if (row.Cells[1].Value == null)
                        continue;

                    val = row.Cells[1].Value.ToString();
                    f = (float)Convert.ToDecimal(val);

                }
                catch (Exception ex)
                {
                    row.Cells[1].Value = fontWidths[curIndex].ToString();
                    f = fontWidths[curIndex];
                }

                newWidths.Add(f);
                curIndex++;
            }

            fontWidths = newWidths;
            NewPreview(textBox1.Text);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog SF = new SaveFileDialog();
            SF.FileName = "newfont.width_table";

            if (SF.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    List<byte> allBytes = new List<byte>();

                    foreach (float width in fontWidths)
                    {
                        byte[] floatBytes = BitConverter.GetBytes(width);
                        byte[] reversedBytes = floatBytes.Reverse().ToArray();
                        allBytes.AddRange(reversedBytes);
                    }

                    File.WriteAllBytes(SF.FileName, allBytes.ToArray());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: ", ex.Message);
                    return;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog SF = new SaveFileDialog();
            SF.FileName = "newfont.width_table";

            if (SF.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    List<byte> allBytes = new List<byte>();

                    byte curByte = 0;
                    bool shift = true;

                    foreach (float width in fontWidths)
                    {
                        if (width > 15)
                            throw new Exception("Widths cannot be over 15");

                        byte w = (byte)width;

                        if (shift == true)
                            curByte = 0;

                        if (shift == true)
                        {
                            curByte |= (byte)(w << 4);
                            shift = false;
                        }
                        else
                        {
                            curByte |= w;
                            allBytes.Add(curByte);
                            shift = true;
                        }
                    }

                    File.WriteAllBytes(SF.FileName, allBytes.ToArray());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: ", ex.Message);
                    return;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            button1_Click(null, null);
        }
    }
}
