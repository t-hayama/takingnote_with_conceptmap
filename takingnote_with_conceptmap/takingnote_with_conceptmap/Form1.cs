using System;
using NMeCab;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.CodeDom.Compiler;

namespace takingnote_with_conceptmap
{
    public partial class Form1 : Form
    {
        Graphics g;
        Node nd1;
        ArrayList al_nodes;
        ArrayList al_links;
        Node node_move;
        Node node_linked;

        public Form1()
        {
            InitializeComponent();
            g = this.panel1.CreateGraphics();
            al_nodes = new ArrayList();
            al_links = new ArrayList();
            node_move = new Node();
            node_linked = new Node();

            this.DoubleBuffered = true;

            this.richTextBox1.KeyDown += RichTextBox1_KeyDown;
            this.panel1.MouseDown += Panel1_MouseDown;
            this.panel1.MouseUp += Panel1_MouseUp;
            this.panel1.MouseMove += Panel1_MouseMove;
            this.panel1.MouseDoubleClick += Panel1_MouseDoubleClick;
            this.panel1.MouseClick += Panel1_MouseClick;
        }

        private void Panel1_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                g.Clear(Color.White);

                foreach (Node nd in al_nodes)
                {
                    if (nd.rec.X - 10 < e.X && e.X < nd.rec.X + nd.rec.Width * nd.concept.Length * nd.freq
                        && nd.rec.Y - 10 < e.Y && e.Y < nd.rec.Y + (nd.rec.Height - 2) * nd.freq)
                    {
                        if (nd.flag_dclicked)
                        {
                            nd.flag_dclicked = false;
                            if(node_linked != null)
                            {
                                node_linked.flag_dclicked = false;
                            }
                            node_linked = null;
                        }
                        else if(node_linked != null)
                        {
                            nd.flag_dclicked = false;
                            node_linked.flag_dclicked = false;
                            node_linked.al_link.Add(nd);
                            node_linked = null;
                        }
                        //MessageBox.Show("Rolled back single click change.");

                        break;
                    }
                }
                if (node_linked != null)
                {
                    node_linked.flag_dclicked = false;
                    node_linked = null;
                }

                foreach (Node nd in al_nodes)
                {
                    nd.repainting(g);
                }
            }
            catch
            {
                Thread.Sleep(200);
            }
        }

        private void Panel1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                g.Clear(Color.White);

                foreach (Node nd in al_nodes)
                {
                    if (nd.rec.X - 10 < e.X && e.X < nd.rec.X + nd.rec.Width * nd.concept.Length * nd.freq
                        && nd.rec.Y - 10 < e.Y && e.Y < nd.rec.Y + (nd.rec.Height - 2) * nd.freq)
                    {
                        nd.flag_dclicked = true;
                        node_linked = nd;
                        //MessageBox.Show("Rolled back single click change.");

                        break;
                    }
                }
                foreach (Node nd in al_nodes)
                {
                    nd.repainting(g);
                }
            }
            catch
            {
                Thread.Sleep(200);
            }
        }

        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (node_move != null) {
                    node_move.rec.X = e.X;
                    node_move.rec.Y = e.Y;

                    g.Clear(Color.White);
                    foreach (Node nd in al_nodes)
                    {
                        nd.repainting(g);
                    }
                }else if(node_linked != null)
                {
                    g.Clear(Color.White);
                    foreach (Node nd in al_nodes)
                    {
                        nd.repainting(g);
                    }
                    Point point = new Point(node_linked.rec.X + node_linked.concept.Length * 8 / 2, 
                        node_linked.rec.Y + node_linked.rec.Height / 2);
                    g.DrawLine(new Pen(Brushes.Red), new Point(e.X, e.Y), point);

                }
            }
            catch
            {
                Thread.Sleep(200);
            }

        }

        private async void Panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (node_move != null)
            {
                node_move.flag_clicked = false;
                node_move = null;
                await Task.Run(() =>
                {
                    HeavyMethod1(g, new ArrayList());
                });
            }
        }

        private void Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                g.Clear(Color.White);

                foreach (Node nd in al_nodes)
                {
                    if (nd.rec.X - 10 < e.X && e.X < nd.rec.X + nd.rec.Width * nd.concept.Length * nd.freq
                        && nd.rec.Y - 10 < e.Y && e.Y < nd.rec.Y + (nd.rec.Height - 2) * nd.freq)
                    {
                        nd.flag_clicked = true;
                        node_move = nd;
                        break;
                    }
                }
                foreach (Node nd in al_nodes)
                {
                    nd.repainting(g);
                }
            }
            catch
            {
                Thread.Sleep(200);
            }

        }

        async private void RichTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string text = richTextBox1.Text;
                string[] array_str = text.Split("\n");
                var tagger = MeCabTagger.Create();

                //改行ひとつ前の文を形態素解析し、その結果を記録
                string result = tagger.Parse(array_str[array_str.Length - 1]);
                // 名詞だけを取り出す
                ArrayList al_concept = get_Concept(result);
                // それを配置させる
                await Task.Run(() =>
                {
                    HeavyMethod1(g, al_concept);
                });

            }
        }

        private ArrayList get_Concept(string results)
        {
            ArrayList al_hinshi = new ArrayList();
            al_hinshi.Add("名詞");
            al_hinshi.Add("未知語");

            ArrayList array_concept = new ArrayList();

            foreach (string result_tmp in results.Split("\n"))
            {
                string[] parts = result_tmp.Split("\t");
                if(parts.Length == 2)
                {
                    string[] info = parts[1].Split(",");
                    if (al_hinshi.Contains(info[0]))
                    {
                        array_concept.Add(parts[0]);
                    }
                }
            }

            return array_concept;
        }

        public void HeavyMethod1(Graphics g, ArrayList al_newconcept)
        {
            ArrayList al_nodes_tmp = new ArrayList();
            ArrayList al_nodes_tmp2 = new ArrayList();

            foreach (string concept in al_newconcept)
            {
                // 既存していれば，頻度を数える
                Boolean flag = false;
                foreach (Node nd in al_nodes)
                {
                    if (nd.concept.Equals(concept))
                    {
                        nd.freq++;
                        al_nodes_tmp2.Add(nd);
                        flag = true;
                        break;
                    }

                }
                // 新規であれば，新たに加える
                if (!flag)
                {
                    nd1 = new Node();
                    nd1.concept = concept;
                    al_nodes_tmp.Add(nd1);
                }
            }

            // ノードにリンク情報を加える（整理必要、特に重複）
            foreach (Node nodes in al_nodes_tmp)
            {
                nodes.al_link.AddRange(al_nodes_tmp);
                nodes.al_link.AddRange(al_nodes_tmp2);
            }
            foreach (Node nodes in al_nodes_tmp2)
            {
                nodes.al_link.AddRange(al_nodes_tmp);
                nodes.al_link.AddRange(al_nodes_tmp2);
            }

            // 以下、グラフ自動描画の処理 /////////////////////

            al_nodes.AddRange(al_nodes_tmp);

            float length_s = 250f;
            float distance = 0.0f;
            float distance_x = 0.0f;
            float distance_y = 0.0f;
            float force = 0.0f;
            float force_x = 0.0f;
            float force_y = 0.0f;

            double sum_of_energy_tmp = 0;

            do
            {
                double sum_of_energy = 0;

                foreach (Node nd in al_nodes)
                {
                    force_x = 0;
                    force_y = 0;

                    foreach (Node nd_tmp in al_nodes)
                    {
                        if (nd == nd_tmp)
                        {
                            continue;
                        }
                        //                    力:= 力 + 定数 / 距離（ノード1, ノード2) ^2  // クーロン力()

                        distance_x = nd.rec.X - nd_tmp.rec.X;
                        distance_y = nd.rec.Y - nd_tmp.rec.Y;
                        distance = (float)Math.Sqrt(distance_x * distance_x + distance_y * distance_y);

                        if (distance <= 1)
                        {
                            distance = length_s;
                        }

                        force = (float)4.0 / distance / distance;

                        force_x = force_x - force * distance_x / distance;
                        force_y = force_y - force * distance_y / distance;
                    }

                    foreach (Node nd_tmp in nd.al_link)
                    {
                        if (nd == nd_tmp)
                        {
                            continue;
                        }

                        //                    力:= 力 + バネ定数 * (距離(ノード1, ノード2) - バネの自然長)
                        distance_x = nd.rec.X - nd_tmp.rec.X;
                        distance_y = nd.rec.Y - nd_tmp.rec.Y;
                        distance = (float)Math.Sqrt(distance_x * distance_x + distance_y * distance_y);

                        if (distance <= 1)
                        {
                            distance = length_s;
                        }

                        force = (float)((float)0.1 * (distance - length_s));

                        force_x = force_x - force * distance_x / distance;
                        force_y = force_y - force * distance_y / distance;
                    }

                    //
                    //ノード１の速度 := (ノード1の速度 +　微小時間 * 力 / ノード1の質量) * 減衰定数
                    //ノード１の位置:= ノード1の位置 + 微小時間 * ノード1の速度
                    nd.rec.X = nd.rec.X + (int)(0.95 * force_x);
                    nd.rec.Y = nd.rec.Y + (int)(0.95 * force_y);

                    //運動エネルギーの合計:= 運動エネルギーの合計 + ノード1の質量 * ノード1の速度 ^ 2

                    sum_of_energy = sum_of_energy + (double)5 * (force_x * force_x + force_y * force_y);

                }
                try
                {
                    // Refresh
                    g.Clear(Color.White);

                    // repaint
                    foreach (Node nd in al_nodes)
                    {
                        nd.repainting(g);
                    }

                }
                catch
                {
                    Thread.Sleep(200);
                    continue;
                }

                Thread.Sleep(200);

                // 描画の収束処理：　ノード数に応じて、条件を緩くする
                if (sum_of_energy < 7 * al_nodes.Count || sum_of_energy == sum_of_energy_tmp)
                {
                    break;
                }
                sum_of_energy_tmp = sum_of_energy;
            } while (true);
        }


        async void button1_ClickAsync(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                HeavyMethod1(g, new ArrayList());
            });
        }

        private class Node
        {
            Random rand = new System.Random();
            public Rectangle rec;
            public double vx;
            public double vy;
            public double weight = 1.0;
            public string concept = "";
            public int font_size = 8;
            public int freq = 1;
            public ArrayList al_link = new ArrayList();
            public System.Drawing.Brush color_fnt2 = Brushes.BlueViolet;
            public System.Drawing.Brush color_fnt_clc2 = Brushes.Red;
            public bool flag_clicked = false;
            public bool flag_dclicked = false;
            public int frame_width = 500;
            public int frame_height = 500;

            public Node()
            {
                rec = new Rectangle(rand.Next(10, 490), rand.Next(10, 490), 10, 10);
                vx = 0.0;
                vy = 0.0;
            }

            public void repainting(Graphics g)
            {
                // 枠内に収まるように
                if (rec.X <= 10)
                {
                    rec.X = 20;
                }
                if (rec.X >= frame_width)
                {
                    rec.X = frame_width - 20;
                }
                if (rec.Y <= 10)
                {
                    rec.Y = 20;
                }
                if (rec.Y >= frame_height)
                {
                    rec.Y = frame_height - 20;
                }

                //g.DrawEllipse(new Pen(Brushes.DeepSkyBlue), rec);
                if(flag_clicked || flag_dclicked)
                {
                    g.DrawString(concept, new Font("MS UI Gothic", (int)(font_size * (Math.Log(freq)+1))), color_fnt_clc2,
                        rec.X
                        , rec.Y);
                }
                else
                {
                    g.DrawString(concept, new Font("MS UI Gothic", (int)(font_size * (Math.Log(freq) + 1))), color_fnt2,
                        rec.X
                        , rec.Y);

                }
                //                g.DrawRectangle(new Pen(Brushes.Gray),rec);


                // 要調整
                foreach (Node node in al_link)
                {
                    Point point = new Point(node.rec.X+node.concept.Length*8/2, node.rec.Y+node.rec.Height/2);
                    g.DrawLine(new Pen(Brushes.GreenYellow), new Point(rec.X+(concept.Length*8)/2, rec.Y+rec.Height/2), point);
                }
            }

        }
    }
}
