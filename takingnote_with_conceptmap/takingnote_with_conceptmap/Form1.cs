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

/*
 *  コンセプト追加を連打すると、エラーになる
 */
namespace takingnote_with_conceptmap
{
    public partial class Form1 : Form
    {
        Graphics g;
        Node nd1;
        ArrayList al_nodes;
        ArrayList al_links;

        public Form1()
        {
            InitializeComponent();
            g = this.panel1.CreateGraphics();
            al_nodes = new ArrayList();
            al_links = new ArrayList();

            this.richTextBox1.KeyDown += RichTextBox1_KeyDown;
        }

        async private void RichTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //形態素解析されるもとの文章
                string text = richTextBox1.Text;// "私はプログラマーです";

                string[] array_str = text.Split("\n");

                var tagger = MeCabTagger.Create();

                //形態素解析を行い結果を記録
                string result = tagger.Parse(array_str[array_str.Length - 1]);

                ArrayList al_concept = get_Concept(result);
                await Task.Run(() =>
                {
                    HeavyMethod1(g, al_concept);
                });

                //richTextBox1.Text = get_Concept(result).ToString();
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
//                array_concept.Add(result_tmp);
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
                if (!flag)
                {
                    nd1 = new Node();
                    nd1.concept = concept;
                    al_nodes_tmp.Add(nd1);
                }
            }

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

            al_nodes.AddRange(al_nodes_tmp);

            float length_s = 200f;
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

                        force = (float)0.4 / distance / distance;

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
            public double vx = 0.0;
            public double vy = 0.0;
            public double weight = 1.0;
            public string concept = "";
            public int font_size = 8;
            public int freq = 1;
            public ArrayList al_link = new ArrayList();

            public Node()
            {
                rec = new Rectangle(rand.Next(100, 400), rand.Next(100, 400), 10, 10);
                vx = 0.0;
                vy = 0.0;
            }

            public void repainting(Graphics g)
            {
                if (rec.X <= 0)
                {
                    rec.X = 1;
                }
                if (rec.X >= 500)
                {
                    rec.X = 490;
                }
                if (rec.Y <= 0)
                {
                    rec.Y = 1;
                }
                if (rec.Y >= 500)
                {
                    rec.Y = 490;
                }

                //g.DrawEllipse(new Pen(Brushes.DeepSkyBlue), rec);
                g.DrawString(concept, new Font("MS UI Gothic", font_size*freq), Brushes.DeepSkyBlue, 
                    rec.X - font_size * freq / 2
                    , rec.Y - 12);
                foreach (Node node in al_link)
                {
                    Point point = new Point(node.rec.X, node.rec.Y);
                    g.DrawLine(new Pen(Brushes.GreenYellow), new Point(rec.X, rec.Y), point);
                }
            }

        }
    }
}
